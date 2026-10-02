# Notas de reanudación del desarrollo del motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · **Español** · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

El desarrollo se reanudó a petición del propietario el 2026-09-14. Los primitivos de gas independientes y
la red compilada del núcleo están ahora implementados. El punto de control de integración del 2026-09-22, más abajo, actualiza el alcance restante.
La línea base anterior del cilindro cerrado pasó la verificación gestionada en Windows, macOS y
Linux. La evidencia real de Unity Editor y Player sigue pendiente; consulta [validación](VALIDATION.es.md).

**Punto de control posterior el 2026-09-14:** la [red de gas del núcleo](GAS_NETWORK.es.md) implementa ahora
nodos de gas de volumen fijo, restricciones de depósito, aperturas controladas, enlaces
térmicos, canales, libros, bifurcaciones y reversión. El método de Heun acotado tiene evidencia analítica y
de refinamiento para flujo suave, con acoplamiento de pared explícito y un limitador conservador
cerca del equilibrio. No es el método implícito propuesto.

**Integración del 2026-09-22:** están implementados el JSON y el esquema, el asset v3, el descubrimiento y los ejemplos de CLI/MCP,
la repetición portátil y las vistas esquemáticas de Unity. Se conservan los lectores
y fixtures v1/v2 anteriores al cambio. La verificación de Unity Editor/Play sigue pendiente. El [incremento posterior del cilindro móvil](MOVING_CYLINDER.es.md) añade intercambio de gas y
trabajo del cigüeñal a través de JSON, assets y agentes. El [incremento posterior de temporización por ángulo de cigüeñal](VALVE_TIMING.es.md) añade perfiles explícitos de 360/720 grados,
inversión y resolución de lóbulo acotada a través de las mismas interfaces. El último
punto de control del motor incluye la [combustión premmezclada](PREMIXED_COMBUSTION.es.md), con
combustible, aire y productos transportados, reactivos limitantes y contabilidad de energía química. La termoquímica
detallada, la dosificación de combustible y el control de encendido siguen siendo trabajo del motor.

La red del núcleo ofrece ahora estados explícitos de masa de gas y de energía interna. El componente de cilindro móvil los acopla ahora a cámaras dependientes del cigüeñal con trabajo de presión conservativo. El cilindro cerrado actual deriva el estado del gas del ángulo y de una entropía inicial inmutable, y no puede representar el intercambio de gas ni el calentamiento de pared. Ese componente sigue siendo una referencia analítica; las cámaras finitas, los depósitos, los cilindros de gas, los orificios controlados y los enlaces de gas a térmico coexisten ahora en el grafo compilado.

Contratos del incremento original (usa el punto de control de arriba para distinguir el trabajo del núcleo ya completado de la integración restante):

- Las restricciones de gas referencian IDs estables de nodo de gas; un futuro cilindro de gas debe definir de forma explícita su extremo de volumen móvil. Un cilindro de gas se conecta también a un nodo rotacional; un enlace térmico conecta un volumen de gas finito con un nodo térmico. Admite redes solo de gas sin exigir un nodo mecánico ficticio.
- Rastrea la masa y la energía interna por separado, con validación de estado positivo y finito. Transfiere juntas la masa y la entalpía aguas arriba. Rastrea el intercambio con el depósito en libros externos de masa y energía; las transferencias internas deben cancelarse. Restringe los gases conectados a la misma constante de gas y gamma hasta que se implemente la mezcla de composición o de especies.
- Expón canales de caudal másico con signo, estado del gas, flujo de calor y residuo de masa. La apertura del orificio usa una fracción adimensional explícita dentro de `[0,1]`, con la misma validación para entradas directas y programadas.
- Investiga una resolución implícita de transferencia por pares y acotada, delimitada por el estado de igual presión del par conectado. Acopla el trabajo de presión del cilindro de gas a través de la resolución de cigüeñal existente. Un método dividido con flujo de Euler hacia atrás sería de primer orden para el intercambio de gas; la conservación por sí sola no demuestra precisión. Valida este método propuesto antes de adoptarlo.
- Conserva la reversión completa del lote, las correcciones en los hashes de estado y las bifurcaciones, la cancelación, los modelos compilados inmutables y cero asignaciones durante un avance correcto. El fallo del solver debe devolver una guía accionable.
- Amplía a la vez los esquemas JSON, los assets portátiles, el descubrimiento de capacidades, los ejemplos, los informes y las vistas de Unity. Si hace falta una versión nueva de asset, conserva los lectores v1/v2 y prueba fixtures reales anteriores al cambio. Conserva las huellas de modelo existentes donde la semántica del solver no cambie.

La evidencia exigida incluye flujo de tobera bloqueado y subcrítico, vaciado adiabático analítico de un recipiente, entalpía de llenado del depósito, conservación de masa y energía en red cerrada, igualación de presión, intercambio de calor gas/pared, refinamiento del paso, flujo inverso, aislamiento con válvula cerrada, trabajo de cigüeñal acoplado, rechazo de topología o entrada mal formada, recuperación de lote fallido, bifurcación y repetición completa de CLI, MCP y asset.

Puntos de partida de la investigación: [bloqueo del caudal másico de la NASA](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) para el flujo ideal de tobera compresible, y [interacciones de reactores de Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) para los conceptos de frontera de volumen de control y de pared. Estas referencias no son dependencias de runtime ni una validación del solver propuesto de Power!.

Un asset auténtico de cilindro v2 anterior al cambio se conserva ahora en los [fixtures de compatibilidad](../tests/Power.Tests/Fixtures/README.md), con su commit de origen y SHA-256. Las pruebas activas verifican su huella y su repetición tras actualizarlo a v3; no lo regeneres con el codificador nuevo.

Siguen pendientes el comportamiento completo del motor, la combustión y la termoquímica predictivas, la dinámica mecánica de la distribución, las transmisiones, los controles y las muestras de vehículo calibradas. Conserva todos los límites de evidencia de las muestras. Los prototipos nativos archivados se han portado a **Zig** bajo [la frontera nativa](NATIVE_ZIG.es.md); la implementación activa de C#/Unity permanece en su sitio.
