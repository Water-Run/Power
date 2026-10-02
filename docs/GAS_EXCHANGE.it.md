# Primitivi dello scambio gas

[English](GAS_EXCHANGE.md) · [简体中文](GAS_EXCHANGE.zh-CN.md) · [Français](GAS_EXCHANGE.fr.md) · [Русский](GAS_EXCHANGE.ru.md) · [日本語](GAS_EXCHANGE.ja.md) · [한국어](GAS_EXCHANGE.ko.md) · [Deutsch](GAS_EXCHANGE.de.md) · [Español](GAS_EXCHANGE.es.md) · **Italiano** · [Português](GAS_EXCHANGE.pt-BR.md)

Questo documento registra la prima porzione dell'incremento di scambio gas descritto
nelle [note sulla ripresa del motore](NEXT_ENGINE_STEP.it.md): la fisica del flusso e del
volume di controllo, validata per conto proprio, prima che qualcosa di essa sia collegato
al grafo del modello compilato.

I primitivi stanno in `src/Power.Core/GasExchange.cs` e sono coperti da
`tests/Power.Tests/GasChecks.cs`. Un successivo [punto di controllo della rete gas di Core](GAS_NETWORK.it.md)
collega ora nodi gas finiti, restrizioni, legami termici e registri di conservazione al
modello compilato. L'integrazione del 2026-09-22 aggiunge JSON, CLI/MCP e l'asset portatile v3,
mantenendo i lettori v1/v2 per i loro insiemi di modelli originali. Viste schematiche Unity e
test sono preparati; la verifica reale dell'Editor resta in sospeso. Il cilindro adiabatico chiuso resta un
riferimento analitico invariato, ancora senza scambio di massa attraverso la sua camera accoppiata all'albero a gomiti.

## Cosa è implementato

| Tipo | Responsabilità |
|---|---|
| `IdealGas` | Gas caloricamente perfetto di una composizione fissa: `R`, `gamma`, `cv`, `cp`, il rapporto di pressione critico e i due coefficienti di flusso di massa dell'ugello precalcolati. |
| `GasVolumeState` | Un volume finito tracciato da **massa ed energia interna come stati indipendenti**, con densità, temperatura, pressione ed entalpia specifica derivate. |
| `Orifice` | Flusso comprimibile ideale attraverso una restrizione, con un coefficiente di efflusso e una frazione di apertura adimensionale in `[0,1]`, con segno in entrambe le direzioni, bloccato e subcritico. |

`GasVolumeState` sostituisce di proposito la derivazione del cilindro chiuso da angolo ed entropia iniziale.
Poiché massa ed energia interna sono portate in modo indipendente, lo stesso stato può assorbire massa
trasportata, entalpia trasportata e calore di parete senza assumere una storia isentropica.

## Equazioni

La pressione statica usa `p = (gamma - 1) U / V`, esatta per un gas caloricamente perfetto, ed evita
un passaggio separato per la temperatura. La temperatura è `T = U / (m cv)`.

Il flusso di massa segue le relazioni standard dell'ugello isentropico. Con `A` area efficace
(area geometrica x coefficiente di efflusso x apertura), stato statico a monte `p_u, T_u` e rapporto
di pressione `pr = p_d / p_u`:

- bloccato, `pr <= (2/(gamma+1))^(gamma/(gamma-1))`:
  `mdot = A (p_u / sqrt(T_u)) sqrt(gamma/R) (2/(gamma+1))^((gamma+1)/(2(gamma-1)))`
- subcritico: `mdot = A (p_u / sqrt(T_u)) sqrt(2 gamma / (R (gamma-1)) (pr^(2/gamma) - pr^((gamma+1)/gamma)))`

La corrente porta l'entalpia a monte, `hdot = mdot cp T_u`, così la direzione del flusso decide
quale temperatura di estremo viene trasportata. Un serbatoio è passato come una coppia ordinaria `(p, T)`, quindi
non serve un volume fittizio per un confine fisso.

Riferimento per i due rami: [blocco del flusso di massa NASA](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html).
È un riferimento per le relazioni, non una validazione di questa implementazione.

## Note numeriche

La funzione di flusso subcritico si valuta come `pr^(2/gamma) * (1 - pr^((gamma-1)/gamma))` con il
secondo fattore calcolato tramite `expm1`. La differenza da manuale di due potenze quasi uguali si cancella
in modo catastrofico quando `pr` si avvicina a uno: a `pr = 1 - 1e-12` conserva circa quattro cifre, mentre
la forma con `expm1` è accurata alla precisione del rapporto memorizzato. `Numeric.Expm1` e `Numeric.Log1p`
sono ora condivisi con la fisica del cilindro chiuso, invece di essere duplicati.

Due limiti sono intrinseci al modello piuttosto che all'implementazione, e un solver che lo adotta
deve gestire entrambi:

- Il ramo subcritico ha una **derivata infinita al rapporto di pressione unitario**. Un passo di Newton non
  deve attraversare quel punto in modo diretto; va racchiuso o smorzato.
- Un rapporto vicino a uno non si può rappresentare in modo utile in binary64. A `pr = 1 - 1e-15` sopravvive circa una
  sola cifra dello scostamento, comunque si scriva la funzione.

Condizioni di ristagno e statiche a monte sono trattate come uguali. È la consueta approssimazione
quasi stazionaria a volume di controllo e **non** è valida per un flusso di camera ad alto Mach.

## Evidenza

`tests/Power.Tests/GasChecks.cs` aggiunge sei controlli, ciascuno scritto contro una forma chiusa indipendente
piuttosto che contro un'uscita registrata di questo codice:

1. **Proprietà e continuità del blocco** — `cv`, `cp` e il rapporto critico contro le loro
   definizioni per `gamma` in `{1.1, 1.3, 1.4, 5/3}`; il ramo subcritico che raggiunge il
   coefficiente bloccato esattamente al rapporto critico; decadimento monotono della funzione di flusso fino a zero, controllato
   contro la forma ingenua dove quella forma è affidabile e contro lo sviluppo al primo ordine
   dove non lo è.
2. **Flusso dell'ugello** — 54 combinazioni di pressione a monte, temperatura a monte e rapporto di pressione
   contro le relazioni NASA scritte per esteso, incluso il fatto che il flusso bloccato è indipendente dalla
   pressione a valle e che l'entalpia è trasportata alla temperatura a monte.
3. **Contratti** — antisimmetria esatta scambiando gli estremi, flusso nullo a orifizio chiuso e
   a pressioni uguali, linearità nella frazione di apertura, e rifiuto di stati non finiti o
   non positivi, aperture fuori da `[0,1]` e parametri di gas o orifizio non validi.
4. **Svuotamento adiabatico di un recipiente** — integrazione RK4 di un recipiente da 2 L da 20 bar e 900 K contro la
   soluzione isentropica analitica `rho(t) = (rho0^-k + k C t / V)^(-1/k)`, `k = (gamma-1)/2`, accordata a
   1e-9 relativo in densità e temperatura e 1e-8 in pressione, con un controllo di raffinamento.
5. **Riempimento da serbatoio** — carica di un recipiente da 0.5 L da un serbatoio a 6 bar e 320 K: l'identità esatta
   `dU = cp T_supply dm` mentre il flusso è unidirezionale, e il limite del recipiente evacuato
   `T -> gamma T_supply`, controllato da due pressioni di partenza diverse.
6. **Rete chiusa a due volumi** — 2 s di scambio tra un volume caldo da 1.5 L e un volume freddo da 0.4 L:
   massa totale conservata a 1e-14 relativo ed energia interna totale a 1e-12 relativo, pressioni
   che si equalizzano, ed equilibrio confermato come meccanico piuttosto che come temperatura di miscela completa.

## Cosa resta aperto

Il [punto di controllo della rete gas di Core](GAS_NETWORK.it.md) copre ora nodi a volume fisso,
serbatoi, restrizioni, legami termici, registri di massa ed energia, canali di uscita e
avanzamento transazionale limitato. JSON, asset portatili, scoperta delle capacità ed esempi
di replay sono integrati. L'[estensione del cilindro mobile](MOVING_CYLINDER.it.md) accoppia ora scambio gas e
lavoro all'albero a gomiti. La [fasatura opzionale sull'angolo di manovella](VALVE_TIMING.it.md) comanda le restrizioni, e
la [combustione premiscelata](PREMIXED_COMBUSTION.it.md) aggiunge carburante/aria/prodotti e la
contabilità dell'energia chimica. L'evidenza dell'Editor Unity è una
consegna separata. Il metodo implicito a coppie proposto non è stato adottato: il metodo
esplicito attuale, il suo limitatore di equilibrio e i suoi limiti di accuratezza sono documentati lì.
I volumi collegati richiedono costanti del gas e gamma identici; la termochimica dettagliata delle specie resta aperta.
