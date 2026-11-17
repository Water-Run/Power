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

Un `compressible_fuel_chamber` può controllare il volume di un serbatoio `relaxing_fuel`. Il lavoro reversibile dell’albero mantiene fissa la frazione di vapore; il rilassamento chimico è una sorgente separata che conserva energia. Un `compressible_fuel_nozzle` collega due serbatoi rilassanti con `phase_mode` su `frozen`, trasportando massa totale, vapore ed entalpia di ristagno. Un ago fisico con bobina, molla e battuta può dosare l’apertura. [Pompe a combustibile con rilassamento e dosaggio congelato](RELAXING_FUEL_BOUNDARIES.it.md)

## Prossimo incremento

Risolvere la permanenza finita nell’ugello con prove sul materiale, poi aggiungere spray risolto e ricevitori in combustione compatibili. Continuare accensione, aspirazione/scarico, perdite meccaniche, attuazione trasmissione e coordinamento ECU/TCU. Calibrazione misurata e accettazione Unity restano separate. [Condotti di combustibile comprimibile in equilibrio](EQUILIBRIUM_FUEL_DUCT.it.md)

## Accettazione

Richiedere riferimenti analitici/limite indipendenti, bilanci completi massa/energia e raffinamento appropriato. Conservare rollback del lotto, annullamento, rami, canali stabili, replay portabile e vecchi lettori. Continuare poi accensione, aspirazione/scarico, perdite meccaniche, trasmissione e coordinamento ECU/TCU.

I parametri restano `unverified`. Entrambe le porte richiedono stesso dominio e materiale; quelle rilassanti rifiutano il transito di equilibrio. Il rilassamento finito nei serbatoi non risolve chimica o permanenza dentro un ugello senza volume. Mancano scorrimento delle fasi, tensione liquida negativa, barriera di nucleazione, spray, gas vettore e ricevitore in combustione. I serbatoi mobili sono miscelati con velocità media nulla; le celle interne restano rigide con un proprietario. Usare vapore ideale diluito lontano dal critico, limiti dichiarati di pressione positiva e temperatura, entalpia latente positiva e volumi di coesistenza separati. La frazione iniziale è un ingresso di ricerca esplicito. L’accordo numerico non attesta calibrazione misurata né accettazione Unity reale.

[DEVELOPMENT_STATUS.it.md](DEVELOPMENT_STATUS.it.md) · [ROADMAP.it.md](ROADMAP.it.md) · [VALIDATION.it.md](VALIDATION.it.md)

[Scarico carburante a titolo congelato e ricevitori finiti](FUEL_DISCHARGE.it.md)
