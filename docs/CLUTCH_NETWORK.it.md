# Simulazione accoppiata della frizione

[English](CLUTCH_NETWORK.md) · [简体中文](CLUTCH_NETWORK.zh-CN.md) · [Français](CLUTCH_NETWORK.fr.md) · [Русский](CLUTCH_NETWORK.ru.md) · [日本語](CLUTCH_NETWORK.ja.md) · [한국어](CLUTCH_NETWORK.ko.md) · [Deutsch](CLUTCH_NETWORK.de.md) · [Español](CLUTCH_NETWORK.es.md) · **Italiano** · [Português](CLUTCH_NETWORK.pt-BR.md)

Il componente gestito `clutch` collega due nodi rotazionali, oppure un rotore alla massa. Partecipa alla risoluzione elettromeccanica e del cilindro già esistente e instrada il calore di attrito generato verso un nodo termico o il registro esterno del calore. JSON, CLI, MCP, l'asset v10 e Studio usano le stesse definizioni. Questo realizza un elemento di accoppiamento della trasmissione; la topologia DCT/AT completa, la dinamica di pompa e stantuffo e il coordinamento ECU/TCU restano lavoro separato. La [rete idraulica](HYDRAULIC_NETWORK.it.md) comanda ora una variante di frizione azionata dalla pressione. Il [convertitore mappato](CONVERTER_NETWORK.it.md) condivide questa risoluzione e usa una frizione parallela separata per il blocco.

Il [contratto fisico della frizione a secco](CLUTCH_PHYSICS.it.md) definisce la legge di Coulomb e un riferimento esatto e indipendente a due inerzie sotto carichi costanti. Il solver del grafo qui sotto estende quella legge alle reti accoppiate. Non congela la coppia del motore o la corrente del motore elettrico in un ingresso unidirezionale verso la frizione.

## Definizione e canali

```json
{
  "id": 16,
  "kind": "clutch",
  "node_a": 1,
  "node_b": 4,
  "heat_node": 5,
  "input_channel": 104,
  "initial_input": { "value": 0, "unit": "fraction" },
  "parameters": {
    "static_capacity": { "value": 100, "unit": "nm" },
    "sliding_capacity": { "value": 30, "unit": "nm" },
    "ratio": 1
  }
}
```

`node_a` è un nodo rotazionale. `node_b` è un nodo rotazionale distinto, oppure è omesso o zero per un freno a massa. `ratio` è finito e non nullo; la massa richiede uno. La capacità statica è almeno pari alla capacità strisciante, ed entrambe sono coppie finite e non negative alla porta A. `initial_input` è una frazione di innesto esplicita in `[0,1]`, che scala entrambe le capacità. Un canale di input facoltativo cambia l'innesto sugli estremi esatti dei tick esterni. Un `heat_node` omesso o zero invia il calore al registro esterno di rigetto; un pozzo fornito deve essere un nodo termico. La temperatura non altera queste capacità.

La factory di Core è `ComponentDefinition.Clutch(id, a, b, staticCapacity, slidingCapacity, channel, engagement, ratio, heat)`. Il suo descrittore `Friction` contiene le due quantità di coppia esplicite. I modelli compilati copiano i parametri, ordinano per ID stabile e aggiungono il tag di impronta 8 solo quando sono presenti frizioni. Ogni fase di frizione conta nel limite esistente di 64 stati. I modelli precedenti conservano impronte e hash di replay. Il nome di fedeltà combinato è `hybrid_clutch_powertrain`, con calibrazione ancora `unverified`.

| Campo | Unità | Significato |
|---|---|---|
| `slip_speed` | rad/s | `omega_A - ratio*omega_B` attuale |
| `clutch_mode` | StateCode | Fase dell'ultimo intervallo accettato: 0 disinnestata, 1 bloccata, 2 slittamento positivo, 3 slittamento negativo |
| `torque` | Nm | Reazione media in A sull'ultimo tick esterno completo |
| `heat_flow` | W | Potenza di attrito generata media su quel tick |
| `friction_heat` | J | Calore generato cumulativo, a prescindere dalla destinazione |

Coppia, potenza e calore iniziali sono zero; la fase iniziale si deduce dall'innesto e dalla velocità relativa, prima di risolvere una reazione al carico. Una fase descrive l'intervallo risolto, quindi un arrivo esattamente sul suo estremo può ancora mostrare la fase in avvicinamento fino alla risoluzione successiva. Un input di confine cambia subito lo stato di input e non riscrive la storia delle uscite dell'intervallo precedente. Vale anche per gli eventi di input alla fine di una chiamata `Step`. Le medie di coppia e potenza includono ogni intervallo interno accettato.

## Integrazione accoppiata ed eventi

Per `g = omega_A - r*omega_B`, le coppie di porta sono `tau_A = tau`, `tau_B = -r*tau`. La potenza meccanica sottratta è `-tau*g`; questa convenzione di segno funziona con entrambi i segni di `r`. Il disinnesto impone coppia nulla. Lo strisciamento usa la capacità cinetica che si oppone allo slittamento. Una frizione bloccata impone velocità relativa nulla al punto medio, con una reazione limitata dalla capacità statica. Si ottiene così lavoro ideale nullo in blocco, senza inserire uno smorzatore artificiale o una molla di penalità rigida.

Ogni intervallo interno usa le equazioni elettromeccaniche implicite al punto medio già esistenti e la risoluzione conservativa del lavoro di pressione del cilindro. Le risposte di forza della frizione si ottengono dagli stessi fattori lineari accoppiati. Una risoluzione di Gauss–Seidel proiettata determina le reazioni statiche limitate, mentre la coppia del cilindro viene ricalcolata per le forze correnti. I vincoli statici saturati si rilasciano quando il moto richiesto supera la tolleranza di velocità. L'insieme attivo viene riconsiderato se un altro vincolo cambia una direzione di partenza. I cicli di frizione ridondanti sono ammessi; le loro reazioni individuali possono non essere uniche. L'ordine stabile dei componenti seleziona un'allocazione deterministica, mentre i test controllano il moto risultante, i limiti di capacità, la quantità di moto totale e l'energia.

Se un intervallo di strisciamento inverte la velocità relativa, una bisezione limitata localizza il confine osservato di slittamento nullo e riesegue l'intervallo da una copia completa dello stato. L'intervallo successivo aderisce, oppure parte con la reazione cinetica opposta. Dinamica, fattori termici e risposte di forza del cilindro sono ricalcolati per ogni durata candidata; tutti i fattori mutabili appartengono alla singola simulazione. Il modello compilato resta immutabile.

La tolleranza del vincolo è `2e-13 + 32*epsilon*(abs(omega_A)+abs(r*omega_B))` rad/s, con `epsilon = 2.2204460492503131e-16`. La cattura accetta radici entro sedici volte quella tolleranza. Non proietta via uno slittamento finito e non scarta energia cinetica finita. Un lavoro di attrito negativo minuscolo, entro il doppio della tolleranza coppia-per-velocità dell'intervallo, viene portato a zero; un lavoro negativo più grande fallisce. I controlli di conservazione includono questo effetto di arrotondamento. Un residuo entro la tolleranza della radice non può creare un secondo evento spurio.

La risoluzione ammette al più 32 intervalli interni per tick esterno, 56 iterazioni di radice, 256 iterazioni di vincolo per insieme attivo e `2*clutch_count+2` tentativi di insieme attivo. Fattori non finiti, vincoli non convergenti, eventi non risolti, budget esauriti o i limiti esistenti di cilindro e gas restituiscono `NumericalFailure`. L'annullamento è controllato durante il lavoro limitato su vincoli e radici. Riduci il tick esterno e controlla le scale di inerzia e rapporto, i vincoli ridondanti e le leggi di capacità; non interpretare una chiamata fallita come un innesto completato in parte.

Il calore generato è integrato come `-duration*tau*g_mid`, poi aggiunto alla risoluzione termica o al registro esterno del calore. Tutto il lavoro di sorgente degli intervalli accettati, il trasporto di gas, la storia chimica, lo scambio di parete e il rigetto termico entrano nella contabilità energetica esistente. Fase della frizione, uscite medie, calore cumulativo e somma di calore compensata sono copiati e sottoposti a hash insieme allo stato fisico. Una chiamata su più tick fallita o annullata ripristina lo stato iniziale completo, compresi input programmati, fase e calore. I fork condividono solo i dati del modello compilato. Il tempo esterno resta un conteggio intero limitato di nanosecondi; le durate degli eventi interni non introducono tick esterni frazionari visibili.

La prova di distacco non lineare usa la richiesta di coppia media sull'intervallo. Non localizza l'istante esatto in tempo continuo in cui un carico statico variabile supera per la prima volta la capacità. Allo stesso modo, la parentesi di evento riguarda la traiettoria discreta al punto medio; un tick grande può perdere oscillazioni fisiche rapide i cui estremi nascondono un'inversione. Affina il tempo intorno alle transizioni e confronta le uscite. La dinamica elettromeccanica regolare conserva l'accuratezza del punto medio, l'accoppiamento termico e di parete resta del primo ordine, e non si rivendica un ordine due universale per tutte le traiettorie con commutazione.

## Laboratorio ed evidenze

Il [laboratorio fired-clutch](../assets/labs/fired-clutch.power.json) collega il cilindro a combustione premiscelata a un carico inerziale separato e a un nodo termico della frizione. Sei eventi su tick esatti applicano innesto parziale e pieno, coppia di carico, rilascio e reinnesto. I parametri sono sintetici. Su 0.6 secondi il report Linux attuale registra:

| Quantità | Risultato |
|---|---|
| Velocità finale motore/carico | 68.58488546 rad/s |
| Lavoro netto delle sorgenti esterne, compresi carico e contropressione del cilindro | -96.74607609 J |
| Calore generato dalla frizione | 191.55570747 J |
| Temperatura finale del nodo termico della frizione | 300.95777854 K |
| Calore del carburante rilasciato | 1,630.91064291 J |
| Slittamento finale | 2.84e-14 rad/s, fase bloccata |
| Residuo energetico finale | 1.79e-10 J |
| Impronta del modello / hash di stato finale | `197be44884deee90` / `28bf5335d8e35cde` |

Tutti i 67 limiti del report coincidono fra dimensioni di batch alternative, riproduzione portabile e replay reale del server figlio MCP. Il report è `artifacts/reports/fired-clutch.json`. Richiedi `get_example_model` con `name: "fired-clutch"`, oppure esegui:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-clutch.power.json --output artifacts/reports/fired-clutch.json
```

I controlli di Core confrontano innesto, frenata e inversione con `ClutchPair`, compresi i rapporti con segno ed entrambe le destinazioni del calore. Un motore RL bloccato coincide con un modello a inerzia combinata in modo analitico; un cilindro a gas reagente e bloccato coincide allo stesso modo con il suo modello indipendente a inerzia equivalente, compresi pressione e consumo di carburante. Un oscillatore molla/freno coincide con il moto sinusoidale analitico a tratti attraverso tre inversioni e un quarto punto di svolta che cattura; l'affinamento riduce l'errore di oltre 3.7 volte a ogni dimezzamento. I cicli a tre frizioni esercitano vincoli ridondanti e innesto simultaneo. I test coprono anche il rollback completo dopo un prefisso riuscito di riscaldamento e cattura, l'annullamento, il replay programmato esatto, la proprietà immutabile, l'indipendenza dei rami e il funzionamento senza allocazioni, compresi eventi interni di inversione ripetuti.

L'asset v10 conserva in ciclo completo le capacità esplicite e tutti i canali. Conteggi malformati, record mancanti, duplicati e di tipo errato, unità errate, limiti non validi e declassamenti contraffatti sono rifiutati. Un fixture autentico v6 del cilindro acceso conserva digest, impronta e replay aggiornato. I test JSON rigorosi e quelli dell'agente distinguono l'esecuzione riuscita dal superamento dei KPI. Vedi [VALIDATION.md](VALIDATION.it.md).

La build esporta `FiredClutch.powerasset`. Studio prepara due dischi di frizione schematici, colori di fase e un'uscita di fase con nome, insieme ai controlli di innesto e ai canali di calore. I test del ciclo di vita di importazione e Play sono preparati. L'evidenza reale di Unity Editor, rendering, Play Mode e IL2CPP resta in sospeso; i controlli degli assembly Standard ospitati da .NET non la sostituiscono.

## Accoppiamento permanente degli ingranaggi

I [vincoli di ingranaggio ideale e planetario](GEAR_NETWORK.it.md) proiettano ora il punto medio libero e le risposte di forza di frizione e cilindro nello stesso spazio di vincoli permanenti. Il laboratorio planetario acceso combina un freno della corona e una frizione solare/corona con un planetario ideale e una riduzione finale, e riesegue un passaggio in salita e uno in discesa. Una frizione la cui velocità relativa è già vincolata in modo permanente è rifiutata come reazione indipendente non definita. Gli altri contratti di stato, capacità, termica ed eventi della frizione restano invariati.
