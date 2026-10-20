# Finite-volume fuel tank equilibrium

**English** · [简体中文](TANK_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](TANK_PHASE_EQUILIBRIUM.fr.md) · [Русский](TANK_PHASE_EQUILIBRIUM.ru.md) · [日本語](TANK_PHASE_EQUILIBRIUM.ja.md) · [한국어](TANK_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](TANK_PHASE_EQUILIBRIUM.de.md) · [Español](TANK_PHASE_EQUILIBRIUM.es.md) · [Italiano](TANK_PHASE_EQUILIBRIUM.it.md) · [Português](TANK_PHASE_EQUILIBRIUM.pt-BR.md)

## Contract

Set `liquid_fuel_tank.parameters.headspace.phase_equilibrium` to `true` to resolve tank evaporation and condensation. The paired film supplies explicit `phase_equilibrium` material properties; the tracked headspace shares its R, cv, heating value and stoichiometric ratio. Declare a common initial gas/liquid temperature and saturated fuel partial pressure for a wet tank; a dry tank starts at or below saturation. Its total inventory density stays below liquid density.

Tank `evaporated_fuel_mass`, `condensed_fuel_mass` and signed `mass_flow` count net phase transfer per complete physical tick. Read `vapor_partial_pressure`, pressure-corrected `saturation_pressure`, liquid inventory and gas volume together. The pump picks up separated liquid only; two-phase pump/rail transport, entrainment and local NPSH remain open.

## Thermodynamic reference

The rigid vessel contains incompressible liquid and an ideal pseudo-species/carrier gas. Liquid volume changes the gas volume. The liquid chemical potential includes `p/rho`, so the receiver's negligible-displacement saturation reference `p_0(T)` receives a pressure correction. `reference_pressure` retains that material-reference meaning; it is not silently reinterpreted as the finite-density pure-fluid saturation pressure.

[Fuel phase equilibrium and liquid supply limits](FUEL_PHASE_EQUILIBRIUM.md)

```text
M_f = m_l + m_v
V_g = V_tank - m_l / rho_l
p = (M_c + m_v) R T / V_g
p_v = m_v R T / V_g
ln(p_v / p_0(T)) = p / (rho_l R T)
U = (M_c + m_v) cv T + m_l (cl T + a)
```

A bounded constant-U,V flash conserves total fuel and thermal energy. Explicit gas orifices carry vapor, carrier gas and chemical energy; gas heat links exchange heat with finite thermal nodes. The pump/return solve exchanges displacement work with the gas, then phase equilibration redistributes internal energy. Its `hydraulic_work` channel records pump-displacement work, excluding internal phase-change work.

## Evidence and limits

`equilibrium-tank-supply` exercises pump reversal, fuel-bearing venting and a finite heated wall. `equilibrium-tank-fired-cylinder` adds the same source to a fired engine. Independent entropy maximization, chemical-potential and caloric-derivative checks, coupled ODE refinement, complete ledgers, replay, rollback and allocation checks cover the implementation. Asset v32 retains the flag and reads v1-v31.

Coupled pump, heat and vent examples demonstrate first-order refinement. Both source phases remain inside the declared material temperature interval; total density stays below liquid density with a binary64 roundoff margin. Constant density/common gas properties, instantaneous equilibrium and a flat interface are research approximations. Real mixtures, surface tension, slosh, finite-rate kinetics, measured calibration and actual Unity Editor/Play/Player/IL2CPP remain unverified.

[Cantera: constant-volume species](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [IUPAC: Poynting correction](https://goldbook.iupac.org/terms/view/15347) · [VALIDATION.md](VALIDATION.md)
