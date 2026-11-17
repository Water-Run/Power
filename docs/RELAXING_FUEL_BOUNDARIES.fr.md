# Pompes à carburant relaxant et dosage à phase gelée

[English](RELAXING_FUEL_BOUNDARIES.md) · [简体中文](RELAXING_FUEL_BOUNDARIES.zh-CN.md) · **Français** · [Русский](RELAXING_FUEL_BOUNDARIES.ru.md) · [日本語](RELAXING_FUEL_BOUNDARIES.ja.md) · [한국어](RELAXING_FUEL_BOUNDARIES.ko.md) · [Deutsch](RELAXING_FUEL_BOUNDARIES.de.md) · [Español](RELAXING_FUEL_BOUNDARIES.es.md) · [Italiano](RELAXING_FUEL_BOUNDARIES.it.md) · [Português](RELAXING_FUEL_BOUNDARIES.pt-BR.md)

## Modèle physique

Un `compressible_fuel_chamber` peut imposer le volume d’un réservoir `relaxing_fuel`. Le travail réversible de l’arbre conserve le titre massique de vapeur ; la relaxation chimique est une source séparée conservant l’énergie. Un `compressible_fuel_nozzle` relie deux réservoirs relaxants avec `phase_mode` réglé sur `frozen`, transportant masse totale, vapeur et enthalpie d’arrêt. Bobine, ressort et butée réels peuvent régler l’ouverture par une aiguille physique.

[NASA: isentropic ideal-gas relations](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Couplage et calcul numérique

La relation d’entropie à titre fixe détermine pression et température après variation de volume. Le travail inclut l’énergie froide de compression liquide et utilise des fonctions incrémentales stables pour les petits mouvements. Le même travail discret produit le couple d’arbre. Chaque intervalle global applique une fois la source de chaque nœud autour du transport conservatif par conduit et buse gelée. Le CFL et une fraction de 0.02 du plus petit inventaire de port bornent les intervalles, limités à 4096. Travail mécanique et source restent séparés. Le transport couplé est du premier ordre et exige un raffinement temporel.

```text
x = vapor_mass / total_mass
cp_x = (1-x) c_liquid + x (c_vapor+R)
cv_x = (1-x) c_liquid + x c_vapor
alpha = x R/cp_x
T/T0 = (p/p0)^alpha                         (fixed quality and entropy)
v(p) = (1-x)/(rho0 (1+p/K)) + x R T/p
work_on_fluid = mass [cv_x T0 expm1(alpha ln(p/p0)) + (1-x) delta_u_cold]
fluid_torque = -work_on_fluid/delta_angle   (nonzero angular increment)
delta_vapor_mass_volume_work = 0
delta_vapor_mass_nozzle = transferred_total_mass * source_vapor_quality
donor_delta_energy = -transferred_total_mass * stagnation_enthalpy
receiver_delta_energy = -donor_delta_energy - wall_fraction * jet_kinetic_energy
dx/dt_phase_source = (x_equilibrium(rho,u)-x)/tau
```

## Utilisation, preuves et limites

Exécuter `relaxing-fuel-chamber` ou `relaxing-pump-needle`. Observer volume et travail, titres réel et d’équilibre, entropie de phase, `total_vapor_delivered`, inventaires des ports et résidu global de vapeur. Les essais comparent adiabatiques pures, quadrature de pression indépendante à titre fixe, travail infinitésimal, cycle réversible et ODE indépendante d’arbre à vapeur idéale. Raffinement couplé, temps de source des réservoirs partagés, bilans complets, annulation, retour arrière, branches, allocations, rejeu portable et MCP réel sont vérifiés séparément. Les valeurs figurent dans les preuves de validation.

Les paramètres restent `unverified`. Les deux ports doivent avoir le même domaine et matériau ; les ports relaxants rejettent le transit à l’équilibre. Une relaxation finie dans les réservoirs ne résout ni conversion chimique ni séjour dans une buse de volume nul. Le modèle omet glissement, tension liquide négative, barrière de nucléation, spray, gaz porteur et récepteur en combustion. Les réservoirs mobiles sont mélangés sans vitesse moyenne ; les cellules du conduit restent rigides avec un propriétaire unique. Utiliser une vapeur idéale diluée loin du critique, dans les bornes déclarées de pression positive et température, avec enthalpie latente positive et volumes de coexistence distincts. Le titre initial est une entrée de recherche explicite. L’accord numérique n’établit ni étalonnage mesuré ni acceptation Unity réelle.

Résoudre le séjour à vitesse finie dans la buse avec des preuves matériau, puis ajouter spray résolu et récepteurs en combustion compatibles. Poursuivre allumage, admission/échappement, pertes mécaniques, actionnement de transmission et coordination ECU/TCU. Étalonnage mesuré et acceptation Unity restent distincts.

[VALIDATION.fr.md](VALIDATION.fr.md)
