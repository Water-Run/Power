# Retour suivi de décharge de carburant

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · **Français** · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## Contrat

`liquid_rail_return` associe une alimentation à un `hydraulic_relief` unidirectionnel exclusif. La soupape relie la rampe à la même pression d'entrée prescrite que la pompe. Toute voie doit être enregistrée ; ports incompatibles, propriété dupliquée et voies non suivies sont rejetés.

`fluid_heat_fraction` choisit explicitement la part [0,1] des pertes transportée par le carburant de retour. Le reste suit le chemin thermique déclaré. Le mélange simultané rampe/réservoir conserve les bilans matière, chimie, travail de pression et chaleur. Les retours à source externe sortent matière et énergie par la frontière.

Lire `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` et `mass_flow` sur retour ID 1515. Alimentation ID 1511 rapporte le transfert brut de pompe. La circulation brute peut dépasser le stock initial ; stock actuel égale stock initial moins pompage plus retour.

## Preuves et limites

`recirculating-liquid-cylinder` et `recirculating-needle-cylinder` gardent carburant fini, injection réelle, évaporation et aiguille facultative. v30 conserve liens et fraction et lit v1-v29. Chaque retour ajoute 8 états dans les mêmes bornes.

Décroissance/travail indépendants, raffinement mécanique/pression/thermique simultané, fractions, voies multiples, frontières externes, replay, rollback et allocations passent. Ébullition ou intervalle de transport non résolu fait échouer tout le lot.

Réservoir rigide mélangé, liquide incompressible et gaz idéal. Ballottement/forme hydrostatique, équilibre des phases, cavitation, cartes pompe/vanne mesurées, calibration OEM et Unity Editor/Play/Player/IL2CPP réel restent ouverts. Paramètres `unverified`.

[VALIDATION.fr.md](VALIDATION.fr.md)

## Géométrie du réservoir et espace gazeux fini

`liquid_fuel_tank.parameters.headspace` déclare `capacity` en `m3` ou `l` et `gas_node`. Le gaz omet `storage` : son volume vaut `capacity - liquid_mass / density`, avec un seul propriétaire et un volume positif. Pompe et retour utilisent une pression prescrite nulle car le gaz fini détermine la pression d'entrée.

[Géométrie du réservoir et espace gazeux fini](TANK_HEADSPACE.fr.md)

## Équilibre des phases du carburant et limites d'alimentation

Rails, réservoirs finis et entrées exposent `saturation_pressure` et `cavitation_margin` en Pa. La marge est la pression absolue locale moins la pression de saturation à la température liquide. Une marge négative avec du liquide rejette tout le lot. Ces contrôles bornent l'alimentation monophasique ; transfert vapeur du réservoir, bulles et débit de pompe cavitant restent ouverts.

La comparaison ne tolère que l'arrondi binary64 : `64 * epsilon * max(p, p_sat)`, avec `epsilon = 2^-52`. La marge affichée n'est pas écrêtée. C'est un contrôle de saturation globale ; pertes locales et NPSH de pompe demandent un modèle d'écoulement résolu. Les contrôles indépendants exigent aussi une entropie totale non décroissante pour le flash adiabatique et l'échange avec la paroi finie.

[Équilibre des phases du carburant et limites d'alimentation](FUEL_PHASE_EQUILIBRIUM.fr.md)
