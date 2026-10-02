# Hydraulikkolben und kontaktbetätigte Kupplung

[English](HYDRAULIC_PISTON.md) · [简体中文](HYDRAULIC_PISTON.zh-CN.md) · [Français](HYDRAULIC_PISTON.fr.md) · [Русский](HYDRAULIC_PISTON.ru.md) · [日本語](HYDRAULIC_PISTON.ja.md) · [한국어](HYDRAULIC_PISTON.ko.md) · **Deutsch** · [Español](HYDRAULIC_PISTON.es.md) · [Italiano](HYDRAULIC_PISTON.it.md) · [Português](HYDRAULIC_PISTON.pt-BR.md)

`hydraulic_piston` verbindet eine translatorische Masse mit einer vorderen Hydraulikkammer und entweder einer hinteren Kammer oder einem ausdrücklichen Gegendruckreservoir. `piston_clutch` liest die Belagkraft des Kolbens. Positiver Druck kann einen Kolben durch freien Spielraum bewegen, ohne Kupplungsdrehmoment zu übertragen.

## Gleichungen und Energie

Für Verschiebung x, Geschwindigkeit v, wirksame Vorder- und Rückflächen Af/Ab und Überdrücke pf/pb ist die Kolbenkraft `Af*pf - Ab*pb`. Vorderseitige Expansion entnimmt `Af*dx`; rückseitige Kontraktion liefert `Ab*dx`. Eine endliche Kammer speichert `C*p*p/2` Joule und `C*p` Kubikmeter Referenzbestand. Das Volumenkonto enthält das vom Kolben überstrichene Volumen `(Af-Ab)*(x-x_initial)`. Ein Rückreservoir trägt vorzeichenbehaftete äußere Arbeit `-pb*Ab*dx` und Referenzvolumen `-Ab*dx` bei.

Der Translationsknoten speichert `m*v*v/2`. Eine `linear_spring` fügt `K*(x-rest)^2/2` und den viskosen Verlust `D*v_relative^2` hinzu, mit ausdrücklicher Wärmeführung. Ihr Kanal `friction_heat` meldet kumulierte Dämpfungswärme mit kompensierter Summation. Sie ist unabhängig von gerundeten Temperaturen des Wärmeknotens.

Das einseitige Belagpotenzial ist `Kpad*max(x-contact,0)^2/2`. Unterer und oberer Anschlag fügen dasselbe quadratische Potenzial außerhalb des Nennhubs hinzu. Anschläge sind nachgiebig: Eindringen speichert Energie und erzeugt eine Rückstellkraft. Sie klemmen die Bewegung nicht. Für ein Gelenkpotenzial V nutzt die Intervallreaktion `-(V(x_next)-V(x_old))/dx`, ausgewertet über einen auslöschungsfesten diskreten Gradienten. Dadurch ist die Kontaktarbeit in den diskreten Gleichungen exakt die Potenzialänderung. Die analytische Jacobi-Matrix deckt aktive, inaktive, aktivierende und lösende Gelenke ab; an einem ruhenden Gelenk nutzt sie die gemittelte einseitige Ableitung.

Haft- und Gleitkapazitäten der Kupplung sind `mu*surfaces*radius*Npad`. Der Löser nutzt während des Intervalls die diskrete Belagkraft und für Schnappschüsse die augenblickliche Belagkraft. Reibungswärme bleibt nichtnegativ; eine verriegelte ideale Kupplung gibt keine Schlupfleistung ab. Dieselbe gemeinsame Lösung enthält Druck, Kolbenträgheit, Federdämpfung, elektrische Versorgung und die bestehenden mechanischen und Kupplungsbindungen.

## Verträge und numerische Grenzen

| Element | Erforderliche Daten |
|---|---|
| Knoten `translational` | Positive Masse in kg, Anfangsgeschwindigkeit in m/s, Lage in m |
| `hydraulic_piston` | Ein translatorischer Anschluss A, hydraulischer Vorderanschluss B; Vorder- und Rückflächen, Rückknoten oder -druck, steigende Hubgrenzen, Anschlagsteifigkeit, Kontaktlage und -steifigkeit |
| `linear_spring` | Translatorische Anschlüsse A/B oder geerdetes B; Steifigkeit N/m, Dämpfung N·s/m, Ruhelage m, optionale Wärmesenke |
| `piston_clutch` | Drehende Anschlüsse A/B oder geerdetes B, Komponenten-ID des Kolbens, Radius m, Haft- und Gleitbeiwerte, ganzzahlige Reibflächen |
| `force_source` | Translatorischer Anschluss A und äußere Krafteingabe in N |

`linear_spring.parameters.rest_angle` behält den gemeinsamen Deskriptorschlüssel, trägt aber eine Verschiebungsgröße in Metern. Ein Kolben besitzt einen gegebenen Translationsknoten; mehrere Kupplungsreibelemente dürfen seinen Belag ausdrücklich referenzieren. Eine endliche Rückkammer muss sich von der Vorderkammer unterscheiden. Ein Reservoir hat `back_node=0` und einen ausdrücklichen nichtnegativen `back_pressure`. Die Haftreibung muss mindestens die Gleitreibung sein. Flächen liegen in 1–128, und der Belagkontakt liegt innerhalb des Nennhubs.

Der akzeptierte Kolbenweg je Intervall ist auf ein Viertel des Nennhubs begrenzt. Den festen Tick verkleinern, wenn die Bewegung diese Grenze verletzt oder die gemeinsame Lösung nicht konvergiert. Negativer akzeptierter Überdruck weist den vollständigen Batch zurück; Versorgungsstrom, Nachgiebigkeit, wirksame Flächen, Trägheit und Dämpfung prüfen. Dieses Modell hat keine Kavitationsklemme. Vollständiges Rollback, Abbruch, Abzweigungen und Zustands-Hashes schließen die Verläufe von Bewegung, Druck, Reibung und Dämpfung ein.

Die Gleichungen setzen konstante wirksame Nachgiebigkeit und Flächen, eine konzentrierte bewegte Masse, lineare Rückstellfeder und -dämpfung, einen elastischen Belag und nachgiebige Enden voraus. Dichtungsreibung, Kavitation, Verformungsmoden der Platte, Verschleiß, detaillierte Reibkennfelder und OEM-Kalibrierung bleiben außerhalb dieser Implementierung.

## Prüfung und Laboratorium

Unabhängige Prüfungen decken analytische Schwingung von gekoppelter Masse, Feder und Fluid, endliche und Reservoir-Rückkammern, überstrichenes Volumen und Druckarbeit, Arbeitsidentitäten der Gelenke, analytische Kontaktableitungen und eine stückweise RK4-Kontaktreferenz ab. Glatte lineare Bewegung zeigt Verfeinerung zweiter Ordnung; Prüfungen nichtglatten Kontakts prüfen sinkenden Fehler, ohne gleichmäßige hybride Ordnung zu beanspruchen. Kupplungsprüfungen decken freie Füllung, Belagkontakt, Aufnahme, Lösung und Reibungswärme ab. Spätes numerisches Versagen, Abbruch, Unabhängigkeit der Abzweigungen, exakte Batchbildung und zuweisungsfreies Schreiten sind geprüft.

Das synthetische [Laboratorium der kolbenbetätigten Kupplung](../assets/labs/piston-actuated-clutch.power.json) nutzt eine endliche Batterie, eine tastverhältnisgeregelte elektrische Pumpe, Füll- und Entleerungsventile, einen Kolben von 20 g, 2 mm Belagfreiweg, eine Rückstellfeder von 10 kN/m und Dämpfung von 300 N·s/m. Die Dämpfung ist ein ausdrücklicher Forschungsparameter, gewählt, damit die vorgegebene Kammer während des Transients nichtnegativ bleibt. Sie ist keine OEM-Messung. Bei 15 s liegt die Belaglast bei etwa 177.28 N, mit Haft- und Gleitkapazitäten 22.69/11.35 N·m. CLI, portable Wiedergabe und echtes MCP-Replay stimmen an jeder gemeldeten Grenze überein.

Siehe [VALIDATION.de.md](VALIDATION.de.md) für numerische Schranken, getrennte Energiekonten, Leistungsmessungen und Laufzeitumfang. Asset v14 behält die vollständige Topologie. Studio-Ansichten von Schlitten und Kontakt sowie Import- und Play-Prüfungen sind vorbereitet; tatsächliche Nachweise für Unity-Editor und Player bleiben ausstehend.
