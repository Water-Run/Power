# Scarico carburante a titolo congelato e ricevitori finiti

[English](FUEL_DISCHARGE.md) · [简体中文](FUEL_DISCHARGE.zh-CN.md) · [Français](FUEL_DISCHARGE.fr.md) · [Русский](FUEL_DISCHARGE.ru.md) · [日本語](FUEL_DISCHARGE.ja.md) · [한국어](FUEL_DISCHARGE.ko.md) · [Deutsch](FUEL_DISCHARGE.de.md) · [Español](FUEL_DISCHARGE.es.md) · **Italiano** · [Português](FUEL_DISCHARGE.pt-BR.md)

## Modello fisico

Un `fuel_discharge` collega una sorgente finita del dominio carburante al nodo gas di un `finite_fuel_film`. Dichiarare `phase_mode` come `frozen`: il titolo massico del vapore resta fisso nell'ugello mentre i volumi alle estremità tornano in equilibrio. Le fasi omogenee condividono temperatura e velocità; liquido incomprimibile e vapore come pseudospecie ideale. Proprietà e densità devono coincidere.

Per pressione e temperatura di ristagno a monte, massimizzare questo flusso massico tra pressione ricevente e sorgente. L'unico massimo interno limita il flusso strozzato. Liquido e vapore puri danno Bernoulli e la relazione sonica del gas ideale. Il coefficiente di efflusso scala l'area efficace di gola.

```text
x = upstream vapor mass fraction; r = p/p0
cp = (1-x) cl + x (cv+R); k = x R/cp; b = (1-x)/rho_l
T(p) = T0 r^k
h0-h(p) = cp T0 (1-r^k) + b p0 (1-r)
v(p) = b + x R T(p)/p
G(p) = sqrt(2 (h0-h(p))) / v(p)
mass_rate = opening Cd A max(G)
jet_kinetic_energy_per_kg = h0-h(p_receiver)
gas_volume = cavity_volume - liquid_mass/rho_l
```

## Energia, volume e quantità di moto

La sorgente perde l'entalpia di ristagno trasportata. `wall_kinetic_fraction` assegna parte dell'energia cinetica del getto ideale completamente espanso alla parete finita ferma; il resto entra nel fluido. La velocità di riferimento assume espansione ideale alla pressione ricevente e non predice una velocità d'uscita misurata. L'impulso raggiunge il ricevitore fermo con reazione opposta della carcassa; queste reazioni non compiono lavoro né aggiungono forza a pistone o ago.

Dichiarare `finite_volume_density`. Il `volume` del gas è la cavità meno massa liquida/densità; il `volume` del film è il volume liquido spostato. Il cilindro mobile calcola lavoro a massa ed entropia costanti dell'intero ricevitore. Il lavoro di spostamento è interno. Sono supportati cavità fisse e cilindri a manovella, non pistoni gas lineari.

## Uso e prove

Eseguire `finite-volume-fuel-discharge` per strozzamento da sorgente bifase, o `pump-fed-finite-volume-cylinder` per pompa elettrica, ago elettromagnetico fisico e combustione prescritta. `needle` e `valve_timing` opzionali moltiplicano l'apertura. L'esempio usa impulsi espliciti di tensione senza massa richiesta per ciclo o controllo dose campionato. Leggere `jet_kinetic_energy`, `jet_impulse`, `force`, `throat_pressure`, `exit_temperature`, `exit_velocity` e `choked` insieme agli inventari completi. Gli ultimi quattro valori mantengono l'ultimo stato di flusso accettato dopo l'arresto.

Limiti indipendenti di Bernoulli e gas ideale, massimizzazione separata del flusso bifase, energia/entropia e ODE ugello/ricevitore integrata indipendentemente verificano le equazioni. Il raffinamento temporale è del primo ordine. Replay portabile, MCP reale, rollback, annullamento, rami indipendenti e allocazioni coprono l'integrazione. Unity Editor/Play/Player/IL2CPP reale resta non verificato.

## Validità e lavoro successivo

I parametri restano `unverified`. Valgono temperature dichiarate, volume gas positivo e dominio di vapore diluito. Mancano inerzia locale d'ingresso, nucleazione finita, scorrimento delle fasi, spray risolto, criticità reale e calibrazione misurata. La combustione prescritta non prova equilibrio chimico o entropia di reazione. Proseguire ingresso, accensione, aspirazione/scarico, perdite e ECU/TCU coordinati.

[NASA: ideal-gas mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) · [NASA-CR-64098: two-phase mass-limiting experiments](https://ntrs.nasa.gov/citations/19650020869) · [VALIDATION.md](VALIDATION.md)
