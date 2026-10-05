# Rampe de carburant liquide alimentée par pompe

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · **Français** · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## Contrat

`liquid_rail_feed` associe un injecteur liquide à une pompe volumétrique existante et à une frontière matière/thermique explicite. Le nœud de sortie hydraulique doit correspondre à la compliance et à la pression absolue initiale de la rampe. Pompe et injecteur possèdent ce nœud ; les autres chemins fluides non suivis sont rejetés.

L'énergie de pression est stockée une seule fois dans le nœud hydraulique. Débit et réaction d'arbre suivent la résolution couplée conservatrice. Le carburant entrant apporte énergie calorique et chimique ; le stockage calorique de la rampe mélange sa température. Le débit inverse signé retourne du carburant à la température actuelle de la rampe. Décharge, chauffage de paroi, vapeur et combustion prescrite restent distincts.

`pump-fed-liquid-cylinder` et `pump-fed-needle-cylinder` conservent l'injection physique et le mouvement d'aiguille facultatif. Les KPI de pression utilisent une borne déclarée de pompe seule avec unités explicites. Lire `total_fuel_delivered`, `reservoir_enthalpy` et `fuel_energy_in` sur l'ID 1511 ; la pompe ID 1510 expose le travail réel arbre-fluide.

## Preuves et limites

Le format v28 conserve les liens et la température source et lit v1-v27. Échange analytique arbre/pression, raffinement ODE simultané indépendant, mélange calorique, bilans masse/carburant/énergie/volume, retour inverse et rollback complet ont des contrôles séparés.

Capacité géométrique, évent/espace gazeux/ballottement, cavitation, remplissage/efficacité/régulation mesurés et spray résolu restent ouverts. Paramètres `unverified` ; Unity Editor/Play/Player/IL2CPP réel et calibration OEM restent non vérifiés.

## Réservoir fini de carburant liquide

`liquid_fuel_tank` stocke masse liquide finie et énergie calorique avec la densité, référence thermique du film et pouvoir calorifique de l'injecteur associé. L'alimentation le choisit via `tank_component` et omet `supply_temperature`. Chaque réservoir appartient à une alimentation compatible.

Énergies calorique et chimique du réservoir entrent dans le stockage complet. Le transfert interne n'ajoute aucune matière ni énergie chimique externe. La pression d'entrée prescrite garde sa frontière de travail de pression. Admission/échappement gazeux peuvent encore transporter de l'énergie chimique.

Capacité géométrique, évent/espace gazeux/ballottement, cavitation, remplissage/efficacité/régulation mesurés et spray résolu restent ouverts. Paramètres `unverified` ; Unity Editor/Play/Player/IL2CPP réel et calibration OEM restent non vérifiés.

[LIQUID_FUEL_TANK.fr.md](LIQUID_FUEL_TANK.fr.md)

## Retour suivi de décharge de carburant

`liquid_rail_return` associe une alimentation à un `hydraulic_relief` unidirectionnel exclusif. La soupape relie la rampe à la même pression d'entrée prescrite que la pompe. Toute voie doit être enregistrée ; ports incompatibles, propriété dupliquée et voies non suivies sont rejetés.

`fluid_heat_fraction` choisit explicitement la part [0,1] des pertes transportée par le carburant de retour. Le reste suit le chemin thermique déclaré. Le mélange simultané rampe/réservoir conserve les bilans matière, chimie, travail de pression et chaleur. Les retours à source externe sortent matière et énergie par la frontière.

[LIQUID_FUEL_RETURN.fr.md](LIQUID_FUEL_RETURN.fr.md)
