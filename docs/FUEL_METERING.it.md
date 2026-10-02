# Binario di carburante gassoso finito e dosatura della dose per ciclo

[English](FUEL_METERING.md) · [简体中文](FUEL_METERING.zh-CN.md) · [Français](FUEL_METERING.fr.md) · [Русский](FUEL_METERING.ru.md) · [日本語](FUEL_METERING.ja.md) · [한국어](FUEL_METERING.ko.md) · [Deutsch](FUEL_METERING.de.md) · [Español](FUEL_METERING.es.md) · **Italiano** · [Português](FUEL_METERING.pt-BR.md)

`gas_fuel_injector` trasferisce carburante da un binario gas tracciato e finito a una camera
a gas compatibile. Una finestra di manovella in avanti aggancia una massa di carburante richiesta per ciclo;
pressione, temperatura, area dell'ugello e inventario disponibile del binario determinano l'erogazione
effettiva. Il controllore strozza la porta vicino alla sua quota. Non aggiunge carburante
direttamente allo stato ricevente e non assume che una dose richiesta sia stata erogata.

Questo estende l'attuale modello di miscela gassosa a proprietà costanti. Ammissione d'aria
separata, dosatura del carburante, miscelazione e reazione prescritta si possono ora modellare. Non
implementa spruzzo di benzina liquida, evaporazione, dinamica dell'ago/elettrica, fisica del
binario/serbatoio liquido, proprietà dettagliate delle specie o comportamento di iniezione/ECU OEM calibrato.
Restano lavoro richiesto verso l'obiettivo del motore completo.

## Flusso, costituenti ed energia

Sorgente e ricevitore sono nodi gas premiscelati finiti e distinti. Entrambi condividono R, gamma,
potere calorifico e rapporto stechiometrico. La sorgente può essere carburante gassoso puro o una
miscela tracciata che contiene carburante. Il flusso segue la legge esistente dell'[orifizio gas](GAS_NETWORK.it.md),
dipendente dalla pressione, bloccata/subcritica, con area e coefficiente di efflusso espliciti.
Il flusso a pressione inversa è chiuso: il gas del ricevitore non riempie a ritroso il binario.

La quota riguarda la **massa di carburante**, non la massa totale della miscela di sorgente. Ogni avanzamento
del gas limita il tasso di carburante con `remaining_cycle_fuel / advance_duration`. Lo stesso fattore di flusso
scala la massa totale e l'entalpia termica a monte; le frazioni dei costituenti usano lo stato
a monte effettivo. Gli stadi di Heun e la storia di erogazione accettata usano gli stessi trasferimenti.
Di conseguenza, il carburante ricevente, l'impoverimento del binario, l'energia termica e l'inventario
chimico restano coerenti anche quando la pressione del binario cala o la composizione della sorgente cambia.

Il trasferimento interno di carburante non entra in `fuel_energy_in` esterno né nell'entalpia
del serbatoio. L'energia chimica di sorgente immagazzinata si sposta col carburante e diventa calore
del gas solo quando il componente di combustione separato la consuma. Chimica incompatibile o
parametri calorici del gas sono rifiutati in compilazione. La limitazione della dose cambia il flusso
ammesso invece di correggere massa o energia dopo l'integrazione.

## Contratto di ciclo e comando

| Dato | Significato |
|---|---|
| Sorgente A / ricevitore B | Volumi gas tracciati finiti e distinti |
| `area`, `discharge_coefficient` | m2/mm2 positivi e coefficiente in (0,1] |
| `crank_node` | Riferimento di fasatura rotazionale esplicito; una camera a manovella mobile usa il proprio albero a gomiti |
| `cycle_angle` | 360 o 720 gradi, con unità angolari esplicite |
| `start_angle`, `duration_angle` | Inizio della finestra e durata positiva non superiore a un ciclo |
| `maximum_dose` | Limite positivo di carburante per ciclo in kg |
| Input `fuel_dose_per_cycle` | kg richiesti in [0,maximum_dose] |

La finestra rettangolare ideale si apre solo durante il moto in avanti. Il primo avanzamento
accettato a finestra aperta campiona la dose richiesta. Le scritture durante quel ciclo osservato
si applicano alla finestra successiva; il canale del ciclo richiesto continua a mostrare l'obiettivo
agganciato. Zero disabilita quel ciclo. Se pressione o carburante disponibili sono insufficienti, l'erogazione
effettiva resta sotto l'obiettivo. Un passo riuscito non implica una dose completa.

Gli ordinali di ciclo sono interi con segno e limitati, ricostruiti da angoli di manovella
rappresentabili. Tornare a un ciclo osservato precedente non può azzerare la sua quota; invertire
il moto chiude la finestra. L'escursione per intervallo meccanico è limitata a
`min(0.25 rad,duration/8)`. Gli estremi della finestra usano l'approssimazione esistente a tick fisso e suddivisione
simmetrica, quindi la fasatura vicino alle discontinuità richiede il raffinamento del passo temporale.
Non si afferma un istante di commutazione continuo esatto.

Le uscite includono l'apertura corrente della finestra di dosatura, la portata media di carburante erogato sull'ultimo tick,
la dose richiesta agganciata, la dose erogata nel ciclo osservato e il carburante erogato cumulativo.
Pressione, temperatura e carburante restante del binario finito sono canali gas ordinari.
Tutte le storie di quota/ordinale/erogazione, inclusa la compensazione, si copiano e si sottopongono a hash con
lo stato speculativo. Annullamento, fallimento tardivo e ramificazioni preservano lo stato completo.

L'asset v17 conserva sia i record di ugello sia quelli di fasatura. JSON, CLI e MCP condividono lo stesso
modello; schema e compilatore controllano unità, limiti, estremi finiti e compatibili e
la proprietà della fasatura. Le destinazioni Core restano net10.0/netstandard2.1 senza dipendenze.

## Esperimento ed evidenza

`metered-fired-cylinder` sostituisce l'aspirazione di carburante premiscelato con un ingresso di sola aria e un
binario gassoso finito. Un iniettore ideale fornisce richieste esplicite di 8/12/4 mg attraverso
una finestra di manovella prima della combustione di Wiebe prescritta. Le variazioni di dose sono campionate alla
finestra osservata successiva. L'esperimento di sei decimi di secondo eroga 28 mg, brucia circa
27.930 mg e rilascia circa 1228.918 J; il carburante incombusto e quello perso al confine restano nel
conto dei costituenti. Tutti i parametri sono sintetici e `unverified`.

I controlli coprono l'erogazione esatta limitata dalla quota, l'esaurimento del binario finito, la pressione inversa,
l'aggancio del comando a metà finestra, l'inversione senza riemissione della quota, un'ODE indipendente di
massa/entalpia a due recipienti con raffinamento liscio, la combustione dosata analitica, le transazioni complete,
capacità di stato/unità/chimica e l'avanzamento senza allocazioni. Report/portatile/MCP concordano
a ogni confine. I canali separati di erogazione e combustione distinguono un comando accettato
dal carburante e dal calore effettivi. Vedi [VALIDATION.it.md](VALIDATION.it.md) per i limiti
misurati e l'evidenza di runtime/prestazioni. Le viste dello Studio e i test Edit/Play sono preparati
in sorgente C# 9; l'accettazione reale di Unity Editor e Player resta in sospeso.
