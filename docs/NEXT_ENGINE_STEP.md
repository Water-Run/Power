# Engine development resume notes

**English** · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Resume point

The tank resolves finite-volume vapor transfer with pressure-corrected chemical potentials and separated-liquid pickup.

[Finite-volume fuel tank equilibrium](TANK_PHASE_EQUILIBRIUM.md)

## Next increment

Next, resolve two-phase pump/rail flow and local inlet losses with explicit material properties and conservative volume, momentum and work coupling. Missing OEM measurements remain missing. Continue ignition, intake/exhaust, mechanical losses and ECU/TCU coordination separately.

## Acceptance

Require independent analytic or limiting references, full mass/energy ledgers and step refinement appropriate to the new equations. Preserve batch rollback, cancellation, forks, stable channels, portable replay and prior asset readers. Then continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control.

Coupled pump, heat and vent examples demonstrate first-order refinement. Both source phases remain inside the declared material temperature interval; total density stays below liquid density with a binary64 roundoff margin. Constant density/common gas properties, instantaneous equilibrium and a flat interface are research approximations. Real mixtures, surface tension, slosh, finite-rate kinetics, measured calibration and actual Unity Editor/Play/Player/IL2CPP remain unverified.

[DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) · [ROADMAP.md](ROADMAP.md) · [VALIDATION.md](VALIDATION.md)
