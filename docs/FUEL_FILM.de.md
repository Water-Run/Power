# Endlicher flüssiger Kraftstofffilm und Verdampfung

[English](FUEL_FILM.md) · [简体中文](FUEL_FILM.zh-CN.md) · [Français](FUEL_FILM.fr.md) · [Русский](FUEL_FILM.ru.md) · [日本語](FUEL_FILM.ja.md) · [한국어](FUEL_FILM.ko.md) · **Deutsch** · [Español](FUEL_FILM.es.md) · [Italiano](FUEL_FILM.it.md) · [Português](FUEL_FILM.pt-BR.md)

`fuel_film` speichert ein explizites anfängliches Flüssigkeitsinventar neben einem verfolgten Gasempfänger.
Ein endlicher thermischer Knoten liefert fühlbare Wärme und Phasenwechselwärme. Verdampfter
Kraftstoff tritt zur Masse, zur inneren Energie und zur Kraftstoffkonstituente des Empfängers hinzu; die bestehende
Vormischreaktion verbraucht nur Dampf. Das Flüssigkeitsinventar ist anfängliche Benetzung,
kein eingespritzter Kraftstoff, und bleibt Teil der Gesamtbilanz von Masse und chemischer Energie.

Das ist ein Forschungsmodell mit konstanten Eigenschaften, vernachlässigbarem Flüssigkeitsverdrängungsvolumen
und vorgeschriebener Sättigungstemperatur. Es bildet keine flüssige Leitung,
keine Nadel- oder Spraydynamik, kein druckabhängiges Phasengleichgewicht, keine Kondensation,
keine mehrkomponentigen Kraftstoffeigenschaften und kein kalibriertes Benzinverhalten ab.

## Phasenenergie und endliche Wärmequelle

`c_l` sei die spezifische Wärme der Flüssigkeit, `c_v` die isochore Wärmekapazität des Empfängergases,
`T_s` die erklärte Sättigungstemperatur und `L_u > 0` die spezifische Differenz der inneren Energie
von Dampf minus Flüssigkeit bei `T_s`. Die gemeinsame thermische Referenz ist:

```text
e_offset = (c_v - c_l) T_s - L_u
U_liquid = m_liquid (c_l T_liquid + e_offset)
U_vapor_added = delta_m c_v T_s
```

`L_u` ist eine Differenz der inneren Energie in J/kg und keine Verdampfungsenthalpie. Eine gelieferte Enthalpie braucht eine explizite, begründete Umrechnung, bevor
sie hier verwendet werden kann. Die thermische Energie der Flüssigkeit kann unter dieser Referenz negativ sein;
Temperatur und Masse müssen trotzdem physikalisch zulässig bleiben. Die chemische Energie
`m_liquid * LHV` ist getrennt und geht mit dem Dampf über, ohne Reaktionswärme oder externe Quellenarbeit zu erzeugen.

Unterhalb der Sättigung koppelt der Leitwert `K` die Flüssigkeitskapazität `m_liquid c_l` an die endliche
Wandkapazität `C_w`. Die Temperaturdifferenz klingt analytisch mit der Rate
`K (1 / (m_liquid c_l) + 1 / C_w)` ab. Die kapazitätsgewichtete mittlere Temperatur bleibt
konstant. Erreicht die Flüssigkeit `T_s`, löst das Gesetz diesen Zeitpunkt auf und verwendet das
verbleibende Intervall für die Verdampfung.

Bei Sättigung mit `T_wall > T_s` klingt die Wandüberhitzung mit der Rate `K / C_w` ab.
Die verfügbare Phasenwärme über das Intervall `h` ist
`C_w (T_wall - T_s) (1 - exp(-K h / C_w))`, begrenzt durch `m_liquid L_u`.
Der Film bleibt bei `T_s`, bis er trocken ist; die verdampfte Masse ist die Phasenwärme geteilt durch
`L_u`. Austrocknen lässt genau die Flüssigkeitsmasse null und die Energie null zurück und beendet den Wärmeentzug.
Eine kalte Wand kann vorhandene Flüssigkeit kühlen; sie kondensiert keinen Empfängerdampf.

Jeder Transfer erfüllt `delta U_liquid + U_vapor_added = Q_from_wall`.
Die Wand verliert dieselbe Wärme, sodass der Phasenwechsel keine externe
Energiegrenze einführt. Nichtnegative Kraftstoffinventare und die volle Konstituentenbilanz
werden unabhängig von der Gesamtenergiebilanz geprüft.

## Graph- und Dokumentvertrag

| Daten | Anforderung |
|---|---|
| `node_a` | Endlicher Gasempfänger mit expliziter Verfolgung von Vormischkraftstoff und LHV |
| `node_b` | Endliche thermische Wand, mit positiver Kapazität und Temperatur |
| `initial_mass` | Nichtnegative kg; das vollständige anfängliche Flüssigkeitsinventar |
| `initial_temperature` | Positive K, nicht größer als die Sättigung |
| `liquid_specific_heat` | Positive J/(kg K), JSON-Einheit `j_kg_k` |
| `saturation_temperature` | Positive K, JSON-Einheit `k` |
| `latent_internal_energy` | Positive J/kg, JSON-Einheit `j_kg` |
| `conductance` | Nichtnegative W/K, JSON-Einheit `w_k` |

JSON verlangt alle sechs Parameter. Der Film hat keinen Eingabekanal, keine Kurbelsteuerzeit und keine
getrennte Wärmesenke. Unbeteiligte Parameter, falsche Anschlussdomänen, Einheiten, nicht endliche
Werte und nicht unterstützte Zustandskapazität werden abgelehnt. Aufrufer im Core verwenden
`ComponentDefinition.LiquidFilm` und `FuelFilmDefinition`; das unabhängige
Gesetz `EquilibriumFuelFilm` stellt zulässiges Erzeugen von Zustand und den Fortschritt im endlichen Bad bereit.

Jeder Film trägt fünf gemeldete Zustandseinträge zum begrenzten Zustandsbudget des Compilers bei.
Masse, thermische Energie, Verdampfungshistorie, mittlerer Strom, Wandwärme und
ihre kompensierten Historien gehören der Simulation. Abbruch, abgelehnte Eingaben,
späte Löserfehler, unabhängige Verzweigungen und spekulative Kupplungsintervalle erhalten
die vollständige Transaktion. Erfolgreiches Vorschreiten und das Lesen von Momentaufnahmen allokieren nach dem Aufwärmen keinen
verwalteten Speicher.

## Integrationsgenauigkeit

Ein angenommenes Intervall verwendet die Halbschritte Film / Gas / Mechanik-und-Reaktion / Gas / Film.
Filme, die eine Wand teilen, laufen vor dem Gasfortschritt in stabiler Komponentenreihenfolge und danach in umgekehrter Reihenfolge.
Ihre endliche Wandtemperatur wird zwischen den Filmteilschritten mitgeführt, und die Wandwärme geht in dieselbe thermische Lösung ein.

Das isolierte Gesetz des endlichen Bades ist über fühlbares Heizen, Sättigung und
Austrocknen analytisch. Unabhängige gleichzeitige Integration gewöhnlicher Differentialgleichungen prüft glatte Verfeinerung zweiter
Ordnung für zwei Filme an einer gemeinsamen Wand und für Dampf, der durch einen kritischen
Gasauslass transportiert wird, ohne andere Wandwärmequellen. Gaswärmeverbindungen
und andere thermische Quellen lesen weiter die explizite Wandtemperatur des Außenintervalls,
sodass diese Kopplung die Genauigkeit erster Ordnung behält. Reaktionsfenster,
Ventilereignisse und Austrocknen brauchen eigene Verfeinerungsprüfungen; exaktes Batch-Replay allein
belegt weder die Zeitschrittgenauigkeit noch einheitlich zweite Ordnung für einen gezündeten Antriebsstrang.

## Beobachtbare und portable Semantik

| Filmfeld | Bedeutung |
|---|---|
| `mass` | Verbleibender flüssiger Kraftstoff, kg |
| `temperature` | Flüssigkeitstemperatur; erklärte Sättigungstemperatur, wenn trocken |
| `internal_energy` | Vorzeichenbehaftete thermische Energie der Flüssigkeit unter der erklärten Phasenreferenz, J |
| `chemical_energy` | Verbleibende chemische Energie des flüssigen Kraftstoffs, J |
| `evaporated_fuel_mass` | Kumulativ geförderter Dampf, kg |
| `mass_flow` | Mittlere Dampfförderung über den letzten vollständigen physikalischen Tick, kg/s |
| `film_wall_heat` | Kumulative von der Wand entzogene Wärme, J; Kühlen kann sie negativ machen |
| `heat_flow` | Momentanes `K (T_wall - T_liquid)`, W; null, wenn trocken |

Ausgabe-IDs und Einheiten über Validierung oder Sitzungserzeugung auffinden. Globale
Beobachtungsgrößen für Masse, Kraftstoff und chemische Energie schließen das Filminventar ein. Interne
Dampfförderung erhöht weder die Reservoir-Kraftstoffenergie noch die externe Enthalpie.

Asset v19 speichert alle Phaseneigenschaften und behält Leser für v1-v18. Jeder Film braucht
einen typisierten Phasendatensatz von 64 Byte. Begrenzte Längen und Anzahlen, Digest, vollständige Abdeckung,
doppelte oder fehlende Datensätze, Einheiten, physikalische Kompilierung und Schutz vor Herabstufung
werden geprüft. Das authentische Fixture v17 der Kraftstoffdosierung behält seinen Fingerabdruck und
das aufgewertete Replay derselben Laufzeit. Siehe [ASSET_FORMAT.de.md](ASSET_FORMAT.de.md).

## Laboratorium und verbleibende Arbeit

`film-fired-cylinder` heizt einen anfangs benetzten Film, lässt Luft getrennt zu und
verbraucht dann verfügbaren Dampf durch den vorgeschriebenen Wiebe-Abbrand. Die endliche heiße Wand
zahlt die Phasenwärme; Flüssigkeit brennt nicht direkt. JSON, CLI, portables Replay und
der tatsächliche MCP-Server stimmen an jeder Berichtsgrenze überein. Verträge für Quelle, Schema und Sitzung
bleiben gemeinsam; die Parameter sind `unverified`.

Siehe [VALIDATION.de.md](VALIDATION.de.md) für gemessenen numerischen Nachweis. Vorbereitete Unity-
Filmmarken und Lebenszyklusprüfungen verlangen weiterhin tatsächliche Verifikation in Editor und Play.
Der getrennte [Flüssigeinspritzer](LIQUID_FUEL_INJECTION.de.md) füllt Filme nun aus
einer endlichen nachgiebigen Quelle nach. Pumpe und Nachfüllung, Nadel und Spray, gemessene Kraftstoffeigenschaften,
Zündung und ECU, vollständiges Einlass- und Auslassverhalten, Getrieberegelung und kalibrierte
Antriebsstränge bleiben getrennte Anforderungen.
