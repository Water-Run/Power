# Simulation d'embrayages couplés

[English](CLUTCH_NETWORK.md) · [简体中文](CLUTCH_NETWORK.zh-CN.md) · **Français** · [Русский](CLUTCH_NETWORK.ru.md) · [日本語](CLUTCH_NETWORK.ja.md) · [한국어](CLUTCH_NETWORK.ko.md) · [Deutsch](CLUTCH_NETWORK.de.md) · [Español](CLUTCH_NETWORK.es.md) · [Italiano](CLUTCH_NETWORK.it.md) · [Português](CLUTCH_NETWORK.pt-BR.md)

Le composant `clutch` managé relie deux nœuds de rotation, ou un rotor au bâti. Il participe à la résolution électromécanique/cylindre existante et route la chaleur de frottement produite vers un nœud thermique ou le registre de rejet thermique externe. JSON, la CLI, le MCP, l'asset v10 et Studio utilisent les mêmes définitions. Cela implémente un élément de couplage de transmission ; la topologie DCT/AT complète, la dynamique pompe/piston et la coordination ECU/TCU restent un travail séparé. Le [réseau hydraulique](HYDRAULIC_NETWORK.fr.md) commande maintenant une variante d'embrayage actionnée par la pression. Le [convertisseur cartographié](CONVERTER_NETWORK.fr.md) partage désormais cette résolution et utilise un embrayage parallèle distinct pour le verrouillage.

Le [contrat physique de l'embrayage sec](CLUTCH_PHYSICS.fr.md) définit la loi de Coulomb et une référence exacte à deux inerties, indépendante, sous charges constantes. Le solveur de graphe ci-dessous étend cette loi aux réseaux couplés. Il ne fige pas le couple moteur ni le courant du moteur électrique en entrée unidirectionnelle vers l'embrayage.

## Définition et canaux

```json
{
  "id": 16,
  "kind": "clutch",
  "node_a": 1,
  "node_b": 4,
  "heat_node": 5,
  "input_channel": 104,
  "initial_input": { "value": 0, "unit": "fraction" },
  "parameters": {
    "static_capacity": { "value": 100, "unit": "nm" },
    "sliding_capacity": { "value": 30, "unit": "nm" },
    "ratio": 1
  }
}
```

`node_a` est un nœud de rotation. `node_b` est un nœud de rotation distinct, ou omis/zéro pour un frein au bâti. `ratio` est fini et non nul ; le bâti exige un. La capacité statique est au moins égale à la capacité glissante, et les deux sont des couples finis et non négatifs au port A. `initial_input` est une fraction d'engagement explicite dans `[0,1]`, qui met les deux capacités à l'échelle. Un canal d'entrée optionnel change l'engagement aux bornes exactes des ticks externes. Un `heat_node` omis/zéro envoie la chaleur au registre de rejet externe ; un puits fourni doit être un nœud thermique. La température ne modifie pas ces capacités.

La fabrique du cœur est `ComponentDefinition.Clutch(id, a, b, staticCapacity, slidingCapacity, channel, engagement, ratio, heat)`. Son descripteur `Friction` contient les deux quantités de couple explicites. Les modèles compilés copient leurs paramètres, trient par ID stable et n'ajoutent la balise d'empreinte 8 que lorsque des embrayages sont présents. Chaque phase d'embrayage compte dans la limite existante de 64 états. Les modèles antérieurs conservent leurs empreintes et leurs hachages de rejeu. Le nom de fidélité combiné est `hybrid_clutch_powertrain`, la calibration restant `unverified`.

| Champ | Unité | Signification |
|---|---|---|
| `slip_speed` | rad/s | `omega_A - ratio*omega_B` courant |
| `clutch_mode` | StateCode | Phase du dernier intervalle accepté : 0 débrayé, 1 verrouillé, 2 glissement positif, 3 glissement négatif |
| `torque` | Nm | Réaction moyenne en A sur le dernier tick externe complet |
| `heat_flow` | W | Puissance de frottement moyenne produite sur ce tick |
| `friction_heat` | J | Chaleur produite cumulée, quelle que soit la destination |

Le couple, la puissance et la chaleur initiaux sont nuls ; la phase initiale est inférée de l'engagement et de la vitesse relative, avant de résoudre une réaction de charge. Une phase décrit l'intervalle résolu, donc une arrivée exactement sur sa fin peut encore montrer la phase d'approche jusqu'à la résolution suivante. Une entrée à la borne change l'état d'entrée immédiatement et ne réécrit pas l'historique de sortie de l'intervalle précédent. Cela s'applique aussi aux événements d'entrée à la fin d'un appel `Step`. Les moyennes de couple et de puissance incluent chaque intervalle interne accepté.

## Intégration couplée et événements

Pour `g = omega_A - r*omega_B`, les couples aux ports sont `tau_A = tau`, `tau_B = -r*tau`. La puissance mécanique retirée est `-tau*g` ; cette convention de signe fonctionne avec l'un ou l'autre signe de `r`. Le débrayage impose un couple nul. Le glissement utilise la capacité cinétique qui s'oppose au glissement. Un embrayage verrouillé impose une vitesse relative nulle au point milieu, avec une réaction bornée par la capacité statique. Cela donne un travail verrouillé idéal nul, sans insérer un amortisseur artificiel ni un ressort de pénalité raide.

Chaque intervalle interne utilise les équations électromécaniques de point milieu implicite existantes et la résolution conservative du travail de pression du cylindre. Les réponses d'effort d'embrayage sont obtenues à partir des mêmes facteurs linéaires couplés. Une résolution de Gauss–Seidel projetée détermine les réactions statiques bornées pendant que le couple du cylindre est recalculé pour les efforts courants. Les contraintes statiques saturées se relâchent lorsque le mouvement requis dépasse la tolérance de vitesse. L'ensemble actif est reconsidéré si une autre contrainte change un sens de départ. Les boucles d'embrayage redondantes sont permises ; leurs réactions individuelles peuvent ne pas être uniques. L'ordre stable des composants sélectionne une répartition déterministe, tandis que les tests vérifient le mouvement résultant, les limites de capacité, la quantité de mouvement totale et l'énergie.

Si un intervalle glissant inverse sa vitesse relative, une dichotomie bornée localise la borne de glissement nul observée et rejoue l'intervalle depuis une copie d'état complète. L'intervalle suivant adhère, ou repart avec la réaction cinétique opposée. La dynamique, les facteurs thermiques et les réponses d'effort du cylindre sont recalculés pour chaque durée candidate ; tous les facteurs mutables appartiennent à la simulation individuelle. Le modèle compilé reste immuable.

La tolérance de contrainte est `2e-13 + 32*epsilon*(abs(omega_A)+abs(r*omega_B))` rad/s, avec `epsilon = 2.2204460492503131e-16`. La capture accepte les racines dans seize fois cette tolérance. Elle ne projette pas un glissement fini pour l'éliminer et n'écarte pas une énergie cinétique finie. Un travail de frottement négatif minuscule, dans deux fois la tolérance couple-fois-vitesse de l'intervalle, est ramené à zéro ; un travail négatif plus grand échoue. Les contrôles de conservation incluent cet effet d'arrondi. Un résidu dans la tolérance de racine ne peut pas créer un second événement parasite.

La résolution permet au plus 32 intervalles internes par tick externe, 56 itérations de racine, 256 itérations de contrainte par ensemble actif, et `2*clutch_count+2` tentatives d'ensemble actif. Des facteurs non finis, des contraintes non convergées, des événements non résolus, des budgets épuisés ou les limites existantes de cylindre/gaz renvoient `NumericalFailure`. L'annulation est vérifiée pendant le travail borné de contrainte et de racine. Réduisez le tick externe et inspectez les échelles d'inertie/rapport, les contraintes redondantes et les séquences de capacité ; n'interprétez pas un appel échoué comme un engagement partiellement achevé.

La chaleur produite est intégrée comme `-duration*tau*g_mid`, puis ajoutée à la résolution thermique ou au registre de chaleur externe. Tout le travail de source, le transport de gaz, l'historique chimique, l'échange de paroi et le rejet thermique des intervalles acceptés entrent dans la comptabilité d'énergie existante. La phase d'embrayage, les sorties moyennes, la chaleur cumulée et la somme de chaleur compensée sont copiées et hachées avec l'état physique. Un appel multi-ticks échoué ou annulé restaure l'état de départ complet, y compris les entrées planifiées, la phase et la chaleur. Les forks ne partagent que les données de modèle compilées. Le temps externe reste un compte entier borné de nanosecondes ; les durées d'événements internes n'introduisent pas de ticks fractionnaires visibles de l'extérieur.

L'essai de décollement non linéaire utilise la demande de couple moyenne sur l'intervalle. Il ne localise pas l'instant exact en temps continu auquel une charge statique variable dépasse d'abord la capacité. De même, l'encadrement d'événement concerne la trajectoire discrète de point milieu ; un grand tick peut manquer des oscillations physiques rapides dont les extrémités masquent une inversion. Raffinez le temps autour des transitions et comparez les sorties. La dynamique électromécanique lisse conserve la précision du point milieu, le couplage thermique/paroi reste du premier ordre, et aucune prétention universelle au second ordre n'est faite pour toutes les trajectoires à commutations.

## Laboratoire et preuves

Le [laboratoire d'embrayage allumé](../assets/labs/fired-clutch.power.json) relie le cylindre à combustion prémélangée à une charge inertielle distincte et à un nœud thermique d'embrayage. Six événements à ticks exacts appliquent un engagement partiel/complet, un couple de charge, un relâchement et un réengagement. Les paramètres sont synthétiques. Sur 0.6 seconde, le rapport Linux courant enregistre :

| Quantité | Résultat |
|---|---|
| Vitesse finale moteur/charge | 68.58488546 rad/s |
| Travail de source externe net, charge et contre-pression du cylindre comprises | -96.74607609 J |
| Chaleur d'embrayage produite | 191.55570747 J |
| Température finale du nœud thermique d'embrayage | 300.95777854 K |
| Chaleur de carburant libérée | 1,630.91064291 J |
| Glissement final | 2.84e-14 rad/s, phase verrouillée |
| Résidu d'énergie final | 1.79e-10 J |
| Empreinte de modèle / hachage d'état final | `197be44884deee90` / `28bf5335d8e35cde` |

Les 67 bornes du rapport coïncident entre tailles de lot alternées, rejeu portable et rejeu réel du serveur MCP enfant. Le rapport est `artifacts/reports/fired-clutch.json`. Demandez `get_example_model` avec `name: "fired-clutch"`, ou exécutez :

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-clutch.power.json --output artifacts/reports/fired-clutch.json
```

Les contrôles du cœur comparent l'engagement, le freinage et l'inversion à `ClutchPair`, y compris les rapports signés et les deux destinations de chaleur. Un moteur RL verrouillé correspond à un modèle dont l'inertie est combinée analytiquement ; un cylindre à gaz réactif verrouillé correspond de même à son modèle indépendant d'inertie équivalente, pression et consommation de carburant comprises. Un oscillateur ressort/frein correspond au mouvement sinusoïdal analytique par morceaux à travers trois inversions et un quatrième point de retournement qui capture, le raffinement réduisant l'erreur de plus de 3.7x à chaque division par deux. Des boucles à trois embrayages exercent les contraintes redondantes et l'engagement simultané. Les tests couvrent aussi le rollback complet après un préfixe réussi d'échauffement/capture, l'annulation, le rejeu planifié exact, la propriété immuable, l'indépendance des branches et le fonctionnement sans allocation, y compris des événements d'inversion interne répétés.

L'asset v10 fait l'aller-retour des capacités explicites et de tous les canaux. Les comptes mal formés, les enregistrements manquants, dupliqués et de mauvais type, les mauvaises unités, les limites invalides et les rétrogradations forgées sont rejetés. Un scénario authentique de cylindre allumé v6 conserve son condensé, son empreinte et son rejeu après mise à niveau. Les tests JSON stricts et agent distinguent une exécution réussie de KPI satisfaits. Voir [VALIDATION.fr.md](VALIDATION.fr.md).

La construction exporte `FiredClutch.powerasset`. Studio prépare deux disques d'embrayage schématiques, des couleurs de phase et une sortie de phase nommée, à côté des commandes d'engagement et des canaux de chaleur. Les tests d'import et de cycle de vie Play sont préparés. Les preuves réelles Unity Editor, de rendu, de Play Mode et IL2CPP restent en attente ; les contrôles d'assembly Standard hébergés par .NET ne les remplacent pas.

## Couplage d'engrenage permanent

Les [contraintes d'engrenage idéal et planétaire](GEAR_NETWORK.fr.md) projettent maintenant le point milieu libre et les réponses d'effort d'embrayage/cylindre dans le même espace de contraintes permanentes. Le laboratoire planétaire allumé combine un frein de couronne et un embrayage solaire/couronne avec un planétaire idéal et une démultiplication finale, en rejouant une montée et une descente. Un embrayage dont la vitesse relative est déjà contrainte de façon permanente est rejeté comme réaction indépendante indéfinie. Les autres contrats d'état, de capacité, thermique et d'événement de l'embrayage restent inchangés.
