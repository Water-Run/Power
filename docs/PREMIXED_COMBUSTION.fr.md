# Combustion prémélangée et bilan d'énergie du carburant

[English](PREMIXED_COMBUSTION.md) · [简体中文](PREMIXED_COMBUSTION.zh-CN.md) · **Français** · [Русский](PREMIXED_COMBUSTION.ru.md) · [日本語](PREMIXED_COMBUSTION.ja.md) · [한국어](PREMIXED_COMBUSTION.ko.md) · [Deutsch](PREMIXED_COMBUSTION.de.md) · [Español](PREMIXED_COMBUSTION.es.md) · [Italiano](PREMIXED_COMBUSTION.it.md) · [Português](PREMIXED_COMBUSTION.pt-BR.md)

`premixed_combustion` couple un profil de combustion de Wiebe prescrit à un vilebrequin et à une chambre
à gaz finie. Le carburant, l'air frais et les produits inertes sont transportés dans le réseau gazeux ;
la réaction consomme le réactif limitant disponible et convertit l'énergie chimique stockée
en énergie thermique du gaz. Le travail de pression entraîne le même solveur de vilebrequin que les
cylindres mobiles. Le cœur, le JSON, la CLI, le MCP et l'asset v6 partagent ces définitions.

C'est un modèle prémélangé global, à propriétés constantes. Chaque constituant d'un réseau
connecté partage un seul R et un seul gamma. Les trois classes de masse ne représentent pas des espèces détaillées,
des capacités thermiques variables, une cinétique de réaction, une propagation de flamme, l'auto-allumage, le cliquetis,
les émissions, l'évaporation du carburant ou l'injection. L'exemple allumé d'origine utilise une admission gazeuse déjà mélangée.
Le [dosage carburant par cycle](FUEL_METERING.fr.md) prend en charge une rampe gazeuse finie
distincte et l'admission d'air ; la pulvérisation et l'évaporation liquides restent hors du modèle. Une combustion prescrite et des tests de conservation réussis n'établissent pas des performances moteur mesurées
et n'achèvent pas l'objectif de groupe motopropulseur complet.

## Composition et orifices

Un nœud gazeux ajoute en option `premixed` à son objet `gas` existant :

```json
"gas": {
  "gas_constant": { "value": 287, "unit": "j_kg_k" },
  "gamma": 1.35,
  "premixed": {
    "lower_heating_value": { "value": 44000000, "unit": "j_kg" },
    "stoichiometric_air_fuel_ratio": 14.7,
    "initial_fractions": { "fuel": 0.04, "fresh_air": 0.96 }
  }
}
```

Le pouvoir calorifique et le rapport massique air/carburant stœchiométrique doivent être positifs et finis.
Les fractions de carburant et d'air frais doivent être non négatives, et leur somme au plus égale à un. Le reste
est constitué de produits inertes. L'air frais représente le comburant avec son diluant ; consommer
`r` kg d'air frais avec 1 kg de carburant crée `1+r` kg de produits. L'excès d'air frais ou de carburant
reste disponible ; les produits ne peuvent pas réagir à nouveau.

Chaque restriction de réservoir sur un nœud prémélangé doit spécifier des
`reservoir_fractions` explicites, avec les mêmes deux champs. Les fractions sont interdites sur les autres
composants ou sur les restrictions internes. À l'entrée, la frontière fournit cette composition ;
à la sortie, elle retire la composition réelle du volume fini. Les volumes gazeux finis connectés
doivent partager le suivi, R, gamma, le LHV et le rapport stœchiométrique. Les connexions incompatibles ou
non suivies sont rejetées ; les inventaires chimiques ne peuvent pas disparaître à un orifice.

Le solveur gazeux transfère chaque constituant avec le même flux massique signé et les mêmes fractions
amont que le gaz total. Il fait évoluer des masses de constituants non négatives et reconstruit la masse
totale à partir de leur somme. Un pas prémélangé est aussi borné par le débit sortant total, même lorsque
les débits massiques totaux entrant et sortant s'annulent presque. Aucun inventaire chimique n'est créé
par l'égalisation de pression ni par un retour de réservoir.

Un gaz prémélangé ajoute trois valeurs de constituants stockées au budget d'état déclaré. Un composant de
combustion ajoute une frontière d'angle irréversible ; le tout reste dans la borne existante de 64 états.
Les bilans de frontière et de réaction compensés participent au rollback, au hachage et aux dérivations.

## Loi de combustion et historique du vilebrequin

Le composant relie `node_a` (vilebrequin) à `node_b` (gaz prémélangé). Une chambre mobile doit
utiliser son propre vilebrequin de géométrie, et chaque chambre n'autorise au plus qu'un composant de combustion. Une
enceinte fixe peut utiliser un vilebrequin indépendant pour des expériences analytiques.

```json
{
  "id": 15,
  "kind": "premixed_combustion",
  "node_a": 1,
  "node_b": 2,
  "input_channel": 103,
  "initial_input": { "value": 1, "unit": "fraction" },
  "parameters": {
    "cycle_angle": { "value": 720, "unit": "deg" },
    "start_angle": { "value": 340, "unit": "deg" },
    "duration_angle": { "value": 80, "unit": "deg" },
    "shape_exponent": 3,
    "burn_coefficient": 6.9
  }
}
```

Le cycle est explicitement de 360 ou 720 degrés. L'angle de début est relatif au vilebrequin réel,
et n'est pas décalé implicitement par la phase géométrique du cylindre. La durée est dans [1e-6 rad, angle de cycle] ;
l'exposant de forme `n` est dans [1,16], et le coefficient `a` dans (0,50]. Le début est normalisé modulo
le cycle. Tous les angles exigent des unités. Pour un avancement avant `z` depuis le début de combustion, tronqué
à [0,1], le hasard intégré est `H(z) = a z^n`. Chaque cycle complet contribue `a`.

Sur les angles avant nouvellement parcourus :

```text
hazard = burn_multiplier * delta(H)
limiting_fuel = min(fuel_mass, fresh_air_mass / stoichiometric_air_fuel_ratio)
burned_fuel = limiting_fuel * (1 - exp(-hazard))
consumed_air = stoichiometric_air_fuel_ratio * burned_fuel
created_products = burned_fuel + consumed_air
released_heat = LHV * burned_fuel
```

Pour une charge fermée et un multiplicateur de 1, la fraction brûlée est `1-exp(-a z^n)` de sa
quantité initiale de carburant limitant. Elle n'est **pas** forcée à un à la frontière de durée :
`exp(-a)` reste imbrûlé après une fenêtre de combustion complète. Les faibles expositions utilisent `expm1` pour
éviter l'annulation. La charge fraîche qui entre pendant une fenêtre active rejoint les réactifs
bien mélangés ; il n'y a pas de source de chaleur cachée et illimitée par cycle.

Le canal d'entrée optionnel est `burn_multiplier`, une fraction dans [0,1] qui met le hasard à l'échelle.
Zéro désactive la réaction ; il n'empêche pas le carburant d'entrer par une admission ouverte. Cette entrée
n'est ni une commande d'injecteur ni un contrôleur d'allumage prédictif.

Chaque composant stocke le plus grand angle de vilebrequin atteint, initialisé à l'angle de
départ. La réaction n'a lieu qu'au-delà de cette frontière. L'arrêt, la rotation arrière ou le
retraçage d'angles déjà visités ne peuvent pas libérer de la chaleur à nouveau. Un parcours avant désactivé
déplace quand même la frontière, donc la réactivation ne libère pas la chaleur manquée. Un départ à l'intérieur
d'une fenêtre de combustion ne consomme que son exposition avant restante. Après une grande inversion,
la combustion reste supprimée jusqu'à ce que le vilebrequin dépasse son maximum antérieur ; l'allumage
moteur bidirectionnel et le réarmement piloté par le contrôleur restent des travaux de contrôle à venir.

## Énergie et couplage numérique

L'énergie interne du gaz reste thermique : `U = m cv T`. L'énergie chimique est séparément
`E_chemical = m_fuel LHV`. L'enthalpie totale du réservoir comprend à la fois `mdot cp T` et l'énergie
chimique transportée. La variation globale d'énergie stockée inclut l'inventaire chimique,
donc la combustion est une conversion interne, pas un travail de source externe supplémentaire :

```text
energy_residual = mechanical/electrical source work + reservoir total enthalpy
                  - rejected heat - change(total stored energy)
```

`net_fuel_energy_in` expose séparément la partie chimique du bilan de frontière. C'est
l'entrée nette, y compris le carburant imbrûlé qui quitte le modèle ; ce n'est ni la livraison brute de carburant ni
un indicateur de consommation de carburant en régime établi. `fuel_residual` et `fresh_air_residual` comparent
l'inventaire initial, le transfert net aux frontières, l'inventaire courant et la réaction cumulée.
`mass_residual` continue de couvrir la masse totale de gaz. La conversion des constituants conserve la masse.

Pour une chambre mobile, l'aperçu de chaleur dépend de l'angle de vilebrequin d'essai et participe
à la résolution non linéaire du vilebrequin. Avec la chaleur totale `Q` pendant le tick et
`r = (V_old/V_new)^(gamma-1)` :

```text
U_after = (U_before + Q/2) r + Q/2
adiabatic_work = (U_before + Q/2) (1-r)
crank_work = adiabatic_work - back_pressure * (V_new - V_old)
```

Le couple de pression discret utilise ce même travail, donc l'énergie du gaz, l'énergie chimique et
le travail du vilebrequin concordent. Le carburant n'est consommé qu'après la réussite de la résolution dans l'état candidat.
Le transport gazeux utilise toujours des demi-pas symétriques autour du travail et de la réaction du vilebrequin. La température de paroi
reste fixe sur le tick extérieur ; le couplage de paroi est du premier ordre.

Pour une combustion activée, le parcours angulaire et le parcours de vitesse aux extrémités par tick doivent rester dans
`min(0.25 rad, duration_angle/32)`, avec une garde correspondante de résolution angulaire binary64.
La chaleur libérée ne doit pas dépasser 25 % de l'énergie thermique avant combustion en un tick. Ce sont des bornes
sur le travail admis et la résolution, pas des garanties de précision. Elles s'appliquent en plus des limites de
sous-pas gazeux et d'itération du cylindre. Réduisez `step_ns` en cas de `numerical_failure`, alignez
les événements planifiés sur le nouveau tick et recréez le modèle ou la session. Les appels échoués ou annulés
ne valident aucun état, entrée, frontière, bilan chimique ni curseur de lecture.

## Sorties, compatibilité et preuves

Les nœuds prémélangés ajoutent les champs de KPI `fuel_mass`, `fresh_air_mass`, `product_mass` et `chemical_energy`.
Un composant de combustion ajoute les cumuls `fuel_burned` (kg) et `heat_released` (J).
Le champ de KPI `burn_frontier` expose son plus grand angle de vilebrequin visité (quantité de canal
`burn_frontier_angle`, rad), afin que la combustion supprimée après inversion puisse être inspectée.
Les canaux globaux ajoutent l'énergie chimique, l'entrée nette d'énergie de carburant, le résidu de carburant et le résidu
d'air frais. Les quantités de canal renvoyées par la découverte font foi ; p. ex. la masse de carburant d'un nœud
est nommée `unburned_fuel_mass`. Les sorties ordinaires d'énergie interne et de débit gazeux conservent
leurs sens thermiques et de débit signé.

Les modèles prémélangés ajoutent l'balise d'empreinte 7 et les paramètres normalisés de réaction et de composition.
Les empreintes et l'avancement antérieurs sans réaction restent inchangés. L'asset v6 ajoute les enregistrements de composition,
de fractions de réservoir et de combustion ; les fixtures authentiques v1–v5 conservent la compatibilité. Les
nouvelles fidélités sont `premixed_gas_transport` et `premixed_wiebe_combustion`.

Les tests couvrent la consommation analytique de carburant et d'air et la température en enceinte fermée, les réactifs
limitants, le transfert de réservoir avant et arrière, la conservation des constituants en réseau fermé,
la convergence d'EDO vilebrequin/gaz réactives indépendantes, la combustion arrêtée, inversée ou désactivée,
les contrats mal formés, le rollback de lot, l'annulation, les dérivations et zéro allocation d'avancement et d'instantané.
Le [laboratoire du cylindre allumé](../assets/labs/fired-cylinder.power.json)
entraîne une charge sur des phases répétées d'admission, de compression, de combustion, de détente et d'échappement, et
se rejoue à l'identique aux 63 bornes de rapport JSON, CLI, MCP et d'asset. Les preuves numériques
et le périmètre d'exécution réel sont consignés dans [VALIDATION.fr.md](VALIDATION.fr.md).

[Les équations de réacteur à gaz parfait de Cantera](https://www.cantera.org/stable/reference/reactors/ideal-gas-reactor.html)
fournissent le contexte masse, espèces et énergie du volume de contrôle. L'
[exemple de moteur à allumage commandé d'Ansys](https://chemkin.docs.pyansys.com/version/stable/examples/advanced/SI_engine_optimization.html)
utilise un calage de combustion explicite et des paramètres de Wiebe. Ces références motivent les contrats ;
leur chimie détaillée, leurs modèles à deux zones et leurs paramètres d'exemple ne sont ni copiés ni
présentés comme vérification de ce solveur à propriétés constantes. Il n'y a aucune dépendance d'exécution
sur l'un ou l'autre paquet. Tous les paramètres d'exemple restent `unverified`.
