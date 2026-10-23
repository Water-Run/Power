# Conduites de carburant compressible à l’équilibre

[English](EQUILIBRIUM_FUEL_DUCT.md) · [简体中文](EQUILIBRIUM_FUEL_DUCT.zh-CN.md) · **Français** · [Русский](EQUILIBRIUM_FUEL_DUCT.ru.md) · [日本語](EQUILIBRIUM_FUEL_DUCT.ja.md) · [한국어](EQUILIBRIUM_FUEL_DUCT.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_DUCT.de.md) · [Español](EQUILIBRIUM_FUEL_DUCT.es.md) · [Italiano](EQUILIBRIUM_FUEL_DUCT.it.md) · [Português](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

## Modèle et matériau

Un `equilibrium_fuel_duct` transporte masse, quantité de mouvement et énergie totale dans des cellules explicites `compressible_fuel`. Le liquide est compressible et la vapeur idéale ; les phases partagent pression, température et vitesse. L’équilibre local permet condensation et vaporisation éclair. Deux réservoirs stationnaires finis et une paroi thermique finie ferment les bilans.

Le liquide possède une dilatation thermique nulle et une capacité thermique constante. `K` est son module volumique à pression nulle ; le module réel vaut `K+p`. Énergie de compression froide, potentiels chimiques, entropie et vitesse du son à l’équilibre dérivent du même potentiel. `a` est le décalage énergétique liquide existant, `p0(T)` la courbe de saturation négligeant le volume liquide, `x` la fraction massique de vapeur et `Cv_eq` la dérivée de l’énergie interne du mélange à volume spécifique constant. `Tr`, `Pref` et `Lu_ref` sont la température, la pression et l’énergie interne latente de référence. Les unités sont SI.

```text
p_l = K (rho_l/rho0 - 1); y = p/K
v_l = 1/[rho0 (1+y)]
u_cold = (K/rho0) [ln(1+y) - y/(1+y)]
u_l = cl T + a + u_cold
h_l = cl T + a + (K/rho0) ln(1+y)
s_l = cl ln(T/Tr)
u_v = cv T; v_v = R T/p; h_v = (cv+R) T
s_v = (cv+R) ln(T/Tr) - R ln(p/Pref) + Lu_ref/Tr + R
R T ln[p_sat/p0(T)] = (K/rho0) ln(1+p_sat/K)
v = (1-x) v_l + x v_v; u = (1-x) u_l + x u_v
dp_sat/dT = (h_v-h_l)/[T(v_v-v_l)]
c_eq^2 = v^2 T (dp_sat/dT)^2 / Cv_eq
c_l^2 = K/rho0; c_v^2 = (1+R/cv) R T
```

## Transport et frontières

Déclarer `liquid_bulk_modulus`, omettre `compliance` et fournir explicitement `initial_velocity` sur chaque nœud. Déclarer 1–24 `cells` internes ordonnées et distinctes, chacune appartenant à une seule conduite. La longueur est le volume divisé par `area`. Les réservoirs ont une vitesse nulle et ne peuvent être des cellules internes. Tous les ports partagent le matériau. `linear_resistance` et `quadratic_resistance` décrivent la résistance totale, répartie par volume ; `fluid_heat_fraction` répartit la chaleur de frottement entre fluide et paroi. Aucune entrée directe d’ouverture.

```text
U = [rho, rho velocity, rho (u + velocity^2/2)]
F = [rho velocity, rho velocity^2+p, (rho (u+velocity^2/2)+p) velocity]
F_Rusanov = (F_left+F_right)/2 - a_max (U_right-U_left)/2
a_max = max_ports(abs(velocity)) + sqrt(max(K/rho0, (1+R/cv) R Tmax))
```

Le flux conservatif de Rusanov avance toutes les faces depuis le même état de sous-pas. Sa borne de vitesse combine la plus grande vitesse absolue et la borne acoustique du matériau. La limite CFL vaut 0.2 fois le volume minimal des ports divisé par section et vitesse ; chaque intervalle autorise au plus 4096 sous-pas. Transport et décroissance locale exacte par frottement sont séparés au premier ordre. La diffusion numérique contribue à la variation totale d’entropie, distincte de la chaleur de frottement rapportée. Les frontières stationnaires absorbent la quantité de mouvement sans travail mécanique ; l’énergie cinétique entrante se thermalise dans les réservoirs finis. Un état invalide ou une baisse d’entropie dépassant l’arrondi rejette tout le lot. Réduire le pas et examiner les limites du matériau et la résolution.

## Utilisation et preuves

Exécuter `equilibrium-fuel-condensing-duct` ou `equilibrium-fuel-flashing-duct`. Chaque cellule expose densité, vitesse, fraction de phase, vitesse du son, température et énergie. Masse et énergie aux ports, impulsion de paroi et inventaires complets sont observables. Les vérifications indépendantes couvrent travail de compression, limite de vapeur idéale, égalité des potentiels chimiques, relations de Clapeyron et Gibbs, vitesse acoustique par inversion d’entropie, inversion masse-énergie, conservation et frottement avec paroi finie. Les erreurs acoustiques moyennes diminuent sur 8, 16 et 24 cellules. Relecture exacte, annulation, restauration, branches, pas sans allocation, assets portables et MCP réel sont vérifiés séparément ; les résultats détaillés figurent dans les preuves de validation.

[VALIDATION.fr.md](VALIDATION.fr.md)

## Validité et suite

Les paramètres restent `unverified`. Respecter les bornes de température et pression, une enthalpie latente positive, des volumes de coexistence séparés et une vapeur diluée. La conduite est horizontale, rigide, de section constante et à équilibre homogène instantané. Nucléation à vitesse finie, glissement, tension superficielle, gravité, élasticité et comportement critique réel sont omis. La diffusion du premier ordre peut étaler les fronts ; conservation et relecture ne prouvent ni précision spatiale ni validité matérielle mesurée. L’acceptation Unity réelle reste à réaliser.

Relier les conduites compressibles à des chambres entraînées par arbre et à des frontières aiguille/buse compatibles, avec travail de pression et hypothèses de phase explicites. Poursuivre relaxation/glissement, allumage, admission/échappement, pertes mécaniques et coordination ECU/TCU. Étalonnage mesuré et acceptation Unity réelle restent distincts.

[NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf) · [MFEM: Rusanov flux](https://docs.mfem.org/html/classmfem_1_1RusanovFlux.html) · [Modelica: conservation balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html)
