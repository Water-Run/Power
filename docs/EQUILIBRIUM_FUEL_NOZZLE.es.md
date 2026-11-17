# Tránsito de combustible en equilibrio por toberas

[English](EQUILIBRIUM_FUEL_NOZZLE.md) · [简体中文](EQUILIBRIUM_FUEL_NOZZLE.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NOZZLE.fr.md) · [Русский](EQUILIBRIUM_FUEL_NOZZLE.ru.md) · [日本語](EQUILIBRIUM_FUEL_NOZZLE.ja.md) · [한국어](EQUILIBRIUM_FUEL_NOZZLE.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NOZZLE.de.md) · **Español** · [Italiano](EQUILIBRIUM_FUEL_NOZZLE.it.md) · [Português](EQUILIBRIUM_FUEL_NOZZLE.pt-BR.md)

## Modelo físico

Defina `phase_mode` de `compressible_fuel_nozzle` como `equilibrium` para equilibrio homogéneo instantáneo durante el tránsito. Líquido y vapor comparten presión, temperatura y velocidad; la calidad cambia sobre la isentrópica. Tobera y depósitos finitos usan el mismo potencial líquido compresible y referencia de vapor ideal. `frozen` sigue siendo una hipótesis independiente admitida.

Presión y entropía específica determinan líquido estable, vapor o coexistencia. El cierre usa igualdad de potenciales químicos y límites térmicos/barométricos existentes. Los extremos térmicos solo admiten vecindades de redondeo binary64. Área, calor de pared, apertura y aguja real siguen el contrato común. El chorro se expande idealmente hasta el receptor; su impulso estacionario no añade fuerza axial ni trabajo mecánico.

## Envolvente global de flujo

La velocidad del sonido de equilibrio al cuadrado positiva implica densidad creciente con presión a entropía fija; la caída de entalpía disminuye. Estos factores monótonos acotan el flujo en intervalos, incluidas fronteras de fase y varios extremos. Ramificación y acotación devuelve cota inferior muestreada y superior restante con ancho relativo máximo 1e-4. Límites: 512 intervalos en pila y 8192 divisiones. Diferencias calóricas usan márgenes de redondeo y cotas integrales monótonas con hasta 256 paneles Riemann y 80 iteraciones. Lo no resuelto rechaza el lote. Un máximo interior suave es sónico; uno de frontera puede no ser suave.

```text
g_liquid(T,p) = g_vapor(T,p)
s(p,rho,T,x) = s_source
D(p) = h_source - h(p,s_source) = integral(p,p_source) v(q,s_source) dq
G(p) = rho(p,s_source) sqrt(2 D(p))
drho/dp at fixed s = 1/c_equilibrium^2 > 0
G_upper([p_low,p_high]) = rho(p_high) sqrt(2 D_upper(p_low))
G_lower = max(sampled rho(p) sqrt(2 D_lower(p)))
flux_relative_uncertainty = G_upper/G_lower - 1 <= 1e-4
mass_flow_local = opening Cd A G_lower
velocity_throat = c_equilibrium  (smooth interior maximum only)
```

Subpasos conservativos de primer orden transfieren igual masa y entalpía y reparten energía cinética entre fluido y pared. Se comprueban estados y entropía total de la pareja. Límites: 4096 subpasos, 64 reducciones a la mitad por candidato y transferencia inicial de hasta 0.1 de la menor masa. El cierre tolera 128 epsilon binary64 por el mayor módulo volumétrico o presión, sin recortar presiones informadas. Un fallo revierte todo el lote. Revise paso, inventario, límites, carrera y área.

## Uso y evidencia

Ejecute `equilibrium-flashing-nozzle` o `equilibrium-pump-needle`. `flux_relative_uncertainty` describe la última envolvente constitutiva local aceptada; no limita caudal medio, masa entregada, integración temporal de primer orden ni calibración. Se comparan inversión independiente de saturación/entropía, bloqueo analítico gas/líquido, búsqueda global densa separada, identidades de energía/entropía y refinamiento analítico de vaciado de vapor. Inventarios, eje/conducto/aguja, reversión, cancelación, ramas, asignaciones, assets y MCP real se verifican aparte. Números y fronteras anteriores están en la evidencia de validación.

[VALIDATION.es.md](VALIDATION.es.md)

## Validez y próximos pasos

Los parámetros siguen `unverified`. El límite de equilibrio supone pseudofluido puro, vapor ideal, expansión térmica líquida nula y velocidad común. Omite tiempos finitos de nucleación/relajación, deslizamiento, pulverización, gas portador y receptor con combustión. La envolvente usa márgenes binary64 declarados; no es una prueba formal de intervalos con redondeo dirigido. La clasificación de garganta cerca de transiciones debe leerse con la envolvente. Validez medida y Unity real siguen pendientes.

Añadir relajación de fase finita con evidencia explícita de residencia y material, después pulverización resuelta y receptor con combustión compatible. Continuar encendido, admisión/escape, pérdidas mecánicas, transmisión y ECU/TCU. Calibración medida y Unity real siguen separados.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
