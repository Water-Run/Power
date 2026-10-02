# Dosatura meccanica del cursore e regolazione della pressione

[English](HYDRAULIC_SPOOL.md) · [简体中文](HYDRAULIC_SPOOL.zh-CN.md) · [Français](HYDRAULIC_SPOOL.fr.md) · [Русский](HYDRAULIC_SPOOL.ru.md) · [日本語](HYDRAULIC_SPOOL.ja.md) · [한국어](HYDRAULIC_SPOOL.ko.md) · [Deutsch](HYDRAULIC_SPOOL.de.md) · [Español](HYDRAULIC_SPOOL.es.md) · **Italiano** · [Português](HYDRAULIC_SPOOL.pt-BR.md)

`hydraulic_spool_valve` dosa una porta idraulica dallo spostamento reale di uno `hydraulic_piston` esplicito. Lo stantuffo fornisce la massa, il volume di fluido spazzato, la forza di pressione e le estremità di corsa cedevoli; una `linear_spring` separata fornisce la forza di richiamo, il precarico e lo smorzamento. Più spalle di dosatura possono riferirsi allo stesso stantuffo.

## Equazioni e confini

Le posizioni di chiusura e di piena apertura definiscono una corsa con segno L. Con lo spostamento x:

```text
opening = clamp((x - closed_position) / L, 0, 1)
d       = pA - pB
Q       = K * opening * d / (d*d + transition_pressure*transition_pressure)^(1/4)
loss    = Q * d >= 0
```

K è un coefficiente esplicito a piena apertura, in m3/(s*sqrt(Pa)); la pressione di transizione è positiva. L'implementazione scala il denominatore per evitare di elevare al quadrato differenze di pressione enormi. La corsa con segno supporta entrambi i versi di apertura. Una spalla chiusa sigilla in modo esatto; il trafilamento richiede un percorso aggiuntivo esplicito. Solo l'apertura satura: posizione, pressione, velocità ed energia immagazzinata non sono saturate.

La spalla è equilibrata in pressione e la forza assiale del getto è trascurata. La differenza di pressione della sua porta di dosatura non applica una forza assiale aggiuntiva allo stantuffo. Le pressioni delle camere anteriore e posteriore dell'attuatore esplicito forniscono la sua forza motrice. Il calore di restrizione e il lavoro di stantuffo e molla usano i registri conservativi esistenti. Questo modello esclude l'attrito delle tenute, gli effetti di quantità di moto della forza di flusso, la cavitazione, l'usura e la geometria o la viscosità dipendenti dalla temperatura. È una riduzione di ricerca dichiarata, non una valvola calibrata.

[MathWorks Spool Orifice (IL)](https://www.mathworks.com/help/hydro/ref/spoolorificeil.html) documenta l'area di apertura variabile e un'opzione separata di forza assiale di flusso. Power! usa la propria spalla lineare normalizzata e la legge di restrizione passiva esistente; non sono stati copiati geometria, default di proprietà del fluido o codice di implementazione.

## Risoluzione condivisa e contratti

La valvola legge `x_old + dx/2` nella stessa risoluzione di Newton congiunta delle sue pressioni di fluido, della forza dello stantuffo e dei vincoli meccanici. Le derivate analitiche includono sia la pressione sia lo spostamento della spalla. Dentro la corsa di dosatura, `dOpening/dx=1/L`; fuori, la derivata è zero. A ogni estremo lo Jacobiano usa la pendenza unilatera media. Questo conserva un anello di retroazione simultaneo, invece di un comando di apertura ritardato.

| Parametro | Significato |
|---|---|
| `piston_component` | ID stabile di uno stantuffo idraulico esplicito |
| `closed_position`, `full_open_position` | Posizioni distinte in m o mm, entrambe dentro la corsa nominale dello stantuffo |
| `coefficient` | Coefficiente non negativo a piena apertura, in `m3_s_sqrt_pa` |
| `transition_pressure` | Pressione di regolarizzazione positiva, in Pa o bar |
| `reservoir_pressure` | Confine di pressione manometrica richiesto quando l'idraulica B è omessa o zero |

Le porte idrauliche A/B e un pozzo termico facoltativo seguono il contratto della restrizione. La valvola non ha `input_channel` né `initial_input`; si osserva il suo canale `opening` e si comanda il circuito reale dell'attuatore. La portata e la potenza medie e il calore idraulico cumulativo sono osservabili. Unità, tipo del componente riferito e limiti di corsa producono errori di validazione azionabili. I contratti ordinari di rollback dell'intero batch, annullamento, fork, orologio intero e replay esatto allo stesso runtime includono tutti gli stati e le storie. Le impronte dei modelli fisici esistenti restano invariate.

L'asset v15 aggiunge un record di 32 byte della geometria di dosatura. JSON, CLI e MCP conservano le stesse definizioni. `get_example_model("spool-regulated-pump")` mostra una pompa elettrica, un bypass governato in modo meccanico e il riempimento e lo scarico programmati della frizione a pressione. Il suo precarico statico di chiusura di 200 N viene da una molla di richiamo da 200 kN/m a 1 mm di compressione e da un'area esplicita dell'attuatore di 1000 mm2. La trazione e il freno rotazionali sono 2 N*m; un esperimento di tre secondi lascia tempo sufficiente perché la frizione alla pressione più bassa catturi. I parametri sono sintetici e non verificati.

## Evidenze e prestazioni

I controlli coprono la corsa di dosatura con segno, il flusso bidirezionale passivo, le derivate analitiche di pressione e posizione, una radice di pressione a regime indipendente, un transitorio RK4 separato a tre stati, l'affinamento regolare del secondo ordine, l'errore decrescente attraverso l'apertura della spalla, l'equalizzazione a porta finita, il volume spazzato, l'energia indipendente di moto e di fluido e le transazioni complete. I passi a regime e le letture di snapshot allocano zero byte gestiti. Le pendenze delle spalle e i buffer di Newton e LU appartengono a ogni simulazione; non si aggiungono un nuovo orologio né un worker.

Vedi [VALIDATION.md](VALIDATION.it.md) per gli errori misurati, l'ambito di runtime e il tempo trascorso. Le viste Studio di valvola e attuatore e i test di importazione e Play sono preparati in sorgente C# 9; l'evidenza reale di Unity Editor e Player resta in sospeso.
