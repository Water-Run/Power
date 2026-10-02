# Film de carburant liquide fini et évaporation

[English](FUEL_FILM.md) · [简体中文](FUEL_FILM.zh-CN.md) · **Français** · [Русский](FUEL_FILM.ru.md) · [日本語](FUEL_FILM.ja.md) · [한국어](FUEL_FILM.ko.md) · [Deutsch](FUEL_FILM.de.md) · [Español](FUEL_FILM.es.md) · [Italiano](FUEL_FILM.it.md) · [Português](FUEL_FILM.pt-BR.md)

`fuel_film` stocke un inventaire liquide initial explicite à côté d'un récepteur gazeux
suivi. Un nœud thermique fini fournit la chaleur sensible et la chaleur de changement de phase. Le carburant
évaporé rejoint la masse, l'énergie interne et le constituant carburant du récepteur ; la réaction
prémélangée existante ne consomme que la vapeur. L'inventaire liquide est un mouillage initial,
pas du carburant injecté, et reste dans le bilan de masse totale et d'énergie chimique.

C'est un modèle de recherche à propriétés constantes, avec un volume de déplacement liquide
négligeable et une température de saturation prescrite. Il n'implémente pas la dynamique de rampe liquide,
d'aiguille ou de pulvérisation, l'équilibre de phase dépendant de la pression, la condensation,
les propriétés de carburant multicomposant ni un comportement d'essence calibré.

## Énergie de phase et source de chaleur finie

Soit `c_l` la chaleur massique du liquide, `c_v` la capacité thermique isochore du gaz récepteur,
`T_s` la température de saturation déclarée et `L_u > 0` la différence d'énergie interne
massique vapeur moins liquide à `T_s`. La référence thermique partagée est :

```text
e_offset = (c_v - c_l) T_s - L_u
U_liquid = m_liquid (c_l T_liquid + e_offset)
U_vapor_added = delta_m c_v T_s
```

`L_u` est une différence d'énergie interne en J/kg, plutôt qu'une enthalpie de
vaporisation. Une enthalpie fournie exige une conversion explicite et justifiée avant
de pouvoir être utilisée ici. L'énergie thermique du liquide peut être négative sous cette référence ;
la température et la masse doivent rester physiquement admissibles. L'énergie chimique
`m_liquid * LHV` est distincte et se transfère avec la vapeur sans créer de chaleur de réaction
ni de travail de source externe.

Sous la saturation, la conductance `K` couple la capacité liquide `m_liquid c_l` à la capacité
finie de paroi `C_w`. La différence de température décroît analytiquement au taux
`K (1 / (m_liquid c_l) + 1 / C_w)`. La température moyenne pondérée par les capacités reste
constante. Si le liquide atteint `T_s`, la loi résout cet instant et utilise l'intervalle
restant pour l'évaporation.

À saturation avec `T_wall > T_s`, la surchauffe de paroi décroît au taux `K / C_w`.
La chaleur de phase disponible sur l'intervalle `h` est
`C_w (T_wall - T_s) (1 - exp(-K h / C_w))`, plafonnée par `m_liquid L_u`.
Le film reste à `T_s` jusqu'au sec ; la masse évaporée est la chaleur de phase divisée par
`L_u`. L'assèchement laisse une masse et une énergie liquides exactement nulles et arrête le prélèvement de chaleur.
Une paroi froide peut refroidir le liquide existant ; elle ne condense pas la vapeur du récepteur.

Chaque transfert satisfait `delta U_liquid + U_vapor_added = Q_from_wall`.
La paroi perd cette même chaleur, donc le changement de phase n'introduit pas de frontière
d'énergie externe. Les inventaires de carburant non négatifs et le bilan complet des constituants
sont contrôlés indépendamment du bilan d'énergie totale.

## Contrat de graphe et de document

| Donnée | Exigence |
|---|---|
| `node_a` | Récepteur gazeux fini, avec suivi de carburant prémélangé explicite et LHV |
| `node_b` | Paroi thermique finie, à capacité et température positives |
| `initial_mass` | kg non négatifs ; l'inventaire liquide initial complet |
| `initial_temperature` | K positifs, au plus égaux à la saturation |
| `liquid_specific_heat` | J/(kg K) positifs, unité JSON `j_kg_k` |
| `saturation_temperature` | K positifs, unité JSON `k` |
| `latent_internal_energy` | J/kg positifs, unité JSON `j_kg` |
| `conductance` | W/K non négatifs, unité JSON `w_k` |

Le JSON exige les six paramètres. Le film n'a pas de canal d'entrée, de calage vilebrequin ni de
puits de chaleur distinct. Les paramètres sans rapport, les domaines d'orifice incorrects, les unités, les valeurs
non finies et une capacité d'état non prise en charge sont rejetés. Les appelants du cœur utilisent
`ComponentDefinition.LiquidFilm` et `FuelFilmDefinition` ; la loi indépendante
`EquilibriumFuelFilm` expose la création d'état admissible et l'avance de bain fini.

Chaque film contribue cinq entrées d'état déclarées au budget d'état borné du compilateur.
La masse, l'énergie thermique, l'historique d'évaporation, le débit moyen, la chaleur de paroi et
leurs historiques compensés sont possédés par la simulation. L'annulation, les entrées rejetées,
les échecs tardifs du solveur, les dérivations indépendantes et les intervalles d'embrayage spéculatifs préservent
la transaction complète. L'avancement réussi et les lectures d'instantané n'allouent pas de
mémoire managée après l'échauffement.

## Précision d'intégration

Un intervalle accepté utilise des demi-pas film / gaz / mécanique et réaction / gaz / film.
Les films qui partagent une paroi s'exécutent dans un ordre de composants stable avant l'avance
gazeuse, et dans l'ordre inverse ensuite. Leur température de paroi finie est portée
entre les sous-pas de film, et la chaleur de paroi entre dans la même résolution thermique.

La loi de bain fini isolée est analytique sur le chauffage sensible, la saturation et
l'assèchement. L'intégration d'EDO simultanées indépendantes vérifie un raffinement lisse
du second ordre pour deux films partageant une paroi et pour de la vapeur transportée par une sortie
gazeuse sonique, sans autre source de chaleur de paroi. Les liaisons thermiques gazeuses
et les autres sources thermiques lisent encore la température de paroi explicite de l'intervalle extérieur,
donc ce couplage conserve une précision du premier ordre. Les fenêtres de réaction,
les événements de soupape et l'assèchement exigent leurs propres contrôles de raffinement ; le rejeu exact d'un lot
ne prouve pas à lui seul la précision du pas de temps, ni un second ordre uniforme pour un groupe motopropulseur allumé.

## Sémantique observable et portable

| Champ du film | Sens |
|---|---|
| `mass` | Carburant liquide restant, kg |
| `temperature` | Température du liquide ; température de saturation déclarée à sec |
| `internal_energy` | Énergie thermique liquide signée sous la référence de phase déclarée, J |
| `chemical_energy` | Énergie chimique du carburant liquide restant, J |
| `evaporated_fuel_mass` | Vapeur livrée cumulée, kg |
| `mass_flow` | Livraison moyenne de vapeur sur le dernier tick physique complet, kg/s |
| `film_wall_heat` | Chaleur cumulée prélevée sur la paroi, J ; le refroidissement peut la rendre négative |
| `heat_flow` | `K (T_wall - T_liquid)` instantané, W ; nul à sec |

Découvrez les identifiants et les unités de sortie par la validation ou la création de session. Les observables
globaux de masse, de carburant et d'énergie chimique incluent l'inventaire du film. La livraison interne
de vapeur n'incrémente ni l'énergie de carburant du réservoir ni l'enthalpie externe.

L'asset v19 stocke toutes les propriétés de phase et conserve les lecteurs v1-v18. Chaque film exige
un enregistrement de phase typé de 64 octets. Les longueurs et comptes bornés, le condensé, la couverture complète,
les enregistrements dupliqués ou manquants, les unités, la compilation physique et la protection contre la rétrogradation
sont contrôlés. La fixture authentique de dosage v17 conserve son empreinte et
son rejeu mis à niveau au même runtime. Voir [ASSET_FORMAT.fr.md](ASSET_FORMAT.fr.md).

## Laboratoire et travaux restants

`film-fired-cylinder` chauffe un film initialement mouillé, admet l'air séparément, puis
consomme la vapeur disponible par la combustion de Wiebe prescrite. La paroi chaude finie
paie la chaleur de phase ; le liquide ne brûle pas directement. Le JSON, la CLI, le rejeu portable et
le serveur MCP réel concordent à chaque borne de rapport. Les contrats de source, de schéma et de session
restent partagés ; les paramètres sont `unverified`.

Voir [VALIDATION.fr.md](VALIDATION.fr.md) pour les preuves numériques mesurées. Les repères de film Unity
préparés et les contrôles de cycle de vie exigent encore une vérification Editor/Play réelle.
L'[injecteur liquide](LIQUID_FUEL_INJECTION.fr.md) distinct réalimente maintenant les films depuis
une source finie et souple. La pompe et la réalimentation, l'aiguille et la pulvérisation, les propriétés de carburant mesurées,
l'allumage et l'ECU, le comportement complet d'admission et d'échappement, les contrôles de transmission et les
groupes motopropulseurs calibrés restent des exigences distinctes.
