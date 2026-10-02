# Fondement du cylindre fermé

[English](SEALED_CYLINDER.md) · [简体中文](SEALED_CYLINDER.zh-CN.md) · **Français** · [Русский](SEALED_CYLINDER.ru.md) · [日本語](SEALED_CYLINDER.ja.md) · [한국어](SEALED_CYLINDER.ko.md) · [Deutsch](SEALED_CYLINDER.de.md) · [Español](SEALED_CYLINDER.es.md) · [Italiano](SEALED_CYLINDER.it.md) · [Português](SEALED_CYLINDER.pt-BR.md)

`sealed_cylinder` couple un système bielle-manivelle rigide à un nœud en rotation. Le cylindre contient une masse fixe de gaz parfait, à rapport de chaleurs spécifiques constant et sans transfert thermique de paroi. C'est une référence de compression et de détente, pas un moteur allumé complet. L'admission, l'échappement, le carburant, la combustion, les fuites, le transfert thermique de paroi, l'inertie alternative et les événements de contrôle restent des travaux d'implémentation distincts. Tous les paramètres actuels sont synthétiques et `unverified`.

La pression et la température initiales s'appliquent à l'angle initial du rotor connecté, plus la phase du cylindre. Modifier cet angle initial change la masse piégée, sauf si la pression et la température sont ajustées de façon cohérente. L'état gazeux est dérivé de la position du vilebrequin et de l'entropie initiale immuable ; il ajoute des canaux observables, mais pas de variable d'état indépendante. Cette réduction n'est valable que pour le composant adiabatique fermé.

## Géométrie et état gazeux

Les longueurs se compilent en mètres, les pressions en pascals et la phase en radians. L'entrée accepte `m`/`mm`, `pa`/`bar` et `rad`/`deg`. La température est en kelvins ; la constante massique du gaz utilise `j_kg_k`. Le rapport de compression et gamma sont sans dimension. L'alésage et la course doivent être positifs, la longueur de bielle doit dépasser la demi-course, le rapport de compression et gamma doivent dépasser un, et la pression, la température et la constante du gaz initiales doivent être positives. La contre-pression peut être nulle.

Avec un rayon de manivelle `r = stroke/2`, une longueur de bielle `l`, une aire de piston `A = π bore²/4` et un angle `θ` mesuré depuis le point mort haut :

```text
x(θ) = r (1 - cos θ) + l - sqrt(l² - r² sin² θ)
Vc   = A stroke / (compression_ratio - 1)
V(θ) = Vc + A x(θ)
```

L'implémentation utilise une forme algébriquement équivalente pour éviter l'annulation près du point mort haut. Cette géométrie suit la [relation de volume du système bielle-manivelle centré de la Colorado State University](https://www.engr.colostate.edu/~allan/thermo/page2/page2.html).

Soient `V0`, `P0` et `T0` l'état initial. Les relations réversibles du gaz parfait sont :

```text
m = P0 V0 / (R T0)
P = P0 (V0/V)^gamma
T = T0 (V0/V)^(gamma-1)
U = P V / (gamma-1)
τ = (P - Pback) dV/dθ
```

Les relations pression/volume et de température suivent la [dérivation de la compression isentropique de la NASA](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/isentrophic-compression/). Pour un rapport de compression de 10 et un gamma de 1.4, la compression du point mort bas au point mort haut multiplie la pression par environ 25.119 et la température par environ 2.512. Ce sont des rapports idéalisés, pas des performances moteur mesurées.

## Intégration et énergie

La résolution électromécanique au point milieu existante fournit une solution de base et une réponse précalculée au couple, pour chaque vilebrequin de cylindre distinct. Une résolution non linéaire réduite détermine les incréments angulaires de ces vilebrequins. Les cylindres sur le même vilebrequin contribuent à une seule somme de couples ; les vilebrequins couplés se résolvent ensemble. Aucun appel à un fournisseur de modèle, objet Unity ou dépendance tierce ne participe à un tick physique.

Chaque cylindre utilise un couple discret cohérent avec le travail :

```text
τ_discrete Δθ = -(U_next - U_old) - Pback (V_next - V_old)
```

Des différences divisées analytiques stables du volume, et des évaluations de logarithme et d'exponentielle à petit argument, traitent les petits incréments et les passages aux points morts. La sortie instantanée `Torque` reste `(P-Pback) dV/dθ` ; elle est distincte du couple moyen utilisé pour intégrer un tick fini.

La variation globale d'énergie stockée inclut la variation d'énergie interne du gaz. Le travail de contre-pression est un travail de source externe, `-Pback ΔV`, donc le bilan reste `source_work - heat_rejected - stored_energy_change`. La dissipation d'arbre et de moteur entre toujours dans le réseau thermique ou dans la chaleur rejetée. Les variations d'énergie du gaz sont évaluées directement, pour éviter de soustraire de grandes énergies absolues lorsque gamma approche de un.

L'itération de Newton est limitée à 16 itérations, avec au plus 10 essais de recherche linéaire par itération. Le prédicteur linéaire et le parcours de vilebrequin accepté doivent rester dans 0.25 radian par tick. Les valeurs non finies, un parcours excessif ou un défaut de convergence renvoient `NumericalFailure` ; l'appel entier, y compris les entrées planifiées et les mises à jour de bilan, fait l'objet d'un rollback. Réduisez `step_ns` et recréez le modèle ou la session pour réessayer avec un tick fixe plus petit. L'acceptation n'est pas une garantie de précision du pas de temps. De très grands angles accumulés perdent aussi la résolution angulaire binary64 ; la précision sur longue durée exige ses propres preuves.

## Expérience observable et portable

Chaque cylindre expose la pression (Pa), la température du gaz (K), le volume (m³), la masse fixe (kg), l'énergie interne absolue (J), le déplacement du piston depuis le point mort haut (m) et le couple au vilebrequin (N·m). Les identifiants de canal conservent l'encodage objet/champ existant. Le modèle déclare la fidélité `sealed_adiabatic_gas` et la calibration `unverified`.

Exécutez `assets/labs/sealed-cylinder.power.json` par la CLI, ou demandez `get_example_model({"name":"sealed-cylinder"})` via le MCP. L'exemple utilise un tick de 100 µs, une durée de 0.2 s, deux changements de couple et 21 bornes de rapport. Les KPI déclarés s'appliquent à l'échantillon final, comme pour les expériences existantes ; les tests de conservation du cœur inspectent des bornes répétées tout au long de leurs exécutions.

Le même document s'exporte vers `SealedCylinder.powerasset`. Unity a une vue schématique du piston pilotée par le canal de déplacement ; une unité de scène représente une course complète. Les dimensions physiques et les sorties restent en SI. Les preuves réelles d'Editor, de Play Mode et d'IL2CPP sont encore en attente.

`EngineChecks` exécute des contrôles de géométrie analytique et de gaz parfait, des exécutions de conservation de deux secondes, un raffinement de pas du second ordre, la rotation inverse, des cas à petit pas et aux points morts, plusieurs cylindres sur des vilebrequins partagés et couplés, le couplage électrique/thermique, l'échec et la reprise atomiques, l'annulation, des branches indépendantes, la compatibilité d'asset et l'avancement sans allocation. Les mêmes contrôles s'exécutent sur les deux assemblys cibles, sur l'hôte .NET.
