# Notes de reprise du développement moteur

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · **Français** · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Point de reprise

La pompe du domaine carburant alimente désormais une aiguille physique et une décharge à titre figé dans un récepteur allumé à volume liquide fini. Les inventaires incluent changement de phase, travail de déplacement, énergie cinétique du jet et impulsion à la paroi immobile.

[Pompe carburant diphasique entraînée par arbre](FUEL_CHAMBER_PUMP.fr.md)

`LiquidColumn` est une référence C# autonome pour un tube horizontal rigide à section, vitesse uniforme, masse volumique liquide et température constantes. Les pressions absolues aux ports sont prescrites pendant chaque intervalle. Les résistances linéaire et quadratique sont des entrées de recherche explicites. Ce n'est pas un composant du graphe ; les équations du réseau et de la tuyère restent inchangées. [Référence de quantité de mouvement à l'entrée liquide](LIQUID_COLUMN.fr.md)

Un `fuel_inertial_pipe` relie deux nœuds de carburant finis et une paroi thermique finie. La ligne horizontale rigide possède masse liquide, énergie calorique et cinétique propres, en plus des réservoirs. Sa densité est constante, sa température variable et parfaitement mélangée. Les ports et toute la ligne restent liquides ; les liens quasi statiques gardent leur comportement diphasique séparé. [Quantité de mouvement d'une ligne finie de carburant liquide](FUEL_INERTIAL_PIPE.fr.md)

Un `equilibrium_fuel_duct` transporte masse, quantité de mouvement et énergie totale dans des cellules explicites `compressible_fuel`. Le liquide est compressible et la vapeur idéale ; les phases partagent pression, température et vitesse. L’équilibre local permet condensation et vaporisation éclair. Deux réservoirs stationnaires finis et une paroi thermique finie ferment les bilans. [Conduites de carburant compressible à l’équilibre](EQUILIBRIUM_FUEL_DUCT.fr.md)

Un `compressible_fuel_chamber` relie un arbre à un réservoir fini `compressible_fuel`. Le mouvement change son volume ; le même travail de pression modifie l’énergie du fluide et le couple. Le réservoir peut alimenter ou recevoir un `equilibrium_fuel_duct`, avec vaporisation éclair et condensation à l’équilibre. [Chambres de carburant compressible entraînées par arbre](COMPRESSIBLE_FUEL_CHAMBER.fr.md)

Un `compressible_fuel_nozzle` transporte le fluide dans un seul sens entre réservoirs finis `compressible_fuel`, y compris les chambres entraînées par arbre. Compression liquide et vapeur idéale partagent leur matériau. Déclarer `phase_mode` comme `frozen` : la qualité massique reste fixe pendant le transit, puis les réservoirs se rééquilibrent. [Buses de carburant compressible à qualité figée](COMPRESSIBLE_FUEL_NOZZLE.fr.md)

Régler `phase_mode` de `compressible_fuel_nozzle` sur `equilibrium` impose l’équilibre homogène instantané pendant le transit. Liquide et vapeur partagent pression, température et vitesse ; le titre change sur l’isentrope. La buse et les réservoirs finis utilisent le même potentiel liquide compressible et la même référence de vapeur idéale. `frozen` reste une hypothèse distincte prise en charge. [Transit de carburant à l’équilibre dans les buses](EQUILIBRIUM_FUEL_NOZZLE.fr.md)

Un nœud `relaxing_fuel` possède un titre massique de vapeur indépendant. Liquide et vapeur partagent pression, température et vitesse, mais l’équilibre chimique demande un temps fini déclaré. Un `relaxing_fuel_duct` transporte masse, quantité de mouvement, énergie totale et masse de vapeur dans des cellules explicites. Le temps de séjour dépend de leur géométrie et de la vitesse réelle. [Relaxation de phase du carburant à vitesse finie](RELAXING_FUEL.fr.md)

## Prochaine étape

Étendre les conditions aux limites compatibles avec la relaxation finie et obtenir des mesures du matériau, puis ajouter spray résolu et récepteurs en combustion. Poursuivre allumage, admission/échappement, pertes mécaniques, actionnement de transmission et contrôle ECU/TCU coordonné. L’étalonnage mesuré et l’acceptation Unity restent distincts. [Conduites de carburant compressible à l’équilibre](EQUILIBRIUM_FUEL_DUCT.fr.md)

## Acceptation

Exiger références analytiques ou limites indépendantes, bilans complets masse/énergie et raffinement du pas adapté. Préserver retour arrière complet, annulation, branches, canaux stables, rejeu portable et anciens lecteurs. Continuer ensuite allumage, admission/échappement, pertes mécaniques, transmission et coordination ECU/TCU.

Les paramètres restent `unverified`. Déclarer `initial_vapor_mass_fraction` dans [0,1] et un temps entier de 1 ns à une heure. Respecter les bornes de température et de pression positive du matériau. Le modèle suppose un conduit rigide horizontal de section constante, un pseudo-fluide pur, un liquide sans dilatation thermique et une vapeur idéale. Il omet glissement, retard thermique interfacial, tension superficielle, spray et gaz porteur. Le couplage direct du domaine relaxant aux pompes, buses, chambres mobiles et récepteurs en combustion reste à faire. Conservation, rejeu et raffinement ne valident ni le matériel mesuré ni Unity réel. Utiliser une vapeur diluée loin de la région critique réelle ; le matériau doit conserver une enthalpie latente positive et des volumes spécifiques de coexistence distincts.

[DEVELOPMENT_STATUS.fr.md](DEVELOPMENT_STATUS.fr.md) · [ROADMAP.fr.md](ROADMAP.fr.md) · [VALIDATION.fr.md](VALIDATION.fr.md)

[Décharge de carburant à titre figé et récepteurs finis](FUEL_DISCHARGE.fr.md)
