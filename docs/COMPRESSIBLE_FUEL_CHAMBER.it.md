# Camere di combustibile comprimibile azionate da albero

[English](COMPRESSIBLE_FUEL_CHAMBER.md) · [简体中文](COMPRESSIBLE_FUEL_CHAMBER.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_CHAMBER.fr.md) · [Русский](COMPRESSIBLE_FUEL_CHAMBER.ru.md) · [日本語](COMPRESSIBLE_FUEL_CHAMBER.ja.md) · [한국어](COMPRESSIBLE_FUEL_CHAMBER.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_CHAMBER.de.md) · [Español](COMPRESSIBLE_FUEL_CHAMBER.es.md) · **Italiano** · [Português](COMPRESSIBLE_FUEL_CHAMBER.pt-BR.md)

## Contratto fisico

Una `compressible_fuel_chamber` collega un albero a un serbatoio finito `compressible_fuel`. Il movimento cambia il volume; lo stesso lavoro di pressione cambia energia del fluido e coppia. Il serbatoio può alimentare o ricevere un `equilibrium_fuel_duct`, con vaporizzazione e condensazione all’equilibrio.

`node_a` è rotazionale e `node_b` ha un solo proprietario geometrico. Dichiarare `minimum_reference_volume`, `swept_volume` e `phase`; il volume iniziale deve corrispondere all’angolo iniziale. Il serbatoio miscelato ha velocità media nulla e non può essere una cella interna. Il meccanismo sinusoidale senza massa non aggiunge inerzia di pistone o camma; l’inerzia d’albero è esplicita. La pressione esterna del carter è nulla.

## Lavoro e trasporto

Il passo di lavoro chiuso mantiene massa ed entropia. Compressione fredda ed energia calorica usano lo stesso potenziale d’equilibrio del condotto. Il liquido puro usa l’integrale esatto della pressione, il vapore ideale l’adiabatica analitica. Il cambiamento di fase inverte l’entropia e verifica l’energia finale. La differenza accettata fornisce una coppia discreta nel solutore meccanico comune. `hydraulic_work` positivo significa energia dall’albero al fluido.

```text
V(theta) = Vmin + Vs sin^2((theta-phase)/2)
dE_fluid = -p dV; dM = 0; dS = 0
tau_pressure = -Delta_E_fluid / Delta_theta
tau_pressure(Delta_theta=0) = p dV/dtheta
Delta_E_shaft + Delta_E_fluid = 0
p_liquid = K (M/(rho0 V)-1)
Delta_E_liquid = K [Delta_V - (M/rho0) ln(V_next/V_old)]
T_vapor_next = T_old (V_old/V_next)^(R/cv)
Delta_E_vapor = M cv (T_next-T_old)
```

Il serbatoio mobile usa mezzo trasporto, lavoro completo d’albero e mezzo trasporto. Il trasporto resta al primo ordine con diffusione numerica; la separazione simmetrica non alza l’ordine. Il moto liquido chiuso e regolare converge al secondo ordine. Gli incrementi angolari sono limitati a 0.25 rad per intervallo meccanico. Restano validi CFL, limiti materiali, rollback completo, annullamento e rami indipendenti. L’impulso stazionario di porte/carter è distinto dal lavoro del pistone.

## Uso e verifiche

Eseguire `compressible-fuel-chamber-duct` o `electric-compressible-fuel-duct`. Leggere massa, volume, pressione, temperatura, entropia e frazione di vapore insieme a celle, coppia, potenza e lavoro. Le prove usano lavoro liquido e limiti del vapore indipendenti, isentropica d’equilibrio invertita separatamente e quadratura della pressione, ODE dell’albero liquido e raffinamento RK4 delle equazioni semidiscrete del vapore ideale. Conservazione, composizione elettrica/ingranaggi/idraulica/frizione, riproduzione portabile, MCP reale e transazioni senza allocazioni sono verificati separatamente. I risultati sono nelle prove di validazione.

[VALIDATION.it.md](VALIDATION.it.md)

## Validità e lavoro restante

I parametri restano `unverified`. Il puro pseudofluido d’equilibrio comprende liquido comprimibile senza dilatazione termica e vapore ideale, camera miscelata e celle rigide. Rispettare i limiti termici e di pressione. La camera non risolve energia cinetica media e non include una valvola di ritegno automatica. Nucleazione a velocità finita, scorrimento tra fasi, comportamento critico reale, calibrazione misurata e accettazione Unity effettiva restano aperti.

Aggiungere confini compatibili di valvola direzionale e ago/ugello comprimibili con entalpia, energia cinetica e ipotesi di fase esplicite. Proseguire con rilassamento/scorrimento, accensione, aspirazione/scarico, perdite meccaniche e coordinamento ECU/TCU. Calibrazione misurata e accettazione Unity effettiva restano separate.

[Modelica: swept volume and pressure work](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [NIST: consistent thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html)
