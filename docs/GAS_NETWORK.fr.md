# Réseau gazeux compilé

[English](GAS_NETWORK.md) · [简体中文](GAS_NETWORK.zh-CN.md) · **Français** · [Русский](GAS_NETWORK.ru.md) · [日本語](GAS_NETWORK.ja.md) · [한국어](GAS_NETWORK.ko.md) · [Deutsch](GAS_NETWORK.de.md) · [Español](GAS_NETWORK.es.md) · [Italiano](GAS_NETWORK.it.md) · [Português](GAS_NETWORK.pt-BR.md)

Les réseaux gazeux finis passent maintenant par `CompiledModel` et `Simulation`. Le jalon
couvre les chambres à volume fixe, les réservoirs à pression et température fixes, les
orifices commandés et les liaisons thermiques de paroi. L'[extension du cylindre mobile](MOVING_CYLINDER.fr.md) relie maintenant l'échange gazeux au volume dépendant du vilebrequin et au travail de pression ; le solveur à volume fixe décrit ci-dessous conserve son comportement d'origine.

## API C# et unités

```csharp
var model = CompiledModel.Compile(new ModelDefinition
{
    StepNanoseconds = 100_000,
    Nodes = [NodeDefinition.GasVolume(1, 0.002, 2e6, 900)],
    Components = [ComponentDefinition.GasReservoir(10, 1, 2e-5, 1e5, 300,
        dischargeCoefficient: 0.9, channel: 100, opening: 0)]
});
var simulation = model.CreateSimulation();
simulation.SubmitInputs([new Scalar(100, 0.5)]);
var status = simulation.Step(100_000_000);
var values = new Scalar[model.OutputCount];
var snapshot = simulation.ReadSnapshot(values);
```

`GasVolume` prend le volume en m³, la pression en Pa, la température en K, et en option
R en J/(kg K) et gamma. Un nœud gazeux stocke le volume dans `Storage`, la température dans `Initial`,
la pression dans `Position` et la composition dans `Gas`. Les litres, les bars et les millimètres carrés sont
acceptés par des quantités explicites et normalisés avant le calcul d'empreinte.

`GasOrifice` relie deux identifiants de nœuds gazeux. `GasReservoir` relie un nœud à une frontière fixe ;
`NodeB == 0` identifie ce réservoir. `GasHeatLink` relie un nœud gazeux et un nœud thermique
avec une conductance en W/K. Les nœuds gazeux connectés doivent partager exactement le même R et le même gamma.
L'ouverture est une fraction sans dimension dans [0,1], validée pour les entrées initiales, directes et planifiées.
Un identifiant de canal d'entrée nul laisse l'ouverture initiale fixe. Les réseaux uniquement gazeux n'ont
pas besoin de rotor fictif. Les limites restent 32 nœuds, 64 composants et 64 états scalaires ; chaque volume
gazeux consomme deux états.

Chaque nœud gazeux expose la pression, la température, la masse et l'énergie interne. Les restrictions
exposent le débit massique signé de A vers B ; les liaisons thermiques exposent le flux de chaleur signé du gaz vers la paroi.
L'enthalpie de réservoir est positive vers l'intérieur. Le résidu d'énergie est
`source_work + reservoir_enthalpy - heat_rejected - stored_energy_change`.
Le résidu de masse est `sum(mass - initial_mass) - cumulative_reservoir_mass`.
Les résidus en virgule flottante sont évalués contre des échelles physiques, pas contre zéro exact.

## Méthode numérique et frontière

Le solveur gazeux utilise des sous-pas explicites avec un prédicteur/correcteur de Heun. Le taux relatif maximal
de masse et d'énergie au début du tick choisit un nombre uniforme de sous-pas, en visant 2 % de variation
par sous-pas. Plus de 4096 sous-pas, des états non physiques, des valeurs non finies ou une variation corrigée
de masse ou d'énergie supérieure à 25 % rejettent le lot entier. Réduisez `StepNanoseconds` et
recompilez, ou inspectez l'aire d'écoulement, le volume, la conductance et les conditions initiales.

La loi de tuyère a une dérivée singulière à pressions égales. Chaque évaluation limite l'énergie
transférée à la quantité d'égalisation de pression de la paire connectée, en mettant à l'échelle ensemble la masse et
l'enthalpie amont. Pour des volumes finis, cette énergie est
`abs(pA-pB) / ((gammaA-1)/VA + (gammaB-1)/VB)` ; un réservoir fixe omet le terme B.
Cela empêche les oscillations de franchissement de pression d'une paire isolée, tout en préservant les
bilans appariés. Le limiteur modifie l'intégration proche de l'équilibre ; la précision du second ordre n'est
affirmée que pour le cas de raffinement d'écoulement sonique lisse et non borné dans les tests.

La température de paroi reste à sa valeur de début pendant les sous-pas gazeux. La chaleur de paroi accumulée
entre ensuite dans la résolution thermique existante. Ce couplage est du premier ordre sur le tick extérieur ;
la stabilité ou la conservation à grand pas n'établissent pas à elles seules la précision. Le test de paroi
compare les températures à temps fini avec la solution analytique à deux capacités. Cette méthode
n'est pas le solveur implicite par paires proposé antérieurement, et elle ne valide pas cette proposition.

La masse, l'énergie, les sommes de réservoir et les corrections de bilan compensé appartiennent à l'état de
simulation et sont inclus dans la copie, le rollback, les dérivations et les hachages. L'avancement réussi et
les instantanés dans le tampon de l'appelant n'allouent pas de mémoire managée. Un lot planifié échoué restaure
tous les ticks et entrées antérieurs, y compris un échec après des ticks déjà réussis. Les mises à jour d'entrée
et les événements planifiés terminaux rejettent aussi les observables gazeux non finis.

Les modèles avec nœuds gazeux ajoutent l'balise d'empreinte de solveur 4. Les empreintes et hachages d'état
des modèles linéaires ou à cylindre existants conservent leur construction antérieure. Les paramètres d'exemple
restent `unverified`.

## Intégration JSON, agent et portable — 2026-09-22

Le [laboratoire de réseau gazeux](../assets/labs/gas-network.power.json) est l'exemple partagé
pour le JSON, la CLI, le MCP et le rejeu portable. Il contient deux chambres gazeuses, une
restriction interne commandée, une restriction de réservoir commandée et une liaison thermique de paroi. Ses événements
comprennent des ticks entre les bornes de rapport et de présentation ; chaque borne de rapport est comparée
au rejeu de l'asset décodé. Les paramètres restent synthétiques et `unverified`.

`power.model.v1` ajoute ces définitions explicites :

| Définition | Champs JSON et unités |
|---|---|
| Nœud gazeux | `domain: "gas"` ; `storage` : m3 ou l ; `initial` : k ; `position` : pa ou bar ; `gas` : gas_constant en j_kg_k et gamma > 1 |
| Orifice gazeux | `kind: "gas_orifice"` ; node_a/node_b ; `initial_input` : fraction dans [0, 1] ; paramètres : aire en m2 ou mm2 et discharge_coefficient |
| Orifice de réservoir | Orifice gazeux avec node_b absent ou nul ; exige aussi reservoir_pressure en pa ou bar et reservoir_temperature en k |
| Liaison de paroi gazeuse | `kind: "gas_heat_link"` ; node_a est gazeux, node_b est thermique ; paramètres : conductance en w_k |

Un `input_channel` absent ou nul maintient l'ouverture initiale explicite fixe. Les paramètres de réservoir
sont interdits pour une restriction à deux volumes. La composition n'est requise que sur les
nœuds gazeux. Les nouveaux champs de vérification sont `mass_flow`, `heat_flow`, `reservoir_enthalpy` et
`mass_residual` ; les champs de vérification d'état gazeux et d'énergie existants restent disponibles.

`CompiledModel.ValidateInput` contrôle les contraintes statiques de canal et de valeur sans muter
l'état. La validation d'expérience et la création d'asset portable l'utilisent pour toutes les ouvertures
planifiées, y compris les événements ultérieurs. La soumission et l'avancement à l'exécution effectuent encore des
contrôles d'observables supplémentaires dépendants de l'état, et conservent un rollback complet.

`power.asset.v3` et les versions ultérieures conservent la composition gazeuse, l'aire, le coefficient de débit et la pression de réservoir
dans des enregistrements d'extension indexés et bornés. La conductance de paroi, la température de réservoir,
les ouvertures initiales et les identifiants de canal d'entrée utilisent les champs de composant de base. Les lecteurs v1/v2
restent pris en charge pour leurs jeux de modèles d'origine et rejettent les définitions gazeuses. Des
fixtures authentiques d'avant le changement vérifient la rétrocompatibilité. Voir [le format d'asset](ASSET_FORMAT.fr.md).

Les capacités MCP en version 0.8.0 annoncent le domaine gazeux, les composants, la fidélité, les bornes
d'ouverture et les limites bornées du solveur. `get_example_model` accepte `gas-network`. La construction
exporte `GasNetwork.powerasset` ; Studio ajoute des enceintes schématiques, des repères de réservoir et
des chemins de restriction et de chaleur, avec les entrées et canaux de sortie existants. Ses nouveaux tests d'import et
de Play Mode exigent une exécution réelle de l'éditeur Unity et ne sont pas couverts par les preuves .NET.

## Validation et travaux moteur restants

Les neuf groupes de modèle compilé et les six groupes de primitives gazeuses continuent de s'exécuter sur
les deux cibles du cœur. Les tests portables couvrent en plus les modèles mixtes cylindre/gaz/thermique,
les quantités hors SI, une composition non par défaut, la corruption d'extension, les enregistrements
manquants ou dupliqués, la compatibilité v1/v2, les bornes planifiées, l'annulation et le rollback du curseur d'événements.
L'équivalence JSON/cœur et le rejeu MCP réel couvrent la frontière d'intégration.
Voir [la validation](VALIDATION.fr.md) pour les résultats de la vérification en série.

Les assemblys Standard s'exécutent sur .NET 10 pour ces contrôles ; ce n'est pas une preuve Unity Editor ni
IL2CPP. Les équations du solveur à volume fixe seul, les limites d'intégration et la construction d'empreinte
restent inchangées pour les modèles sans restrictions calées ni suivi prémélangé. Les modèles à chambres mobiles
ou à restrictions calées utilisent le couplage scindé versionné séparément, documenté dans
[MOVING_CYLINDER.fr.md](MOVING_CYLINDER.fr.md) et [VALVE_TIMING.fr.md](VALVE_TIMING.fr.md).

La [combustion prémélangée](PREMIXED_COMBUSTION.fr.md) optionnelle transporte maintenant le carburant, l'air frais
et les produits à propriétés de gaz constantes. La thermochimie détaillée des espèces, les échantillons de
véhicule calibrés et les jalons complets de moteur, de transmission et de contrôle restent ouverts.
