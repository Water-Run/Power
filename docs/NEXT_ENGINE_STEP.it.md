# Note sulla ripresa dello sviluppo del motore

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · **Italiano** · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto di ripresa

I recipienti carburante scambiano miscele liquido/vapore attraverso restrizioni dissipative conservative e collegamenti termici finiti. Riempimento pompa, albero e scarico iniettore restano da collegare.

[Rete di trasporto del carburante in equilibrio](EQUILIBRIUM_FUEL_NETWORK.it.md)

## Prossimo incremento

Collegare i volumi carburante a riempimento pompa da albero e scarico fisico iniettore con materia, quantità di moto e lavoro conservativi. Risolvere inerzia locale d’ingresso e flusso critico con riferimenti indipendenti. Proseguire accensione, aspirazione/scarico, perdite, attuazione trasmissione ed ECU/TCU senza considerare questa rete un gruppo motopropulsore completo.

## Accettazione

Richiedere riferimenti analitici/limite indipendenti, bilanci completi massa/energia e raffinamento appropriato. Conservare rollback del lotto, annullamento, rami, canali stabili, replay portabile e vecchi lettori. Continuare poi accensione, aspirazione/scarico, perdite meccaniche, trasmissione e coordinamento ECU/TCU.

Restano i limiti del materiale e della deformazione rappresentabile della chiusura. I parametri restano `unverified`. Riempimento pompa e lavoro d’albero, collegamento iniettore, inerzia locale, flusso critico, ritardo di nucleazione, scorrimento e misure di miscele reali rimangono separati. I rail iniettore alimentati da pompa mantengono il percorso liquido separato fino all’accoppiamento.

[DEVELOPMENT_STATUS.it.md](DEVELOPMENT_STATUS.it.md) · [ROADMAP.it.md](ROADMAP.it.md) · [VALIDATION.it.md](VALIDATION.it.md)
