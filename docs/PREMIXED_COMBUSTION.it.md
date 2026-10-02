# Combustione premiscelata e contabilità dell'energia del carburante

[English](PREMIXED_COMBUSTION.md) · [简体中文](PREMIXED_COMBUSTION.zh-CN.md) · [Français](PREMIXED_COMBUSTION.fr.md) · [Русский](PREMIXED_COMBUSTION.ru.md) · [日本語](PREMIXED_COMBUSTION.ja.md) · [한국어](PREMIXED_COMBUSTION.ko.md) · [Deutsch](PREMIXED_COMBUSTION.de.md) · [Español](PREMIXED_COMBUSTION.es.md) · **Italiano** · [Português](PREMIXED_COMBUSTION.pt-BR.md)

`premixed_combustion` accoppia un profilo di combustione di Wiebe prescritto a un albero a gomiti e a una camera
a gas finita. Carburante, aria fresca e prodotti inerti sono trasportati attraverso la rete gas;
la reazione consuma il reagente limitante disponibile e converte l'energia chimica immagazzinata
in energia termica del gas. Il lavoro di pressione aziona lo stesso solver dell'albero a gomiti usato dai cilindri
mobili. Core, JSON, CLI, MCP e l'asset v6 condividono queste definizioni.

È un modello premiscelato a parametri concentrati e proprietà costanti. Ogni costituente di una rete
collegata condivide un solo R e gamma. Le tre classi di massa non rappresentano specie dettagliate,
capacità termiche variabili, cinetica di reazione, propagazione della fiamma, autoaccensione, detonazione,
emissioni, evaporazione del carburante o iniezione. L'esempio acceso originale usa un ingresso gassoso già miscelato.
La [dosatura del carburante per ciclo](FUEL_METERING.it.md) supporta un binario gassoso finito separato
e l'ammissione d'aria; spruzzo liquido ed evaporazione restano fuori dal modello. Una combustione prescritta e il superamento dei controlli di conservazione non stabiliscono prestazioni misurate del motore
né completano l'obiettivo del gruppo motopropulsore completo.

## Composizione e porte

Un nodo gas aggiunge in opzione `premixed` al suo oggetto `gas` esistente:

```json
"gas": {
  "gas_constant": { "value": 287, "unit": "j_kg_k" },
  "gamma": 1.35,
  "premixed": {
    "lower_heating_value": { "value": 44000000, "unit": "j_kg" },
    "stoichiometric_air_fuel_ratio": 14.7,
    "initial_fractions": { "fuel": 0.04, "fresh_air": 0.96 }
  }
}
```

Il potere calorifico e il rapporto stechiometrico di massa aria/carburante devono essere positivi e finiti.
Le frazioni di carburante e aria fresca devono essere non negative, con somma al massimo uno. Il resto
è prodotti inerti. L'aria fresca rappresenta l'ossidante insieme al suo diluente; consumare
`r` kg di aria fresca con 1 kg di carburante crea `1+r` kg di prodotti. L'aria fresca o il carburante in eccesso
restano disponibili; i prodotti non possono reagire di nuovo.

Ogni restrizione di serbatoio su un nodo premiscelato deve specificare
`reservoir_fractions` esplicite, usando gli stessi due campi. Le frazioni sono vietate sugli altri
componenti o sulle restrizioni interne. In ingresso, il confine fornisce quella composizione;
in uscita, rimuove la composizione effettiva del volume finito. I volumi gas finiti collegati
devono condividere tracciamento, R, gamma, LHV e rapporto stechiometrico. I collegamenti incompatibili o
non tracciati sono rifiutati; gli inventari chimici non possono sparire a una porta.

Il solver del gas trasferisce ogni costituente con lo stesso flusso di massa con segno e le stesse frazioni
a monte del gas totale. Fa evolvere masse dei costituenti non negative e ricostruisce la massa totale
dalla loro somma. Un passo premiscelato è limitato anche dal flusso uscente totale, anche quando
i tassi di massa totale in ingresso e in uscita quasi si annullano. Nessun inventario chimico è creato
dall'equalizzazione della pressione o dal riflusso del serbatoio.

Un gas premiscelato aggiunge tre valori di costituenti memorizzati al budget di stato dichiarato. Un componente
di combustione aggiunge una frontiera angolare irreversibile; tutti restano entro il limite esistente di 64 stati.
I registri compensati di confine e di reazione partecipano a rollback, hash e ramificazioni.

## Legge di combustione e storia dell'albero a gomiti

Il componente collega `node_a` (albero a gomiti) a `node_b` (gas premiscelato). Una camera mobile deve
usare l'albero a gomiti della propria geometria, e ogni camera ammette al massimo un componente di combustione. Un
recipiente fisso può usare un albero a gomiti indipendente per esperimenti analitici.

```json
{
  "id": 15,
  "kind": "premixed_combustion",
  "node_a": 1,
  "node_b": 2,
  "input_channel": 103,
  "initial_input": { "value": 1, "unit": "fraction" },
  "parameters": {
    "cycle_angle": { "value": 720, "unit": "deg" },
    "start_angle": { "value": 340, "unit": "deg" },
    "duration_angle": { "value": 80, "unit": "deg" },
    "shape_exponent": 3,
    "burn_coefficient": 6.9
  }
}
```

Il ciclo è esplicitamente di 360 o 720 gradi. L'angolo di inizio è relativo all'albero a gomiti effettivo,
non implicitamente sfasato dalla fase geometrica del cilindro. La durata è in [1e-6 rad, angolo di ciclo];
l'esponente di forma `n` è in [1,16] e il coefficiente `a` in (0,50]. L'inizio è normalizzato modulo
il ciclo. Tutti gli angoli richiedono unità. Per l'avanzamento in avanti `z` dall'inizio della combustione, limitato
a [0,1], l'hazard integrato è `H(z) = a z^n`. Ogni ciclo completo contribuisce `a`.

Sugli angoli in avanti appena percorsi:

```text
hazard = burn_multiplier * delta(H)
limiting_fuel = min(fuel_mass, fresh_air_mass / stoichiometric_air_fuel_ratio)
burned_fuel = limiting_fuel * (1 - exp(-hazard))
consumed_air = stoichiometric_air_fuel_ratio * burned_fuel
created_products = burned_fuel + consumed_air
released_heat = LHV * burned_fuel
```

Per una carica chiusa e moltiplicatore 1, la frazione bruciata è `1-exp(-a z^n)` della
quantità iniziale di carburante limitante. **Non** è forzata a uno al confine della durata:
`exp(-a)` resta incombusto dopo una finestra di combustione completa. Le esposizioni piccole usano `expm1` per
evitare la cancellazione. La carica fresca che entra durante una finestra attiva si unisce ai reagenti
ben miscelati; non c'è una sorgente di calore nascosta e illimitata per ciclo.

Il canale di input opzionale è `burn_multiplier`, una frazione in [0,1] che scala l'hazard.
Zero disabilita la reazione; non ferma il carburante che entra da un ingresso aperto. Questo input
non è un comando dell'iniettore né un controllore predittivo di accensione.

Ogni componente memorizza il massimo angolo di manovella raggiunto, inizializzato all'angolo di
partenza. La reazione avviene solo oltre quella frontiera. Arrestarsi, ruotare all'indietro o
ripercorrere angoli già visitati non può rilasciare di nuovo calore. L'avanzamento in avanti disabilitato
sposta comunque la frontiera, quindi riabilitare non rilascia il calore mancato. Partire dentro
una finestra di combustione consuma solo l'esposizione in avanti restante. Dopo una grande inversione,
la combustione resta soppressa finché l'albero a gomiti non supera il suo massimo precedente; l'accensione
bidirezionale del motore e il riarmo guidato dal controllore restano lavoro di controllo futuro.

## Energia e accoppiamento numerico

L'energia interna del gas resta termica: `U = m cv T`. L'energia chimica è separatamente
`E_chemical = m_fuel LHV`. L'entalpia totale del serbatoio include sia `mdot cp T` sia l'energia
chimica trasportata. La variazione globale di energia immagazzinata include l'inventario chimico,
quindi la combustione è una conversione interna, non lavoro di sorgente esterno aggiuntivo:

```text
energy_residual = mechanical/electrical source work + reservoir total enthalpy
                  - rejected heat - change(total stored energy)
```

`net_fuel_energy_in` espone separatamente la parte chimica del registro di confine. È
ingresso netto, incluso il carburante incombusto che lascia il modello; non è l'erogazione lorda di carburante né
una metrica di consumo di carburante a regime. `fuel_residual` e `fresh_air_residual` confrontano
inventario iniziale, trasferimento netto di confine, inventario corrente e reazione cumulativa.
`mass_residual` continua a coprire la massa totale del gas. La conversione dei costituenti conserva la massa.

Per una camera mobile, l'anteprima del calore dipende dall'angolo di manovella nuovo di prova e partecipa
alla soluzione non lineare dell'albero a gomiti. Con calore totale `Q` durante il tick e
`r = (V_old/V_new)^(gamma-1)`:

```text
U_after = (U_before + Q/2) r + Q/2
adiabatic_work = (U_before + Q/2) (1-r)
crank_work = adiabatic_work - back_pressure * (V_new - V_old)
```

La coppia di pressione discreta usa quello stesso lavoro, così energia del gas, energia chimica e
lavoro all'albero a gomiti concordano. Il carburante è consumato solo dopo che la soluzione riesce nello stato candidato.
Il trasporto del gas usa ancora mezzi passi simmetrici intorno a lavoro all'albero/reazione. La temperatura di parete
resta fissa sul tick esterno; l'accoppiamento di parete è del primo ordine.

Per una combustione abilitata, l'escursione angolare e l'escursione dalla velocità agli estremi per tick devono restare entro
`min(0.25 rad, duration_angle/32)`, con una corrispondente protezione di risoluzione angolare binary64.
Il calore rilasciato non deve superare il 25% dell'energia termica prima della combustione in un tick. Sono limiti
sul lavoro ammesso e sulla risoluzione, non garanzie di accuratezza. Si applicano insieme ai limiti di
sottopasso del gas e di iterazione del cilindro. Riduci `step_ns` su `numerical_failure`, allinea
gli eventi programmati al nuovo tick e ricrea il modello/sessione. Le chiamate fallite o annullate
non confermano stato, input, frontiera, registro chimico o cursore di riproduzione.

## Uscite, compatibilità ed evidenza

I nodi premiscelati aggiungono i campi KPI `fuel_mass`, `fresh_air_mass`, `product_mass` e `chemical_energy`.
Un componente di combustione aggiunge i cumulativi `fuel_burned` (kg) e `heat_released` (J).
Il campo KPI `burn_frontier` espone il massimo angolo di manovella visitato (quantità di canale
`burn_frontier_angle`, rad), così la combustione soppressa dopo un'inversione si può ispezionare.
I canali globali aggiungono energia chimica, ingresso netto di energia del carburante, residuo di carburante e residuo
di aria fresca. Le quantità di canale restituite dalla scoperta fanno fede; per esempio la massa di carburante del nodo
si chiama `unburned_fuel_mass`. Le uscite ordinarie di energia interna del gas e di flusso conservano
i loro significati termici e di flusso con segno.

I modelli premiscelati aggiungono il tag di impronta 7 e i parametri normalizzati di reazione/composizione.
Le impronte e l'avanzamento precedenti non reagenti restano invariati. L'asset v6 aggiunge record di composizione,
frazione di serbatoio e combustione; le fixture autentiche v1–v5 conservano la compatibilità. Le
nuove fedeltà sono `premixed_gas_transport` e `premixed_wiebe_combustion`.

I test coprono consumo analitico di carburante/aria e temperatura in recipiente chiuso, reagenti
limitanti, trasferimento di serbatoio avanti/indietro, conservazione dei costituenti in rete chiusa,
convergenza di ODE indipendenti di albero/gas reagenti, combustione arrestata/invertita/disabilitata,
contratti malformati, rollback del batch, annullamento, ramificazioni e allocazioni nulle di avanzamento/snapshot.
Il [laboratorio del cilindro acceso](../assets/labs/fired-cylinder.power.json)
aziona un carico attraverso fasi ripetute di aspirazione/compressione/combustione/espansione/scarico e
si riproduce in modo identico a tutti i 63 confini di report JSON/CLI/MCP/asset. L'evidenza numerica
e l'ambito di esecuzione reale sono registrati in [VALIDATION.it.md](VALIDATION.it.md).

[Le equazioni del reattore a gas ideale di Cantera](https://www.cantera.org/stable/reference/reactors/ideal-gas-reactor.html)
forniscono il contesto di massa/specie/energia del volume di controllo. L'[esempio di motore SI di Ansys](https://chemkin.docs.pyansys.com/version/stable/examples/advanced/SI_engine_optimization.html)
usa fasatura di combustione esplicita e parametri di Wiebe. Questi riferimenti motivano i contratti;
la loro chimica dettagliata, i modelli a due zone e i parametri di esempio non sono copiati né
presentati come verifica di questo solver a proprietà costanti. Non c'è una dipendenza di runtime
da nessuno dei due pacchetti. Tutti i parametri di esempio restano `unverified`.
