# Note sulla ripresa dello sviluppo del motore

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · **Italiano** · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto di ripresa

La pompa del dominio carburante alimenta ora ago fisico e scarico a titolo congelato in un ricevitore acceso con volume liquido finito. Gli inventari comprendono cambio fase, lavoro di spostamento, energia cinetica del getto e impulso sulla parete ferma.

[Pompa carburante a cambio di fase azionata da albero](FUEL_CHAMBER_PUMP.it.md)

`LiquidColumn` è un riferimento C# autonomo per tubo orizzontale rigido con sezione, velocità uniforme, densità liquida e temperatura costanti. Le pressioni assolute sono prescritte nell'intervallo. Resistenze lineari e quadratiche sono input di ricerca espliciti. Non è un componente del grafo e non cambia rete o ugello. [Riferimento della quantità di moto all'ingresso liquido](LIQUID_COLUMN.it.md)

Un `fuel_inertial_pipe` collega due nodi finiti di carburante e una parete termica finita. La linea orizzontale rigida possiede massa liquida, energia calorica e cinetica proprie oltre ai serbatoi. Densità costante, temperatura variabile e miscelata. Porte e linea restano liquide; i collegamenti quasi statici mantengono il modello bifase separato. [Quantità di moto della linea finita di carburante liquido](FUEL_INERTIAL_PIPE.it.md)

Un `equilibrium_fuel_duct` trasporta massa, quantità di moto ed energia totale attraverso celle esplicite `compressible_fuel`. Il liquido è comprimibile e il vapore ideale; le due fasi condividono pressione, temperatura e velocità. L’equilibrio locale permette condensazione e vaporizzazione istantanea. Due serbatoi finiti fermi e una parete termica finita chiudono i bilanci. [Condotti di combustibile comprimibile in equilibrio](EQUILIBRIUM_FUEL_DUCT.it.md)

Una `compressible_fuel_chamber` collega un albero a un serbatoio finito `compressible_fuel`. Il movimento cambia il volume; lo stesso lavoro di pressione cambia energia del fluido e coppia. Il serbatoio può alimentare o ricevere un `equilibrium_fuel_duct`, con vaporizzazione e condensazione all’equilibrio. [Camere di combustibile comprimibile azionate da albero](COMPRESSIBLE_FUEL_CHAMBER.it.md)

Un `compressible_fuel_nozzle` porta flusso unidirezionale tra serbatoi finiti `compressible_fuel`, incluse camere azionate da albero. Compressione liquida e vapore ideale condividono il materiale. Dichiarare `phase_mode` come `frozen`: il titolo massico resta costante nel transito, mentre i serbatoi si riequilibrano. [Ugelli di combustibile comprimibile a titolo congelato](COMPRESSIBLE_FUEL_NOZZLE.it.md)

Impostare `phase_mode` di `compressible_fuel_nozzle` su `equilibrium` per l’equilibrio omogeneo istantaneo durante il transito. Liquido e vapore condividono pressione, temperatura e velocità; il titolo cambia lungo l’isentropica. Ugello e serbatoi finiti usano lo stesso potenziale liquido comprimibile e riferimento di vapore ideale. `frozen` resta un’ipotesi distinta supportata. [Transito di combustibile all’equilibrio negli ugelli](EQUILIBRIUM_FUEL_NOZZLE.it.md)

Un nodo `relaxing_fuel` ha una frazione massica di vapore indipendente. Liquido e vapore condividono pressione, temperatura e velocità, mentre l’equilibrio chimico richiede un tempo finito dichiarato. Un `relaxing_fuel_duct` trasporta massa, quantità di moto, energia totale e massa di vapore in celle esplicite. La permanenza dipende da geometria e velocità effettiva. [Rilassamento di fase del combustibile a velocità finita](RELAXING_FUEL.it.md)

## Prossimo incremento

Estendere le condizioni al contorno compatibili con velocità finita e ottenere misure di rilassamento del materiale, poi aggiungere spray risolto e ricevitori in combustione. Continuare accensione, aspirazione/scarico, perdite meccaniche, attuazione della trasmissione e coordinamento ECU/TCU. Calibrazione misurata e accettazione reale Unity restano separate. [Condotti di combustibile comprimibile in equilibrio](EQUILIBRIUM_FUEL_DUCT.it.md)

## Accettazione

Richiedere riferimenti analitici/limite indipendenti, bilanci completi massa/energia e raffinamento appropriato. Conservare rollback del lotto, annullamento, rami, canali stabili, replay portabile e vecchi lettori. Continuare poi accensione, aspirazione/scarico, perdite meccaniche, trasmissione e coordinamento ECU/TCU.

I parametri restano `unverified`. Dichiarare `initial_vapor_mass_fraction` in [0,1] e un tempo intero da 1 ns a un’ora. Rispettare temperatura e pressione positiva del materiale. Si assumono condotto rigido orizzontale a sezione costante, pseudofluido puro, dilatazione termica liquida nulla e vapore ideale. Mancano scorrimento fra le fasi, ritardo termico interfacciale, tensione superficiale, spray e gas vettore. Restano aperti gli accoppiamenti diretti del dominio rilassante con pompe, ugelli, camere mobili e ricevitori in combustione. Conservazione, riproduzione e raffinamento non attestano validità hardware misurata né accettazione reale Unity. Usare vapore diluito lontano dalla regione critica reale; il materiale deve mantenere entalpia latente positiva e volumi specifici di coesistenza distinti.

[DEVELOPMENT_STATUS.it.md](DEVELOPMENT_STATUS.it.md) · [ROADMAP.it.md](ROADMAP.it.md) · [VALIDATION.it.md](VALIDATION.it.md)

[Scarico carburante a titolo congelato e ricevitori finiti](FUEL_DISCHARGE.it.md)
