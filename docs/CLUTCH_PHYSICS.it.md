# Fisica della frizione a secco gestita

[English](CLUTCH_PHYSICS.md) · [简体中文](CLUTCH_PHYSICS.zh-CN.md) · [Français](CLUTCH_PHYSICS.fr.md) · [Русский](CLUTCH_PHYSICS.ru.md) · [日本語](CLUTCH_PHYSICS.ja.md) · [한국어](CLUTCH_PHYSICS.ko.md) · [Deutsch](CLUTCH_PHYSICS.de.md) · [Español](CLUTCH_PHYSICS.es.md) · **Italiano** · [Português](CLUTCH_PHYSICS.pt-BR.md)

`Power.Core` fornisce una legge di attrito `DryClutch` immutabile e un integratore di riferimento `ClutchPair` per due inerzie sotto coppie esterne e innesto costanti. Entrambi compilano per `net10.0` e `netstandard2.1` senza dipendenze di terze parti.

Questi primitivi sono un riferimento indipendente per il [componente a grafo della frizione](CLUTCH_NETWORK.it.md), ora integrato. Il grafo accoppia alberi, motori e cilindri, supporta più frizioni e l'instradamento termico, e conserva il rollback dell'intero batch attraverso gli eventi interni. JSON, CLI/MCP, l'asset v8 e Studio usano quella definizione di grafo. La coppia autonoma documentata qui resta un riferimento a carico costante; non fa avanzare da sola una rete compilata.

## Contratto di attrito

Tutte le capacità e le reazioni sono espresse alla porta A. Il rapporto con segno `r` usa la stessa convenzione di potenza del componente albero esistente:

```text
g       = omega_A - r * omega_B                  [rad/s]
tau_B   = -r * tau_A                            [Nm]
P_heat  = -tau_A * g                            [W]
C_s     = engagement * static_capacity          [Nm]
C_k     = engagement * sliding_capacity         [Nm]
```

La frazione di innesto sta in `[0,1]`. La capacità statica è non negativa e non inferiore alla capacità strisciante. Entrambe possono essere zero. Una capacità statica efficace nulla disinnesta la frizione. Non si deducono pressione di serraggio, coefficiente di attrito, geometria dei dischi, decadimento termico, usura, trascinamento o ritardo dell'attuatore.

Per slittamento non nullo, `tau_A = -sign(g) * C_k`. A slittamento esattamente nullo il sistema che integra deve fornire la coppia richiesta per tenere nulla l'accelerazione relativa. Se il suo modulo è al più `C_s`, la frizione si blocca su quella reazione e non produce calore di attrito. Altrimenti inizia a strisciare nella direzione del carico sbilanciato, usando `C_k`. L'uguaglianza al limite statico resta in blocco. La legge non ha una banda morta di velocità e non trasforma in silenzio una piccola velocità relativa in un vincolo di aderenza.

Questa distinzione idealizzata fra attrito cinetico e reazione statica vincolata segue la meccanica descritta dai riferimenti principali: [Modelica Clutch](https://doc.modelica.org/om/Modelica.Mechanics.Rotational.Components.Clutch.html) e [MathWorks Fundamental Friction Clutch](https://www.mathworks.com/help/sdl/ref/fundamentalfrictionclutch.html). L'implementazione di Power! è scritta in modo indipendente e usa capacità di coppia esplicite; non riproduce nessuna delle due implementazioni e non ne rivendica i modelli costitutivi più ampi.

`ClutchMode` distingue `Disengaged`, `Locked`, `SlippingPositive` e `SlippingNegative`. Una modalità a velocità nulla può essere uno stato di strisciamento in partenza, quando il carico esterno supera la capacità statica. `HeatFlowWatts` è istantaneo; in una partenza di questo tipo a velocità nulla il suo valore è zero, anche se il calore successivo è positivo.

## Coppia esatta a carico costante

Per due inerzie positive `J_A`, `J_B` e coppie esterne costanti `T_A`, `T_B`:

```text
J_A * domega_A/dt = T_A + tau_A
J_B * domega_B/dt = T_B - r * tau_A
D                 = 1/J_A + r*r/J_B
b                 = T_A/J_A - r*T_B/J_B
required_tau_A    = -b/D
dg/dt             = b + D*tau_A
```

Ogni fase di strisciamento ha accelerazione costante. Se la velocità relativa raggiunge zero entro l'intervallo richiesto, il solver avanza esattamente fino a `t_zero = -g / (dg/dt)` e valuta la reazione statica. Integra poi il resto o in blocco, o in strisciamento nella direzione opposta. Una forzante costante ammette al più un arrivo di questo tipo, quindi la risoluzione richiede al più due fasi, senza ciclo di convergenza né suddivisione del tempo. Un evento esattamente all'estremo dell'intervallo restituisce la modalità di reazione del limite destro.

La traiettoria bloccata obbedisce a `omega_A = r*omega_B` con

```text
domega_B/dt = (r*T_A + T_B) / (r*r*J_A + J_B).
```

A un arrivo calcolato, una proiezione che conserva la quantità di moto rimuove il residuo di arrotondamento dell'evento in binary64. Usa pesi inerziali limitati, invece di formare grandi somme pesate sull'inerzia. Una volta in blocco, il vincolo di velocità è costruito in modo esplicito. È una correzione di arrotondamento su un evento risolto, non un innesto istantaneo anelastico di uno slittamento finito. Gli avanzamenti angolari integrano ogni fase ad accelerazione costante. Il lavoro esterno è `T_A*delta_theta_A + T_B*delta_theta_B`; il calore di attrito è l'integrale di `-tau_A*g`. Il risultato include l'impulso di coppia con segno in A e la variazione di energia cinetica verificabile in modo indipendente. Il residuo energetico è `external_work - heat - delta_kinetic`.

La coppia supporta entrambi i segni di un `r` finito e non nullo. La sua quantità di moto generalizzata `r*J_A*omega_A + J_B*omega_B` cambia solo attraverso `r*T_A + T_B`. La conservazione ordinaria della quantità di moto angolare vale quando `r = 1`; un rapporto rappresenta un trasformatore meccanico ideale, il cui supporto può reagire una coppia. Per un freno a massa si costruisce `ClutchPair.Brake(J, friction)`. La porta B ha allora velocità e coppia esterna fissate a zero, e `r = 1`. L'infinito non è usato come sentinella di inerzia.

## API e comportamento in caso di errore

```csharp
var pair = new ClutchPair(0.2, 0.8, new DryClutch(20, 10));
var status = pair.Advance(new ClutchPairState(100, 0),
    externalTorqueANewtonMeters: 0,
    externalTorqueBNewtonMeters: 0,
    engagement: 1,
    durationSeconds: 4,
    out var step);
```

L'esempio raggiunge 20 rad/s a entrambe le porte dopo 1.6 s e genera 800 J di calore. I suoi avanzamenti angolari su quattro secondi sono 144 rad e 64 rad. Tutti i numeri sono sintetici, senza pretesa di calibrazione del veicolo.

Le due classi sono immutabili. `ClutchPairState` e `ClutchPairStep` sono tipi valore. `Advance` non muta lo stato del chiamante e non alloca memoria. Chiamanti indipendenti possono condividere la stessa coppia. Non c'è una storia delle fasi conservata, né un orologio di simulazione globale; le velocità fornite e i nuovi carichi costanti determinano l'intervallo successivo.

| Stato | Significato e recupero |
|---|---|
| `Ok` | È disponibile un risultato locale finito e completo; conservazione e idoneità del modello si valutano a parte |
| `InvalidDuration` | Fornisci un intervallo finito e strettamente positivo, in secondi |
| `InvalidEngagement` | Fornisci una frazione finita in `[0,1]` |
| `InvalidState` | Fornisci velocità finite; un freno a massa richiede velocità B uguale a zero |
| `InvalidTorque` | Fornisci coppie esterne finite; un freno a massa richiede coppia B uguale a zero |
| `NumericalFailure` | Il moto derivato, il tempo dell'evento o l'energia supera l'intervallo binary64 supportato; controlla unità e scale e accorcia o riformula l'intervallo |

A ogni rifiuto l'uscita è `default`; non c'è uno stato pubblicato in parte. Parametri immutabili non validi lanciano `ArgumentException` o le sue sottoclassi alla costruzione. `DryClutch.Evaluate` rifiuta allo stesso modo input non validi o calore istantaneo in overflow. Un tempo di evento che va in underflow a zero fallisce, invece di scartare in silenzio energia cinetica relativa finita. I risultati fisici restano soggetti all'arrotondamento in virgola mobile; input finiti da soli non garantiscono quantità derivate rappresentabili.

`ZeroSlipTimeSeconds` è il primo arrivo innestato a velocità relativa nulla, oppure zero se l'intervallo inizia già lì. È null quando non esiste tale arrivo, moto disinnestato compreso. Non implica aderenza: un carico esterno grande può causare un'inversione immediata. `SlippingDurationSeconds` include le fasi di strisciamento in partenza; il moto disinnestato è escluso. `EndReaction` è istantanea nello stato finale, mentre calore, lavoro, impulso e avanzamenti angolari sono integrati sull'intervallo completo.

Il parametro locale in secondi non sostituisce l'orologio a nanosecondi fisso e limitato di `Simulation`. L'integrazione del grafo conserva gli estremi esatti dei tick e degli eventi esterni, gli hash di stato, l'indipendenza dei fork, l'annullamento e il rollback completo su più tick.

## Evidenze e limiti

[ClutchChecks.cs](../tests/Power.Tests/ClutchChecks.cs) esegue gli stessi dieci gruppi su entrambi gli assembly di destinazione di Core:

- Tempo di sincronizzazione in forma chiusa per due inerzie, velocità, avanzamenti angolari, impulso, quantità di moto ed energia cinetica perduta, con innesto pieno e parziale.
- Ripartizione esatta del carico statico, soglia di distacco inclusiva, capacità cinetica più bassa, slittamento non nullo senza banda morta e aggancio statico a capacità cinetica nulla.
- Inversione entro un intervallo e al suo estremo, più frenata a massa, tenuta e partenza sotto carico eccessivo.
- Rapporti positivi e negativi, quantità di moto generalizzata e variazioni di energia calcolate in modo indipendente. Duemila combinazioni deterministiche spaziano inerzia, rapporto, velocità, carico esterno, capacità e durata.
- Invarianza di partizione attraverso eventi ibridi, sotto forzante costante. Il campionamento al punto medio di carichi sinusoidali variabili converge verso integrali analitici indipendenti di velocità, angolo e calore. Questo dimostra il comportamento del secondo ordine di quell'esempio di campionamento del carico; il grafo ha controlli di convergenza accoppiata e ibrida propri e separati.
- Input non validi, overflow, un evento non risolvibile, uscita di default in caso di fallimento, valutazioni ripetute indipendenti e zero allocazioni su 10,000 intervalli riusciti.

La coppia restituisce il calore come energia generata; il componente del grafo lo instrada verso un nodo termico o il registro esterno. Nessuna delle due API implementa la topologia DCT/AT, la selezione delle marce, un convertitore di coppia, attuatori idraulici, il coordinamento ECU/TCU, l'identificazione del materiale della frizione o una calibrazione misurata. Quei confini restano nella [tabella di marcia](ROADMAP.it.md).
