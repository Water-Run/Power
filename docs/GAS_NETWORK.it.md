# Rete gas compilata

[English](GAS_NETWORK.md) · [简体中文](GAS_NETWORK.zh-CN.md) · [Français](GAS_NETWORK.fr.md) · [Русский](GAS_NETWORK.ru.md) · [日本語](GAS_NETWORK.ja.md) · [한국어](GAS_NETWORK.ko.md) · [Deutsch](GAS_NETWORK.de.md) · [Español](GAS_NETWORK.es.md) · **Italiano** · [Português](GAS_NETWORK.pt-BR.md)

Le reti gas finite girano ora attraverso `CompiledModel` e `Simulation`. Il punto di controllo
copre camere a volume fisso, serbatoi a pressione/temperatura fisse, orifizi
controllati e legami termici di parete. L'[estensione del cilindro mobile](MOVING_CYLINDER.it.md) collega ora lo scambio gas al volume dipendente dall'albero a gomiti e al lavoro di pressione; il solver a volume fisso descritto sotto conserva il suo comportamento originale.

## API C# e unità

```csharp
var model = CompiledModel.Compile(new ModelDefinition
{
    StepNanoseconds = 100_000,
    Nodes = [NodeDefinition.GasVolume(1, 0.002, 2e6, 900)],
    Components = [ComponentDefinition.GasReservoir(10, 1, 2e-5, 1e5, 300,
        dischargeCoefficient: 0.9, channel: 100, opening: 0)]
});
var simulation = model.CreateSimulation();
simulation.SubmitInputs([new Scalar(100, 0.5)]);
var status = simulation.Step(100_000_000);
var values = new Scalar[model.OutputCount];
var snapshot = simulation.ReadSnapshot(values);
```

`GasVolume` prende il volume in m³, la pressione in Pa, la temperatura in K e, in opzione,
R in J/(kg K) e gamma. Un nodo gas memorizza il volume in `Storage`, la temperatura in `Initial`,
la pressione in `Position` e la composizione in `Gas`. Litri, bar e millimetri quadrati sono
accettati da quantità esplicite e normalizzati prima dell'impronta.

`GasOrifice` unisce due ID di nodi gas. `GasReservoir` unisce un nodo a un confine fisso;
`NodeB == 0` identifica quel serbatoio. `GasHeatLink` unisce un nodo gas e un nodo termico
con conduttanza in W/K. I nodi gas collegati devono condividere esattamente gli stessi R e gamma.
L'apertura è una frazione adimensionale in [0,1], validata per input iniziali, diretti e
programmati. Un ID di canale di input nullo lascia fissa l'apertura iniziale. Le reti di solo gas non
hanno bisogno di un rotore fittizio. I limiti restano 32 nodi, 64 componenti e 64 stati scalari; ogni
volume gas consuma due stati.

Ogni nodo gas espone pressione, temperatura, massa ed energia interna. Le restrizioni
espongono il flusso di massa con segno da A a B; i legami termici espongono il flusso di calore con segno dal gas alla parete.
L'entalpia del serbatoio è positiva in ingresso. Il residuo energetico è
`source_work + reservoir_enthalpy - heat_rejected - stored_energy_change`.
Il residuo di massa è `sum(mass - initial_mass) - cumulative_reservoir_mass`.
I residui in virgola mobile si valutano rispetto a scale fisiche, non allo zero esatto.

## Metodo numerico e confine

Il solver del gas usa sottopassi espliciti con un predittore/correttore di Heun. Il massimo
tasso relativo iniziale di massa/energia del tick seleziona un numero uniforme di sottopassi, con obiettivo del 2% di variazione
per sottopasso. Più di 4096 sottopassi, stati non fisici, valori non finiti o una variazione
corretta di massa/energia oltre il 25% rifiutano l'intero batch. Riduci `StepNanoseconds` e
ricompila, oppure ispeziona area di flusso, volume, conduttanza e condizioni iniziali.

La legge dell'ugello ha una derivata singolare a pressione uguale. Ogni valutazione limita
l'energia trasferita alla quantità a pressione uguale della coppia collegata, scalando insieme massa ed
entalpia a monte. Per volumi finiti questa energia è
`abs(pA-pB) / ((gammaA-1)/VA + (gammaB-1)/VB)`; un serbatoio fisso omette il termine B.
Questo impedisce oscillazioni di attraversamento della pressione della coppia isolata, preservando i
registri accoppiati. Il limitatore cambia l'integrazione vicino all'equilibrio; l'accuratezza del secondo ordine è
affermata solo per il caso di raffinamento a flusso bloccato liscio e non limitato nei test.

La temperatura di parete resta al suo valore iniziale durante i sottopassi del gas. Il calore di parete accumulato
entra poi nella soluzione termica esistente. Questo accoppiamento è del primo ordine nel tick esterno;
la stabilità a passo grande o la sola conservazione non stabiliscono l'accuratezza. Il test di parete
confronta le temperature a tempo finito con la soluzione analitica a due capacità. Questo metodo
non è il solver implicito a coppie proposto in precedenza e non valida quella proposta.

Massa, energia, somme dei serbatoi e correzioni dei registri compensati appartengono allo
stato di simulazione e sono inclusi in copia, rollback, ramificazioni e hash. L'avanzamento riuscito e
gli snapshot sul buffer del chiamante non allocano memoria gestita. Un batch programmato fallito ripristina
tutti i tick e gli input precedenti, compreso il fallimento dopo tick precedenti riusciti. Gli aggiornamenti
degli input e gli eventi programmati terminali rifiutano anche osservabili del gas non finiti.

I modelli con nodi gas aggiungono il tag di impronta del solver 4. Le impronte e gli hash di stato dei modelli
lineari/a cilindro esistenti conservano la costruzione precedente. I parametri di esempio
restano `unverified`.

## Integrazione JSON, agente e portatile — 2026-09-22

Il [laboratorio della rete gas](../assets/labs/gas-network.power.json) è l'esempio condiviso
per JSON, CLI, MCP e replay portatile. Contiene due camere a gas, una restrizione
interna controllata, una restrizione di serbatoio controllata e un legame termico di parete. I suoi eventi
includono tick fra i confini di report/presentazione; ogni confine di report è confrontato
con il replay dell'asset decodificato. I parametri restano sintetici e `unverified`.

`power.model.v1` aggiunge queste definizioni esplicite:

| Definizione | Campi JSON e unità |
|---|---|
| Nodo gas | `domain: "gas"`; `storage`: m3 o l; `initial`: k; `position`: pa o bar; `gas`: gas_constant in j_kg_k e gamma > 1 |
| Orifizio gas | `kind: "gas_orifice"`; node_a/node_b; `initial_input`: frazione in [0, 1]; parametri: area in m2 o mm2 e discharge_coefficient |
| Orifizio di serbatoio | Orifizio gas con node_b assente/zero; richiede anche reservoir_pressure in pa o bar e reservoir_temperature in k |
| Legame di parete del gas | `kind: "gas_heat_link"`; node_a è gas, node_b è termico; parametri: conductance in w_k |

Un `input_channel` mancante o nullo mantiene fissa l'apertura iniziale esplicita. I parametri
di serbatoio sono vietati per una restrizione a due volumi. La composizione è richiesta solo sui
nodi gas. I nuovi campi di controllo sono `mass_flow`, `heat_flow`, `reservoir_enthalpy` e
`mass_residual`; i campi di controllo esistenti dello stato del gas e dell'energia restano disponibili.

`CompiledModel.ValidateInput` controlla i vincoli statici di canale/valore senza mutare
lo stato. La validazione dell'esperimento e la creazione dell'asset portatile lo usano per tutte le
aperture programmate, inclusi gli eventi successivi. L'invio e l'avanzamento a runtime eseguono ancora
controlli aggiuntivi degli osservabili dipendenti dallo stato e conservano il rollback completo.

`power.asset.v3` e le versioni successive conservano composizione del gas, area, coefficiente di efflusso e pressione
del serbatoio con record di estensione indicizzati e limitati. Conduttanza di parete, temperatura del serbatoio,
aperture iniziali e ID dei canali di input usano i campi di base del componente. I lettori v1/v2
restano supportati per i loro insiemi di modelli originali e rifiutano le definizioni gas. Fixture autentiche
precedenti alla modifica verificano la compatibilità all'indietro. Vedi [il formato degli asset](ASSET_FORMAT.it.md).

Le capacità MCP versione 0.8.0 annunciano il dominio gas, i componenti, la fedeltà, i limiti
di apertura e i limiti vincolati del solver. `get_example_model` accetta `gas-network`. La build
esporta `GasNetwork.powerasset`; lo Studio aggiunge recipienti schematici, marcatori di serbatoio e
percorsi di restrizione/calore con gli input e i canali di uscita esistenti. I suoi nuovi test di importazione e
Play Mode richiedono un'esecuzione reale dell'Editor Unity e non sono coperti dall'evidenza .NET.

## Validazione e lavoro restante sul motore

I nove gruppi di modelli compilati e i sei gruppi di primitivi del gas continuano a girare contro
entrambe le destinazioni Core. I test portatili coprono in più modelli misti cilindro/gas/termico,
quantità non SI, composizione non predefinita, corruzione dell'estensione, record
mancanti/duplicati, compatibilità v1/v2, limiti programmati, annullamento e rollback del cursore degli eventi.
L'equivalenza JSON/Core e il replay MCP reale coprono il confine di integrazione.
Vedi [validazione](VALIDATION.it.md) per i risultati della verifica in serie.

Gli assembly Standard girano su .NET 10 per questi controlli; non è evidenza di Unity Editor o
IL2CPP. Le equazioni del solver solo a volume fisso, i limiti di integrazione e la costruzione dell'impronta
restano invariati per i modelli senza restrizioni temporizzate o tracciamento premiscelato. I modelli con camere mobili
o restrizioni temporizzate usano l'accoppiamento suddiviso, versionato separatamente, documentato in
[MOVING_CYLINDER.it.md](MOVING_CYLINDER.it.md) e [VALVE_TIMING.it.md](VALVE_TIMING.it.md).

La [combustione premiscelata](PREMIXED_COMBUSTION.it.md) opzionale trasporta ora carburante, aria fresca
e prodotti a proprietà del gas costanti. Termochimica dettagliata delle specie, campioni
di veicolo calibrati e i traguardi completi di motore/trasmissione/controllo restano aperti.
