# Quantità di moto della linea finita di carburante liquido

[English](FUEL_INERTIAL_PIPE.md) · [简体中文](FUEL_INERTIAL_PIPE.zh-CN.md) · [Français](FUEL_INERTIAL_PIPE.fr.md) · [Русский](FUEL_INERTIAL_PIPE.ru.md) · [日本語](FUEL_INERTIAL_PIPE.ja.md) · [한국어](FUEL_INERTIAL_PIPE.ko.md) · [Deutsch](FUEL_INERTIAL_PIPE.de.md) · [Español](FUEL_INERTIAL_PIPE.es.md) · **Italiano** · [Português](FUEL_INERTIAL_PIPE.pt-BR.md)

## Contratto fisico

Un `fuel_inertial_pipe` collega due nodi finiti di carburante e una parete termica finita. La linea orizzontale rigida possiede massa liquida, energia calorica e cinetica proprie oltre ai serbatoi. Densità costante, temperatura variabile e miscelata. Porte e linea restano liquide; i collegamenti quasi statici mantengono il modello bifase separato.

Dichiarare lunghezza, area, resistenze lineare/quadratica, portata e temperatura iniziali e `fluid_heat_fraction`. Le unità sono `pa_s_m3` e `pa_s2_m6`. Materiale e densità coincidono. Non c'è comando diretto d'apertura: chiudere un'altra valvola non cancella il moto. Riferimenti materiale e chimico sono ereditati dalle porte.

## Energia e porte

L'entalpia donatrice lascia il recipiente a monte. La linea miscela energia sensibile e inventario proprio, poi fornisce entalpia miscelata alla pressione a valle. Il lavoro cambia energia cinetica e fornisce calore d'attrito una sola volta. La frazione dichiarata scalda il liquido, il resto la parete finita. L'inversione scambia donatore/ricevitore con gli stessi bilanci.

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

Il moto usa portata media e pressioni finali accettate. Energia e miscela usano Eulero implicito; l'accoppiamento completo converge al primo ordine. Anche senza attrito, la discretizzazione trasforma parte dell'oscillazione in calore numerico, non registrato come attrito fisico. L'entropia comprende miscela e discretizzazione. Gli eventi di portata nulla separano i trasferimenti diretti e inversi.

## Uso e prove

Eseguire `inertial-fuel-line` per inversione/miscela o `inertial-pump-fed-cylinder` per ingresso, pompa, ago elettromagnetico fisico e combustione prescritta. Leggere `volume_flow`, `mass_flow`, `forward_volume`, `reverse_volume`, `kinetic_energy`, `energy_at_a`, `energy_at_b`, inventari e bilanci. Portata volumetrica istantanea e massica media dell'ultimo passo. La linea aggiunge massa ed energia chimica proprie.

Soluzione armonica indipendente a cedevolezza finita e RK4 indipendente del moto/temperature non lineari verificano accuratezza e primo ordine. Coperti calore, entropia, inversione, bilanci, intervalli speculativi frizione, allocazioni, rollback, annullamento e rami. Assets portabili e MCP reale verificano ogni confine. Non sono calibrazione misurata o accettazione Unity reale.

## Validità e lavoro successivo

I parametri restano `unverified`. Valgono intervalli materiali e stabilità liquida corretta in pressione. Un fallimento ripristina l'intero intervallo; ridurre passo e controllare esaurimento, pressione e fase. Mancano moto bifase a densità variabile, cavitazione, acustica, gravità, geometria mobile e fronti termici. La combustione prescritta non prova chimica d'equilibrio o entropia di reazione dettagliate.

Estendere il moto a densità variabile e cambio fase con chiusura energetica/entropica compatibile prima di dichiarare ingresso cavitante. Proseguire rilassamento/scorrimento, accensione, aspirazione/scarico, perdite, trasmissione ed ECU/TCU. Calibrazione misurata e Unity reale sono separati.

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: dynamic pipe boundaries](https://doc.modelica.org/om/Modelica.Fluid.Pipes.DynamicPipe.html) · [VALIDATION.md](VALIDATION.md)
