# Synchronisation échantillonnée et passation étagée du double embrayage

[English](DCT_CONTROL.md) · [简体中文](DCT_CONTROL.zh-CN.md) · **Français** · [Русский](DCT_CONTROL.ru.md) · [日本語](DCT_CONTROL.ja.md) · [한국어](DCT_CONTROL.ko.md) · [Deutsch](DCT_CONTROL.de.md) · [Español](DCT_CONTROL.es.md) · [Italiano](DCT_CONTROL.it.md) · [Português](DCT_CONTROL.pt-BR.md)

`dct_controller` possède les deux canaux d'embrayage d'entraînement et les huit canaux de sélecteur d'un [graphe de recherche à sept rapports avant et marche arrière](DUAL_CLUTCH_TRANSMISSION.fr.md). Sa commande entière de rapport demandé est distincte du rapport réel confirmé, des chemins sélectionnés, de la phase de passage, de l'erreur de synchronisation mesurée et du défaut du contrôleur.

C'est une machine à états de recherche pilotée par capteurs, avec une interruption de couple explicite. Elle n'établit pas un mélange de couple TCU/ECU complet, un comportement détaillé de crabot, de bague de synchronisation ou d'actionneur d'embrayage, une stratégie de passage calibrée ni une gestion complète des défauts véhicule.

## États et confirmation physique

| Phase | Politique de commande maintenue et transition |
|---|---|
| Point mort | Les deux embrayages d'entraînement et les sélecteurs sont relâchés |
| Préparation | Le chemin opposé cible est déchargé et présélectionné pendant que l'entraînement précédent reste engagé |
| Relâchement | L'ouverture de l'entraînement précédent décroît en rampe ; l'entraînement cible reste relâché |
| Synchronisation | Le sélecteur cible monte en rampe jusqu'à l'ouverture complète, les deux entraînements étant relâchés ; attente du glissement mesuré et du verrouillage physique |
| Engagement | L'entraînement cible monte en rampe ; l'autre entraînement reste relâché |
| Marche | Entraînement et sélecteur cibles confirmés ; la présélection du chemin adjacent déchargé est permise |
| Défaut | Les deux entraînements et chaque sélecteur sont relâchés ; le défaut est conservé jusqu'au point mort ou à une demande différente |

La cible est mémorisée pendant qu'une passation est en cours. Les demandes ultérieures hors point mort sont traitées après cette passation ; le point mort interrompt à l'échantillon dû. Les changements sur le même chemin d'entrée relâchent son entraînement avant de changer les sélecteurs. Les changements vers le chemin d'entrée opposé peuvent préparer la cible déchargée avant le relâchement de l'entraînement. Chaque chemin commande au plus un sélecteur, et aucun recouvrement commandé des embrayages d'entraînement n'est utilisé.

Les rampes d'ouverture des sélecteurs utilisent la durée d'engagement configurée. Un sélecteur n'est prêt qu'après la commande complète, un glissement mesuré dans la tolérance fournie et le mode physique `Locked`. L'entraînement n'est confirmé qu'après la commande d'engagement complète, un petit glissement d'entraînement et le verrouillage physique. L'acceptation d'une commande n'annonce pas un rapport instantané ni l'achèvement physique du rapport.

Une présélection inactive peut perturber brièvement un chemin confirmé. Le rapport réel de l'instantané est nul tant que l'entraînement ou le chemin sélectionné n'est pas physiquement verrouillé. La perte persistante est temporisée séparément ; le contrôleur ne confond pas un échantillon transitoire avec un défaut soutenu. Ce temporisateur se réinitialise aux changements de phase et au rétablissement.

## Rapport demandé, sens et défauts

Le rapport demandé est un entier dans `[-1,7]`, zéro étant le point mort et -1 la marche arrière. La validation statique, immédiate et planifiée des entrées rejette les fractions. La commande source utilise des unités `state_code` explicites ; aucune fraction d'embrayage ordinaire n'est interprétée comme un numéro de rapport.

Un dépassement du délai de synchronisation renvoie un défaut observable et déchargé. Une demande de marche arrière contre un mouvement véhicule positif au-dessus de la limite de vitesse fournie, ou une demande de marche avant contre un mouvement négatif, est bloquée comme défaut de changement de sens. La perte soutenue d'un verrouillage confirmé d'entraînement ou de sélecteur utilise le même délai fourni et un code de défaut distinct. Ces issues du contrôleur sont des états de politique physique, pas des échecs numériques ni des KPI implicites de passage réussi.

| Code de défaut | Signification |
|---:|---|
| 0 | Aucun défaut |
| 1 | Délai de synchronisation ou d'engagement |
| 2 | Demande de changement de sens bloquée par le mouvement du véhicule |
| 3 | Perte persistante du verrouillage confirmé |

Le point mort efface le défaut et libère le train. Une demande valide différente peut commencer une nouvelle tentative ; soumettre de nouveau la même cible échouée ne réinitialise pas le délai à chaque échantillon. Les décisions de défaut de plus haut niveau, les contrôles de plausibilité, les défaillances de capteur et les fonctions de sécurité conducteur/véhicule restent un travail séparé.

## Définition et propriété

Le contrôleur déclare le nœud A du moteur, le nœud véhicule, les ID des embrayages d'entraînement impair et pair, huit sélecteurs dans l'ordre avant 1 à 7 puis marche arrière, et son entrée de rapport demandé. Les dix canaux d'actionneur doivent être distincts, initialement relâchés, et avoir un seul propriétaire. Le compilateur vérifie les types d'embrayage ordinaires, la topologie arbre/moyeu/démultiplication finale, l'affectation impair/pair, le chemin du pignon de renvoi de marche arrière et les références stables. Les listes de sélecteurs sont copiées dans les données de définition et compilées immuables.

Le minutage explicite comprend les nanosecondes d'échantillon, de relâchement, d'engagement et de délai de synchronisation. L'échantillonnage s'aligne sur les ticks physiques ; les autres temps sont des multiples positifs de l'échantillon et au plus dix secondes. La tolérance de synchronisation et la limite de vitesse de sens utilisent `rad_s` ou `rpm`. Aucune valeur OEM, carte d'actionneur ou courbe de pertes n'est fournie silencieusement.

Les agents écrivent le rapport demandé. Les écritures directes d'entraînement ou de sélecteur renvoient `controlled_input` avec le nom et le canal de commande corrects, et laissent la révision et l'état inchangés. Les lectures exposent la demande en cours, le rapport confirmé, les sélections impair/pair commandées, la phase, le glissement du sélecteur cible et le défaut. Ces codes d'état et ces canaux physiques conservent leurs sémantiques distinctes.

## Horloges entières et transactions complètes

Les échantillons s'exécutent sur le temps de simulation entier borné. Les écritures d'entrée n'avancent pas la mémoire de contrôle. Les fractions maintenues sont appliquées au solveur physique normal ; l'inertie, les réactions d'engrenage, la chaleur de synchronisation et d'entraînement restent dans les registres existants. L'état du contrôleur contient le rapport mémorisé et actif, les sélections, la phase et le défaut, l'horloge de phase, l'erreur mesurée et le temporisateur de verrouillage persistant. Les forks, l'annulation, les lots échoués tardifs et les intervalles spéculatifs copient, hachent et soumettent au rollback cette mémoire et chaque commande maintenue ensemble. L'avance réussie et les instantanés n'allouent pas de mémoire managée.

Les longues marches en rapport commandé utilisent des incréments de coordonnées compensés à partir de la vitesse au point milieu. Leur compensation est transactionnelle et hachée ; les tolérances de phase strictes restent inchangées. Cela résout l'arrondi accumulé exposé par le nouveau scénario long de synchronisation en charge. Les chemins de modèle antérieurs conservent le comportement d'intégration et de rejeu précédent.

La limite d'état rapportée est explicitement **128**, avec 32 nœuds et 64 composants inchangés. Cela permet la composition de recherche complète allumée/DCT/contrôleur, qui dépasse la borne antérieure de 64 états. La compilation et l'avance aux bornes exactes, ainsi que les modèles physiques et de contrôleur au-delà de la limite, sont contrôlés ; les constructions et les tests restent en série.

## Expériences portables et partagées

L'asset v22 ajoute un enregistrement typé de 104 octets de chemin, de minutage et de tolérance par contrôleur DCT. Il conserve les lecteurs v1-v21, les ID précédents stables et les contrôles bornés de compte et de longueur, de condensé, de propriété typée, d'unités et de compilation physique. Les modèles de contrôleur ajoutent la balise d'empreinte 26. Les champs demandé, réel, sélection, phase, erreur et défaut sont ajoutés sans changer les ID précédents. Un scénario de graphe authentique v21 conserve son condensé et son rejeu mis à niveau sur le même runtime.

`controlled-dual-clutch` émet des demandes de rapport à travers les sept chemins et des descentes choisies. Il observe la passation finale et la présélection inactive jusqu'à l'achèvement physique, plutôt que de supposer un temps nominal. `controlled-fired-dual-clutch` combine la même politique échantillonnée avec la combustion prémélangée à cylindre ouvert. JSON, la CLI, le rejeu portable et un serveur MCP enfant réel partagent les définitions.

[VALIDATION.fr.md](VALIDATION.fr.md) consigne les preuves d'état et d'interverrouillage, de défaut et de rétablissement, de propriété, d'entrée entière, de chemin immuable, de capacité, de conservation de phase longue, de conservation et de rejeu complet. Tous les paramètres restent `unverified`. Le mélange de couple, le comportement complet d'actionneur, de capteur et d'ECU, l'AT complète, Unity réel et les groupes motopropulseurs cibles calibrés restent inachevés.
