# Cámaras de combustible compresible accionadas por eje

[English](COMPRESSIBLE_FUEL_CHAMBER.md) · [简体中文](COMPRESSIBLE_FUEL_CHAMBER.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_CHAMBER.fr.md) · [Русский](COMPRESSIBLE_FUEL_CHAMBER.ru.md) · [日本語](COMPRESSIBLE_FUEL_CHAMBER.ja.md) · [한국어](COMPRESSIBLE_FUEL_CHAMBER.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_CHAMBER.de.md) · **Español** · [Italiano](COMPRESSIBLE_FUEL_CHAMBER.it.md) · [Português](COMPRESSIBLE_FUEL_CHAMBER.pt-BR.md)

## Contrato físico

Una `compressible_fuel_chamber` conecta un eje a un depósito finito `compressible_fuel`. Su movimiento cambia el volumen; el mismo trabajo de presión cambia energía del fluido y par del eje. El depósito puede alimentar o recibir un `equilibrium_fuel_duct`, con vaporización súbita y condensación en equilibrio.

`node_a` es rotacional y `node_b` tiene un único propietario geométrico. Declare `minimum_reference_volume`, `swept_volume` y `phase`; el volumen inicial debe coincidir con el ángulo inicial. El depósito mezclado tiene velocidad global cero y no puede ser celda interna. El mecanismo sinusoidal sin masa no añade inercia de pistón o leva; la inercia del eje es explícita. La presión exterior de carcasa es cero.

## Trabajo y transporte

El paso de trabajo cerrado mantiene masa y entropía. Energía de compresión fría y energía calórica usan el potencial de equilibrio del conducto. El líquido puro usa la integral exacta de presión y el vapor ideal su adiabática analítica. El cambio de fase invierte entropía y comprueba la energía final. La diferencia aceptada determina un par discreto en la resolución mecánica común. Un `hydraulic_work` positivo significa energía del eje al fluido.

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

El depósito móvil usa medio transporte, trabajo completo del eje y medio transporte. El transporte sigue siendo de primer orden con difusión numérica; la separación simétrica no eleva su orden. El movimiento líquido cerrado y suave converge a segundo orden. Los incrementos angulares se limitan a 0.25 rad por intervalo mecánico. Siguen aplicándose CFL, límites materiales, reversión completa, cancelación y ramas independientes. El impulso estacionario de puertos/carcasa se separa del trabajo del pistón.

## Uso y evidencia

Ejecute `compressible-fuel-chamber-duct` o `electric-compressible-fuel-duct`. Lea masa, volumen, presión, temperatura, entropía y fracción de vapor junto a estados de celda, par, potencia y trabajo. Las pruebas emplean trabajo líquido y límites de vapor independientes, isentrópica de equilibrio invertida aparte y cuadratura de presión, EDO del eje líquido y refinamiento RK4 de ecuaciones semidiscretas de vapor ideal. Conservación, composición eléctrica/engranajes/hidráulica/embrague, reproducción portable, MCP real y transacciones sin asignaciones se verifican por separado. Los resultados están en la evidencia de validación.

[VALIDATION.es.md](VALIDATION.es.md)

## Validez y trabajo pendiente

Los parámetros siguen `unverified`. El pseudofluido puro en equilibrio usa líquido compresible sin expansión térmica y vapor ideal, cámara mezclada y celdas rígidas. Respete los límites de temperatura y presión. La cámara no resuelve energía cinética global ni incluye válvula antirretorno automática. Nucleación a velocidad finita, deslizamiento, comportamiento crítico real, calibración medida y aceptación real de Unity siguen pendientes.

Añadir fronteras compatibles de válvula direccional y aguja/tobera compresibles, con entalpía, energía cinética e hipótesis de fase explícitas. Después abordar relajación/deslizamiento, encendido, admisión/escape, pérdidas mecánicas y coordinación ECU/TCU. Calibración medida y aceptación real de Unity siguen separadas.

[Modelica: swept volume and pressure work](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [NIST: consistent thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html)
