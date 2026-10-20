# Note sulla ripresa dello sviluppo del motore

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · **Italiano** · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto di ripresa

Le camere finite scambiano lavoro di pressione con alberi e riempiono/erogano attraverso restrizioni unidirezionali ideali, con cambio di fase sottoalimentato. Iniettore fisico e ricevitore restano da collegare.

[Pompa carburante a cambio di fase azionata da albero](FUEL_CHAMBER_PUMP.it.md)

## Prossimo incremento

Collegare la pompa a iniettore fisico e ricevitori cilindro/film con trasporto bifase, spostamento e lavoro cinetico espliciti. Risolvere inerzia d’ingresso e flusso critico con riferimenti indipendenti. Continuare accensione, aspirazione/scarico, perdite, attuazione trasmissione ed ECU/TCU verso l’obiettivo completo.

## Accettazione

Richiedere riferimenti analitici/limite indipendenti, bilanci completi massa/energia e raffinamento appropriato. Conservare rollback del lotto, annullamento, rami, canali stabili, replay portabile e vecchi lettori. Continuare poi accensione, aspirazione/scarico, perdite meccaniche, trasmissione e coordinamento ECU/TCU.

I parametri restano `unverified`. Limiti di temperatura, pressione e volume rappresentabile valgono per tutto il ciclo. Si assumono liquido incomprimibile, vapore ideale pseudo-specie, equilibrio istantaneo a temperatura comune e pressione esterna del carter nulla. Inerzia locale/NPSH, flusso critico, ritardo di nucleazione, inerzia/isteresi valvole, miscele reali e calibrazione misurata restano aperti. Iniettore fisico e lavoro di spostamento/cinetica del ricevitore sono separati.

[DEVELOPMENT_STATUS.it.md](DEVELOPMENT_STATUS.it.md) · [ROADMAP.it.md](ROADMAP.it.md) · [VALIDATION.it.md](VALIDATION.it.md)
