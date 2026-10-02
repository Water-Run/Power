# Dosage mécanique par coulisseau et régulation de pression

[English](HYDRAULIC_SPOOL.md) · [简体中文](HYDRAULIC_SPOOL.zh-CN.md) · **Français** · [Русский](HYDRAULIC_SPOOL.ru.md) · [日本語](HYDRAULIC_SPOOL.ja.md) · [한국어](HYDRAULIC_SPOOL.ko.md) · [Deutsch](HYDRAULIC_SPOOL.de.md) · [Español](HYDRAULIC_SPOOL.es.md) · [Italiano](HYDRAULIC_SPOOL.it.md) · [Português](HYDRAULIC_SPOOL.pt-BR.md)

`hydraulic_spool_valve` dose un port hydraulique à partir du déplacement réel d'un `hydraulic_piston` explicite. Le piston fournit la masse, le volume de fluide balayé, la force de pression et les extrémités de course compliantes ; un `linear_spring` distinct fournit la force de rappel, la précharge et l'amortissement. Plusieurs portées de dosage peuvent référencer le même piston.

## Équations et bornes

Les positions fermée et pleine ouverture définissent une course signée L. Avec le déplacement x :

```text
opening = clamp((x - closed_position) / L, 0, 1)
d       = pA - pB
Q       = K * opening * d / (d*d + transition_pressure*transition_pressure)^(1/4)
loss    = Q * d >= 0
```

K est un coefficient explicite de pleine ouverture en m3/(s*sqrt(Pa)) ; la pression de transition est positive. L'implémentation met le dénominateur à l'échelle pour éviter de mettre au carré d'énormes différences de pression. La course signée prend en charge les deux sens d'ouverture. Une portée fermée obture exactement ; une fuite exige un chemin supplémentaire explicite. Seule l'ouverture sature : la position, la pression, la vitesse et l'énergie stockée ne sont pas bornées.

La portée est équilibrée en pression, la force axiale de jet étant négligée. La différence de pression de son port de dosage n'applique pas de force axiale supplémentaire au piston. Les pressions des chambres avant et arrière de l'actionneur explicite fournissent sa force motrice. La chaleur de restriction et le travail du piston et du ressort utilisent les registres conservatifs existants. Ce modèle exclut le frottement de joint, les effets de quantité de mouvement de la force d'écoulement, la cavitation, l'usure et la géométrie ou la viscosité dépendantes de la température. C'est une réduction de recherche déclarée, plutôt qu'une vanne calibrée.

[MathWorks Spool Orifice (IL)](https://www.mathworks.com/help/hydro/ref/spoolorificeil.html) documente une aire d'ouverture variable et une option séparée de force axiale d'écoulement. Power! utilise sa propre portée linéaire normalisée et la loi de restriction passive existante ; aucune géométrie, valeur par défaut de propriété de fluide ni code d'implémentation n'ont été copiés.

## Résolution partagée et contrats

La vanne lit `x_old + dx/2` dans la même résolution de Newton conjointe que ses pressions de fluide, la force du piston et les contraintes mécaniques. Les dérivées analytiques incluent à la fois la pression et le déplacement de la portée. Dans la course de dosage, `dOpening/dx=1/L` ; en dehors, la dérivée est nulle. À chaque extrémité, le jacobien utilise la pente unilatérale moyenne. Cela conserve une boucle de retour simultanée, plutôt qu'une commande d'ouverture retardée.

| Paramètre | Signification |
|---|---|
| `piston_component` | ID stable d'un piston hydraulique explicite |
| `closed_position`, `full_open_position` | Positions distinctes en m ou mm, toutes deux dans la course nominale du piston |
| `coefficient` | Coefficient de pleine ouverture non négatif en `m3_s_sqrt_pa` |
| `transition_pressure` | Pression de régularisation positive en Pa ou bar |
| `reservoir_pressure` | Frontière de pression manométrique requise lorsque le B hydraulique est omis ou nul |

Les ports hydrauliques A/B et un puits thermique optionnel suivent le contrat de restriction. La vanne n'a ni `input_channel` ni `initial_input` ; observez son canal `opening` et commandez le circuit d'actionneur réel. Le débit et la puissance moyens et la chaleur hydraulique cumulée sont observables. Les unités, le type de composant référencé et les bornes de course produisent des erreurs de validation exploitables. Les contrats ordinaires de rollback de lot entier, d'annulation, de fork, d'horloge entière et de rejeu exact sur le même runtime incluent tous les états et historiques. Les empreintes des modèles physiques existants restent inchangées.

L'asset v15 ajoute un enregistrement de géométrie de dosage de 32 octets. JSON, la CLI et le MCP conservent les mêmes définitions. `get_example_model("spool-regulated-pump")` montre une pompe électrique, une dérivation régie mécaniquement et un remplissage et une vidange planifiés d'embrayage à pression. Sa précharge statique de fermeture de 200 N vient d'un ressort de rappel de 200 kN/m comprimé de 1 mm et d'une aire d'actionneur explicite de 1000 mm2. L'entraînement et le frein en rotation sont de 2 N*m ; une expérience de trois secondes laisse assez de temps pour que l'embrayage à plus basse pression capture. Les paramètres sont synthétiques et non vérifiés.

## Preuves et performance

Les contrôles couvrent la course de dosage signée, l'écoulement passif bidirectionnel, les dérivées analytiques de pression et de position, une racine de pression stationnaire indépendante, un transitoire RK4 séparé à trois états, le raffinement lisse du second ordre, une erreur décroissante à l'ouverture de la portée, l'égalisation à ports finis, le volume balayé, les énergies indépendantes de mouvement et de fluide, et les transactions complètes. L'avance à chaud et les lectures d'instantané n'allouent aucun octet managé. Les pentes de portée et les tampons Newton/LU appartiennent à chaque simulation ; aucune nouvelle horloge ni aucun travailleur n'est ajouté.

Voir [VALIDATION.fr.md](VALIDATION.fr.md) pour les erreurs mesurées, le périmètre d'exécution et le temps écoulé. Les vues Studio de vanne et d'actionneur et les tests d'import et Play sont préparés en source C# 9 ; les preuves réelles d'éditeur Unity et de Player restent en attente.
