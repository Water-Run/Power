# Chambres de carburant compressible entraînées par arbre

[English](COMPRESSIBLE_FUEL_CHAMBER.md) · [简体中文](COMPRESSIBLE_FUEL_CHAMBER.zh-CN.md) · **Français** · [Русский](COMPRESSIBLE_FUEL_CHAMBER.ru.md) · [日本語](COMPRESSIBLE_FUEL_CHAMBER.ja.md) · [한국어](COMPRESSIBLE_FUEL_CHAMBER.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_CHAMBER.de.md) · [Español](COMPRESSIBLE_FUEL_CHAMBER.es.md) · [Italiano](COMPRESSIBLE_FUEL_CHAMBER.it.md) · [Português](COMPRESSIBLE_FUEL_CHAMBER.pt-BR.md)

## Contrat physique

Un `compressible_fuel_chamber` relie un arbre à un réservoir fini `compressible_fuel`. Le mouvement change son volume ; le même travail de pression modifie l’énergie du fluide et le couple. Le réservoir peut alimenter ou recevoir un `equilibrium_fuel_duct`, avec vaporisation éclair et condensation à l’équilibre.

Le port `node_a` est rotatif et `node_b` possède une seule géométrie. Déclarer `minimum_reference_volume`, `swept_volume` et `phase` ; le volume initial du nœud doit correspondre à l’angle initial. Le réservoir mélangé a une vitesse globale nulle et ne peut être une cellule interne. Le mécanisme sinusoïdal sans masse n’ajoute aucune inertie de piston ou de came ; l’inertie d’arbre est explicite. La pression extérieure du carter est nulle.

## Travail et transport

Le travail fermé conserve masse et entropie. Énergie de compression froide et énergie calorique utilisent le potentiel d’équilibre du conduit. Le liquide seul utilise l’intégrale exacte de pression, la vapeur idéale son adiabatique analytique. Le changement de phase inverse l’entropie et vérifie l’état énergétique final. La différence acceptée produit le couple discret dans la résolution mécanique commune. Un `hydraulic_work` positif représente l’énergie de l’arbre vers le fluide.

```text
V(theta) = Vmin + Vs sin^2((theta-phase)/2)
dE_fluid = -p dV; dM = 0; dS = 0
tau_pressure = -Delta_E_fluid / Delta_theta
tau_pressure(Delta_theta=0) = p dV/dtheta
Delta_E_shaft + Delta_E_fluid = 0
p_liquid = K (M/(rho0 V)-1)
Delta_E_liquid = K [Delta_V - (M/rho0) ln(V_next/V_old)]
T_vapor_next = T_old (V_old/V_next)^(R/cv)
Delta_E_vapor = M cv (T_next-T_old)
```

Le réservoir mobile utilise un demi-transport, le travail complet de l’arbre, puis un demi-transport. Le transport reste du premier ordre avec diffusion numérique ; la séparation symétrique n’augmente pas son ordre. Le mouvement liquide fermé et lisse converge au second ordre. L’incrément angulaire est limité à 0.25 rad par intervalle mécanique. CFL, limites matérielles, restauration complète, annulation et branches indépendantes s’appliquent. L’impulsion stationnaire des ports et du carter est distincte du travail du piston.

## Utilisation et preuves

Exécuter `compressible-fuel-chamber-duct` ou `electric-compressible-fuel-duct`. Lire masse, volume, pression, température, entropie et fraction de vapeur avec les cellules et le couple, la puissance et le travail. Les vérifications utilisent travail liquide et limites de vapeur indépendants, inversion séparée de l’isentrope et quadrature de pression, EDO d’arbre liquide et raffinement RK4 des équations semi-discrètes de vapeur idéale. Conservation, composition électrique/engrenages/hydraulique/embrayage, relecture portable, MCP réel et transactions sans allocation sont vérifiés séparément. Les résultats figurent dans les preuves de validation.

[VALIDATION.fr.md](VALIDATION.fr.md)

## Validité et travaux restants

Les paramètres restent `unverified`. Le pseudo-fluide pur à l’équilibre comprend un liquide compressible sans dilatation thermique et une vapeur idéale, avec chambre mélangée et cellules rigides. Respecter les limites thermiques et de pression. La chambre ne résout pas d’énergie cinétique globale et n’inclut pas de clapet automatique. Nucléation à vitesse finie, glissement, comportement critique réel, étalonnage mesuré et acceptation Unity restent ouverts.

Ajouter des frontières compatibles de distributeur et d’aiguille/buse compressibles, avec enthalpie, énergie cinétique et hypothèses de phase explicites. Poursuivre relaxation/glissement, allumage, admission/échappement, pertes mécaniques et coordination ECU/TCU. Étalonnage mesuré et acceptation Unity réelle restent distincts.

[Modelica: swept volume and pressure work](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [NIST: consistent thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html)
