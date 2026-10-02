# Sincronizzazione campionata a doppia frizione e passaggio a stadi

[English](DCT_CONTROL.md) · [简体中文](DCT_CONTROL.zh-CN.md) · [Français](DCT_CONTROL.fr.md) · [Русский](DCT_CONTROL.ru.md) · [日本語](DCT_CONTROL.ja.md) · [한국어](DCT_CONTROL.ko.md) · [Deutsch](DCT_CONTROL.de.md) · [Español](DCT_CONTROL.es.md) · **Italiano** · [Português](DCT_CONTROL.pt-BR.md)

`dct_controller` possiede i due canali delle frizioni di trazione e gli otto canali dei selettori di un [grafo di ricerca a sette marce avanti e retromarcia](DUAL_CLUTCH_TRANSMISSION.it.md). Il suo comando intero di marcia richiesta è separato dalla marcia reale confermata, dai percorsi selezionati, dalla fase di cambio, dall'errore di sincronizzazione misurato e dal guasto del controllore.

È una macchina a stati di ricerca guidata dai sensori, con un'interruzione di coppia esplicita. Non stabilisce il raccordo completo di coppia TCU/ECU, il comportamento dettagliato dei denti di innesto, dell'anello sincronizzatore o dell'attuatore della frizione, una strategia di cambio calibrata o una gestione completa dei guasti del veicolo.

## Stati e conferma fisica

| Fase | Politica del comando mantenuto e transizione |
|---|---|
| Folle | Entrambe le frizioni di trazione e i selettori rilasciati |
| Preparazione | Il percorso opposto di destinazione è scarico e preselezionato, mentre la trazione precedente resta innestata |
| Rilascio | L'apertura della trazione precedente scende in rampa; la trazione di destinazione resta rilasciata |
| Sincronizzazione | Il selettore di destinazione sale in rampa fino all'apertura piena, con entrambe le trazioni rilasciate; attesa di slittamento misurato e blocco fisico |
| Innesto | La trazione di destinazione sale in rampa; l'altra trazione resta rilasciata |
| Guida | Trazione e selettore di destinazione confermati; è ammessa la preselezione del percorso scarico adiacente |
| Guasto | Entrambe le trazioni e ogni selettore rilasciati; il guasto resta fino alla folle o a una richiesta diversa |

La destinazione resta agganciata mentre un passaggio è in corso. Le richieste non in folle successive si elaborano dopo quel passaggio; la folle interrompe al campione dovuto. I cambi sullo stesso percorso di ingresso rilasciano la sua trazione prima di cambiare i selettori. I cambi sul percorso di ingresso opposto possono preparare la destinazione scarica prima del rilascio della trazione. Ogni percorso comanda al più un selettore, e non si usa sovrapposizione comandata delle frizioni di trazione.

Le rampe di apertura dei selettori usano la durata di innesto configurata. Un selettore è pronto solo dopo il comando pieno, uno slittamento misurato entro la tolleranza fornita e la modalità fisica `Locked`. La trazione è confermata solo dopo il comando di innesto pieno, un piccolo slittamento di trazione e il blocco fisico. L'accettazione del comando non annuncia un rapporto istantaneo né il completamento fisico della marcia.

La preselezione inattiva può disturbare per breve tempo un percorso confermato. La marcia reale dello snapshot è zero mentre la trazione o il percorso selezionato non è fisicamente bloccato. La perdita persistente è temporizzata a parte; il controllore non confonde un campione transitorio con un guasto sostenuto. Quel timer si azzera ai cambi di fase e al recupero.

## Marcia richiesta, direzione e guasti

La marcia richiesta è un intero in `[-1,7]`, con zero in folle e -1 in retromarcia. La validazione statica, immediata e programmata degli input rifiuta le frazioni. Il comando sorgente usa unità `state_code` esplicite; nessuna frazione ordinaria di frizione è interpretata come numero di marcia.

Un timeout di sincronizzazione restituisce un guasto osservabile a scarico. Una richiesta di retromarcia contro un moto positivo del veicolo sopra il limite di velocità fornito, o una richiesta in avanti contro un moto negativo, è bloccata come guasto di cambio di direzione. La perdita sostenuta del blocco di una trazione o di un selettore confermati usa lo stesso timeout fornito e un codice di guasto distinto. Questi esiti del controllore sono stati di politica fisica, non fallimenti numerici né KPI impliciti di cambio riuscito.

| Codice di guasto | Significato |
|---:|---|
| 0 | Nessun guasto |
| 1 | Timeout di sincronizzazione o di innesto |
| 2 | Richiesta di cambio di direzione bloccata dal moto del veicolo |
| 3 | Perdita persistente del blocco confermato |

La folle azzera il guasto e rilascia il treno. Una richiesta valida diversa può avviare un nuovo tentativo; inviare di nuovo lo stesso bersaglio fallito non azzera il timeout a ogni campione. Decisioni di guasto di livello più alto, controlli di plausibilità, guasti dei sensori e funzioni di sicurezza di guidatore e veicolo restano lavoro separato.

## Definizione e proprietà

Il controllore dichiara il nodo del motore A, il nodo del veicolo, gli ID delle frizioni di trazione dispari e pari, otto selettori nell'ordine avanti 1-7/retromarcia e il suo input di marcia richiesta. Tutti e dieci i canali degli attuatori devono essere distinti, inizialmente rilasciati e avere un solo proprietario. Il compilatore verifica i tipi ordinari di frizione, la topologia di alberi, mozzi e riduzione finale, l'assegnazione dispari/pari, il percorso della ruota folle di retromarcia e i riferimenti stabili. Le liste dei selettori sono copiate in dati di definizione e compilati immutabili.

La temporizzazione esplicita consiste di nanosecondi di campione, rilascio, innesto e timeout di sincronizzazione. Il campionamento si allinea ai tick fisici; gli altri tempi sono multipli positivi del campione e al più dieci secondi. La tolleranza di sincronizzazione e il limite di velocità per la direzione usano `rad_s` o `rpm`. Non si forniscono in silenzio valori OEM, mappe di attuatori o curve di perdita.

Gli agenti scrivono la marcia richiesta. Le scritture dirette di trazione e selettori restituiscono `controlled_input` con il nome e il canale di comando corretti, e lasciano invariati revisione e stato. Le letture espongono la richiesta viva, la marcia confermata, le selezioni dispari e pari comandate, la fase, lo slittamento del selettore di destinazione e il guasto. Questi codici di stato e i canali fisici conservano semantiche diverse.

## Orologi interi e transazioni complete

I campioni girano sul tempo di simulazione intero limitato. Le scritture di input non fanno avanzare la memoria di controllo. Le frazioni mantenute sono applicate al solver fisico normale; inerzia, reazioni degli ingranaggi, sincronizzazione e calore di trazione restano nei registri esistenti. Lo stato del controllore contiene marcia agganciata e attiva, selezioni, fase e guasto, orologio di fase, errore misurato e timer del blocco persistente. Fork, annullamento, batch falliti in ritardo e intervalli speculativi copiano, sottopongono a hash e riportano indietro quella memoria e ogni comando mantenuto, insieme. I passi riusciti e gli snapshot non allocano memoria gestita.

Le esecuzioni lunghe di marcia comandata usano incrementi di coordinata compensati dalla velocità al punto medio. La loro compensazione è transazionale e sottoposta a hash; le tolleranze di fase rigorose restano invariate. Questo risolve l'arrotondamento accumulato esposto dal nuovo scenario lungo di sincronizzazione sotto carico. I percorsi di modello precedenti conservano l'integrazione e il comportamento di replay antecedenti.

Il limite di stato riportato è esplicitamente **128**, con 32 nodi e 64 componenti invariati. Questo consente la composizione completa di ricerca motore acceso, DCT e controllore, che supera il limite precedente di 64 stati. Compilazione e passo al confine esatto, e i modelli fisici o di controllore oltre il limite, sono controllati; build e test restano in serie.

## Esperimenti portabili e condivisi

L'asset v22 aggiunge un record tipizzato di 104 byte di percorso, temporizzazione e tolleranza per ogni controllore DCT. Conserva i lettori v1-v21, gli ID precedenti stabili e i controlli limitati di conteggio e lunghezza, digest, proprietà tipizzata, unità e compilazione fisica. I modelli con controllore aggiungono il tag di impronta 26. I campi di richiesta, reale, selezione, fase, errore e guasto sono accodati senza cambiare gli ID precedenti. Un fixture autentico di grafo v21 conserva il suo digest e il replay aggiornato allo stesso runtime.

`controlled-dual-clutch` emette richieste di marcia attraverso tutti e sette i percorsi e passaggi in discesa selezionati. Osserva il passaggio finale e la preselezione inattiva fino al completamento fisico, invece di assumere un tempo nominale. `controlled-fired-dual-clutch` combina la stessa politica campionata con la combustione premiscelata a cilindro aperto. JSON, CLI, replay portabile e un server figlio MCP reale condividono le definizioni.

[VALIDATION.md](VALIDATION.it.md) registra evidenze di stato e interblocco, guasto e recupero, proprietà, input intero, percorso immutabile, capacità, conservazione delle fasi lunghe, conservazione e replay completo. Tutti i parametri restano `unverified`. Raccordo di coppia, comportamento completo di attuatore, sensore ed ECU, AT completo, Unity reale e gruppi motopropulsori di destinazione calibrati restano incompiuti.
