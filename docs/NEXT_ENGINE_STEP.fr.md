# Notes de reprise du développement moteur

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · **Français** · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Point de reprise

La pompe du domaine carburant alimente désormais une aiguille physique et une décharge à titre figé dans un récepteur allumé à volume liquide fini. Les inventaires incluent changement de phase, travail de déplacement, énergie cinétique du jet et impulsion à la paroi immobile.

[Pompe carburant diphasique entraînée par arbre](FUEL_CHAMBER_PUMP.fr.md)

`LiquidColumn` est une référence C# autonome pour un tube horizontal rigide à section, vitesse uniforme, masse volumique liquide et température constantes. Les pressions absolues aux ports sont prescrites pendant chaque intervalle. Les résistances linéaire et quadratique sont des entrées de recherche explicites. Ce n'est pas un composant du graphe ; les équations du réseau et de la tuyère restent inchangées. [Référence de quantité de mouvement à l'entrée liquide](LIQUID_COLUMN.fr.md)

Un `fuel_inertial_pipe` relie deux nœuds de carburant finis et une paroi thermique finie. La ligne horizontale rigide possède masse liquide, énergie calorique et cinétique propres, en plus des réservoirs. Sa densité est constante, sa température variable et parfaitement mélangée. Les ports et toute la ligne restent liquides ; les liens quasi statiques gardent leur comportement diphasique séparé. [Quantité de mouvement d'une ligne finie de carburant liquide](FUEL_INERTIAL_PIPE.fr.md)

Un `equilibrium_fuel_duct` transporte masse, quantité de mouvement et énergie totale dans des cellules explicites `compressible_fuel`. Le liquide est compressible et la vapeur idéale ; les phases partagent pression, température et vitesse. L’équilibre local permet condensation et vaporisation éclair. Deux réservoirs stationnaires finis et une paroi thermique finie ferment les bilans. [Conduites de carburant compressible à l’équilibre](EQUILIBRIUM_FUEL_DUCT.fr.md)

## Prochaine étape

Relier les conduites compressibles à des chambres entraînées par arbre et à des frontières aiguille/buse compatibles, avec travail de pression et hypothèses de phase explicites. Poursuivre relaxation/glissement, allumage, admission/échappement, pertes mécaniques et coordination ECU/TCU. Étalonnage mesuré et acceptation Unity réelle restent distincts. [Conduites de carburant compressible à l’équilibre](EQUILIBRIUM_FUEL_DUCT.fr.md)

## Acceptation

Exiger références analytiques ou limites indépendantes, bilans complets masse/énergie et raffinement du pas adapté. Préserver retour arrière complet, annulation, branches, canaux stables, rejeu portable et anciens lecteurs. Continuer ensuite allumage, admission/échappement, pertes mécaniques, transmission et coordination ECU/TCU.

Les paramètres restent `unverified`. Respecter les bornes de température et pression, une enthalpie latente positive, des volumes de coexistence séparés et une vapeur diluée. La conduite est horizontale, rigide, de section constante et à équilibre homogène instantané. Nucléation à vitesse finie, glissement, tension superficielle, gravité, élasticité et comportement critique réel sont omis. La diffusion du premier ordre peut étaler les fronts ; conservation et relecture ne prouvent ni précision spatiale ni validité matérielle mesurée. L’acceptation Unity réelle reste à réaliser.

[DEVELOPMENT_STATUS.fr.md](DEVELOPMENT_STATUS.fr.md) · [ROADMAP.fr.md](ROADMAP.fr.md) · [VALIDATION.fr.md](VALIDATION.fr.md)

[Décharge de carburant à titre figé et récepteurs finis](FUEL_DISCHARGE.fr.md)
