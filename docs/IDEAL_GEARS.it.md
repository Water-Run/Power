# Riferimenti di ingranaggio ideale e planetario

[English](IDEAL_GEARS.md) · [简体中文](IDEAL_GEARS.zh-CN.md) · [Français](IDEAL_GEARS.fr.md) · [Русский](IDEAL_GEARS.ru.md) · [日本語](IDEAL_GEARS.ja.md) · [한국어](IDEAL_GEARS.ko.md) · [Deutsch](IDEAL_GEARS.de.md) · [Español](IDEAL_GEARS.es.md) · **Italiano** · [Português](IDEAL_GEARS.pt-BR.md)

`Power.Core` fornisce due riferimenti immutabili, senza allocazioni e a carico costante: `IdealGearPair` e `SimplePlanetaryGear`. Restituiscono le velocità dei membri, gli avanzamenti angolari, le coppie di reazione, il lavoro esterno, la variazione di energia cinetica e un residuo energetico. Forniscono evidenza analitica indipendente per il solver di trasmissione accoppiato. Il [solver di ingranaggi accoppiato](GEAR_NETWORK.it.md), separato, espone ora componenti permanenti di ingranaggio e planetario attraverso JSON, asset portabili e CLI/MCP, compresi esperimenti di cambio comandati da frizione. Le classi di riferimento restano soluzioni analitiche locali pure.

## Ambito fisico e segni

Un ingranaggio ideale non ha inerzia di ingranamento, cedevolezza, gioco o perdite; tutte le inerzie fornite sono inerzie dei rotori collegati. Entrambe le inerzie di una coppia, o tutti e tre i membri di un planetario, devono essere positive e finite. Massa e nodi privi di massa non si deducono da un'inerzia nulla. L'astrazione segue l'ambito di [IdealGear](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealGear.html) e [IdealPlanetary](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealPlanetary.html) della Modelica Standard Library. L'implementazione di Power! è scritta in modo indipendente; nessuna implementazione di terze parti è inclusa o chiamata.

Per una coppia, il rapporto con segno `r` definisce:

```text
omega_A = r omega_B
reaction_A = lambda
reaction_B = -r lambda
```

I rapporti positivi danno la stessa direzione delle porte; i rapporti negativi la invertono. Le reazioni sono coppie **sui rotori collegati**, non le coppie applicate dai rotori all'ingranaggio. Compiono lavoro netto nullo per un moto compatibile. La carcassa di una coppia di ingranaggi può portare una reazione; la sola quantità di moto angolare ordinaria dei due rotori non è in generale conservata. La quantità di moto generalizzata `r J_A omega_A + J_B omega_B` cambia con la coppia esterna generalizzata `r T_A + T_B`.

Per il planetario semplice, solare, corona e portasatelliti condividono un asse positivo. Il rapporto dei denti `k = N_ring / N_sun` deve essere maggiore di uno. La relazione cinematica e i due gradi di libertà indipendenti concordano con le [equazioni dell'ingranaggio planetario MathWorks](https://www.mathworks.com/help/sdl/ref/planetarygear.html).

```text
omega_S + k omega_R = (1+k) omega_C
reaction_S = lambda
reaction_R = k lambda
reaction_C = -(1+k) lambda
```

Queste reazioni sommano a zero e compiono lavoro netto nullo. Il modello accetta un rapporto continuo, senza dedurre numeri di denti, modulo, resistenza del dente o geometria fabbricabile. Inerzia di rotazione e di orbita dei satelliti, perdite, cuscinetti, lubrificazione e comportamento termico restano fuori da questo riferimento.

## Soluzione indipendente a coordinate ridotte

Il moto della coppia usa la porta B come coordinata indipendente:

```text
J_equivalent_B = r^2 J_A + J_B
alpha_B = (r T_A + T_B) / J_equivalent_B
alpha_A = r alpha_B
```

Il planetario elimina il moto del portasatelliti prima di formare la matrice di massa dell'energia cinetica. Con `a = 1/(1+k)` e `b = k/(1+k)`:

```text
omega_C = a omega_S + b omega_R

M = [ J_S + a^2 J_C,    ab J_C        ]
    [ ab J_C,           J_R + b^2 J_C ]

M [alpha_S, alpha_R]^T = [T_S + a T_C, T_R + b T_C]^T
alpha_C = a alpha_S + b alpha_R
```

L'implementazione scala questa matrice due per due e sviluppa il determinante in termini positivi, per evitare di sottrarre prodotti quasi uguali. Per carichi costanti l'accelerazione è costante, quindi velocità e spostamento seguono un'integrazione temporale lineare e quadratica esatta, a meno dell'arrotondamento in virgola mobile. Le coppie di reazione si recuperano poi dalle equazioni dei membri. I test usano un moltiplicatore indipendente del vincolo di accelerazione per il planetario libero; non riusano la matrice ridotta come soluzione attesa.

## Stato, unità e contratto di fallimento

I nomi delle proprietà pubbliche portano unità SI: kg m2, rad/s, rad, N m, J e secondi. I rapporti sono adimensionali. `Advance` riceve una durata locale positiva e finita e restituisce `GearStepStatus`. Questa durata locale di riferimento non sostituisce l'orologio intero limitato di `Simulation`. Le classi non trattengono stato in evoluzione. Gli input sono record valore; l'uscita è il default a ogni rifiuto, e istanze indipendenti possono essere condivise dai chiamanti.

Le velocità iniziali devono già soddisfare la relazione. La compatibilità usa una prova di arrotondamento relativo con epsilon binary64 `2.2204460492503131e-16`, senza banda morta assoluta alle basse velocità. Per una coppia il limite è `64 epsilon (|omega_A| + |r omega_B|)`. Il planetario include in più i moduli dei suoi due termini di velocità pesati, così la cancellazione è trattata in modo relativo alle operazioni che hanno formato la velocità del portasatelliti. I termini sono scalati prima della somma, per evitare l'overflow della tolleranza.

Dopo la validazione, velocità dipendente e avanzamento angolare sono ricostruiti dalle coordinate indipendenti. Questo rimuove il residuo di arrotondamento accettato; non è un calcolo di innesto a slittamento finito né di sincronizzazione. Velocità incompatibili restituiscono `IncompatibleState`. Per un disaccordo reale di velocità usa un modello esplicito di frizione o di urto, invece di scartarne l'energia. La fase assoluta dell'ingranaggio non è specificata: si riportano solo gli avanzamenti angolari.

Parametri di costruzione non validi lanciano eccezioni di argomento azionabili. Le combinazioni di parametri non finite o mal condizionate sono rifiutate; il determinante planetario scalato deve superare `64 epsilon`. Il rifiuto dell'intervallo distingue durata non valida, stato non valido, stato incompatibile, coppia non valida e fallimento numerico. L'overflow aritmetico restituisce `NumericalFailure`; input finiti da soli non garantiscono quantità derivate rappresentabili. Un controllo di equilibrio delle forze a runtime rifiuta anche una cancellazione che lascia reazioni dei membri finite ma incoerenti: ogni residuo di forza è limitato da `512 epsilon` volte la somma dei moduli delle coppie inerziali, applicate e di reazione. L'estremo accettato controlla anche il bilancio dell'impulso di ogni membro, usando `512 epsilon` volte i moduli della quantità di moto vecchia e nuova e degli impulsi applicati e di reazione. Quest'ultimo rileva una cancellazione eccessiva nella ricostruzione della velocità dipendente. Questi controlli limitano i residui, non l'errore di soluzione per parametri mal condizionati arbitrari. I test includono un fallimento finito per cancellazione e una coppia ad alto rapporto la cui reazione piccola deve restare osservabile. Il residuo è `external_work - kinetic_energy_change`; non si fabbrica calore di attrito.

## Stati di trasmissione ed evidenze

I test forniscono in modo esplicito coppie di tenuta o di blocco per stabilire questi limiti ideali:

| Condizione imposta | Relazione di velocità risultante |
|---|---|
| Corona tenuta | `omega_C = omega_S / (1+k)` |
| Solare tenuto | `omega_C = k omega_R / (1+k)` |
| Portasatelliti tenuto | `omega_S = -k omega_R` |
| Solare bloccato sulla corona | Le velocità dei tre membri sono uguali |

Il freno fornito compie lavoro nullo quando il suo membro è tenuto; un blocco solare/corona riceve coppie opposte con lavoro combinato nullo. Questi controlli stabiliscono stati di trasmissione statici. Questo riferimento non implementa un cambio, un innesto di frizione, un circuito idraulico o una TCU, e coppie esterne arbitrarie non tengono un membro in modo automatico.

Gli stessi otto gruppi di test girano su `net10.0` e `netstandard2.1`:

- Rapporti con segno e inerzia riflessa; potenza di reazione e bilancio dell'impulso per membro.
- Moto planetario libero contro una soluzione indipendente a moltiplicatore di forza.
- Tre casi di membro tenuto e presa diretta, con carichi espliciti di tenuta e di blocco.
- Invarianza di partizione a carico costante e inversione della velocità attraverso zero.
- Carico sinusoidale contro integrali indipendenti per entrambi i riferimenti; il dimezzamento dell'intervallo dà una riduzione circa quadrupla dell'errore di velocità e di angolo.
- Valori non validi di inerzia, rapporto, stato e carico, velocità incompatibili, mal condizionamento e overflow.
- 2,500 casi deterministici per ogni riferimento, che controllano lavoro, quantità di moto e riproducibilità.
- 10,000 valutazioni ripetute di ogni primitivo con zero allocazioni gestite, più uso immutabile condiviso da chiamanti concorrenti indipendenti.

Vedi la [validazione](VALIDATION.it.md) per il risultato completo della verifica in serie. I test dell'assembly Standard girano su .NET 10 e non forniscono evidenza di Unity Editor, Play o IL2CPP.

## Integrazione accoppiata

I vincoli permanenti partecipano ora alla risoluzione elettromeccanica, del cilindro e della frizione, con spazi di lavoro di simulazione indipendenti, rollback completo e canali stabili di reazione e di errore. JSON e schema, l'asset v8 con i lettori precedenti, la scoperta MCP e il replay usano la stessa topologia. L'esperimento planetario acceso esegue passaggi in salita di riduzione e presa diretta, e un passaggio in discesa. Vedi [il contratto accoppiato](GEAR_NETWORK.it.md) per equazioni ed evidenze. Topologia DCT/AT completa, convertitore, idraulica, controlli, comportamento completo del motore e calibrazione misurata del veicolo restano parte dell'obiettivo completo di Power!.
