# Native Zig-Grenze

[English](NATIVE_ZIG.md) · [简体中文](NATIVE_ZIG.zh-CN.md) · [Français](NATIVE_ZIG.fr.md) · [Русский](NATIVE_ZIG.ru.md) · [日本語](NATIVE_ZIG.ja.md) · [한국어](NATIVE_ZIG.ko.md) · **Deutsch** · [Español](NATIVE_ZIG.es.md) · [Italiano](NATIVE_ZIG.it.md) · [Português](NATIVE_ZIG.pt-BR.md)

Der Eigentümer hat die Migration der nativen Sprache am 2026-09-10 wieder aufgenommen. Die nativen
Prototypen unter `legacy/native` sind nach Zig migriert, einschließlich ihrer
Tests und Hosts. Sie bleiben eine getrennte Forschungslaufzeit: `Power.Core` und
`Power.Assets` behalten ihre abhängigkeitsfreien verwalteten Verträge und Doppelziele,
und Unity lädt weiterhin die verwalteten Assemblies.

## Interoperabilität

Die native Shared Library behält den versionierten Einstiegspunkt `pwr_get_api`,
Skalarfelder fester Breite, Strukturgrößen, ganzzahlige Nanosekundenzeit, Generations-Handles, vom Aufrufer besessene Schnappschusspuffer und die Funktionstabelle. Zig-Deklarationen `extern struct`
und Aufrufkonventionen `.c` drücken das bestehende binäre ABI aus;
sie verlangen keine C-Quellen oder Header in diesem Repository. Der C#-P/Invoke-Experimentrunner im .NET-Build-Werkzeug bleibt ein Verbraucher dieser Grenze.
Native Motor-, Auslass- und Getriebeprototypen bleiben interne Zig-APIs und werden nicht
still dem verwalteten Modellschema oder öffentlichen nativen Fähigkeiten hinzugefügt.

Die Migration muss Gleichungen, Modellgrenzen, stabile IDs, Diagnosen,
Batch-Rollback, Energie- und Massenbilanzen sowie Regressionsszenarien behalten. Die Modelltreue der nativen
Prototypen und die Beispielkalibrierung `unverified` sind unverändert. Die native
Prüfung ist getrennt von tatsächlichen Unity-Editor-, Play-Mode- und IL2CPP-Nachweisen und vom Abschluss des vollen Antriebsstrangziels.

## Herkunft

Die ursprüngliche C-Implementierung ist aus dem Git-Commit
`c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3` wiederherstellbar. Pfade und Hashes ihrer Quelldateien
stehen in `legacy/native/migration-manifest.json`. Die Zig-Portierung behält
die ursprünglichen Urheberrechts- und GPL-3.0-or-later-Hinweise mit Unity-Linking-Ausnahme.
Historische Entwurfs- und Forschungsdokumente behalten ihre ursprünglichen
Zitate; ihre Beschreibungen aus der C-Zeit beschreiben nicht den neuen Build.

Der ungenutzte LuaInstaller-Starter und seine Paket-README wurden am
2026-09-11 zurückgezogen. Ihre ursprünglichen Pfade und Hashes stehen im selben Manifest
und verweisen auf dieselbe Quellrevision. Der Starter hing von einer nicht umgesetzten
Brücke `power_native` ab und gehörte nie zu einem funktionierenden Build. Aktuelle CLI- und
Modelloperationen verwenden die bestehenden C#-/JSON- und Zig-Hosts sowie den P/Invoke-ABI-Host
des C#-Build-Werkzeugs. Lua-Vorschläge
in historischen Dokumenten sind Herkunftsbelege, keine aktuellen Abhängigkeiten oder
Umsetzungsanforderungen.


<a id="build-and-maintenance"></a>
## Bauen und Pflege

Der Compiler ist in `.zig-version` auf Zig 0.15.2 gepinnt. Der Befehl
`install-zig` des Build-Werkzeugs (in `tools/Build.cs`) verwendet die offiziellen
[Zig-Download-Metadaten](https://ziglang.org/download/index.json)
mit committeten Archiv-Hashes je Plattform. Der Build braucht keinen C-Übersetzer, keine Header, kein CMake und
keine Kompilierung von C-Quellen. Linux braucht keine libc; macOS verwendet
das vom Betriebssystem bereitgestellte `libSystem`. Die Portierung wurde zunächst einmal übersetzt
und dann in gepflegte Zig-Module mit gemeinsamen binären Layouts aufgeteilt. Speicher,
mathematische Funktionen und atomare Operationen verwenden Zig und die OS-APIs der Plattform.
Sicherheitsprüfungen bleiben in ReleaseSafe-Builds aktiv.

Zig-Quellen verwenden auf jeder Plattform LF-Checkouts. Unter macOS schaltet das Prüfwerkzeug die Apple-SDK-Erkennung nur in seinen Zig-Build-Teilprozessen ab, indem es
`DEVELOPER_DIR=/dev/null` setzt. Das wählt die mitgelieferten Darwin-Linker-Stubs von Zig und umgeht die [Inkompatibilität von Zig 0.15.2 mit Xcode 26.4 und neueren SDKs](https://github.com/ghostty-org/ghostty/issues/11991),
deren `libSystem`-Stub arm64e-Ziele verwendet. Die Xcode-Auswahl des Systems bleibt unverändert;
diese nativen Ziele brauchen keine Apple-Frameworks oder SDK-Header.

`dotnet run --file tools/Build.cs -- verify` führt die verwaltete Prüfung aus und danach
die serielle native Prüfung. `native-verify` führt nur den nativen Teil aus.
Die Umsetzung von `native-verify` (in `tools/Build.cs`) lehnt C/C++-Quellen und Header sowie Lua-Quelltext, Bytecode und Pakete ab. Sie prüft
Compiler-Pin und Formatierung, baut die Bibliothek und beide Zig-Hosts, führt die
Zig-Suiten und die C#-P/Invoke-ABI-Host-Suite aus und vergleicht das elektrothermische
Experiment mit seiner ursprünglichen numerischen C-Baseline-Fixture. Unter Linux prüft sie außerdem, dass
nur `pwr_get_api` öffentlich exportiert wird und die Bibliothek keine unaufgelösten
externen Symbole hat.

Der Baseline-Vergleich erhält Modell-Fingerabdrücke, Einheiten, Kanalzuordnungen,
11 Abtastzeitpunkte und physikalische Werte. Werte über Toolchains hinweg verwenden explizite
absolute und relative Toleranzen; Replay-Hashes müssen innerhalb desselben Binary übereinstimmen.
Berichte unter `artifacts/reports` trennen Ausführung, KPI-/Replay-Ergebnisse und
ungeprüfte Kalibrierung. Ignorierte Compiler-Caches und private Drittanbieter-Referenz-Checkouts sind keine Repository-Quellen.

## Korrekturen der Archivtests

Drei alte Testeinstiege lieferten null, auch wenn ihr Makro `CHECK` fehlschlug.
Die Portierung gibt diese Fehler weiter. Die Motorsuite blieb früher an einem
verdeckten Fehler stehen, weil ihre letzte Probe bei 22 Nm Last an der Kraftstoffabschaltung des Drehzahlbegrenzers liegen konnte.
Dieses Szenario bleibt als expliziter Begrenzertest erhalten; kontinuierliche Verbrennung
verwendet eine synthetische Prüflast von 32 Nm. Der Gegendruckvergleich startet jetzt aus einem
gemeinsamen laufenden Zustand und prüft das analytische Pumpmoment-Inkrement, bevor
er die Drehzahl vergleicht, damit Anlauf und Abwürgen sich nicht vermischen. Es wurden keine Motorgleichungen oder
Produktionskalibrierungswerte geändert. Die Abdeckung des Automatik-Antriebsstrangs übt jetzt
außerdem Replay, Differenzialenergie, Spannungseinbruch und Rollback; der alte
CMake-Build ließ dieses ganze Modul aus.
