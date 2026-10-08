# Réservoir fini de carburant liquide

[English](LIQUID_FUEL_TANK.md) · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · **Français** · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · [Deutsch](LIQUID_FUEL_TANK.de.md) · [Español](LIQUID_FUEL_TANK.es.md) · [Italiano](LIQUID_FUEL_TANK.it.md) · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## Contrat

`liquid_fuel_tank` stocke masse liquide finie et énergie calorique avec la densité, référence thermique du film et pouvoir calorifique de l'injecteur associé. L'alimentation le choisit via `tank_component` et omet `supply_temperature`. Chaque réservoir appartient à une alimentation compatible.

Le débit positif est borné par l'inventaire restant sur l'intervalle accepté. Le même déplacement rempli effectif fixe réaction d'arbre et transfert de pression, conservant le travail arbre/fluide. Une rotation avant à vide ne livre ni liquide ni travail fluide ; le retour signé mélange l'énergie calorique actuelle de la rampe dans le réservoir.

Énergies calorique et chimique du réservoir entrent dans le stockage complet. Le transfert interne n'ajoute aucune matière ni énergie chimique externe. La pression d'entrée prescrite garde sa frontière de travail de pression. Admission/échappement gazeux peuvent encore transporter de l'énergie chimique.

`finite-tank-liquid-cylinder` et `finite-tank-needle-cylinder` utilisent réservoir ID 1513 et alimentation ID 1511. Lire `mass`, `temperature`, `internal_energy`, `chemical_energy` et `tank_state` ; 0 signifie liquide présent et 1 vide. La température sèche rapporte la référence initiale déclarée.

## Géométrie du réservoir et espace gazeux fini

`liquid_fuel_tank.parameters.headspace` déclare `capacity` en `m3` ou `l` et `gas_node`. Le gaz omet `storage` : son volume vaut `capacity - liquid_mass / density`, avec un seul propriétaire et un volume positif. Pompe et retour utilisent une pression prescrite nulle car le gaz fini détermine la pression d'entrée.

[Géométrie du réservoir et espace gazeux fini](TANK_HEADSPACE.fr.md)

## Preuves et limites

v29 conserve les données et la sélection et lit v1-v28. Chaque réservoir ajoute 4 états dans les bornes inchangées. Échange humide indépendant, pression/énergie d'arbre après épuisement analytiques, mélange retour, bilans complets, rollback, branches et pas sans allocation sont vérifiés.

Réservoir rigide mélangé, liquide incompressible et gaz idéal. Ballottement/forme hydrostatique, équilibre des phases, cavitation, cartes pompe/vanne mesurées, calibration OEM et Unity Editor/Play/Player/IL2CPP réel restent ouverts. Paramètres `unverified`.

[VALIDATION.fr.md](VALIDATION.fr.md)

## Retour suivi de décharge de carburant

`liquid_rail_return` associe une alimentation à un `hydraulic_relief` unidirectionnel exclusif. La soupape relie la rampe à la même pression d'entrée prescrite que la pompe. Toute voie doit être enregistrée ; ports incompatibles, propriété dupliquée et voies non suivies sont rejetés.

`fluid_heat_fraction` choisit explicitement la part [0,1] des pertes transportée par le carburant de retour. Le reste suit le chemin thermique déclaré. Le mélange simultané rampe/réservoir conserve les bilans matière, chimie, travail de pression et chaleur. Les retours à source externe sortent matière et énergie par la frontière.

[LIQUID_FUEL_RETURN.fr.md](LIQUID_FUEL_RETURN.fr.md)
