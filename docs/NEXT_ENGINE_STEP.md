# Engine development resume notes

Development resumed at the owner's request on 2026-09-14. Standalone gas primitives and
the compiled Core network are now implemented. The 2026-09-22 integration checkpoint below updates the remaining scope.
The preceding sealed-cylinder baseline passed managed verification on Windows, macOS and
Linux. Actual Unity Editor and Player evidence is still pending; see [validation](VALIDATION.md).

**Subsequent checkpoint on 2026-09-14:** the [Core gas network](GAS_NETWORK.md) now
implements fixed-volume gas nodes, reservoir restrictions, controlled openings, thermal
links, channels, ledgers, forks and rollback. The bounded Heun method has analytic and
refinement evidence for smooth flow, with explicit wall coupling and a conservative
near-equilibrium limiter. It is not the proposed implicit method.

**2026-09-22 integration:** JSON/schema, asset v3, CLI/MCP discovery and examples,
portable replay and Unity schematic views are implemented. Pre-change v1/v2 readers
and fixtures are retained. Unity Editor/Play verification remains pending. The subsequent [moving-cylinder increment](MOVING_CYLINDER.md) adds gas exchange and
crank work through JSON, assets and agents. The subsequent [crank-angle timing increment](VALVE_TIMING.md) adds explicit 360/720-degree
profiles, reversal and bounded lobe resolution through the same interfaces. The latest
engine checkpoint includes [premixed combustion](PREMIXED_COMBUSTION.md), with
transported fuel/air/products, limiting reactants and chemical-energy accounting. Detailed
thermochemistry, fuel metering and ignition control remain engine work.

The Core network now provides explicit gas mass and internal-energy states. The moving-cylinder component now couples them to crank-dependent chambers with conservative pressure work. The present sealed cylinder derives gas state from angle and immutable initial entropy, which cannot represent gas exchange or wall heating. That component remains an analytic benchmark; finite chambers, reservoirs, gas cylinders, controlled orifices and gas-to-thermal links now coexist in the compiled graph.

Original increment contracts (use the checkpoint above to distinguish completed Core work from remaining integration):

- Gas restrictions reference stable gas node IDs; a future gas cylinder must define its moving-volume endpoint explicitly. A gas cylinder also connects to a rotational node; a heat link connects a finite gas volume to a thermal node. Support gas-only networks without requiring a dummy mechanical node.
- Track mass and internal energy independently, with positive finite state validation. Transfer mass and upstream enthalpy together. Track reservoir exchange in external mass and energy ledgers; internal transfers must cancel. Restrict connected gases to identical gas constant and gamma until composition/species mixing is implemented.
- Expose signed mass flow, gas state, heat flow, and mass residual channels. Orifice opening uses an explicit dimensionless fraction within `[0,1]`, with the same validation for direct and scheduled inputs.
- Investigate a bounded pairwise implicit transfer solve, bracketed by the connected pair's equal-pressure state. Couple gas-cylinder pressure work through the existing crank solve. A split method with backward-Euler flow would be first order for gas exchange; conservation alone does not prove accuracy. Validate this proposed method before adopting it.
- Preserve complete batch rollback, corrections in state hashes and forks, cancellation, immutable compiled models, and zero allocations during successful stepping. Solver failure must return actionable guidance.
- Extend JSON schemas, portable assets, capability discovery, examples, reports and Unity views together. If a new asset version is required, retain v1/v2 readers and test real pre-change fixtures. Preserve existing model fingerprints where solver semantics remain unchanged.

Required evidence includes choked and subcritical nozzle flow, analytic adiabatic vessel blowdown, reservoir filling enthalpy, closed-network mass/energy conservation, pressure equalization, gas/wall heat exchange, step refinement, reverse flow, closed-valve isolation, coupled crank work, malformed topology/input rejection, failed-batch recovery, branching, and complete CLI/MCP/asset replay.

Research starting points: [NASA mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) for ideal compressible nozzle flow, and [Cantera reactor interactions](https://www.cantera.org/stable/reference/reactors/interactions.html) for control-volume boundary and wall concepts. These references are not runtime dependencies or validation of Power!'s proposed solver.

An authentic pre-change cylinder v2 asset is now retained in the [compatibility fixtures](../tests/Power.Tests/Fixtures/README.md), with its source commit and SHA-256. The active tests verify its fingerprint and replay after upgrading to v3; do not regenerate it with the new encoder.

Complete engine behavior, predictive combustion/thermochemistry, mechanical valve-train dynamics, transmissions, controls and calibrated vehicle samples remain open. Preserve all sample evidence boundaries. The archived native prototypes have been ported to **Zig** under [the native boundary](NATIVE_ZIG.md); the active C#/Unity implementation remains in place.
