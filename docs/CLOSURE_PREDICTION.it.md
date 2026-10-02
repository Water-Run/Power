# Predizione limitata della chiusura dell'ago e taglio sulla griglia dei tick

[English](CLOSURE_PREDICTION.md) · [简体中文](CLOSURE_PREDICTION.zh-CN.md) · [Français](CLOSURE_PREDICTION.fr.md) · [Русский](CLOSURE_PREDICTION.ru.md) · [日本語](CLOSURE_PREDICTION.ja.md) · [한국어](CLOSURE_PREDICTION.ko.md) · [Deutsch](CLOSURE_PREDICTION.de.md) · [Español](CLOSURE_PREDICTION.es.md) · **Italiano** · [Português](CLOSURE_PREDICTION.pt-BR.md)

Il [driver fisico dell'ago](NEEDLE_ACTUATION.it.md) può compensare il carburante erogato
dopo la fine del suo comando di tensione. `closure_prediction_ns` opzionale abilita un replay
separato dell'intero impianto, preallocato. Alzata reale dell'ago, decadimento della corrente, lavoro di pressione,
rimbalzo sulla sede e inventario di carburante restano fisici; la predizione cambia la fasatura
del comando invece di tagliare la massa erogata effettiva.

## Contratto di predizione

A un campione dovuto, il predittore copia lo stato corrente completo. Riproduce i
tick fisici configurati con la bobina a tensione nulla, oppure con un periodo limitato
di tensione di azionamento prima del taglio. Tutti gli altri comandi degli attuatori sono tenuti fermi. I controllori
campionati non ricorrono né cambiano comandi dentro questa previsione, e gli eventi
di input esterni futuri non sono anticipati. Scambio gas, comportamento di albero a gomiti/cilindro,
combustione, alimentazione idraulica/elettrica, contatti e intervalli di frizione accettati
continuano attraverso le equazioni normali dell'impianto.

La massa aggiuntiva prevista è l'aumento dell'erogazione totale di quell'iniettore.
Lo scratch non viene confermato nella simulazione reale. Ogni candidato parte
dallo stesso stato di sorgente completo; tempo reale, memoria del controllore e storie
fisiche restano intatti. Lo spazio di lavoro del solver di proprietà della simulazione è preparato di nuovo
per l'intervallo reale. `PredictNeedleClosure(driver_id, out estimate)` espone una
predizione di sola lettura a tensione nulla per i client Core, con annullamento e stato.

L'orizzonte è un multiplo intero di tick fisici, copre almeno due periodi di
campionamento del driver ed è limitato a **4096 tick fisici**. Zero conserva il comportamento
on/off precedente del driver. La predizione non può avvolgere l'orologio intero limitato. Le previsioni
fallite o annullate rifiutano l'intero batch reale; una previsione parziale non è
trattata in silenzio come una stima valida.

## Decisione di chiusura programmata

Il driver confronta l'erogazione del ciclo corrente più il carburante di chiusura previsto con la
richiesta agganciata. Se la chiusura a tensione nulla raggiunge già l'obiettivo, taglia subito.
Altrimenti prevede anche di tenere l'azionamento fino al campione successivo. Se quei due
candidati racchiudono l'obiettivo, una bisezione intera limitata trova candidati di taglio
su tick fisici vicini e sceglie la massa finale proiettata più vicina.

La scadenza selezionata è un conto alla rovescia di tick fisici. Può togliere la tensione
prima del campione successivo del controllore. La decisione di taglio si aggancia per il ciclo
osservato, evitando riaperture ripetute su piccole differenze di predizione. Un nuovo ciclo
osservato azzera quell'aggancio. L'arresto per finestra/inversione può annullare una scadenza in sospeso.
Il carburante effettivo resta governato dall'ago in movimento per tutta la chiusura e il rimbalzo.

La parentesi locale di taglio deve essere monotona entro la tolleranza numerica
dichiarata. Una parentesi violata restituisce fallimento numerico con lo stato di modello/sessione
invariato; ispeziona tensione, meccanica, campionamento e ipotesi di predizione invece
di accettare un taglio non valido. Ogni candidato è limitato a 4096 tick e la
bisezione intera ha al massimo dodici interrogazioni interne più le previsioni agli estremi.

È una fasatura on/off basata sul modello, non combustione predittiva, controllo ECU
calibrato, gestione robusta dei guasti o una mappa misurata dell'iniettore. Tenere fermi gli altri
comandi e omettere gli eventi esterni futuri sono ipotesi esplicite della previsione.
Cambiamenti del carico, della pressione o dell'azione del controllore futuri possono cambiare l'erogazione effettiva.

## Orizzonte e accuratezza fisica

Una predizione finita deve includere il carburante rilevante di chiusura/rimbalzo. Nell'attuatore
di ricerca isolato, la predizione di 8 ms tronca una coda tardiva rilevante; la predizione di 20/30 ms
dà la stessa decisione sulla griglia dei tick. Lo studio dell'orizzonte è conservato come
evidenza, invece di trattare una previsione corta arbitraria come chiusura completa.

Passo temporale fisico, periodo di campionamento del controllore e orizzonte di previsione sono controlli
di accuratezza separati. Un orizzonte più lungo non ripara un'integrazione elettrica/di contatto
grossolana o un modello costitutivo inaccurato. L'uguaglianza previsione/reale sotto
lo stesso modello a input tenuti fermi verifica l'implementazione, non una calibrazione OEM. I controlli analitici,
di ODE indipendenti, di conservazione e di eventi nell'impianto sottostante si applicano ancora.

## Osservabili e transazioni

Le uscite del driver con predizione abilitata includono:

- `predicted_fuel_mass`: carburante aggiuntivo per il candidato di chiusura selezionato, kg.
- `prediction_ticks`: il conteggio di replay fisico configurato.
- `driver_state`: se il taglio è stato agganciato per il ciclo osservato.
- `closing_delay_ticks`: tick fisici restanti prima della rimozione programmata della tensione.

Tensione tenuta e obiettivo/erogazione dell'ultimo campione restano disponibili. La quantità prevista
include ogni ritardo di azionamento pianificato, mentre la query pubblica di sola lettura di Core
predice sempre la chiusura immediata a tensione nulla. Queste quantità non sono trasferimenti
effettivi di carburante e non entrano nei registri di massa, chimica o energia.

Cinque voci aggiuntive di stato riportato per driver conservano massa/conteggio di predizione,
aggancio/ciclo di taglio e conto alla rovescia quando la predizione è abilitata. Lo stato di replay
separato è allocato una volta per simulazione. Letture, avanzamento attivo riuscito e
snapshot non allocano memoria gestita dopo il riscaldamento. Annullamento, revisioni,
ramificazioni indipendenti, cattura speculativa della frizione e fallimento numerico tardivo preservano
tutte le storie di predizione/controllo e fisiche. La predizione disabilitata conserva le
impronte e gli hash precedenti.

## Definizioni condivise ed evidenza

JSON accetta `needle_driver.parameters.closure_prediction_ns` opzionale. Core usa
`NeedleDriverDefinition.ClosurePredictionNanoseconds`. Le capacità dichiarano limiti,
ipotesi di tenuta e osservabili; `closure-compensated-cylinder` è l'esempio
condiviso. Le richieste di sorgente in `kg` restano scrivibili e la tensione della bobina resta di proprietà del driver.
Le richieste riuscite sono distinte dall'inseguimento effettivo della dose e dal superamento dei KPI.

L'asset v21 conserva la tabella dei conteggi esistente ed estende ogni record del driver da
32 a 40 byte con un uint64 di orizzonte. I lettori precedenti usano per default la predizione disabilitata;
una fixture autentica v20 conserva la sua impronta e il replay nello stesso runtime. La predizione
abilitata aggiunge il tag di impronta 25 e l'orizzonte configurato. Conteggi limitati,
unità, orizzonte/allineamento, proprietà del controllore e rifiuto del declassamento sono controllati.

La richiesta isolata di 8 mg, il laboratorio acceso completo, lo studio dell'orizzonte, i modelli immutabili,
la previsione di sola lettura/chiusura manuale indipendente, il replay completo, le allocazioni nulle
e i batch annullati/falliti sono verificati in [VALIDATION.it.md](VALIDATION.it.md).
Motore/trasmissione/controllo completi, mappe fisiche/di azionamento misurate, rabbocco del binario,
Unity reale e accettazione del veicolo calibrato restano incompiuti.
