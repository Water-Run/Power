# Note sulla ripresa dello sviluppo del motore

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · **Italiano** · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto di ripresa

Il serbatoio risolve vapore a volume finito con potenziali corretti e aspirazione liquida separata.

[Equilibrio del serbatoio con volume liquido finito](TANK_PHASE_EQUILIBRIUM.it.md)

La chiusura del recipiente cedevole per carburante puro risolve liquido, vapore e coesistenza con accumulo elastico. È un riferimento costitutivo locale; il trasporto di pompa e ugello nel grafo richiede ancora integrazione.

[Riferimento del volume di carburante cedevole](COMPLIANT_FUEL_VOLUME.it.md)

## Prossimo incremento

Collegare la chiusura al trasporto bifase conservativo di pompa e rail, poi risolvere perdite locali d’ingresso, quantità di moto della miscela e scarico dell’ugello. Mantenere proprietà esplicite e pieno accoppiamento volume/lavoro. Le misure OEM mancanti restano mancanti. Proseguire separatamente accensione, aspirazione/scarico, perdite meccaniche e coordinamento ECU/TCU.

## Accettazione

Richiedere riferimenti analitici/limite indipendenti, bilanci completi massa/energia e raffinamento appropriato. Conservare rollback del lotto, annullamento, rami, canali stabili, replay portabile e vecchi lettori. Continuare poi accensione, aspirazione/scarico, perdite meccaniche, trasmissione e coordinamento ECU/TCU.

Casi accoppiati pompa/calore/sfiato mostrano il primo ordine. Entrambe le fasi sorgente restano nell'intervallo dichiarato e la densità totale sotto quella liquida con margine binary64. Densità/proprietà costanti, equilibrio istantaneo e interfaccia piana sono approssimazioni di ricerca. Miscele reali, tensione superficiale, sloshing, cinetica finita, calibrazione misurata e Unity Editor/Play/Player/IL2CPP reale restano non verificati.

[DEVELOPMENT_STATUS.it.md](DEVELOPMENT_STATUS.it.md) · [ROADMAP.it.md](ROADMAP.it.md) · [VALIDATION.it.md](VALIDATION.it.md)
