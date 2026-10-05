# Registro di validazione

[English](VALIDATION.md) · [简体中文](VALIDATION.zh-CN.md) · [Français](VALIDATION.fr.md) · [Русский](VALIDATION.ru.md) · [日本語](VALIDATION.ja.md) · [한국어](VALIDATION.ko.md) · [Deutsch](VALIDATION.de.md) · [Español](VALIDATION.es.md) · **Italiano** · [Português](VALIDATION.pt-BR.md)


## 2026-10-05: Retroazione AT idraulica e controllo pressione

La verifica seriale richiesta passa su Windows x64/.NET 10.0.12. La build Release non ha avvisi o errori. L'accettazione reale Unity e nuove verifiche Linux/macOS restano non confermate.

`dotnet run --file tools/Build.cs -- verify`

| Identifier | Value |
|---|---|
| managed_checks | 367/367 |
| standard_checks_on_dotnet | 283/283 |
| actual_mcp_groups | 39/39 |
| laboratories | 36 |
| zig_tests | 16/16 |
| native_model_cs_groups | 6 |
| original_baseline_values | 176 |
| original_baseline_maximum_error | 0 |
| asset_version | power.asset.v25 |
| v24_fixture_sha256 | 6d6dc0f3b17ae1dfdcda59fc45ca7db63d260fb954c8c51567f9b00bab783ecc |

| Identifier | controlled-hydraulic-ravigneaux | controlled-fired-hydraulic-ravigneaux |
|---|---|---|
| duration_s | 4.5 | 2.2 |
| boundaries | 451 | 221 |
| states | 99 | 122 |
| confirmed_range | 1 | 4 |
| fault | 0 | 0 |
| lockup_state | 0 | 2 |
| max_sampled_energy_j | 7.059115887386724e-7 | 2.7647047318168916e-7 |
| max_sampled_hydraulic_volume_m3 | 1.7499700690273845e-18 | 2.7681036716270535e-18 |
| fingerprint | ceb522c56530be00 | 9fa63e406ccae528 |
| final_state_hash | 7ff1919e1f504740 | 958185fe86764b63 |
| source_sha256 | 9cac20ed7a79a2b9dd30f630adf5c6b3ce1f5dbbe4fa837eba3f0465cf67855b | cd4bd02357168532793462903670f9ec59e0c47edcd24986adc71c27bb4e88af |

- `artifacts/reports/at-control-final-2026-10-05.log`
- `artifacts/reports/at-control-evidence-2026-10-05.json`
- `artifacts/reports/at-control-schema-audit-2026-10-05.json`
- `artifacts/reports/at-controller-probe-2026-10-05.log`

8 gruppi fisici/transazionali coprono tutte le marce avanti/indietro, pressione/contatto/blocco reali, proprietà valvole, clock e limiti PI. I guasti comprendono perdita alimentazione, scarico bloccato, timeout applicazione/rilascio, interblocco direzione e perdita di blocco confermato. 2 gruppi asset conservano percorsi tipizzati e rifiutano downgrade rifirmati. 3 gruppi integrati e 2 scenari MCP reali corrispondono in ogni scalare e hash di stato.

L'avvio in retromarcia e il recupero restano Applying a 500 ms e sono confermati prima di 800 ms nel timeout dichiarato. I test seguono il blocco reale, non un ritardo fisso. Lo scarico sicuro non rimuove un blocco fisico. Il grafo motore/planetari/attuatori/controllo mantiene 122 stati entro il limite invariato di 128.

I parametri restano `unverified`. Coordinamento coppia ECU, sensori/valvole dettagliati, guasti completi veicolo, calibrazione OEM e accettazione reale Editor/Play/Player/IL2CPP restano aperti.

[AT_CONTROL.it.md](AT_CONTROL.it.md)


## 2026-10-02: alimentazione idraulica AT condivisa e azionamento dinamico a stantuffo

Il comando seriale richiesto `dotnet run --file tools/Build.cs -- verify` passa in locale
su Windows x64/.NET 10.0.12: **354/354** controlli gestiti, **273/273**
controlli degli assembly Standard ospitati su .NET 10, **37/37** gruppi MCP reali,
**16/16** test Zig e sei controlli C# dei modelli nativi. La compilazione Release segnala
zero avvisi/errori. Tutti i **34** laboratori passano e tutti i **176** valori di
baseline originali coincidono esattamente. Gli audit C/C++/Lua restano vuoti. L'accettazione
reale di Unity e le nuove accettazioni Linux/macOS restano non verificate.

File di evidenza:

- `artifacts/reports/at-actuation-closed-2026-10-02.log`: esecuzione seriale completa.
- `artifacts/reports/at-actuation-evidence-2026-10-02.json`: digest di log/report/sorgente,
  stato di pressione/corsa/contatto, lavoro della pompa e limiti completi di inventario/energia.
- `artifacts/reports/at-actuation-schema-audit-2026-10-02.json`: tutti i 34 documenti
  sono validi con jsonschema 4.25.1.
- `artifacts/reports/at-finer-10000.json` e `at-finer-5000.json`: riferimenti accesi
  accoppiati più fini, con documenti di authoring conservati esplicitamente.

Sei gruppi fisici/di transazione controllano l'abbassamento immutabile del grafo ordinario,
la capacità reale di pressione/corsa/contatto, il raffinamento RK4 simultaneo e indipendente
di pressione della pompa e del moto, l'inventario spazzato completo, il lavoro della pompa e il
calore instradato, sei rami ad alimentazione comune, errori espliciti di unità/corsa/riferimento/canale,
annullamento, rollback tardivo, fork indipendenti e stepping senza allocazioni.
Il controllo termico del singolo attuatore ricava in modo indipendente il calore di trascinamento
dell'albero dal lavoro di sorgente, dalla variazione di energia cinetica dell'albero e dal lavoro
della pompa. Le aree anteriore/posteriore restano esplicite;
gli esempi usano 0.001/0 m2 e quindi trattengono nell'inventario il volume spazzato anteriore.

Tre gruppi di integrazione e due nuovi scenari MCP reali conservano ogni scalare
e hash di stato ai **201** confini del treno di coppia a cinque rami e agli **87**
confini accesi a sei rami. Le impronte dell'helper e del JSON coincidono. I canali di innesto
precedenti sono assenti; solo gli input reali di riempimento/scarico comandano pressione e moto.
Una capacità iniziale nulla delle pastiglie resta nulla nonostante un comando di applicazione.
I blocchi fisici sono confermati su tutti e quattro i percorsi avanti di salita/discesa.
Anche il blocco acceso usa il moto reale dello stantuffo. Sia l'energia di pressione sia
l'energia di stantuffo/ritorno/pastiglia/arresto restano nella transazione completa e nella
contabilità energetica globale.

Il confronto degli stati finali più grossolano a 40/20/10-us non era monotono: gli errori
normalizzati rispetto a 10 us erano `1.74505e-5` a 40 us e `2.19913e-5` a 20 us. Quel
criterio fallito non è trattato come un risultato di convergenza superato. Un confronto
più fine a **20/10/5-us** entra in un intervallo a errore decrescente: rispetto a 5 us, il
massimo normalizzato su otto uscite di motore/veicolo/pressione/corsa/lavoro/calore diminuisce
da **2.01463e-5** a 20 us a **6.51409e-6** a 10 us. Tutte le esecuzioni di riferimento soddisfano
i KPI fisici e il replay esatto del proprio runtime. Dai passaggi ibridi prescritti non si
inferisce un ordine di convergenza globale né un'accuratezza OEM.

| Grandezza finale | Treno a cinque rami (2 s) | Treno acceso a sei rami (0.8 s) |
|---|---:|---:|
| Velocità motore/sorgente | 102.2242615437 rad/s | 36.2608726849 rad/s |
| Velocità del veicolo | 8.5186884620 rad/s | 13.5978272568 rad/s |
| Pressione di linea | 1.1963842937 MPa | 1.0666023745 MPa |
| Lavoro della pompa | 117.4226709117 J | 37.6491650676 J |
| Stati riportati | 83 | 106 |
| Residuo energetico campionato massimo | `1.64680e-7 J` | `1.29307e-8 J` |
| Residuo di inventario spazzato massimo | `1.18076e-18 m3` | `2.94767e-19 m3` |
| Residuo di fase degli ingranaggi massimo | `5.68434e-14 rad` | `1.42109e-14 rad` |
| Impronta | `f61d582874bd086b` | `4e83efb34de63922` |
| Hash finale | `9307fb49117dad0a` | `70e00a107ef1ca6d` |

I valori SHA-256 delle sorgenti sono
`119008206e0d1a5372ec399665d30669ac41bbf2f0fbc0c27c56bf3bc90c8f02`
(treno di coppia) e
`9659b282454202857b16ee2dbc0226f30b79549fedab88b4c79f92c6c6e66801`
(acceso). L'impianto acceso denso rientra nel limite esistente di 128 stati con 106 stati,
senza rimuovere lo stato di motore, convertitore, pressione o attuatore. Bastano i record
v24 esistenti; non si introduce un nuovo formato di asset. La suite MCP reale da 37 scenari
resta limitata a 300 s, compresi questi impianti accoppiati più grandi.

Parametri e mappe restano `unverified`. Le schedule delle valvole sono prescritte;
la retroazione AT completa, il coordinamento di coppia dell'ECU, il comportamento misurato
del corpo valvole, i modelli di tenuta/cavitazione/aerazione/temperatura e la calibrazione
OEM restano aperti. Gli import e la riproduzione preparati in Studio non stabiliscono
l'accettazione reale di Editor/Play/Player/IL2CPP. L'obiettivo completo resta incompiuto. Vedi
[AT_HYDRAULIC_ACTUATION.it.md](AT_HYDRAULIC_ACTUATION.it.md).

## 2026-10-02: rotazione assoluta dei satelliti, inerzia orbitale e quattro ingranamenti fisici

Il comando seriale richiesto `dotnet run --file tools/Build.cs -- verify` passa in locale
su Windows x64/.NET 10.0.12: **345/345** controlli gestiti, **267/267**
controlli degli assembly Standard ospitati su .NET 10, **35/35** gruppi MCP reali,
**16/16** test Zig e sei controlli C# dei modelli nativi. La compilazione Release segnala
zero avvisi/errori. Tutti i **32** laboratori passano e tutti i **176** valori di
baseline storici coincidono esattamente. Gli audit C/C++ e Lua restano vuoti. L'accettazione
reale di Unity e le nuove accettazioni Linux/macOS restano non verificate.

File di evidenza:

- `artifacts/reports/resolved-planets-endpoint-2026-10-02.log`: esecuzione seriale completa.
- `artifacts/reports/resolved-planets-evidence-2026-10-02.json`: digest di log/report/sorgente,
  geometria, energie di rotazione/orbita, residui e diagnostica numerica.
- `artifacts/reports/resolved-planets-schema-audit-2026-10-02.json`: 32 documenti validi
  e otto casi malformati di ingranamento del portasatelliti rifiutati da jsonschema 4.25.1.
- `artifacts/reports/resolved-planet-failure-2026-10-02.json` e
  `resolved-planet-refinement-failure-2026-10-02.json`: tracce pre-correzione conservate.

Otto gruppi fisici/di transazione controllano i rapporti con segno relativi al portasatelliti,
matrici di massa indipendenti nello spazio delle accelerazioni, geometria rigida di passo,
aggregazione di massa/rotazione per satellite e inerzia orbitale, tutte e quattro le inerzie
riflesse avanti/indietro, il momento angolare, la potenza di reazione di ingranamento sommata
nulla, l'impulso/calore di cattura del portasatelliti, l'overdrive caricato di 20-second,
errori di unità/impacchettamento/riferimento, annullamento, rollback tardivo, fork e
stepping/rilettura senza allocazioni.
Due gruppi portatili conservano rapporti con segno, portasatelliti e memorizzazione della
rotazione completi, rifiutano record malformati e downgrade v23 risigillati sul digest, e
fanno il replay del grafo ridotto v23 autentico. Il suo SHA-256 è
`f2bd390e8ecf013e5843ed3352ccf2a2828133b541fc61d7927995f8ac1dc2e2`;
l'impronta `63d28eb32bc4cfb2` e il replay aggiornato a ogni confine restano intatti.

L'esecuzione iniziale del treno di coppia si è fermata dopo **1.4033 s**. Il residuo di
velocità solare grande/satellite esterno era `1.2406076e-11 rad/s`, vicino al limite
invariato `1.2406654e-11 rad/s`; l'errore di fase era zero e il residuo energetico
`5.26143e-10 J`. Il solo raffinamento di Schur relativo ha ritardato l'arresto a **1.4494 s**.
La risoluzione libera al punto medio ora impone `G v_next=0` usando
`v_next=2 v_mid-v_old`, invece di riflettere ripetutamente l'arrotondamento precedente.
Su stati vecchi esattamente compatibili questo è il vincolo ordinario di punto medio nullo.
Tutte le modifiche usano moltiplicatori reali della risposta in forza, che si accumulano
nelle reazioni medie. Tre raffinamenti relativi limitati migliorano le risposte in forza piccole;
lo scratch è di proprietà della simulazione o locale al costruttore, e i fattori compilati restano
immutabili. I grafi esistenti conservano il comportamento precedente di proiezione/replay.
Nessuna tolleranza di inerzia o di velocità/fase è stata ridotta o aumentata per far passare il caso.

Tre gruppi di integrazione e due nuovi scenari MCP reali coincidono su ogni scalare
e hash di stato ai **201** confini del treno di coppia e agli **87** confini accesi/convertitore.
Le impronte dell'helper e del JSON piatto coincidono. Rotazione assoluta interna/esterna,
memorizzazione orbitale del portasatelliti, tutte le reazioni di ingranamento e le contabilità
termiche complete di attrito/convertitore restano osservabili. Revisioni dell'agente, annullamento,
riparazione di rapporti non validi e fork neutri indipendenti restano coperti.

| Grandezza finale | Treno di coppia (2 s) | Treno acceso (0.8 s) |
|---|---:|---:|
| Velocità motore/sorgente | 117.3118882900 rad/s | 40.9899766814 rad/s |
| Velocità del veicolo | 9.7759906908 rad/s | 15.3712412555 rad/s |
| Velocità assoluta del satellite interno | -469.2475531599 rad/s | -204.9498834069 rad/s |
| Velocità assoluta del satellite esterno | 156.4158510533 rad/s | 122.9699300441 rad/s |
| Energia di rotazione dei satelliti | 23.3037873338 J | 12.2863030022 J |
| Energia orbitale dei satelliti | Quasi zero con portasatelliti tenuto | 15.4891426738 J |
| Stati riportati | 27 | 41 |
| Residuo energetico campionato massimo | `4.01224e-9 J` | `3.00179e-9 J` |
| Residuo di fase degli ingranaggi massimo | `1.13687e-13 rad` | `2.84217e-14 rad` |
| Residuo di velocità degli ingranaggi massimo | `3.97904e-13 rad/s` | `2.27374e-13 rad/s` |
| Impronta | `d3010be4b33fdbaf` | `faf9384667ece88d` |
| Hash finale | `8e1da00bda34941b` | `e606196f7b345c99` |

Il treno acceso brucia **38.0629762199 mg** e rilascia **1674.7709536768 J**.
Rispetto a un riferimento di stato finale a 12.5-us, l'errore massimo normalizzato su
velocità di motore/veicolo/satelliti e uscite di lavoro/calore diminuisce da `1.13450e-6`
a 50 us a `1.04173e-6` a 25 us. È un raffinamento limitato attraverso passaggi ibridi
prescritti; non si rivendica un ordine di convergenza globale.

Le tre coppie sincrone dichiarate usano raggio della corona **0.1 m**, masse per satellite
interno/esterno **0.3/1 kg** e inerzie di rotazione **0.000015/0.0005 kg m2**.
La struttura del portasatelliti **0.03 kg m2** riceve un'inerzia orbitale esplicita
**0.0184375 kg m2**. I valori SHA-256 delle sorgenti sono
`c45d90282097303da44c9405eb6945637b578db84617f0b749efc58ed879f0a5`
(coppia) e
`e71d9ea4755826b1dcd916fd0157827b2e845dd48506e32f6291674d56f315f3`
(acceso). Geometria, masse e mappe restano `unverified`. Gli ingranamenti sincroni rigidi
non stabiliscono la ripartizione dei carichi di fabbricazione, la cedevolezza dei denti,
lubrificazione/perdite, l'idraulica/il controllo AT completi, l'identità OEM o un comportamento
del veicolo calibrato. I casi Studio preparati includono tutte e tre le porte di ingranamento
del portasatelliti; non stabiliscono risultati reali di Editor/Play/Player/IL2CPP. Vedi
[RESOLVED_PLANETS.it.md](RESOLVED_PLANETS.it.md).

## 2026-10-02: percorsi composti Ravigneaux e composizione del convertitore acceso

Il comando seriale richiesto `dotnet run --file tools/Build.cs -- verify` passa in locale
su Windows x64 con .NET 10.0.12: **332/332** controlli gestiti, **257/257**
controlli degli assembly Standard ospitati su .NET 10, **33/33** gruppi MCP reali,
**16/16** test Zig e sei controlli C# dei modelli nativi. La compilazione Release segnala
zero avvisi/errori. Tutti i **176** valori di baseline originali coincidono esattamente;
gli inventari di sorgenti C/C++ e Lua restano vuoti. Tutti i **30** laboratori passano.
L'accettazione reale di Unity e le nuove accettazioni Linux/macOS restano non verificate.

File di evidenza:

- `artifacts/reports/ravigneaux-confirmed-2026-10-02.log`: esecuzione seriale completa.
- `artifacts/reports/ravigneaux-evidence-2026-10-02.json`: digest di log/sorgente/report,
  limiti di stato, contabilità numeriche e limiti dichiarati.
- `artifacts/reports/ravigneaux-schema-audit-2026-10-02.json`: tutti i 30 documenti
  passano jsonschema 4.25.1; otto casi di topologia malformata vengono rifiutati.
- `artifacts/reports/ravigneaux-phase-failure-2026-10-02.json`: diagnosi isolata
  prima della compensazione delle coordinate.

Sei gruppi fisici/di transazione controllano la matrice di massa libera 2x2 ridotta in modo
indipendente, tutte e quattro le inerzie riflesse avanti e indietro, le reazioni dei membri e
la potenza di reazione degli ingranaggi sommata nulla, l'impulso/calore di cattura del freno
del portasatelliti, l'overdrive caricato lungo, unità/geometria/porte malformate, annullamento,
rollback tardivo, fork indipendenti e stepping/rilettura senza allocazioni. Due gruppi portatili
controllano record completi del portasatelliti, conteggi tipizzati, riferimenti duplicati e il
rifiuto di downgrade v22 contraffatti. Lo SHA-256 della fixture DCT controllata v22 autentica è
`db90df5fa9ca90067341abdcaf8c3a4a38316794a4b3c8ecc7c89a852375c24e`;
l'impronta `72122eae163df98e` e il replay aggiornato esatto restano intatti.

L'overdrive caricato lungo si è fermato inizialmente dopo l'ultimo confine commesso di **2.9668 s**.
Il residuo di fase del doppio pignone era `-8.27754e-10 rad`, vicino al limite
`8.27906e-10 rad`, mentre il residuo di velocità era `-1.77991e-12 rad/s` contro
`5.21235e-11 rad/s`. I modelli composti ora usano un accumulo transazionale di coordinate
compensate. Lo stesso controllo analitico di carico di **20-second** passa senza aumentare
le tolleranze di fase/velocità né proiettare le posizioni. Lo stato di correzione si copia,
si hasha e fa rollback con ogni intervallo; i modelli precedenti privi di composizione conservano
il comportamento esistente di integrazione/hash.

Tre gruppi di integrazione e due scenari MCP reali conservano ogni uscita e
hash di stato ai **201** confini del treno di coppia e agli **87** confini accesi/convertitore.
I cinque elementi di attrito attuano fisicamente i passaggi avanti prescritti di salita/discesa;
l'accettazione del comando non è trattata come blocco completato. L'accumulo termico coincide
con la somma di tutto il calore di frizione/convertitore instradato. Controlli di revisione,
annullamento, rifiuto di input limitato e fork neutri indipendenti restano coperti.

| Grandezza finale | Treno di coppia (2 s) | Treno acceso con convertitore (0.8 s) |
|---|---:|---:|
| Velocità di ingresso/motore | 119.6764605858 rad/s | 42.4589002302 rad/s |
| Velocità del veicolo | 9.9730383821 rad/s | 15.9220875863 rad/s |
| Stati riportati | 21 | 35 |
| Calore di attrito nei cinque elementi di gamma | 546.0473656082 J | 117.6775595348 J |
| Residuo energetico campionato massimo | `1.87947e-9 J` | `1.96445e-9 J` |
| Residuo di fase degli ingranaggi massimo | `3.19744e-14 rad` | `1.06581e-14 rad` |
| Differenza massima della contabilità termica | `1.52568e-10 J` | `2.41471e-9 J` |
| Impronta | `63d28eb32bc4cfb2` | `7c207499d9050fc6` |
| Hash finale | `4eb3c05eeef034c6` | `0cbed5442969c06f` |

Il treno acceso brucia **37.7116554560 mg**, rilascia **1659.3128400630 J**,
dissipa **66.4214778376 J** nel convertitore e **125.9622150447 J** nel
blocco. Rispetto al riferimento di stato finale a 12.5-us, l'errore massimo normalizzato
su velocità di motore/veicolo e tre uscite di lavoro/calore diminuisce da
`9.92882e-7` a 50 us a `7.19762e-7` a 25 us. È un raffinamento limitato
attraverso eventi ibridi prescritti, non un ordine globale di convergenza rivendicato.

I valori SHA-256 delle sorgenti sono
`8063388dbd341fd16b05ab971f6c2cc23d87ebfee3681da9d1ffcf95fffa52a0`
(treno di coppia) e
`55587bff2a2e5f33c6e876ed0d539615cfee1daf938d72dc4946c39a36a74d44`
(treno acceso). Parametri/mappe di ricerca restano `unverified`. Rotazione interna dei
satelliti, perdite dettagliate degli ingranaggi, idraulica/controllo AT, coordinamento ECU,
topologia OEM esatta e campioni misurati restano aperti. I test di import/riproduzione Unity
sono preparati come casi di risorsa singoli, compresi errori precedenti di conteggio degli
argomenti riparati; dal run gestito non si inferisce alcun risultato di Editor/Play o IL2CPP.
Vedi [RAVIGNEAUX_TRANSMISSION.it.md](RAVIGNEAUX_TRANSMISSION.it.md).

## 2026-10-01: sincronizzazione DCT campionata, passaggio graduale e controllo acceso combinato

Il comando seriale richiesto `dotnet run --file tools/Build.cs -- verify` passa in locale
su Windows x64 con SDK 10.0.401/runtime 10.0.12: **321/321** controlli gestiti,
**249/249** controlli degli assembly Standard ospitati su .NET 10, **31/31** gruppi MCP
reali, **16/16** Zig e **6/6** controlli ABI C#. La compilazione Release segnala zero
avvisi/errori. Tutti i **176** valori di baseline storici coincidono esattamente e
l'audit C/C++/Lua è vuoto. L'accettazione reale di Unity e le nuove accettazioni Linux/macOS non sono verificate.

File di evidenza:

- `artifacts/reports/tcu-final-2026-10-01.log` — esecuzione seriale completa.
- `artifacts/reports/tcu-evidence-2026-10-01.json` — ambito, digest di log/sorgente,
  marcia/guasto/fase reali, limiti di stato, residui di fase ed energia.
- `artifacts/reports/tcu-schema-audit-2026-10-01.json` — tutti i **28** laboratori
  passano jsonschema 4.25.1; **10** casi di controllore malformati vengono rifiutati.
- `artifacts/reports/controlled-dual-clutch.json` e
  `artifacts/reports/controlled-fired-dual-clutch.json` — report completi.

Otto gruppi fisici/di controllo verificano tutti e sette i percorsi confermati, la
preselezione scarica, il passaggio esclusivo graduale della trazione, la retromarcia con segno,
il blocco della direzione di moto, il timeout di sincronizzazione, la perdita persistente del
blocco confermato, il recupero in folle/nuovo bersaglio, i controlli completi degli input
statici/immediati/schedulati, la proprietà di dieci canali, il campionamento intero, i percorsi
immutabili, annullamento/rollback tardivo, fork e zero allocazioni. Un'esecuzione controllata di
20-second verifica la conservazione stretta della fase di marcia e l'energia. Gli esiti del
controllore restano guasti osservabili; non sono trattati in silenzio come un cambio riuscito
o come un fallimento numerico.

La sincronizzazione caricata lunga ha esposto per prima l'arrotondamento accumulato delle
coordinate a 3.7688 s. Il residuo di velocità degli ingranaggi era entro il suo limite, mentre
l'errore di fase normalizzato `-3.2883917811e-10` superava di poco il limite esistente
`3.2882809435e-10`. I nuovi modelli controllati ora accumulano coordinate di velocità al punto
medio con correzione compensata transazionale. Le tolleranze strette non sono state aumentate e
nessuna posizione di stato è stata proiettata su un rapporto scelto. I modelli precedenti conservano
il comportamento di integrazione/hash antecedente; la nuova compensazione si copia/si hasha/fa
rollback con ogni intervallo reale e speculativo.

Il limite esplicito degli stati riportati è **128**. I limiti di nodi/componenti restano
**32/64**; il comportamento seriale di build/test è invariato. Un modello con 32 rotori/64 stati RL
compila e avanza esattamente a 128 stati riportati. Modelli con più gas tracciato, film,
iniezione e controllore rifiutano la capacità. La composizione completa acceso/DCT/controllore
ora rientra in 70 stati, invece di omettere lo stato di motore/controllo per rientrare nel
limite precedente. Questo non stabilisce prestazioni sparse/Burst o Unity.

Due gruppi portatili verificano percorsi v22, periodi, rampe, tolleranze, proprietà e
storia di guasti/replay, conteggi tipizzati limitati, riferimenti/unità errati, record mancanti
e downgrade v21 contraffatti. Lo SHA-256 della fixture di grafo v21 autentica è
`706618f7d32a5ef4b8a18ca801d2c3ba0b293eac03b584d88d584e9290a38437`;
l'impronta `7466a75b99fbfd78` e il replay aggiornato allo stesso runtime restano intatti.
Le fixture precedenti e le impronte fisiche sono evidenza di regressione conservata.

Tre gruppi di integrazione e due scenari MCP reali controllano lo stato campionato,
la richiesta intera, la guida azionabile sui canali posseduti, le revisioni, i batch
annullati/falliti e i fork indipendenti di comando marcia. Tutti i **421** confini del
treno controllato e gli **83** confini acceso controllato di report/portatile/MCP coincidono esattamente.

| Grandezza finale | Treno controllato (4.2 s) | Treno acceso controllato (0.8 s) |
|---|---:|---:|
| Marcia richiesta / confermata | 1 / 1 | 3 / 3 |
| Fase di cambio / guasto | Driving / None | Driving / None |
| Velocità del motore | 197.0169047878 rad/s | 51.7225290420 rad/s |
| Velocità del rotore del veicolo | 12.6455009492 rad/s | 7.6456066581 rad/s |
| Stati riportati | 64 | 70 |
| Residuo energetico campionato massimo | `1.18562e-8 J` | `1.19940e-9 J` |
| Errore di fase di marcia riportato massimo | `7.16227e-14 rad` | `7.10543e-15 rad` |
| Impronta | `72122eae163df98e` | `1d2b1a0c1eabf259` |
| Hash finale | `ef2843acbb273e6d` | `b50dea693d6af82a` |

La scalata discendente finale sotto carico e la successiva preselezione scarica si osservano
attraverso la conferma fisica a 4.2 s. A 4.0 s la conferma precedente era stata disturbata
temporaneamente dallo slittamento reale di selettore/trazione, quindi la marcia reale ha
riportato correttamente zero invece di assumere il completamento. La combinazione accesa brucia
**37.2949777641 mg** e rilascia **1640.9790216183 J**.

I valori SHA-256 delle sorgenti sono
`f15629d3fc8d8915f5884f77603458053c83e7835cf34d434fa607d85442bc87`
(controllato) e
`c3a6d491fd614d526a99831d7dfdc4285bca3c10a57ac811959023cfb9e40d5b`
(acceso controllato). Tutti i parametri restano `unverified`. Questo controllore usa
deliberatamente un passaggio con interruzione di coppia; la miscelazione completa di coppia
dell'ECU, sensori/attuatori, meccanismi a denti e anelli sincronizzatori, guasti completi,
AT, gruppi motopropulsori bersaglio misurati e Unity reale restano aperti. Vedi
[DCT_CONTROL.it.md](DCT_CONTROL.it.md).

## 2026-10-01: percorsi di potenza a doppia frizione con sette marce avanti e retromarcia

Il comando seriale richiesto `dotnet run --file tools/Build.cs -- verify` passa in locale
su Windows x64 con SDK 10.0.401/runtime 10.0.12: **308/308** controlli gestiti,
**239/239** controlli degli assembly Standard ospitati su .NET 10, **29/29** gruppi MCP reali,
**16/16** Zig e **6/6** controlli ABI C#. La compilazione Release ha zero avvisi/errori.
Tutti i **176** valori di baseline storici coincidono esattamente; l'audit C/C++/Lua è vuoto.
L'accettazione reale di Unity e le nuove accettazioni Linux/macOS restano non verificate.

File di evidenza:

- `artifacts/reports/dct-final-2026-10-01.log` — verifica seriale completa.
- `artifacts/reports/dct-evidence-2026-10-01.json` — ambito, digest, grafo/replay,
  grandezze finali e raffinamento misurato.
- `artifacts/reports/dct-schema-audit-2026-10-01.json` — tutti i **26** laboratori
  passano il contratto strutturale jsonschema 4.25.1 esistente.
- `artifacts/reports/dual-clutch-transmission.json` e
  `artifacts/reports/fired-dual-clutch.json` — report di esperimento completi.

Sei gruppi fisici verificano un grafo ordinario con quattordici rotori interni,
dodici ingranaggi permanenti e dieci frizioni di attrito, binding stabili di proprietà del
chiamante, sette percorsi avanti e di retromarcia con segno, tre rami di riduzione finale e
parametri immutabili. Riferimenti indipendenti di inerzia riflessa/coppia costante coprono ogni
percorso selezionato con e senza preselezione del percorso inattivo. Controlli indipendenti di
proiezione di cattura a due coordinate verificano impulso di sincronizzazione, velocità finali
e calore. Fork completi, annullamento, rollback tardivo, contratti di capacità/unità/ID/selezione
e zero allocazioni gestite per stepping/rilettura riusciti passano.

Lo scenario di coppia completo ha esposto un fallimento del passaggio da sei a sette subito
dopo il rilascio della frizione vecchia. I blocchi riflessi dagli ingranaggi correlati hanno
esaurito il budget di proiezione scalare. Un fallback di Schur lineare normalizzato e preallocato
ora risolve i blocchi indipendenti dopo l'esaurimento di quel budget, con gli stessi limiti statici,
rilascio limitato dell'insieme attivo e controlli di residuo e calore passivo. I casi
singolari/non lineari conservano i limiti esistenti. L'accelerazione indipendente diretta sei/sette
e il passaggio che prima falliva sono evidenza di regressione; la fisica precedente e le traiettorie
autentiche degli asset restano verificate. Non si è usato un aumento del limite di iterazioni né
l'accettazione di un residuo fallito.

Tre gruppi di integrazione verificano l'identità di impronta assembly/JSON, la partenza,
la preselezione, tutti i passaggi avanti di salita/discesa, l'instradamento termico, la diagnostica
strutturata, revisioni/annullamento e fork indipendenti dei selettori. Entrambi i laboratori fanno
il replay attraverso asset portatili e un server figlio MCP reale. Tutti i **281** confini del
laboratorio di coppia e gli **83** del laboratorio acceso di report/portatile/MCP coincidono esattamente.

Lo scenario di coppia raggiunge ogni rapporto effettivo dichiarato dopo il passaggio:

| Marcia avanti | Rapporto verificato di velocità motore/veicolo |
|---|---:|
| 1 | 15.58 |
| 2 | 9.43 |
| 3 | 6.765 |
| 4 | 5.166 |
| 5 | 3.774 |
| 6 | 3.182 |
| 7 | 2.664 |

Sono riduzioni di ricerca dichiarate, non misure OEM. Il segno della retromarcia e
l'inerzia riflessa preselezionata hanno evidenza indipendente a carico costante; lo scenario
stradale non rivendica l'innesto della retromarcia a veicolo in moto.

| Grandezza finale | DCT di coppia (2.8 s) | DCT acceso (0.8 s) |
|---|---:|---:|
| Velocità del motore | 161.3549080416 rad/s | 53.8218438613 rad/s |
| Velocità del rotore del veicolo | 10.3565409526 rad/s | 7.9559266609 rad/s |
| Calore totale di frizione/sincronizzazione | 870.3601867183 J | 224.4917038405 J |
| Nodo termico | 300.8703601867 K | 351.7212768995 K |
| Residuo energetico campionato massimo | `5.22732e-9 J` | `1.23919e-9 J` |
| Conteggio degli stati riportati | 55 | 61 |
| Impronta del modello | `7466a75b99fbfd78` | `b7216a2ed8c88dc3` |
| Hash finale | `c8932376afe516c6` | `35b434aca827c3a6` |

L'esempio acceso brucia **39.1255750233 mg** e rilascia **1721.5253010267 J**.
Il suo grafo completo a sette marce avanti/retromarcia guida il passaggio schedulato da 1 a 2 a 3
entro il budget di stato limitato attuale. Rispetto a un riferimento di 12.5-microsecond, le
differenze finali scalate massime per velocità di motore/veicolo, lavoro di sorgente e calori
delle frizioni selezionate sono **4.9008455434e-6** a 50 microseconds e **4.3731765238e-6** a
25 microseconds. L'errore diminuisce in modo modesto; questo da solo non stabilisce un ordine
uniforme degli eventi ibridi né una convergenza asintotica completa. I riferimenti indipendenti
di ingranaggio/frizione e la conservazione restano evidenza separata.

I valori SHA-256 delle sorgenti sono
`f5db9f0a9f3a0585eff261d71285e4a390c099ec1a3bec27d1b9194beb311294`
(coppia) e
`0dfc3ed92930b649c8312043625789a4a4394811fed9e0c88a615a18a6b07473`
(acceso). Si conservano i tipi di componente, le unità, il formato degli asset e i lettori
precedenti esistenti. Tutti i parametri restano `unverified`; questa disposizione di treno di
ricerca non è un DQ200 calibrato. I selettori ad attrito non completano l'azionamento a denti
e anelli sincronizzatori, e le schedule prescritte non implementano il coordinamento completo
di coppia TCU/ECU. AT completa, perdite/azionamento misurati, gruppi motopropulsori bersaglio
completi e Unity reale restano aperti. Vedi
[DUAL_CLUTCH_TRANSMISSION.it.md](DUAL_CLUTCH_TRANSMISSION.it.md).

## 2026-10-01: replay di chiusura limitato e compensazione del taglio sui tick fisici

Il comando seriale `dotnet run --file tools/Build.cs -- verify` passa in locale su Windows
x64 con SDK 10.0.401/runtime 10.0.12: **299/299** controlli gestiti, **233/233**
controlli degli assembly Standard ospitati su .NET 10, **27/27** gruppi MCP reali,
**16/16** Zig e **6/6** controlli ABI C#. La compilazione Release ha zero avvisi/errori.
Tutti i **176** valori numerici storici coincidono esattamente; l'audit dei sorgenti C/C++/Lua
resta vuoto. Unity reale e la nuova CI Linux/macOS non sono verificati.

File di evidenza:

- `artifacts/reports/closure-final-2026-10-01.log` — verifica seriale completa.
- `artifacts/reports/closure-evidence-2026-10-01.json` — ambito leggibile da macchina,
  digest di sorgente/log, impronte di replay, evidenza di inseguimento e di orizzonte.
- `artifacts/reports/closure-schema-audit-2026-10-01.json` — tutti i **24** documenti
  di laboratorio passano jsonschema 4.25.1; **6** casi di orizzonte malformati vengono rifiutati.
- `artifacts/reports/closure-compensated-cylinder.json` — esperimento acceso.

Cinque gruppi Core verificano il replay manuale esatto e indipendente a tensione nulla, i valori
commessi/hash/tempo invariati, l'erogazione reale migliorata, la memoria di taglio, il raffinamento
dell'orizzonte, allineamento/budget/immutabilità, lettura e annullamento di batch, fallimento
tardivo, fork indipendenti, zero allocazioni gestite per le previsioni e per lo stepping predittivo
attivo, e storie speculative complete delle frizioni. Ogni candidato usa stato preallocato separato
ed equazioni ordinarie dell'impianto; nessun carburante previsto viene aggiunto a una contabilità
reale. Le predizioni disabilitate conservano impronte e hash precedenti. L'annullamento o una
predizione non valida rifiuta il batch reale completo.

A 1 ms il benchmark isolato prevede circa **2.9894117019 mg** di carburante aggiuntivo
sotto rimozione immediata della tensione, e il suo replay manuale di chiusura separato concorda
entro la tolleranza di confronto dichiarata `1e-15 kg`. La previsione a input tenuto è
distinta dagli eventi futuri reali o dal comportamento misurato del dispositivo.

L'inseguimento a orizzonte finito conserva il rimbalzo tardivo sulla sede:

| Orizzonte di predizione | Carburante erogato reale per una richiesta di 8-mg |
|---|---:|
| 8 ms | 8.2537740284 mg |
| 12 ms | 8.0606880453 mg |
| 20 ms | 8.0101785078 mg |
| 30 ms | 8.0101785078 mg |

La retroazione on/off precedente eroga **9.8939438959 mg**. Con una previsione di 20-ms
e il taglio sui tick fisici, l'errore relativo scende da **23.6743%** a **0.12723%**,
circa **186 times** più piccolo in questo benchmark sintetico. La decisione a 20/30-ms
concorda, mentre 8 ms tronca una coda materiale. È evidenza di inseguimento limitato basato
sul modello, non accuratezza calibrata dell'iniettore. Il passo temporale elettrico/di contatto
e il raffinamento di controllore/orizzonte restano controlli di accettazione separati.

Due gruppi portatili verificano il replay v21 di orizzonte/taglio, la ricodifica esatta, orizzonti
del driver malformati e il rifiuto di downgrade v20 contraffatti. La fixture v20 autentica di SHA-256
`4d87e996d92a50508cfcffac610551b84220f2b3055f5b104c8ec7ec64ca9a2e`
conserva l'impronta `3fa813ff44b95a79` e il replay aggiornato allo stesso runtime. Le fixture
più vecchie e le traiettorie dei modelli ideali/on-off restano evidenza di regressione.

Tre gruppi di integrazione più un server figlio MCP reale verificano gli orizzonti del modello,
gli osservabili di predizione, la tensione posseduta, gli errori azionabili, le revisioni, l'annullamento,
i fork indipendenti e le contabilità complete di sorgente/fase/energia. Tutti i **65** confini
accesi di report/portatile/MCP coincidono. Al confine di 0.6-s:

| Grandezza | Valore |
|---|---:|
| Liquido erogato ed evaporato | 28.0021908833 mg |
| Pressione del binario | 725.327490978 kPa |
| Film liquido rimanente | 0 mg |
| Vapore bruciato | 27.9775490263 mg |
| Calore di reazione | 1231.0121571575 J |
| Dose di ciclo richiesta / erogata più recente | 12 mg / 12.0346025230 mg |
| Ultima predizione di chiusura selezionata | 2.7431038678 mg |
| Lunghezza della predizione | 2000 physical ticks |
| Latch di taglio / tick di taglio in attesa | 1 / 0 |
| Residuo energetico assoluto campionato massimo | `1.51078e-8 J` |
| Residuo di massa assoluto campionato massimo | `4.06576e-18 kg` |
| Residuo di carburante assoluto campionato massimo | `8.97855e-20 kg` |

L'impronta è `ddefea6d870e7e75`; l'hash finale è `b8edcd36b58805b6`; lo SHA-256 della sorgente
del modello è `106bde4e86ccb10cd214d1372cff300c09f277c88ee8811d2d9a144d72ca8326`.
Il comando vivo del ciclo successivo resta 4 mg; predizione ed erogazione misurata sono
riportate separatamente da quel comando.

Le previsioni tengono gli altri comandi degli attuatori, ignorano gli eventi di input esterni futuri,
rispettano un orizzonte intero finito e richiedono una parentesi locale monotona di taglio.
Quelle assunzioni e i parametri fisici non verificati limitano questa evidenza.
ECU/TCU completi, rabbocco del binario, raffinamento magnetico/elettronico/fluido, Unity reale e
l'accettazione di gruppi motopropulsori calibrati restano aperti. Vedi
[CLOSURE_PREDICTION.it.md](CLOSURE_PREDICTION.it.md).

## 2026-10-01: ago elettromagnetico e retroazione di erogazione campionata

Il comando seriale richiesto `dotnet run --file tools/Build.cs -- verify` passa
in locale su Windows x64 con SDK 10.0.401/runtime 10.0.12: **289/289** controlli
gestiti, **226/226** controlli degli assembly Standard su .NET 10, **26/26** gruppi MCP
reali, **16/16** Zig e **6/6** controlli ABI C#. La compilazione Release ha zero
avvisi/errori. Tutti i **176** valori numerici storici coincidono esattamente; l'audit
dei sorgenti trova zero file C/C++/Lua. Non si rivendica una nuova CI Linux/macOS né
un'accettazione reale di Unity Editor/Play/Player/IL2CPP.

File di evidenza:

- `artifacts/reports/needle-final-2026-10-01.log` — esecuzione seriale completa.
- `artifacts/reports/needle-evidence-2026-10-01.json` — ambito leggibile da macchina,
  digest di sorgente/log, impronte di laboratorio, grandezze finali e raffinamento.
- `artifacts/reports/needle-schema-audit-2026-10-01.json` — tutti i **23** laboratori
  passano jsonschema 4.25.1; **16** casi magnetici/arresto/ago/driver malformati vengono rifiutati.
- `artifacts/reports/needle-actuated-cylinder.json` — esperimento/replay completi.

Nove gruppi fisici verificano l'identità energetica discreta magnetica, il lavoro di alimentazione
con segno e il calore del rame non negativo, il Jacobiano analitico della forza, la corrente RL
analitica stazionaria, la dinamica simultanea magnetica/molla, le cerniere conservative degli
arresti di corsa, l'erogazione reale dell'ago fuori quota/finestra, la tensione posseduta, il
distacco dalla sede e la chiusura ritardati, l'eccesso di dose, le transazioni complete e i
contratti immutabili/dimensionali. Lo stepping attivo e gli snapshot allocano **zero managed bytes**.
I test di cattura speculativa della frizione conservano flusso, sorgente/fase, forza media,
calore/lavoro compensati e storia di controllo campionata/tenuta attraverso il batching esatto
e un batch successivo fallito.

Un'ODE indipendente a cinque stati integra posizione/velocità dell'ago, flusso magnetico,
calore del rame e lavoro elettrico. I riferimenti RK4 a **20,000/40,000** passi concordano
entro la tolleranza scalata `1e-10`. Su 5 ms, il raffinamento fisico liscio è:

| Tick | Errore scalato massimo | Errore precedente / errore corrente |
|---|---:|---:|
| 25 microseconds | `8.4976116406e-4` | — |
| 12.5 microseconds | `2.1110507406e-4` | 4.02530 |
| 6.25 microseconds | `5.2689855150e-5` | 4.00656 |
| 3.125 microseconds | `1.3167017353e-5` | 4.00165 |

È un raffinamento elettromagnetico/meccanico liscio del secondo ordine. Contatto,
commutazione di finestra/driver e l'accoppiamento di parete esplicito esistente mantengono
limiti di accuratezza separati; il replay esatto non dimostra un ordine uniforme né un controllo calibrato.

La richiesta isolata di 8-mg eroga **9.8939438959 mg** dopo la chiusura elettrica/meccanica
passiva e il rimbalzo sulla sede, un eccesso di **1.8939438959 mg**. Un'apertura nulla a 20 ms
è seguita da circa **0.0001200614 mg** di erogazione aggiuntiva di rimbalzo prima dell'assestamento.
Il modello conserva quel flusso invece di tagliare la massa sul bersaglio o di equiparare un comando
a tensione nulla a una valvola chiusa. Sono dinamiche di ricerca, non inseguimento di dose accettato
né comportamento misurato dell'iniettore.

Due gruppi portatili controllano record v20 completi di magnete, arresto, ago e driver,
il replay di tutti i confini allo stesso runtime, la ricodifica esatta, conteggi tipizzati limitati,
unità/riferimenti errati, tabelle mancanti/duplicate e il rifiuto di downgrade v19 contraffatti.
Lo SHA-256 della fixture v19 autentica è
`4ff5f6180006d53be5ffb6ef0663cc6de4af45f35f39dc4d52fd7e00e8cdd8c5`;
l'impronta `4f74c6da6d89ab08` e il replay aggiornato allo stesso runtime restano intatti.
Il percorso legacy di quota ideale, le fixture più vecchie e le impronte fisiche esistenti
restano evidenza di regressione.

Tre gruppi di integrazione più un server figlio MCP reale verificano dimensioni/riferimenti
stretti, la guida del comando di tensione posseduta, le revisioni, l'annullamento, i fork
indipendenti e le contabilità magnetiche/di sorgente/fase/termiche. Tutti i **65** confini
di report/portatile/MCP concordano. Il laboratorio acceso di 0.6-s termina con:

| Grandezza | Valore |
|---|---:|
| Liquido erogato | 40.3903592511 mg |
| Pressione del binario | 692.292375330 kPa |
| Carburante evaporato | 34.5106221450 mg |
| Film liquido rimanente | 5.8797371061 mg |
| Vapore bruciato | 31.2333851260 mg |
| Calore di reazione | 1374.2689455424 J |
| Lavoro di alimentazione elettrica | 0.168028754073 J |
| Calore del rame | 0.167510468341 J |
| Energia magnetica | `6.30199e-16 J` |
| Comando di bobina tenuto | 0 V |
| Alzata reale dell'ago | 0.4842244877 micrometers |
| Velocità reale dell'ago | -0.1290065607 m/s |
| Richiesta agganciata / dose erogata reale più recenti | 4 mg / 5.8930695115 mg |
| Residuo energetico assoluto campionato massimo | `1.57642e-8 J` |
| Residuo di massa assoluto campionato massimo | `3.30682e-18 kg` |
| Residuo di carburante assoluto campionato massimo | `9.48677e-20 kg` |

Il confine finale ha ancora un ago in moto, quasi seduto, e un film evaporato in modo incompleto.
Il nuovo esperimento controlla quindi l'inventario rimanente finito e la contabilità completa del
carburante invece di ereditare la condizione di film secco del laboratorio a iniettore ideale.
Superare i KPI numerici non stabilisce la dose comandata esatta.
L'impronta del modello è `3fa813ff44b95a79`; l'hash finale è `de68045d420b9ffa`; lo SHA-256
della sorgente è `d8c7472a3335a523821209cb183cf34687e454f326d9ac1946c618b7ca73050c`.

Tutti i parametri restano `unverified`. Induttanza lineare non saturata, R costante,
ago equilibrato in pressione, arresti elastici e comando di tensione ideale sono riduzioni
dichiarate. Mappe magnetiche/termiche non lineari, comando di commutazione/flyback/batteria,
forze fluide assiali, spray/spostamento, pompa/rabbocco del binario, ECU/TCU completi e
gruppi motopropulsori misurati restano aperti. Le viste Unity preparate di bobina/arresto/controllore
e la scala della corsa dell'ago richiedono la verifica reale di Editor/Play. Vedi
[NEEDLE_ACTUATION.it.md](NEEDLE_ACTUATION.it.md).

## 2026-10-01: binario liquido cedevole e iniezione per ciclo

Il comando seriale richiesto `dotnet run --file tools/Build.cs -- verify` passa
in locale su Windows x64 con SDK 10.0.401/runtime 10.0.12: **275/275** controlli
gestiti, **215/215** controlli degli assembly Standard ospitati su .NET 10, **25/25** gruppi
MCP reali, **16/16** test Zig e **6/6** controlli ABI C#. La compilazione Release segnala
zero avvisi/errori. Tutti i **176** valori di baseline storici coincidono esattamente;
l'audit dei sorgenti trova zero file C/C++/Lua. Questo non stabilisce una nuova CI Linux/macOS
né l'accettazione reale di Unity Editor/Play/Player/IL2CPP.

File di evidenza:

- `artifacts/reports/liquid-injector-final-2026-10-01.log` — esecuzione seriale completa.
- `artifacts/reports/liquid-injector-evidence-2026-10-01.json` — ambito leggibile da
  macchina, digest del log, impronte correnti di laboratorio/sorgente, grandezze finali e
  raffinamento indipendente.
- `artifacts/reports/liquid-injector-schema-audit-2026-10-01.json` — tutti i **22**
  laboratori passano jsonschema 4.25.1; **16** casi di iniettore liquido malformati vengono rifiutati.
- `artifacts/reports/liquid-injected-cylinder.json` — esperimento e replay completo.

Otto gruppi fisici controllano il decadimento analitico della prevalenza di pressione, l'inventario
finito della sorgente cedevole, il lavoro esatto del binario, il calore passivo dell'ugello, le
contabilità calorica/chimica/di pressione, l'aggancio della quota, la chiusura per pressione inversa
e l'inversione senza riemissione della quota. L'esaurimento raggiunge la pressione prescritta del
ricevitore senza inventare carburante. Fork indipendenti, input rifiutati/annullati, fallimento
tardivo dopo un'iniezione accettata e cattura speculativa della frizione conservano ogni storia di
sorgente/film/quota/calore. L'iniezione attiva a caldo e gli snapshot allocano **zero managed bytes**.
Si esercitano proprietà esplicite, fattibilità del volume di sorgente, unità, proprietà di film/albero,
capacità di stato limitata e compilazione immutabile.

Un'ODE simultanea indipendente a otto stati integra il liquido erogato, la massa del film,
la temperatura di parete, massa/energia interna del gas ricevitore e tre storie di pressione/calore.
Esecuzioni RK4 indipendenti a **20,000/40,000** passi concordano entro la tolleranza scalata
dichiarata `1e-10`. Su 0.2 s in una finestra avanti liscia, i tick fisici di 20/10/5/2.5-ms danno
errori scalati massimi:

| Tick | Errore scalato massimo | Errore precedente / errore corrente |
|---|---:|---:|
| 20 ms | `2.9306688569e-7` | — |
| 10 ms | `7.3266958993e-8` | 3.999987 |
| 5 ms | `1.8316757833e-8` | 3.999996 |
| 2.5 ms | `4.5791930144e-9` | 3.999997 |

Questo stabilisce un accoppiamento liscio di iniezione/evaporazione del secondo ordine sotto la
riduzione dichiarata. Gli eventi di finestra a tick fisso, l'esaurimento e altre sorgenti di parete
esplicite conservano i propri limiti di accuratezza; non si rivendica un ordine uniforme del
gruppo motopropulsore acceso.

Due gruppi portatili verificano record v19 di sorgente/ugello/fasatura, ricodifica esatta,
replay a ogni confine, conteggi tipizzati limitati, unità/proprietà errate, record duplicati/mancanti
e downgrade v18 contraffatti. La fixture di film v18 autentica conserva
SHA-256 `c0e59c6f6527b2b59ee1094cd6627c0829ecbd4ab0eb2e6ec06394ccfe15da97`,
impronta `cb103bce098f4e82` e replay aggiornato allo stesso runtime. Le fixture precedenti
e la fisica priva di film restano evidenza di regressione. La revisione aggiorna anche gli offset
correnti di header nei test dei record malformati, conservando le dimensioni delle tabelle delle versioni vecchie.

Tre gruppi di integrazione più un server figlio MCP reale controllano la sorgente finita condivisa,
la deposizione liquida, la reazione solo del vapore, i bilanci sorgente/film/parete, i documenti
stretti e revisioni/annullamento/fork di sessione. Tutti i **65** confini di report/portatile/MCP
concordano. Il laboratorio di 0.6-s inizia con un film secco e **500 mg** di
liquido a **800 kPa**, e termina con:

| Grandezza | Valore |
|---|---:|
| Liquido erogato ed evaporato | 28 mg |
| Liquido di sorgente rimanente | 472 mg |
| Pressione del binario | 725.333333333 kPa |
| Liquido di film rimanente | 0 mg |
| Lavoro di pressione del binario rilasciato | 0.028472888889 J |
| Lavoro di pressione del ricevitore esportato | 0.003236038241 J |
| Calore dell'ugello | 0.025236850648 J |
| Calore del film prelevato dalla parete | 14 J |
| Temperatura di parete del film | 498.602523685 K |
| Vapore bruciato | 27.9759472237 mg |
| Calore di reazione | 1230.9416778442 J |
| Residuo energetico assoluto campionato massimo | `5.52370e-9 J` |
| Residuo di massa assoluto campionato massimo | `1.08420e-18 kg` |
| Residuo di carburante assoluto campionato massimo | `7.45389e-20 kg` |

Il ciclo finale osservato conserva una richiesta/erogazione di **12-mg** mentre il comando vivo
è **4 mg** per una finestra futura. Accettazione della dose, erogazione e reazione restano distinte.
L'impronta del modello è `4f74c6da6d89ab08`; l'hash finale è
`ddf5b1c4b0678451`; lo SHA-256 della sorgente è
`85b9cba983e31258d9341943dab3c844a18c05c5320dda3d9d610a3935a14d09`.

L'energia di pressione del binario è energia interna accumulata; il calore dell'ugello entra nella
parete finita. Il ricevitore a volume liquido trascurabile esporta esplicitamente il lavoro di
pressione di spostamento invece di accreditare un volume di gas nascosto o lavoro all'albero.
Il suo riferimento di cedevolezza a pressione nulla e le proprietà costanti sono riduzioni di
ricerca dichiarate. Tutti i parametri restano `unverified`. Pompa/rabbocco, mappe di
contropressione/proprietà, dinamica di ago/elettrica/spray, accoppiamento a volume liquido finito,
accensione/ECU, trasmissione/controllo completi e gruppi motopropulsori di veicolo calibrati restano
aperti. Le viste Unity di binario/ugello e i test di ciclo di vita sono preparati ma richiedono
l'Editor fissato. Vedi [LIQUID_FUEL_INJECTION.it.md](LIQUID_FUEL_INJECTION.it.md).

## 2026-10-01: film di carburante liquido finito e trasporto simmetrico

Il comando seriale richiesto `dotnet run --file tools/Build.cs -- verify` passa
su Windows x64 con SDK 10.0.401/runtime 10.0.12: **262/262** controlli gestiti,
**205/205** controlli degli assembly Standard ospitati su .NET 10, **24/24** gruppi MCP reali,
**16/16** test Zig e **6/6** controlli ABI C#. Tutti i **176** valori di baseline storici
conservati coincidono esattamente. L'audit dei sorgenti trova zero file C/C++/Lua. La compilazione
Release segnala zero avvisi/errori. È evidenza locale dell'albero di lavoro, senza nuova CI
Linux/macOS né accettazione reale di Unity Editor/Play/Player/IL2CPP.

File di evidenza:

- `artifacts/reports/fuel-film-final-2026-10-01.log` — esecuzione seriale completa.
- `artifacts/reports/fuel-film-evidence-2026-10-01.json` — ambito leggibile da macchina,
  digest del log, impronte di laboratorio, grandezze di fase ed errori di raffinamento.
- `artifacts/reports/fuel-film-schema-audit-2026-10-01.json` — tutti i **21** documenti
  di laboratorio passano jsonschema 4.25.1; **12** casi di film malformati vengono rifiutati.
- `artifacts/reports/film-fired-cylinder.json` — esperimento e replay completo.

Dieci gruppi fisici verificano il riscaldamento analitico del bagno finito, il riferimento di fase
con segno, saturazione/essiccamento, disponibilità di calore limitata, raffreddamento, conduttanza
nulla, reazione reale solo del vapore e contabilità indipendenti di costituenti/chimica/termica.
Fork completi e batch annullati o rifiutati in ritardo conservano tutte le storie; lo stepping a
caldo e le letture di snapshot allocano **zero managed bytes**. Si esercitano porte/unità errate,
proprietà di fase non valide, overflow della capacità di stato e compilazione immutabile.

La revisione ha corretto il secondo mezzo passo da film/gas a gas/film e ha invertito la spazzata
del film sulla parete condivisa. Questo rende simmetrici film/gas/meccanica/gas/film, conservando
il percorso di solver esistente per i modelli privi di film. Riferimenti RK4 simultanei indipendenti
a 20,000 e 40,000 passi concordano entro la tolleranza scalata dichiarata `1e-10`. Uno studio liscio di film saturo di two-second usa tick fisici di 40/20/10/5 ms e l'errore scalato massimo su
massa liquido/gas, temperatura di parete, energia interna del gas e costituenti trasportati:

| Accoppiamento | Errore a 40 ms | Errore a 5 ms | Rapporti di dimezzamento successivi |
|---|---:|---:|---|
| Due film che condividono una parete finita | `3.43324e-8` | `5.36263e-10` | 4.0000, 4.0002, 4.0012 |
| Vapore del film che esce da una porta gas a flusso bloccato | `3.17195e-5` | `4.89033e-7` | 4.0495, 4.0029, 4.0014 |
| Film più scambio termico gas-parete | `5.30200e-4` | `6.61364e-5` | 2.0024, 2.0012, 2.0006 |

I casi di film condiviso e di trasporto del vapore mostrano un raffinamento liscio del secondo ordine.
Le altre sorgenti di calore di parete conservano la temperatura esplicita dell'intervallo esterno e il
limite di accoppiamento del primo ordine. Questi controlli non stabiliscono un secondo ordine uniforme
attraverso essiccamento, eventi di valvola/reazione o un gruppo motopropulsore acceso completo.

Due gruppi di asset verificano le grandezze di fase v18, la codifica deterministica, il replay a
ogni confine, i record tipizzati limitati, unità/conteggi malformati, record duplicati/mancanti e il
rifiuto di downgrade v17 contraffatti. La fixture di dosatura v17 autentica di SHA-256
`d51dc0968bc3d154b2c3b227f74b7210215d4ee00c5a47e8dd16dc1d71cd5da1`
conserva l'impronta `099db1021c8df1fe` e il replay aggiornato allo stesso runtime. Le fixture
precedenti e le impronte dei modelli privi di film restano evidenza di regressione.

Tre gruppi di integrazione e il server figlio MCP reale verificano lo stesso inventario finito,
l'energia di fase, la reazione solo del vapore, i documenti stretti e il comportamento di
revisione/fork/annullamento. Tutti i **63** confini JSON/report/portatile/MCP concordano. Il
laboratorio di 0.6-second inizia con **40 mg** di bagnatura esplicita e termina con:

| Grandezza | Valore |
|---|---:|
| Liquido rimanente | 0 mg |
| Carburante evaporato | 40 mg |
| Calore prelevato dalla parete finita | 20 J |
| Temperatura di parete del film | 498 K |
| Vapore bruciato | 31.1117599551 mg |
| Calore di reazione | 1368.9174380261 J |
| Residuo energetico finale | `-1.58434e-9 J` |
| Residuo energetico assoluto campionato massimo | `2.15960e-9 J` |
| Residuo di massa assoluto campionato massimo | `1.49078e-18 kg` |
| Residuo di carburante assoluto campionato massimo | `2.09641e-19 kg` |

L'impronta del modello è `cb103bce098f4e82`; l'hash finale è `495582b10f40832c`.
Lo SHA-256 della sorgente è
`8c933a5889d3b8f22f37738e307989d2c0fa5c7dfe9018ffe50a21f19e2aa416`.
Tutti i parametri restano `unverified`. La bagnatura iniziale non è iniezione liquida;
proprietà di fase dipendenti dalla pressione, reintegro, accensione/ECU, trasmissione/controlli
completi e gruppi motopropulsori misurati restano aperti. I marcatori di film Unity preparati
e i test di ciclo di vita richiedono l'Editor fissato. Vedi [FUEL_FILM.it.md](FUEL_FILM.it.md).

## 2026-09-30: binario carburante finito e dosatura per ciclo

Il checkpoint pubblicato `dc7ec2d` dell'accumulatore a gas passa la
[CI Windows/Linux/macOS](https://github.com/Water-Run/Power/actions/runs/36710249585).
Quell'evidenza copre la sorgente precedente, non il nuovo iniettore né Unity reale.

Il comando seriale richiesto `dotnet run --file tools/Build.cs -- verify` passa
in locale su Windows x64 con SDK 10.0.401/runtime 10.0.12: **247/247** controlli gestiti,
**193/193** controlli degli assembly Standard ospitati su .NET 10, **23/23** gruppi MCP reali,
**16/16** Zig e **6/6** controlli ABI C#. Tutti i **176** valori numerici storici
coincidono esattamente; l'audit dei sorgenti trova zero file C/C++/Lua. La compilazione Release
ha zero avvisi/errori. Log: `artifacts/reports/fuel-injector-final-2026-09-30.log`.

Otto gruppi fisici verificano la fasatura avanti/avvolta, il binario finito e la quota esatta,
le richieste agganciate, la chiusura per pressione inversa e l'esaurimento, l'inversione senza
riemissione della quota, un'ODE indipendente di massa/entalpia a due recipienti con raffinamento
liscio, la combustione premiscelata dosata analitica, le transazioni complete, dimensioni/compatibilità/capacità
e geometria immutabile. Lo stepping a caldo e gli snapshot allocano **zero managed bytes**.
Fallimento tardivo di coppia/volume, annullamento, rifiuto degli input, batching esatto e fork
conservano tutte le storie di erogazione/ordinale. Le discontinuità di fasatura conservano i
requisiti di raffinamento a tick fisso; non si fa alcuna rivendicazione di tempo di commutazione continuo.

Due gruppi portatili verificano ugello/fasatura v17, record tipizzati limitati, unità errate,
record mancanti/duplicati e il rifiuto di downgrade contraffatti. La fixture autentica
dell'accumulatore a gas v16 conserva SHA-256
`72f0ee5b3180b763de091674565b2b8f2cbd61f80a3e4ab46e9694ad153a79c7`, impronta
`739f2baba8c669a0` e replay aggiornato allo stesso runtime a ogni confine. Le fixture precedenti
e le impronte fisiche restano invariate. Tre gruppi di integrazione verificano l'esaurimento reale
del binario, il carburante erogato/reagito/di confine, i contratti stretti e le sessioni.

Tutti i **20** documenti di laboratorio passano la validazione strutturale dello schema; **12** casi
di iniettore malformati sono rifiutati da jsonschema 4.25.1. Valori di ciclo, porte compatibili
finite, massimi di dose e proprietà dell'albero restano controlli aggiuntivi del compilatore. Report:
`artifacts/reports/fuel-injector-schema-audit.json`. Anche gli audit di schema esistenti passano
tutti i 20 documenti. Un ricevitore di serbatoio non valido ora restituisce una diagnostica di
connessione prima della validazione della frazione di serbatoio. R/gamma/LHV/stechiometria diverse
vengono rifiutate, così i trasferimenti interni non possono inventare inventario chimico.

`metered-fired-cylinder` sostituisce l'aspirazione di carburante premiscelato con aria pura più un
binario gassoso finito. Le dosi richieste sono 8/12/4 mg; si agganciano alla finestra avanti
successiva. A 0.6 s il ciclo osservato più recente tiene ancora 12 mg, mentre il comando vivo
è 4 mg per una finestra futura. Esecuzione e accettazione della dose restano distinte
dall'erogazione reale. Tutti i **65** confini di report/portatile/MCP coincidono esattamente.

| Grandezza finale | Valore |
|---|---:|
| Carburante erogato | 28 mg |
| Carburante bruciato | 27.9299615615 mg |
| Calore di reazione rilasciato | 1228.918308706072 J |
| Carburante di camera rimanente | 0.0055539661 mg |
| Carburante netto di confine | -0.0644844724 mg |

Carburante bruciato, rimanente e perso al confine rendono conto del carburante erogato. Il trasferimento
interno del binario non aggiunge alcun ingresso esterno di energia del carburante né una sorgente
illimitata. L'entalpia termica del binario e l'energia chimica usano lo stesso flusso limitato di costituenti.

| Errore assoluto massimo sull'esperimento | Valore | Limite asserito |
|---|---:|---:|
| Esaurimento del binario rispetto all'erogazione | 3.67e-18 kg | 1e-16 kg |
| Carburante erogato rispetto a reagito/rimanente/di confine | 6.78e-21 kg | 1e-14 kg |
| Energia dell'intero modello | 1.52e-9 J | 1e-6 J |
| Massa totale | 4.07e-18 kg | 1e-14 kg |
| Costituente carburante | 3.67e-18 kg | 1e-14 kg |
| Costituente aria fresca | 1.20e-18 kg | 1e-14 kg |

Impronta `099db1021c8df1fe`; hash finale Windows/runtime `329e1109392b37f3`.
Report: `artifacts/reports/metered-fired-cylinder.json` e
`fuel-injector-evidence-summary.json`.

Tre esecuzioni CLI seriali, ciascuna con due traiettorie di 0.6-s (24,000 tick accettati),
replay completo e 65 confini, impiegano **0.502 / 0.364 / 0.370 s**, mediana **0.370 s**
a carico desktop ordinario. Le tracce coincidono esattamente; lo stepping a caldo a zero allocazioni
è controllato separatamente su entrambi gli assembly. È un costo locale osservato, non uno
speedup né una garanzia di throughput portatile.

L'agente 0.20.0 e l'MCP reale verificano la scoperta della dose in kg, export/replay completi, il
rifiuto di quote non valide e i fork indipendenti di controllore/carburante. Le viste Unity di
iniettore/binario/fasatura e i test Edit/Play sono preparati in sorgente C# 9. Unity Editor/Play/
Mono/IL2CPP/Player reali e l'evidenza nuova su tre piattaforme per questo incremento restano
separati. È dosatura gassosa ideale con proprietà di gas costanti comuni; spray/evaporazione
liquidi, hardware di ago/binario/serbatoio, iniezione di benzina calibrata, accensione/ECU e il
gruppo motopropulsore completo restano incompiuti. I parametri non sono verificati.

## 2026-09-30: stantuffo a gas e accumulatore a energia finita

Lo sviluppo è ripreso su richiesta del proprietario. Il checkpoint precedente `cebc978` del cursore
ha passato la [CI Windows, Linux e macOS](https://github.com/Water-Run/Power/actions/runs/36695753045);
ogni job della matrice ha completato la verifica seriale richiesta. Log scaricato:
`artifacts/reports/spool-three-platform-2026-09-30.log`. Quell'evidenza copre la
sorgente del cursore pubblicata, non il nuovo incremento dello stantuffo a gas né Unity reale.

Il nuovo comando seriale richiesto passa su Windows x64 locale:
`dotnet run --file tools/Build.cs -- verify`. Risultati: **234/234** controlli gestiti,
**183/183** controlli degli assembly Standard ospitati su .NET 10, **22/22** gruppi MCP reali,
**16/16** Zig e **6/6** controlli ABI C#. Tutti i **176** valori storici coincidono esattamente;
l'audit dei sorgenti trova zero file C/C++/Lua. La compilazione Release ha zero avvisi/errori.
Log: `artifacts/reports/gas-accumulator-final-2026-09-30.log`.

Otto gruppi fisici coprono il lavoro adiabatico analitico e il suo Jacobiano, la corsa piccola,
camere con segno/contrapposte, RK4 indipendente di massa/energia e raffinamento liscio del
secondo ordine, moto comune gas/fluido, un riferimento RK4 separato di parete finita con
raffinamento accoppiato alla parete del primo ordine, afflusso di gas a volume mobile ed entalpia
del serbatoio, transazioni complete, geometria/unità e stepping senza allocazioni. La contabilità
di afflusso ha misurato circa `1.14e-17 kg` di residuo cumulativo in virgola mobile dopo aggiornamenti
ripetuti della massa; il suo limite `1e-16 kg` riflette quell'accumulo. Chiudere la porta
conserva esattamente la massa accettata. Nessuna correzione di massa o energia forza un passaggio.

Due gruppi portatili conservano geometria v16 completa, direzione con segno, conteggi tipizzati
limitati, unità malformate, record mancanti/duplicati e rifiuto dei downgrade. La fixture autentica
del cursore v15 coincide byte per byte con il pacchetto del checkpoint pubblicato:
SHA-256 `67b976a27ca0bd7343ca6b024c5bc736e14f48326945da841785ade43b61f14b`,
impronta `28aa0965d248e280`. Tutti i confini di evento originali/aggiornati coincidono entro
il runtime in esecuzione. Le fixture precedenti e le impronte fisiche restano intatte.

Tutti i **19** documenti di laboratorio passano la validazione dello schema. **12** documenti
di stantuffo a gas malformati sono rifiutati separatamente da jsonschema 4.25.1. Volume nominale
positivo e proprietà univoca della geometria restano controlli aggiuntivi del compilatore. I report
sono sotto `artifacts/reports`, compreso `gas-piston-schema-audit.json`; anche gli audit esistenti
di stantuffo, cursore, batteria/duty e controllore di pressione passano tutti i 19 documenti.

Il modello `gas-accumulator-pump` di six-second aggiunge una camera a gas di 50-ml e un
separatore di 50-g alla pompa elettrica, al bypass meccanico del cursore e alla frizione di pressione.
Il gas parte a 200 kPa assoluti/300 K, con un riferimento dichiarato di 100-kPa e una penetrazione
di appoggio cedevole di 0.1-mm. Durante l'impulso di 3-4-s, la tensione del motore è 6 V ed entrambi
i percorsi di riempimento/scarico sono esplicitamente aperti. L'energia interna del gas scende di
**0.6196209894 J**; il lavoro alla pressione di riferimento è **-0.2094578629 J**. Dopo l'energia
cinetica/di arresto del separatore e lo smorzamento, l'erogazione netta al liquido è **0.4092090634 J**,
con **2.094578629 ml** di volume spazzato restituito. Il riempimento riprende e la frizione è bloccata
con slittamento nullo al confine finale. Sono risultati sintetici, non una calibrazione OEM.

| Errore assoluto massimo su tutti i 306 confini | Valore | Limite asserito |
|---|---:|---:|
| Conto separato di pompa/fluido/moto/gas/riferimento/calore | 6.09e-13 J | 1e-8 J |
| Energia dell'intero modello | 1.83e-8 J | 1e-6 J |
| Conto del volume di riferimento del liquido | 5.19e-19 m3 | 1e-16 m3 |
| Invariante adiabatico del gas chiuso normalizzato | 6.83e-13 J | 1e-8 J |

Ogni **306** confine JSON/portatile/MCP ha hash e valori identici. Impronta
`739f2baba8c669a0`; hash finale Windows/runtime `074917dc8e343131`. Report:
`artifacts/reports/gas-accumulator-pump.json` e `gas-accumulator-evidence-summary.json`.
Il conto gas/fluido sottrae esplicitamente il lavoro di riferimento e il potenziale iniziale
di arresto; l'energia interna assoluta del gas da sola non è etichettata come energia del fluido erogata.

### Ottimizzazione misurata della camera chiusa

I modelli nuovi di stantuffo a gas chiusi e non miscelati, senza trasporto di gas né legami termici,
conservano la validazione di stato e saltano l'integrazione a tasso nullo. Tre esecuzioni CLI seriali
complete per stadio includono due traiettorie di six-second (600,000 tick accettati) e tutti i 306
confini. I tempi di baseline erano **2.727 / 2.603 / 2.496 s**; i tempi ottimizzati erano
**2.382 / 2.362 / 2.357 s**. Il costo mediano diminuisce di circa il **9.3%** su questo desktop.
Tutte e tre le tracce prima/dopo coincidono esattamente su ogni osservabile e hash di stato.
L'allocazione stazionaria di Step/ReadSnapshot resta **zero managed bytes**. È una misura locale,
non una garanzia di throughput portatile. Porte, legami di parete e trasporto dei costituenti
conservano il percorso di integrazione ordinario e test indipendenti.

L'agente 0.19.0/MCP verifica la scoperta termodinamica, export/replay completi, le revisioni
e i fork indipendenti gas/fluido. Le viste Studio e i test Edit/Play sono preparati in
sorgente C# 9. Unity Editor/Play/Mono/IL2CPP/Player reali e la verifica nuova su tre piattaforme
di questo incremento restano separati. Motore completo, trasmissione, ECU/TCU, campioni di
veicolo calibrati e applicazione desktop accettata restano incompiuti. Tutti i confini dei campioni,
le licenze e la provenienza sono conservati.

## 2026-09-30: checkpoint del regolatore meccanico a cursore

Il comando seriale richiesto passa su Windows x64, SDK 10.0.401/runtime 10.0.12:
`dotnet run --file tools/Build.cs -- verify`. Il risultato è **221/221** controlli
gestiti, **173/173** controlli degli assembly Standard ospitati su .NET 10, **21/21** gruppi
MCP di server figlio reali, **16/16** Zig e **6/6** controlli ABI C#. Tutti i **176**
valori numerici storici coincidono esattamente. La compilazione Release ha zero avvisi/errori;
l'audit dei sorgenti trova zero file C/C++/Lua. Log:
`artifacts/reports/spool-final-2026-09-30.log`.

Il [contratto del cursore](HYDRAULIC_SPOOL.it.md) aggiunge una fascia di dosatura equilibrata
in pressione, trascurando esplicitamente la forza assiale del getto. La posizione reale dello
stantuffo ed entrambe le pressioni delle porte fluido partecipano alla soluzione di Newton
condivisa con derivate analitiche. Sette gruppi fisici verificano la corsa con segno, il flusso
bidirezionale passivo e le derivate, la pressione stazionaria indipendente, un transitorio RK4
separato a tre stati con raffinamento liscio del secondo ordine, l'errore decrescente attraverso
l'apertura, l'equalizzazione a porte finite, le transazioni e la geometria immutabile. L'allocazione
a caldo di Step/ReadSnapshot resta zero su entrambi i target Core. Fallimento tardivo, annullamento
e fork indipendenti conservano tutto lo stato di pressione/moto/calore. Non si rivendica un ordine
uniforme non liscio.

Due gruppi portatili coprono la geometria v15 completa e conteggi/tipi/unità malformati contraffatti,
record mancanti/duplicati e rifiuto dei downgrade. Lo SHA-256 della fixture autentica dello stantuffo v14 è
`72e0605d00972a58e2f5358aa8cbd44417e81cf1c452a20ab9661eac0919bae0`; la sua impronta,
i riferimenti fisici e il replay aggiornato esatto allo stesso runtime conservano ogni confine
di evento. Le fixture precedenti sono intatte. Tre gruppi di integrazione verificano documenti
stretti, revisioni/annullamento/fork di sessione e una contabilità separata fluido/moto.
Le unità errate della posizione di dosatura conservano la diagnostica di unità invece di essere
intercettate come errori di intervallo del costruttore.

Tutti i **18** documenti di laboratorio passano la validazione strutturale dello schema. **12** casi
di cursore malformati sono rifiutati in modo indipendente da jsonschema 4.25.1. Report:
`artifacts/reports/spool-schema-audit.json`. Corsa con segno non nulla, posizioni della fascia
entro la corsa dello stantuffo e proprietà tipizzata restano controlli aggiuntivi del compilatore.

L'esperimento `spool-regulated-pump` esegue **3 s** con tick di **20,000 ns** e
**156** confini JSON/portatile/MCP coincidenti. Il suo attuatore di pressione mobile, la molla
di ritorno/lo smorzamento e il bypass regolano la linea senza un controllore di valvola campionato.
I 2 N*m di trazione/freno sono carichi di ricerca espliciti. Il criterio ereditato di cattura ad
alta pressione di two-second falliva alla pressione regolata più bassa; l'esperimento ora dura
abbastanza da osservare la cattura reale, conservando le asserzioni di slittamento nullo e di modo
bloccato. Nessuno stato di pressione, energia o slittamento è corretto per far passare il test.

| Grandezza finale | Valore |
|---|---:|
| Pressione di linea | 233956.17402528782 Pa |
| Spostamento del cursore | 0.00016975695474013045 m |
| Apertura di dosatura | 0.08487847737006522 |
| Calore di restrizione del cursore | 2.917614235503143 J |
| Calore di smorzamento di ritorno | 0.0006249365922203377 J |
| Lavoro idraulico della pompa | 3.3299555096992406 J |
| Calore della frizione | 260.2402810714738 J |
| Modo/slittamento finale della frizione | Locked / 0 rad/s |

Su tutti i confini, il bilancio separato idraulico/moto/molla/pastiglia/calore ha errore massimo
**9.77e-15 J** (limite asserito 1e-8 J), energia globale **1.99e-8 J**
(limite 1e-6 J) e inventario del volume di riferimento **1.35e-20 m3** (limite 1e-16 m3).
Impronta `28aa0965d248e280`, hash finale Windows/runtime `180744d025212ef7`.
Report: `artifacts/reports/spool-regulated-pump.json` e
`artifacts/reports/spool-evidence-summary.json`.

Tre esecuzioni CLI seriali, ciascuna comprendente due traiettorie complete (300,000 tick
accettati), tutti i controlli di replay e 156 confini di uscita, hanno impiegato **1.183 / 1.230 / 1.153 s**;
mediana **1.183 s** a carico desktop ordinario. È un costo di checkpoint osservato, non una
garanzia di throughput multipiattaforma né evidenza di speedup. Matrice, pendenza della fascia
e buffer di rollback sono limitati e di proprietà della simulazione; stepping/rilettura stazionari
restano senza allocazioni.

L'agente 0.18.0 annuncia l'idraulica regolata meccanicamente, le unità di geometria e l'omissione
della forza del getto. I controlli MCP reali coprono export/replay completi e il rifiuto di una
scrittura tentata sull'uscita di apertura. Le viste Unity di valvola/attuatore e i test Edit/Play
sono preparati in sorgente C# 9. `POWER_UNITY_EDITOR` non è impostato; Editor/Play/Mono/IL2CPP/Player
reali e la verifica nuova Linux/macOS restano in sospeso. Il proprietario chiude lo sviluppo di
oggi a questo checkpoint numerico; Power! completo e la calibrazione non sono rivendicati. La
consegna di sorgenti/pacchetti conserva licenze e confini dei campioni.

## 2026-09-30: stantuffo dinamico e frizione azionata per contatto

Il comando seriale richiesto è passato su Windows x64, SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **209/209** controlli gestiti, **164/164** controlli degli assembly Standard ospitati su .NET 10
  e **20/20** gruppi MCP di server figlio reali.
- **16/16** Zig e **6/6** controlli ABI C#; tutti i **176** valori storici coincidono esattamente.
  L'audit dei sorgenti trova zero file C/C++/Lua. Compilazione Release: zero avvisi/errori.
- Log: `artifacts/reports/piston-final-2026-09-30.log`.
- Tutti i **17** laboratori passano la validazione JSON Schema. Audit separati rifiutano ciascuno
  **12** casi malformati di stantuffo, batteria/duty e controllo di tensione con jsonschema
  4.25.1. I report sono `piston-schema-audit.json`, `battery-schema-audit.json` e
  `pressure-controller-schema-audit.json` sotto `artifacts/reports`.

Gli [stantuffi idraulici](HYDRAULIC_PISTON.it.md) aggiungono massa esplicita, spostamento, volume
spazzato di camera, molla/smorzamento, gioco delle pastiglie ed estremità di corsa cedevoli. Le
frizioni di contatto ricavano la capacità dalla forza sulle pastiglie. Otto gruppi di controlli
fisici coprono il lavoro di cerniera e le derivate analitiche, storie di smorzamento compatte
separate/decadimento analitico, oscillazione accoppiata molla/fluido con raffinamento liscio del
secondo ordine, lavoro e volume di serbatoio finito/posteriore, raffinamento RK4 a tratti del
contatto, riempimento/cattura/rilascio liberi, contratti tipizzati e transazioni complete. Non si
rivendica un ordine uniforme attraverso gli eventi di contatto. Fallimento numerico tardivo,
annullamento, fork e batching esatto conservano l'intero stato. Lo stepping a caldo del nucleo e
le letture di snapshot allocano **zero managed bytes**.

Il laboratorio sintetico `piston-actuated-clutch` avanza di **15 s** a tick di **20,000 ns**
con controllo di duty campionato a **5 ms**. Tutti i **761** confini di report/portatile/MCP
hanno hash e valori osservabili identici in questo runtime. La pressione durante il riempimento
libero non produce forza sulle pastiglie. Lo scarico schedulato rilascia la frizione; il riempimento
successivo la cattura. Al confine finale:

| Grandezza | Valore |
|---|---:|
| Spostamento dello stantuffo | 0.0021772797986273195 m |
| Forza sulle pastiglie | 177.27979862731945 N |
| Capacità statica/strisciante | 22.69181422429689 / 11.345907112148446 N*m |
| Pressione della camera anteriore | 199053.02928772685 Pa |
| Energia accumulata di pastiglia/arresto | 0.01571406350067147 J |
| Calore cumulativo di smorzamento di ritorno | 0.0025718766359138913 J |

Impronta `46f746398142c258`; hash finale Windows/runtime `3a7b8eee248785d3`.
Report: `artifacts/reports/piston-actuated-clutch.json`. La sorgente usa esplicitamente
uno smorzamento sintetico di 300 N*s/m per mantenere l'alimentazione sufficiente durante il
transitorio di contatto; il solver conserva il rifiuto della pressione negativa invece di
bloccare quello stato.

### Conti energetici indipendenti ed evidenza delle tolleranze

Ogni confine confronta in modo indipendente il lavoro della pompa con fluido cedevole, cinetica
dello slider, energia di molla di ritorno e pastiglia più calore di restrizione/smorzamento; la
perdita chimica/RC della batteria con energia cinetica/induttiva del motore, lavoro della pompa
e calore elettrico; e il lavoro rotazionale esterno con energia dei rotori e calore della frizione.

| Conto | Errore assoluto massimo sull'esperimento | Limite asserito |
|---|---:|---:|
| Idraulica, moto e contatto | 1.56e-13 J | 1e-9 J |
| Alimentazione elettrica e motore | 2.48e-8 J | 1e-7 J |
| Nodo termico condiviso rispetto alle storie di calore dirette | 2.87e-7 J | 5e-7 J |
| Rotori trascinati e frizione | 1.16e-6 J | 2e-6 J |
| Modello intero | 1.42e-6 J | 5e-6 J |

L'asserzione idraulica iniziale di 1e-7-J inferiva un piccolo calore di molla sottraendo il grande
calore della frizione dalla temperatura arrotondata del nodo condiviso. È fallita, e quel calore
inferito diminuiva persino vicino al moto stazionario. Un canale diretto di smorzamento compensato
ora conserva la dissipazione fisica in modo indipendente; il bilancio idraulico più stretto sopra
passa. L'accumulo termico e rotazionale spiega il residuo globale rimanente. La soglia globale
ereditata di 1e-6-J era insufficiente per questa esecuzione di 750,000 tick; 5e-6 J è un limite
numerico esplicito di lunga durata, integrato dai controlli più stretti analitici, di volume fluido
e di energia separata. Nessuno stato energetico è corretto per forzare un passaggio.
Audit supplementare: `artifacts/reports/piston-evidence-summary.json`.

### Ambito delle prestazioni

Tre esecuzioni CLI seriali a carico desktop ordinario includono ciascuna **due** traiettorie
complete (1.5 million tick accettati), controlli di replay e 761 confini di uscita.
Le mediane trascorse erano **4.007 s** prima della derivata analitica/storie di smorzamento,
**4.100 s** con la derivata analitica e le storie indicizzate per componente complete, e
**4.234 s** con storie compatte. Le ultime tre esecuzioni erano 4.234, 4.088 e 4.928 s.
Queste misure non stabiliscono uno speedup né una garanzia di throughput portatile.
Il Jacobiano analitico rimuove la perturbazione di lunghezza fisica e le valutazioni ripetute
del contatto; le storie compatte allocano e copiano solo gli slot di molla reali. Lo spazio di
lavoro denso e i fattori LU restano limitati, in cache e di proprietà della simulazione. Lo
stepping stazionario riuscito e le letture di snapshot conservano l'asserzione di zero allocazioni.

L'asset v14 memorizza confine stantuffo/posteriore, parametri di corsa/pastiglia e geometria di
attrito referenziata. Conteggi, copertura, unità/tipi malformati, record duplicati/mancanti e
downgrade contraffatti sono rifiutati. La fixture autentica della batteria v13 conserva SHA-256
`67e7bde4dfcc2b8b41f49cadae8f89ff5fc5a1668059e084b7b5d9da02c7c44a`, i riferimenti
fisici e il replay aggiornato esatto allo stesso runtime. Le fixture più vecchie restano intatte.
L'agente 0.17.0/MCP esercita scoperta, validazione, ogni confine esportato, forza di contatto,
conflitti di revisione e fork indipendenti.

Le viste Unity di cursore/contatto e i test Edit/Play sono preparati in sorgente C# 9. L'host
dei controlli Standard è .NET 10; non esercita Unity Editor. `POWER_UNITY_EDITOR` non è impostato.
L'evidenza di Editor/Play/Mono/IL2CPP/Player e quella nuova Linux/macOS restano in sospeso.
I parametri sono input di ricerca non verificati; motore completo, trasmissione, ECU/TCU e
campioni di veicolo calibrati restano incompiuti.

## 2026-09-30: alimentazione a batteria finita e regolazione a duty cycle

Il comando seriale richiesto è passato su Windows x64, SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **196/196** controlli gestiti, **154/154** controlli degli assembly Standard ospitati su .NET 10
  e **19/19** gruppi MCP di server figlio reali.
- **16/16** Zig e **6/6** controlli ABI C#; tutti i **176** valori storici coincidono esattamente.
  L'audit dei sorgenti trova zero file C/C++/Lua. Compilazione Release: zero avvisi/errori.
- Log: `artifacts/reports/battery-final-2026-09-30.log`.
- Tutti i **16** laboratori passano JSON Schema; **12** casi di batteria/duty malformati sono
  rifiutati da jsonschema 4.25.1 in una cache isolata ignorata. Report supplementare:
  `artifacts/reports/battery-schema-audit.json`.

I nodi batteria a capacità finita aggiungono OCV/SOC affine e un ramo RC di polarizzazione.
I motori di batteria usano un trasformatore di duty mediato bidirezionale; i carichi accessori
resistivi commutati condividono la resistenza del bus. Energia chimica/RC e induttiva del motore,
calore di batteria/rame/carico e trasferimenti meccanici/idraulici condividono la contabilità di
conservazione. Il lavoro del motore di batteria è interno, non lavoro di sorgente esterna duplicato.
I controlli numerici iniziali hanno esposto l'energia induttiva mancante del motore di batteria;
la contabilità ora la include.

L'evidenza copre il decadimento analitico di polarizzazione a vuoto e la risposta RC del carico
resistivo, l'inventario di carica, l'integrazione RK4 indipendente a quattro stati di motore/batteria
e il raffinamento liscio del secondo ordine. Duty con segno, carica rigenerativa, equivalenza degli
avvolgimenti in parallelo, risposte ingranate dipendenti dal duty, accoppiamento congiunto
frizione/pompa, rollback completo dell'esaurimento tardivo, annullamento, rifiuto degli input, fork
indipendenti, replay di batch e zero allocazioni passano su entrambi gli assembly. I fattori di
runtime e le risposte meccaniche restano di proprietà della simulazione e si aggiornano per duty
e aperture di carico cambiati.

La pompa regolata a batteria usa tick fisici di 100 µs e un regolatore di duty di 5 ms.
Gli impulsi accessori causano una caduta di bus misurata in simulazione. Tutti i **761** confini
di report/portatile/MCP concordano. A 15 s lo SOC è **0.6269678451**, la carica rimanente
**31.3483922575 C**, la tensione ai terminali **12.6056975839 V**, la polarizzazione
**0.0207555849 V** e la corrente di scarica **0.2052072574 A**. La pressione termina a
**200828.2935823 Pa** per un bersaglio di 200000 Pa. L'ultimo errore campionato è **-825.4441034 Pa**;
integrale/duty tenuto sono **0.0642978516 / 0.0629221114**. Il residuo energetico finale è circa
`2.13e-7 J`. Impronta `40d4fcbab9cad8f8`, hash finale Windows/runtime `803d9adef384cd35`.
Report: `artifacts/reports/battery-regulated-pump.json`. La capacità di carica di 50 C è
esplicitamente un piccolo inventario sintetico di verifica, non una misura di batteria OEM.
Ogni confine controlla l'energia elettrica isolata e l'assenza di lavoro di sorgente duplicato.

L'asset v13 conserva i parametri OCV/resistenza/capacità/calore della batteria e periodo/guadagni/
limiti/integrale iniziale del controllo di duty. Tipi malformati rifirmati, conteggi, unità,
estensioni mancanti/duplicate e downgrade contraffatti sono rifiutati. La fixture autentica della
pompa regolata v12 conserva il suo digest, l'impronta, i riferimenti fisici e il replay aggiornato
allo stesso runtime. Le fixture precedenti e le impronte dei modelli non controllati restano invariate.
L'agente 0.16.0/MCP verifica la scoperta della batteria, il replay completo di esperimento/export,
la proprietà del duty, le scritture accessorie non valide, le revisioni e i fork indipendenti di
batteria/controllo.

Le viste Unity di batteria/elettrica/duty e i test di ciclo di vita import/Play sono preparati.
L'evidenza reale di Editor/Play/Mono/IL2CPP/Player e i nuovi controlli Linux/macOS restano in sospeso.
Parametri di batteria affini costanti, convertitore ideale e schedule prescritte di accessori/valvole
non stabiliscono BMS/chimica/invecchiamento, controllo PWM/corrente, contattori/guasti, meccanica
degli attuatori, ECU/TCU completi, DCT/AT completi, il comportamento rimanente del motore o la
calibrazione del veicolo. Tutti i parametri restano `unverified`; l'obiettivo completo di Power! resta aperto.

## 2026-09-30: checkpoint del controllo di pressione campionato

Il comando seriale richiesto è passato su Windows x64 con SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **185/185** controlli gestiti, **146/146** controlli degli assembly Standard ospitati su .NET 10
  e **18/18** gruppi di integrazione MCP di server figlio reali.
- **16/16** Zig e **6/6** controlli ABI C#; tutti i **176** valori storici coincidono esattamente.
  L'audit dei sorgenti trova zero file C/C++/Lua. Compilazione Release: zero avvisi/errori.
- Log: `artifacts/reports/pressure-control-final-2026-09-30.log`.
- Tutti i **15** laboratori passano il JSON Schema del modello; **12** casi di controllore
  malformati sono rifiutati strutturalmente. L'audit supplementare usa jsonschema 4.25.1 in
  una cache isolata ignorata. Report: `artifacts/reports/pressure-controller-schema-audit.json`.

`pressure_controller` legge un nodo idraulico e possiede un input di tensione del motore CC.
I campioni avvengono al tempo zero e a multipli interi di un periodo allineato ai tick; la tensione
è tenuta fra i campioni. Il primo campione conserva l'integrale iniziale fornito. L'integrazione
condizionale impedisce incrementi più avanti nella saturazione di tensione. Quattro stati del
controllore e l'input tenuto partecipano a rollback completo, annullamento, fork, hash e stepping
senza allocazioni. Le sovrascritture esterne di tensione sono rifiutate con errori azionabili
`controlled_input`. Vedi [il contratto](HYDRAULIC_PUMP.it.md#sampled-pressure-regulation).

L'evidenza numerica indipendente include un impianto di motore/albero/pressione PI/RK4 campionato
programmato separatamente. Dimezzare i tick fisici a un periodo di controllo fisso di 10 ms mostra
una convergenza liscia del secondo ordine verso quel riferimento campionato; non è una rivendicazione
di convergenza del secondo ordine verso un controllore a tempo continuo. Test esatti a pressione
costante verificano sample/hold, l'ordinamento degli estremi degli eventi e la fase di clock dei fork.
Saturazione e svolgimento, unità/periodi/proprietà malformati, capacità di stato, rollback di
fallimento aritmetico tardivo, annullamento, memoria indipendente e controlli di allocazione girano
su entrambi gli assembly target Core. Un bersaglio irraggiungibile esegue e fa il replay con successo
ma fallisce i KPI di inseguimento mentre il comando resta saturato e l'integrale è tenuto.

L'asset v12 porta il record di controllo completo di 80-byte con bersaglio, periodo intero, guadagni,
limiti di tensione e integrale iniziale. Copertura tipizzata, conteggi limitati, unità/record
malformati, estensioni duplicate/mancanti e downgrade contraffatti sono rifiutati. La fixture
autentica della pompa accesa v11 è stata catturata prima del cambiamento del writer; i suoi SHA-256
e impronta originali restano fissi. Il replay aggiornato allo stesso runtime e i riferimenti fisici
passano, insieme a tutte le fixture precedenti e ai modelli non controllati invariati.

L'esperimento `pressure-regulated-pump` usa tick fisici di 100 µs, campioni di controllo di 5 ms
e setpoint di 300/350/200 kPa con disturbi schedulati di riempimento/scarico della frizione.
Tutti i **757** confini di report, portatile e MCP concordano. A 15 s la pressione di linea è
**200550.7972096 Pa** per il bersaglio di 200000 Pa. L'ultima pressione campionata è
**200544.9882311 Pa**, errore **-544.9882311 Pa**, integrale **0.8124749429 V** e tensione tenuta
**0.8015751783 V**. La corrente del motore è **0.5964870315 A** e la velocità dell'albero della pompa
**2.0526093297 rad/s**. Impronta del modello `67e8edb13dc42f42`; hash di stato finale Windows/runtime
`44342c02cd41f3c6`. Report: `artifacts/reports/pressure-regulated-pump.json`.
I test isolano anche il lavoro elettrico dai confini meccanici di trascinato/carico a ogni campione
di report e controllano i residui di volume/energia.

L'agente 0.15.0 espone il contratto di controllo, i guadagni dimensionali, gli errori di input
posseduto e l'esempio. I test MCP reali esercitano il replay completo di esperimento/export, le
scritture di tensione bloccate, gli aggiornamenti di setpoint, le revisioni, i fork del controllore
e l'indipendenza del genitore. Unity ora prepara viste di regolatore/sensore/comando e test di
reset/replay import/Play. Non è stata ottenuta evidenza reale di Editor/Play/Mono/IL2CPP o Player;
anche la verifica nuova Linux/macOS resta in sospeso. Le schedule di valvole prescritte e il
sensore/sorgente di tensione ideali non implementano ECU/TCU completi, batteria/PWM, dinamica dei
sensori, meccanica degli attuatori, DCT/AT completi, il comportamento rimanente del motore o una
calibrazione misurata. Tutti i parametri di ricerca e di campione restano `unverified`. L'obiettivo
completo di Power! resta aperto.

## 2026-09-30: checkpoint ripreso delle perdite di pompa e dell'alimentazione elettrica

Il proprietario ha ripreso lo sviluppo. Le modifiche ai sorgenti sono state verificate in locale su
Windows x64 con .NET SDK 10.0.401 e runtime 10.0.12 (il roll-forward `latestPatch` configurato).
Il commit di base è `d266095`; queste modifiche non erano committate al momento della verifica.

```sh
dotnet run --file tools/Build.cs -- verify
```

- **174/174** controlli gestiti, **138/138** controlli degli assembly Standard ospitati su .NET 10
  e **17/17** gruppi MCP contro un server figlio reale.
- **16/16** controlli Zig e **6/6** controlli ABI C#; tutti i **176** valori storici coincidono
  esattamente. L'audit dei sorgenti conserva zero file C/C++/Lua. Compilazione Release: zero avvisi/errori.
- Log: `artifacts/reports/pump-assembly-final-2026-09-30.log`. La sola riparazione della baseline
  precedente è passata 165/165, 132/132 e 15/15 in `resume-baseline-2026-09-30.log`.

La [esecuzione CI 35809732259](https://github.com/Water-Run/Power/actions/runs/35809732259)
precedente è passata su Linux e ha fallito quattro controlli di asset su Windows/macOS. Ciascuno
è fallito solo su un hash di stato finale codificato in modo fisso dall'esecuzione originale della
fixture su Linux, dopo che la riproduzione originale e quella aggiornata concordavano. I digest dei
file di fixture e le impronte dei modelli restano esatti. I test ora conservano l'uguaglianza di hash
allo stesso runtime e usano riferimenti fisici dai checkpoint documentati, con tolleranze esplicite
che riflettono la precisione pubblicata. Gli hash storici restano registrati nella provenienza delle
fixture. Questa esecuzione Windows ripara il fallimento osservato in locale; non stabilisce un nuovo
risultato di CI Linux/macOS.

`HydraulicPumpAssembly` compone la pompa ideale, la perdita di pressione da uscita a ingresso e
l'attrito viscoso d'albero riferito a terra. L'evidenza indipendente copre tutti i regimi con segno
e le identità di potenza passiva, il moto analitico smorzato di albero/pressione, il raffinamento
liscio del secondo ordine, l'inventario di ingresso finito, il lavoro del serbatoio, un'integrazione
RK4 indipendente dell'ODE motore RL/albero/pressione, l'equilibrio elettrico analitico, il rollback
completo di fallimento tardivo, annullamento, fork, immutabilità e stepping senza allocazioni. Entrambi
gli assembly target eseguono gli stessi controlli. L'esperimento acceso a perdite nulle riproduce gli
osservabili fisici condivisi del laboratorio originale entro le tolleranze dichiarate.

Il laboratorio `fired-pump-losses` ha **89** confini coincidenti di report/portatile/MCP.
Il suo modello ha 60 stati contati. A 0.8 s il lavoro della pompa è **52.6573534421 J**, il calore
di perdita **8.6081420133 J**, il calore combinato di perdita della pompa **41.0517855370 J**, e il
nodo termico della pompa raggiunge **300.4105178554 K**. La velocità finale dell'albero è
**68.6812975063 rad/s** e la pressione di linea **1.0581382360 MPa**. Il residuo energetico è circa
`-6.13e-10 J`. Impronta `524661ea3d721bbc`. Report: `artifacts/reports/fired-pump-losses.json`.

Il laboratorio `electric-pump` ha **106** confini coincidenti su 2 s. Il suo motore RL, l'albero
separato della pompa, perdita, trascinamento, scarico e linea cedevole comandano il riempimento/
scarico/ricattura schedulati della frizione di pressione. Il lavoro idraulico esterno è **zero**.
La velocità finale dell'albero della pompa è **67.0570291504 rad/s**, la corrente **5.3148298325 A**,
la pressione di linea **0.5606191525 MPa** e lo slittamento della frizione sotto `1e-8 rad/s`. Il
lavoro della pompa è **3.6117807126 J**; il calore di perdita è **0.3597803209 J**. Il residuo
energetico è circa `5.85e-10 J`. Impronta `d8f8fedfdce59003`.
Report: `artifacts/reports/electric-pump.json`. I controlli isolano il lavoro elettrico dai confini
separati degli alberi trascinato/carico e verificano che le valvole sigillate impediscano l'azionamento
di pressione anche mentre la pompa elettrica opera.

L'agente 0.14.0 annuncia la composizione, le unità, la semantica di potenza ed entrambi gli esempi.
Lo schema JSON invariato e l'asset v11 portano componenti ordinari; non è stato introdotto un nuovo
formato né un tipo di componente. Quattordici laboratori esportano ed eseguono nello strumento di
build seriale, compreso il report CLI della pompa accesa che prima era solo di export.

I test Unity di replay import/Play, reset e pulizia sono preparati per entrambi i nuovi asset.
`POWER_UNITY_EDITOR` non è impostato e l'Editor fissato non è stato trovato nella directory di
installazione standard. Non è stata ottenuta evidenza reale di Editor/Play/Mono/IL2CPP o di Player
desktop. Anche la verifica nuova Linux/macOS resta in sospeso. I valori di perdita costanti, i
comandi prescritti e tutti i parametri di campione restano `unverified`; mappe misurate, dinamica
di batteria/controllo/regolatore/stantuffo, DCT/AT e ECU/TCU completi, il comportamento rimanente
del motore, campioni di veicolo calibrati e l'accettazione di rilascio restano incompiuti.

## 2026-09-22 checkpoint di chiusura: pompa comandata dall'albero e scarico di pressione

Aggiunte pompe volumetriche ideali reversibili, ingressi espliciti finiti/di serbatoio e scarico
di pressione unidirezionale a conduttanza finita. Le velocità della pompa e le pressioni idrauliche
entrano nella soluzione di Newton di cilindro/convertitore; le capacità delle frizioni di pressione
si aggiornano dentro l'iterazione dei vincoli. Trasferimento accettato albero/fluido, lavoro del
serbatoio, volume di riferimento e perdite termiche condividono lo stato transazionale completo.
JSON/schema, asset v11, agente 0.13.0, scoperta MCP e il laboratorio della pompa accesa usano le
stesse definizioni.

La verifica seriale richiesta è stata completata con successo:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **165/165** controlli gestiti, **132/132** controlli degli assembly Standard ospitati su .NET 10
  e **15/15** gruppi MCP contro un processo server figlio reale.
- **16/16** test Zig e **6/6** test ABI Python; tutti i **176** valori numerici storici conservati
  coincidono esattamente. L'audit dei sorgenti conserva zero file C/C++/Lua.
- Compilazione Release: zero avvisi ed errori. Log:
  `artifacts/reports/pump-integration-verify.log`.

L'evidenza indipendente include identità di potenza ideali in entrambe le direzioni, oscillazione
analitica albero/cedevolezza, inventario di ingresso chiuso, motoring inverso, reazioni della pompa
ingranata, decadimento esatto dello scarico al punto medio, un equilibrio regolato a carico costante
e una soluzione analitica di retroazione di pressione della frizione strisciante. I raffinamenti
dell'oscillatore liscio e della retroazione della frizione si avvicinano al secondo ordine. Cattura,
indipendenza dei rami, annullamento, fallimento numerico tardivo, retry, rifiuto della pressione
negativa e stepping senza allocazioni passano anch'essi. Il test iniziale di allocazione ha esposto
allocazione nella formattazione del proprio stato; la formattazione ora è limitata ai fallimenti, e
il ciclo caldo misurato alloca zero byte.

I test degli asset conservano pressione/topologia di ingresso e taratura dello scarico, rifiutano
estensioni malformate e duplicate, dimensioni/conteggi non validi e downgrade contraffatti. La fixture
autentica idraulica accesa v10 conserva l'impronta `01b69cb3abe52211` e l'hash finale
`46a01d103e6159d3` dopo l'aggiornamento. Anche i digest e le traiettorie delle fixture precedenti passano.

Il laboratorio della pompa accesa ha **89** confini coincidenti di report, portatile e MCP su
0.8 s a tick di 50,000-ns. I suoi 56 stati contati includono una linea di alimentazione di 4e-12 m³/Pa,
una pompa ideale comandata dall'albero di 1e-6 m³/rad e una taratura di scarico di 1e6-Pa con conduttanza
1e-9 m³/(s·Pa). L'energia idraulica iniziale è esplicitamente 3 J. Il lavoro della pompa è
**53.9425016232 J**, il lavoro idraulico esterno **0 J** e il calore di scarico **45.0264051429 J**.
La pressione finale di linea è **1.0697262404 MPa**, la velocità albero/turbina **69.7555356890 rad/s**,
la velocità di carico **6.6433843513 rad/s** e la temperatura del nodo termico della trasmissione
**301.5306383749 K**. Il residuo di energia totale è `1.0671e-9 J`; il residuo del volume di riferimento
è `3.0493e-20 m³`. Impronta `d0bd8f29a706fd89`, hash finale `572150ab5d66a2f6`.
Report: `artifacts/reports/fired-pump.json`.

Tutti i dodici documenti di laboratorio passano JSON Schema; dieci casi malformati di pompa/scarico
sono rifiutati. Audit: `artifacts/reports/pump-schema-audit.json`. Le viste Studio delle porte della
pompa e i test di ciclo di vita import/Play sono preparati. `POWER_UNITY_EDITOR` non è impostato;
Editor, Play Mode e IL2CPP restano non verificati. Questi controlli gestiti non sono evidenza Unity.

Lo sviluppo è in pausa qui su richiesta del proprietario. Perdite/controllo della pompa, dinamica del
cursore regolatore e dello stantuffo attuatore, DCT/AT completi, ECU/TCU, comportamento del motore più
ricco, campioni di veicolo calibrati e accettazione desktop restano incompiuti. Vedi
[il contratto della pompa](HYDRAULIC_PUMP.it.md) e lo [stato di sviluppo](DEVELOPMENT_STATUS.it.md).

## 2026-09-22: rete idraulica e trasmissione azionata a pressione

Aggiunti nodi idraulici cedevoli, restrizioni lineari e turbolente regolarizzate, pressioni esplicite
di serbatoio e frizioni azionate a pressione. Pressione idraulica e storie di volume/lavoro/calore
partecipano alle prove interne di cattura e alle transazioni dell'intero batch. JSON/schema, asset v10,
capacità dell'agente 0.12.0 e il laboratorio idraulico acceso condividono queste definizioni. Vedi
[le equazioni e i limiti](HYDRAULIC_NETWORK.it.md).

Verificato in locale su Linux con:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **154/154** controlli gestiti e **123/123** controlli degli assembly Standard Core/Assets su .NET 10.
- **14/14** gruppi MCP contro un processo server figlio reale.
- **16/16** Zig e **6/6** controlli ABI Python; tutti i **176** valori storici coincidono esattamente.
- La compilazione Release ha zero avvisi/errori; l'audit dei sorgenti passa.
- Log: `artifacts/reports/hydraulic-integration-verify.log`.

La nuova evidenza fisica include controlli di flusso/passività/intervallo con segno, carica RC
analitica, equalizzazione chiusa, identità esatte di lavoro del serbatoio e termiche, integrazione
RK4 indipendente del flusso non lineare, raffinamento del secondo ordine della pressione e dell'impulso
della frizione comandata da pressione, precarico e cattura/rilascio. Il fallimento dopo una storia
idraulica/frizione accettata, l'annullamento, l'input non valido, i fork e il batching conservano la
transazione completa. Cattura e letture di snapshot allocano zero byte dopo il riscaldamento. Un passo
di scarico sovradimensionato rifiuta la pressione relativa negativa senza cambiare stato.

Il replay portatile ha esposto un campo di pressione del serbatoio omesso durante l'implementazione.
Il record di restrizione v10 ora lo porta esplicitamente, e i test di round trip confrontano i
descrittori fisici completi e ogni confine di replay. Record malformati rifirmati, estensioni
duplicate/mancanti, dimensioni errate, porte di attuatore non valide e downgrade di nodi solo
idraulici sono rifiutati. La fixture autentica v9 di SHA-256
`96a78326ae2f2e8dd4a434fb81fbe88f4a19156cbf13cf069a7bd4e798e93c9f` conserva l'impronta
`839d03901973668d` e lo stato finale `834a679376b7a6fd` quando viene aggiornata. Le fixture più vecchie restano.

Il laboratorio idraulico acceso ha **89** confini coincidenti di report/portatile/MCP su
0.8 s a tick di 50,000-ns. Impronta `01b69cb3abe52211`, hash finale `46a01d103e6159d3`.
La velocità finale albero/turbina è 70.94321138 rad/s; la velocità di carico è 6.75649632 rad/s. I serbatoi
forniscono 8 J, le restrizioni dissipano 7 J e l'energia idraulica accumulata aumenta di 1 J.
Il calore del convertitore è 48.80297187 J; il calore di blocco 32.89304173 J; il calore di frizione/freno
di cambio 119.31915873 J e 60.16098886 J. Il nodo di calore condiviso raggiunge 301.34088081 K.
Il residuo di energia totale è `1.0896e-9 J`; il residuo del volume di riferimento è `-1.0804e-18 m³`.
Il lavoro netto di sorgente esterna è -65.07102675 J, compresi alimentazione idraulica, carico e lavoro
di contropressione del cilindro. Non è una misura diretta del solo lavoro di carico.

Tutti gli undici documenti di laboratorio sono validi rispetto allo schema; dieci contratti idraulici
malformati sono rifiutati strutturalmente. Il compilatore aggiunge controlli di dimensione/topologia/intervallo.
Audit: `artifacts/reports/hydraulic-schema-audit.json`. Le viste idrauliche Studio e i test
import/Play sono preparati, ma `POWER_UNITY_EDITOR` non è impostato; Editor reale, rendering,
Play Mode e Player/IL2CPP non sono verificati. Pompe/regolatori, dinamica di stantuffo e
accumulatore, DCT/AT completi, motore/controlli e campioni calibrati restano aperti.

Le descrizioni precedenti del lavoro di sorgente identificano ora esplicitamente il lavoro esterno netto:
quella contabilità include la contropressione del cilindro, quindi la sua grandezza non va etichettata
come lavoro di uscita del solo carico. È una correzione della descrizione dell'evidenza, non un cambiamento di fisica.

## 2026-09-22: convertitore di coppia accoppiato e laboratorio di blocco/cambio acceso

Aggiunte quattro mappe esplicite del convertitore con segno, validazione dell'interpolazione passiva,
reazioni dello statore stazionario, calore del fluido e una soluzione congiunta convertitore/cilindro
integrata con i vincoli degli ingranaggi e gli eventi di frizione. JSON/schema, v9 portatile, capacità
dell'agente 0.11.0 e l'esempio `fired-converter` condividono questo contratto. Le viste Studio e i test
Editor/Play sono preparati. Vedi [CONVERTER_NETWORK.it.md](CONVERTER_NETWORK.it.md).

Il comando seriale completo:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

Evidenza Linux locale:

- **143/143** controlli gestiti; **114/114** controlli degli assembly Standard Core/Assets su .NET 10.
- **13/13** gruppi MCP contro un server figlio reale.
- **16/16** Zig e **6/6** test ABI Python; tutti i **176** numeri storici coincidono esattamente.
- Compilazione Release: zero avvisi/errori; l'audit dei sorgenti passa.
- Log: `artifacts/reports/converter-integration-verify.log`.

I nuovi controlli coprono mappe con segno e continuità del membro di riferimento, bilancio
statore/energia, violazioni di passività interne, unità strette, proprietà immutabile dei punti,
fallimento per overflow, accoppiamento fluido analitico e stallo, raffinamento del secondo ordine,
reverse/coast/controrotazione, porte condivise, instradamento delle perdite termiche/esterne,
riflessione degli ingranaggi e blocco in parallelo. I controlli di fallimento, annullamento e ramo
conservano lo stato completo; la cattura interna alloca zero byte dopo il riscaldamento. I controlli
portatili rifiutano conteggi errati rifirmati, indici/unità sbagliati, mappe duplicate e downgrade.
La fixture autentica v8 conserva SHA-256
`6872f857bc521ed114d6eea5837cbb380a01f374ddfe7e829b30afa2c44fe0aa`, impronta
`6703f00c995e6b62` e stato finale `b328de221532fbae` in lettura o dopo l'aggiornamento. Le fixture
più vecchie restano invariate.

L'esperimento fired-converter di 0.8-s usa tick di 50,000-ns e fa il replay esatto di tutti gli **87**
confini attraverso il report, l'asset portatile e MCP. Impronta `839d03901973668d`,
hash finale `834a679376b7a6fd`; velocità pompa/turbina 73.37747546 rad/s e velocità di carico
6.98833100 rad/s. Il calore del fluido è 24.27663069 J, il calore di blocco 22.84709072 J, il calore
della frizione di cambio 157.18199410 J e il calore del freno 83.42288714 J. Il nodo termico 5 termina
a 301.43864301 K, con residuo di energia totale `3.2969e-11 J`. Il lavoro netto di sorgente esterna è
-63.19344680 J, compresi carico e lavoro di contropressione del cilindro, mentre il carburante tracciato
rilascia 2049.02269691 J. Sono uscite numeriche sintetiche.

Uno studio a cinque passi a 50,000/25,000/12,500/6,250/3,125 ns verifica la distanza normalizzata
combinata che si riduce in velocità finale dell'albero, calore del fluido e calore di blocco rispetto
all'esecuzione più fine, più differenze assolute sotto 0.0002 rad/s o J rispettivamente. Le differenze
individuali di calore non sono monotone vicino agli eventi di frizione; non si rivendica un ordine di
convergenza accoppiata uniforme. L'esecuzione più fine dà 73.37753852 rad/s, 24.27656820 J e 22.84711838 J.
Test analitici lisci separati conservano un fattore di raffinamento maggiore di 3.9.

Unity Editor reale, Play Mode, rendering e IL2CPP restano non verificati:
`POWER_UNITY_EDITOR` non è impostato. Comportamento completo del motore, topologia DCT/AT, idraulica,
controlli e campioni di veicolo calibrati restano aperti. Le mappe quasi stazionarie non stabiliscono
la dinamica del fluido né le prestazioni misurate del convertitore.

## 2026-09-22: ingranaggi ideali accoppiati e trasmissione planetaria accesa

Ingranaggi ideali e vincoli planetari a tre porte condividono ora la soluzione elettromeccanica,
del cilindro e della frizione. La proiezione diretta dei vincoli conserva il moto compatibile e la
fase relativa iniziale; le reazioni medie per porta sono osservabili e transazionali.
JSON/schema, asset v8, CLI/MCP e Studio usano la stessa topologia. Vedi
[il contratto degli ingranaggi e i limiti numerici](GEAR_NETWORK.it.md).

Il comando seriale completo è passato su Linux x64 con .NET SDK 10.0.400/runtime 10.0.11 in cache
e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **131/131** controlli gestiti Core/applicazione.
- **105/105** controlli degli assembly Standard Core/Assets ospitati su .NET 10.
- **12/12** gruppi MCP di server figlio reali.
- **16/16** gruppi Zig, **6/6** test ABI Python e **176** valori di baseline storici coincidenti
  esattamente. L'audit dei sorgenti non trova file di implementazione C/C++ o Lua.
- La compilazione Release segnala zero avvisi/errori. Log: `artifacts/reports/gear-integration-verify.log`.

Nove gruppi di grafo confrontano rapporti positivi/negativi e moto/reazioni planetarie libere con
riferimenti esatti indipendenti, inerzia riflessa multistadio e ordinamento per ID stabile,
equivalenza di motore RL/termica e di cilindro reagente, e riduzione analitica/cattura diretta del
cambio e calore. Un oscillatore vincolato dimostra convergenza del secondo ordine e conservazione
dell'energia. Si controllano rollback completo dopo un prefisso di cambio accettato, annullamento,
indipendenza dei rami, replay batch esatto e zero allocazioni; la copertura delle allocazioni include
la cattura interna con fattori variabili. Le diagnostiche di rango, velocità iniziale, porta, rapporto
e parametro non supportato sono esplicite.

Due gruppi di asset coprono la topologia a tre porte, ogni confine di riproduzione, conteggi malformati,
record mancanti/duplicati/di tipo errato, portasatelliti non validi e tentativi di ingranaggio in
downgrade. Una fixture autentica di frizione accesa v7 conserva l'impronta `197be44884deee90` e l'hash
finale `28bf5335d8e35cde` dopo l'aggiornamento. Le fixture più vecchie e i modelli privi di ingranaggi
restano invariati. Due gruppi di integrazione gestiti aggiungono contratti JSON/agente stretti,
comportamento di revisione/annullamento/ramo e la distinzione fra esecuzione riuscita e KPI superati.

Il nuovo laboratorio planetario acceso ha **84** confini coincidenti fra batch alternati, riproduzione
portatile e MCP. Il suo esperimento di salita/discesa di 0.8-second registra **-56.83157714 J** di
lavoro netto di sorgente esterna, generando **254.52399968 J** nella frizione solare/corona e
**156.31560557 J** nel freno della corona. Il nodo termico termina a **302.05419803 K**;
le velocità albero/carico sono **76.81548837 / 7.31576080 rad/s**, con la corona tenuta. Il residuo
energetico finale è **2.51020538e-10 J**. L'impronta è `6703f00c995e6b62`; l'hash finale
è `b328de221532fbae`. Sorgente/report sono `assets/labs/fired-planetary.power.json` e
`artifacts/reports/fired-planetary.json`. I parametri restano sintetici e `unverified`.
Disabilitare la schedule di cambio rimuove il calore della frizione solare/corona e cambia il moto del carico.

Studio ha viste schematiche del planetario a tre porte e della riduzione finale, con test preparati di
import, replay del cambio, reset e pulizia. `POWER_UNITY_EDITOR` non è impostato: non si rivendica
evidenza di Editor/Play/IL2CPP. Topologia DCT/AT completa, convertitore, idraulica, ECU/TCU,
comportamento rimanente del motore, calibrazione misurata del veicolo e accettazione di rilascio restano aperti.

## 2026-09-22: riferimenti indipendenti di ingranaggio ideale e planetario

Aggiunti i primitivi immutabili `IdealGearPair` e `SimplePlanetaryGear` per coppie esterne costanti.
I risultati espongono velocità dei membri, spostamenti, coppie di reazione, lavoro, variazione di
energia cinetica e residuo. Le velocità iniziali devono soddisfare il vincolo; non si inferisce una
sincronizzazione a slittamento finito. Vedi [equazioni, segni e limiti](IDEAL_GEARS.it.md).

Il comando seriale completo è passato su Linux x64 con SDK 10.0.400/runtime 10.0.11 in cache:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **118/118** controlli gestiti Core/applicazione e **94/94** controlli degli assembly Standard
  Core/Assets ospitati su .NET 10; otto nuovi gruppi girano contro ciascun target Core.
- **11/11** gruppi MCP di server figlio reali, **16/16** gruppi Zig e **6/6** test ABI Python.
  Tutti i **176** valori di baseline storici coincidono esattamente; l'audit dei sorgenti non trova
  file di implementazione C/C++ o Lua.
- La compilazione Release ha zero avvisi/errori. Log:
  `artifacts/reports/ideal-gear-reference-verify.log`.

I nuovi controlli coprono rapporti di ingranaggio positivi/negativi, inerzia riflessa, bilancio
dell'impulso per membro, potenza di reazione nulla, dinamica indipendente delle forze di vincolo
planetarie, tre condizioni di membro tenuto e presa diretta solare/corona. I carichi di tenuta e di
blocco sono espliciti. I risultati a carico costante coincidono sugli intervalli partizionati, inclusa
l'inversione di velocità. Il campionamento al punto medio di carichi sinusoidali converge contro
integrali indipendenti a circa quattro volte di riduzione dell'errore per dimezzamento dell'intervallo,
per entrambi i primitivi.

La spazzata deterministica include **2,500** casi per riferimento. **10,000** valutazioni di
ciascun primitivo non allocano memoria gestita; chiamanti concorrenti indipendenti condividono solo
parametri immutabili. Valori non validi, velocità iniziali incompatibili, condizionamento del
costruttore, overflow aritmetico ed errori finiti di cancellazione della forza rifiutano senza un
risultato parziale. Una regressione ad alto rapporto conserva una reazione piccola, fisicamente
richiesta, invece di perderla per sottrazione di coppie quasi uguali.

La semantica del solver di grafo e dell'asset v7 è invariata; i controlli esistenti di laboratorio,
portatile e replay MCP restano superati. Questi primitivi non sono ancora componenti di trasmissione
accoppiati, strumenti agente, simulazioni di cambio o modelli calibrati. La verifica reale di Unity
Editor/Play/IL2CPP resta in sospeso, come il resto del motore, DCT/AT, idraulica, controlli e gli
obiettivi completi di veicolo calibrato.

## 2026-09-22: frizioni accoppiate e integrazione del carico del motore acceso

Le reazioni statiche/cinetiche della frizione condividono ora la soluzione elettromeccanica/del cilindro,
con eventi interni limitati di cattura/inversione e instradamento del calore di attrito. Fase, uscite
medie e calore compensato sono transazionali e hashed. JSON/schema, CLI/MCP, asset v7 e Studio
consumano lo stesso componente; i lettori v1–v6 restano supportati. Vedi
[le equazioni e i limiti numerici espliciti](CLUTCH_NETWORK.it.md).

Il comando seriale completo è passato su Linux x64 con .NET SDK 10.0.400/runtime 10.0.11 in cache
e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **110/110** controlli gestiti Core/applicazione.
- **86/86** controlli degli assembly Standard Core/Assets ospitati su .NET 10.
- **11/11** gruppi di integrazione MCP di processo figlio reali.
- **16/16** gruppi Zig e **6/6** test ABI Python; tutti i **176** valori storici
  coincidono esattamente. L'audit dei sorgenti non trova file di implementazione C/C++ o Lua.
- La compilazione Release segnala zero avvisi ed errori. Il log locale è
  `artifacts/reports/clutch-integration-verify.log`.

La nuova evidenza fisica confronta il grafo con il `ClutchPair` esatto a carico costante attraverso
innesto interno e inversione, rapporti positivi/negativi ed entrambe le destinazioni termiche. Le
traiettorie di motore/corrente bloccati e di pressione/carburante del cilindro reagente coincidono
con modelli separati a inerzia combinata analiticamente. Un oscillatore molla/freno coincide con il
moto sinusoidale a tratti attraverso tre inversioni e la cattura finale al quarto punto di inversione;
dimezzare il tick riduce l'errore di più di 3.7x. Anelli a tre frizioni esercitano vincoli ridondanti
e innesto simultaneo con moto ed energia conservati. Il rilascio statico richiede saturazione; il
residuo della risoluzione della radice non può creare una seconda inversione spuria.

Il rollback completo su più tick è controllato dopo un prefisso accettato di riscaldamento/cattura e
un sovraccarico numerico successivo. Replay schedulato, annullamento, indipendenza dei fork, proprietà
immutabile e zero allocazioni sono conservati. I controlli di allocazione includono eventi interni
ripetuti di inversione, esercitando copie candidate e fattori variabili. Questi test sostengono
l'ambito documentato del solver, non un'accuratezza ibrida arbitraria a tick grandi.

Il nuovo laboratorio `fired-clutch` ha 67 confini di report coincidenti esattamente fra batch
alternati, riproduzione portatile e MCP. Il suo report di 0.6-second registra 96.74607609 J di
lavoro esterno netto esportato, compresi carico e contropressione del cilindro, 191.55570747 J di
calore della frizione, velocità finale motore/carico 68.58488546 rad/s e un residuo energetico finale
di 1.79e-10 J. La sua impronta è `197be44884deee90` e l'hash di stato finale `28bf5335d8e35cde`.
Il nodo termico della frizione raggiunge 300.95777854 K. Sorgente e risultato sono
`assets/labs/fired-clutch.power.json` e `artifacts/reports/fired-clutch.json`; i parametri restano
sintetici e `unverified`.

Il round trip dell'asset v7 conserva capacità e canali, rifiuta estensioni malformate/mancanti/duplicate
e downgrade non validi, e preserva digest, impronta e replay aggiornato di una fixture autentica di
cilindro acceso v6. Gli hash dei modelli precedenti restano invariati. I controlli JSON/agente stretti
conservano errori azionabili, atomicità di input/revisione e la distinzione fra esecuzione riuscita,
KPI superati e calibrazione misurata.

Piastre della frizione, uscite di fase nominate e test di import/ciclo di vita di Studio sono preparati.
`POWER_UNITY_EDITOR` è ancora non impostato: Editor, rendering, Play Mode e IL2CPP restano non
verificati. Topologia DCT/AT, gruppi planetari, convertitore di coppia, idraulica, controlli,
comportamento completo del motore e campioni di veicolo calibrati restano incompiuti.

## 2026-09-22: legge della frizione a secco gestita e riferimento a carico costante

Aggiunti `DryClutch`, una legge immutabile di capacità di coppia statica/cinetica, e `ClutchPair`,
un riferimento esatto a carico costante di due inerzie o freno a terra. Un evento di slittamento nullo
è risolto dentro l'intervallo, seguito da moto vincolato o inversione. I risultati espongono moto,
avanzamenti angolari, modo di reazione, impulso, calore, lavoro esterno e variazione di energia.
Vedi [le equazioni, l'API e il confine di implementazione](CLUTCH_PHYSICS.it.md).

Il comando seriale è passato su Linux x64 con .NET SDK 10.0.400/runtime 10.0.11 in cache
e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **99/99** controlli gestiti Core/applicazione e **77/77** controlli degli assembly Standard
  ospitati su .NET 10, compresi gli stessi dieci nuovi gruppi di frizione in entrambi i target.
- **10/10** gruppi di integrazione MCP di processo figlio reali.
- **16/16** gruppi Zig e **6/6** test ABI Python. Tutti i **176** valori numerici storici
  coincidono esattamente. L'audit dei sorgenti trova zero file di implementazione C/C++ o Lua.
- La compilazione Release segnala zero avvisi ed errori. L'output completo è conservato in locale in
  `artifacts/reports/clutch-kernel-verify.log`.

L'evidenza fisica copre innesto analitico, ripartizione esatta del carico statico, distacco,
inversione, eventi di estremo, innesto parziale, un freno a terra e rapporti di ingranaggio con segno.
I test controllano in modo indipendente quantità di moto, lavoro esterno integrato ed energie cinetiche
assolute, invece di confrontare solo i contatori energetici dell'implementazione. Una coppia con
inerzie 0.2 e 0.8 kg m2, velocità iniziali 100 e 0 rad/s e capacità strisciante 10 Nm si sincronizza
a 20 rad/s dopo 1.6 s, generando 800 J di calore.

Le soluzioni a carico costante concordano attraverso partizioni di intervallo che tagliano gli eventi
ibridi. I carichi sinusoidali congelati al punto medio convergono contro integrali indipendenti di
velocità, angolo e calore di più di 3.8x per dimezzamento, con l'errore massimo più fine sotto 2e-5
nelle uscite SI provate. Una spazzata deterministica di intervallo di 2,000 casi controlla conservazione
e valutazione ripetuta. Ha trovato e corretto un conteggio in eccesso di un ulp della durata di
strisciamento durante un'inversione. Dati non validi e fallimenti di risoluzione aritmetica/degli eventi
non pubblicano un risultato parziale. Il percorso di misura isolato e riscaldato registra zero
allocazioni per 10,000 intervalli.

È un primitivo fisico Core autonomo, **non ancora un componente di grafo compilato**.
Accoppiamento albero/motore/cilindro, vincoli a frizioni multiple, instradamento termico, stato ibrido
transazionale, rappresentazione JSON/asset/MCP e integrazione Studio restano in sospeso.
Le impronte di grafo esistenti, sette esperimenti di laboratorio e la semantica dell'asset v6 restano
invariati. La loro evidenza di combustione precedente è conservata sotto.

`POWER_UNITY_EDITOR` resta non impostato. Questi test degli assembly Standard non stabiliscono il
comportamento reale di import Unity, Play Mode o IL2CPP. Tutti i parametri del veicolo restano `unverified`;
il nuovo primitivo non completa una trasmissione, i controlli o un gruppo motopropulsore calibrato.

## 2026-09-22: combustione premiscelata, reagenti trasportati e lavoro del carico acceso

Aggiunto il tracciamento opzionale di carburante/aria fresca/prodotti ai nodi gas e frazioni esplicite
di serbatoio, più un componente `premixed_combustion` riferito all'albero. Un hazard di Wiebe prescritto
consuma i reagenti limitanti, memorizza la storia irreversibile dell'albero e converte l'energia chimica
in energia termica. L'anteprima del calore partecipa al lavoro conservativo dell'albero. Vedi
[il modello, le equazioni e i limiti](PREMIXED_COMBUSTION.it.md).

La verifica seriale completa è passata su Linux x64 con .NET SDK 10.0.400/runtime 10.0.11 in cache
e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **89/89** controlli gestiti Core/applicazione.
- **67/67** controlli degli assembly Standard Core/Assets ospitati su .NET 10.
- **10/10** gruppi di integrazione MCP di processo figlio reali.
- **16/16** gruppi Zig e **6/6** test ABI Python, compresi entrambi gli host nativi.
- Tutti i **176** valori nativi storici coincidono esattamente; trovati zero sorgenti C/C++ o Lua.
  La compilazione Release segnala zero avvisi ed errori.

La nuova evidenza fisica include:

- Esposizione analitica di Wiebe attraverso cicli espliciti, fasi negative e wrap del ciclo.
  Carburante in recipiente chiuso, consumo di aria fresca, calore e temperatura coincidono con la
  soluzione in forma chiusa del reagente limitante per cariche magre, ricche, prive di carburante e
  prive di aria. Massa ed energia totale termica più chimica sono controllate indipendentemente dal
  contatore di calore.
- La conservazione dei costituenti in rete chiusa e il riempimento/scarico del serbatoio trasportano
  la composizione a monte e l'entalpia chimica in entrambe le direzioni di flusso. Un recipiente a
  massa costante e temperatura costante con afflusso/deflusso bloccati bilanciati coincide con la
  sostituzione esponenziale della miscela entro 2e-5 in frazione di massa, anche con più di un ricambio
  del recipiente per tick esterno. Questo esercita il limite del flusso uscente quando i soli tassi di
  massa netta e di energia termica non forniscono un passo temporale utile del tracciante.
- Un riferimento RK4 di cilindro reagente scritto in modo indipendente integra moto dell'albero, massa,
  energia termica, carburante e aria fresca con scarico bloccato. Le sue soluzioni a 1 e 0.5 microsecond
  differiscono di meno di 1e-9 normalizzato. I tick Core di 100, 50 e 12.5 microseconds riducono
  l'errore normalizzato massimo di più di 2.8x e poi di 8x, con il più fine sotto 1e-4. È evidenza del
  secondo ordine senza parete; l'accoppiamento di parete resta del primo ordine.
- Più cilindri reagenti su alberi condivisi o accoppiati per albero conservano energia totale e
  costituenti, comprese miscele isolate con poteri calorifici e rapporti stechiometrici diversi.
  Fermarsi, invertire e ripercorrere non può ripetere il rilascio di calore; disabilitare una combustione
  salta l'esposizione in avanti senza recupero successivo. L'angolo di manovella visitato più grande è
  osservabile come `burn_frontier_angle`.
- Cambi di coppia schedulati falliti dopo una reazione parziale fanno rollback dello stato dei
  costituenti, della storia angolare e delle contabilità compensate. Annullamento, indipendenza dei
  batch del chiamante, isolamento dei fork, rifiuto/recupero di combustione sotto-risolta e zero
  allocazioni di stepping/snapshot passano su entrambi gli assembly. Si controllano composizione stretta,
  proprietà, unità e il budget esteso di 64 stati.

Il [laboratorio del cilindro acceso](../assets/labs/fired-cylinder.power.json) supera i suoi KPI
con impronta `a10f880d74494677` e **63** confini di report coincidenti. JSON/CLI, MCP e l'asset v6
decodificato concordano su ogni canale a ogni confine, compresi due eventi di carico fra i tempi di
report. L'esecuzione di 0.6-second registra **-369.98 J** di lavoro netto di sorgente esterna, consuma
**3.265e-5 kg** di carburante in reazione e rilascia **1436.67 J**. L'energia netta di confine del
carburante finale è **1785.07 J**, con carburante che resta anche in camera; questi numeri transitori
non sono una rivendicazione di rendimento stazionario o di consumo. Disabilitare la combustione rimuove
il rilascio di calore e produce una velocità dell'albero sostanzialmente più bassa sotto lo stesso carico.

Il residuo energetico finale è circa **-2.11e-9 J**, il residuo di massa totale **-1.25e-18 kg**,
il residuo di carburante **2.03e-20 kg** e il residuo di aria fresca **1.41e-18 kg**. La pressione
campionata ha un picco di circa **2.08 MPa** e la temperatura di **1761 K**. Sono uscite sintetiche
del modello; il campionamento di report a 10 ms non stabilisce il picco continuo di pressione/temperatura.

L'asset v6 conserva i lettori v1–v5. I nuovi test preservano definizioni di modello/miscela/combustione
e il replay, rifiutano semantiche di estensione errate/mancanti/duplicate e conteggi malformati, e
controllano digest, impronta e replay aggiornato di una fixture autentica v5 precedente al cambiamento.
La [provenienza delle fixture](../tests/Power.Tests/Fixtures/README.md) registra il suo checkpoint di
sorgente non committato senza rivendicare un commit pubblicato. Le impronte non reagenti esistenti
restano invariate. I test dell'agente coprono validazione strutturata, input non valido e annullamento
senza cambi di revisione, scritture stantie, indipendenza dei rami, uscite filtrate di carburante/calore,
recupero a tick più piccolo e la distinzione fra esecuzione riuscita e KPI falliti.

Lo schema Draft 2020-12 e tutti i **sette** laboratori passano `jsonschema` di Python. Otto forme
malformate di composizione/combustione sono rifiutate, comprese frazioni mancanti, costituenti sconosciuti,
unità errate, frazioni non valide, parametri di combustione mancanti e frazioni di serbatoio fuori posto/nulle.
I controlli del compilatore impongono separatamente le somme delle frazioni e la compatibilità della
miscela connessa. L'evidenza locale è in `artifacts/reports/combustion-verify.log` e
`artifacts/reports/fired-cylinder.json`.

I test Unity di import e Play Mode includono ora un marcatore di rilascio di calore, reset e replay
completo dell'esempio acceso. **Non sono stati eseguiti nell'Editor**: `POWER_UNITY_EDITOR` non è impostato.
Dai test degli assembly Standard non si inferisce alcuna rivendicazione di rendering, Mono/IL2CPP o Player.
R/gamma costanti, combustione prescritta e la politica di frontiera in avanti sono limiti espliciti;
dosatura del carburante, controllo dell'accensione, chimica predittiva, aspirazione/scarico dettagliati,
perdite meccaniche, trasmissioni, controlli e campioni di veicolo calibrati restano aperti.

## 2026-09-22: fasatura valvole in angolo di manovella e motoring a velocità variabile

Aggiunta `valve_timing` opzionale sulle restrizioni gas, con cicli espliciti a 360/720-degree,
angoli di apertura/durata, ingresso di picco e uscita di apertura efficace. I profili seguono l'angolo
reale di manovella attraverso accelerazione, arresto, inversione e wrap di fase. I lobi sotto-risolti
rifiutano il batch completo. Vedi [le equazioni, i limiti e l'ambito](VALVE_TIMING.it.md).

Il comando seriale completo è passato su Linux x64 con .NET SDK 10.0.400/runtime 10.0.11 in cache
e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **76/76** controlli gestiti Core/applicazione.
- **57/57** controlli contro gli assembly Core/Assets .NET Standard 2.1 ospitati su .NET 10.
- **9/9** gruppi di integrazione MCP di processo figlio reali.
- **16/16** gruppi Zig e **6/6** test ABI Python, con entrambi gli host nativi eseguiti.
- Tutti i **176** valori nativi storici coincidono esattamente; l'audit dei sorgenti trova zero file
  C/C++ o Lua. La compilazione Release segnala zero avvisi ed errori.

Evidenza fisica aggiuntiva:

- Svuotamento adiabatico bloccato in forma chiusa con esposizione della valvola a seno quadrato
  integrata in modo indipendente, a +40 e -40 rad/s, prova l'accoppiamento fasatura-flusso attraverso
  il wrap del ciclo. Raffinare i tick da 1 ms a 0.5 ms riduce l'errore relativo di massa di più di
  2.8x; 0.125 ms lo riduce di più di un ulteriore 8x, sotto 1e-7. Le contabilità di energia/massa
  sono controllate separatamente.
- Un riferimento RK4 indipendente del cilindro mobile include il lavoro di pressione all'albero, il
  flusso bloccato e un lobo temporizzato stretto. La traiettoria attraversa entrambi i confini del lobo.
  Dimezzare i passi di riferimento da 1 a 0.5 microseconds cambia i risultati normalizzati di meno di
  1e-10. I tick Core di 200, 100 e 25 microseconds riducono l'errore di più di 2.8x e poi di 8x, con
  l'errore più fine sotto 1e-6. L'evidenza del secondo ordine senza parete non cambia l'accoppiamento
  di parete documentato del primo ordine.
- Controlli cinematici esatti a coppia costante dell'apertura durante decelerazione/inversione; le
  valvole stazionarie e disabilitate conservano il comportamento documentato. Un tick che copre un intero
  lobo stretto con estremi chiusi deve fallire e fare rollback. Ridurre il tick risolve il suo flusso.
- Annullamento, schedule fallite, indipendenza dei batch del chiamante, isolamento dei fork, parametri
  di fasatura malformati e stepping/snapshot senza allocazioni passano su entrambi gli assembly.

Il [laboratorio a fasatura di manovella](../assets/labs/crank-timed-cylinder.power.json) supera tutti
i KPI con impronta `38f0437eac4def69` e **63** confini di report coincidenti. JSON/CLI, MCP e l'asset
v5 decodificato concordano a ogni confine, compresi due eventi di coppia fra i tempi di report. Il
residuo energetico finale è circa **8.53e-10 J** e il residuo di massa è **-2.87e-18 kg**. La velocità
campionata dell'albero va da **53.25 a 63.34 rad/s** mentre l'apertura è controllata in modo indipendente
contro l'angolo di manovella. Sono controlli numerici di parametri sintetici, non calibrazione.

L'asset v5 conserva i lettori v1–v4. I test rifiutano conteggi malformati, record di fasatura
duplicati/errati e semantiche di fasatura rimosse, e verificano una fixture autentica v4 precedente
al cambiamento con il suo digest originale, l'impronta e il replay aggiornato. La provenienza delle
fixture è registrata [nelle note delle fixture](../tests/Power.Tests/Fixtures/README.md). Le impronte
dei modelli precedenti restano invariate. I test MCP conservano anche stato/revisione su un ingresso
di picco non valido, e i controlli applicativi distinguono l'esecuzione riuscita dai KPI falliti e
dimostrano il recupero da un fallimento di runtime su lobo stretto ricreando con un tick più piccolo.

Lo schema Draft 2020-12 e tutti i **sei** documenti di laboratorio passano `jsonschema` di Python.
Sei forme di fasatura malformate sono rifiutate, compresi campi assenti, campi di profilo extra, unità
errate, un posizionamento di componente non valido e fasatura nulla. I test del compilatore coprono
separatamente le restrizioni di ciclo/intervallo e di topologia.

L'evidenza è un'esecuzione Linux locale, registrata in `artifacts/reports/valve-timing-verify.log`
e `artifacts/reports/crank-timed-cylinder.json`. I nuovi test Unity di import/Play controllano marcatori
temporizzati, reset e replay, ma **non sono stati eseguiti nell'Editor**: `POWER_UNITY_EDITOR` non è
impostato. Dai controlli gestiti non si inferisce alcun risultato di Editor, rendering, Mono/IL2CPP o
Player desktop. Combustione, comportamento completo del motore, trasmissioni, controlli e campioni di
veicolo calibrati restano aperti; tutti i parametri di ricerca restano `unverified`.

## 2026-09-22: scambio gas del cilindro mobile e lavoro conservativo all'albero

Aggiunto `gas_cylinder`, un componente di geometria che collega un albero rotante e una camera a gas
con massa/energia interna indipendenti. Il volume iniziale è derivato da posizione dell'albero e
geometria; volume/proprietà ambigui sono rifiutati. Scambio gas, lavoro all'albero e trasferimento di
parete passano attraverso rollback dell'intero batch, fork e annullamento. Le stesse definizioni sono
accettate da JSON/CLI/MCP e dall'asset portatile v4, con i lettori v1/v2/v3 conservati. Vedi
[le equazioni e il contratto](MOVING_CYLINDER.it.md).

La verifica seriale completa è passata su Linux x64 con .NET SDK 10.0.400/runtime 10.0.11 in cache
e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **68/68** controlli gestiti Core/applicazione.
- **51/51** controlli contro gli assembly Core/Assets .NET Standard 2.1 ospitati su .NET 10.
- **8/8** gruppi di integrazione MCP di processo figlio reali.
- **16/16** gruppi Zig e **6/6** test ABI Python, con entrambi gli host nativi eseguiti.
- Tutti i **176** valori di baseline nativi originali coincidono esattamente; l'inventario dei sorgenti
  contiene zero file C/C++ e Lua. La compilazione Release segnala zero avvisi ed errori.

I nuovi controlli fisici coprono:

- Accordo a valvola chiusa con il benchmark del cilindro chiuso durante rotazione avanti/indietro,
  punti morti e tick minuscoli; massa costante ed energia conservata.
- Moto a flusso bloccato aperto confrontato con un'integrazione RK4 scritta in modo indipendente delle
  ODE di massa, energia e albero. Le sue equazioni di geometria e flusso non chiamano gli helper Core
  sotto test. Dimezzare il passo di riferimento da 1 a 0.5 microseconds cambia i risultati normalizzati
  di meno di 1e-10. Ridurre il tick Core da 200 a 100 microseconds riduce l'errore di flusso liscio di
  più di 3x; 25 microseconds lo riduce di più di un ulteriore 10x e resta sotto 1e-6 relativo.
- Il raffinamento accoppiato alla parete è valutato separatamente come primo ordine: gli stessi
  raffinamenti riducono l'errore di più di 1.7x e 3x rispettivamente, con l'errore più fine sotto
  1e-5 relativo.
- Alberi condivisi/accoppiati, cilindri chiusi e aperti misti, legami gas, calore di parete e contabilità
  complete di energia/massa. Cambi di coppia schedulati falliti ripristinano tutti i tick e gli input
  precedenti; annullamento, isolamento dei fork e zero allocazioni di stepping/snapshot passano.

Il nuovo laboratorio di motoring ha impronta `dd62971021fa06e6` e **28** confini di replay, tutti
identici fra report di esperimento JSON, asset decodificati ed export MCP reale. Ammette ed espelle
in modo dimostrabile gas mentre la camera si muove. Il residuo energetico finale è
`2.9882230023758893e-10 J`; il residuo di massa è `1.463672932855431e-18 kg`.
Sono osservazioni numeriche di conservazione per parametri sintetici, non calibrazione.
I quattro report di laboratorio precedenti conservano le loro impronte e superano replay/KPI.

La copertura portatile include record misti di cilindro vecchi/nuovi, round trip esatto della geometria,
estensioni di tipo errato/duplicate/mancanti, conteggi non validi e rifiuto dei record di cilindro mobile
sotto versioni più vecchie. La fixture salvata v3 a volume fisso conserva l'impronta `eeb18a7f1dc76175`
e il replay dopo la ricodifica v4. La provenienza di sorgente/digest delle fixture è registrata in
[Fixture](../tests/Power.Tests/Fixtures/README.md).

Lo schema Draft 2020-12 e tutti e cinque i laboratori passano `jsonschema` di Python; sei documenti
di cilindro mobile malformati sono rifiutati. I test dell'agente coprono errori di geometria, proprietà
della camera, fallimento non lineare limitato senza cambi di revisione/stato e recupero.
I log sono `artifacts/reports/moving-cylinder-verify.log` e
`artifacts/reports/moving-cylinder-schema.log`; l'esperimento è
`artifacts/reports/moving-cylinder.json`.

I test Unity di pistone mobile/import/Play sono preparati ma non eseguiti: `POWER_UNITY_EDITOR` non è
impostato. Unity Editor/Play/rendering, Mono/IL2CPP, packaging del Player e l'esecuzione Windows/macOS
di questo incremento restano non verificati. Le aperture di restrizione schedulate nel tempo non
implementano la fasatura valvole in angolo di manovella. Combustione, comportamento completo del ciclo
motore, trasmissioni, controlli e campioni di veicolo calibrati restano aperti; l'obiettivo completo
di Power! non è completo.


## 2026-09-22: integrazione JSON, asset e agente della rete gas finita

Il checkpoint Core a `69bc1c4` è stato verificato prima delle modifiche: **53/53** gestiti,
**41/41** assembly Standard e **6/6** gruppi MCP. Le equazioni del solver esistenti, la costruzione
dell'impronta e i limiti fisici sono invariati in questo incremento.

La verifica seriale completa è poi passata su Linux x64 usando l'SDK .NET fissato in cache
10.0.400 e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **60/60** controlli Core/applicazione .NET 10.
- **45/45** controlli contro gli assembly Core/Assets .NET Standard 2.1, ospitati su .NET 10.
- **7/7** gruppi di integrazione MCP contro un server figlio reale.
- **16/16** gruppi Zig nativi e **6/6** test ABI Python; entrambi gli host nativi eseguiti.
- Tutti i **176** valori di baseline nativi originali coincidono esattamente; zero file C/C++ e Lua.
- Compilazione Release: **zero** avvisi e **zero** errori.

I quattro report di laboratorio superano KPI e replay: elettrotermico (11 confini),
rete termica (11), cilindro chiuso (21) e rete gas (14). Il documento gas compila alla stessa
impronta di una definizione Core assemblata in modo indipendente: `eeb18a7f1dc76175`. I report JSON,
la riproduzione v3 decodificata e l'export MCP reale concordano a ogni confine di report gas, compresi
gli eventi di valvola fra i confini di campionamento. I controlli dei residui di massa ed energia usano
limiti assoluti di 1e-14 kg e 1e-6 J rispettivamente; il test ricostruisce anche lo scambio energetico
del serbatoio dagli stati di camera e parete.

I controlli portatili includono topologia mista gas/cilindro/termica, composizione di gas non predefinita,
unità non SI, proprietà, limiti di schedule, annullamento e fallimento a metà batch con rollback del
cursore degli eventi. File rifirmati correttamente ma non validi coprono conteggi, record di estensione
mancanti/duplicati/di tipo errato, rifiuto del gas di versione vecchia e impronte stantie. Le fixture
autentiche v1 e del cilindro v2 conservano l'impronta originale e il comportamento di replay dopo la
ricodifica v3; il commit di sorgente e l'hash della fixture v2 sono registrati in
[Fixture](../tests/Power.Tests/Fixtures/README.md).

I controlli dell'agente coprono la scopribilità, i limiti di apertura iniziale/schedulata, l'atomicità
dell'input non valido, i conflitti di revisione, l'annullamento, l'indipendenza dei rami e la distinzione
fra una chiamata riuscita e un KPI che fallisce. Separatamente, `jsonschema` di Python ha validato lo
schema Draft 2020-12 pubblicato, tutti e quattro i documenti di laboratorio e una variante ad apertura
fissa, e ha rifiutato dodici casi di documenti gas malformati.

Log: `artifacts/reports/gas-integration-baseline.log`,
`artifacts/reports/gas-integration-verify.log` e
`artifacts/reports/gas-integration-schema.log`. Report e asset Unity generati restano artefatti di
build riproducibili piuttosto che fixture di sorgente.

`POWER_UNITY_EDITOR` non è impostato. Viste schematiche del gas, test di import e un test di ciclo di
vita/replay in Play Mode sono preparati ma **non eseguiti in Unity**. Editor/Play/rendering,
Mono/IL2CPP, packaging del Player e l'esecuzione Windows/macOS di questo incremento restano non
verificati. La fisica completa del ciclo motore, le trasmissioni, i controlli e la calibrazione del
veicolo restano aperti; i parametri di campione restano `unverified`.

## 2026-09-19: toolchain Python ritirata, verifica nativa spostata in C#

Il repository non contiene più Python. `tools/Install Zig.py`, `tools/VerifyNative.py`,
`legacy/native/tools/model_lab.py` e i loro test sono stati portati in C# e incorporati nello
strumento di build a file singolo `tools/Build.cs` (le app basate su file di .NET 10 consentono un
solo file sorgente). L'host ctypes è diventato P/Invoke. Gli attributi del consumatore ABI esterno
sono invariati. L'installer Zig usa l'estrattore ZIP integrato su Windows e delega le piattaforme
`tar.xz` al `tar` di sistema. CI e documenti sono stati aggiornati nello stesso cambiamento.

Verifica seriale locale su Windows x64 (`install-zig` + `native-verify`):

- Audit dei sorgenti: 0 file C/C++, 0 file Lua, 35 sorgenti Zig, 38 voci del manifest di migrazione.
- `verify` completo (Windows x64, locale, seriale): 53/53 controlli gestiti, 41/41 controlli degli assembly .NET Standard,
  6/6 gruppi di integrazione MCP, 16/16 test Zig nativi. Girano `power_host` e `power_model_host`.
- 6/6 test ABI C# portati passano, compresa la pulizia di 70 slot di compilazione falliti, il rifiuto di 15 mutazioni di documento
  e il codice di uscita 2 per KPI falliti.
- Confronto di baseline: **176/176** valori di baseline C originali coincidono esattamente (errore assoluto massimo 0.0).

## 2026-09-14: checkpoint Core della rete gas compilata

Recuperato il lavoro WSL fino a `4a81716` e la sua integrazione Core incompiuta. Il checkpoint ora
compila reti solo gas e miste gas/termiche, valida composizione e intervalli di apertura, e include
stato di massa/energia interna e contabilità dei serbatoi in snapshot, hash, fork e rollback dell'intero
batch. Un limitatore di stadio conservativo impedisce a una coppia isolata in equalizzazione di oscillare
attraverso l'equilibrio. Il cilindro chiuso invariato e i modelli lineari conservano le impronte precedenti
e il comportamento di replay.

Verifica seriale con l'SDK .NET 10.0.400 fissato in cache su Linux x64:

- **53/53** controlli Core/applicazione .NET 10.
- **41/41** controlli contro gli assembly Core/Assets .NET Standard 2.1 su .NET 10.
- **6/6** gruppi di integrazione MCP contro un server figlio reale.
- I report di replay esistenti degli esperimenti elettrotermico, termico e del cilindro passano.
- I gruppi Zig nativi e sei test ABI Python passano; tutti i **176** valori di baseline originali
  coincidono esattamente. Audit dei sorgenti: zero file C/C++ e zero file Lua.

L'evidenza specifica del gas include fisica degli ugelli bloccati/subcritici, svuotamento analitico del
recipiente e raffinamento, entalpia di riempimento del serbatoio, flusso inverso, conservazione di massa
ed energia in rete chiusa, scambio analitico di parete a tempo finito, isolamento a valvola chiusa,
rifiuto degli intervalli di input/schedule, overflow osservabile, fallimento/recupero a metà batch,
equivalenza di ramo e di batching, compilazione immutabile e zero allocazioni di stepping/snapshot.
Il rifiuto degli asset v1/v2 è testato per impedire di scartare campi gas non supportati.

Vedi [GAS_NETWORK.it.md](GAS_NETWORK.it.md) per il metodo numerico e l'ambito rimanente.
Questo checkpoint ha evidenza Linux locale; lo stato corrente della CI Windows/macOS va letto dal
workflow del suo commit. Unity Editor/Play/IL2CPP e il comportamento di veicolo calibrato restano non
verificati. Il record autonomo precedente sotto descrive il commit antecedente.

## 2026-09-14: fisica dello scambio gas (autonoma)

Aggiunta la prima fetta dell'incremento di scambio gas come **solo fisica**: `IdealGas`,
`GasVolumeState` e `Orifice` in `src/Power.Core/GasExchange.cs`. I volumi finiti ora portano massa
ed energia interna come stati indipendenti, e l'orifizio implementa le relazioni standard dell'ugello
isentropico in entrambe le direzioni con un coefficiente di efflusso e una frazione di apertura
adimensionale. `Numeric.Expm1` e `Numeric.Log1p` sono usciti da `CylinderPhysics` e sono condivisi;
le implementazioni sono invariate, e ogni hash di stato del cilindro esistente, impronta di modello
e confine di replay coincide ancora.

**Nessun tipo di nodo, tipo di componente, canale, campo di schema o versione di asset è cambiato.**
Un documento modello ancora non può contenere un volume di gas finito, e le superfici CLI, MCP e Unity
sono intatte. Il metodo di splitting proposto con flusso di Eulero all'indietro resta non validato e
non adottato. Vedi [scambio gas](GAS_EXCHANGE.it.md) per le equazioni, i limiti numerici e l'elenco
completo dei contratti che non sono atterrati.

Sei nuovi controlli analitici in `tests/Power.Tests/GasChecks.cs`, ciascuno scritto contro una forma
chiusa indipendente piuttosto che un'uscita registrata: continuità del blocco e monotonicità della
funzione di flusso per gamma in {1.1, 1.3, 1.4, 5/3}; 54 casi di ugello contro le relazioni NASA di
portata comprimibile; contratti dell'orifizio compresi antisimmetria esatta del flusso inverso, isolamento
a valvola chiusa e stati rifiutati; svuotamento adiabatico del recipiente contro la soluzione isentropica
analitica a 1e-9 relativo; l'identità di riempimento del serbatoio dU = cp*T_supply*dm con il limite
del recipiente evacuato T -> gamma*T_supply; e conservazione chiusa a due volumi a 1e-14 relativo in
massa e 1e-12 in energia con equalizzazione della pressione.

Il `dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify` seriale completo è
passato su Linux x64 con l'SDK .NET 10.0.400 fissato in cache e Zig 0.15.2: **44/44** controlli
gestiti (38 prima di questo cambiamento), **26/26** controlli contro gli assembly .NET Standard 2.1
rivolti a Unity, **6/6** gruppi di processo MCP, tre report di esperimento con 11, 11 e 21 confini di replay,
**16/16** gruppi Zig nativi e i test ABI esterni Python. Entrambi gli host Zig sono girati contro la
libreria condivisa reale, l'audit dei sorgenti ha riportato `c_source_files: 0` e `lua_files: 0`, e
tutti i **176** valori di baseline hanno coinciso esattamente con il binario C originale (errore assoluto massimo 0.0).
Il log è `artifacts/reports/gas-exchange-verify.log`.

Windows e macOS non sono stati esercitati per questo cambiamento, e la validazione di Unity Editor,
Play Mode, rendering e IL2CPP resta in sospeso come prima. I parametri di campione restano `unverified`.

## 2026-09-11: pulizia del packaging Lua

Rimosso il launcher LuaInstaller rimanente e il suo README di packaging obsoleto. Il launcher dipendeva
dal bridge `power_native` mai implementato e non aveva chiamanti attivi di build o runtime. Percorsi
originali e hash SHA-256 sono conservati nel [manifest di migrazione](../legacy/native/migration-manifest.json)
e coincidono con i file alla revisione di sorgente registrata. I documenti di progetto storici conservano
la loro provenienza; le loro proposte Lua sono ritirate.

L'audit dei sorgenti ora rifiuta sorgente, bytecode e pacchetti Lua oltre a sorgenti e header C/C++,
e riporta `lua_files: 0`. Sonde temporanee non tracciate `.lua`, `.luau`, `.luac`, `.rockspec`,
`.rock` e `.LUA` maiuscolo hanno ciascuna prodotto uno stato di uscita fallito e un errore strutturato
che identifica il file; l'albero pulito è passato dopo.

Il `dotnet run --file tools/Build.cs -- verify` seriale completo è passato su Linux x64:
**38/38** gestiti, **26/26** assembly .NET Standard, **6/6** MCP, **16/16** Zig e **6/6** controlli
ABI Python. Entrambi gli host nativi sono girati, e tutti i 176 valori di baseline hanno coinciso
esattamente. Il log è `artifacts/reports/lua-removal-verify.log`. Comportamento del nucleo, evidenza
dei campioni e file di licenza sono invariati. Unity Editor non è stato esercitato.

## 2026-09-11: migrazione Zig nativa

Il proprietario ha ripreso la migrazione del linguaggio nativo il 2026-09-10. Tutti i **26 file C
di implementazione/test/host e 12 header** sono stati sostituiti con Zig. Il
[manifest di migrazione](../legacy/native/migration-manifest.json) registra revisione Git originale,
percorsi dei file e hash SHA-256. Nell'inventario dei sorgenti del repository non restano sorgenti o
header C/C++; il comando di verifica alla radice impone quel vincolo. File di licenza/eccezione ed
evidenza dei campioni sono conservati.

Il `dotnet run --file tools/Build.cs -- verify` seriale completo è passato su Linux x64 usando
l'SDK .NET 10.0.400 fissato in cache e Zig 0.15.2: **38/38** controlli gestiti, **26/26** controlli contro
gli assembly .NET Standard 2.1 rivolti a Unity, **6/6** gruppi di processo MCP, **16/16** gruppi Zig nativi e
**6/6** test ABI esterni Python. La suite nativa è passata anche tutti i 16 gruppi in Debug con i
controlli di sicurezza abilitati. Entrambi gli host Zig sono girati contro la libreria condivisa reale.
La libreria esporta solo `pwr_get_api` e non ha simboli ELF irrisolti. Tutti i **176** valori a 11
tempi di campione elettrotermici hanno coinciso esattamente con il binario C originale su questo host, con la stessa
impronta di modello e gli stessi contratti di canale. Il confronto della fixture fra toolchain usa
ancora tolleranze esplicite, e il replay dello stesso binario deve coincidere esattamente. I report
sono in `artifacts/reports/zig-migration-verify.log`, `native-verification.json` e
`native-electrothermal.json`.

Anche la cross-compilazione ReleaseSafe di libreria/host è passata per **x86_64 Windows**
e **aarch64 macOS**. La cross-compilazione non è evidenza di esecuzione per quei sistemi. I log
locali sono `artifacts/reports/zig-cross-windows.log` e `zig-cross-macos.log`.

GitHub Actions ha poi completato con successo la verifica reale su **Linux x64, Windows x64 e
macOS arm64**, ciascuna passando tutti i **38/38** gestiti, **26/26** assembly .NET Standard, **6/6** MCP,
**16/16** Zig e **6/6** controlli ABI Python. Entrambi gli host di libreria condivisa sono girati su ogni
piattaforma. Tutti i 176 valori di baseline nativi hanno coinciso esattamente su tutti e tre i runner,
e i loro inventari di sorgenti contenevano zero sorgenti o header C/C++. Evidenza:
[esecuzione 34549950147](https://github.com/Water-Run/Power/actions/runs/34549950147),
commit di codice [`dd3de10`](https://github.com/Water-Run/Power/commit/dd3de10294949c8b2ffb49afc02a9c70b5808c00).
Il checkout Windows fissa i file Zig a LF; la verifica macOS usa gli stub Darwin inclusi in Zig per
evitare l'incompatibilità con l'SDK Apple più recente descritta nelle
[note di build nativa](NATIVE_ZIG.it.md#build-and-maintenance). I log completi dei job sono conservati
in locale sotto `artifacts/reports/zig-ci-34549950147-{linux,windows,macos}.log`, con i metadati
dell'esecuzione in `zig-ci-34549950147.json`. Le correzioni successive di documentazione e commenti
non cambiano codice eseguibile.

La suite nativa copre in più il modulo del gruppo motopropulsore automatico prima non compilato,
compresi replay, contabilità energetica, brownout e rollback delle transazioni, più la gestione
concorrente della vita dell'SDK. Tre punti di ingresso di test storici restituivano in silenzio
successo su controlli falliti; il port corregge la propagazione e separa gli scenari di combustione
stazionaria, limitatore e contropressione del motore. Vedi [le note di migrazione](NATIVE_ZIG.it.md)
per le equazioni conservate e l'assetto sperimentale corretto. Il solo risultato CTest originale era
insufficiente a causa di quei fallimenti nascosti.

Questa migrazione non completa gli obiettivi gestiti di motore/trasmissione né stabilisce la
calibrazione del veicolo. Unity Editor, Play Mode, rendering, Mono e IL2CPP non sono stati esercitati.
Tutta la calibrazione dei campioni resta `unverified`.


## 2026-09-08: incremento del cilindro chiuso

Il `tools/Build.cs verify` seriale è passato su Linux x64 con SDK 10.0.400 e runtime 10.0.11: **38/38** controlli gestiti, **26/26** controlli contro gli assembly .NET Standard 2.1 reali e **6/6** gruppi di integrazione di processo MCP. La compilazione Release ha segnalato zero avvisi ed errori. Il log è `artifacts/reports/cylinder-verify.log`; anche tutti e tre i documenti JSON di laboratorio hanno passato il JSON Schema pubblicato usando il validatore locale `jsonschema`.

I nuovi controlli coprono geometria e derivate analitiche del manovellismo, identità di stato del gas ideale, esecuzioni di conservazione di two-second con e senza contropressione, convergenza del secondo ordine sotto raffinamento del passo, rotazione inversa, passi minuscoli e punti morti, alberi condivisi/accoppiati con componenti elettrici e termici, rollback di fallimento non lineare, annullamento, fork, unità, estensioni di cilindro malformate e zero allocazioni gestite in stepping/snapshot stazionari. Una fixture v1 conservata decodifica, conserva l'impronta lineare originale e fa il replay in modo identico dopo l'export v2.

GitHub Actions ha ripetuto la stessa verifica con successo su **Windows, macOS e Linux**, con controlli 38/38, 26/26 e 6/6 e zero avvisi/errori su ogni piattaforma. Evidenza: [esecuzione 34176008291](https://github.com/Water-Run/Power/actions/runs/34176008291), commit di codice [`6209df2`](https://github.com/Water-Run/Power/commit/6209df22876f9928ddc04a1a32845cd7efe087da). I log completi dei job e i metadati di stato sono conservati in locale come `artifacts/reports/github-actions-34176008291.log` e `.json`. L'aggiornamento successivo della documentazione non cambia codice eseguibile.

L'esperimento sintetico del cilindro ha superato i KPI finali e ha fatto il replay esatto a 21 confini attraverso JSON, riproduzione dell'asset e un server figlio MCP reale. Risultati Linux: impronta `c64b61efdb827680`, velocità finale `153.00340249454544 rad/s`, pressione `118835.36885412445 Pa`, temperatura `315.16234058802814 K`, residuo energetico finale `8.7464e-10 J` e residuo assoluto campionato massimo `8.9570e-10 J`. Report completo: `artifacts/reports/sealed-cylinder.json`. Questi valori stabiliscono evidenza numerica per questo benchmark di gas ideale chiuso, non la calibrazione del motore.

I test dell'importatore Unity e di Play includono ora l'asset del cilindro e il moto schematico del pistone, ma **non sono stati eseguiti**. L'evidenza di Unity Editor, Mono, rendering e IL2CPP resta in sospeso. Lo stack attivo resta C#/Unity; la direzione futura di riscrittura Zig non introduce un runtime nativo in questo incremento.

## Checkpoint di pausa: 2026-09-08

Il proprietario ha chiesto la chiusura e una pausa dello sviluppo dopo l'incremento del cilindro. La sorgente eseguibile resta al commit di codice verificato `6209df2`; i commit successivi aggiornano solo la documentazione. L'estensione prospettica dello scambio gas non è stata applicata, compilata o pubblicata. Le sue [note di ripresa](NEXT_ENGINE_STEP.it.md) distinguono il lavoro proposto dalle capacità implementate. Per questo checkpoint di sola documentazione non serviva un'ulteriore build. Riprendere lo sviluppo solo dopo un'istruzione esplicita del proprietario.

## Baseline storica: 2026-09-07

Ambiente: 2026-09-07, Linux x64, .NET SDK 10.0.400, runtime .NET 10.0.11. Il risultato eseguito è l'output di `tools/Build.cs verify` e i report generati.

Baseline gestita a quella data: 30/30 controlli di nucleo, asset e agente, 19/19 controlli degli assembly della libreria standard e 5/5 gruppi di integrazione di processo MCP passati. La compilazione Release ha segnalato 0 avvisi e 0 errori. Il processo MCP vivo ha scoperto 12 strumenti e controllato gli schemi di input e output. Risposte di successo e di errore sono state controllate per i campi di output richiesti e un risultato testuale compatibile. Il log grezzo è `artifacts/reports/managed-verification.log`.

## Evidenze registrate allora

- Core e Assets sono stati compilati sia per `net10.0` sia per `netstandard2.1`.
- I controlli analitici hanno coperto coppia costante, la risposta RL e l'equilibrio termico. Dimezzare il passo ha controllato la convergenza meccanica del secondo ordine e la convergenza termica del primo ordine.
- Rapporti positivi e negativi sono stati controllati per quantità di moto generalizzata, calore di smorzamento e conservazione. La frenata rigenerativa è stata controllata per corrente negativa e una diminuzione del lavoro di sorgente.
- Rifiuto degli input, un overflow a un tick successivo, pre-annullamento e controlli di capacità del buffer hanno tutti confermato che stato e dati del chiamante non sono modificati parzialmente.
- Proprietà della descrizione del modello, istanze indipendenti parallele, fork di stato completo e stepping per tick rispetto al batch sono stati controllati per l'accordo.
- Un contatore di allocazione dei thread .NET ha misurato 0 allocazioni gestite per il percorso caldo del nucleo di input, step e snapshot combinati. Quel conteggio esclude compilazione, report e la UI di Unity.
- Codifica e decodifica degli asset hanno conservato sorgente, modello ed eventi. Un digest danneggiato, conteggi contraffatti, una versione di formato errata, un'impronta di modello errata e byte extra sono stati tutti rifiutati.
- Gli input schedulati hanno coperto il tempo zero, la fine di un batch e gli eventi dentro un batch di presentazione. Un fallimento numerico successivo ha fatto rollback dell'intero batch. La riproduzione degli asset ha misurato 0 allocazioni gestite quando ogni tick portava un cambio di input. L'annullamento ha conservato il cursore degli eventi.
- I report JSON e gli asset importati hanno confrontato hash di stato e valori di uscita a ogni confine di report, compresi eventi che non cadono su un confine di presentazione di 20 ms.
- Gli stessi controlli fisici hanno caricato le DLL .NET Standard 2.1 reali copiate per Unity e controllato il loro target framework. L'host era ancora .NET 10, quindi questo non mostra che Mono o IL2CPP siano passati.
- I controlli dell'agente hanno coperto diagnostica strutturata dei campi, snapshot filtrati, il limite di sessione, conflitti di revisione concorrenti, annullamento, isolamento dei fork genitore e figlio, ciclo di vita e report compatti.
- Il client MCP ufficiale ha avviato un processo figlio server reale e ha completato la scoperta di 12 strumenti, schemi di input e output, recupero dagli errori, operazioni di sessione, un esperimento completo ed export dell'asset. Il digest del file è stato controllato dopo la decodifica Base64, e la riproduzione importata è stata confrontata con lo stato finale dell'esperimento MCP.

L'esperimento elettrotermico predefinito dura 10 seconds, scende a 4 V a 5 seconds e torna a 24 V a 6 seconds. Due dimensioni di batch concordano bit a bit a 11 confini. Valori finali tipici sono circa motore `29.74182442 rad/s`, carico `9.91394147 rad/s` e temperatura del motore `302.4760663 K`. La soglia del residuo energetico è `1e-5 J`. Gli hash di replay si confrontano solo per lo stesso binario, runtime e architettura. I numeri fra runtime usano una tolleranza.

L'esperimento di scambio termico usa i nodi 42/77, nessun input esterno e un passo di 7 ms, e dura 7 seconds. Due dimensioni di batch concordano a 11 confini. La temperatura finale differisce dalla soluzione discreta di Eulero all'indietro di meno di `1e-9 K`, dalla soluzione analitica continua di meno di `0.004 K`, e l'errore di energia totale è minore di `1e-7 J`. I due report sono `artifacts/reports/electrothermal.json` e `thermal-network.json`.

## Riproduzione

```sh
dotnet run --file tools/Build.cs -- verify
```

Questi controlli sono programmi di accettazione da console che eseguono asserzioni in Release. Non dipendono da test `Debug.Assert` vuoti. Non hanno bisogno di Unity, Python o della libreria C originale. L'host di verifica nativo e l'installer Zig sono stati spostati nello stesso strumento di build .NET il 2026-09-19 (P/Invoke C#). Il progetto MCP usa il pacchetto NuGet ufficiale, e `packages.lock.json` fissa la risoluzione.

GitHub Actions ha completato la stessa accettazione gestita su Windows, macOS e Linux: 30/30, 19/19 e 5/5 su ogni piattaforma. L'evidenza è il commit di codice [`aea6136`](https://github.com/Water-Run/Power/commit/aea6136bbdcbeaea91d63836d947637e7eac730e) e l'[esecuzione 34087686661](https://github.com/Water-Run/Power/actions/runs/34087686661). `artifacts/reports/github-actions-34087686661.log` e `.json` sono conservati in locale e contengono l'output dei job e lo stato finale. Un'ulteriore accettazione locale è stata eseguita da una copia pulita dei sorgenti senza cache e senza assembly generati. Il suo log è `github-clean-checkout.log`.

I collegamenti di evidenza del repository e della CI sono pubblici. Lo sviluppo è ripreso il 2026-09-08; i record precedenti sotto identificano le proprie baseline verificate.

L'aggiornamento di pubblicazione GPL ha aggiunto notice di licenza senza cambiare il contenuto eseguibile dei sorgenti; un confronto contro il commit precedente ha confermato che tutte le 90 modifiche di sorgente/build erano soltanto notice. Una verifica seriale nuova è passata 30/30 controlli gestiti, 19/19 controlli degli assembly rivolti a Unity e 5/5 gruppi di integrazione MCP, con zero avvisi o errori di build. Il suo log è `artifacts/reports/license-verification.log`. Questo non aggiunge evidenza di validazione di Unity Editor o Player.

## Evidenze non ancora ottenute

Questo ambiente non ha Unity Editor installato. Import dell'Editor, test di Edit Mode e Play Mode, controlli di rendering della scena e una build IL2CPP non sono stati eseguiti. Progetto, scene, test e ingresso di automazione sono al loro posto:

```sh
dotnet run --file tools/Build.cs -- unity-test
```

Impostare prima `POWER_UNITY_EDITOR`. I log di Unity e i risultati XML vanno in `artifacts/unity`. I test Play hanno bisogno di una macchina che possa eseguire l'editor grafico e di una licenza Unity valida. I test scritti e non ancora eseguiti coprono import URP e degli assembly per entrambi gli asset modello, accordo della riproduzione, avvio e arresto ripetuti senza residui, l'esperimento di riferimento di 10 second, il passaggio a una topologia solo termica durante l'esecuzione, elenchi dinamici di nodi e input, e la schedulazione dei tick a 7 ms. Controlli, tema, dimensioni delle finestre e presentazione desktop hanno ancora bisogno che una persona li guardi, e le build Player per le tre piattaforme desktop sono ancora aperte.

L'ingresso di pubblicazione è `Power.Studio.Editor.ProjectSetup.BuildPlayer`, usando il target desktop selezionato e IL2CPP. Ha bisogno del modulo di build della piattaforma Unity corrispondente. Non esiste ancora un pacchetto Player compilato o testato.

Ogni parametro attuale è un parametro di esperimento sintetico. Un motore e una trasmissione completi, la calibrazione del veicolo, emissioni, acustica, un budget di tempo reale e le esecuzioni lunghe hanno ancora bisogno della propria implementazione e della propria evidenza. Questi controlli non stabiliscono quel lavoro.


