# Piston à gaz linéaire et accumulateur hydraulique

[English](GAS_PISTON.md) · [简体中文](GAS_PISTON.zh-CN.md) · **Français** · [Русский](GAS_PISTON.ru.md) · [日本語](GAS_PISTON.ja.md) · [한국어](GAS_PISTON.ko.md) · [Deutsch](GAS_PISTON.de.md) · [Español](GAS_PISTON.es.md) · [Italiano](GAS_PISTON.it.md) · [Português](GAS_PISTON.pt-BR.md)

`gas_piston` relie une masse en translation à une chambre à gaz finie. La masse, l'énergie interne, la pression et la température de la chambre restent de vrais états de simulation. Son volume vient de la géométrie du piston plutôt que d'un volume de stockage fixe. Des orifices de gaz et des liaisons thermiques de paroi peuvent utiliser la même chambre.

Combiner un piston à gaz et un piston hydraulique sur le même nœud de translation crée un séparateur d'accumulateur à gaz. Les deux forces de pression agissent sur une masse et un déplacement dans la résolution conjointe. Les ressorts de rappel, l'amortissement et les extrémités de course compliantes restent des composants explicites. C'est un accumulateur à piston localisé, avec une compliance liquide effective constante et un gaz parfait à chaleur massique constante. La géométrie de vessie, le frottement de joint, la dissolution du gaz, la cavitation, l'usure et la calibration OEM restent hors de lui.

## Géométrie, pression et travail

L'aire A et le volume de référence Vr sont positifs. La position de référence xr est explicite, et le sens de compression s est +1 ou -1 :

```text
gas volume       V(x) = Vr - s*A*(x-xr)
absolute pressure p  = (gamma-1)*U/V
gas temperature   T  = U/(m*cv)
force on slider      = -s*A*(p-p_reference)
```

`p_reference` est une pression absolue explicite, y compris zéro pour un vide déclaré. Lorsqu'on compose un accumulateur, elle fournit la référence de réservoir utilisée par la convention de pression manométrique du liquide. Elle n'est pas inférée de la précharge de gaz. La pression et la température initiales du gaz, la constante des gaz R et gamma viennent du nœud de gaz ; sa masse initiale est `p_initial*V_initial/(R*T_initial)`.

Pendant l'intervalle mécanique, le travail du gaz fermé suit `U_next=U_old*(V_old/V_next)^(gamma-1)`. La force utilise la pression moyenne qui donne exactement ce transfert d'énergie discret. Le travail de pression de référence est `p_reference*s*A*dx` et entre dans le travail de source externe. Ainsi l'énergie interne du gaz plus l'énergie mécanique du séparateur équilibre le travail de fluide, le travail de référence et les pertes explicites. L'énergie en pression manométrique disponible depuis le stockage de gaz utilise `Delta U - reference_work` ; l'énergie absolue du gaz seule surestime ce transfert.

Pour une variation relative de volume z, le facteur de pression est `phi=(1-(1+z)^(1-gamma))/((gamma-1)*z)`. Sa valeur limite est un et sa dérivée limite est `-gamma/2`. L'implémentation utilise des séries mises à l'échelle pour une petite course, et des différences stables de logarithme et d'exponentielle sinon. Le jacobien de force est analytique. Des chambres à gaz inversées et opposées partagent la même coordonnée et conservent la convention signée de volume et de travail.

Le débit et la chaleur de gaz utilisent la répartition symétrique débit/travail/débit existante. Une chambre fermée conserve l'invariant adiabatique ; le traitement explicite de la température de paroi conserve la précision existante du premier ordre couplée à la paroi. Le débit massique de gaz est compté avec l'enthalpie du réservoir, plutôt que de traiter la masse ajoutée comme sans énergie. Aucun ajustement polytropique ni aucune dérogation isotherme ne remplace l'état d'énergie.

## Contrats et bornes numériques

| Paramètre | Signification |
|---|---|
| `node_a` | Nœud de translation de masse positive |
| `node_b` | Chambre à gaz avec un seul propriétaire de volume mobile ; omettre le `storage` du nœud |
| `area` | Aire positive en m2 ou mm2 |
| `reference_volume` | Volume positif en m3 ou litres |
| `reference_position` | Position en m ou mm à ce volume |
| `reference_pressure` | Pression absolue non négative en Pa ou bar |
| `compression_direction` | +1 (orientation par défaut) ou -1 |

Une chambre à gaz a un seul propriétaire de géométrie ; plusieurs chambres distinctes peuvent agir sur une masse. Un piston à gaz n'a ni entrée directe ni dérogation de puits thermique. Utilisez une source de force explicite, un piston hydraulique connecté, un orifice de gaz ou une liaison thermique de gaz. Observez la masse, l'énergie, la pression et la température de la chambre, ainsi que le volume du composant, la force sur la glissière et le `source_work` issu de la pression de référence.

Le volume de gaz doit rester positif, y compris tout au long de la course nominale d'un piston hydraulique partagé. Un intervalle mécanique accepté change au plus 25 % du volume de gaz courant. Une grande course, un volume non positif ou une force non résolue rejette le lot entier. Réduisez la taille du tick et inspectez les échelles de géométrie, de masse, de pression et de force avant de réessayer. Le mouvement et l'énergie ne sont pas bornés. La pénétration d'une extrémité compliante stocke encore le potentiel de butée explicite et reste soumise à un volume de gaz positif.

L'état physique et de contrôle, les inventaires de gaz et tous les historiques partagent l'annulation, le rollback complet, les hachages et des forks indépendants. L'asset v16 conserve les quatre quantités de géométrie et de référence et le sens de compression. JSON, la CLI et le MCP exposent les mêmes définitions. Les vues Studio de séparateur et de chambre et les tests d'import et Play sont préparés en source C# 9 ; les preuves réelles d'éditeur et de Player restent séparées.

## Expérience d'accumulateur et preuves

`gas-accumulator-pump` ajoute une chambre à gaz initiale de 50 ml, un séparateur de 50 g, un amortissement visqueux explicite et des extrémités compliantes à la pompe électrique, à la dérivation mécanique par coulisseau et à l'embrayage à pression. Le gaz commence à 200 kPa absolus et 300 K ; la pression de référence est 100 kPa. Le séparateur commence avec une pénétration d'appui compliante de 0.1 mm, équilibrant sa précharge contre une pression manométrique liquide nulle. Toutes les valeurs sont des paramètres de recherche. L'impulsion de tension et de demande de 3 à 4 s ouvre explicitement les deux chemins de remplissage et de vidange ; l'énergie de gaz stockée et le volume liquide balayé diminuent ensuite avant que la charge reprenne.

[MathWorks Gas-Charged Accumulator (IL)](https://www.mathworks.com/help/hydro/ref/gaschargedaccumulatoril.html) décrit la séparation gaz/liquide et le mécanisme de charge et de décharge. Power! compose ses propres ports de gaz à énergie finie et mécaniques/hydrauliques, plutôt que de copier un exposant polytropique fixe, des valeurs de paramètres par défaut ou du code d'implémentation.

Les contrôles incluent le travail adiabatique analytique et ses dérivées, une course mince, un transitoire RK4 masse/énergie séparé avec raffinement lisse du second ordre, des chambres opposées, un mouvement commun gaz/fluide, un raffinement RK4 à paroi finie, un apport de gaz à volume mobile, des comptes indépendants d'énergie et de volume, et des transactions complètes. Les chambres fermées et non mélangées, sans transport ni chaleur, sautent l'intégration redondante à taux nul après validation de l'état. L'optimisation mesurée conserve chaque valeur et hachage de borne ; l'avance stationnaire et les lectures d'instantané n'allouent aucun octet managé. Les bornes d'erreur détaillées, les durées et le périmètre de plateforme sont dans [VALIDATION.fr.md](VALIDATION.fr.md).
