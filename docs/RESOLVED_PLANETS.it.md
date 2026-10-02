# Moto risolto dei satelliti Ravigneaux

[English](RESOLVED_PLANETS.md) · [简体中文](RESOLVED_PLANETS.zh-CN.md) · [Français](RESOLVED_PLANETS.fr.md) · [Русский](RESOLVED_PLANETS.ru.md) · [日本語](RESOLVED_PLANETS.ja.md) · [한국어](RESOLVED_PLANETS.ko.md) · [Deutsch](RESOLVED_PLANETS.de.md) · [Español](RESOLVED_PLANETS.es.md) · **Italiano** · [Português](RESOLVED_PLANETS.pt-BR.md)

L'assemblaggio risolto include la rotazione assoluta di entrambi gli insiemi interni di satelliti e la loro inerzia di massa orbitale attorno al portasatelliti. Quattro vincoli fisici di ingranamento collegano sei rotori. Le cinque frizioni o freni di gamma e il convertitore esterno restano componenti ordinari. La riduzione a quattro membri è ancora disponibile come semplificazione dichiarata separata; non fornisce evidenza della rotazione dei satelliti.

La connettività di ingranamento e le relazioni di passo primitivo hanno un [riferimento strutturale](https://www.mathworks.com/help/sdl/ref/ravigneauxgear.html) separato. Power! deriva e implementa il proprio grafo di rotori conservativo e controlli indipendenti della matrice di massa. Non è inclusa alcuna implementazione o pacchetto di modello del fornitore.

## Geometria ed energia

Per raggio primitivo della corona `R` e rapporti del solare grande e piccolo `kL` e `kS`, la geometria primitiva rigida è:

```text
large sun radius = R/kL
small sun radius = R/kS
outer planet radius = (R-large sun radius)/2
inner planet radius = (large sun radius-small sun radius)/2
outer orbit radius = (R+large sun radius)/2
inner orbit radius = (large sun radius+small sun radius)/2
```

`RavigneauxPlanetParameters` richiede un raggio SI della corona, masse e inerzie di rotazione positive per satellite, e 1..32 coppie di satelliti uguali e sincrone. La spaziatura pari deve contenere entrambi gli insiemi senza sovrapposizione delle circonferenze primitive. Geometria e inerzia aggregata devono restare rappresentabili. Tutti questi input sono proprietà di ricerca esplicite; l'ausilio non fornisce valori misurati.

Per `n` coppie uguali, il portasatelliti riceve l'inerzia orbitale `n (mInner orbitInner^2 + mOuter orbitOuter^2)`. Il parametro `CarrierInertia` esistente è l'inerzia di struttura del portasatelliti in questo percorso risolto. Ogni nuovo rotore ha `n` volte la sua inerzia di rotazione per satellite. Le loro velocità sono velocità angolari assolute, quindi l'energia cinetica è l'ordinaria `J omega^2/2`; la corotazione conserva l'energia di rotazione dei satelliti. Usare la rotazione relativa con questo accumulo diagonale ometterebbe l'accoppiamento del portasatelliti.

## Contratto di ingranamento

`carrier_gear` impone `A - ratio B + (ratio-1) C = 0`, dove C è il portasatelliti che si muove davvero. Gli ingranamenti esterni usano un rapporto negativo dei raggi primitivi; l'ingranamento interno corona/satellite esterno usa un rapporto positivo. Sono supportati rapporti con segno finiti e non nulli, uno compreso. Servono tre porte rotazionali distinte e velocità iniziali compatibili.

I quattro ingranamenti sono solare grande/satellite esterno, solare piccolo/satellite interno, corona/satellite esterno e satellite interno/satellite esterno. Tutte e tre le coppie di reazione entrano nella stessa proiezione al punto medio e hanno potenza di porta sommata nulla e somma delle coppie nulla. La reazione del portasatelliti non è inviata in silenzio alla massa ferma. Un affinamento limitato del residuo relativo migliora le risposte a forze piccole. La risoluzione al punto medio impone residuo di velocità nullo all'estremo successivo, evitando la riflessione ripetuta dell'arrotondamento precedente. Entrambe le operazioni usano le risposte reali di forza dei vincoli e conservano i loro moltiplicatori di correzione nelle storie reali delle reazioni. Lo scratch appartiene a ogni simulazione; i fattori compilati restano immutabili. Righe normalizzate, coordinate compensate e storie complete delle reazioni conservano fase, fork, annullamento e rollback del batch.

Il riferimento libero indipendente usa le coordinate di corona e portasatelliti. Con `aOuter = R/outerRadius` e `aInner = R/innerRadius`:

```text
outer planet speed = aOuter ring + (1-aOuter) carrier
inner planet speed = -aInner ring + (1+aInner) carrier
M = sum over rotors of J [ring coefficient, carrier coefficient]^T
                         [ring coefficient, carrier coefficient]
```

Questo include entrambe le energie di rotazione e l'inerzia orbitale aggiunta a parte. Carichi generalizzati indipendenti, inerzie riflesse per tutti i percorsi avanti e di retromarcia, quantità di moto angolare e impulso e calore di cattura del portasatelliti controllano la risoluzione assemblata.

## Grafo condiviso ed evidenze

`CreateResolvedGraph` riceve le porte originali, quattro ID distinti di nodo e ingranamento dei satelliti, e le proprietà dei satelliti dichiarate. Restituisce definizioni ordinarie immutabili: sei rotori interni, quattro ingranamenti di portasatelliti, una riduzione finale e cinque elementi di attrito. Il JSON piatto conserva le inerzie totali dei rotori e i rapporti di ingranamento con segno; la descrizione dell'esempio registra la geometria generatrice e le proprietà per satellite. I digest dei sorgenti conservano quell'evidenza di authoring dichiarata.

`resolved-ravigneaux-transmission` esercita tutti i passaggi avanti in salita e in discesa. `fired-resolved-ravigneaux-converter` aggiunge il motore, le mappe con segno del convertitore e il blocco. Entrambi dichiarano tre coppie, R=0.1 m, masse interno/esterno 0.3/1 kg e inerzie di rotazione per satellite 0.000015/0.0005 kg m2. La struttura del portasatelliti è 0.03 kg m2; l'aggiunta esplicita di orbita è 0.0184375 kg m2. Sono ingressi di ricerca.

L'asset portabile v24 conserva l'ingranamento con segno del portasatelliti e legge le versioni precedenti. Il primitivo aggiunge il tag di impronta 28; i grafi precedenti conservano impronte e replay. I marcatori Studio preparati identificano tutte e tre le porte di ingranamento. La verifica reale di Unity Editor, Play, Player e IL2CPP resta separata. Esegui la verifica in serie richiesta `dotnet run --file tools/Build.cs -- verify`; esiti numerici e ambito stanno in [VALIDATION.md](VALIDATION.it.md).

## Comportamento restante

Insiemi di satelliti uguali, rigidi e sincroni non modellano cedevolezza dei denti, ripartizione del carico di fabbricazione, gioco, perdite di ingranamento, lubrificazione o proprietà dipendenti dalla temperatura. È disponibile l'[azionamento a stantuffo idraulico alimentato da pompa](AT_HYDRAULIC_ACTUATION.it.md). Controllo completo del cambio, coordinamento della ECU e geometria e mappe OEM misurate restano incompiuti. L'assemblaggio generico non dimostra l'identità PSA AT8/AL4. I confini dei campioni e le misure mancanti restano intatti.
