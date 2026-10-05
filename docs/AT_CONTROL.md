# Hydraulic AT feedback

**English** · [简体中文](AT_CONTROL.zh-CN.md) · [Français](AT_CONTROL.fr.md) · [Русский](AT_CONTROL.ru.md) · [日本語](AT_CONTROL.ja.md) · [한국어](AT_CONTROL.ko.md) · [Deutsch](AT_CONTROL.de.md) · [Español](AT_CONTROL.es.md) · [Italiano](AT_CONTROL.it.md) · [Português](AT_CONTROL.pt-BR.md)

## Contract

`at_controller` accepts an integer requested range in [-1,4]; zero is neutral. It owns five fill/drain actuator pairs and optional converter lockup. Route order is carrier input, small-sun input, large-sun input, carrier brake, large-sun brake, then lockup.

Release is confirmed from actual pad force before a conflicting range applies. Bounded pressure PI uses measured chamber pressure. A range becomes active only when the required contacts and physical clutch locks are confirmed. Fractional requests and direct writes to owned valves return actionable errors.

Sampled phase, fault, pressure integrals and timing belong to the full transactional state. Cancellation, late failures and forks preserve the same histories. Faults cover release/apply timeout, low supply, direction change and loss of confirmed lock. A vent command cannot release a physically blocked drain.

Optional lockup uses forward-range, input-speed, slip and dwell limits with separate unlock hysteresis. The output describes actual Released/Applying/Locked/Releasing state. Lockup is a physical piston clutch, not a commanded speed equality.

## Evidence and limits

The examples `controlled-hydraulic-ravigneaux` and `controlled-fired-hydraulic-ravigneaux` use request channel 900 and controller ID 1400. They retain 99 and 122 reported states within the unchanged 128-state limit. Asset v25 retains routes, gains and clocks and reads v1-v24.

These are research controls and parameters remain `unverified`. Coordinated ECU torque blending, detailed sensors/valves, comprehensive vehicle faults and OEM calibration remain unfinished. Managed and Standard checks do not establish actual Unity Editor/Play/Player/IL2CPP acceptance.
