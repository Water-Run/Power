# Rilassamento di fase del combustibile a velocità finita

[English](RELAXING_FUEL.md) · [简体中文](RELAXING_FUEL.zh-CN.md) · [Français](RELAXING_FUEL.fr.md) · [Русский](RELAXING_FUEL.ru.md) · [日本語](RELAXING_FUEL.ja.md) · [한국어](RELAXING_FUEL.ko.md) · [Deutsch](RELAXING_FUEL.de.md) · [Español](RELAXING_FUEL.es.md) · **Italiano** · [Português](RELAXING_FUEL.pt-BR.md)

## Modello fisico

Un nodo `relaxing_fuel` ha una frazione massica di vapore indipendente. Liquido e vapore condividono pressione, temperatura e velocità, mentre l’equilibrio chimico richiede un tempo finito dichiarato. Un `relaxing_fuel_duct` trasporta massa, quantità di moto, energia totale e massa di vapore in celle esplicite. La permanenza dipende da geometria e velocità effettiva.

La chiusura mantiene il potenziale freddo del liquido comprimibile e il vapore ideale del modello di equilibrio. La frazione iniziale è esplicita e non proiettata all’equilibrio. A massa, volume ed energia interna fissi, tende esponenzialmente al valore di equilibrio con `phase_relaxation_time_ns` costante. Il calore latente resta nel bilancio interno. L’affinità chimica è il potenziale del liquido meno quello del vapore; il prodotto con il trasferimento di fase, diviso per la temperatura, dà produzione entropica non negativa. È una legge di ricerca, non una correlazione di nucleazione misurata.

[Lund: A hierarchy of relaxation models for two-phase flow](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Trasporto e metodo numerico

I flussi conservativi di Rusanov trasportano quattro campi. Ogni intervallo CFL globale applica mezza sorgente locale, trasporto e mezza sorgente. I serbatoi condivisi ricevono ogni sorgente una sola volta indipendentemente dal numero di condotti. La sorgente è esatta per stati isolati a volume fisso; il trasporto accoppiato mantiene errore del primo ordine. CFL usa il limite acustico a frazione congelata. Entropia di fase e del condotto sono verificate separatamente. Uno stato fuori limite annulla l’intero lotto.

```text
x = vapor_mass / total_mass; v = 1/rho
v_liquid(p) = 1 / (rho0 (1 + p/K)); v_vapor(T,p) = R T/p
v = (1-x) v_liquid + x v_vapor
u = (1-x) [c_liquid T + a + u_cold(p)] + x c_vapor T
u_cold(p) = K/rho0 [ln(1+p/K) - (p/K)/(1+p/K)]

dx/dt = (x_equilibrium(rho,u) - x)/tau
x(t+h) = x_equilibrium + (x(t)-x_equilibrium) exp(-h/tau)
d(total_mass)/dt = d(total_internal_energy)/dt = d(volume)/dt = 0  (local source)
affinity = mu_liquid - mu_vapor
(ds/dx) at fixed u,v = affinity/T
ds/dt = affinity (x_equilibrium-x)/(T tau) >= 0

cp_frozen = (1-x) c_liquid + x (c_vapor+R)
alpha = x R/cp_frozen
c_frozen^2 = v^2 / [(1-x) v_liquid/(K+p) + (1-alpha) x R T/p^2]
U = (rho, rho velocity, rho total_specific_energy, rho x)
F = (rho velocity, rho velocity^2+p,
     velocity (rho total_specific_energy+p), rho x velocity)
```

## Uso, prove e limiti

Eseguire `relaxing-fuel-evaporation` o `relaxing-fuel-duct`. Osservare frazioni effettiva e di equilibrio, affinità, entropia di fase, bilanci di vapore alle porte e residuo globale. Le storie di evaporazione e condensazione separano per segno il trasferimento netto della sorgente di ogni passo accettato, senza contare i flussi microscopici lordi. Le prove includono soluzione esponenziale indipendente, derivate entropiche, acustica congelata, limite rigido di equilibrio, autoraffinamento temporale e raffinamento spaziale acustico lineare indipendente. Transazioni, riproduzione portatile e MCP reale sono verificati separatamente; i numeri sono nelle prove di validazione.

I parametri restano `unverified`. Entrambe le porte richiedono stesso dominio e materiale; quelle rilassanti rifiutano il transito di equilibrio. Il rilassamento finito nei serbatoi non risolve chimica o permanenza dentro un ugello senza volume. Mancano scorrimento delle fasi, tensione liquida negativa, barriera di nucleazione, spray, gas vettore e ricevitore in combustione. I serbatoi mobili sono miscelati con velocità media nulla; le celle interne restano rigide con un proprietario. Usare vapore ideale diluito lontano dal critico, limiti dichiarati di pressione positiva e temperatura, entalpia latente positiva e volumi di coesistenza separati. La frazione iniziale è un ingresso di ricerca esplicito. L’accordo numerico non attesta calibrazione misurata né accettazione Unity reale.

Risolvere la permanenza finita nell’ugello con prove sul materiale, poi aggiungere spray risolto e ricevitori in combustione compatibili. Continuare accensione, aspirazione/scarico, perdite meccaniche, attuazione trasmissione e coordinamento ECU/TCU. Calibrazione misurata e accettazione Unity restano separate.

[VALIDATION.it.md](VALIDATION.it.md)

Un `compressible_fuel_chamber` può controllare il volume di un serbatoio `relaxing_fuel`. Il lavoro reversibile dell’albero mantiene fissa la frazione di vapore; il rilassamento chimico è una sorgente separata che conserva energia. Un `compressible_fuel_nozzle` collega due serbatoi rilassanti con `phase_mode` su `frozen`, trasportando massa totale, vapore ed entalpia di ristagno. Un ago fisico con bobina, molla e battuta può dosare l’apertura. [Pompe a combustibile con rilassamento e dosaggio congelato](RELAXING_FUEL_BOUNDARIES.it.md)
