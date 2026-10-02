# Confine Zig nativo

[English](NATIVE_ZIG.md) · [简体中文](NATIVE_ZIG.zh-CN.md) · [Français](NATIVE_ZIG.fr.md) · [Русский](NATIVE_ZIG.ru.md) · [日本語](NATIVE_ZIG.ja.md) · [한국어](NATIVE_ZIG.ko.md) · [Deutsch](NATIVE_ZIG.de.md) · [Español](NATIVE_ZIG.es.md) · **Italiano** · [Português](NATIVE_ZIG.pt-BR.md)

Il proprietario ha ripreso la migrazione del linguaggio nativo il 2026-09-10. I prototipi nativi sotto `legacy/native` sono stati migrati a Zig, compresi i test e gli host. Restano un runtime di ricerca separato: `Power.Core` e `Power.Assets` mantengono i contratti gestiti privi di dipendenze e i doppi target, e Unity continua a caricare gli assembly gestiti.

## Interoperabilità

La libreria condivisa nativa conserva il punto di ingresso versionato `pwr_get_api`, campi scalari a larghezza fissa, dimensioni delle strutture, tempo intero in nanosecondi, handle di generazione, buffer di istantanea posseduti dal chiamante e la tabella delle funzioni. Le dichiarazioni Zig `extern struct` e le convenzioni di chiamata `.c` esprimono l'ABI binaria esistente; non richiedono sorgenti o header C in questo repository. Il runner di esperimenti P/Invoke C# nello strumento di build .NET resta un consumatore di questo confine. I prototipi nativi di motore, scarico e trasmissione restano API Zig interne, invece di essere aggiunti in silenzio allo schema del modello gestito o alle capacità native pubbliche.

La migrazione deve conservare equazioni, limiti del modello, ID stabili, diagnostica, rollback dei batch, registri di energia e di massa, e scenari di regressione. La fedeltà dei prototipi nativi e la calibrazione `unverified` dei campioni non cambiano. La verifica nativa è separata dall'evidenza reale di Unity Editor, Play Mode e IL2CPP e dal completamento dell'obiettivo del gruppo motopropulsore completo.

## Provenienza

L'implementazione C originale è recuperabile dal commit Git `c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3`. I percorsi e gli hash dei file sorgente sono registrati in `legacy/native/migration-manifest.json`. Il port Zig conserva il copyright originale e gli avvisi GPL-3.0-or-later con l'eccezione di collegamento Unity. I documenti storici di progettazione e ricerca conservano le citazioni originali; le loro descrizioni dell'epoca C non descrivono la nuova build.

Il launcher LuaInstaller inutilizzato e il suo README di packaging sono stati ritirati il 2026-09-11. I percorsi e gli hash originali sono inclusi nello stesso manifest e si riferiscono alla stessa revisione dei sorgenti. Il launcher dipendeva da un ponte `power_native` non implementato e non ha mai fatto parte di una build funzionante. Le operazioni attuali di CLI e di modello usano gli host C#/JSON e Zig esistenti più l'host ABI P/Invoke dello strumento di build C#. Le proposte Lua nei documenti storici sono registrazioni di provenienza, non dipendenze attuali né requisiti di implementazione.


<a id="build-and-maintenance"></a>
## Build e manutenzione

Il compilatore è fissato a Zig 0.15.2 in `.zig-version`. Il comando `install-zig` dello strumento di build (dentro `tools/Build.cs`) usa i [metadati di download ufficiali di Zig](https://ziglang.org/download/index.json) con gli hash degli archivi per piattaforma committati. La build non ha bisogno di un traduttore C, di header, di CMake né della compilazione di sorgenti C. Linux non ha bisogno di libc; macOS usa `libSystem` fornito dal sistema operativo. Il port è stato tradotto una volta all'inizio, poi suddiviso in moduli Zig mantenuti con layout binari condivisi. Memoria, funzioni matematiche e operazioni atomiche usano Zig e le API del sistema operativo della piattaforma. I controlli di sicurezza restano abilitati nelle build ReleaseSafe.

I sorgenti Zig usano checkout LF su ogni piattaforma. Su macOS gli strumenti di verifica disabilitano la scoperta dell'SDK Apple solo nei sottoprocessi di build Zig impostando `DEVELOPER_DIR=/dev/null`. Questo seleziona gli stub del linker Darwin inclusi in Zig ed evita l'[incompatibilità di Zig 0.15.2 con Xcode 26.4 e SDK più recenti](https://github.com/ghostty-org/ghostty/issues/11991), il cui stub `libSystem` usa target arm64e. La selezione di Xcode di sistema non cambia; questi target nativi non hanno bisogno di framework Apple né di header dell'SDK.

`dotnet run --file tools/Build.cs -- verify` esegue la verifica gestita, poi la verifica nativa in serie. `native-verify` esegue solo la parte nativa. L'implementazione di `native-verify` (dentro `tools/Build.cs`) rifiuta sorgenti e header C/C++ oltre a sorgente, bytecode e pacchetti Lua. Controlla il pin del compilatore e la formattazione, compila la libreria ed entrambi gli host Zig, esegue le suite Zig e la suite dell'host ABI P/Invoke C#, e confronta l'esperimento elettrotermico con la fixture di baseline C originale. Su Linux controlla anche che solo `pwr_get_api` sia esportato pubblicamente e che la libreria non abbia simboli esterni non risolti.

Il confronto con la baseline conserva impronte del modello, unità, mappature dei canali, 11 istanti di campionamento e valori fisici. I valori tra toolchain diverse usano tolleranze assolute/relative esplicite; gli hash di replay devono coincidere all'interno dello stesso binario. I report sotto `artifacts/reports` distinguono esecuzione, risultati di KPI/replay e calibrazione `unverified`. Le cache del compilatore ignorate e i checkout privati di riferimento di terze parti non sono sorgente del repository.

## Correzioni dei test dell'archivio

Tre vecchi punti di ingresso dei test restituivano zero anche quando la loro macro `CHECK` falliva. Il port propaga questi fallimenti. La suite del motore in precedenza si fermava su un fallimento nascosto perché il suo campione finale a carico di 22 Nm poteva trovarsi al taglio carburante del limitatore di giri. Quello scenario è conservato come test esplicito del limitatore; la combustione continua usa un carico di prova sintetico di 32 Nm. Il confronto di contropressione ora parte da uno stato di funzionamento condiviso e controlla l'incremento analitico della coppia di pompaggio prima di confrontare la velocità, evitando la confusione tra avviamento e stallo. Non sono state cambiate equazioni del motore né valori di calibrazione di produzione. La copertura del gruppo motopropulsore automatico ora esercita anche replay, energia differenziale, brownout e rollback; la vecchia build CMake ometteva l'intero modulo.
