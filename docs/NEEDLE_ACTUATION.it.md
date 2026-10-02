# Azionamento elettromagnetico dell'ago e retroazione di dose campionata

[English](NEEDLE_ACTUATION.md) · [简体中文](NEEDLE_ACTUATION.zh-CN.md) · [Français](NEEDLE_ACTUATION.fr.md) · [Русский](NEEDLE_ACTUATION.ru.md) · [日本語](NEEDLE_ACTUATION.ja.md) · [한국어](NEEDLE_ACTUATION.ko.md) · [Deutsch](NEEDLE_ACTUATION.de.md) · [Español](NEEDLE_ACTUATION.es.md) · **Italiano** · [Português](NEEDLE_ACTUATION.pt-BR.md)

Un iniettore liquido azionato legge l'alzata di un ago traslazionale invece di
chiudere un passaggio di massa ideale alla dose richiesta. Un solenoide dipendente dalla posizione,
massa esplicita dell'ago, molla di richiamo/smorzamento e arresti elastici di corsa forniscono il
moto. Un driver campionato possiede la tensione della bobina e ferma il suo comando quando la finestra
del ciclo si chiude o l'erogazione misurata raggiunge la richiesta agganciata.

Il decadimento della corrente, il ritardo meccanico di chiusura e il rimbalzo sulla sede possono continuare l'erogazione
dopo quel comando. Il carburante effettivo resta nel registro sorgente/film/gas; la dose
richiesta è un obiettivo di controllo, non un taglio fisico imposto. È un attuatore di ricerca
e una semplice retroazione on/off. Mappe magnetiche non lineari, saturazione,
isteresi/perdite per correnti parassite, resistenza dipendente dalla temperatura, commutazione/flyback,
alimentazione da batteria, forza fluida assiale e iniezione calibrata restano aperti.

## Energia magnetica e meccanica reciproche

`solenoid` usa una resistenza di avvolgimento costante fornita e un'induttanza lineare:

```text
L(x) = L_reference + gradient (x - x_reference) > 0
lambda = L(x) i
W_magnetic = lambda^2 / (2 L(x))
d(lambda)/dt = V - R i
F_magnetic = gradient i^2 / 2
```

L'alzata positiva aumenta l'induttanza e la forza magnetica agisce in quella direzione.
Entrambe le polarità di corrente attraggono l'ancora. La forza segue la coenergia
magnetica, come descritto dalla [guida alla forza di riluttanza di Modelica](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Magnetic.FluxTubes.UsersGuide.ReluctanceForceCalculation.html)
e dalle [equazioni del solenoide di Simscape](https://www.mathworks.com/help/simscape-electrical/ref/solenoid.html).
Power! usa la propria legge costitutiva ridotta e la propria integrazione; non si introduce una dipendenza
da Modelica o Simscape. L'induttanza deve restare positiva in tutte le
posizioni accettate e speculative. Il modello non tronca l'induttanza negativa
né sostituisce misure magnetiche mancanti con una mappa calibrata.

Lo stato magnetico è il concatenamento di flusso. Per un intervallo `h`, induttanze agli estremi
`L0,L1` e tensione tenuta, un gradiente discreto simmetrico dà:

```text
A = (1/L0 + 1/L1) / 4
lambda1 = ((1 - h R A) lambda0 + h V) / (1 + h R A)
i_bar = A (lambda0 + lambda1)
F_bar = gradient (lambda0^2 + lambda1^2) / (4 L0 L1)
W_supply = h V i_bar
Q_copper = h R i_bar^2
delta W_magnetic = W_supply - Q_copper - F_bar (x1 - x0)
```

Il flusso è eliminato analiticamente per una posizione di estremo proposta. La forza restante
e la sua derivata analitica rispetto alla posizione si uniscono alla stessa soluzione meccanica non lineare
di cilindri, convertitori, stantuffi idraulici e frizioni speculative.
Il moto accettato conferma flusso, lavoro elettrico, calore nel rame e forza media una volta.
Il calore nel rame entra nel nodo termico dichiarato o nel calore esterno ceduto; l'energia immagazzinata
magnetica e meccanica restano separate.

L'integrazione ODE simultanea indipendente verifica il raffinamento liscio del secondo ordine.
Anche il limite RL stazionario ha un riferimento analitico di corrente. L'avanzamento
coniugato in energia non stabilisce da solo un moto accurato a passo temporale grossolano;
le costanti di tempo elettriche, l'escursione di corsa e gli eventi di contatto hanno ancora bisogno di risoluzione.

## Massa dell'ago, molla e arresti elastici

L'ago è un nodo traslazionale ordinario con kg, m e m/s. Una `linear_spring` ordinaria
fornisce precarico e smorzamento con instradamento esplicito del calore.
`travel_stop` aggiunge energia unilaterale reversibile ai limiti nominali di corsa:

```text
E_stop = K/2 [max(0,x_min-x)^2 + max(0,x-x_max)^2]
```

La sua reazione discreta è il gradiente di energia negativo fra gli estremi accettati.
La penetrazione immagazzina energia invece di bloccare la posizione. La derivata analitica
condivide la soluzione meccanica; l'escursione di corsa nominale per intervallo è limitata a
un quarto dell'ampiezza. Un pattino ha un solo proprietario di arresto, inclusi gli arresti già
posseduti da uno stantuffo idraulico. Coordinate idrauliche/solenoide condivise restano
possibili, con ogni forza che contribuisce alla stessa coordinata.

Il rimbalzo sulla sede è fisico dentro questa riduzione elastica. Un'apertura nulla in uno
snapshot non dimostra flusso nullo per tutto un intervallo successivo. Smorzamento di contatto,
attrito di tenuta, restituzione d'urto e comportamento misurato di sede/ago restano aperti.

## Apertura fisica ed erogazione

`parameters.needle` opzionale su `liquid_fuel_injector` contiene un `needle_node`
traslazionale più `closed_position` e `full_open_position` in m/mm. L'apertura effettiva
è il rapporto di alzata lineare limitato:

```text
opening = clamp((x - x_closed)/(x_full_open - x_closed), 0, 1)
```

La legge passiva del [binario/ugello liquido](LIQUID_FUEL_INJECTION.it.md) integra il carico di pressione
con quell'apertura efficace. Conserva inventario finito e limiti di energia di pressione,
ma non limita l'erogazione fisica alla dose richiesta né cancella il flusso
quando la finestra di manovella si chiude o si inverte. Un ago aperto può ammettere carburante anche con
albero a gomiti fermo o dose richiesta nulla. La finestra di manovella aggancia ancora la storia
dell'obiettivo per la retroazione; l'erogazione fuori da una nuova finestra osservata resta parte delle
storie dell'ultimo ciclo osservato e del totale.

Senza `needle`, il percorso precedente dell'iniettore ideale limitato dalla quota è conservato, con
impronte del modello e replay invariati. I modelli dotati di ago dichiarano la loro
fedeltà diversa. L'ago è equilibrato in pressione in questo incremento; non si deduce alcuna forza
assiale di pressione/getto. Il ricevitore esistente a volume liquido trascurabile
esporta in modo esplicito il suo lavoro di pressione di spostamento.

## Driver a orologio intero e proprietà degli input

`needle_driver` nomina un iniettore azionato, il suo solenoide e lo stesso albero a gomiti
di fasatura. Richiede un `sample_period_ns` positivo esplicito, allineato ai tick fisici
e non maggiore di un secondo, e una `drive_voltage` positiva in V. La
bobina posseduta parte a tensione nulla. A ogni campione dovuto il driver registra la
dose agganciata e la massa erogata effettiva, poi tiene la tensione di azionamento mentre la finestra
in avanti ha ancora erogazione di obiettivo restante; altrimenti tiene tensione nulla.

Il driver possiede il canale di tensione del solenoide. Gli agenti scrivono la richiesta in `kg`
dell'iniettore; le scritture dirette di tensione restituiscono `controlled_input`, identificano il canale
di comando scrivibile e preservano stato/revisione. Le scritture iniziali e di evento non
fanno avanzare la storia di controllo. La fase di campionamento segue il tempo intero di simulazione. Questo
driver non implementa regolazione di corrente peak/hold, compensazione predittiva della chiusura,
PWM/flyback o comportamento completo di ECU/TCU.

## Definizioni, canali e transazioni

| Componente | Parametri e porte |
|---|---|
| `solenoid` | Nodo A traslazionale; input in V; resistenza in ohm non negativa, induttanza di riferimento positiva in H e gradiente in H/m (`h_m`), posizione di riferimento in m/mm, corrente iniziale in A; pozzo termico opzionale |
| `travel_stop` | Nodo A traslazionale; limiti crescenti in m/mm e rigidezza positiva in N/m |
| `needle_driver` | Nodo A di fasatura rotazionale; ID stabili di iniettore/solenoide, periodo di campionamento intero e livello di azionamento in V |

Le definizioni rifiutano quantità non correlate, unità/domini errati, induttanza iniziale
non valida, proprietà duplicata di arresto/tensione, ago/bobina/albero a gomiti non corrispondenti e
periodi di campionamento non allineati. I client Core usano `SolenoidCoil`, `StrokeStop`,
`NeedleDrive`, `InjectorNeedleDefinition` e le leggi magnetiche/di contatto indipendenti.

Le uscite del solenoide espongono `current` istantanea, `force` media discreta dell'ultimo tick,
`internal_energy` magnetica, `copper_heat` cumulativo e `source_work` elettrico.
Le uscite dell'arresto espongono energia elastica e reazione istantanea. Le uscite del driver
espongono `command_voltage` tenuta e la dose richiesta/erogata dell'ultimo campione. L'`opening`
dell'iniettore è l'apertura di posizione effettiva, con l'erogazione media effettiva dell'ultimo tick.
Tutti gli ID e le unità si scoprono attraverso la validazione e la creazione della sessione.

Quattro voci di stato riportate per solenoide e tre per driver si uniscono al budget di
stato limitato. Flusso, forza media, calore/lavoro compensati, stato di controllo campionato,
input tenuti, ago/arresto e tutte le storie di sorgente/fase si copiano/sottopongono a hash/eseguono il rollback con
la simulazione completa. L'avanzamento attivo riuscito e gli snapshot non allocano
memoria gestita. Annullamento, batch falliti e ramificazioni indipendenti preservano insieme
le storie elettriche, meccaniche, termiche e di controllore.

L'asset v20 aggiunge tabelle tipizzate magnetiche, di arresto, di ago e di driver conservando
i lettori v1-v19. Lunghezze/conteggi limitati, digest, unità, proprietà distinte e
protezione dal declassamento contraffatto sono controllati. Una fixture autentica di iniezione liquida v19
conserva la sua impronta e il replay aggiornato nello stesso runtime. Vedi
[ASSET_FORMAT.it.md](ASSET_FORMAT.it.md).

## Esperimenti e accettazione

`needle-actuated-cylinder` collega l'attuatore e la retroazione campionata al cilindro
acceso a binario finito/film. Il suo confine di 0.6 s può trattenere film liquido durante
l'ultimo transitorio di chiusura/evaporazione. L'inventario completo sorgente/film/gas/reazione
è verificato invece di assumere un film asciutto o l'erogazione esatta dell'obiettivo. JSON,
asset portatili e un vero server MCP figlio condividono le stesse definizioni/replay.

L'attuatore isolato richiede 8 mg e osserva erogazione in eccesso attraverso il decadimento
della corrente, il moto di chiusura e piccoli rimbalzi successivi sulla sede. Quelle quantità sono risultati
di ricerca, non una fasatura di iniettore calibrata né un controllore di inseguimento della dose accettato.
[VALIDATION.it.md](VALIDATION.it.md) registra l'evidenza numerica e i limiti.
Le viste Unity preparate di bobina, arresto, controllore e ago in scala richiedono ancora
la verifica reale di Editor/Play. Gruppo motopropulsore completo, azionamento misurato, forze
magnetiche/elettroniche/fluide raffinate, rabbocco del binario ed ECU/TCU restano incompiuti.
