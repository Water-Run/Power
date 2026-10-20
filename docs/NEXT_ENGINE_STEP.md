# Engine development resume notes

**English** · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Resume point

Fuel-domain vessels now exchange liquid/vapor mixtures through conservative dissipative restrictions and finite-wall heat links. Pump filling, shaft coupling and injector discharge remain to be connected.

[Equilibrium fuel transport network](EQUILIBRIUM_FUEL_NETWORK.md)

## Next increment

Connect fuel-domain volumes to shaft-driven pump filling and physical injector discharge, with conservative material, momentum and work coupling. Resolve local inlet inertia and choking with independent references. Continue ignition, intake/exhaust, mechanical losses, transmission actuation and ECU/TCU coordination without treating this network as a complete powertrain.

## Acceptance

Require independent analytic or limiting references, full mass/energy ledgers and step refinement appropriate to the new equations. Preserve batch rollback, cancellation, forks, stable channels, portable replay and prior asset readers. Then continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control.

The material and representable wall-deformation limits of the constitutive closure still apply. Parameters remain `unverified`. Resolved pump filling and shaft work, injector handoff, local fluid inertia, sonic choking, nucleation delay, phase slip and real-mixture measurements remain separate work. Existing pump-fed injector rails retain their separated-liquid path until those couplings are implemented.

[DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) · [ROADMAP.md](ROADMAP.md) · [VALIDATION.md](VALIDATION.md)
