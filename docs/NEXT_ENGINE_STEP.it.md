# Note sulla ripresa dello sviluppo del motore

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · **Italiano** · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto di ripresa

Il ricevitore supporta equilibrio reversibile e controlli di saturazione dell'alimentazione.

[Equilibrio di fase del carburante e limiti di alimentazione](FUEL_PHASE_EQUILIBRIUM.it.md)

## Prossimo incremento

Risolvere poi trasferimento vapore del serbatoio e flusso bifase pompa/rail con proprietà esplicite e accoppiamento volume/lavoro conservativo. Conservare mancanti le misure OEM assenti. Accensione, aspirazione/scarico, perdite meccaniche e coordinamento ECU/TCU restano lavori distinti.

## Accettazione

Richiedere riferimenti analitici/limite indipendenti, bilanci completi massa/energia e raffinamento appropriato. Conservare rollback del lotto, annullamento, rami, canali stabili, replay portabile e vecchi lettori. Continuare poi accensione, aspirazione/scarico, perdite meccaniche, trasmissione e coordinamento ECU/TCU.

Parametri `unverified`. Non sono fornite proprietà misurate della benzina, cinetica multicomponente o a velocità finita, spostamento liquido/spray risolti o calibrazione OEM. L'accettazione reale Unity Editor/Play/Player/IL2CPP resta in attesa.

[DEVELOPMENT_STATUS.it.md](DEVELOPMENT_STATUS.it.md) · [ROADMAP.it.md](ROADMAP.it.md) · [VALIDATION.it.md](VALIDATION.it.md)
