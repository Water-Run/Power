# Décharge de carburant à titre figé et récepteurs finis

[English](FUEL_DISCHARGE.md) · [简体中文](FUEL_DISCHARGE.zh-CN.md) · **Français** · [Русский](FUEL_DISCHARGE.ru.md) · [日本語](FUEL_DISCHARGE.ja.md) · [한국어](FUEL_DISCHARGE.ko.md) · [Deutsch](FUEL_DISCHARGE.de.md) · [Español](FUEL_DISCHARGE.es.md) · [Italiano](FUEL_DISCHARGE.it.md) · [Português](FUEL_DISCHARGE.pt-BR.md)

## Modèle physique

Un `fuel_discharge` relie une source finie du domaine carburant au nœud gazeux d'un `finite_fuel_film`. Déclarer `phase_mode` à `frozen` : le titre massique vapeur reste figé dans la tuyère, tandis que les deux volumes retrouvent leur équilibre. Les phases homogènes partagent température et vitesse ; le liquide est incompressible et la vapeur une pseudo-espèce idéale. Propriétés et masse volumique doivent correspondre.

Pour la pression et la température d'arrêt amont, maximiser ce flux massique entre pression réceptrice et pression amont. L'unique maximum intérieur limite le débit étranglé. Les limites liquide pur et vapeur pure donnent Bernoulli et la relation sonique du gaz idéal. Le coefficient de débit module l'aire efficace du col.

```text
x = upstream vapor mass fraction; r = p/p0
cp = (1-x) cl + x (cv+R); k = x R/cp; b = (1-x)/rho_l
T(p) = T0 r^k
h0-h(p) = cp T0 (1-r^k) + b p0 (1-r)
v(p) = b + x R T(p)/p
G(p) = sqrt(2 (h0-h(p))) / v(p)
mass_rate = opening Cd A max(G)
jet_kinetic_energy_per_kg = h0-h(p_receiver)
gas_volume = cavity_volume - liquid_mass/rho_l
```

## Énergie, volume et quantité de mouvement

La source perd l'enthalpie d'arrêt transportée. `wall_kinetic_fraction` affecte une part de l'énergie cinétique du jet idéal complètement détendu à la paroi finie immobile ; le reste entre dans le fluide. Cette vitesse de référence suppose une détente idéale jusqu'à la pression réceptrice, sans prédire une vitesse de sortie mesurée. L'impulsion atteint le récepteur immobile, avec réaction opposée du carter ; ces réactions ne travaillent pas et n'ajoutent aucune force au piston ou à l'aiguille.

Déclarer `finite_volume_density`. Le `volume` gazeux est celui de la cavité moins masse liquide/masse volumique ; le `volume` du film est le déplacement liquide. Le cylindre mobile applique un travail à masse et entropie constantes au récepteur entier. Ce travail reste interne. Cavités fixes et cylindres à manivelle sont pris en charge ; un piston gazeux linéaire ne peut posséder ce récepteur.

## Utilisation et preuves

Exécuter `finite-volume-fuel-discharge` pour l'étranglement d'une source diphasique, ou `pump-fed-finite-volume-cylinder` pour pompe électrique, aiguille électromagnétique physique et combustion prescrite. `needle` et `valve_timing` multiplient l'ouverture. Des impulsions de tension explicites commandent l'exemple, sans masse demandée par cycle ni régulateur de dose échantillonné. Lire `jet_kinetic_energy`, `jet_impulse`, `force`, `throat_pressure`, `exit_temperature`, `exit_velocity` et `choked` avec les inventaires complets. Les quatre dernières valeurs conservent le dernier état accepté en écoulement à l'arrêt du débit.

Limites indépendantes de Bernoulli et du gaz idéal, maximisation séparée du flux diphasique, bilans d'énergie/entropie et ODE indépendante tuyère/récepteur vérifient les équations. Le raffinement temporel est du premier ordre. Relecture portable, MCP réel, annulation, rollback, branches indépendantes et allocations couvrent l'intégration. Unity Editor/Play/Player/IL2CPP réel reste non vérifié.

## Validité et suite

Les paramètres restent `unverified`. Respecter températures déclarées, volume gazeux positif et domaine vapeur diluée. Inertie locale d'entrée, nucléation à vitesse finie, glissement, spray résolu, comportement critique réel et calibration mesurée sont omis. La combustion prescrite ne valide pas l'équilibre chimique ni l'entropie réactionnelle. Poursuivre entrée, allumage, admission/échappement, pertes et coordination ECU/TCU.

[NASA: ideal-gas mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) · [NASA-CR-64098: two-phase mass-limiting experiments](https://ntrs.nasa.gov/citations/19650020869) · [VALIDATION.md](VALIDATION.md)
