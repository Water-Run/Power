# Buses de carburant compressible à qualité figée

[English](COMPRESSIBLE_FUEL_NOZZLE.md) · [简体中文](COMPRESSIBLE_FUEL_NOZZLE.zh-CN.md) · **Français** · [Русский](COMPRESSIBLE_FUEL_NOZZLE.ru.md) · [日本語](COMPRESSIBLE_FUEL_NOZZLE.ja.md) · [한국어](COMPRESSIBLE_FUEL_NOZZLE.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_NOZZLE.de.md) · [Español](COMPRESSIBLE_FUEL_NOZZLE.es.md) · [Italiano](COMPRESSIBLE_FUEL_NOZZLE.it.md) · [Português](COMPRESSIBLE_FUEL_NOZZLE.pt-BR.md)

## Contrat physique

Un `compressible_fuel_nozzle` transporte le fluide dans un seul sens entre réservoirs finis `compressible_fuel`, y compris les chambres entraînées par arbre. Compression liquide et vapeur idéale partagent leur matériau. Déclarer `phase_mode` comme `frozen` : la qualité massique reste fixe pendant le transit, puis les réservoirs se rééquilibrent.

Déclarer section, coefficient de section effective et `wall_kinetic_fraction`, avec une paroi thermique finie. `initial_input` est l’ouverture dans [0,1], modifiable par un canal optionnel. `needle_node`, `closed_position` et `full_open_position` optionnels multiplient la commande par l’ouverture réelle de l’aiguille en translation. Les ports ont une vitesse globale nulle, les mêmes matériau et pouvoir calorifique, et ne sont pas des cellules internes. Une pression inverse ferme cette frontière idéale sans seuil d’ouverture.

## Énergie et blocage sonique

Les phases figées partagent pression, température et vitesse ; le transfert massique entre phases est supprimé. Entropie et compression froide donnent la chute d’enthalpie isentropique. Le maximum unique de flux utilise le son figé, distinct du son d’équilibre. Le coefficient réduit la section effective. En régime critique, l’énergie de sortie décrit un jet idéal complètement détendu à la pression réceptrice. L’enthalpie d’arrêt alimente une seule fois récepteur et chaleur explicite de paroi. L’impulsion décrit la thermalisation stationnaire, pas la poussée totale du carter ni la force axiale de l’aiguille.

```text
cp_f = (1-x) cl + x (cv+R); alpha = x R/cp_f
T(p) = T0 (p/p0)^alpha
v(p) = (1-x)/[rho0 (1+p/K)] + x R T(p)/p
Delta_s = cp_f ln(T/T0) - x R ln(p/p0) = 0
D(p) = h0-h(p)
     = cp_f T0 [1-(p/p0)^alpha] + (1-x) K/rho0 ln[(K+p0)/(K+p)]
G(p) = sqrt(2 D(p))/v(p)
c_frozen^2 = -v(p)^2 / (dv/dp at fixed s,x)
F(p) = v(p)^2 + 2 D(p) v'(p); F'(p) = 2 D(p) v''(p) > 0
G_throat = max(G(p), p_receiver <= p <= p0)
mass_flow = opening Cd A G_throat
Delta_E_A = -Delta_m h0
Delta_E_B = Delta_m h0 - xi Delta_m D(p_receiver)
Delta_E_wall = xi Delta_m D(p_receiver)
```

Des sous-pas conservatifs du premier ordre échangent la même masse et enthalpie ; l’énergie cinétique du jet se répartit entre récepteur et paroi. Chaque acceptation vérifie les états et l’entropie totale de la paire. Limites : 4096 sous-pas, 64 réductions de moitié par candidat, transfert initial borné à 0.1 fois la plus petite masse. La fermeture tolère 128 fois epsilon binary64 multiplié par le plus grand module volumique ou la pression des ports, sans écrêter les pressions. Un échec restaure tout le lot. Réduire le pas et examiner inventaire, limites, course et section.

## Utilisation et preuves

Exécuter `compressible-fuel-nozzle` pour la décharge finie ou `compressible-pump-needle` pour conduite, arbre, admission unidirectionnelle et aiguille électromagnétique. Lire masse, enthalpie, énergie cinétique, chaleur de paroi, impulsion et entropie cumulées. Débit et force sont moyennés sur le tick entier ; gorge, sortie et qualité gardent le dernier transit accepté. Les comparaisons couvrent travail liquide exact, blocage analytique gaz/liquide, quadrature indépendante, entropie et dérivée acoustique figées, balayage dense et raffinement de vidange critique analytique. Conservation, mouvement réel, relecture, branches, restauration, annulation, allocations, assets et MCP sont vérifiés séparément.

[VALIDATION.fr.md](VALIDATION.fr.md)

## Validité et suite

Les paramètres restent `unverified`. Le pseudo-fluide pur associe vapeur idéale et liquide compressible sans dilatation thermique. Respecter pression et température déclarées, y compris celles du jet idéal. Le transit figé peut conserver du liquide surchauffé sans prédire une limite mesurée de nucléation ou métastabilité. Relaxation finie, glissement, pulvérisation résolue, gaz porteur non condensable, récepteur avec combustion, cartes mesurées et acceptation Unity réelle restent ouverts. Conservation et relecture ne prouvent pas la validité matérielle.

Ajouter une relaxation de phase à vitesse finie avec preuves explicites de temps de séjour et de matériau, puis pulvérisation résolue et récepteur avec combustion compatible. Continuer allumage, admission/échappement, pertes mécaniques, actionnement de transmission et ECU/TCU. Étalonnage mesuré et acceptation Unity restent distincts.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
