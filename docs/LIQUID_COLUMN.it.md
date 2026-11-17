# Riferimento della quantità di moto all'ingresso liquido

[English](LIQUID_COLUMN.md) · [简体中文](LIQUID_COLUMN.zh-CN.md) · [Français](LIQUID_COLUMN.fr.md) · [Русский](LIQUID_COLUMN.ru.md) · [日本語](LIQUID_COLUMN.ja.md) · [한국어](LIQUID_COLUMN.ko.md) · [Deutsch](LIQUID_COLUMN.de.md) · [Español](LIQUID_COLUMN.es.md) · **Italiano** · [Português](LIQUID_COLUMN.pt-BR.md)

## Confine fisico

`LiquidColumn` è un riferimento C# autonomo per tubo orizzontale rigido con sezione, velocità uniforme, densità liquida e temperatura costanti. Le pressioni assolute sono prescritte nell'intervallo. Resistenze lineari e quadratiche sono input di ricerca espliciti. Non è un componente del grafo e non cambia rete o ugello.

## Quantità di moto ed energia

Le porte hanno uguale portata e velocità; i flussi cinetici convettivi si cancellano. Il lavoro di pressione cambia l'energia cinetica accumulata e produce calore d'attrito. Un bagno isotermo riceve calore; temperatura ed energia interna restano costanti. L'entropia prodotta è calore diviso temperatura dichiarata. Non rappresenta inventario termico finito.

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

Il punto medio implicito risolve l'equazione scalare monotona. L'inversione si divide a portata zero con la stessa equazione, conservando volumi diretti/inversi e calore. Una chiamata accettata usa al massimo due intervalli. Superare l'equilibrio stazionario restituisce `StepTooLarge`; ridurre durata. Energia e impulso sono controllati all'arrotondamento binary64. Anche un passo grossolano accettato richiede verifica d'accuratezza.

## API del nucleo e prove numeriche

Costruire `LiquidColumn` con geometria SI, densità, resistenze, temperatura e pressione assoluta minima esplicite. Passare `LiquidColumnState` e secondi a `Advance`. I nomi contengono unità SI. Il risultato include trasporto con segno e direzionale, lavoro, variazione cinetica, calore ed entropia. Un rifiuto lascia risultato predefinito e ingresso intatto, senza allocazioni gestite. La durata locale non sostituisce l'orologio intero della simulazione.

Accelerazione senza perdite, esponenziale lineare e tangente iperbolica quadratica indipendenti verificano i limiti. Una RK4 indipendente a pressione variabile verifica il secondo ordine con inversione. Decadimento passivo, equilibrio, energia completa, entropia non negativa, rifiuti, condivisione immutabile e allocazioni sono testati su entrambi gli obiettivi.

## Prossimo accoppiamento

I parametri restano `unverified`. La pressione minima è un confine liquido dichiarato dal chiamante, non una previsione di tensione di vapore o NPSH. Comprimibilità, acustica, gravità, geometria mobile, accumulo termico, cavitazione, scorrimento e rilassamento sono esclusi. I collegamenti bifase restano quasi statici. Unity reale e calibrazione misurata sono separati.

Un `fuel_inertial_pipe` collega due nodi finiti di carburante e una parete termica finita. La linea orizzontale rigida possiede massa liquida, energia calorica e cinetica proprie oltre ai serbatoi. Densità costante, temperatura variabile e miscelata. Porte e linea restano liquide; i collegamenti quasi statici mantengono il modello bifase separato. [Quantità di moto della linea finita di carburante liquido](FUEL_INERTIAL_PIPE.it.md)

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: pipe flow models](https://doc.modelica.org/om/Modelica.Fluid.Pipes.BaseClasses.FlowModels.html) · [VALIDATION.md](VALIDATION.md)
