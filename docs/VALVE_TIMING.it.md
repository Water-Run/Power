# Fasatura delle valvole sull'angolo di manovella

[English](VALVE_TIMING.md) · [简体中文](VALVE_TIMING.zh-CN.md) · [Français](VALVE_TIMING.fr.md) · [Русский](VALVE_TIMING.ru.md) · [日本語](VALVE_TIMING.ja.md) · [한국어](VALVE_TIMING.ko.md) · [Deutsch](VALVE_TIMING.de.md) · [Español](VALVE_TIMING.es.md) · **Italiano** · [Português](VALVE_TIMING.pt-BR.md)

Un `valve_timing` opzionale su un `gas_orifice` moltiplica la sua apertura per un inviluppo
periodico sull'angolo di manovella. Supporta recipienti a gas fissi e cilindri mobili attraverso le
stesse definizioni di Core, JSON, CLI, MCP e asset portatili. L'inviluppo segue la posizione
reale dell'albero a gomiti durante accelerazione, arresto e inversione. Rappresenta l'area di
flusso efficace; non modella il contatto camma, l'alzata fisica della valvola, le forze di molla o l'attrito.

## Contratto e fase

```json
"valve_timing": {
  "crank_node": 1,
  "cycle_angle": { "value": 720, "unit": "deg" },
  "open_angle": { "value": 520, "unit": "deg" },
  "duration_angle": { "value": 200, "unit": "deg" }
}
```

`crank_node` deve identificare un nodo rotazionale. Tutti e tre gli angoli richiedono unità esplicite `deg`
o `rad`. L'angolo di ciclo è esattamente 360 o 720 gradi; la durata sta fra 1e-6
radianti e l'angolo di ciclo. L'angolo di apertura è finito e normalizzato modulo il ciclo.
Angoli negativi e un lobo che attraversa il confine del ciclo sono supportati. Più valvole
possono riferirsi a un solo albero a gomiti, inclusi lobi sovrapposti.

La fase è relativa all'angolo dell'albero a gomiti referenziato, inclusa la sua posizione iniziale.
La fase geometrica di un cilindro **non** viene aggiunta in automatico: l'autore deve scegliere
l'angolo di apertura della valvola adatto a ciascun cilindro. Un ciclo di 720 gradi distingue
le rivoluzioni successive dell'albero a gomiti. Non c'è una fase a quattro tempi implicita dedotta da
posizione del pistone, velocità o tempo trascorso.

Per ciclo `C`, angolo di apertura `a`, durata `D`, apertura di picco `u` e angolo di manovella `theta`:

```text
s = modulo(theta - a, C)       // in [0, C)
opening = u sin²(pi s / D)     // 0 < s < D
opening = 0                   // altrimenti, compresi entrambi i confini
A_effective = A_orifice * opening
```

Questo profilo e la sua derivata prima sono continui ai confini del lobo. La rotazione
inversa lo ripercorre; un albero a gomiti fermo mantiene l'apertura corrente e può continuare a far fluire.
L'apertura non sceglie la direzione del flusso: vale ancora la legge dell'orifizio esistente, bidirezionale
e guidata dalla pressione. Il suo coefficiente di efflusso resta un moltiplicatore separato.

Per una restrizione temporizzata, `initial_input` e il suo canale di input opzionale specificano l'**apertura
di picco**, una frazione in [0, 1]. La quantità di canale è `peak_opening`; zero disabilita
il lobo. La quantità di uscita `effective_opening` usa `Field.Opening` (campo KPI JSON
`opening`) e riporta la frazione effettiva. `mass_flow` si valuta con quella frazione.
Le restrizioni non temporizzate conservano la quantità di input e la semantica esistenti. Le variazioni
di picco programmate e interattive conservano la validazione atomica degli input e i controlli di revisione.

## Integrazione e recupero

I modelli temporizzati usano l'integrazione mezzo passo del gas / passo intero di lavoro all'albero / mezzo passo del gas,
incluso un recipiente fisso azionato da un albero a gomiti indipendente. Il primo mezzo
usa l'angolo di manovella iniziale, e il secondo l'angolo risultante. Il solver del gas
risolve la propria dinamica di massa/energia dentro ogni mezzo passo. Non localizza in continuo
i fronti delle valvole né adatta il tick meccanico esterno.

Per ogni lobo attivo, il tick esterno deve soddisfare:

```text
limit = min(0.25 rad, D / 8)
max(abs(theta_next - theta_old), dt * max(abs(omega_old), abs(omega_next))) <= limit
8 * binary64_epsilon * max(abs(theta_old), abs(theta_next)) <= limit
```

Il limite sulla velocità agli estremi copre anche un'inversione il cui cambiamento netto di angolo è piccolo.
Il limite di precisione impedisce a un angolo svolto di perdere la risoluzione necessaria per
il suo lobo. Un tick sotto-risolto fallisce anche quando entrambi gli estremi sono chiusi; non può
saltare in silenzio un'intera apertura stretta. Un picco disabilitato non richiede la risoluzione del lobo.
Sono protezioni numeriche, non una tolleranza di errore né una garanzia per dinamiche arbitrarie.

Un fallimento restituisce `NumericalFailure` / `numerical_failure` e non conferma alcuna parte del
batch del chiamante, inclusi i tick precedenti e gli input programmati. Riduci `step_ns` e ricrea
il modello/sessione; assicurati che gli eventi restino allineati al nuovo tick. Per angoli iniziali molto grandi
scegli un angolo equivalente coerente con la fase di ogni componente collegato.
Valgono anche i limiti esistenti di cilindro e gas. Non si aggiunge uno stato camma mutabile nascosto;
la posizione dell'albero a gomiti e gli input di picco partecipano già a snapshot, hash e ramificazioni.

I casi di riferimento lisci e senza parete mostrano convergenza del secondo ordine. Le temperature di parete
restano fisse sul tick esterno, quindi i modelli accoppiati alla parete restano del primo ordine. Il
limitatore di flusso esistente vicino all'equilibrio può ridurre l'ordine locale. Conservazione e replay non
dimostrano da soli l'accuratezza temporale.

## Evidenza e compatibilità

I controlli includono valori analitici dell'inviluppo, cicli espliciti, avvolgimento di fase, accelerazione,
inversione, un albero a gomiti fermo, picchi disabilitati, attraversamenti dell'intero lobo non risolti, annullamento,
rollback dell'intero batch, ramificazioni indipendenti e avanzamento/snapshot senza allocazioni.

Un test di svuotamento di un recipiente fisso integra in modo indipendente l'esposizione al seno quadrato e usa
la soluzione in forma chiusa dello scarico bloccato adiabatico. Sia il caso in avanti sia quello inverso
convergono sotto il raffinamento del tick. Un test separato del cilindro mobile integra massa, energia
interna, moto dell'albero a gomiti e una restrizione dipendente dall'angolo con ODE RK4 scritte in modo
indipendente, attraversando entrambi i confini del lobo. Il raffinamento di riferimento stabilisce la propria accuratezza
prima di confrontare i risultati di Core. Vedi [validazione](VALIDATION.it.md) per le soglie.

Solo i modelli temporizzati aggiungono il tag di impronta 6, gli ID di componente/albero a gomiti di destinazione e i
parametri di profilo normalizzati. I modelli non temporizzati conservano le impronte e l'avanzamento precedenti. L'asset v5
aggiunge record di fasatura limitati e conserva i lettori v1–v4; fixture autentiche precedenti controllano
impronte e replay aggiornato. Vedi [disposizione degli asset](ASSET_FORMAT.it.md).

Il [laboratorio del cilindro temporizzato sull'albero a gomiti](../assets/labs/crank-timed-cylinder.power.json)
esegue il motoring di una camera sintetica attraverso cicli ripetuti di 720 gradi con profili di ammissione e
scarico. Due variazioni di coppia programmate variano la velocità dell'albero a gomiti; la fasatura delle valvole stessa non ha
una programmazione temporale. JSON/CLI, MCP e la riproduzione dell'asset concordano a tutti i 63 confini di report.
La build esporta `CrankTimedCylinder.powerasset`; lo Studio anima marcatori schematici
dai canali di apertura efficace. L'esecuzione Editor/Play/IL2CPP resta in sospeso.

L'[esempio di reattore a combustione interna di Cantera](https://cantera.org/stable/examples/python/reactors/ic_engine.html)
è un riferimento concettuale per il controllo delle porte sull'angolo di manovella. Le sue ipotesi di velocità fissa,
la legge della valvola e i parametri di esempio non sono adottati come calibrazione o verifica del
solver di Power!. Questa implementazione usa l'accoppiamento conservativo dell'albero a gomiti del progetto
e la legge dell'ugello bidirezionale. La [combustione premiscelata](PREMIXED_COMBUSTION.it.md) separata
aggiunge ora carburante e la contabilità dell'energia chimica. Tutti i parametri di esempio restano `unverified`;
il comportamento completo del motore e la calibrazione misurata del veicolo restano aperti.
