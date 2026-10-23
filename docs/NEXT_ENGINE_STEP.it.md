# Note sulla ripresa dello sviluppo del motore

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · **Italiano** · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto di ripresa

La pompa del dominio carburante alimenta ora ago fisico e scarico a titolo congelato in un ricevitore acceso con volume liquido finito. Gli inventari comprendono cambio fase, lavoro di spostamento, energia cinetica del getto e impulso sulla parete ferma.

[Pompa carburante a cambio di fase azionata da albero](FUEL_CHAMBER_PUMP.it.md)

`LiquidColumn` è un riferimento C# autonomo per tubo orizzontale rigido con sezione, velocità uniforme, densità liquida e temperatura costanti. Le pressioni assolute sono prescritte nell'intervallo. Resistenze lineari e quadratiche sono input di ricerca espliciti. Non è un componente del grafo e non cambia rete o ugello. [Riferimento della quantità di moto all'ingresso liquido](LIQUID_COLUMN.it.md)

Un `fuel_inertial_pipe` collega due nodi finiti di carburante e una parete termica finita. La linea orizzontale rigida possiede massa liquida, energia calorica e cinetica proprie oltre ai serbatoi. Densità costante, temperatura variabile e miscelata. Porte e linea restano liquide; i collegamenti quasi statici mantengono il modello bifase separato. [Quantità di moto della linea finita di carburante liquido](FUEL_INERTIAL_PIPE.it.md)

## Prossimo incremento

Estendere il moto a densità variabile e cambio fase con chiusura energetica/entropica compatibile prima di dichiarare ingresso cavitante. Proseguire rilassamento/scorrimento, accensione, aspirazione/scarico, perdite, trasmissione ed ECU/TCU. Calibrazione misurata e Unity reale sono separati. [Quantità di moto della linea finita di carburante liquido](FUEL_INERTIAL_PIPE.it.md)

## Accettazione

Richiedere riferimenti analitici/limite indipendenti, bilanci completi massa/energia e raffinamento appropriato. Conservare rollback del lotto, annullamento, rami, canali stabili, replay portabile e vecchi lettori. Continuare poi accensione, aspirazione/scarico, perdite meccaniche, trasmissione e coordinamento ECU/TCU.

I parametri restano `unverified`. Valgono temperature dichiarate, volume gas positivo e dominio di vapore diluito. Mancano inerzia locale d'ingresso, nucleazione finita, scorrimento delle fasi, spray risolto, criticità reale e calibrazione misurata. La combustione prescritta non prova equilibrio chimico o entropia di reazione. Proseguire ingresso, accensione, aspirazione/scarico, perdite e ECU/TCU coordinati.

[DEVELOPMENT_STATUS.it.md](DEVELOPMENT_STATUS.it.md) · [ROADMAP.it.md](ROADMAP.it.md) · [VALIDATION.it.md](VALIDATION.it.md)

[Scarico carburante a titolo congelato e ricevitori finiti](FUEL_DISCHARGE.it.md)
