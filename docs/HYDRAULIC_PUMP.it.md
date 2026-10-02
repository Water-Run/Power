# Alimentazione idraulica comandata dall'albero

[English](HYDRAULIC_PUMP.md) · [简体中文](HYDRAULIC_PUMP.zh-CN.md) · [Français](HYDRAULIC_PUMP.fr.md) · [Русский](HYDRAULIC_PUMP.ru.md) · [日本語](HYDRAULIC_PUMP.ja.md) · [한국어](HYDRAULIC_PUMP.ko.md) · [Deutsch](HYDRAULIC_PUMP.de.md) · [Español](HYDRAULIC_PUMP.es.md) · **Italiano** · [Português](HYDRAULIC_PUMP.pt-BR.md)

Il grafo gestito supporta una pompa a cilindrata ideale e reversibile e uno scarico di pressione unidirezionale quasi stazionario. Il [laboratorio fired-pump](../assets/labs/fired-pump.power.json) collega l'albero a gomiti a una linea di alimentazione cedevole, a valvole di cambio e a frizioni comandate dalla pressione. I suoi parametri sono sintetici e `unverified`.

## Equazioni e potenza

La cilindrata `D > 0` è in m³/rad. Una velocità positiva dell'albero eroga volume di riferimento dall'ingresso all'uscita:

```
Q = D * omega
shaft reaction = -D * (p_out - p_in)
shaft-to-fluid power = Q * (p_out - p_in) = -shaft reaction * omega
```

Flusso inverso e funzionamento come motore idraulico sono ammessi. Non si deducono una valvola di non ritorno, trafilamento, attrito o una mappa di rendimento. L'inerzia appartiene al nodo albero esplicito. Un ingresso finito perde esattamente il volume erogato all'uscita. Un ingresso a serbatoio contribuisce `p_in * Q` al lavoro idraulico esterno; il lavoro dall'albero al fluido è un trasferimento interno e non si aggiunge al lavoro globale di sorgente.

Lo scarico usa una caratteristica lineare esplicita dell'eccesso di pressione:

```
Q_A_to_B = G * max(p_A - p_B - p_crack, 0)
heat = Q_A_to_B * (p_A - p_B)
```

`G >= 0` ha unità m³/(s·Pa), e `p_crack >= 0` è una pressione differenziale. Sotto la soglia sigilla in modo esatto. Una portata finita richiede sovrapressione; la pressione non viene mai saturata al valore impostato. È un'approssimazione costitutiva, non la meccanica del cursore né una curva adattata dell'area di apertura della valvola. L'intera caduta di pressione genera calore, parte alla pressione di cracking compresa.

Le equazioni della pompa ideale seguono il limite a perdite nulle della [descrizione MathWorks della pompa a cilindrata fissa](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html). Il comportamento di soglia è coerente con la [descrizione della valvola di scarico di pressione](https://www.mathworks.com/help/hydro/ref/pressurereliefvalveil.html); la legge lineare di eccesso di pressione di Power! è una scelta di modellazione esplicita e più semplice. Questi riferimenti forniscono equazioni e ambito, non misure di parametri OEM né codice sorgente.

## Contratti condivisi

`hydraulic_pump` richiede un `node_a` rotazionale, un'uscita idraulica `node_b` e `parameters.inlet_node` (zero seleziona il serbatoio). L'ingresso deve differire dall'uscita. I parametri includono `displacement` positivo in `m3_rad`, più `reservoir_pressure` esplicita in `pa` o `bar` solo quando l'ingresso è zero. Non ha input, pozzo di calore né `node_c` planetario.

`hydraulic_relief` usa un `node_a` idraulico, un `node_b` idraulico facoltativo (zero o omesso seleziona un serbatoio), un `heat_node` termico facoltativo e i parametri `coefficient`, `cracking_pressure` e `reservoir_pressure` solo per il serbatoio. Non ha un input di apertura. Le programmazioni delle valvole usano ancora restrizioni comandate separate.

Le uscite della pompa sono la media dell'ultimo tick di `volume_flow`, la reazione d'albero `torque`, `hydraulic_power` con segno e `hydraulic_work` cumulativo con segno. Le storie iniziali sono zero; i cambi di input lasciano invariate le medie accettate. Il `hydraulic_work` globale resta il lavoro esterno di serbatoio. Le uscite dello scarico riusano la portata della restrizione, la potenza termica media e il calore di fluido cumulativo. Tutti gli input, le storie e i termini di compensazione partecipano a fork, hash, annullamento e rollback dell'intero batch. L'asset v11 conserva le nuove definizioni e tutti i lettori v1–v10. L'agente 0.13.0 dichiara `shaft_driven_hydraulics` e `fired-pump`.

## Evidenza numerica e limiti

Velocità della pompa, pressioni delle camere, velocità delle porte del convertitore e lavoro del cilindro condividono un sistema di Newton, usando le risposte meccaniche proiettate dagli ingranaggi. Le capacità delle frizioni a pressione si aggiornano dentro l'iterazione di vincolo limitata. I trasferimenti di fluido accettati aggiornano entrambe le porte e il registro del volume di riferimento. Il percorso idraulico indipendente esistente è conservato per i modelli senza pompe, così restano gli hash di replay precedenti.

La risoluzione di Newton congiunta ammette 24 iterazioni e 12 dimezzamenti della ricerca lineare. La tolleranza del residuo di pressione idraulica è `2e-7 Pa + 64*epsilon*(|p_mid|+|p_old|)`; le tolleranze meccaniche e di frizione conservano i contratti esistenti. Stati non finiti, pressioni manometriche accettate negative o budget del solver esauriti rifiutano la chiamata completa. Riduci `step_ns` e controlla pressione, cedevolezza, cilindrata, inerzia e scale della frizione prima di riprovare. Non c'è una saturazione di cavitazione.

I test coprono l'oscillazione analitica di albero e cedevolezza e l'affinamento del secondo ordine, la conservazione a ingresso chiuso, il funzionamento inverso come motore, le reazioni di una pompa ingranata, il decadimento analitico dello scarico, un carico d'albero a regime regolato e una soluzione analitica indipendente di frizione strisciante dipendente dalla pressione. Cattura, rami, annullamento, fallimento tardivo, nuovo tentativo e passi senza allocazioni sono controllati. Il replay portabile e MCP confronta tutti gli 89 limiti di fired-pump; i contratti malformati e i declassamenti di versione sono rifiutati.

Nell'esperimento acceso di 0.8 s, l'albero eroga 53.94250162 J al fluido, il lavoro idraulico esterno è zero e lo scarico dissipa 45.02640514 J. L'energia idraulica iniziale è esplicitamente 3 J. La pressione finale di linea è 1.06972624 MPa, la velocità di albero e turbina 69.75553569 rad/s e la velocità del carico 6.64338435 rad/s. Il residuo di energia totale è circa `1.07e-9 J`; il residuo di volume di riferimento è `3.05e-20 m³`. Impronta `d0bd8f29a706fd89`, hash finale `572150ab5d66a2f6`.

Mappe di perdita misurate, controllo della cilindrata, dinamica di batteria e del controllo di tensione, corsa e inerzia di cursore e stantuffo, accumulatori a gas, cavitazione, proprietà dipendenti dalla temperatura e coordinamento ECU/TCU restano aperti. Questo checkpoint non stabilisce una DCT/AT completa, prestazioni di veicolo calibrate o l'accettazione di Unity Editor o Player.

## Trafilamento esplicito, attrito d'albero e alimentazione elettrica

`HydraulicPumpAssembly` fornisce una riduzione riusabile di pompa a coefficienti costanti. Accetta la cilindrata D in m³/rad, la conduttanza di trafilamento G in m³/(s·Pa) e l'attrito viscoso d'albero B in N·m·s/rad. D deve essere positiva; G e B devono essere non negativi e finiti. Non si deducono un rendimento nominale né una proprietà dell'olio.

Per la pressione differenziale `dp = p_out - p_in`:

```text
net inlet-to-outlet flow = D*omega - G*dp
shaft reaction          = -D*dp - B*omega
leakage heat power      = G*dp^2
shaft friction heat     = B*omega^2
absorbed shaft power    = net fluid power + leakage heat + shaft friction heat
```

I segni supportano il pompaggio e il funzionamento come motore idraulico in entrambi i versi, e anche il trafilamento attraverso una pompa ferma. Il trafilamento resta un percorso passivo dall'uscita all'ingresso anche quando supera la portata di cilindrata. La riduzione di trafilamento a conduttanza costante segue la descrizione analitica delle perdite nel [riferimento MathWorks della pompa](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html). Il trascinamento viscoso lineare è una scelta costitutiva esplicita di Power!; non è il modello di attrito dipendente dalla pressione di quel riferimento, né una mappa di rendimento OEM.

`TryEvaluate` restituisce la portata netta istantanea, la reazione totale d'albero, la potenza con segno di albero e fluido e le due potenze di perdita non negative. Rifiuta pressioni manometriche negative, input non finiti e overflow, senza restituire una reazione parziale.

`CreateComponents` restituisce una lista immutabile con ID espliciti e distinti per una `hydraulic_pump` ideale, una `hydraulic_resistance` ad apertura fissa dall'uscita all'ingresso e un `shaft` a rigidezza nulla dall'albero della pompa alla massa. Specifica un pozzo termico, oppure lascia che le perdite entrino nel rigetto esterno di calore. Il compilatore del modello controlla porte, domini, unità e ID globali. I componenti ordinari conservano la risoluzione accoppiata al punto medio, le transazioni, i canali, lo schema JSON e l'asset v11; non c'è uno stato di assemblaggio nascosto né un formato nuovo. I canali della pompa descrivono il ramo ideale. Sottrai la portata di trafilamento per ottenere l'erogazione dell'assemblaggio; includi il trascinamento d'albero quando interpreti il carico totale d'albero. Non contare il lavoro della pompa ideale sia come lavoro esterno di sorgente sia come trasferimento interno.

`fired-pump-losses` collega trafilamento e trascinamento alla trasmissione accesa esistente. Un nodo termico separato della pompa riceve entrambe le perdite. Il limite a perdite nulle riproduce tutti gli osservabili condivisi di `fired-pump` entro la tolleranza fisica. G e B costanti sono ingressi di ricerca e restano `unverified`.

`electric-pump` collega un motore CC RL a 12 V a un albero di pompa separato, con forza controelettromotrice, induttanza, coppia e calore nel rame espliciti. Una linea di alimentazione cedevole, uno scarico e valvole programmate di riempimento e scarico azionano una frizione fra un albero comandato e un carico. I cambi di tensione e gli eventi di valvola usano tick esatti. La pompa non ha collegamento all'albero a gomiti e il lavoro idraulico esterno è zero. Il lavoro elettrico è incluso nel lavoro globale di sorgente; l'albero comandato e la coppia di carico sono confini di potenza esterni separati. Tensione e comandi di valvola prescritti non implementano una batteria, una ECU/TCU o un regolatore ad anello chiuso.

Il moto analitico smorzato di albero e pressione, e una ODE a tre stati di motore RL, albero e pressione integrata in modo indipendente, controllano l'affinamento regolare del secondo ordine. Ingressi chiusi, lavoro di serbatoio, funzionamento con segno, perdite passive, instradamento del calore, zero allocazioni, rami, annullamento e rollback per fallimento tardivo sono controllati su entrambi gli assembly di Core. JSON, asset portabili e MCP confrontano tutti gli 89 limiti del report delle perdite accese e i 106 del report elettrico. I test Unity preparati di importazione e Play richiedono un'esecuzione separata dell'Editor.

<a id="sampled-pressure-regulation"></a>
## Regolazione di pressione campionata

`pressure_controller` legge un nodo di pressione manometrica idraulica e possiede un canale esistente di tensione di un motore CC. È un controllore PI discreto con guadagno proporzionale esplicito in `v_pa`, guadagno integrale in `v_pa_s`, limiti di tensione e tensione integrale iniziale. L'input di setpoint ha unità di pressione. Il suo `sample_period_ns` intero è 1 ns..1 s e deve essere un multiplo esatto del tick del modello. Guadagni e setpoint di pressione sono non negativi; i limiti di tensione sono finiti e strettamente crescenti. Non si deduce alcuna taratura.

```text
error = setpoint - sampled_pressure
I_candidate = I + Ki * sample_period_s * error
raw_voltage = Kp * error + I_candidate
if raw_voltage exceeds a limit and the integral increment pushes farther outside:
    retain the preceding integral state
command = clamp(Kp * error + accepted_integral, minimum_voltage, maximum_voltage)
```

L'integrazione condizionale è la strategia anti-windup per saturazione descritta dal [riferimento di controllo MathWorks](https://www.mathworks.com/help/simulink/slref/anti-windup-control-using-a-pid-controller.html). La transizione discreta precisa di Power! qui sopra è il suo modello dichiarato, non codice di implementazione copiato né evidenza di una taratura OEM. La sola saturazione non stabilisce l'inseguimento: un bersaglio irraggiungibile può eseguirsi e rieseguirsi con successo fallendo i KPI.

I campioni cadono al tempo zero e ai multipli assoluti del periodo configurato. Il primo campione conserva l'integrale iniziale fornito in modo esplicito; i campioni successivi usano il periodo. Il comando è mantenuto fra i campioni. Gli eventi su tick esatti si applicano prima di un campione allo stesso tick. Un evento all'estremo di una chiamata aggiorna il setpoint prima dello snapshot; il campionamento a quell'estremo avviene solo quando inizia il tick fisico successivo. Gli intervalli interni di cattura o inversione della frizione non innescano aggiornamenti aggiuntivi del controllore.

Il compilatore controlla che il bersaglio sia un input di tensione di un motore CC e abbia esattamente un proprietario, che il sensore sia idraulico e che la tensione iniziale del motore sia nei limiti. Il canale di tensione posseduto resta nella definizione del componente, ma è assente dalla lista degli input esterni. Le scritture dirette e gli override di tensione programmati sono rifiutati; cambia invece l'input `pressure_setpoint` del controllore. Gli altri canali di motore, pompa e valvola conservano la semantica esistente. Più anelli indipendenti possono condividere un sensore di pressione.

I canali osservabili sono `sampled_pressure`, `pressure_error`, `integral_voltage` e `command_voltage`. Le storie di pressione e di errore partono da zero; il comando iniziale è la tensione configurata del motore, e l'integrale iniziale è esplicito. Le storie descrivono l'ultimo campione, non un errore di pressione ricalcolato in continuo. Tutti e quattro gli stati del controllore e l'input mantenuto del motore partecipano a hash, fork e rollback completo del batch. Il campionamento e i passi riusciti non allocano memoria gestita dopo il riscaldamento. Un'aritmetica PI non finita rifiuta la chiamata completa; controlla le scale di guadagno, setpoint e integrale.

Il controllore non aggiunge energia fisica immagazzinata né un confine di potenza. Il suo comando cambia il confine di tensione del motore esistente, la cui corrente, il lavoro e il calore nel rame restano nella risoluzione accoppiata e nel registro di conservazione. I modelli senza controllori conservano impronte e passi. I modelli comandati aggiungono il tag di impronta 13. L'asset v12 conserva la definizione completa del controllore; un fixture autentico di pompa v11 conserva il digest originale, l'impronta e il replay allo stesso runtime dopo l'aggiornamento.

`pressure-regulated-pump` usa un controllore da 5 ms e tick fisici da 100 µs, con disturbi programmati di riempimento e scarico della frizione e bersagli di 300/350/200 kPa. Guadagni, limiti dell'attuatore e tutti gli altri parametri restano `unverified`. Ha 757 limiti di report coincidenti fra JSON, asset e MCP. I test confrontano un impianto separato a controllore campionato e RK4, controllano l'affinamento del tick fisico a periodo di controllore fisso, le regole esatte di orologio e di estremo, il recupero dalla saturazione, le diagnostiche di unità e di proprietà, il rollback dello stato del controllore, i rami, zero allocazioni e il registro completo del lavoro elettrico e idraulico.

Questo fornisce un anello di retroazione di pressione. Dinamica di batteria e di PWM o dell'anello di corrente, filtraggio, ritardo e quantizzazione del sensore, dinamica di valvola, cursore e stantuffo, coordinamento ECU/TCU, DCT/AT complete, guasti e calibrazione misurata restano lavoro separato e incompiuto.

<a id="finite-battery-supply-and-duty-regulation"></a>
## Alimentazione finita di batteria e regolazione del duty

Un nodo `battery` possiede due stati: la frazione di carica z e la tensione di polarizzazione v_p. Il suo accumulo è una capacità di carica esplicita Q in C o Ah (1 Ah = 3600 C), lo stato iniziale è lo SOC in `fraction` e la posizione è la tensione di polarizzazione iniziale in V. Il suo record di batteria fornisce la tensione a circuito aperto (OCV) a vuoto e a pieno, la resistenza serie R0, la resistenza di polarizzazione Rp, la capacità Cp e un pozzo termico o il rigetto esterno di calore. L'OCV è affine nello SOC:

```text
E(z)       = V_empty + (V_full - V_empty)*z
z'         = -I_battery / Q
v_p'       = I_battery / Cp - v_p/(Rp*Cp)
V_bus      = E(z) - v_p - R0*I_battery
U_chemical = Q*(V_empty*z + (V_full - V_empty)*z^2/2)
U_polar    = Cp*v_p^2/2
heat power = R0*I_battery^2 + v_p^2/Rp
```

La corrente positiva scarica; la corrente negativa carica. La topologia segue la [descrizione del circuito equivalente di batteria](https://www.mathworks.com/help/simscape-battery/ref/batteryequivalentcircuit.html). L'OCV affine e i parametri costanti sono riduzioni esplicite di Power!, non tabelle di temperatura o di invecchiamento, chimica misurata, fade di capacità o un BMS. Lo SOC resta entro [0,1]. Superare l'inventario di carica, o ottenere una tensione di bus negativa, rifiuta l'intero batch; non ci sono saturazione silenziosa né una riserva inventata. Accorcia il batch, interrompi la scarica o la carica, oppure fornisci condizioni iniziali dichiarate diverse.

`battery_motor` collega un albero rotazionale a un bus di batteria e conserva resistenza del motore, induttanza, coefficiente di coppia e di forza controelettromotrice e corrente iniziale espliciti. Il suo input di duty bidirezionale mediato sta in [-1,1]: la tensione del motore è il duty per la tensione di bus, e la corrente lato batteria è il duty per la corrente del motore. Quel trasferimento di potenza è interno e non si aggiunge a `source_work`. Sia l'energia induttiva del motore sia l'energia chimica e di polarizzazione della batteria partecipano all'energia totale immagazzinata. Il calore nel rame, in serie e di polarizzazione si instrada verso i pozzi espliciti. È un convertitore mediato ideale, non commutazione PWM, perdite del convertitore, contattori o un anello di controllo della corrente.

`resistive_load` fornisce una resistenza positiva esplicita, un input di apertura facoltativo in [0,1] e un pozzo termico. L'apertura scala la conduttanza; apertura zero disconnette in modo esatto. Per la conduttanza totale di carico G e la corrente di bus lato motore I_m:

```text
V_bus     = (E(z) - v_p - R0*I_m)/(1 + R0*G)
I_battery = I_m + G*V_bus
load heat = opening*V_bus^2/R_load
```

La resistenza condivisa della batteria accoppia tutti i consumatori. La matrice al punto medio accoppiata include carica, polarizzazione, corrente del motore e risposte meccaniche. I fattori posseduti dalla simulazione si aggiornano quando cambiano i duty, le aperture degli accessori o le durate degli intervalli interni. Le risposte di ingranaggio, cilindro, convertitore e frizione usano questi stessi fattori. I modelli precedenti non alimentati conservano il percorso del solver e le impronte precedenti. L'energia chimica affine e quella RC sono quadratiche, quindi i trasferimenti elettrici al punto medio hanno controlli di conservazione indipendenti. La stessa fisica supporta la rigenerazione del motore.

`pressure_duty_controller` usa la transizione PI a saturazione sull'orologio intero esistente, con guadagni in `fraction_pa` e `fraction_pa_s`, limiti di duty espliciti entro [-1,1] e un duty integrale iniziale. Possiede un canale di duty di `battery_motor`. Gli agenti cambiano `pressure_setpoint`; gli override diretti di duty restituiscono `controlled_input`. Si leggono `sampled_pressure`, `pressure_error`, `integral_duty` e `command_duty`. Duty mantenuto, carica, polarizzazione e memoria di controllo condividono snapshot, fork, annullamento e rollback completo. Campionamento e passi riusciti restano senza allocazioni.

`battery-regulated-pump` combina un'alimentazione finita di batteria, impulsi di carico accessorio e un regolatore di duty da 5 ms con il laboratorio della frizione a pressione. La sua capacità di 50 C è un piccolo inventario di prova sintetico, non una misura di batteria di veicolo. A 15 s lo SOC scende da 0.8 a circa 0.627, mentre la pressione termina a circa 200.828 kPa per un bersaglio di 200 kPa. Tutti i 761 limiti JSON, asset e MCP concordano. I test controllano a parte il rilassamento RC analitico, l'inventario del carico resistivo, l'integrazione RK4 indipendente di motore e circuito, l'affinamento del tick fisico, il duty con segno e la rigenerazione, l'equivalenza degli avvolgimenti in parallelo, l'accoppiamento di ingranaggio, frizione e pompa, il rollback per esaurimento tardivo, i rami, l'annullamento e zero allocazioni.

BMS, chimica e invecchiamento della batteria e retroazione di temperatura, guasti e contattori, controllo PWM e di corrente, dinamica dei sensori, meccanica degli attuatori, ECU/TCU complete e calibrazione restano aperti. I parametri della batteria e tutti gli input di laboratorio restano `unverified`.
