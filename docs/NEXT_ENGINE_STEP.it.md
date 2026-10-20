# Note sulla ripresa dello sviluppo del motore

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · **Italiano** · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto di ripresa

Il serbatoio risolve vapore a volume finito con potenziali corretti e aspirazione liquida separata.

[Equilibrio del serbatoio con volume liquido finito](TANK_PHASE_EQUILIBRIUM.it.md)

## Prossimo incremento

Risolvere poi flusso bifase e perdite locali con proprietà esplicite e accoppiamento conservativo di volume, quantità di moto e lavoro. Conservare mancanti le misure OEM assenti. Continuare separatamente accensione, aspirazione/scarico, perdite meccaniche e ECU/TCU.

## Accettazione

Richiedere riferimenti analitici/limite indipendenti, bilanci completi massa/energia e raffinamento appropriato. Conservare rollback del lotto, annullamento, rami, canali stabili, replay portabile e vecchi lettori. Continuare poi accensione, aspirazione/scarico, perdite meccaniche, trasmissione e coordinamento ECU/TCU.

Casi accoppiati pompa/calore/sfiato mostrano il primo ordine. Entrambe le fasi sorgente restano nell'intervallo dichiarato e la densità totale sotto quella liquida con margine binary64. Densità/proprietà costanti, equilibrio istantaneo e interfaccia piana sono approssimazioni di ricerca. Miscele reali, tensione superficiale, sloshing, cinetica finita, calibrazione misurata e Unity Editor/Play/Player/IL2CPP reale restano non verificati.

[DEVELOPMENT_STATUS.it.md](DEVELOPMENT_STATUS.it.md) · [ROADMAP.it.md](ROADMAP.it.md) · [VALIDATION.it.md](VALIDATION.it.md)
