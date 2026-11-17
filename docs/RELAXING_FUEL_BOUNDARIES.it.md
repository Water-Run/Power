# Pompe a combustibile con rilassamento e dosaggio congelato

[English](RELAXING_FUEL_BOUNDARIES.md) · [简体中文](RELAXING_FUEL_BOUNDARIES.zh-CN.md) · [Français](RELAXING_FUEL_BOUNDARIES.fr.md) · [Русский](RELAXING_FUEL_BOUNDARIES.ru.md) · [日本語](RELAXING_FUEL_BOUNDARIES.ja.md) · [한국어](RELAXING_FUEL_BOUNDARIES.ko.md) · [Deutsch](RELAXING_FUEL_BOUNDARIES.de.md) · [Español](RELAXING_FUEL_BOUNDARIES.es.md) · **Italiano** · [Português](RELAXING_FUEL_BOUNDARIES.pt-BR.md)

## Modello fisico

Un `compressible_fuel_chamber` può controllare il volume di un serbatoio `relaxing_fuel`. Il lavoro reversibile dell’albero mantiene fissa la frazione di vapore; il rilassamento chimico è una sorgente separata che conserva energia. Un `compressible_fuel_nozzle` collega due serbatoi rilassanti con `phase_mode` su `frozen`, trasportando massa totale, vapore ed entalpia di ristagno. Un ago fisico con bobina, molla e battuta può dosare l’apertura.

[NASA: isentropic ideal-gas relations](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Accoppiamento e calcolo numerico

La relazione entropica a frazione fissa determina pressione e temperatura dopo la variazione di volume. Il lavoro include compressione fredda del liquido e funzioni incrementali stabili per piccoli movimenti. Lo stesso lavoro discreto determina la coppia all’albero. Ogni intervallo globale applica una volta la sorgente di ogni nodo attorno al trasporto conservativo in condotto e ugello congelato. CFL e 0.02 dell’inventario minore alle porte limitano gli intervalli, al massimo 4096. Lavoro e sorgente restano separati. Il trasporto accoppiato rimane del primo ordine e richiede raffinamento temporale.

```text
x = vapor_mass / total_mass
cp_x = (1-x) c_liquid + x (c_vapor+R)
cv_x = (1-x) c_liquid + x c_vapor
alpha = x R/cp_x
T/T0 = (p/p0)^alpha                         (fixed quality and entropy)
v(p) = (1-x)/(rho0 (1+p/K)) + x R T/p
work_on_fluid = mass [cv_x T0 expm1(alpha ln(p/p0)) + (1-x) delta_u_cold]
fluid_torque = -work_on_fluid/delta_angle   (nonzero angular increment)
delta_vapor_mass_volume_work = 0
delta_vapor_mass_nozzle = transferred_total_mass * source_vapor_quality
donor_delta_energy = -transferred_total_mass * stagnation_enthalpy
receiver_delta_energy = -donor_delta_energy - wall_fraction * jet_kinetic_energy
dx/dt_phase_source = (x_equilibrium(rho,u)-x)/tau
```

## Uso, prove e limiti

Eseguire `relaxing-fuel-chamber` o `relaxing-pump-needle`. Osservare volume/lavoro, frazioni effettiva e di equilibrio, entropia di fase, `total_vapor_delivered`, inventari alle porte e residuo globale di vapore. Si confrontano adiabatiche pure, quadratura indipendente della pressione a frazione fissa, lavoro infinitesimo, ciclo reversibile e ODE indipendente dell’albero con vapore ideale. Raffinamento accoppiato, tempo di sorgente condiviso, bilanci completi, annullamento, rollback, rami, allocazioni, riproduzione portatile e MCP reale sono verificati separatamente. I numeri sono nelle prove di validazione.

I parametri restano `unverified`. Entrambe le porte richiedono stesso dominio e materiale; quelle rilassanti rifiutano il transito di equilibrio. Il rilassamento finito nei serbatoi non risolve chimica o permanenza dentro un ugello senza volume. Mancano scorrimento delle fasi, tensione liquida negativa, barriera di nucleazione, spray, gas vettore e ricevitore in combustione. I serbatoi mobili sono miscelati con velocità media nulla; le celle interne restano rigide con un proprietario. Usare vapore ideale diluito lontano dal critico, limiti dichiarati di pressione positiva e temperatura, entalpia latente positiva e volumi di coesistenza separati. La frazione iniziale è un ingresso di ricerca esplicito. L’accordo numerico non attesta calibrazione misurata né accettazione Unity reale.

Risolvere la permanenza finita nell’ugello con prove sul materiale, poi aggiungere spray risolto e ricevitori in combustione compatibili. Continuare accensione, aspirazione/scarico, perdite meccaniche, attuazione trasmissione e coordinamento ECU/TCU. Calibrazione misurata e accettazione Unity restano separate.

[VALIDATION.it.md](VALIDATION.it.md)
