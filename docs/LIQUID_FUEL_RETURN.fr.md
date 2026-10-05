# Retour suivi de décharge de carburant

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · **Français** · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## Contrat

`liquid_rail_return` associe une alimentation à un `hydraulic_relief` unidirectionnel exclusif. La soupape relie la rampe à la même pression d'entrée prescrite que la pompe. Toute voie doit être enregistrée ; ports incompatibles, propriété dupliquée et voies non suivies sont rejetés.

`fluid_heat_fraction` choisit explicitement la part [0,1] des pertes transportée par le carburant de retour. Le reste suit le chemin thermique déclaré. Le mélange simultané rampe/réservoir conserve les bilans matière, chimie, travail de pression et chaleur. Les retours à source externe sortent matière et énergie par la frontière.

Lire `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` et `mass_flow` sur retour ID 1515. Alimentation ID 1511 rapporte le transfert brut de pompe. La circulation brute peut dépasser le stock initial ; stock actuel égale stock initial moins pompage plus retour.

## Preuves et limites

`recirculating-liquid-cylinder` et `recirculating-needle-cylinder` gardent carburant fini, injection réelle, évaporation et aiguille facultative. v28 conserve liens et fraction et lit v1-v27. Chaque retour ajoute 8 états dans les mêmes bornes.

Décroissance/travail indépendants, raffinement mécanique/pression/thermique simultané, fractions, voies multiples, frontières externes, replay, rollback et allocations passent. Ébullition ou intervalle de transport non résolu fait échouer tout le lot. Géométrie/évent, soupapes/pompes mesurées, cavitation, spray, calibration OEM et Unity Editor/Play/Player/IL2CPP réel restent ouverts.

[VALIDATION.fr.md](VALIDATION.fr.md)
