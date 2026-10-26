# Note sulla ripresa dello sviluppo del motore

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · **Italiano** · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto di ripresa

La pompa del dominio carburante alimenta ora ago fisico e scarico a titolo congelato in un ricevitore acceso con volume liquido finito. Gli inventari comprendono cambio fase, lavoro di spostamento, energia cinetica del getto e impulso sulla parete ferma.

[Pompa carburante a cambio di fase azionata da albero](FUEL_CHAMBER_PUMP.it.md)

`LiquidColumn` è un riferimento C# autonomo per tubo orizzontale rigido con sezione, velocità uniforme, densità liquida e temperatura costanti. Le pressioni assolute sono prescritte nell'intervallo. Resistenze lineari e quadratiche sono input di ricerca espliciti. Non è un componente del grafo e non cambia rete o ugello. [Riferimento della quantità di moto all'ingresso liquido](LIQUID_COLUMN.it.md)

Un `fuel_inertial_pipe` collega due nodi finiti di carburante e una parete termica finita. La linea orizzontale rigida possiede massa liquida, energia calorica e cinetica proprie oltre ai serbatoi. Densità costante, temperatura variabile e miscelata. Porte e linea restano liquide; i collegamenti quasi statici mantengono il modello bifase separato. [Quantità di moto della linea finita di carburante liquido](FUEL_INERTIAL_PIPE.it.md)

Un `equilibrium_fuel_duct` trasporta massa, quantità di moto ed energia totale attraverso celle esplicite `compressible_fuel`. Il liquido è comprimibile e il vapore ideale; le due fasi condividono pressione, temperatura e velocità. L’equilibrio locale permette condensazione e vaporizzazione istantanea. Due serbatoi finiti fermi e una parete termica finita chiudono i bilanci. [Condotti di combustibile comprimibile in equilibrio](EQUILIBRIUM_FUEL_DUCT.it.md)

Una `compressible_fuel_chamber` collega un albero a un serbatoio finito `compressible_fuel`. Il movimento cambia il volume; lo stesso lavoro di pressione cambia energia del fluido e coppia. Il serbatoio può alimentare o ricevere un `equilibrium_fuel_duct`, con vaporizzazione e condensazione all’equilibrio. [Camere di combustibile comprimibile azionate da albero](COMPRESSIBLE_FUEL_CHAMBER.it.md)

## Prossimo incremento

Aggiungere confini compatibili di valvola direzionale e ago/ugello comprimibili con entalpia, energia cinetica e ipotesi di fase esplicite. Proseguire con rilassamento/scorrimento, accensione, aspirazione/scarico, perdite meccaniche e coordinamento ECU/TCU. Calibrazione misurata e accettazione Unity effettiva restano separate. [Condotti di combustibile comprimibile in equilibrio](EQUILIBRIUM_FUEL_DUCT.it.md)

## Accettazione

Richiedere riferimenti analitici/limite indipendenti, bilanci completi massa/energia e raffinamento appropriato. Conservare rollback del lotto, annullamento, rami, canali stabili, replay portabile e vecchi lettori. Continuare poi accensione, aspirazione/scarico, perdite meccaniche, trasmissione e coordinamento ECU/TCU.

I parametri restano `unverified`. Rispettare gli intervalli dichiarati di temperatura e pressione, entalpia latente positiva, volumi di coesistenza separati e vapore diluito. Il condotto è rigido, orizzontale, a sezione costante e con equilibrio omogeneo istantaneo. Esclude nucleazione a velocità finita, scorrimento tra fasi, tensione superficiale, gravità, elasticità e comportamento critico reale. La diffusione di primo ordine può allargare i fronti; conservazione e riproduzione non dimostrano accuratezza spaziale o validità hardware misurata. L’accettazione Unity effettiva è ancora aperta.

[DEVELOPMENT_STATUS.it.md](DEVELOPMENT_STATUS.it.md) · [ROADMAP.it.md](ROADMAP.it.md) · [VALIDATION.it.md](VALIDATION.it.md)

[Scarico carburante a titolo congelato e ricevitori finiti](FUEL_DISCHARGE.it.md)
