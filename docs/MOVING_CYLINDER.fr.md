# Échange gazeux du cylindre mobile

[English](MOVING_CYLINDER.md) · [简体中文](MOVING_CYLINDER.zh-CN.md) · **Français** · [Русский](MOVING_CYLINDER.ru.md) · [日本語](MOVING_CYLINDER.ja.md) · [한국어](MOVING_CYLINDER.ko.md) · [Deutsch](MOVING_CYLINDER.de.md) · [Español](MOVING_CYLINDER.es.md) · [Italiano](MOVING_CYLINDER.it.md) · [Português](MOVING_CYLINDER.pt-BR.md)

Un `gas_cylinder` relie un vilebrequin en rotation à une chambre à gaz. Contrairement à la référence
adiabatique fermée, cette chambre porte une masse et une énergie interne indépendantes, de sorte que
les restrictions et les liaisons de paroi peuvent changer son état pendant que la pression entraîne le vilebrequin.
Le composant est disponible par le cœur, le JSON, la CLI, le MCP et les assets portables. Il ne
modélise ni l'inertie du piston ni une chimie détaillée. Des composants distincts de
[combustion prémélangée](PREMIXED_COMBUSTION.fr.md) et de [calage sur l'angle vilebrequin](VALVE_TIMING.fr.md)
fournissent maintenant la conversion d'énergie du carburant et commandent les restrictions connectées.

## Contrat de modèle

```csharp
var definition = new ModelDefinition
{
    StepNanoseconds = 50_000,
    Nodes = [NodeDefinition.Rotor(1, 0.2, 60),
        NodeDefinition.CylinderGas(2, 100_000, 300),
        NodeDefinition.Thermal(3, 500, 350)],
    Components = [ComponentDefinition.GasCylinder(10, 1, 2, new()
    {
        Bore = new(86, Unit.Millimeter), Stroke = new(86, Unit.Millimeter),
        RodLength = new(143, Unit.Millimeter), Phase = new(0, Unit.Radian),
        CompressionRatio = 10, BackPressure = new(1, Unit.Bar)
    }), ComponentDefinition.GasReservoir(11, 2, 50e-6, 110_000, 300, 0.8, 100, 1),
        ComponentDefinition.GasHeatLink(12, 2, 3, 2.5)]
};
```

Le nœud gazeux fournit la pression absolue initiale, la température, R et gamma. Sa quantité `Storage`
est nulle/None : exactement un composant cylindre possède le volume. Le compilateur
dérive la masse et l'énergie initiales de la géométrie à l'angle initial du vilebrequin, phase
comprise. Il rejette un volume spécifié indépendamment, ou deux propriétaires cylindre pour une chambre.
Les nœuds gazeux fixes non connectés exigent toujours un volume explicite positif.

En JSON, utilisez `domain: "gas"` et omettez `storage` pour une chambre mobile. Le composant `gas_cylinder`
exige `node_a` (en rotation), `node_b` (gazeux) et les paramètres `bore`, `stroke`,
`rod_length`, `phase`, `compression_ratio` et `back_pressure`. Aucune composition ni aucun état gazeux initial
n'est dupliqué dans ce composant. Le nœud gazeux expose la pression, la température,
la masse et l'énergie interne ; le cylindre expose le volume, le déplacement du piston et le couple au
vilebrequin. Les ports, les restrictions, les bornes d'ouverture et les liaisons de paroi utilisent le
[contrat de réseau gazeux](GAS_NETWORK.fr.md) existant.

Les modèles qui contiennent des chambres mobiles sans restrictions calées ni suivi prémélangé déclarent la fidélité `moving_cylinder_gas_exchange` et
ajoutent l'balise d'empreinte de solveur 5. Les modèles linéaires, à cylindre fermé et à volume fixe seul
existants conservent leurs empreintes et leur avancement. La composition reste fixe, les nœuds gazeux
connectés doivent s'accorder, et tous les paramètres restent `unverified`.

## Équations et couplage conservatif

Pour un gaz caloriquement parfait à composition fixe :

```text
p = (gamma - 1) U / V(theta)
T = U / (m cv)
dm/dt = sum(inflow) - sum(outflow)
dU/dt = -p dV/dt + Q_wall + sum(mdot_in h_in) - sum(mdot_out h_out)
tau_gas = (p - p_back) dV/dtheta
```

Le bilan de masse et d'énergie suit le premier principe usuel des systèmes ouverts ; voir
[les équations de volume de contrôle de Cantera](https://www.cantera.org/stable/reference/reactors/controlreactor.html).
Cette référence appuie les équations, pas le schéma d'intégration ni la validation de Power!.
Le gaz utilise la loi de tuyère compressible bidirectionnelle existante, plutôt que l'implémentation
de vanne linéaire et unidirectionnelle de Cantera. Le travail de contre-pression est un travail de source externe ;
l'enthalpie de réservoir et l'échange de paroi conservent leurs signes de bilan existants.

L'implémentation utilise une décomposition d'opérateur symétrique pour les modèles qui contiennent des chambres mobiles :

1. Avancer l'échange gazeux et la chaleur de paroi d'un demi-tick, à la géométrie de vilebrequin du début.
2. Résoudre l'électromécanique couplée et le travail de pression adiabatique sur le tick entier, avec
   le solveur de vilebrequin à gradient discret borné.
3. Avancer l'échange gazeux et la chaleur de paroi d'un demi-tick, à la géométrie de vilebrequin résultante.
4. Appliquer la chaleur gaz-paroi accumulée et les pertes électromécaniques à la résolution thermique.

Pendant l'étape 2, la masse est fixe et `U_new = U_old (V_old/V_new)^(gamma-1)`. La pression moyenne du gaz
et le couple viennent de la différence divisée de cette même variation d'énergie.
Le vilebrequin gagne le travail du gaz moins le travail de contre-pression ; la chambre perd exactement le
travail du gaz correspondant, à la précision en virgule flottante. `log1p`/`expm1` et la différence divisée
analytique du volume évitent de soustraire des états presque égaux autour des petits pas et des
points morts. Plusieurs cylindres peuvent partager un vilebrequin ou agir par des arbres couplés.

La décomposition a une convergence du second ordre pour le cas d'écoulement sonique lisse testé, sans
transfert de paroi. Les températures de paroi restent fixes pendant les deux demi-pas gazeux, puis vient
la résolution thermique existante : la précision avec couplage de paroi reste du premier ordre. Le limiteur
d'écoulement proche de l'équilibre peut aussi changer l'ordre local. La conservation n'établit pas la précision.

## Bornes, échec et compatibilité

Le parcours du vilebrequin est limité à 0.25 rad par tick ; la résolution non linéaire utilise au plus 16
itérations et 10 essais de recherche linéaire. Chaque demi-pas gazeux conserve sa borne de 4096 sous-pas,
sa variation relative cible de 2 % et son rejet de variation corrigée à 25 %. Les états, sorties ou épuisements
de solveur invalides ou non finis rejettent le lot entier de l'appelant, y compris tous les ticks
et entrées planifiées antérieurs. Réduisez `step_ns` et inspectez l'aire d'écoulement, l'état gazeux, la conductance,
la vitesse du vilebrequin et l'inertie avant de réessayer. L'annulation et les dérivations conservent tout l'état gazeux et de bilan ;
l'avancement réussi et les instantanés dans le tampon de l'appelant n'allouent pas de mémoire managée.

L'asset v4 ajoute un enregistrement de géométrie indexé et borné pour chaque cylindre gazeux, en préservant tous
les lecteurs v1/v2/v3. Il ne sérialise pas les espaces de travail du solveur. Une
fixture v3 authentique à volume fixe vérifie que l'introduction de la géométrie mobile ne change pas les empreintes gazeuses
antérieures ni le rejeu. Voir le [format d'asset](ASSET_FORMAT.fr.md) et la [provenance des fixtures](../tests/Power.Tests/Fixtures/README.md).

## Expérience et preuves

Le [laboratoire du cylindre mobile](../assets/labs/moving-cylinder.power.json) entraîne un
cylindre avec deux restrictions de réservoir et une paroi thermique finie. Les huit événements
d'ouverture temporels exercent l'écoulement entrant et sortant de la chambre et comprennent des ticks entre les bornes
de rapport et de présentation. C'est une expérience de moteur entraîné non calibrée ; la planification
n'est ni un ECU, ni un profil de came, ni un contrôleur de moteur à quatre temps, ni un modèle de combustion.

Les tests comparent une chambre fermée à l'implémentation existante du cylindre fermé, en
rotation avant et arrière et aux points morts, et comparent une chambre ouverte à une
intégration RK4 écrite indépendamment des EDO gouvernantes. Cette dernière écrit la géométrie,
le débit massique sonique et le travail de pression directement depuis les équations. Les contrôles de raffinement de pas
séparent la précision de l'écoulement lisse et celle du couplage de paroi. D'autres contrôles couvrent plusieurs
vilebrequins couplés ou partagés, des cylindres fermés et ouverts mixtes, la conservation, une propriété mal formée,
la normalisation des unités, l'échec et la reprise atomiques, les dérivations, l'annulation, zéro allocation,
la compatibilité portable et toutes les bornes de rapport JSON, MCP et d'asset.

Unity inclut une vue de piston mobile, des connexions gazeuses et des tests d'import et de Play. Les preuves
réelles d'Editor, de rendu, de Play Mode et d'IL2CPP sont encore en attente. Voir le
[registre de validation](VALIDATION.fr.md) pour les contrôles exécutés et la [feuille de route](ROADMAP.fr.md)
pour les travaux restants de moteur, de transmission, de contrôle et de calibration.
