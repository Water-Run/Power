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

I parametri restano `unverified`. Dichiarare `initial_vapor_mass_fraction` in [0,1] e un tempo intero da 1 ns a un’ora. Rispettare temperatura e pressione positiva del materiale. Si assumono condotto rigido orizzontale a sezione costante, pseudofluido puro, dilatazione termica liquida nulla e vapore ideale. Mancano scorrimento fra le fasi, ritardo termico interfacciale, tensione superficiale, spray e gas vettore. Restano aperti gli accoppiamenti diretti del dominio rilassante con pompe, ugelli, camere mobili e ricevitori in combustione. Conservazione, riproduzione e raffinamento non attestano validità hardware misurata né accettazione reale Unity. Usare vapore diluito lontano dalla regione critica reale; il materiale deve mantenere entalpia latente positiva e volumi specifici di coesistenza distinti.

Estendere le condizioni al contorno compatibili con velocità finita e ottenere misure di rilassamento del materiale, poi aggiungere spray risolto e ricevitori in combustione. Continuare accensione, aspirazione/scarico, perdite meccaniche, attuazione della trasmissione e coordinamento ECU/TCU. Calibrazione misurata e accettazione reale Unity restano separate.

[VALIDATION.it.md](VALIDATION.it.md)
