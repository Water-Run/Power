# Pistone a gas lineare e accumulatore idraulico

[English](GAS_PISTON.md) · [简体中文](GAS_PISTON.zh-CN.md) · [Français](GAS_PISTON.fr.md) · [Русский](GAS_PISTON.ru.md) · [日本語](GAS_PISTON.ja.md) · [한국어](GAS_PISTON.ko.md) · [Deutsch](GAS_PISTON.de.md) · [Español](GAS_PISTON.es.md) · **Italiano** · [Português](GAS_PISTON.pt-BR.md)

`gas_piston` collega una massa traslazionale a una camera di gas finita. Massa, energia interna, pressione e temperatura della camera restano stati reali di simulazione. Il suo volume viene dalla geometria del pistone, non da un volume di accumulo fisso. Orifizi di gas e legami di calore di parete possono usare la stessa camera.

Combinare un pistone a gas e uno stantuffo idraulico sullo stesso nodo traslazionale crea un separatore di accumulatore sostenuto a gas. Entrambe le forze di pressione agiscono su una sola massa e su un solo spostamento nella risoluzione congiunta. Molle di richiamo, smorzamento ed estremità di corsa cedevoli restano componenti espliciti. È un accumulatore a pistone concentrato, con cedevolezza efficace del liquido costante e gas ideale caloricamente perfetto. Geometria della membrana, attrito delle tenute, dissoluzione del gas, cavitazione, usura e calibrazione OEM restano fuori.

## Geometria, pressione e lavoro

L'area A e il volume di riferimento Vr sono positivi. La posizione di riferimento xr è esplicita, e la direzione di compressione s è +1 o -1:

```text
gas volume       V(x) = Vr - s*A*(x-xr)
absolute pressure p  = (gamma-1)*U/V
gas temperature   T  = U/(m*cv)
force on slider      = -s*A*(p-p_reference)
```

`p_reference` è una pressione assoluta esplicita, zero compreso per un vuoto dichiarato. Nel comporre un accumulatore, fornisce il riferimento di serbatoio usato dalla convenzione di pressione manometrica del liquido. Non si deduce dalla precarica del gas. Pressione e temperatura iniziali del gas, la costante dei gas R e gamma vengono dal nodo gas; la sua massa iniziale è `p_initial*V_initial/(R*T_initial)`.

Durante l'intervallo meccanico, il lavoro del gas chiuso segue `U_next=U_old*(V_old/V_next)^(gamma-1)`. La forza usa la pressione media che dà esattamente questo trasferimento di energia discreto. Il lavoro della pressione di riferimento è `p_reference*s*A*dx` ed entra nel lavoro esterno di sorgente. Così l'energia interna del gas più l'energia meccanica del separatore bilancia il lavoro del fluido, il lavoro di riferimento e le perdite esplicite. L'energia a pressione manometrica disponibile dall'accumulo di gas usa `Delta U - reference_work`; la sola energia assoluta del gas sovrastima quel trasferimento.

Per una variazione relativa di volume z, il fattore di pressione è `phi=(1-(1+z)^(1-gamma))/((gamma-1)*z)`. Il suo valore limite è uno e la sua derivata limite è `-gamma/2`. L'implementazione usa serie scalate per le corse piccole, e differenze stabili di logaritmo ed esponenziale negli altri casi. Lo Jacobiano della forza è analitico. Camere di gas invertite e opposte condividono la stessa coordinata e conservano la convenzione con segno di volume e di lavoro.

Flusso e calore del gas usano la ripartizione simmetrica esistente flusso/lavoro/flusso. Una camera chiusa conserva l'invariante adiabatico; la gestione esplicita della temperatura di parete conserva l'accuratezza esistente del primo ordine accoppiata alla parete. La portata massica di gas è contabilizzata con l'entalpia del serbatoio, invece di trattare la massa aggiunta come priva di energia. Nessun adattamento politropico e nessun override isotermo sostituiscono lo stato energetico.

## Contratti e confini numerici

| Parametro | Significato |
|---|---|
| `node_a` | Nodo traslazionale con massa positiva |
| `node_b` | Camera di gas con un solo proprietario del volume mobile; ometti lo `storage` del nodo |
| `area` | Area positiva in m2 o mm2 |
| `reference_volume` | Volume positivo in m3 o litri |
| `reference_position` | Posizione in m o mm a quel volume |
| `reference_pressure` | Pressione assoluta non negativa in Pa o bar |
| `compression_direction` | +1 (orientamento predefinito) o -1 |

Una camera di gas ha un solo proprietario della geometria; più camere distinte possono agire su una massa. Un pistone a gas non ha un input diretto né un override del pozzo termico. Usa una sorgente di forza esplicita, uno stantuffo idraulico collegato, un orifizio di gas o un legame di calore del gas. Osserva massa, energia, pressione e temperatura della camera, e il volume del componente, la forza sul corsoio e `source_work` dalla pressione di riferimento.

Il volume di gas deve restare positivo, anche per tutta la corsa nominale di uno stantuffo idraulico condiviso. Un intervallo meccanico accettato cambia al più il 25% del volume di gas corrente. Una corsa grande, un volume non positivo o una forza non risolta rifiutano l'intero batch. Riduci la dimensione del tick e controlla geometria, massa, pressione e scale di forza prima di riprovare. Moto ed energia non sono saturati. La penetrazione cedevole dell'estremità immagazzina ancora il potenziale esplicito del finecorsa e resta soggetta a un volume di gas positivo.

Lo stato fisico e di controllo, gli inventari di gas e tutte le storie condividono annullamento, rollback completo, hash e fork indipendenti. L'asset v16 conserva le quattro quantità di geometria e di riferimento e la direzione di compressione. JSON, CLI e MCP espongono le stesse definizioni. Le viste Studio di separatore e di camera e i test di importazione e Play sono preparati in sorgente C# 9; l'evidenza reale di Editor e Player resta separata.

## Esperimento dell'accumulatore ed evidenze

`gas-accumulator-pump` aggiunge una camera di gas iniziale da 50 ml, un separatore da 50 g, smorzamento viscoso esplicito ed estremità cedevoli alla pompa elettrica, al bypass meccanico a cursore e alla frizione a pressione. Il gas parte a 200 kPa assoluti e 300 K; la pressione di riferimento è 100 kPa. Il separatore parte con 0.1 mm di penetrazione cedevole di appoggio, che equilibra la sua precarica contro una pressione manometrica del liquido nulla. Tutti i valori sono parametri di ricerca. L'impulso di tensione e di domanda da 3-4 s apre in modo esplicito entrambi i percorsi di riempimento e di scarico; l'energia di gas immagazzinata e il volume di liquido spazzato diminuiscono poi, prima che la carica riprenda.

[MathWorks Gas-Charged Accumulator (IL)](https://www.mathworks.com/help/hydro/ref/gaschargedaccumulatoril.html) descrive la separazione fra gas e liquido e il meccanismo di carica e scarica. Power! compone le proprie porte di gas a energia finita e quelle meccaniche e idrauliche, invece di copiare un esponente politropico fisso, default di parametri o codice di implementazione.

I controlli includono il lavoro adiabatico analitico e le derivate, la corsa sottile, un transitorio RK4 separato di massa ed energia con affinamento regolare del secondo ordine, le camere opposte, il moto comune di gas e fluido, l'affinamento RK4 a parete finita, l'ingresso di gas a volume mobile, i conti indipendenti di energia e volume e le transazioni complete. Le camere chiuse e non mescolate, senza trasporto né calore, saltano l'integrazione ridondante a tasso nullo dopo la validazione dello stato. L'ottimizzazione misurata conserva ogni valore di limite e ogni hash; i passi a regime e le letture di snapshot allocano zero byte gestiti. I limiti di errore dettagliati, i tempi e l'ambito di piattaforma sono in [VALIDATION.md](VALIDATION.it.md).
