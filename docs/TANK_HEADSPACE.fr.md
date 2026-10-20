# Géométrie du réservoir et espace gazeux fini

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · **Français** · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## Contrat

`liquid_fuel_tank.parameters.headspace` déclare `capacity` en `m3` ou `l` et `gas_node`. Le gaz omet `storage` : son volume vaut `capacity - liquid_mass / density`, avec un seul propriétaire et un volume positif. Pompe et retour utilisent une pression prescrite nulle car le gaz fini détermine la pression d'entrée.

Le solveur couplé échange le travail de pression entre arbre, rampe et gaz sans source externe. Orifices gazeux et liens thermiques fournissent ventilation et chaleur explicites. Lire `pressure`, `fill_fraction`, `hydraulic_work` cumulé signé et masse, énergie, volume du gaz. Dose, liquide livré, évaporation et combustion restent distincts.

## Preuves et limites

Les exemples `vented-tank-liquid-cylinder` et `vented-tank-needle-cylinder` utilisent réservoir 1513, gaz 1520 et entrée de ventilation 960. Asset v32 conserve la géométrie et lit v1-v31. Travail/dérivées analytiques, convergence ODE indépendante, bilans, rejeu portable/MCP, retour arrière et avancement sans allocation passent.

Réservoir rigide mélangé, liquide incompressible et gaz idéal. Ballottement/forme hydrostatique, thermodynamique des mélanges réels, cavitation, cartes pompe/vanne mesurées, calibration OEM et Unity Editor/Play/Player/IL2CPP réel restent ouverts. Paramètres `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.fr.md)

## Équilibre du réservoir à volume fini

Activer `liquid_fuel_tank.parameters.headspace.phase_equilibrium` avec `true` pour résoudre évaporation et condensation du réservoir. Le film associé fournit les propriétés `phase_equilibrium` ; l'espace gazeux suivi partage R, cv, pouvoir calorifique et rapport stœchiométrique. Déclarer une température initiale commune et une pression partielle saturée pour un réservoir humide ; un réservoir sec commence au plus à saturation. La densité totale reste inférieure à celle du liquide.

`evaporated_fuel_mass`, `condensed_fuel_mass` et `mass_flow` signé cumulent le transfert net par pas physique complet. Lire ensemble `vapor_partial_pressure`, `saturation_pressure` corrigée, inventaire liquide et volume gazeux. La pompe aspire seulement le liquide séparé ; transport diphasique pompe/rail, entraînement et NPSH local restent ouverts.

[Équilibre du réservoir à volume fini](TANK_PHASE_EQUILIBRIUM.fr.md)
