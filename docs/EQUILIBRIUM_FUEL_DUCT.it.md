# Condotti di combustibile comprimibile in equilibrio

[English](EQUILIBRIUM_FUEL_DUCT.md) · [简体中文](EQUILIBRIUM_FUEL_DUCT.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_DUCT.fr.md) · [Русский](EQUILIBRIUM_FUEL_DUCT.ru.md) · [日本語](EQUILIBRIUM_FUEL_DUCT.ja.md) · [한국어](EQUILIBRIUM_FUEL_DUCT.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_DUCT.de.md) · [Español](EQUILIBRIUM_FUEL_DUCT.es.md) · **Italiano** · [Português](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

## Modello e materiale

Un `equilibrium_fuel_duct` trasporta massa, quantità di moto ed energia totale attraverso celle esplicite `compressible_fuel`. Il liquido è comprimibile e il vapore ideale; le due fasi condividono pressione, temperatura e velocità. L’equilibrio locale permette condensazione e vaporizzazione istantanea. Due serbatoi finiti fermi e una parete termica finita chiudono i bilanci.

Una `compressible_fuel_chamber` può cambiare il volume di entrambi i serbatoi miscelati tramite lavoro d’albero. La velocità media resta nulla e le celle interne rigide. L’impulso stazionario di porte/carter non compie lavoro; il lavoro del pistone è uno scambio meccanico separato. [Camere di combustibile comprimibile azionate da albero](COMPRESSIBLE_FUEL_CHAMBER.it.md)

Il liquido ha dilatazione termica nulla e calore specifico costante. `K` è il modulo volumetrico a pressione zero; il valore effettivo è `K+p`. Energia di compressione fredda, potenziali chimici, entropia e velocità del suono all’equilibrio derivano dallo stesso potenziale. `a` è lo scostamento energetico liquido esistente, `p0(T)` la curva di saturazione che trascura il volume liquido, `x` la frazione massica di vapore e `Cv_eq` la derivata dell’energia interna della miscela a volume specifico costante. `Tr`, `Pref` e `Lu_ref` sono temperatura, pressione ed energia interna latente di riferimento. Si usano unità SI.

```text
p_l = K (rho_l/rho0 - 1); y = p/K
v_l = 1/[rho0 (1+y)]
u_cold = (K/rho0) [ln(1+y) - y/(1+y)]
u_l = cl T + a + u_cold
h_l = cl T + a + (K/rho0) ln(1+y)
s_l = cl ln(T/Tr)
u_v = cv T; v_v = R T/p; h_v = (cv+R) T
s_v = (cv+R) ln(T/Tr) - R ln(p/Pref) + Lu_ref/Tr + R
R T ln[p_sat/p0(T)] = (K/rho0) ln(1+p_sat/K)
v = (1-x) v_l + x v_v; u = (1-x) u_l + x u_v
dp_sat/dT = (h_v-h_l)/[T(v_v-v_l)]
c_eq^2 = v^2 T (dp_sat/dT)^2 / Cv_eq
c_l^2 = K/rho0; c_v^2 = (1+R/cv) R T
```

## Trasporto e confini

Dichiarare `liquid_bulk_modulus`, omettere `compliance` e fornire esplicitamente `initial_velocity` per ogni nodo. Dichiarare 1–24 `cells` interne ordinate e distinte, ciascuna appartenente a un solo condotto. La lunghezza è il volume diviso per `area`. I serbatoi hanno velocità nulla e non possono essere celle interne. Tutte le porte condividono il materiale. `linear_resistance` e `quadratic_resistance` descrivono la resistenza totale, distribuita per volume; `fluid_heat_fraction` ripartisce il calore d’attrito tra fluido e parete. Non esiste un ingresso diretto di apertura.

```text
U = [rho, rho velocity, rho (u + velocity^2/2)]
F = [rho velocity, rho velocity^2+p, (rho (u+velocity^2/2)+p) velocity]
F_Rusanov = (F_left+F_right)/2 - a_max (U_right-U_left)/2
a_max = max_ports(abs(velocity)) + sqrt(max(K/rho0, (1+R/cv) R Tmax))
```

Il flusso conservativo di Rusanov aggiorna tutte le facce dallo stesso stato di sottopasso. Il limite di velocità somma la massima velocità assoluta e il limite acustico del materiale. Il vincolo CFL è 0.2 volte il volume minimo delle porte diviso per area e velocità; ogni intervallo ammette al massimo 4096 sottopassi. Trasporto e decadimento locale esatto per attrito sono separati al primo ordine. La diffusione numerica contribuisce alla variazione totale di entropia, distinta dal calore d’attrito riportato. I confini fermi assorbono quantità di moto senza lavoro meccanico; l’energia cinetica entrante si termalizza nell’inventario finito. Stati invalidi o diminuzione d’entropia oltre l’arrotondamento rifiutano l’intero lotto. Ridurre il passo e controllare limiti del materiale e risoluzione.

## Uso e verifiche

Eseguire `equilibrium-fuel-condensing-duct` o `equilibrium-fuel-flashing-duct`. Ogni cella espone densità, velocità, frazione di fase, velocità del suono, temperatura ed energia. Sono osservabili massa/energia alle porte, impulso della parete e inventari completi. Le verifiche indipendenti coprono lavoro di compressione, limite del vapore ideale, uguaglianza dei potenziali chimici, relazioni di Clapeyron e Gibbs, suono tramite inversione dell’entropia, inversione massa-energia, conservazione e attrito con parete finita. Gli errori acustici medi di cella diminuiscono con 8, 16 e 24 celle. Riproduzione esatta, rollback, annullamento, rami, passi senza allocazioni, asset portabili e MCP reale sono verificati separatamente; i risultati dettagliati sono nelle prove di validazione.

[VALIDATION.it.md](VALIDATION.it.md)

## Validità e prossimi passi

I parametri restano `unverified`. Rispettare gli intervalli dichiarati di temperatura e pressione, entalpia latente positiva, volumi di coesistenza separati e vapore diluito. Il condotto è rigido, orizzontale, a sezione costante e con equilibrio omogeneo istantaneo. Esclude nucleazione a velocità finita, scorrimento tra fasi, tensione superficiale, gravità, elasticità e comportamento critico reale. La diffusione di primo ordine può allargare i fronti; conservazione e riproduzione non dimostrano accuratezza spaziale o validità hardware misurata. L’accettazione Unity effettiva è ancora aperta.

Aggiungere confini compatibili di valvola direzionale e ago/ugello comprimibili con entalpia, energia cinetica e ipotesi di fase esplicite. Proseguire con rilassamento/scorrimento, accensione, aspirazione/scarico, perdite meccaniche e coordinamento ECU/TCU. Calibrazione misurata e accettazione Unity effettiva restano separate.

[NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf) · [MFEM: Rusanov flux](https://docs.mfem.org/html/classmfem_1_1RusanovFlux.html) · [Modelica: conservation balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html)
