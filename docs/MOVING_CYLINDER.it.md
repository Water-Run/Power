# Scambio gas del cilindro mobile

[English](MOVING_CYLINDER.md) · [简体中文](MOVING_CYLINDER.zh-CN.md) · [Français](MOVING_CYLINDER.fr.md) · [Русский](MOVING_CYLINDER.ru.md) · [日本語](MOVING_CYLINDER.ja.md) · [한국어](MOVING_CYLINDER.ko.md) · [Deutsch](MOVING_CYLINDER.de.md) · [Español](MOVING_CYLINDER.es.md) · **Italiano** · [Português](MOVING_CYLINDER.pt-BR.md)

Un `gas_cylinder` collega un albero a gomiti rotazionale a una camera a gas. A differenza del
riferimento adiabatico chiuso, questa camera porta massa ed energia interna indipendenti, così
restrizioni e legami di parete possono cambiarne lo stato mentre la pressione aziona l'albero a gomiti.
Il componente è disponibile attraverso Core, JSON, CLI, MCP e asset portatili. Non
modella l'inerzia del pistone né la chimica dettagliata. Componenti separati di
[combustione premiscelata](PREMIXED_COMBUSTION.it.md) e [fasatura sull'angolo di manovella](VALVE_TIMING.it.md)
forniscono ora la conversione dell'energia del carburante e comandano le restrizioni collegate.

## Contratto del modello

```csharp
var definition = new ModelDefinition
{
    StepNanoseconds = 50_000,
    Nodes = [NodeDefinition.Rotor(1, 0.2, 60),
        NodeDefinition.CylinderGas(2, 100_000, 300),
        NodeDefinition.Thermal(3, 500, 350)],
    Components = [ComponentDefinition.GasCylinder(10, 1, 2, new()
    {
        Bore = new(86, Unit.Millimeter), Stroke = new(86, Unit.Millimeter),
        RodLength = new(143, Unit.Millimeter), Phase = new(0, Unit.Radian),
        CompressionRatio = 10, BackPressure = new(1, Unit.Bar)
    }), ComponentDefinition.GasReservoir(11, 2, 50e-6, 110_000, 300, 0.8, 100, 1),
        ComponentDefinition.GasHeatLink(12, 2, 3, 2.5)]
};
```

Il nodo gas fornisce pressione assoluta iniziale, temperatura, R e gamma. La sua quantità `Storage`
è zero/None: esattamente un componente cilindro possiede il volume. Il compilatore
ricava massa ed energia iniziali dalla geometria all'angolo iniziale dell'albero a gomiti, inclusa
la fase. Rifiuta un volume specificato in modo indipendente o due proprietari cilindro per una camera.
I nodi gas fissi non collegati richiedono ancora un volume esplicito positivo.

In JSON, usa `domain: "gas"` e ometti `storage` per una camera mobile. Il componente `gas_cylinder`
richiede `node_a` (rotazionale), `node_b` (gas) e i parametri `bore`, `stroke`,
`rod_length`, `phase`, `compression_ratio` e `back_pressure`. Nessuna composizione o stato
iniziale del gas è duplicato in questo componente. Il nodo gas espone pressione, temperatura,
massa ed energia interna; il cilindro espone volume, spostamento del pistone e coppia
all'albero a gomiti. Porte, restrizioni, limiti di apertura e legami di parete usano il
[contratto della rete gas](GAS_NETWORK.it.md) esistente.

I modelli che contengono camere mobili senza restrizioni temporizzate o tracciamento premiscelato riportano la fedeltà `moving_cylinder_gas_exchange` e
aggiungono il tag di impronta del solver 5. I modelli lineari, a cilindro chiuso e solo a volume fisso
esistenti conservano le loro impronte e il loro avanzamento. La composizione resta fissa, i nodi gas collegati
devono concordare e tutti i parametri restano `unverified`.

## Equazioni e accoppiamento conservativo

Per un gas caloricamente perfetto a composizione fissa:

```text
p = (gamma - 1) U / V(theta)
T = U / (m cv)
dm/dt = sum(inflow) - sum(outflow)
dU/dt = -p dV/dt + Q_wall + sum(mdot_in h_in) - sum(mdot_out h_out)
tau_gas = (p - p_back) dV/dtheta
```

Il bilancio di massa/energia segue il primo principio standard per sistema aperto; vedi
[le equazioni del volume di controllo di Cantera](https://www.cantera.org/stable/reference/reactors/controlreactor.html).
Quel riferimento sostiene le equazioni, non lo schema di integrazione di Power! né la validazione.
Il gas usa la legge dell'ugello comprimibile bidirezionale esistente, piuttosto che l'implementazione
di valvola lineare e unidirezionale di Cantera. Il lavoro di contropressione è lavoro di sorgente esterno;
l'entalpia del serbatoio e lo scambio di parete conservano i segni dei registri esistenti.

L'implementazione usa una suddivisione simmetrica dell'operatore per i modelli che contengono camere mobili:

1. Avanza scambio gas e calore di parete di mezzo tick alla geometria iniziale dell'albero a gomiti.
2. Risolvi elettromeccanica accoppiata e lavoro di pressione adiabatico sull'intero tick con
   il solver dell'albero a gomiti a gradiente discreto e limitato.
3. Avanza scambio gas e calore di parete di mezzo tick alla geometria dell'albero a gomiti risultante.
4. Applica il calore gas-parete accumulato e le perdite elettromeccaniche alla soluzione termica.

Durante il passo 2 la massa è fissa e `U_new = U_old (V_old/V_new)^(gamma-1)`. La pressione media del gas
e la coppia vengono dalla differenza divisa di questa stessa variazione di energia.
L'albero a gomiti guadagna il lavoro del gas meno il lavoro di contropressione; la camera perde esattamente il
lavoro del gas corrispondente, all'accuratezza di virgola mobile. `log1p`/`expm1` e la differenza divisa
analitica del volume evitano di sottrarre stati quasi uguali intorno ai passi piccoli e
ai punti morti. Più cilindri possono condividere un albero a gomiti o agire attraverso alberi accoppiati.

La suddivisione ha convergenza del secondo ordine per il caso di flusso bloccato liscio provato, senza
trasferimento di parete. Le temperature di parete restano fisse durante entrambi i mezzi passi del gas, seguite
dalla soluzione termica esistente: l'accuratezza accoppiata alla parete resta del primo ordine. Il limitatore
di flusso vicino all'equilibrio può anche cambiare l'ordine locale. La conservazione non stabilisce l'accuratezza.

## Limiti, fallimento e compatibilità

L'escursione dell'albero a gomiti è limitata a 0.25 rad per tick; la soluzione non lineare usa al massimo 16
iterazioni e 10 tentativi di ricerca lineare. Ogni mezzo passo del gas conserva il limite di 4096 sottopassi,
l'obiettivo del 2% di variazione relativa e il rifiuto al 25% di variazione corretta. Stati, uscite o
esaurimento del solver del gas non validi/non finiti rifiutano l'intero batch del chiamante, inclusi tutti i
tick precedenti e gli input programmati. Riduci `step_ns` e ispeziona area di flusso, stato del gas, conduttanza,
velocità dell'albero a gomiti e inerzia prima di riprovare. Annullamento e ramificazioni conservano tutto lo stato di gas e registri;
l'avanzamento riuscito e gli snapshot sul buffer del chiamante non allocano memoria gestita.

L'asset v4 aggiunge un record di geometria indicizzato e limitato per ogni cilindro a gas, preservando tutti
i lettori v1/v2/v3. Non serializza mai gli spazi di lavoro del solver. Una fixture autentica v3 a volume fisso
verifica che introdurre la geometria mobile non cambi le impronte gas precedenti
né il replay. Vedi [formato degli asset](ASSET_FORMAT.it.md) e [provenienza delle fixture](../tests/Power.Tests/Fixtures/README.md).

## Esperimento ed evidenza

Il [laboratorio del cilindro mobile](../assets/labs/moving-cylinder.power.json) esegue il motoring di un
cilindro con due restrizioni di serbatoio e una parete termica finita. Gli otto eventi di
apertura basati sul tempo esercitano il flusso in ingresso e in uscita dalla camera e includono tick fra i confini
di report e di presentazione. È un esperimento di motoring non calibrato; la programmazione
non è una ECU, un profilo camma, un controllore di motore a quattro tempi o un modello di combustione.

I test confrontano una camera chiusa con l'implementazione esistente del cilindro chiuso attraverso
rotazione avanti/indietro e punti morti, e confrontano una camera aperta con
un'integrazione RK4 scritta in modo indipendente delle ODE governanti. Quest'ultima scrive geometria,
flusso di massa bloccato e lavoro di pressione direttamente dalle equazioni. I controlli di raffinamento del passo
verificano separatamente l'accuratezza del flusso liscio e quella accoppiata alla parete. Controlli aggiuntivi coprono più
alberi a gomiti accoppiati/condivisi, cilindri chiusi/aperti misti, conservazione, proprietà malformata,
normalizzazione delle unità, fallimento/recupero atomico, ramificazioni, annullamento, allocazioni nulle,
compatibilità portatile e tutti i confini di report JSON/MCP/asset.

Unity include una vista del pistone mobile, collegamenti gas e test di importazione/Play. L'evidenza reale
di Editor, rendering, Play Mode e IL2CPP è ancora in sospeso. Vedi il
[registro di validazione](VALIDATION.it.md) per i controlli eseguiti e la [tabella di marcia](ROADMAP.it.md)
per il lavoro restante su motore, trasmissione, controllo e calibrazione.
