# Engine development resume notes

Development is paused at the owner's request on 2026-09-08. Resume implementation only after an explicit owner instruction. These notes describe proposed work, not current capabilities.

The published executable baseline is code commit `6209df2`: sealed adiabatic cylinders, crank-slider geometry, conservative crank coupling, asset v2, CLI/MCP experiments and a Unity piston view. Windows, macOS and Linux passed managed verification; actual Unity Editor and Player evidence is still pending. See [validation](VALIDATION.md).

The next engine increment should introduce explicit gas mass and internal-energy states. The present sealed cylinder derives gas state from angle and immutable initial entropy, which cannot represent gas exchange or wall heating. Retain that component as an analytic benchmark while adding finite chambers, fixed pressure/temperature reservoirs, gas cylinders, controlled orifices and gas-to-thermal-node heat links.

Proposed contracts:

- Gas endpoints reference stable component IDs. A gas cylinder also connects to a rotational node; a heat link connects a finite gas volume to a thermal node. Support gas-only networks without requiring a dummy mechanical node.
- Track mass and internal energy independently, with positive finite state validation. Transfer mass and upstream enthalpy together. Track reservoir exchange in external mass and energy ledgers; internal transfers must cancel. Restrict connected gases to identical gas constant and gamma until composition/species mixing is implemented.
- Expose signed mass flow, gas state, heat flow, and mass residual channels. Orifice opening uses an explicit dimensionless fraction within `[0,1]`, with the same validation for direct and scheduled inputs.
- Investigate a bounded pairwise implicit transfer solve, bracketed by the connected pair's equal-pressure state. Couple gas-cylinder pressure work through the existing crank solve. A split method with backward-Euler flow would be first order for gas exchange; conservation alone does not prove accuracy. Validate this proposed method before adopting it.
- Preserve complete batch rollback, corrections in state hashes and forks, cancellation, immutable compiled models, and zero allocations during successful stepping. Solver failure must return actionable guidance.
- Extend JSON schemas, portable assets, capability discovery, examples, reports and Unity views together. If a new asset version is required, retain v1/v2 readers and test real pre-change fixtures. Preserve existing model fingerprints where solver semantics remain unchanged.

Required evidence includes choked and subcritical nozzle flow, analytic adiabatic vessel blowdown, reservoir filling enthalpy, closed-network mass/energy conservation, pressure equalization, gas/wall heat exchange, step refinement, reverse flow, closed-valve isolation, coupled crank work, malformed topology/input rejection, failed-batch recovery, branching, and complete CLI/MCP/asset replay.

Research starting points: [NASA mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) for ideal compressible nozzle flow, and [Cantera reactor interactions](https://www.cantera.org/stable/reference/reactors/interactions.html) for control-volume boundary and wall concepts. These references are not runtime dependencies or validation of Power!'s proposed solver.

A pre-change cylinder v2 asset was saved locally under `artifacts/drafts/gas-exchange/sealed-cylinder-v2.powerasset` for a future compatibility test. It is outside the active test suite and is not published as new functionality. It can be reproduced from the verified baseline before changing the encoder.

The full engine cycle, combustion, valve timing, transmissions, controls and calibrated vehicle samples remain open. Preserve all sample evidence boundaries. The archived native prototypes have been ported to **Zig** under [the native boundary](NATIVE_ZIG.md); the active C#/Unity implementation remains in place.
