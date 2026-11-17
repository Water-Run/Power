# Ugelli di combustibile comprimibile a titolo congelato

[English](COMPRESSIBLE_FUEL_NOZZLE.md) · [简体中文](COMPRESSIBLE_FUEL_NOZZLE.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_NOZZLE.fr.md) · [Русский](COMPRESSIBLE_FUEL_NOZZLE.ru.md) · [日本語](COMPRESSIBLE_FUEL_NOZZLE.ja.md) · [한국어](COMPRESSIBLE_FUEL_NOZZLE.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_NOZZLE.de.md) · [Español](COMPRESSIBLE_FUEL_NOZZLE.es.md) · **Italiano** · [Português](COMPRESSIBLE_FUEL_NOZZLE.pt-BR.md)

## Contratto fisico

Un `compressible_fuel_nozzle` porta flusso unidirezionale tra serbatoi finiti `compressible_fuel`, incluse camere azionate da albero. Compressione liquida e vapore ideale condividono il materiale. Dichiarare `phase_mode` come `frozen`: il titolo massico resta costante nel transito, mentre i serbatoi si riequilibrano.

Dichiarare area, coefficiente di area efficace e `wall_kinetic_fraction`, con parete termica finita. `initial_input` è apertura in [0,1], modificabile da un canale opzionale. Gli opzionali `needle_node`, `closed_position` e `full_open_position` moltiplicano il comando per l’apertura reale dell’ago traslazionale. Le porte hanno velocità media nulla, materiale e potere calorifico uguali e non sono celle interne. La pressione inversa chiude il confine ideale a soglia nulla.

## Energia e blocco sonico

Le fasi condividono pressione, temperatura e velocità; il trasferimento massico di fase è soppresso. Entropia ed energia di compressione fredda danno il salto isentropico d’entalpia. Il massimo unico di flusso usa il suono congelato, diverso da quello d’equilibrio. Il coefficiente scala l’area efficace. Al blocco, l’energia d’uscita descrive un getto ideale completamente espanso alla pressione ricevente. L’entalpia di ristagno paga una sola volta energia ricevente e calore esplicito di parete. L’impulso misura la termalizzazione stazionaria, non la spinta totale del carter né la forza assiale dell’ago.

```text
cp_f = (1-x) cl + x (cv+R); alpha = x R/cp_f
T(p) = T0 (p/p0)^alpha
v(p) = (1-x)/[rho0 (1+p/K)] + x R T(p)/p
Delta_s = cp_f ln(T/T0) - x R ln(p/p0) = 0
D(p) = h0-h(p)
     = cp_f T0 [1-(p/p0)^alpha] + (1-x) K/rho0 ln[(K+p0)/(K+p)]
G(p) = sqrt(2 D(p))/v(p)
c_frozen^2 = -v(p)^2 / (dv/dp at fixed s,x)
F(p) = v(p)^2 + 2 D(p) v'(p); F'(p) = 2 D(p) v''(p) > 0
G_throat = max(G(p), p_receiver <= p <= p0)
mass_flow = opening Cd A G_throat
Delta_E_A = -Delta_m h0
Delta_E_B = Delta_m h0 - xi Delta_m D(p_receiver)
Delta_E_wall = xi Delta_m D(p_receiver)
```

Sottopassi conservativi del primo ordine trasferiscono la stessa massa ed entalpia e dividono l’energia cinetica fra fluido e parete. Si verificano stati ed entropia totale della coppia. Limiti: 4096 sottopassi, 64 dimezzamenti per candidato, trasferimento iniziale entro 0.1 della massa minore. La chiusura tollera 128 epsilon binary64 per il massimo modulo volumetrico o pressione senza limitare i valori riportati. Un errore annulla l’intero lotto. Controllare passo, inventario, limiti, corsa e area.

## Uso e verifiche

Eseguire `compressible-fuel-nozzle` per lo scarico finito o `compressible-pump-needle` per condotto, albero, ingresso unidirezionale e ago a solenoide. Leggere massa, entalpia, energia cinetica, calore, impulso ed entropia cumulati. Portata e forza medie coprono il tick intero; gola, uscita e titolo mantengono l’ultimo transito accettato. Si confrontano lavoro liquido esatto, blocco analitico gas/liquido, quadratura indipendente, entropia e derivata sonora congelate, scansione densa e raffinamento dello svuotamento critico analitico. Conservazione, moto reale, riproduzione, rami, rollback, annullamento, allocazioni, asset e MCP sono verificati separatamente.

[VALIDATION.it.md](VALIDATION.it.md)

## Validità e prossimi passi

I parametri restano `unverified`. Il puro pseudofluido usa vapore ideale e liquido comprimibile senza dilatazione termica. Rispettare limiti di pressione e temperatura, incluso il getto ideale. Il transito congelato può mantenere liquido surriscaldato senza predire limiti misurati di nucleazione o metastabilità. Rilassamento finito, scorrimento, spray risolto, gas portante non condensabile, ricevitore con combustione, mappe misurate e Unity reale restano aperti. Conservazione e riproduzione non dimostrano validità hardware.

Estendere il transito a modelli di fase d’equilibrio e a velocità finita, poi aggiungere spray risolto e ricevitore con combustione compatibile. Continuare accensione, aspirazione/scarico, perdite meccaniche, attuazione trasmissione e coordinamento ECU/TCU. Calibrazione misurata e accettazione Unity restano separate.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
