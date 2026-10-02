# Film di carburante liquido finito ed evaporazione

[English](FUEL_FILM.md) · [简体中文](FUEL_FILM.zh-CN.md) · [Français](FUEL_FILM.fr.md) · [Русский](FUEL_FILM.ru.md) · [日本語](FUEL_FILM.ja.md) · [한국어](FUEL_FILM.ko.md) · [Deutsch](FUEL_FILM.de.md) · [Español](FUEL_FILM.es.md) · **Italiano** · [Português](FUEL_FILM.pt-BR.md)

`fuel_film` memorizza un inventario liquido iniziale esplicito accanto a un ricevitore gas
tracciato. Un nodo termico finito fornisce calore sensibile e di cambiamento di fase. Il carburante
evaporato si unisce a massa, energia interna e costituente carburante del ricevitore; la reazione
premiscelata esistente consuma solo il vapore. L'inventario liquido è bagnatura iniziale,
non carburante iniettato, e resta parte del registro di massa totale ed energia chimica.

È un modello di ricerca a proprietà costanti, con volume di spostamento del liquido trascurabile
e una temperatura di saturazione prescritta. Non implementa dinamica di binario liquido,
ago o spruzzo, equilibrio di fase dipendente dalla pressione, condensazione,
proprietà multicomponente del carburante o comportamento della benzina calibrato.

## Energia di fase e sorgente di calore finita

Sia `c_l` il calore specifico del liquido, `c_v` la capacità termica isocora del gas del ricevitore,
`T_s` la temperatura di saturazione dichiarata e `L_u > 0` la differenza di energia interna
specifica vapore meno liquido a `T_s`. Il riferimento termico condiviso è:

```text
e_offset = (c_v - c_l) T_s - L_u
U_liquid = m_liquid (c_l T_liquid + e_offset)
U_vapor_added = delta_m c_v T_s
```

`L_u` è una differenza di energia interna in J/kg, piuttosto che un'entalpia di
vaporizzazione. Un'entalpia fornita ha bisogno di una conversione esplicita e giustificata prima
di poter essere usata qui. L'energia termica del liquido può essere negativa sotto questo riferimento;
temperatura e massa devono comunque essere fisicamente ammissibili. L'energia chimica
`m_liquid * LHV` è separata e si trasferisce col vapore senza creare calore di reazione
o lavoro di sorgente esterno.

Sotto la saturazione, la conduttanza `K` accoppia la capacità del liquido `m_liquid c_l` alla capacità
di parete finita `C_w`. La differenza di temperatura decade analiticamente al tasso
`K (1 / (m_liquid c_l) + 1 / C_w)`. La temperatura media pesata sulle capacità resta
costante. Se il liquido raggiunge `T_s`, la legge risolve quell'istante e usa l'intervallo
restante per l'evaporazione.

A saturazione con `T_wall > T_s`, il surriscaldamento della parete decade al tasso `K / C_w`.
Il calore di fase disponibile sull'intervallo `h` è
`C_w (T_wall - T_s) (1 - exp(-K h / C_w))`, limitato da `m_liquid L_u`.
Il film resta a `T_s` finché è asciutto; la massa evaporata è il calore di fase diviso per
`L_u`. L'essiccamento lascia massa ed energia liquide esattamente nulle e interrompe il prelievo di calore.
Una parete fredda può raffreddare il liquido esistente; non condensa il vapore del ricevitore.

Ogni trasferimento soddisfa `delta U_liquid + U_vapor_added = Q_from_wall`.
La parete perde quello stesso calore, quindi il cambiamento di fase non introduce un confine
energetico esterno. Gli inventari di carburante non negativi e il registro completo dei costituenti
sono controllati in modo indipendente dal registro di energia totale.

## Contratto di grafo e documento

| Dato | Requisito |
|---|---|
| `node_a` | Ricevitore gas finito con tracciamento esplicito del carburante premiscelato e LHV |
| `node_b` | Parete termica finita, con capacità e temperatura positive |
| `initial_mass` | kg non negativi; l'inventario liquido iniziale completo |
| `initial_temperature` | K positivi, non superiori alla saturazione |
| `liquid_specific_heat` | J/(kg K) positivi, unità JSON `j_kg_k` |
| `saturation_temperature` | K positivi, unità JSON `k` |
| `latent_internal_energy` | J/kg positivi, unità JSON `j_kg` |
| `conductance` | W/K non negativi, unità JSON `w_k` |

JSON richiede tutti e sei i parametri. Il film non ha canale di input, fasatura di manovella o
pozzo di calore separato. Parametri non correlati, domini di porta errati, unità, valori
non finiti e capacità di stato non supportata sono rifiutati. I chiamanti Core usano
`ComponentDefinition.LiquidFilm` e `FuelFilmDefinition`; la legge indipendente
`EquilibriumFuelFilm` espone la creazione di stato ammissibile e l'avanzamento a bagno finito.

Ogni film contribuisce cinque voci di stato riportate al budget di stato limitato del compilatore.
Massa, energia termica, storia di evaporazione, flusso medio, calore di parete e
le loro storie compensate sono di proprietà della simulazione. Annullamento, input rifiutati,
fallimenti tardivi del solver, ramificazioni indipendenti e intervalli di frizione speculativi preservano
la transazione completa. L'avanzamento riuscito e le letture di snapshot non allocano
memoria gestita dopo il riscaldamento.

## Accuratezza dell'integrazione

Un intervallo accettato usa mezzi passi film / gas / meccanica e reazione / gas / film.
I film che condividono una parete girano in ordine stabile dei componenti prima dell'avanzamento
del gas e in ordine inverso dopo. La loro temperatura di parete finita è trasportata
fra i sottopassi del film, e il calore di parete entra nella stessa soluzione termica.

La legge isolata del bagno finito è analitica attraverso riscaldamento sensibile, saturazione ed
essiccamento. L'integrazione ODE simultanea indipendente verifica il raffinamento liscio del secondo
ordine per due film che condividono una parete e per il vapore trasportato attraverso un'uscita
gas bloccata, senza altre sorgenti di calore di parete. I legami termici del gas
e le altre sorgenti termiche leggono ancora la temperatura di parete esplicita dell'intervallo esterno,
quindi quell'accoppiamento conserva l'accuratezza del primo ordine. Finestre di reazione,
eventi di valvola ed essiccamento hanno bisogno dei propri controlli di raffinamento; il solo replay esatto del batch
non dimostra l'accuratezza del passo temporale né un secondo ordine uniforme per un gruppo motopropulsore acceso.

## Semantica osservabile e portatile

| Campo del film | Significato |
|---|---|
| `mass` | Carburante liquido restante, kg |
| `temperature` | Temperatura del liquido; temperatura di saturazione dichiarata quando è asciutto |
| `internal_energy` | Energia termica liquida con segno sotto il riferimento di fase dichiarato, J |
| `chemical_energy` | Energia chimica del carburante liquido restante, J |
| `evaporated_fuel_mass` | Vapore erogato cumulativo, kg |
| `mass_flow` | Erogazione media di vapore sull'ultimo tick fisico completo, kg/s |
| `film_wall_heat` | Calore cumulativo prelevato dalla parete, J; il raffreddamento può renderlo negativo |
| `heat_flow` | `K (T_wall - T_liquid)` istantaneo, W; nullo quando è asciutto |

Scopri ID e unità di uscita attraverso la validazione o la creazione della sessione. Gli osservabili
globali di massa, carburante ed energia chimica includono l'inventario del film. L'erogazione interna
di vapore non incrementa l'energia di carburante del serbatoio né l'entalpia esterna.

L'asset v19 memorizza tutte le proprietà di fase e conserva i lettori v1-v18. Ogni film richiede
un record di fase tipizzato da 64 byte. Lunghezze/conteggi limitati, digest, copertura completa,
record duplicati/mancanti, unità, compilazione fisica e protezione dal declassamento
sono controllati. La fixture autentica di dosatura carburante v17 conserva la sua impronta e il
replay aggiornato nello stesso runtime. Vedi [ASSET_FORMAT.it.md](ASSET_FORMAT.it.md).

## Laboratorio e lavoro restante

`film-fired-cylinder` riscalda un film inizialmente bagnato, ammette aria separatamente, poi
consuma il vapore disponibile attraverso la combustione di Wiebe prescritta. La parete calda finita
paga il calore di fase; il liquido non brucia direttamente. JSON, CLI, replay portatile e
il server MCP reale concordano a ogni confine di report. I contratti di sorgente, schema e sessione
restano condivisi; i parametri sono `unverified`.

Vedi [VALIDATION.it.md](VALIDATION.it.md) per l'evidenza numerica misurata. I marcatori del film
Unity preparati e i controlli del ciclo di vita richiedono ancora la verifica reale di Editor/Play.
L'[iniettore liquido](LIQUID_FUEL_INJECTION.it.md) separato reintegra ora i film da
una sorgente finita e cedevole. Pompa/rabbocco, ago/spruzzo, proprietà del carburante misurate,
accensione/ECU, comportamento completo di aspirazione/scarico, controlli della trasmissione e gruppi
motopropulsori calibrati restano requisiti separati.
