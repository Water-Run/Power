# Cantidad de movimiento en una línea finita de combustible líquido

[English](FUEL_INERTIAL_PIPE.md) · [简体中文](FUEL_INERTIAL_PIPE.zh-CN.md) · [Français](FUEL_INERTIAL_PIPE.fr.md) · [Русский](FUEL_INERTIAL_PIPE.ru.md) · [日本語](FUEL_INERTIAL_PIPE.ja.md) · [한국어](FUEL_INERTIAL_PIPE.ko.md) · [Deutsch](FUEL_INERTIAL_PIPE.de.md) · **Español** · [Italiano](FUEL_INERTIAL_PIPE.it.md) · [Português](FUEL_INERTIAL_PIPE.pt-BR.md)

## Contrato físico

Un `fuel_inertial_pipe` conecta dos nodos finitos de combustible y una pared térmica finita. La línea horizontal rígida posee masa líquida, energía calórica y cinética propias, además de los recipientes. Densidad constante y temperatura variable perfectamente mezclada. Puertos y línea deben permanecer líquidos; los enlaces cuasiestáticos conservan su modelo bifásico separado.

Declare longitud, área, resistencia lineal/cuadrática, caudal inicial, temperatura inicial y `fluid_heat_fraction`. Las unidades son `pa_s_m3` y `pa_s2_m6`. Material y densidad coinciden. No hay apertura directa: cerrar otra válvula no borra la cantidad de movimiento. Las referencias material y química se heredan de los puertos.

## Energía y puertos

La entalpía donante sale del recipiente aguas arriba. La línea mezcla energía sensible con su inventario y entrega entalpía mezclada a presión aguas abajo. El trabajo cambia energía cinética y aporta calor de fricción una sola vez. La fracción declarada calienta el líquido; el resto la pared finita. La inversión intercambia donante y receptor con iguales balances.

```text
M_line = rho L A; I = rho L / A
E_line = U_line + I Q^2/2 + M_line LHV
D = R Q^2 + K abs(Q)^3
dM_A/dt = -rho Q; dM_B/dt = rho Q
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
dU_line/dt = rho abs(Q) (u_donor-u_line) + xi D
dE_kinetic/dt = (p_A-p_B) Q - D
dE_wall/dt = (1-xi) D
h_port = u + p/rho
```

El movimiento usa caudal medio y presiones finales aceptadas. Energía y mezcla usan Euler hacia atrás; el acoplamiento converge a primer orden. Incluso sin fricción, la discretización transforma parte de la oscilación en calor numérico, no registrado como fricción física. La entropía incluye mezcla y discretización. Los eventos de caudal cero separan transferencias directas e inversas.

## Uso y evidencia

Ejecute `inertial-fuel-line` para inversión/mezcla, o `inertial-pump-fed-cylinder` para entrada, bomba, aguja electromagnética física y combustión prescrita. Lea `volume_flow`, `mass_flow`, `forward_volume`, `reverse_volume`, `kinetic_energy`, `energy_at_a`, `energy_at_b`, inventarios y balances. Caudal volumétrico instantáneo y másico medio del último paso. La línea añade masa y energía química propias.

Solución armónica independiente de flexibilidad finita y RK4 independiente de movimiento/temperatura no lineales verifican exactitud y primer orden. Se verifican calor, entropía, inversión, balances, intervalos especulativos de embrague, asignaciones, reversión, cancelación y ramas. Assets portables y MCP real comprueban cada frontera. Esto no acredita calibración ni Unity real.

## Validez y siguiente trabajo

Los parámetros siguen `unverified`. Respete rangos materiales y estabilidad líquida corregida por presión. El fallo revierte todo el intervalo; reduzca paso e inspeccione agotamiento, presión y fase. Se omiten movimiento bifásico de densidad variable, cavitación, acústica, gravedad, geometría móvil y frentes térmicos. La combustión prescrita no prueba equilibrio químico ni entropía de reacción detallados.

Añadir fronteras compatibles de válvula direccional y aguja/tobera compresibles, con entalpía, energía cinética e hipótesis de fase explícitas. Después abordar relajación/deslizamiento, encendido, admisión/escape, pérdidas mecánicas y coordinación ECU/TCU. Calibración medida y aceptación real de Unity siguen separadas. [Conductos de combustible compresible en equilibrio](EQUILIBRIUM_FUEL_DUCT.es.md)

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: dynamic pipe boundaries](https://doc.modelica.org/om/Modelica.Fluid.Pipes.DynamicPipe.html) · [VALIDATION.md](VALIDATION.md)
