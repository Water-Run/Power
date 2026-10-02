# Réseau de convertisseur de couple quasi stationnaire

[English](CONVERTER_NETWORK.md) · [简体中文](CONVERTER_NETWORK.zh-CN.md) · **Français** · [Русский](CONVERTER_NETWORK.ru.md) · [日本語](CONVERTER_NETWORK.ja.md) · [한국어](CONVERTER_NETWORK.ko.md) · [Deutsch](CONVERTER_NETWORK.de.md) · [Español](CONVERTER_NETWORK.es.md) · [Italiano](CONVERTER_NETWORK.it.md) · [Português](CONVERTER_NETWORK.pt-BR.md)

`torque_converter` participe à la même résolution que les arbres, les moteurs, les cylindres, les embrayages et les engrenages idéaux. La pompe et la turbine sont des nœuds de rotation distincts à inertie explicite. Le stator est un bâti immobile ; sa réaction est observable, mais il n'effectue aucun travail. La perte de fluide alimente un nœud thermique optionnel ou le registre de rejet thermique externe. Un `clutch` parallèle distinct fournit le verrouillage. Tous les paramètres du laboratoire sont synthétiques et `unverified`.

## Cartes et équations explicites

Quatre cartes sont requises : `pump_positive`, `pump_negative`, `turbine_positive` et `turbine_negative`. L'élément de plus grande vitesse absolue est le membre menant de référence ; la pompe l'emporte en cas d'égalité exacte. Le signe de cet élément sélectionne sa carte positive ou négative. C'est une convention mathématique d'élément de référence, y compris pendant la contre-rotation. Elle n'infère pas une caractéristique de marche arrière ou de retenue qui ne serait pas disponible.

Pour une vitesse du membre menant `wD` et une vitesse du membre mené `wF`, `s = wF/wD` est dans `[-1,1]`. Chaque carte contient 2 à 32 points explicites, avec un `speed_ratio` sans dimension, un `torque_ratio` R et un `capacity_coefficient` C en `nm_s2_rad2`. C multiplie la vitesse au carré ; ce n'est pas l'inverse d'un facteur K. R et C s'interpolent linéairement et ne sont jamais extrapolés. Deux vitesses nulles produisent des réactions nulles et le mode zéro.

```text
Tdriver   = -C(s) * wD * abs(wD)
Tfollower =  R(s) * C(s) * wD * abs(wD)
Tstator   = -(Tpump + Tturbine)
Qdot      = C(s) * abs(wD)^3 * (1 - s*R(s))
```

La compilation exige des nœuds strictement croissants couvrant `[-1,1]`, des C et R non négatifs, et `s*R(s) <= 1` sur chaque segment. Vérifier seulement les nœuds ne suffit pas : R linéaire en s rend le rendement quadratique ; tout maximum intérieur est aussi contrôlé. Les pentes doivent être finies. À `s=1`, C doit être nul et R égal à un, ce qui donne un couple de fluide nul à vitesses égales de même sens. À `s=-1`, les cartes pompe-positive/turbine-négative et pompe-négative/turbine-positive doivent donner des réactions physiques concordantes. Cela empêche un saut lorsque l'élément de référence change pendant la contre-rotation. La comparaison des extrémités n'autorise que la tolérance d'arrondi `64*epsilon*(abs(a)+abs(b))`. Les tableaux de cartes sont possédés et immuables ; les coefficients normalisés entrent dans l'empreinte du modèle.

Ces restrictions définissent le modèle passif actuel de Power! à cartes signées. Elles ne prétendent pas couvrir des courbes de convertisseur mesurées arbitraires. Les conventions générales d'entraînement et de retenue fondées sur des cartes sont documentées par [MathWorks Torque Converter](https://www.mathworks.com/help/sdl/ref/torqueconverter.html) et son [exemple à deux modes](https://de.mathworks.com/help/sdl/ug/torque-converter-two-mode.html). Les quatre cartes signées, la validation de l'interpolation et le solveur ci-dessous sont une conception de Power! ; aucun code source ni jeu de paramètres mesurés n'a été copié de ces références.

## Intégration couplée et limites

Les réactions du convertisseur utilisent les vitesses au point milieu de l'intervalle. Lorsque des cylindres sont présents, un système de Newton conjoint résout leur travail discret d'angle de vilebrequin et les deux vitesses de port du convertisseur. Chaque réponse à un couple unitaire inclut le système électromécanique et la projection des engrenages permanents. Les itérations de contrainte d'embrayage appellent cette même résolution non linéaire ; la subdivision de capture et d'inversion régénère les réponses d'intervalle. Aucun couple de convertisseur retardé n'est appliqué après l'intégration du cylindre ou de l'embrayage.

La résolution non linéaire a 24 itérations et 12 tentatives de recherche linéaire par dichotomie à chaque itération. La tolérance de résidu d'angle du cylindre est `2e-14 rad`. Un résidu de vitesse du convertisseur utilise `2e-12 + 64*epsilon*(abs(value)+abs(free_prediction)) rad/s`. Des jacobiens de carte analytiques par morceaux et des dérivées de travail de cylindre par différences finies construisent le système conjoint. Les limites existantes de course de cylindre de 0.25 rad, de résolution de soupape et de combustion, d'itération et d'événement d'embrayage, et de contrainte d'engrenage s'appliquent encore. Au plus huit convertisseurs sont pris en charge dans les budgets existants de 32 nœuds, 64 composants et 64 états. Chaque convertisseur ajoute quatre états logiques d'historique observable : deux couples moyens, la puissance thermique moyenne et la chaleur cumulée. La sommation de chaleur compensée participe aussi à la copie, au hachage et au rollback.

La chaleur d'intervalle acceptée est `-h*(Tp*wp_mid + Tt*wt_mid)`, de sorte que le travail mécanique retiré est la chaleur enregistrée. Une chaleur négative au-delà de la tolérance d'arrondi des vitesses résolues rejette l'intervalle ; seul un résidu négatif de l'ordre de l'arrondi est ramené à zéro. Les couples et la puissance thermique sont pondérés par la durée sur les intervalles internes acceptés, puis divisés par le tick complet. Les essais d'événement spéculatifs ne valident jamais leur chaleur ni leurs réactions. Tout l'état de convertisseur, d'embrayage, d'engrenage, de gaz, d'historique de combustion, d'entrée et de registre global subit un rollback si le lot échoue ou est annulé. Les forks possèdent leurs espaces de travail. Les tests exercent la capture sans allocation.

L'intégration au point milieu est du second ordre pour un mouvement de convertisseur autonome et lisse. Le modèle allumé couplé conserve un couplage explicite de température de paroi et un décollement d'embrayage en moyenne d'intervalle, donc il ne revendique pas un second ordre uniforme à travers toutes les transitions. Raffinez les ticks pour un échec numérique ou une étude de précision ; inspectez les pentes de carte, les échelles d'inertie et de vitesse et les contraintes d'embrayage avant de recréer une session. Une carte valide ne garantit pas que chaque pas de temps choisi soit résoluble.

## Contrat de modèle partagé et observable

Le JSON utilise les `node_a` (pompe) et `node_b` (turbine) requis, quatre tableaux sous `parameters`, et un `heat_node` optionnel. Il n'accepte ni canal d'entrée de convertisseur, ni port de porte-satellites, ni valeurs de carte implicites par défaut. `ComponentDefinition.TorqueConverter` expose le même modèle du cœur. Les erreurs de carte portent l'ID de composant et un champ exploitable tel que `converter.pump_positive` ou `converter.counter_rotation`.

| Champ | Signification | Unité |
|---|---|---|
| `torque` | Couple moyen de pompe du dernier tick complet | Nm |
| `torque_at_b` | Couple moyen de turbine du dernier tick complet | Nm |
| `torque_at_c` | Réaction moyenne du stator immobile du dernier tick complet | Nm |
| `heat_flow` | Puissance thermique moyenne du fluide du dernier tick complet | W |
| `fluid_heat` | Chaleur de fluide acceptée cumulée | J |
| `speed_ratio` | Rapport de vitesse signé courant mené/menant ; zéro à l'arrêt | fraction |
| `converter_drive` | 0 arrêté, 1 pompe positive, 2 pompe négative, 3 turbine positive, 4 turbine négative | code d'état |

Les couples et la puissance moyens commencent à zéro. Les mises à jour d'entrée aux bornes ne réécrivent pas les sorties moyennes du tick précédent. `torque_at_c` nomme ici la réaction du stator ; ce composant n'a pas de troisième rotor. Les modèles qui contiennent un convertisseur annoncent `quasisteady_converter_powertrain` et ajoutent la balise d'empreinte 10. Les empreintes et les trajectoires des modèles sans convertisseur restent inchangées. L'asset portable v10 conserve les quatre cartes ; les lecteurs v1–v9 et les scénarios authentiques demeurent.

## Laboratoire et preuves

Demandez l'exemple MCP `fired-converter`, ou exécutez :

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-converter.power.json --output artifacts/reports/fired-converter.json
```

Le laboratoire de 0.8 s commence avec une pompe à 600 rpm et une turbine à 300 rpm, alimentant le solaire d'un planétaire et une démultiplication finale 3:1. Le freinage de couronne sélectionne la réduction ; un embrayage solaire/couronne sélectionne la prise directe. Un verrouillage, un relâchement et une recapture planifiés indépendamment exercent les chemins de fluide et de frottement. Ce sont des événements prescrits, pas un contrôleur de transmission automatique.

À des ticks de 50,000 ns, les 87 bornes du rapport se rejouent exactement via la CLI, les assets portables et le MCP. L'empreinte du modèle est `839d03901973668d` et le hachage d'état final est `834a679376b7a6fd`. La vitesse finale pompe/turbine est d'environ 73.37748 rad/s, la vitesse de charge 6.988331 rad/s, la chaleur de fluide 24.27663 J et la chaleur de verrouillage 22.84709 J. L'embrayage de passage et le frein ajoutent 157.18199 J et 83.42289 J. Le nœud thermique partagé atteint 301.438643 K ; le résidu d'énergie total est d'environ `3.30e-11 J`. Ces nombres décrivent un transitoire synthétique.

Des tests indépendants couvrent la solution analytique à deux inerties `C(s)=k(1-s), R=1`, la décroissance analytique au calage, le raffinement du second ordre, le routage thermique et vers la chaleur externe, l'équilibre du stator, les états de marche arrière, de retenue et de contre-rotation, les ports de convertisseur partagés, la réflexion d'engrenage, le verrouillage, le rollback de lot complet, l'annulation, les forks et zéro allocation. Le raffinement du modèle allumé contrôle une distance combinée sans dimension sur la vitesse finale, la chaleur de fluide et la chaleur de verrouillage, par rapport à une exécution à 3,125 ns, avec cinq tailles de tick. Il borne aussi les différences absolues en dessous de 0.0002 rad/s ou J respectivement. Les erreurs de chaleur individuelles n'ont pas besoin de diminuer à chaque division par deux près des événements. Ce contrôle est distinct de l'ordre analytique autonome. La propriété et la validation des cartes signées, ainsi que les enregistrements portables mal formés re-signés, ont des contrôles dédiés.

Le [réseau hydraulique](HYDRAULIC_NETWORK.fr.md) fournit maintenant une capacité de verrouillage et de passage dérivée de la pression. Le moment cinétique du fluide, la dynamique de remplissage et de pression du convertisseur, la mécanique de stator tournant ou en roue libre, les propriétés dépendantes de la température, la dynamique de pompe, de régulateur et de piston, la topologie DCT/AT complète et la coordination ECU/TCU restent ouverts. Le comportement moteur complet, les mesures OEM et la calibration véhicule restent aussi ouverts. Studio a des ports de fluide schématiques et des tests préparés ; les preuves réelles Editor/Play/IL2CPP restent séparées et indisponibles dans cet environnement.
