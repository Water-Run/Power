# Referencia de cantidad de movimiento en la entrada líquida

[English](LIQUID_COLUMN.md) · [简体中文](LIQUID_COLUMN.zh-CN.md) · [Français](LIQUID_COLUMN.fr.md) · [Русский](LIQUID_COLUMN.ru.md) · [日本語](LIQUID_COLUMN.ja.md) · [한국어](LIQUID_COLUMN.ko.md) · [Deutsch](LIQUID_COLUMN.de.md) · **Español** · [Italiano](LIQUID_COLUMN.it.md) · [Português](LIQUID_COLUMN.pt-BR.md)

## Frontera física

`LiquidColumn` es una referencia C# independiente de un tubo horizontal rígido con sección, velocidad uniforme, densidad líquida y temperatura constantes. Las presiones absolutas se prescriben por intervalo. Las resistencias lineal y cuadrática son entradas de investigación explícitas. No es un componente del grafo ni cambia las ecuaciones de red o tobera.

## Cantidad de movimiento y energía

Ambos puertos tienen igual caudal y velocidad; los flujos cinéticos advectados se cancelan. El trabajo de presión cambia la energía cinética almacenada y genera calor de fricción. Un baño isotermo recibe calor; temperatura y energía interna permanecen constantes. La producción de entropía es calor dividido por temperatura declarada. No representa inventario térmico finito.

```text
I = rho L / A                         [Pa s^2 / m3]
R                                    [Pa s / m3]
K                                    [Pa s^2 / m6]
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
E_kinetic = I Q^2 / 2
Q_mean = (Q_before + Q_after) / 2
I (Q_after-Q_before) = dt (p_A-p_B-R Q_mean-K Q_mean abs(Q_mean))
delta_V = dt Q_mean
W_pressure = (p_A-p_B) delta_V
H_friction = dt (R Q_mean^2 + K abs(Q_mean)^3)
W_pressure = delta_E_kinetic + H_friction
S_produced = H_friction / T
```

El punto medio implícito resuelve la ecuación escalar monótona. La inversión se divide en caudal cero mediante la misma ecuación, reteniendo volúmenes directo/inverso y calor. Una llamada aceptada usa como máximo dos intervalos. Sobrepasar el equilibrio estacionario devuelve `StepTooLarge`; reduzca duración. Energía e impulso se comprueban al redondeo binary64. Un paso grueso aceptado aún necesita comprobar exactitud.

## API del núcleo y evidencia numérica

Construya `LiquidColumn` con geometría SI, densidad, resistencias, temperatura y presión absoluta mínima explícitas. Pase `LiquidColumnState` y segundos a `Advance`. Las propiedades incluyen unidades SI. El resultado contiene transporte con signo y direccional, trabajo, cambio cinético, calor y entropía. El rechazo deja resultado predeterminado y entrada intacta, sin asignaciones administradas. La duración local no sustituye el reloj entero de simulación.

Aceleración sin pérdidas, exponencial lineal y tangente hiperbólica cuadrática independientes verifican límites. Una RK4 independiente con presión variable comprueba segundo orden con inversión. Se verifican decaimiento pasivo, equilibrio, energía completa, entropía no negativa, rechazos, uso inmutable y asignaciones en ambas compilaciones.

## Siguiente acoplamiento

Los parámetros siguen `unverified`. El mínimo de presión es un límite líquido declarado por el usuario, no predicción de presión de vapor o NPSH. Compresibilidad, acústica, gravedad, geometría móvil, almacenamiento térmico, cavitación, deslizamiento y relajación quedan fuera. Los enlaces bifásicos siguen cuasiestáticos. Unity real y calibración medida son independientes.

Conectar la referencia de cantidad de movimiento a la red finita con inventario energético de línea y entalpías compatibles explícitos. Establecer validez de densidad variable y cambio de fase antes de afirmar dinámica cavitante de entrada. Continuar relajación, encendido, admisión/escape, pérdidas y ECU/TCU coordinados.

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: pipe flow models](https://doc.modelica.org/om/Modelica.Fluid.Pipes.BaseClasses.FlowModels.html) · [VALIDATION.md](VALIDATION.md)
