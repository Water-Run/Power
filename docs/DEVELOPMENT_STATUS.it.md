# Stato di sviluppo

[English](DEVELOPMENT_STATUS.md) · [简体中文](DEVELOPMENT_STATUS.zh-CN.md) · [Français](DEVELOPMENT_STATUS.fr.md) · [Русский](DEVELOPMENT_STATUS.ru.md) · [日本語](DEVELOPMENT_STATUS.ja.md) · [한국어](DEVELOPMENT_STATUS.ko.md) · [Deutsch](DEVELOPMENT_STATUS.de.md) · [Español](DEVELOPMENT_STATUS.es.md) · **Italiano** · [Português](DEVELOPMENT_STATUS.pt-BR.md)

Power! ha un nucleo di simulazione gestito, documenti di modello condivisi e asset portabili, una CLI senza interfaccia, un servizio agente MCP e uno studio Unity preparato. I laboratori sintetici esercitano il comportamento del motore, della trasmissione, dell'idraulica e dell'elettrica. I gruppi motopropulsori completi, il controllo ECU/TCU coordinato, la calibrazione misurata e un'applicazione desktop Unity accettata restano incompiuti.

## Implementazione attuale

| Area | Implementato | Accettazione restante |
|---|---|---|
| Nucleo | Unità esplicite e ID stabili; compilazione immutabile; tempo intero limitato; registri osservabili; replay, annullamento, fork indipendenti e rollback dell'intero batch | Evidenza di lungo periodo e del gruppo motopropulsore completo |
| Motore | Cilindri chiusi/aperti, lavoro di pressione biella-manovella, flusso di gas bidirezionale, valvole fasate sull'albero a gomiti, calore di parete e combustione premiscelata prescritta | Aspirazione/scarico dettagliati, accensione, perdite meccaniche, termochimica più ricca e comportamento misurato del motore |
| Carburante | Carburante/aria/prodotti tracciati, binari gassosi finiti con dosatura per ciclo, binari liquidi finiti e cedevoli che alimentano i film, evaporazione pagata dalla parete e reazione solo del vapore; [Rail di combustibile liquido alimentato da pompa](PUMP_FED_FUEL.it.md) | geometria/ventilazione del serbatoio e riempimento/regolazione misurati, comportamento magnetico/elettronico/a spruzzo non lineare, equilibrio di fase dipendente dalla pressione e proprietà misurate del carburante |
| Trasmissione | Frizioni statiche/striscianti, azionamento a contatto, ingranaggi/planetari con segno, convertitore/blocco mappato e percorsi DCT a sette avanti/retromarcia e Ravigneaux a quattro avanti/retromarcia con rotazione dei satelliti e inerzia orbitale risolte | Cedevolezza/perdite di ingranamento e ripartizione del carico, azionamento DCT, controllo completo di pressione/cambio AT e instradamento misurato, cambi coordinati, perdite misurate e comportamento più ricco del convertitore |
| Idraulica | Volumi cedevoli, restrizioni, pompe con trafilamento/trascinamento espliciti, scarico, stantuffi dinamici, cursori dosati e accumulatori a gas a energia finita | Mappe misurate di valvole/accumulatori/pompe, attrito delle tenute, cavitazione e idraulica completa della trasmissione |
| Elettrica | Motori RL, solenoidi a induttanza variabile reciproca, batteria a carica finita, polarizzazione resistiva/RC, conversione di duty mediata e accessori | Comportamento chimico/termico misurato, BMS, controllo di corrente e integrazione completa dell'alimentazione |
| Controlli | PI di pressione campionato, retroazione dell'ago/predizione di chiusura e controllo DCT graduale confermato dai sensori, con proprietà degli attuatori, orologi interi e memoria transazionale | Coordinamento di coppia ECU/TCU, sensori, attuatori e gestione dei guasti |
| Documenti e asset | 40 laboratori JSON/CLI, 39 esempi MCP, asset v27 e lettori v1-v26 | Modifica/salvataggio e raccolte di modelli calibrati |
| Agenti | Dodici strumenti MCP definiti da schema; evidenza compatta, controlli di revisione e diagnostica azionabile | Flussi completi per l'ambito fisico/di controllo restante |
| Unity | Importazione dei modelli, riproduzione a tick esatti, componenti schematici 3D, controlli, reset e test del ciclo di vita preparati | Accettazione reale di Editor/Play, grafici selezionabili, modifica/salvataggio del grafo e Player/IL2CPP |
| Archivio nativo | Prototipi di ricerca Zig 0.15.2, ABI conservata e provenienza dei sorgenti originali | Riferimento storico; la migrazione gestita resta separata dalla funzionalità completa |

Core e Assets puntano sia a `net10.0` sia a `netstandard2.1`; il Core non ha dipendenze da Unity, trasporto, fornitore di modelli o terze parti. Gli script in Unity Assets usano C# 9. Unity carica gli assembly Standard compilati dall'SDK esterno; non compila sorgenti .NET 10/C# 14.

## Evidenza e limiti

Il [grafo di azionamento idraulico AT](AT_HYDRAULIC_ACTUATION.it.md) alimenta tutti e cinque gli elementi di gamma e il blocco opzionale del convertitore da una pompa condivisa comandata dall'albero. Percorsi espliciti di riempimento/scarico, moto finito dello stantuffo, molle di richiamo e capacità ricavata dalle pastiglie conservano il lavoro di spostamento idraulico e il comportamento reale di cattura/rilascio. Le valvole prescritte non sono un controllo AT confermato dai sensori né un'accettazione misurata del corpo valvole. Il comportamento completo del gruppo motopropulsore e l'accettazione misurata restano incompiuti.


Il `dotnet run --file tools/Build.cs -- verify` seriale richiesto passa in locale su Windows x64. Copre entrambi i target di assembly ospitati su .NET 10, un processo figlio MCP reale, tutti i report dei laboratori, l'archivio Zig e l'ABI C#. Conteggi attuali, percorsi dei log, controlli degli schemi, risultati numerici e provenienza CI conservata stanno in [VALIDATION.it.md](VALIDATION.it.md). I test degli assembly Standard su .NET 10 non stabiliscono la compatibilità con il runtime Unity.

L'[incremento del film liquido](FUEL_FILM.it.md) ha ora controlli analitici di riscaldamento/saturazione/essiccamento, riferimenti ODE simultanei indipendenti, conservazione di massa/chimica/termica, replay portabile e transazioni di sessione complete. L'ordinamento simmetrico dei film dà un raffinamento regolare del secondo ordine per i film che condividono una parete. L'accoppiamento ad altre sorgenti di calore di parete conserva il limite esistente di parete esplicita del primo ordine. Il replay esatto è separato dall'accuratezza del passo, dal superamento dei KPI e dalla fisica calibrata.

Il [grafo di ricerca Ravigneaux](RAVIGNEAUX_TRANSMISSION.it.md) aggiunge vincoli a pignone singolo/doppio, cinque percorsi di attrito, quattro gamme avanti e la retromarcia. Riferimenti indipendenti di massa libera, inerzia riflessa e cattura del freno controllano le reazioni alle porte e il calore. Gli esperimenti di coppia condivisa e di convertitore acceso conservano il replay completo e confini di ricerca espliciti. Il percorso ridotto omette la rotazione dei satelliti; idraulica dettagliata, controllo AT e topologia/calibrazione OEM restano incompiuti. L'[opzione risolta](RESOLVED_PLANETS.it.md) aggiunge quattro ingranamenti reali, due rotori di rotazione assoluta e inerzia orbitale esplicita; riferimenti indipendenti di massa a sei rotori, momento angolare e cattura conservano quelle energie. Il comportamento dettagliato di denti/lubrificazione/ripartizione del carico ha ancora bisogno di evidenza.

Il [regolatore DCT campionato](DCT_CONTROL.it.md) ora possiede i comandi di trazione/selettore, preseleziona i percorsi scarichi, attende la sincronizzazione/il blocco fisici ed esegue un passaggio esclusivo graduale di rilascio/innesto. Richieste di marcia intere, interruzione in folle, guasti di direzione/timeout/blocco persistente e recupero su nuova richiesta sono osservabili. La marcia confermata può essere temporaneamente zero durante uno slittamento transitorio anche dopo una conferma precedente. Le corse lunghe di marcia controllate usano coordinate compensate transazionali, con le tolleranze di fase strette invariate. Il limite esplicito di stato è 128; i limiti di nodi e componenti restano 32/64, e consentono la composizione accesa/regolatore da 70 stati. I passaggi completi con coppia miscelata, gli attuatori e i guasti ECU/TCU completi restano aperti.

L'[incremento a doppia frizione](DUAL_CLUTCH_TRANSMISSION.it.md) ora assembla sette percorsi avanti, una ruota folle di retromarcia sul percorso pari, tre rami di uscita/riduzione finale e selettori di attrito espliciti. Riferimenti indipendenti di inerzia con segno/riflessa e di impulso/calore di preselezione verificano i percorsi di potenza. Gli esperimenti di coppia e accesi fanno replay attraverso tutti i livelli ordinari di grafo/asset/agente. Un ripiego di blocco lineare normalizzato e limitato risolve il passaggio sei/sette che prima falliva, mantenendo le traiettorie esistenti come regressioni. Selezione/passaggio prescritti non sono una TCU completa né il comportamento dettagliato di denti/anello sincronizzatore/attuatore; parametri e campioni OEM restano `unverified`.

L'[incremento di compensazione della chiusura](CLOSURE_PREDICTION.it.md) esegue il replay di un futuro dell'impianto a input tenuti, limitato, senza commettere stato. Predice il flusso residuo dell'ago e pianifica la rimozione della tensione sulla griglia dei tick fisici. L'inseguimento isolato della dose migliora, mentre la chiusura/il rimbalzo reali e le storie di carburante/energia restano meccanismi fisici invariati. Predizioni in sola lettura, limiti interi, raffinamento dell'orizzonte, zero allocazioni e transazioni complete sono verificati. La predizione tiene gli altri comandi e omette gli eventi di input esterni futuri; il suo modello e l'orizzonte finito sono limiti espliciti, non una calibrazione né un'accettazione ECU completa.

L'[incremento dell'ago](NEEDLE_ACTUATION.it.md) accoppia l'energia del flusso magnetico e la forza reciproca alla massa reale dell'ago, a molla/smorzamento e agli arresti elastici. Il campionamento intero possiede la tensione della bobina dalla retroazione della dose erogata. Il fluido resta governato dall'alzata fisica attraverso il ritardo di chiusura e il rimbalzo; non è tagliato sul bersaglio. Passano riferimenti indipendenti magnetici/RL/di moto, registri di sorgente/fase/elettrici/termici, replay portabile/MCP e transazioni complete del regolatore. L'erogazione in eccesso e il liquido restante al confine dell'esperimento restano osservabili; questi risultati non stabiliscono un inseguimento di dose calibrato né un'elettronica/magnetica completa dell'iniettore.

L'[incremento di iniezione liquida](LIQUID_FUEL_INJECTION.it.md) ora parte da un film asciutto e preleva da una sorgente finita e cedevole. Passano pressione/lavoro analitici del binario, raffinamento simultaneo indipendente, registri completi di sorgente/film/chimici/termici, replay MCP reale e rollback speculativo della frizione. L'energia di pressione del binario è immagazzinata; il calore dell'ugello e il lavoro di pressione esportato del ricevitore restano distinti. Capacità geometrica, ventilazione/spazio gas/oscillazione, cavitazione, riempimento/efficienza/regolazione misurati e spray risolto restano aperti. Parametri `unverified`; Unity Editor/Play/Player/IL2CPP reale e calibrazione OEM non sono verificati.

`film-fired-cylinder` contiene inizialmente un inventario liquido dichiarato. Riscalda ed evapora quell'inventario prima della reazione prescritta; non implementa un iniettore liquido. Marcatori e test dello Studio preparati consumano le stesse definizioni. `POWER_UNITY_EDITOR` non è impostato, quindi Editor/Play reali, il rendering e Player/IL2CPP restano non verificati.

Tutti i parametri di ricerca restano `unverified`. EA211 DJS + DQ200 e PSA EC5 + AT8 conservano i confini completi del gruppo motopropulsore e i manifest di evidenza in [assets/samples](../assets/samples). Le misure OEM mancanti restano mancanti. Licenze e provenienza storica dei sorgenti sono conservate.

## Prossima sequenza di sviluppo

1. Estendere il serbatoio finito con capacità geometrica, dinamica ventilazione/spazio gas, riempimento/regolazione misurati e azionamento magnetico/elettronico raffinato. Sostituisci il confine dichiarato di lavoro di spostamento esportato quando volume liquido finito e quantità di moto dello spray sono risolti. Tieni separatamente osservabili il liquido erogato, il carburante evaporato e la reazione, e conserva riferimenti indipendenti.
2. Estendi il motore con controllo dell'accensione, dinamica di aspirazione/scarico, perdite meccaniche e termochimica più ricca. Conserva l'obiettivo completo del motore.
3. Estendi i percorsi di ricerca DCT verificati con azionamento dettagliato, proprietà/perdite misurate dei satelliti e idraulica/controllo AT completi, poi costruisci il coordinamento di cambio/coppia ECU/TCU dai primitivi verificati di ingranaggi, frizione, convertitore e idraulica. Aggiungi stato del regolatore limitato, comportamento di sensori/attuatori e recupero dai guasti.
4. Esegui `unity-test` con l'Editor fissato, poi ottieni evidenza Player/IL2CPP. Completa selezione dei canali, modifica del grafo e salvataggio come funzionalità distinte.
5. Ottieni mappe misurate, dati OEM e budget di incertezza per i due gruppi motopropulsori obiettivo prima di dichiarare campioni calibrati o prontezza al rilascio.

## Retroazione AT idraulica

`at_controller` accetta una marcia richiesta intera in [-1,4]; zero indica folle. Gestisce cinque coppie di valvole di riempimento/scarico e il blocco facoltativo del convertitore. L'ordine è ingresso del portasatelliti, solare piccolo, solare grande, freno del portasatelliti, freno del solare grande, poi blocco.

`controlled-hydraulic-ravigneaux` e `controlled-fired-hydraulic-ravigneaux` usano il canale 900 e l'ID 1400. Conservano 99 e 122 stati dichiarati entro il limite invariato di 128. v27 conserva percorsi, guadagni e clock e legge v1-v26.

Sono controlli di ricerca e i parametri restano `unverified`. Coordinamento della coppia ECU, sensori/valvole dettagliati, guasti completi del veicolo e calibrazione OEM restano aperti. Le verifiche managed e Standard non provano l'accettazione reale Unity Editor/Play/Player/IL2CPP.

[AT_CONTROL.it.md](AT_CONTROL.it.md)

## Rail di combustibile liquido alimentato da pompa

`liquid_rail_feed` associa un iniettore liquido a una pompa volumetrica esistente e a un confine esplicito di materia/calore. Il nodo di uscita idraulica deve corrispondere alla cedevolezza e alla pressione assoluta iniziale del rail. Pompa e iniettore possiedono questo nodo; altri percorsi fluidi non contabilizzati sono rifiutati.

v27 conserva collegamenti e temperatura sorgente e legge v1-v26. Scambio analitico albero/pressione, raffinamento ODE simultaneo indipendente, miscelazione termica, bilanci massa/combustibile/energia/volume, ritorno e rollback completo hanno verifiche separate.

Capacità geometrica, ventilazione/spazio gas/oscillazione, cavitazione, riempimento/efficienza/regolazione misurati e spray risolto restano aperti. Parametri `unverified`; Unity Editor/Play/Player/IL2CPP reale e calibrazione OEM non sono verificati.

[PUMP_FED_FUEL.it.md](PUMP_FED_FUEL.it.md)

## Serbatoio finito di combustibile liquido

`liquid_fuel_tank` conserva massa liquida finita ed energia termica con densità, riferimento termico del film e potere calorifico dell'iniettore associato. L'alimentazione lo seleziona con `tank_component` e omette `supply_temperature`. Ogni serbatoio appartiene a un'alimentazione compatibile.

Energia termica e chimica del serbatoio entrano nello stoccaggio totale. Il trasferimento interno non aggiunge massa o energia chimica esterna. La pressione prescritta all'ingresso mantiene il confine di lavoro di pressione. Aspirazione/scarico gas possono ancora trasportare energia chimica.

Capacità geometrica, ventilazione/spazio gas/oscillazione, cavitazione, riempimento/efficienza/regolazione misurati e spray risolto restano aperti. Parametri `unverified`; Unity Editor/Play/Player/IL2CPP reale e calibrazione OEM non sono verificati.

[LIQUID_FUEL_TANK.it.md](LIQUID_FUEL_TANK.it.md)
