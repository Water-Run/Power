# Calage des soupapes sur l'angle vilebrequin

[English](VALVE_TIMING.md) · [简体中文](VALVE_TIMING.zh-CN.md) · **Français** · [Русский](VALVE_TIMING.ru.md) · [日本語](VALVE_TIMING.ja.md) · [한국어](VALVE_TIMING.ko.md) · [Deutsch](VALVE_TIMING.de.md) · [Español](VALVE_TIMING.es.md) · [Italiano](VALVE_TIMING.it.md) · [Português](VALVE_TIMING.pt-BR.md)

Un `valve_timing` optionnel sur un `gas_orifice` multiplie son ouverture par une enveloppe périodique
d'angle vilebrequin. Il prend en charge les enceintes gazeuses fixes et les cylindres mobiles par les
mêmes définitions du cœur, du JSON, de la CLI, du MCP et d'asset portable. L'enveloppe suit la position
réelle du vilebrequin pendant l'accélération, l'arrêt et l'inversion. Elle représente l'aire
d'écoulement effective ; elle ne modélise ni le contact de came, ni la levée physique de soupape, ni les efforts de ressort, ni le frottement.

## Contrat et phase

```json
"valve_timing": {
  "crank_node": 1,
  "cycle_angle": { "value": 720, "unit": "deg" },
  "open_angle": { "value": 520, "unit": "deg" },
  "duration_angle": { "value": 200, "unit": "deg" }
}
```

`crank_node` doit identifier un nœud en rotation. Les trois angles exigent des unités explicites `deg`
ou `rad`. L'angle de cycle est exactement 360 ou 720 degrés ; la durée est comprise entre 1e-6
radian et l'angle de cycle. L'angle d'ouverture est fini et normalisé modulo le cycle.
Les angles négatifs et un lobe qui franchit la frontière de cycle sont pris en charge. Plusieurs soupapes
peuvent référencer un même vilebrequin, y compris des lobes qui se chevauchent.

La phase est relative à l'angle du vilebrequin référencé, position initiale comprise.
La phase géométrique d'un cylindre n'est **pas** ajoutée automatiquement : l'auteur doit choisir
l'angle d'ouverture de soupape approprié pour chaque cylindre. Un cycle de 720 degrés distingue
les révolutions successives du vilebrequin. Aucune phase quatre temps implicite n'est déduite de
la position du piston, de la vitesse ou du temps écoulé.

Pour un cycle `C`, un angle d'ouverture `a`, une durée `D`, une ouverture de crête `u` et un angle de vilebrequin `theta` :

```text
s = modulo(theta - a, C)       // in [0, C)
opening = u sin²(pi s / D)     // 0 < s < D
opening = 0                   // otherwise, including both boundaries
A_effective = A_orifice * opening
```

Ce profil et sa dérivée première sont continus aux frontières du lobe. La rotation inverse
le retrace ; un vilebrequin arrêté maintient son ouverture courante et peut continuer à s'écouler.
L'ouverture ne choisit pas le sens de l'écoulement : la loi d'orifice bidirectionnelle existante,
entraînée par la pression, s'applique toujours. Son coefficient de débit reste un multiplicateur distinct.

Pour une restriction calée, `initial_input` et son canal d'entrée optionnel spécifient l'**ouverture
de crête**, une fraction dans [0, 1]. La quantité de canal est `peak_opening` ; zéro désactive
le lobe. La quantité de sortie `effective_opening` utilise `Field.Opening` (champ de KPI JSON
`opening`) et rapporte la fraction réelle. `mass_flow` est évalué avec cette fraction.
Les restrictions non calées conservent leur quantité d'entrée et leur sémantique existantes. Les changements de crête
planifiés et interactifs conservent la validation atomique des entrées et les contrôles de révision.

## Intégration et reprise

Les modèles calés utilisent une intégration en demi-pas gazeux / pas complet de travail de vilebrequin / demi-pas
gazeux, y compris une enceinte fixe entraînée par un vilebrequin indépendant. Le premier demi-pas
utilise l'angle de vilebrequin du début, et le second l'angle résultant. Le solveur gazeux
résout sa propre dynamique de masse et d'énergie dans chaque demi-pas. Il ne localise pas en continu
les fronts de soupape et n'adapte pas le tick mécanique extérieur.

Pour chaque lobe actif, le tick extérieur doit satisfaire :

```text
limit = min(0.25 rad, D / 8)
max(abs(theta_next - theta_old), dt * max(abs(omega_old), abs(omega_next))) <= limit
8 * binary64_epsilon * max(abs(theta_old), abs(theta_next)) <= limit
```

La borne de vitesse aux extrémités couvre aussi une inversion dont la variation nette d'angle est faible.
La borne de précision empêche un angle déroulé de perdre la résolution nécessaire à
son lobe. Un tick sous-résolu échoue même lorsque les deux extrémités sont fermées ; il ne peut pas
sauter en silence une ouverture étroite entière. Une crête désactivée n'exige pas la résolution du lobe.
Ce sont des gardes numériques, pas une tolérance d'erreur ni une garantie pour une dynamique arbitraire.

Un échec renvoie `NumericalFailure` / `numerical_failure` et ne valide aucune partie du
lot de l'appelant, y compris les ticks antérieurs et les entrées planifiées. Réduisez `step_ns` et recréez
le modèle ou la session ; assurez-vous que les événements restent alignés sur le nouveau tick. Pour de très grands angles initiaux,
choisissez un angle équivalent cohérent avec la phase de chaque composant connecté.
Les limites existantes de cylindre et de gaz s'appliquent aussi. Aucun état de came mutable caché n'est ajouté ;
la position du vilebrequin et les entrées de crête participent déjà aux instantanés, aux hachages et aux dérivations.

Les cas de référence lisses et sans paroi montrent une convergence du second ordre. Les températures de paroi
restent fixes sur le tick extérieur, donc les modèles couplés à la paroi restent du premier ordre. La
limitation d'écoulement proche de l'équilibre existante peut réduire l'ordre local. La conservation et le rejeu ne
démontrent pas à eux seuls la précision temporelle.

## Preuves et compatibilité

Les contrôles comprennent les valeurs d'enveloppe analytiques, les cycles explicites, le repliement de phase, l'accélération,
l'inversion, un vilebrequin immobile, les crêtes désactivées, les franchissements de lobe entier non résolus, l'annulation,
le rollback du lot entier, les dérivations indépendantes et l'avancement et les instantanés sans allocation.

Un test de vidange d'enceinte fixe intègre indépendamment l'exposition en sinus carré et utilise
la solution fermée de décharge adiabatique sonique. Les cas avant et arrière
convergent sous le raffinement de tick. Un test distinct de cylindre mobile intègre la masse, l'énergie
interne, le mouvement du vilebrequin et une restriction dépendante de l'angle avec des EDO RK4
écrites indépendamment, en franchissant les deux frontières de lobe. Le raffinement de référence établit sa propre précision
avant de comparer les résultats du cœur. Voir [la validation](VALIDATION.fr.md) pour les seuils.

Seuls les modèles calés ajoutent l'balise d'empreinte 6, les identifiants de composant et de vilebrequin cibles et les
paramètres de profil normalisés. Les modèles non calés conservent leurs empreintes et leur avancement antérieurs. L'asset v5
ajoute des enregistrements de calage bornés et conserve les lecteurs v1–v4 ; des fixtures antérieures authentiques contrôlent
les empreintes et le rejeu mis à niveau. Voir [la disposition d'asset](ASSET_FORMAT.fr.md).

Le [laboratoire du cylindre calé sur le vilebrequin](../assets/labs/crank-timed-cylinder.power.json)
entraîne une chambre synthétique sur des cycles répétés de 720 degrés, avec des profils d'admission et d'échappement.
Deux changements de couple planifiés font varier la vitesse du vilebrequin ; le calage des soupapes lui-même n'a pas
de planification temporelle. Le JSON/la CLI, le MCP et la lecture d'asset concordent aux 63 bornes de rapport.
La construction exporte `CrankTimedCylinder.powerasset` ; Studio anime des repères schématiques
depuis les canaux d'ouverture effective. L'exécution Editor/Play/IL2CPP reste en attente.

L'[exemple de réacteur à combustion interne de Cantera](https://cantera.org/stable/examples/python/reactors/ic_engine.html)
est une référence conceptuelle pour la commande d'orifice par angle vilebrequin. Ses hypothèses de vitesse fixe,
sa loi de soupape et ses paramètres d'exemple ne sont pas adoptés comme calibration ni comme vérification du
solveur de Power!. Cette implémentation utilise le couplage conservatif du vilebrequin du projet
et la loi de tuyère bidirectionnelle. La [combustion prémélangée](PREMIXED_COMBUSTION.fr.md) distincte
ajoute maintenant le bilan de carburant et d'énergie chimique. Tous les paramètres d'exemple restent `unverified` ;
le comportement moteur complet et la calibration véhicule mesurée restent ouverts.
