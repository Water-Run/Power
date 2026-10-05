# Tabella di marcia di sviluppo di Power!

[English](ROADMAP.md) · [简体中文](ROADMAP.zh-CN.md) · [Français](ROADMAP.fr.md) · [Русский](ROADMAP.ru.md) · [日本語](ROADMAP.ja.md) · [한국어](ROADMAP.ko.md) · [Deutsch](ROADMAP.de.md) · [Español](ROADMAP.es.md) · **Italiano** · [Português](ROADMAP.pt-BR.md)

L'obiettivo è la piattaforma completa di gruppi motopropulsori Power!: fisica C# moderna, uno studio Unity 3D e l'operazione diretta da parte degli agenti. Un laboratorio sintetico superato stabilisce un risultato numerico limitato; motore, trasmissione, controlli, calibrazione del veicolo e accettazione desktop hanno ciascuno bisogno della propria evidenza.

## Traguardi

| Traguardo | Fondazione disponibile | Lavoro ancora necessario |
|---|---|---|
| Nucleo gestito | Fisica a doppio target priva di dipendenze, topologia, unità, tempo intero, replay e transazioni atomiche | Validazione integrata di lungo periodo del gruppo motopropulsore |
| Interfaccia agente | Strumenti MCP definiti da schema, diagnostica strutturata, revisioni, rami, annullamento e report compatti | Flussi di modellazione/controllo per l'ambito restante del gruppo motopropulsore completo |
| Studio Unity | Importazione condivisa dei modelli, riproduzione dei laboratori, componenti schematici 3D e test preparati | Evidenza reale di Editor/Play/Player/IL2CPP e pacchettizzazione desktop |
| Banco di modellazione | Asset portabile v26, lettori v1-v25 e definizioni JSON/CLI/MCP condivise | Modifica del grafo, salvataggio e grafici dei canali selezionabili |
| Fisica del motore | Massa/energia del gas indipendenti, lavoro biella-manovella, valvole fasate, combustione prescritta, dosatura di carburante gassoso e liquido, binari finiti cedevoli, evaporazione del film e azionamento fisico dell'ago; [Rail di combustibile liquido alimentato da pompa](PUMP_FED_FUEL.it.md) | serbatoio finito e regolazione misurata della pompa, comportamento magnetico/elettronico/a spruzzo raffinato, accoppiamento del volume liquido finito, controllo dell'accensione, aspirazione/scarico dettagliati, perdite meccaniche, termochimica e calibrazione misurata |
| Trasmissione | Frizioni accoppiate, ingranaggi/planetari, convertitore/blocco mappato, idraulica e percorsi DCT a sette avanti/retromarcia e Ravigneaux a quattro avanti/retromarcia con rotazione dei satelliti e inerzia orbitale risolte | Cedevolezza/perdite di ingranamento e ripartizione del carico, azionamento DCT, controllo completo di pressione/cambio AT e instradamento misurato, mappe misurate, comportamento di valvole/tenute/cavitazione e dinamica più ricca del convertitore |
| Controlli e integrazione elettrica | PI di pressione campionato, controllo di chiusura dell'ago e passaggio DCT graduale confermato dai sensori, tensione/duty cycle limitati, proprietà degli attuatori, circuito equivalente della batteria e accessori | Cicli ECU/TCU coordinati, sensori/attuatori, richieste di coppia, guasti, BMS e comportamento termico/elettrico misurato |
| Evidenza del veicolo e rilascio | Campioni di ricerca con confini completi e provenienza | Due gruppi motopropulsori misurati completi, budget di incertezza, stabilità, accettazione desktop e distribuzione |

I punti di controllo numerici attuali, le fixture di asset autentiche e i registri di verifica specifici della piattaforma stanno in [VALIDATION.it.md](VALIDATION.it.md). Vedi [DEVELOPMENT_STATUS.it.md](DEVELOPMENT_STATUS.it.md) per lo stato di implementazione e [ARCHITECTURE.it.md](ARCHITECTURE.it.md) per gli invarianti. L'evidenza CI pubblicata vale per la revisione registrata; le nuove modifiche locali richiedono un'accettazione di piattaforma separata.

## Prossimo lavoro gestito

Estendere il rail alimentato da pompa con serbatoio finito, regolazione misurata e azionamento magnetico/elettronico raffinato. Massa della sorgente finita e cedevole ed energia di pressione, moto reale dell'ago, retroazione di dose campionata, reintegro del film ed evaporazione sono implementati. Vedi [il contratto dell'ago](NEEDLE_ACTUATION.it.md) e [la predizione di chiusura limitata](CLOSURE_PREDICTION.it.md). Il ricevitore esporta ancora il lavoro di pressione di spostamento sotto il confine dichiarato di volume liquido trascurabile; lo spray e lo spostamento risolti devono sostituirlo con geometria verificata e accoppiamento di quantità di moto e di lavoro. Tieni separati erogazione, disponibilità del vapore e reazione prescritta, e conserva l'evidenza analitica, di conservazione e di convergenza. [Rail di combustibile liquido alimentato da pompa](PUMP_FED_FUEL.it.md)

Poi estendi accensione/controllo, dinamica di aspirazione/scarico e perdite meccaniche del motore. La combustione di Wiebe attuale è prescritta e non stabilisce combustione predittiva, detonazione, emissioni o calibrazione OEM. Il [contratto di dosatura gassosa](FUEL_METERING.it.md) resta un percorso supportato indipendente.

Costruisci sul [grafo DCT a sette avanti/retromarcia](DUAL_CLUTCH_TRANSMISSION.it.md) con azionamento dettagliato di sincronizzatore, innesto a denti e frizione. Costruisci sul [grafo Ravigneaux](RAVIGNEAUX_TRANSMISSION.it.md) con [proprietà dei satelliti e comportamento di ingranamento misurati](RESOLVED_PLANETS.it.md), [azionamento completo a stantuffo alimentato dalla pompa](AT_HYDRAULIC_ACTUATION.it.md) e controllo AT usando i primitivi accoppiati di convertitore, ingranaggi, frizioni e idraulica. Costruisci sul [controllo DCT campionato](DCT_CONTROL.it.md) verso passaggi con coppia miscelata e coordinamento ECU/TCU limitato, comprese richieste di coppia, sensori/attuatori e guasti recuperabili. Estendi i modelli di perdita costante della pompa, di batteria e di valvola/accumulatore quando sono disponibili dati misurati di proprietà e di controllo; i valori di ricerca forniti restano `unverified`.

## Studio e accettazione misurata

Imposta `POWER_UNITY_EDITOR` sull'Editor fissato ed esegui `unity-test`. Ottieni evidenza reale di importazione/Play/rendering e poi evidenza Player/IL2CPP. Selezione generale dei canali, modifica del grafo e salvataggio restano funzionalità separate dello Studio. CLI, MCP e Unity devono continuare a consumare la stessa semantica di modello.

EA211 DJS + DQ200 e PSA EC5 + AT8 conservano i confini completi del gruppo motopropulsore, l'applicabilità al veicolo e i manifest di evidenza. Le misure OEM mancanti non sono sostituite da valori predefiniti silenziosi. Completamento funzionale, correttezza numerica e credibilità del veicolo misurato richiedono accettazioni separate.

L'[archivio Zig](NATIVE_ZIG.it.md) conserva gli hash originali e la provenienza Git in `legacy/native/migration-manifest.json`, compresa la revisione C originale `c342d4c`. Resta separato dall'applicazione attiva C#/Unity. La migrazione nativa non completa la migrazione delle funzionalità gestite né l'accettazione del gruppo motopropulsore. Introduci parallelismo aggiuntivo, soluzione sparsa o Burst quando le misure lo giustificano e i contratti del nucleo restano stabili.

## Retroazione AT idraulica

`at_controller` accetta una marcia richiesta intera in [-1,4]; zero indica folle. Gestisce cinque coppie di valvole di riempimento/scarico e il blocco facoltativo del convertitore. L'ordine è ingresso del portasatelliti, solare piccolo, solare grande, freno del portasatelliti, freno del solare grande, poi blocco.

`controlled-hydraulic-ravigneaux` e `controlled-fired-hydraulic-ravigneaux` usano il canale 900 e l'ID 1400. Conservano 99 e 122 stati dichiarati entro il limite invariato di 128. v26 conserva percorsi, guadagni e clock e legge v1-v25.

Sono controlli di ricerca e i parametri restano `unverified`. Coordinamento della coppia ECU, sensori/valvole dettagliati, guasti completi del veicolo e calibrazione OEM restano aperti. Le verifiche managed e Standard non provano l'accettazione reale Unity Editor/Play/Player/IL2CPP.

[AT_CONTROL.it.md](AT_CONTROL.it.md)

## Rail di combustibile liquido alimentato da pompa

`liquid_rail_feed` associa un iniettore liquido a una pompa volumetrica esistente e a un confine esplicito di materia/calore. Il nodo di uscita idraulica deve corrispondere alla cedevolezza e alla pressione assoluta iniziale del rail. Pompa e iniettore possiedono questo nodo; altri percorsi fluidi non contabilizzati sono rifiutati.

v26 conserva collegamenti e temperatura sorgente e legge v1-v25. Scambio analitico albero/pressione, raffinamento ODE simultaneo indipendente, miscelazione termica, bilanci massa/combustibile/energia/volume, ritorno e rollback completo hanno verifiche separate.

La sorgente è un confine esterno esplicito, non un serbatoio finito modellato. Svuotamento, efficienza/regolazione pompa, perdite tubazioni, cavitazione, proprietà dipendenti dalla pressione e spray a volume finito restano aperti. Parametri `unverified`; non si stabilisce calibrazione OEM o accettazione reale Unity Editor/Play/Player/IL2CPP.

[PUMP_FED_FUEL.it.md](PUMP_FED_FUEL.it.md)
