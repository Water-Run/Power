# Rete del convertitore di coppia quasi stazionario

[English](CONVERTER_NETWORK.md) · [简体中文](CONVERTER_NETWORK.zh-CN.md) · [Français](CONVERTER_NETWORK.fr.md) · [Русский](CONVERTER_NETWORK.ru.md) · [日本語](CONVERTER_NETWORK.ja.md) · [한국어](CONVERTER_NETWORK.ko.md) · [Deutsch](CONVERTER_NETWORK.de.md) · [Español](CONVERTER_NETWORK.es.md) · **Italiano** · [Português](CONVERTER_NETWORK.pt-BR.md)

`torque_converter` partecipa alla stessa risoluzione di alberi, motori, cilindri, frizioni e ingranaggi ideali. Pompa e turbina sono nodi rotazionali distinti con inerzia esplicita. Lo statore è massa ferma; la sua reazione è osservabile, ma non compie lavoro. La perdita di fluido alimenta un nodo termico facoltativo o il registro esterno di rigetto del calore. Una `clutch` parallela separata fornisce il blocco. Tutti i parametri del laboratorio sono sintetici e `unverified`.

## Mappe ed equazioni esplicite

Sono richieste quattro mappe: `pump_positive`, `pump_negative`, `turbine_positive` e `turbine_negative`. Il membro con velocità assoluta maggiore è il conduttore di riferimento; in un pareggio esatto vince la pompa. Il segno di quel membro seleziona la sua mappa positiva o negativa. È una convenzione matematica del membro di riferimento, anche durante la controrotazione. Non deduce una caratteristica di retromarcia o di trascinamento non disponibile.

Per velocità del conduttore `wD` e velocità del condotto `wF`, `s = wF/wD` sta in `[-1,1]`. Ogni mappa contiene 2–32 punti espliciti, con `speed_ratio` adimensionale, `torque_ratio` R e `capacity_coefficient` C in `nm_s2_rad2`. C moltiplica la velocità al quadrato; non è un K-factor inverso. R e C interpolano in modo lineare e non sono mai estrapolati. Entrambe le velocità nulle producono reazioni nulle e modalità zero.

```text
Tdriver   = -C(s) * wD * abs(wD)
Tfollower =  R(s) * C(s) * wD * abs(wD)
Tstator   = -(Tpump + Tturbine)
Qdot      = C(s) * abs(wD)^3 * (1 - s*R(s))
```

La compilazione richiede nodi strettamente crescenti che coprono `[-1,1]`, C e R non negativi, e `s*R(s) <= 1` su ogni segmento. Controllare solo i nodi non basta: R lineare in s rende l'efficienza quadratica; si controlla anche ogni massimo interno. Le pendenze devono essere finite. A `s=1`, C deve essere zero e R uno, così la coppia di fluido è nulla a velocità uguali e nello stesso verso. A `s=-1`, le mappe pompa-positiva/turbina-negativa e pompa-negativa/turbina-positiva devono dare reazioni fisiche coincidenti. Questo impedisce un salto quando il membro di riferimento cambia durante la controrotazione. Il confronto agli estremi ammette solo la tolleranza di arrotondamento `64*epsilon*(abs(a)+abs(b))`. Gli array delle mappe sono posseduti e immutabili; i coefficienti normalizzati entrano nell'impronta del modello.

Queste restrizioni definiscono l'attuale modello passivo a mappe con segno di Power!. Non pretendono di coprire curve di convertitore misurate arbitrarie. Le convenzioni generali di guida e trascinamento basate su mappe sono documentate da [MathWorks Torque Converter](https://www.mathworks.com/help/sdl/ref/torqueconverter.html) e dal suo [esempio a due modalità](https://de.mathworks.com/help/sdl/ug/torque-converter-two-mode.html). Le quattro mappe con segno, la validazione dell'interpolazione e il solver qui sotto sono progetto di Power!; da quei riferimenti non è stato copiato codice sorgente né un insieme di parametri misurati.

## Integrazione accoppiata e limiti

Le reazioni del convertitore usano le velocità al punto medio dell'intervallo. Quando sono presenti cilindri, un sistema di Newton congiunto risolve il loro lavoro discreto sull'angolo di manovella e entrambe le velocità di porta del convertitore. Ogni risposta a coppia unitaria include il sistema elettromeccanico e la proiezione degli ingranaggi permanenti. Le iterazioni dei vincoli di frizione chiamano questa stessa risoluzione non lineare; la suddivisione per cattura e inversione rigenera le risposte dell'intervallo. Non c'è una coppia di convertitore ritardata applicata dopo l'integrazione del cilindro o della frizione.

La risoluzione non lineare ha 24 iterazioni e 12 tentativi di dimezzamento della ricerca lineare per iterazione. La tolleranza del residuo angolare del cilindro è `2e-14 rad`. Un residuo di velocità del convertitore usa `2e-12 + 64*epsilon*(abs(value)+abs(free_prediction)) rad/s`. Jacobiani analitici della mappa a tratti e derivate alle differenze finite del lavoro del cilindro costruiscono il sistema congiunto. Restano i limiti esistenti di corsa del cilindro di 0.25 rad, di risoluzione valvola/combustione, di iterazione ed eventi della frizione e di vincolo degli ingranaggi. Sono supportati al più otto convertitori, dentro i budget esistenti di 32 nodi, 64 componenti e 64 stati. Ogni convertitore aggiunge quattro stati logici di storia osservabile: due coppie medie, la potenza termica media e il calore cumulativo. Anche la somma di calore compensata partecipa a copia, hash e rollback.

Il calore dell'intervallo accettato è `-h*(Tp*wp_mid + Tt*wt_mid)`, quindi il lavoro meccanico sottratto è il calore registrato. Calore negativo oltre la tolleranza di arrotondamento della velocità risolta rifiuta l'intervallo; solo un residuo negativo dell'ordine dell'arrotondamento viene portato a zero. Coppie e potenza termica sono pesate sulla durata degli intervalli interni accettati, poi divise per il tick intero. Le prove speculative di evento non confermano mai il loro calore o le loro reazioni. Tutto lo stato di convertitore, frizione, ingranaggio, gas, storia di combustione, input e registro globale torna indietro su un batch fallito o annullato. I fork possiedono i propri spazi di lavoro. I test esercitano la cattura senza allocazioni.

L'integrazione al punto medio è del secondo ordine per il moto regolare di un convertitore isolato. Il modello acceso accoppiato conserva l'accoppiamento esplicito della temperatura di parete e il distacco della frizione sulla media dell'intervallo, quindi non rivendica un ordine due uniforme attraverso tutte le transizioni. Affina i tick per un fallimento numerico o uno studio di accuratezza; controlla le pendenze delle mappe, le scale di inerzia e velocità e i vincoli di frizione prima di ricreare una sessione. Una mappa valida non garantisce che ogni passo scelto sia risolvibile.

## Contratto di modello condiviso e osservabile

JSON usa `node_a` obbligatorio (pompa), `node_b` (turbina), quattro array sotto `parameters` e `heat_node` facoltativo. Non accetta un canale di input del convertitore, una porta del portasatelliti o default impliciti di mappa. `ComponentDefinition.TorqueConverter` espone lo stesso modello di Core. Gli errori di mappa portano l'ID del componente e un campo azionabile come `converter.pump_positive` o `converter.counter_rotation`.

| Campo | Significato | Unità |
|---|---|---|
| `torque` | Coppia media della pompa sull'ultimo tick completo | Nm |
| `torque_at_b` | Coppia media della turbina sull'ultimo tick completo | Nm |
| `torque_at_c` | Reazione media dello statore fermo sull'ultimo tick completo | Nm |
| `heat_flow` | Potenza termica media del fluido sull'ultimo tick completo | W |
| `fluid_heat` | Calore di fluido accettato cumulativo | J |
| `speed_ratio` | Rapporto di velocità con segno condotto/conduttore attuale; zero da fermo | fraction |
| `converter_drive` | 0 fermo, 1 pompa positiva, 2 pompa negativa, 3 turbina positiva, 4 turbina negativa | state code |

Coppie e potenza medie partono da zero. Gli aggiornamenti di input al confine non riscrivono le uscite medie del tick precedente. `torque_at_c` nomina qui la reazione dello statore; questo componente non ha un terzo rotore. I modelli che contengono un convertitore dichiarano `quasisteady_converter_powertrain` e aggiungono il tag di impronta 10. Impronte e traiettorie dei modelli senza convertitore restano invariate. L'asset portabile v10 conserva tutte e quattro le mappe; i lettori v1–v9 e i fixture autentici restano.

## Laboratorio ed evidenze

Richiedi l'esempio MCP `fired-converter`, oppure esegui:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-converter.power.json --output artifacts/reports/fired-converter.json
```

Il laboratorio di 0.8 s parte con una pompa a 600 rpm e una turbina a 300 rpm, che alimentano il solare di un planetario e una riduzione finale 3:1. Il freno della corona seleziona la riduzione; una frizione solare/corona seleziona la presa diretta. Blocco, rilascio e ricattura programmati in modo indipendente esercitano i percorsi di fluido e di attrito. Sono eventi prescritti, non un controllore di cambio automatico.

Con tick da 50,000 ns, tutti gli 87 limiti del report si rieseguono in modo esatto attraverso CLI, asset portabili e MCP. L'impronta del modello è `839d03901973668d` e l'hash di stato finale è `834a679376b7a6fd`. La velocità finale di pompa e turbina è circa 73.37748 rad/s, la velocità del carico 6.988331 rad/s, il calore di fluido 24.27663 J e il calore di blocco 22.84709 J. La frizione di cambio e il freno aggiungono 157.18199 J e 83.42289 J. Il nodo termico condiviso raggiunge 301.438643 K; il residuo energetico totale è circa `3.30e-11 J`. Questi numeri descrivono un transitorio sintetico.

I test indipendenti coprono la soluzione analitica a due inerzie `C(s)=k(1-s), R=1`, il decadimento analitico allo stallo, l'affinamento del secondo ordine, l'instradamento termico ed esterno del calore, il bilancio dello statore, gli stati di retromarcia, trascinamento e controrotazione, le porte di convertitore condivise, la riflessione degli ingranaggi, il blocco, il rollback dell'intero batch, l'annullamento, i fork e zero allocazioni. L'affinamento del modello acceso controlla una distanza combinata adimensionale di velocità finale, calore di fluido e calore di blocco rispetto a un'esecuzione a 3,125 ns, usando cinque dimensioni di tick. Limita anche le differenze assolute sotto 0.0002 rad/s o J, rispettivamente. Gli errori di calore singoli non devono diminuire a ogni dimezzamento vicino agli eventi. Questo controllo è separato dall'ordine analitico del caso isolato. Proprietà e validazione delle mappe con segno, e i record portabili malformati rifirmati, hanno controlli dedicati.

La [rete idraulica](HYDRAULIC_NETWORK.it.md) fornisce ora capacità di blocco e di cambio derivate dalla pressione. Quantità di moto angolare del fluido, dinamica di riempimento e pressione del convertitore, meccanica dello statore rotante o in ruota libera, proprietà dipendenti dalla temperatura, dinamica di pompa, regolatore e stantuffo, topologia DCT/AT completa e coordinamento ECU/TCU restano aperti. Restano aperti anche il comportamento completo del motore, le misure OEM e la calibrazione del veicolo. Studio ha porte di fluido schematiche e test preparati; l'evidenza reale di Editor, Play e IL2CPP resta separata e non è disponibile in questo ambiente.
