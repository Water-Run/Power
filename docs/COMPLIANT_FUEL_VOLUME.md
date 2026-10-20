# Compliant fuel volume reference

**English** · [简体中文](COMPLIANT_FUEL_VOLUME.zh-CN.md) · [Français](COMPLIANT_FUEL_VOLUME.fr.md) · [Русский](COMPLIANT_FUEL_VOLUME.ru.md) · [日本語](COMPLIANT_FUEL_VOLUME.ja.md) · [한국어](COMPLIANT_FUEL_VOLUME.ko.md) · [Deutsch](COMPLIANT_FUEL_VOLUME.de.md) · [Español](COMPLIANT_FUEL_VOLUME.es.md) · [Italiano](COMPLIANT_FUEL_VOLUME.it.md) · [Português](COMPLIANT_FUEL_VOLUME.pt-BR.md)

## Constitutive scope

`CompliantFuelVolume` supplies the constitutive closure used by the equilibrium fuel network. It resolves liquid, vapor and coexistence with elastic storage from total mass and temperature or stored energy. The pump-fed injector path remains separate until its coupling is extended. [Equilibrium fuel transport network](EQUILIBRIUM_FUEL_NETWORK.md)

## Thermodynamics

Liquid has constant density and heat capacity; vapor is an ideal pseudo-species. Both share the existing film material reference. The liquid chemical potential includes `p/rho_l`. Consequently `p_0(T)` is the negligible-displacement material reference, not the finite-density pure-fluid saturation pressure. The elastic wall has zero external pressure and stores pressure work once.

`StoredEnergyJoules` contains caloric and elastic energy; chemical heating-value energy remains a separate ledger when this closure is connected to the graph.

```text
V = V_0 + C p = m_l / rho_l + m_v R T / p
E = m_l (cl T + a) + m_v cv T + C p^2 / 2
ln(p / p_0(T)) = p / (rho_l R T)       [coexistence]
h = (U_fluid + p V) / M
dE = T dS + mu dM                    [fixed vessel parameters]
```

Coexistence selects the low-density vapor root. Its latent enthalpy is `L_u + R T - p/rho_l`; the pressure derivative satisfies the Clapeyron relation. At fixed mass the reported heat capacity differentiates fluid plus elastic energy, including phase transfer. The wall carries no entropy.

## Core API

`Evaluate` takes temperature and total mass. `Flash` takes total mass and fluid-plus-elastic stored energy. `AdvanceHeat` exchanges heat with a finite bath through a declared conductance and local duration in seconds, using backward Euler. SI units appear in property names. These local calls do not replace the bounded integer clock of `Simulation`.

`FuelVolumeStatus` distinguishes invalid input, temperature, mass and energy bounds, and solver failure; rejected outputs are default. `Flash` retains the requested energy and reports floating-point caloric reconstruction error as `CaloricResidualJoules`. Instances hold no evolving state and successful calls allocate no managed memory.

## Validity

Declare positive reference volume, compliance, liquid density and maximum wall pressure. The pressure limit covers the entire coexistence curve and stays below `rho_l R T_min`. The minimum-temperature coexistence wall expansion must be at least `128 * epsilon` of the reference volume, where `epsilon = 2^-52`. This excludes unresolved wall deformation and the dense-vapor branch outside the declared model; it does not predict a critical point.

Use the material temperature interval and `0 < M <= rho_l (V_0 + C p_max)`. Vacuum, carrier gas, real mixtures, thermal expansion, finite wall inertia, surface tension and finite-rate phase kinetics are outside this reference. Material and wall parameters remain `unverified`. Shaft-driven pump filling, injector handoff, resolved momentum and choking require further coupling and independent evidence.

## Independent evidence

Checks compare chemical potentials, Clapeyron slopes, caloric and entropy derivatives, and the Gibbs relation with elastic storage. An independent constrained entropy maximization does not use the saturation solve. Rigid-vessel, ideal-gas and extensive-scaling limits and finite-bath RK4 refinement provide independent references. Heat exchange conserves total energy and has first-order refinement with nondecreasing total entropy. This evidence does not establish measured fuel validity or Unity acceptance.

[NIST: pure-fluid equilibrium](https://pages.nist.gov/teqp-docs/en/main/algorithms/VLE.html) · [Cantera: constant-volume thermodynamics](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [VALIDATION.md](VALIDATION.md)
