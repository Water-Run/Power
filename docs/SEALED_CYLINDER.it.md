# Fondamenta del cilindro chiuso

[English](SEALED_CYLINDER.md) · [简体中文](SEALED_CYLINDER.zh-CN.md) · [Français](SEALED_CYLINDER.fr.md) · [Русский](SEALED_CYLINDER.ru.md) · [日本語](SEALED_CYLINDER.ja.md) · [한국어](SEALED_CYLINDER.ko.md) · [Deutsch](SEALED_CYLINDER.de.md) · [Español](SEALED_CYLINDER.es.md) · **Italiano** · [Português](SEALED_CYLINDER.pt-BR.md)

`sealed_cylinder` accoppia un manovellismo di spinta rigido a un nodo rotazionale. Il cilindro contiene una massa fissa di gas ideale con rapporto dei calori specifici costante e senza scambio termico di parete. È un riferimento di compressione/espansione, non un motore acceso completo. Aspirazione, scarico, carburante, combustione, trafilamento, scambio termico di parete, inerzia alternativa ed eventi di controllo restano lavoro di implementazione separato. Tutti i parametri attuali sono sintetici e `unverified`.

La pressione e la temperatura iniziali valgono all'angolo iniziale del rotore collegato più la fase del cilindro. Cambiare quell'angolo iniziale cambia la massa intrappolata, a meno che pressione e temperatura non siano regolate in modo coerente. Lo stato del gas si ricava dalla posizione dell'albero a gomiti e dall'entropia iniziale immutabile; aggiunge canali osservabili ma nessuna variabile di stato indipendente. Questa riduzione vale solo per il componente adiabatico chiuso.

## Geometria e stato del gas

Le lunghezze si compilano in metri, le pressioni in pascal e la fase in radianti. L'input accetta `m`/`mm`, `pa`/`bar` e `rad`/`deg`. La temperatura è in kelvin; la costante specifica del gas usa `j_kg_k`. Il rapporto di compressione e gamma sono adimensionali. Alesaggio e corsa devono essere positivi, la lunghezza di biella deve superare metà corsa, il rapporto di compressione e gamma devono superare uno, e pressione, temperatura e costante del gas iniziali devono essere positive. La contropressione può essere zero.

Con raggio di manovella `r = stroke/2`, lunghezza di biella `l`, area del pistone `A = π bore²/4` e angolo `θ` misurato dal punto morto superiore:

```text
x(θ) = r (1 - cos θ) + l - sqrt(l² - r² sin² θ)
Vc   = A stroke / (compression_ratio - 1)
V(θ) = Vc + A x(θ)
```

L'implementazione usa una forma algebricamente equivalente per evitare la cancellazione vicino al punto morto superiore. Questa geometria segue la [relazione di volume del manovellismo di spinta centrato della Colorado State University](https://www.engr.colostate.edu/~allan/thermo/page2/page2.html).

Siano `V0`, `P0` e `T0` lo stato iniziale. Le relazioni reversibili del gas ideale sono:

```text
m = P0 V0 / (R T0)
P = P0 (V0/V)^gamma
T = T0 (V0/V)^(gamma-1)
U = P V / (gamma-1)
τ = (P - Pback) dV/dθ
```

Le relazioni pressione/volume e temperatura seguono la [derivazione della compressione isentropica della NASA](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/isentrophic-compression/). Con rapporto di compressione 10 e gamma 1.4, la compressione dal punto morto inferiore a quello superiore moltiplica la pressione per circa 25.119 e la temperatura per circa 2.512. Sono rapporti idealizzati, non prestazioni misurate del motore.

## Integrazione ed energia

La soluzione elettromeccanica al punto medio già presente fornisce una soluzione di base e una risposta precalcolata alla coppia su ogni albero a gomiti distinto del cilindro. Una soluzione non lineare ridotta determina gli incrementi angolari di quegli alberi. I cilindri sullo stesso albero contribuiscono a una sola somma di coppia; gli alberi accoppiati si risolvono insieme. Nessuna chiamata a un provider di modelli, oggetto Unity o dipendenza di terze parti partecipa a un tick fisico.

Ogni cilindro usa una coppia discreta coerente col lavoro:

```text
τ_discrete Δθ = -(U_next - U_old) - Pback (V_next - V_old)
```

Differenze divise analitiche e stabili del volume, e valutazioni di logaritmo/esponenziale per argomenti piccoli, trattano incrementi piccoli e attraversamenti dei punti morti. L'uscita istantanea `Torque` resta `(P-Pback) dV/dθ`; è distinta dalla coppia media usata per integrare un tick finito.

La variazione globale di energia immagazzinata include la variazione di energia interna del gas. Il lavoro di contropressione è lavoro di sorgente esterno, `-Pback ΔV`, così il registro resta `source_work - heat_rejected - stored_energy_change`. La dissipazione di albero e motore entra ancora nella rete termica o nel calore ceduto. Le variazioni di energia del gas si valutano in modo diretto, per evitare di sottrarre grandi energie assolute quando gamma si avvicina a uno.

L'iterazione di Newton è limitata a 16 iterazioni, con al massimo 10 tentativi di ricerca lineare per iterazione. Il predittore lineare e l'escursione accettata dell'albero a gomiti devono restare entro 0.25 radianti per tick. Valori non finiti, escursione eccessiva o mancata convergenza restituiscono `NumericalFailure`; l'intera chiamata, inclusi gli input programmati e gli aggiornamenti del registro, esegue il rollback. Riduci `step_ns` e ricrea il modello/sessione per riprovare con un tick fisso più piccolo. L'accettazione non è una garanzia di accuratezza del passo temporale. Anche angoli accumulati molto grandi perdono la risoluzione angolare binary64; l'accuratezza su lunga durata ha bisogno di una propria evidenza.

## Esperimento osservabile e portatile

Ogni cilindro espone pressione (Pa), temperatura del gas (K), volume (m³), massa fissa (kg), energia interna assoluta (J), spostamento del pistone dal punto morto superiore (m) e coppia all'albero a gomiti (N·m). Gli ID di canale conservano la codifica oggetto/campo esistente. Il modello riporta la fedeltà `sealed_adiabatic_gas` e la calibrazione `unverified`.

Esegui `assets/labs/sealed-cylinder.power.json` dalla CLI, oppure richiedi `get_example_model({"name":"sealed-cylinder"})` via MCP. L'esempio usa un tick di 100 µs, una durata di 0.2 s, due variazioni di coppia e 21 confini di report. I KPI dichiarati si applicano al campione finale, come per gli esperimenti esistenti; i controlli di conservazione del nucleo ispezionano confini ripetuti lungo le esecuzioni.

Lo stesso documento si esporta in `SealedCylinder.powerasset`. Unity ha una vista schematica del pistone guidata dal canale di spostamento; un'unità di scena rappresenta una corsa completa. Dimensioni fisiche e uscite restano SI. L'evidenza reale di Editor, Play Mode e IL2CPP è ancora in sospeso.

`EngineChecks` esegue controlli analitici di geometria e gas ideale, esecuzioni di conservazione di due secondi, raffinamento del passo del secondo ordine, rotazione inversa, casi a passo piccolo e punto morto, più cilindri su alberi a gomiti condivisi e accoppiati, accoppiamento elettrico/termico, fallimento/recupero atomico, annullamento, rami indipendenti, compatibilità degli asset e avanzamento senza allocazioni. Gli stessi controlli girano su entrambi gli assembly di destinazione sull'host .NET.
