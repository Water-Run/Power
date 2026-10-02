# Physique managée de l'embrayage sec

[English](CLUTCH_PHYSICS.md) · [简体中文](CLUTCH_PHYSICS.zh-CN.md) · **Français** · [Русский](CLUTCH_PHYSICS.ru.md) · [日本語](CLUTCH_PHYSICS.ja.md) · [한국어](CLUTCH_PHYSICS.ko.md) · [Deutsch](CLUTCH_PHYSICS.de.md) · [Español](CLUTCH_PHYSICS.es.md) · [Italiano](CLUTCH_PHYSICS.it.md) · [Português](CLUTCH_PHYSICS.pt-BR.md)

`Power.Core` fournit une loi de frottement `DryClutch` immuable et un intégrateur de référence `ClutchPair` pour deux inerties sous couples externes et engagement constants. Les deux compilent pour `net10.0` et `netstandard2.1` sans dépendances tierces.

Ces primitives fournissent une référence indépendante pour le [composant de graphe d'embrayage](CLUTCH_NETWORK.fr.md) désormais intégré. Le graphe couple arbres, moteurs et cylindres, prend en charge plusieurs embrayages et le routage thermique, et conserve le rollback de lot entier à travers les événements internes. JSON, CLI/MCP, l'asset v8 et Studio consomment cette définition de graphe. La paire autonome documentée ici reste une référence à charge constante ; elle n'avance pas elle-même un réseau compilé.

## Contrat de frottement

Toutes les capacités et les réactions sont exprimées au port A. Le rapport signé `r` utilise la même convention de puissance que le composant d'arbre existant :

```text
g       = omega_A - r * omega_B                  [rad/s]
tau_B   = -r * tau_A                            [Nm]
P_heat  = -tau_A * g                            [W]
C_s     = engagement * static_capacity          [Nm]
C_k     = engagement * sliding_capacity         [Nm]
```

La fraction d'engagement est dans `[0,1]`. La capacité statique est non négative et au moins égale à la capacité glissante. Les deux peuvent être nulles. Une capacité statique effective nulle débraye l'embrayage. Il n'y a pas de pression de serrage, de coefficient de frottement, de géométrie de disque, d'affaiblissement thermique, d'usure, de traînée ni de retard d'actionneur inférés.

Pour un glissement non nul, `tau_A = -sign(g) * C_k`. À glissement exactement nul, le système intégrateur doit fournir le couple requis pour maintenir l'accélération relative à zéro. Si son module est au plus `C_s`, l'embrayage se verrouille à cette réaction et ne produit pas de chaleur de frottement. Sinon, il commence à glisser dans le sens de la charge déséquilibrée, avec `C_k`. L'égalité à la limite statique reste verrouillée. La loi n'a pas de zone morte en vitesse et ne transforme pas silencieusement une petite vitesse relative en contrainte d'adhérence.

Cette distinction idéalisée entre frottement cinétique et réaction statique contrainte suit la mécanique décrite par les références principales : [Modelica Clutch](https://doc.modelica.org/om/Modelica.Mechanics.Rotational.Components.Clutch.html) et [MathWorks Fundamental Friction Clutch](https://www.mathworks.com/help/sdl/ref/fundamentalfrictionclutch.html). L'implémentation de Power! est écrite de façon indépendante et utilise des capacités de couple explicites ; elle ne reproduit aucune des deux implémentations et ne revendique pas leurs modèles constitutifs plus larges.

`ClutchMode` distingue `Disengaged`, `Locked`, `SlippingPositive` et `SlippingNegative`. Un mode à vitesse nulle peut être un état glissant en départ lorsque la charge externe dépasse la capacité statique. `HeatFlowWatts` est instantané ; sa valeur à un tel départ à vitesse nulle est nulle, même si la chaleur ultérieure est positive.

## Paire exacte à charge constante

Pour deux inerties positives `J_A`, `J_B`, et des couples externes constants `T_A`, `T_B` :

```text
J_A * domega_A/dt = T_A + tau_A
J_B * domega_B/dt = T_B - r * tau_A
D                 = 1/J_A + r*r/J_B
b                 = T_A/J_A - r*T_B/J_B
required_tau_A    = -b/D
dg/dt             = b + D*tau_A
```

Chaque phase de glissement a une accélération constante. Si sa vitesse relative atteint zéro dans l'intervalle demandé, le solveur avance exactement jusqu'à `t_zero = -g / (dg/dt)` et évalue la réaction statique. Il intègre ensuite le reste, soit verrouillé, soit en glissement dans le sens opposé. Une sollicitation constante n'autorise au plus qu'une telle arrivée, donc la résolution exige au plus deux phases, sans boucle de convergence ni subdivision temporelle. Un événement exactement à la fin de l'intervalle renvoie son mode de réaction à droite.

La trajectoire verrouillée obéit à `omega_A = r*omega_B` avec

```text
domega_B/dt = (r*T_A + T_B) / (r*r*J_A + J_B).
```

À une arrivée calculée, une projection qui conserve la quantité de mouvement retire le résidu d'arrondi d'événement binary64. Elle utilise des poids inertiels bornés plutôt que de former de grandes sommes pondérées par l'inertie. Une fois verrouillé, la contrainte de vitesse est construite explicitement. C'est une correction d'arrondi à un événement résolu, pas un engagement instantané inélastique d'un glissement fini. Les avancées angulaires intègrent chaque phase à accélération constante. Le travail externe est `T_A*delta_theta_A + T_B*delta_theta_B` ; la chaleur de frottement est l'intégrale de `-tau_A*g`. Le résultat comprend l'impulsion de couple signée en A et la variation d'énergie cinétique vérifiable indépendamment. Le résidu d'énergie est `external_work - heat - delta_kinetic`.

La paire accepte l'un ou l'autre signe d'un `r` fini et non nul. Son moment cinétique généralisé `r*J_A*omega_A + J_B*omega_B` ne change que par `r*T_A + T_B`. La conservation du moment cinétique ordinaire s'applique lorsque `r = 1` ; un rapport représente un transformateur mécanique idéal dont le support peut encaisser un couple. Pour un frein au bâti, construisez `ClutchPair.Brake(J, friction)`. Le port B a alors une vitesse et un couple externe fixés à zéro, et `r = 1`. L'infini n'est pas utilisé comme sentinelle d'inertie.

## API et comportement en échec

```csharp
var pair = new ClutchPair(0.2, 0.8, new DryClutch(20, 10));
var status = pair.Advance(new ClutchPairState(100, 0),
    externalTorqueANewtonMeters: 0,
    externalTorqueBNewtonMeters: 0,
    engagement: 1,
    durationSeconds: 4,
    out var step);
```

L'exemple atteint 20 rad/s aux deux ports après 1.6 s et produit 800 J de chaleur. Ses avancées angulaires sur quatre secondes sont 144 rad et 64 rad. Tous les nombres sont synthétiques, sans prétention de calibration véhicule.

Les deux classes sont immuables. `ClutchPairState` et `ClutchPairStep` sont des types valeur. `Advance` ne mute pas l'état de l'appelant et n'alloue pas de mémoire. Des appelants indépendants peuvent partager la même paire. Il n'y a pas d'historique de phase retenu ni d'horloge de simulation globale ; les vitesses fournies et les nouvelles charges constantes déterminent l'intervalle suivant.

| Statut | Signification et reprise |
|---|---|
| `Ok` | Un résultat local fini complet est disponible ; évaluez la conservation et l'adéquation du modèle séparément |
| `InvalidDuration` | Fournissez un intervalle fini et strictement positif, en secondes |
| `InvalidEngagement` | Fournissez une fraction finie dans `[0,1]` |
| `InvalidState` | Fournissez des vitesses finies ; un frein au bâti exige une vitesse B égale à zéro |
| `InvalidTorque` | Fournissez des couples externes finis ; un frein au bâti exige un couple B égal à zéro |
| `NumericalFailure` | Le mouvement dérivé, l'instant d'événement ou l'énergie dépasse la plage binary64 prise en charge ; inspectez les unités et les échelles, puis raccourcissez ou reformulez l'intervalle |

À tout rejet, la sortie est `default` ; il n'y a pas d'état partiellement publié. Des paramètres immuables invalides lèvent `ArgumentException` ou l'une de ses sous-classes à la construction. `DryClutch.Evaluate` rejette de même les entrées invalides ou une chaleur instantanée en dépassement. Un instant d'événement qui sous-passe à zéro échoue au lieu d'écarter silencieusement une énergie cinétique relative finie. Les résultats physiques restent soumis à l'arrondi en virgule flottante ; des entrées finies seules ne garantissent pas des quantités dérivées représentables.

`ZeroSlipTimeSeconds` est la première arrivée engagée à vitesse relative nulle, ou zéro lorsque l'intervalle y commence. Il est nul lorsqu'il n'y a pas une telle arrivée, y compris pour un mouvement débrayé. Il n'implique pas l'adhérence : une forte charge externe peut provoquer une inversion immédiate. `SlippingDurationSeconds` inclut les phases de glissement en départ ; le mouvement débrayé est exclu. `EndReaction` est instantané à l'état final, tandis que la chaleur, le travail, l'impulsion et les avancées angulaires sont intégrés sur l'intervalle complet.

Le paramètre local en secondes ne remplace pas l'horloge fixe en nanosecondes bornées de `Simulation`. L'intégration du graphe conserve les bornes exactes de ticks et d'événements externes, les hachages d'état, l'indépendance des forks, l'annulation et le rollback complet sur plusieurs ticks.

## Preuves et limites

[ClutchChecks.cs](../tests/Power.Tests/ClutchChecks.cs) exécute les mêmes dix groupes sur les deux assemblys cibles du cœur :

- Temps de synchronisation à deux inerties en forme close, vitesse, avancées angulaires, impulsion, quantité de mouvement et énergie cinétique perdue, avec engagement complet et partiel.
- Répartition exacte de la charge statique, seuil de décollement inclus, capacité cinétique plus basse, glissement non nul sans zone morte, et accrochage statique à capacité cinétique nulle.
- Inversion dans un intervalle et à sa fin, plus freinage au bâti, maintien et départ sous charge excessive.
- Rapports positifs et négatifs, moment cinétique généralisé et variations d'énergie calculées indépendamment. Deux mille combinaisons déterministes balayent l'inertie, le rapport, la vitesse, la charge externe, la capacité et la durée.
- Invariance par partition à travers des événements hybrides sous sollicitation constante. L'échantillonnage au point milieu de charges sinusoïdales variables converge vers des intégrales analytiques indépendantes de vitesse, d'angle et de chaleur. Cela établit le comportement du second ordre de cet exemple d'échantillonnage de charge ; le graphe a ses propres vérifications de convergence couplée et hybride.
- Entrées invalides, dépassement, événement insoluble, sortie par défaut en échec, évaluations répétées indépendantes et zéro allocation sur 10,000 intervalles réussis.

La paire renvoie la chaleur comme énergie produite ; le composant de graphe la route vers un nœud thermique ou le registre externe. Aucune des deux API n'implémente une topologie DCT/AT, une sélection de rapport, un convertisseur de couple, des actionneurs hydrauliques, une coordination ECU/TCU, une identification de matériau d'embrayage ou une calibration mesurée. Ces frontières restent dans la [feuille de route](ROADMAP.fr.md).
