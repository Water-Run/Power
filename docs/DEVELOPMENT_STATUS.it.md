# Stato di sviluppo

[English](DEVELOPMENT_STATUS.md) · [简体中文](DEVELOPMENT_STATUS.zh-CN.md) · [Français](DEVELOPMENT_STATUS.fr.md) · [Русский](DEVELOPMENT_STATUS.ru.md) · [日本語](DEVELOPMENT_STATUS.ja.md) · [한국어](DEVELOPMENT_STATUS.ko.md) · [Deutsch](DEVELOPMENT_STATUS.de.md) · [Español](DEVELOPMENT_STATUS.es.md) · **Italiano** · [Português](DEVELOPMENT_STATUS.pt-BR.md)

Power! ha un nucleo di simulazione gestito, documenti di modello condivisi e asset portabili, una CLI senza interfaccia, un servizio agente MCP e uno studio Unity preparato. I laboratori sintetici esercitano il comportamento del motore, della trasmissione, dell'idraulica e dell'elettrica. I gruppi motopropulsori completi, il controllo ECU/TCU coordinato, la calibrazione misurata e un'applicazione desktop Unity accettata restano incompiuti.

## Implementazione attuale

| Area | Implementato | Accettazione restante |
|---|---|---|
| Nucleo | Unità esplicite e ID stabili; compilazione immutabile; tempo intero limitato; registri osservabili; replay, annullamento, fork indipendenti e rollback dell'intero batch | Evidenza di lungo periodo e del gruppo motopropulsore completo |
| Motore | Cilindri chiusi/aperti, lavoro di pressione biella-manovella, flusso di gas bidirezionale, valvole fasate sull'albero a gomiti, calore di parete e combustione premiscelata prescritta | Aspirazione/scarico dettagliati, accensione, perdite meccaniche, termochimica più ricca e comportamento misurato del motore |
| Carburante | Dosaggio per ciclo, rail cedevoli alimentati da pompa, serbatoi finiti, ritorni conservativi, evaporazione del film, aghi fisici e previsione limitata di chiusura; [Geometria del serbatoio e spazio gassoso finito](TANK_HEADSPACE.it.md) | sloshing/forma idrostatica e riempimento/regolazione misurati, comportamento magnetico/elettronico/a spruzzo non lineare, equilibrio di fase dipendente dalla pressione e proprietà misurate del carburante |
| Trasmissione | Frizioni statiche/striscianti, azionamento a contatto, ingranaggi/planetari con segno, convertitore/blocco mappato e percorsi DCT a sette avanti/retromarcia e Ravigneaux a quattro avanti/retromarcia con rotazione dei satelliti e inerzia orbitale risolte | Cedevolezza/perdite di ingranamento e ripartizione del carico, azionamento DCT, controllo completo di pressione/cambio AT e instradamento misurato, cambi coordinati, perdite misurate e comportamento più ricco del convertitore |
| Idraulica | Volumi cedevoli, restrizioni, pompe con trafilamento/trascinamento espliciti, scarico, stantuffi dinamici, cursori dosati e accumulatori a gas a energia finita | Mappe misurate di valvole/accumulatori/pompe, attrito delle tenute, cavitazione e idraulica completa della trasmissione |
| Elettrica | Motori RL, solenoidi a induttanza variabile reciproca, batteria a carica finita, polarizzazione resistiva/RC, conversione di duty mediata e accessori | Comportamento chimico/termico misurato, BMS, controllo di corrente e integrazione completa dell'alimentazione |
| Controlli | PI di pressione campionato, dosaggio/chiusura dell'ago, passaggio DCT a stadi e cambi AT con retroazione di pressione e conferma fisica del blocco | Coordinamento di coppia ECU/TCU, sensori, attuatori e gestione dei guasti |
| Documenti e asset | 44 laboratori JSON/CLI, 43 esempi MCP, asset v29 e lettori v1-v28 | Modifica/salvataggio e raccolte di modelli calibrati |
| Agenti | Dodici strumenti MCP definiti da schema; evidenza compatta, controlli di revisione e diagnostica azionabile | Flussi completi per l'ambito fisico/di controllo restante |
| Unity | Importazione dei modelli, riproduzione a tick esatti, componenti schematici 3D, controlli, reset e test del ciclo di vita preparati | Accettazione reale di Editor/Play, grafici selezionabili, modifica/salvataggio del grafo e Player/IL2CPP |
| Archivio nativo | Prototipi di ricerca Zig 0.15.2, ABI conservata e provenienza dei sorgenti originali | Riferimento storico; la migrazione gestita resta separata dalla funzionalità completa |

Core e Assets puntano sia a `net10.0` sia a `netstandard2.1`; il Core non ha dipendenze da Unity, trasporto, fornitore di modelli o terze parti. Gli script in Unity Assets usano C# 9. Unity carica gli assembly Standard compilati dall'SDK esterno; non compila sorgenti .NET 10/C# 14.

## Evidenza e limiti

Il comando seriale richiesto `dotnet run --file tools/Build.cs -- verify` copre entrambi i target assembly, un processo MCP reale, tutti i laboratori, l'archivio Zig e l'ABI C#. Conteggi, risultati e percorsi dei log sono in [VALIDATION.md](VALIDATION.md). Le verifiche Standard su .NET 10 non attestano l'accettazione del runtime Unity.

L'alimentazione comprende [serbatoi finiti](LIQUID_FUEL_TANK.it.md) e [ritorni di scarico tracciati](LIQUID_FUEL_RETURN.it.md), con bilanci di massa, energia calorica/chimica e lavoro di pressione. Il [controllore AT idraulico](AT_CONTROL.it.md) regola la pressione degli attuatori e conferma rapporto/blocco fisici. Riferimenti indipendenti e transazioni complete sostengono questi modelli di ricerca; coordinamento ECU/TCU completo e comportamento hardware misurato restano aperti.

`POWER_UNITY_EDITOR` non è impostato nell'ambiente corrente. Import Studio, replay e test predisposti richiedono ancora prove reali Editor/Play, rendering e Player/IL2CPP.

Tutti i parametri restano `unverified`. EA211 DJS + DQ200 e PSA EC5 + AT8 conservano confini completi e manifesti di evidenza in [assets/samples](../assets/samples). Le misure OEM mancanti restano mancanti. Licenze e provenienza storica del codice sono preservate.

CLI `list-labs`, scoperta degli esempi MCP e verifica seriale condividono [un catalogo dei laboratori](../assets/labs/catalog.json). La verifica controlla che copra ogni sorgente di laboratorio.

## Prossima sequenza di sviluppo

1. Estendere equilibrio di fase dipendente dalla pressione, cavitazione, riempimento/regolazione misurati e azionamento magnetico/elettronico raffinato. Sostituisci il confine dichiarato di lavoro di spostamento esportato quando volume liquido finito e quantità di moto dello spray sono risolti. Tieni separatamente osservabili il liquido erogato, il carburante evaporato e la reazione, e conserva riferimenti indipendenti.
2. Estendi il motore con controllo dell'accensione, dinamica di aspirazione/scarico, perdite meccaniche e termochimica più ricca. Conserva l'obiettivo completo del motore.
3. Estendere attuazione DCT e idraulica planetaria/AT misurata, poi coordinare richieste di coppia e cambi ECU/TCU con i controllori DCT e AT campionati esistenti. Aggiungere sensori/attuatori misurati e guasti recuperabili.
4. Esegui `unity-test` con l'Editor fissato, poi ottieni evidenza Player/IL2CPP. Completa selezione dei canali, modifica del grafo e salvataggio come funzionalità distinte.
5. Ottieni mappe misurate, dati OEM e budget di incertezza per i due gruppi motopropulsori obiettivo prima di dichiarare campioni calibrati o prontezza al rilascio.
