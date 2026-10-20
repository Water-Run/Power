# Retour suivi de décharge de carburant

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · **Français** · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## Contrat

`liquid_rail_return` associe une alimentation à un `hydraulic_relief` unidirectionnel exclusif. La soupape relie la rampe à la même pression d'entrée prescrite que la pompe. Toute voie doit être enregistrée ; ports incompatibles, propriété dupliquée et voies non suivies sont rejetés.

`fluid_heat_fraction` choisit explicitement la part [0,1] des pertes transportée par le carburant de retour. Le reste suit le chemin thermique déclaré. Le mélange simultané rampe/réservoir conserve les bilans matière, chimie, travail de pression et chaleur. Les retours à source externe sortent matière et énergie par la frontière.

Lire `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` et `mass_flow` sur retour ID 1515. Alimentation ID 1511 rapporte le transfert brut de pompe. La circulation brute peut dépasser le stock initial ; stock actuel égale stock initial moins pompage plus retour.

## Preuves et limites

`recirculating-liquid-cylinder` et `recirculating-needle-cylinder` gardent carburant fini, injection réelle, évaporation et aiguille facultative. v33 conserve liens et fraction et lit v1-v32. Chaque retour ajoute 8 états dans les mêmes bornes.

Décroissance/travail indépendants, raffinement mécanique/pression/thermique simultané, fractions, voies multiples, frontières externes, replay, rollback et allocations passent. Ébullition ou intervalle de transport non résolu fait échouer tout le lot.

Réservoir rigide mélangé, liquide incompressible et gaz idéal. Ballottement/forme hydrostatique, thermodynamique des mélanges réels, cavitation, cartes pompe/vanne mesurées, calibration OEM et Unity Editor/Play/Player/IL2CPP réel restent ouverts. Paramètres `unverified`.

[VALIDATION.fr.md](VALIDATION.fr.md)

## Géométrie du réservoir et espace gazeux fini

`liquid_fuel_tank.parameters.headspace` déclare `capacity` en `m3` ou `l` et `gas_node`. Le gaz omet `storage` : son volume vaut `capacity - liquid_mass / density`, avec un seul propriétaire et un volume positif. Pompe et retour utilisent une pression prescrite nulle car le gaz fini détermine la pression d'entrée.

[Géométrie du réservoir et espace gazeux fini](TANK_HEADSPACE.fr.md)

## Équilibre des phases du carburant et limites d'alimentation

L'alimentation liquide par défaut rejette les états humides sous saturation. L'option de réservoir à volume fini résout le transfert vapeur source avec potentiels chimiques corrigés. Rail et aspiration restent liquides séparés ; entraînement de bulles et écoulement cavitant demandent une physique supplémentaire.

La comparaison ne tolère que l'arrondi binary64 : `64 * epsilon * max(p, p_sat)`, avec `epsilon = 2^-52`. La marge affichée n'est pas écrêtée. C'est un contrôle de saturation globale ; pertes locales et NPSH de pompe demandent un modèle d'écoulement résolu. Les contrôles indépendants exigent aussi une entropie totale non décroissante pour le flash adiabatique et l'échange avec la paroi finie.

[Équilibre des phases du carburant et limites d'alimentation](FUEL_PHASE_EQUILIBRIUM.fr.md)

## Équilibre du réservoir à volume fini

Activer `liquid_fuel_tank.parameters.headspace.phase_equilibrium` avec `true` pour résoudre évaporation et condensation du réservoir. Le film associé fournit les propriétés `phase_equilibrium` ; l'espace gazeux suivi partage R, cv, pouvoir calorifique et rapport stœchiométrique. Déclarer une température initiale commune et une pression partielle saturée pour un réservoir humide ; un réservoir sec commence au plus à saturation. La densité totale reste inférieure à celle du liquide.

`evaporated_fuel_mass`, `condensed_fuel_mass` et `mass_flow` signé cumulent le transfert net par pas physique complet. Lire ensemble `vapor_partial_pressure`, `saturation_pressure` corrigée, inventaire liquide et volume gazeux. La pompe aspire seulement le liquide séparé ; transport diphasique pompe/rail, entraînement et NPSH local restent ouverts.

[Équilibre du réservoir à volume fini](TANK_PHASE_EQUILIBRIUM.fr.md)
