# Références d'engrenage idéal et de train planétaire

[English](IDEAL_GEARS.md) · [简体中文](IDEAL_GEARS.zh-CN.md) · **Français** · [Русский](IDEAL_GEARS.ru.md) · [日本語](IDEAL_GEARS.ja.md) · [한국어](IDEAL_GEARS.ko.md) · [Deutsch](IDEAL_GEARS.de.md) · [Español](IDEAL_GEARS.es.md) · [Italiano](IDEAL_GEARS.it.md) · [Português](IDEAL_GEARS.pt-BR.md)

`Power.Core` fournit deux références à charge constante, immuables et sans allocation : `IdealGearPair` et `SimplePlanetaryGear`. Elles renvoient les vitesses des éléments, les avancées angulaires, les couples de réaction, le travail externe, la variation d'énergie cinétique et un résidu d'énergie. Elles fournissent des preuves analytiques indépendantes pour le solveur de transmission couplé. Le [solveur d'engrenages couplé](GEAR_NETWORK.fr.md) distinct expose maintenant des composants permanents d'engrenage et de planétaire via JSON, des assets portables et la CLI/MCP, y compris des expériences de passage commandées par embrayage. Les classes de référence restent des solutions analytiques locales pures.

## Périmètre physique et signes

Un engrenage idéal n'a pas d'inertie d'engrènement, de compliance, de jeu ni de pertes ; toutes les inerties fournies sont des inerties de rotor attachées. Les deux inerties d'une paire, ou les trois éléments d'un planétaire, doivent être positives et finies. Le bâti et les nœuds sans masse ne sont pas déduits d'une inertie nulle. L'abstraction suit le périmètre de [IdealGear](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealGear.html) et d'[IdealPlanetary](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealPlanetary.html) de la Modelica Standard Library. L'implémentation de Power! est écrite de façon indépendante ; aucune implémentation tierce n'est incluse ni appelée.

Pour une paire, le rapport signé `r` définit :

```text
omega_A = r omega_B
reaction_A = lambda
reaction_B = -r lambda
```

Les rapports positifs donnent le même sens de port ; les rapports négatifs l'inversent. Les réactions sont des couples **sur les rotors attachés**, pas les couples appliqués par les rotors à l'engrenage. Elles effectuent un travail net nul pour un mouvement compatible. Le carter d'une paire d'engrenages peut porter une réaction ; le moment cinétique ordinaire des deux rotors seuls n'est en général pas conservé. Le moment cinétique généralisé `r J_A omega_A + J_B omega_B` change avec le couple externe généralisé `r T_A + T_B`.

Pour le planétaire simple, le solaire, la couronne et le porte-satellites partagent un axe positif. Le rapport de dents `k = N_ring / N_sun` doit dépasser un. La relation cinématique et les deux degrés de liberté indépendants concordent avec les [équations d'engrenage planétaire MathWorks](https://www.mathworks.com/help/sdl/ref/planetarygear.html).

```text
omega_S + k omega_R = (1+k) omega_C
reaction_S = lambda
reaction_R = k lambda
reaction_C = -(1+k) lambda
```

Ces réactions somment à zéro et effectuent un travail net nul. Le modèle accepte un rapport continu, sans inférer les nombres de dents, le module, la résistance des dents ni une géométrie fabricable. L'inertie de rotation propre et d'orbite des satellites, les pertes, les paliers, la lubrification et le comportement thermique restent hors de cette référence.

## Solution indépendante en coordonnées réduites

Le mouvement de la paire utilise le port B comme coordonnée indépendante :

```text
J_equivalent_B = r^2 J_A + J_B
alpha_B = (r T_A + T_B) / J_equivalent_B
alpha_A = r alpha_B
```

Le planétaire élimine le mouvement du porte-satellites avant de former sa matrice de masse d'énergie cinétique. Avec `a = 1/(1+k)` et `b = k/(1+k)` :

```text
omega_C = a omega_S + b omega_R

M = [ J_S + a^2 J_C,    ab J_C        ]
    [ ab J_C,           J_R + b^2 J_C ]

M [alpha_S, alpha_R]^T = [T_S + a T_C, T_R + b T_C]^T
alpha_C = a alpha_S + b alpha_R
```

L'implémentation met à l'échelle cette matrice deux par deux et développe son déterminant en termes positifs pour éviter de soustraire des produits presque égaux. Pour des charges constantes, l'accélération est constante, donc la vitesse et le déplacement suivent une intégration temporelle linéaire/quadratique exacte, à l'arrondi en virgule flottante près. Les couples de réaction sont ensuite retrouvés à partir des équations des éléments. Les tests utilisent un multiplicateur de contrainte d'accélération indépendant pour le planétaire libre ; ils ne réutilisent pas la matrice réduite comme solution attendue.

## Contrat d'état, d'unités et d'échec

Les noms de propriétés publics portent les unités SI : kg m2, rad/s, rad, N m, J et secondes. Les rapports sont sans dimension. `Advance` prend une durée locale finie et positive et renvoie `GearStepStatus`. Cette durée de référence locale ne remplace pas l'horloge entière bornée de `Simulation`. Les classes ne portent pas d'état évolutif. Les entrées sont des enregistrements par valeur ; la sortie est la valeur par défaut à chaque rejet, et les instances peuvent être partagées par des appelants indépendants.

Les vitesses initiales doivent déjà satisfaire la relation. La compatibilité utilise un test d'arrondi relatif avec l'epsilon binary64 `2.2204460492503131e-16`, sans zone morte absolue à basse vitesse. Pour une paire, la borne est `64 epsilon (|omega_A| + |r omega_B|)`. Le planétaire inclut en plus les modules de ses deux termes de vitesse pondérés, afin que la compensation numérique soit traitée relativement aux opérations qui ont formé la vitesse du porte-satellites. Les termes sont mis à l'échelle avant l'addition pour éviter de faire déborder la tolérance.

Après validation, la vitesse dépendante et l'avancée angulaire sont reconstruites à partir des coordonnées indépendantes. Cela retire le résidu d'arrondi accepté ; ce n'est pas un calcul d'engagement à glissement fini ni de synchronisation. Des vitesses incompatibles renvoient `IncompatibleState`. Utilisez un modèle explicite d'embrayage ou d'impact pour un écart réel de vitesse, plutôt que d'en écarter l'énergie. La phase absolue de l'engrenage n'est pas spécifiée : seules les avancées angulaires sont rapportées.

Des paramètres de construction invalides lèvent des exceptions d'argument exploitables. Les combinaisons de paramètres non finies ou mal conditionnées sont rejetées ; le déterminant planétaire mis à l'échelle doit dépasser `64 epsilon`. Le rejet d'intervalle distingue une durée invalide, un état invalide, un état incompatible, un couple invalide et un échec numérique. Le dépassement arithmétique renvoie `NumericalFailure` ; des entrées finies seules ne garantissent pas des quantités dérivées représentables. Un contrôle d'équilibre des efforts à l'exécution rejette aussi une compensation numérique qui laisse des réactions d'éléments finies mais incohérentes : chaque résidu d'effort est borné par `512 epsilon` fois la somme des modules des couples d'inertie, appliqués et de réaction. L'extrémité acceptée contrôle aussi le bilan d'impulsion de chaque élément, avec `512 epsilon` fois les modules de l'ancien et du nouveau moment cinétique et des impulsions appliquées et de réaction. Ce dernier détecte une compensation excessive dans la reconstruction des vitesses dépendantes. Ces contrôles bornent les résidus, pas l'erreur de solution pour des paramètres arbitrairement mal conditionnés. Les tests incluent un échec de compensation numérique fini et une paire à rapport élevé dont la petite réaction doit rester observable. Le résidu est `external_work - kinetic_energy_change` ; aucune chaleur de frottement n'est fabriquée.

## États de transmission et preuves

Les tests fournissent explicitement des couples de maintien ou de verrouillage pour établir ces limites idéales :

| Condition imposée | Relation de vitesse résultante |
|---|---|
| Couronne maintenue | `omega_C = omega_S / (1+k)` |
| Solaire maintenu | `omega_C = k omega_R / (1+k)` |
| Porte-satellites maintenu | `omega_S = -k omega_R` |
| Solaire verrouillé à la couronne | Les trois vitesses d'éléments sont égales |

Le frein fourni effectue un travail nul lorsque son élément est maintenu ; un verrouillage solaire/couronne reçoit des couples opposés dont le travail combiné est nul. Ces contrôles établissent des états de transmission statiques. Cette référence n'implémente aucun passage, engagement d'embrayage, circuit hydraulique ni TCU, et des couples externes arbitraires ne maintiennent pas un élément automatiquement.

Les mêmes huit groupes de tests s'exécutent sur `net10.0` et `netstandard2.1` :

- Rapports signés et inertie ramenée ; puissance de réaction et bilan d'impulsion par élément.
- Mouvement planétaire libre contre une solution indépendante à multiplicateur d'effort.
- Trois cas d'élément maintenu et la prise directe, avec des charges explicites de maintien et de verrouillage.
- Invariance de la partition à charge constante et inversion de vitesse par zéro.
- Charge sinusoïdale contre des intégrales indépendantes pour les deux références ; la division de l'intervalle par deux donne une réduction d'environ quatre fois de l'erreur de vitesse et d'angle.
- Valeurs invalides d'inertie, de rapport, d'état et de charge, vitesses incompatibles, mauvais conditionnement et dépassement.
- 2,500 cas déterministes pour chaque référence, contrôlant le travail, la quantité de mouvement et la reproductibilité.
- 10,000 évaluations répétées de chaque primitive avec zéro allocation managée, plus un usage immuable partagé par des appelants concurrents indépendants.

Voir la [validation](VALIDATION.fr.md) pour le résultat complet de la vérification en série. Les tests d'assembly Standard s'exécutent sur .NET 10 et ne fournissent aucune preuve Unity Editor/Play/IL2CPP.

## Intégration couplée

Les contraintes permanentes participent maintenant à la résolution électromécanique/cylindre/embrayage, avec des espaces de travail de simulation indépendants, un rollback complet et des canaux stables de réaction et d'erreur. Le JSON et le schéma, l'asset v8 avec les lecteurs antérieurs, la découverte MCP et le rejeu utilisent la même topologie. L'expérience planétaire allumée effectue une montée réduction/prise directe et une descente. Voir [le contrat couplé](GEAR_NETWORK.fr.md) pour les équations et les preuves. La topologie DCT/AT complète, le convertisseur, l'hydraulique, les contrôles, le comportement moteur complet et la calibration véhicule mesurée restent dans l'objectif complet de Power!.
