# Vormischverbrennung und Bilanz der Kraftstoffenergie

[English](PREMIXED_COMBUSTION.md) · [简体中文](PREMIXED_COMBUSTION.zh-CN.md) · [Français](PREMIXED_COMBUSTION.fr.md) · [Русский](PREMIXED_COMBUSTION.ru.md) · [日本語](PREMIXED_COMBUSTION.ja.md) · [한국어](PREMIXED_COMBUSTION.ko.md) · **Deutsch** · [Español](PREMIXED_COMBUSTION.es.md) · [Italiano](PREMIXED_COMBUSTION.it.md) · [Português](PREMIXED_COMBUSTION.pt-BR.md)

`premixed_combustion` koppelt ein vorgeschriebenes Wiebe-Abbrandprofil an eine Kurbel und eine endliche
Gaskammer. Kraftstoff, Frischluft und inerte Produkte werden durch das Gasnetz transportiert;
die Reaktion verbraucht den verfügbaren limitierenden Reaktanten und wandelt gespeicherte chemische Energie
in thermische Gasenergie um. Druckarbeit treibt denselben Kurbellöser an, den bewegte
Zylinder verwenden. Core, JSON, CLI, MCP und Asset v6 teilen diese Definitionen.

Das ist ein konzentriertes Vormischmodell mit konstanten Eigenschaften. Jede Konstituente in einem verbundenen
Netz teilt ein R und gamma. Die drei Massenklassen bilden keine detaillierten Spezies ab,
keine variablen Wärmekapazitäten, keine Reaktionskinetik, keine Flammenausbreitung, keine Selbstzündung, kein Klopfen,
keine Emissionen, keine Kraftstoffverdampfung und keine Einspritzung. Das ursprüngliche gezündete Beispiel verwendet einen bereits gemischten gasförmigen
Einlass. [Zyklus-Kraftstoffdosierung](FUEL_METERING.de.md) unterstützt eine getrennte endliche gasförmige
Leitung und Luftzufuhr; flüssiges Spray und Verdampfung bleiben außerhalb des Modells. Ein vorgeschriebener Abbrand und bestandene Erhaltungsprüfungen belegen keine gemessene Motorleistung
und schließen das vollständige Antriebsstrangziel nicht ab.

## Zusammensetzung und Anschlüsse

Ein Gasknoten kann seinem bestehenden Objekt `gas` optional `premixed` hinzufügen:

```json
"gas": {
  "gas_constant": { "value": 287, "unit": "j_kg_k" },
  "gamma": 1.35,
  "premixed": {
    "lower_heating_value": { "value": 44000000, "unit": "j_kg" },
    "stoichiometric_air_fuel_ratio": 14.7,
    "initial_fractions": { "fuel": 0.04, "fresh_air": 0.96 }
  }
}
```

Heizwert und stöchiometrisches Luft-/Kraftstoff-Massenverhältnis müssen positiv und endlich sein.
Die Anteile von Kraftstoff und Frischluft müssen nichtnegativ sein, ihre Summe höchstens eins. Der Rest
sind inerte Produkte. Frischluft steht für das Oxidationsmittel zusammen mit seinem Verdünnungsmittel; der Verbrauch von
`r` kg Frischluft mit 1 kg Kraftstoff erzeugt `1+r` kg Produkte. Überschüssige Frischluft oder überschüssiger Kraftstoff
bleibt verfügbar; Produkte können nicht erneut reagieren.

Jede Reservoir-Drossel an einem Vormischknoten muss explizite
`reservoir_fractions` angeben, mit denselben zwei Feldern. Anteile sind an anderen
Komponenten oder inneren Drosseln verboten. Beim Einströmen liefert die Grenze diese Zusammensetzung;
beim Ausströmen entnimmt sie die tatsächliche Zusammensetzung des endlichen Volumens. Verbundene endliche Gasvolumina
müssen Verfolgung, R, gamma, LHV und stöchiometrisches Verhältnis teilen. Unverträgliche oder
nicht verfolgte Verbindungen werden abgelehnt; chemische Inventare können an einem Anschluss nicht verschwinden.

Der Gaslöser überträgt jede Konstituente mit demselben vorzeichenbehafteten Massenfluss und denselben stromaufwärtigen
Anteilen wie das Gesamtgas. Er entwickelt nichtnegative Konstituentenmassen und rekonstruiert die Gesamtmasse
aus ihrer Summe. Ein Vormischschritt ist auch durch den gesamten ausgehenden Strom begrenzt, selbst wenn
eingehende und ausgehende Gesamtmassenraten sich nahezu aufheben. Druckausgleich oder Rückstrom aus dem Reservoir
erzeugt kein chemisches Inventar.

Ein Vormischgas ergänzt drei gespeicherte Konstituentenwerte zum erklärten Zustandsbudget. Eine Abbrandkomponente
ergänzt eine irreversible Winkelfront; alle bleiben innerhalb der bestehenden Grenze von 64 Zuständen.
Kompensierte Grenz- und Reaktionsbilanzen nehmen an Rollback, Hashing und Verzweigungen teil.

## Abbrandgesetz und Kurbelhistorie

Die Komponente verbindet `node_a` (Kurbel) mit `node_b` (Vormischgas). Eine bewegte Kammer muss
ihre eigene Geometriekurbel verwenden, und jede Kammer erlaubt höchstens eine Abbrandkomponente. Ein fester
Behälter darf für analytische Experimente eine unabhängige Kurbel verwenden.

```json
{
  "id": 15,
  "kind": "premixed_combustion",
  "node_a": 1,
  "node_b": 2,
  "input_channel": 103,
  "initial_input": { "value": 1, "unit": "fraction" },
  "parameters": {
    "cycle_angle": { "value": 720, "unit": "deg" },
    "start_angle": { "value": 340, "unit": "deg" },
    "duration_angle": { "value": 80, "unit": "deg" },
    "shape_exponent": 3,
    "burn_coefficient": 6.9
  }
}
```

Der Zyklus ist explizit 360 oder 720 Grad. Der Startwinkel ist relativ zur tatsächlichen Kurbel,
nicht implizit um die Geometriephase des Zylinders versetzt. Die Dauer liegt in [1e-6 rad, Zykluswinkel];
der Formexponent `n` liegt in [1,16] und der Koeffizient `a` in (0,50]. Der Start wird modulo
des Zyklus normiert. Alle Winkel verlangen Einheiten. Für den Vorwärtsfortschritt `z` ab Abbrandbeginn, beschnitten
auf [0,1], ist die integrierte Hazardfunktion `H(z) = a z^n`. Jeder volle Zyklus trägt `a` bei.

Über neu durchlaufene Vorwärtswinkel:

```text
hazard = burn_multiplier * delta(H)
limiting_fuel = min(fuel_mass, fresh_air_mass / stoichiometric_air_fuel_ratio)
burned_fuel = limiting_fuel * (1 - exp(-hazard))
consumed_air = stoichiometric_air_fuel_ratio * burned_fuel
created_products = burned_fuel + consumed_air
released_heat = LHV * burned_fuel
```

Für eine geschlossene Ladung und den Multiplikator 1 ist der verbrannte Anteil `1-exp(-a z^n)` ihrer
anfänglichen Menge an limitierendem Kraftstoff. Er wird an der Dauergrenze **nicht** auf eins gezwungen:
`exp(-a)` bleibt nach einem vollen Abbrandfenster unverbrannt. Kleine Expositionen verwenden `expm1`, um
Auslöschung zu vermeiden. Frische Ladung, die während eines aktiven Fensters eintritt, tritt zu den gut durchmischten
Reaktanten hinzu; es gibt keine versteckte, unbegrenzte Wärmequelle je Zyklus.

Der optionale Eingabekanal ist `burn_multiplier`, ein Anteil in [0,1], der die Hazardfunktion skaliert.
Null deaktiviert die Reaktion; er stoppt nicht den Kraftstoffeintritt durch einen offenen Einlass. Diese Eingabe
ist kein Injektorbefehl und kein prädiktiver Zündregler.

Jede Komponente speichert den größten erreichten Kurbelwinkel, initialisiert mit dem Startwinkel.
Reaktion tritt nur jenseits dieser Front auf. Anhalten, Rückwärtsdrehen oder
erneutes Durchlaufen bereits besuchter Winkel kann Wärme nicht erneut freisetzen. Deaktivierter Vorwärtsweg
verschiebt die Front trotzdem, sodass erneutes Aktivieren verpasste Wärme nicht freisetzt. Ein Start innerhalb
eines Abbrandfensters verbraucht nur seine verbleibende Vorwärtsexposition. Nach einer großen Umkehr
bleibt der Abbrand unterdrückt, bis die Kurbel ihr bisheriges Maximum überschreitet; bidirektionale
Motorzündung und reglergesteuertes erneutes Aktivieren bleiben künftige Regelungsarbeit.

## Energie und numerische Kopplung

Die innere Gasenergie bleibt thermisch: `U = m cv T`. Die chemische Energie ist getrennt
`E_chemical = m_fuel LHV`. Die gesamte Reservoir-Enthalpie enthält sowohl `mdot cp T` als auch die
transportierte chemische Energie. Die globale Änderung der gespeicherten Energie enthält das chemische Inventar,
sodass Verbrennung eine innere Umwandlung ist und keine zusätzliche externe Quellenarbeit:

```text
energy_residual = mechanische/elektrische Quellenarbeit + gesamte Reservoir-Enthalpie
                  - abgeführte Wärme - Änderung(gesamte gespeicherte Energie)
```

`net_fuel_energy_in` stellt den chemischen Teil der Grenzbilanz getrennt bereit. Es ist
Nettozufluss, einschließlich unverbrannten Kraftstoffs, der das Modell verlässt; es ist nicht die Brutto-Kraftstoffförderung und
keine stationäre Kennzahl des Kraftstoffverbrauchs. `fuel_residual` und `fresh_air_residual` vergleichen
Anfangsinventar, Nettotransfer über die Grenze, aktuelles Inventar und kumulative Reaktion.
`mass_residual` deckt weiterhin die Gesamtgasmasse ab. Die Umwandlung der Konstituenten erhält die Masse.

Für eine bewegte Kammer hängt die Wärmevorschau vom probeweisen neuen Kurbelwinkel ab und nimmt
an der nichtlinearen Kurbellösung teil. Mit der Gesamtwärme `Q` während des Ticks und
`r = (V_old/V_new)^(gamma-1)`:

```text
U_after = (U_before + Q/2) r + Q/2
adiabatic_work = (U_before + Q/2) (1-r)
crank_work = adiabatic_work - back_pressure * (V_new - V_old)
```

Das diskrete Druckdrehmoment verwendet dieselbe Arbeit, sodass Gasenergie, chemische Energie und
Kurbelarbeit übereinstimmen. Kraftstoff wird erst verbraucht, nachdem die Lösung im Kandidatenzustand gelingt.
Der Gastransport verwendet weiter symmetrische Halbschritte um Kurbelarbeit und Reaktion. Die Wandtemperatur
bleibt über den äußeren Tick fest; die Wandkopplung ist erster Ordnung.

Für einen aktivierten Abbrand müssen Winkelweg und Weg aus der Endpunktdrehzahl je Tick innerhalb von
`min(0.25 rad, duration_angle/32)` bleiben, mit einer entsprechenden Schutzschranke der Winkelauflösung von binary64.
Freigesetzte Wärme darf in einem Tick 25% der thermischen Energie vor dem Abbrand nicht überschreiten. Das sind Schranken
für zugelassene Arbeit und Auflösung, keine Genauigkeitsgarantien. Sie gelten neben den Grenzen der Gasteilschritte
und der Zylinderiteration. Bei `numerical_failure` `step_ns` verringern, geplante Ereignisse
auf den neuen Tick ausrichten und Modell sowie Sitzung neu erzeugen. Fehlgeschlagene oder abgebrochene
Aufrufe übernehmen keinen Zustand, keine Eingabe, keine Front, keine chemische Bilanz und keinen Wiedergabezeiger.

## Ausgaben, Kompatibilität und Nachweis

Vormischknoten ergänzen die KPI-Felder `fuel_mass`, `fresh_air_mass`, `product_mass` und `chemical_energy`.
Eine Abbrandkomponente ergänzt kumulative `fuel_burned` (kg) und `heat_released` (J).
Das KPI-Feld `burn_frontier` stellt seinen größten besuchten Kurbelwinkel bereit (Kanalgröße
`burn_frontier_angle`, rad), sodass unterdrückter Abbrand nach einer Umkehr geprüft werden kann.
Globale Kanäle ergänzen chemische Energie, Nettozufuhr der Kraftstoffenergie, Kraftstoffresiduum und Frischluftresiduum.
Die von der Erkennung gelieferten Kanalgrößen sind maßgeblich; die Kraftstoffmasse eines Knotens heißt zum Beispiel
`unburned_fuel_mass`. Gewöhnliche Ausgaben der inneren Gasenergie und des Stroms behalten
ihre thermische Bedeutung und die Bedeutung des vorzeichenbehafteten Stroms.

Vormischmodelle ergänzen den Fingerabdruck-Tag 7 und normierte Reaktions- und Zusammensetzungsparameter.
Frühere nicht reagierende Fingerabdrücke und das Vorschreiten bleiben unverändert. Asset v6 ergänzt Zusammensetzung,
Reservoiranteile und Abbranddatensätze; authentische Fixtures v1–v5 behalten die Kompatibilität. Die
neuen Genauigkeitsbezeichnungen sind `premixed_gas_transport` und `premixed_wiebe_combustion`.

Prüfungen decken analytischen Kraftstoff- und Luftverbrauch sowie die Temperatur im geschlossenen Behälter, limitierende
Reaktanten, Reservoirtransfer vorwärts und rückwärts, Konstituentenerhaltung im geschlossenen Netz,
Konvergenz unabhängiger reagierender Differentialgleichungen von Kurbel und Gas, angehaltenen, umgekehrten und deaktivierten Abbrand,
fehlerhafte Verträge, Batch-Rollback, Abbruch, Verzweigungen und keine Allokationen bei Vorschreiten und Momentaufnahme ab. Das [Laboratorium des gezündeten Zylinders](../assets/labs/fired-cylinder.power.json)
treibt eine Last durch wiederholte Phasen Einlass/Verdichtung/Abbrand/Expansion/Auslass und
replayt an allen 63 Berichtsgrenzen von JSON, CLI, MCP und Asset identisch. Numerischer Nachweis
und der tatsächliche Ausführungsumfang sind in [VALIDATION.de.md](VALIDATION.de.md) festgehalten.

[Canteras Gleichungen des idealen Gasreaktors](https://www.cantera.org/stable/reference/reactors/ideal-gas-reactor.html)
liefern den Kontrollvolumen-Kontext für Masse, Spezies und Energie. Das
[Ansys-Beispiel eines SI-Motors](https://chemkin.docs.pyansys.com/version/stable/examples/advanced/SI_engine_optimization.html)
verwendet explizite Abbrandsteuerzeit und Wiebe-Parameter. Diese Belege motivieren die Verträge;
ihre detaillierte Chemie, Zweizonenmodelle und Beispielparameter werden nicht kopiert und
nicht als Verifikation dieses Lösers mit konstanten Eigenschaften beansprucht. Es gibt keine Laufzeitabhängigkeit
von einem der beiden Pakete. Alle Beispielparameter bleiben `unverified`.
