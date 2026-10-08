# Géométrie du réservoir et espace gazeux fini

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · **Français** · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## Contrat

`liquid_fuel_tank.parameters.headspace` déclare `capacity` en `m3` ou `l` et `gas_node`. Le gaz omet `storage` : son volume vaut `capacity - liquid_mass / density`, avec un seul propriétaire et un volume positif. Pompe et retour utilisent une pression prescrite nulle car le gaz fini détermine la pression d'entrée.

Le solveur couplé échange le travail de pression entre arbre, rampe et gaz sans source externe. Orifices gazeux et liens thermiques fournissent ventilation et chaleur explicites. Lire `pressure`, `fill_fraction`, `hydraulic_work` cumulé signé et masse, énergie, volume du gaz. Dose, liquide livré, évaporation et combustion restent distincts.

## Preuves et limites

Les exemples `vented-tank-liquid-cylinder` et `vented-tank-needle-cylinder` utilisent réservoir 1513, gaz 1520 et entrée de ventilation 960. Asset v29 conserve la géométrie et lit v1-v28. Travail/dérivées analytiques, convergence ODE indépendante, bilans, rejeu portable/MCP, retour arrière et avancement sans allocation passent.

Réservoir rigide mélangé, liquide incompressible et gaz idéal. Ballottement/forme hydrostatique, équilibre des phases, cavitation, cartes pompe/vanne mesurées, calibration OEM et Unity Editor/Play/Player/IL2CPP réel restent ouverts. Paramètres `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.fr.md)
