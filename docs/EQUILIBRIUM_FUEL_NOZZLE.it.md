# Transito di combustibile all’equilibrio negli ugelli

[English](EQUILIBRIUM_FUEL_NOZZLE.md) · [简体中文](EQUILIBRIUM_FUEL_NOZZLE.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NOZZLE.fr.md) · [Русский](EQUILIBRIUM_FUEL_NOZZLE.ru.md) · [日本語](EQUILIBRIUM_FUEL_NOZZLE.ja.md) · [한국어](EQUILIBRIUM_FUEL_NOZZLE.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NOZZLE.de.md) · [Español](EQUILIBRIUM_FUEL_NOZZLE.es.md) · **Italiano** · [Português](EQUILIBRIUM_FUEL_NOZZLE.pt-BR.md)

## Modello fisico

Impostare `phase_mode` di `compressible_fuel_nozzle` su `equilibrium` per l’equilibrio omogeneo istantaneo durante il transito. Liquido e vapore condividono pressione, temperatura e velocità; il titolo cambia lungo l’isentropica. Ugello e serbatoi finiti usano lo stesso potenziale liquido comprimibile e riferimento di vapore ideale. `frozen` resta un’ipotesi distinta supportata.

Pressione ed entropia specifica determinano liquido stabile, vapore o coesistenza. La chiusura usa uguaglianza dei potenziali chimici e limiti termici e di pressione esistenti. Gli estremi termici ammettono solo vicinati d’arrotondamento binary64. Area, calore, comando e ago reale seguono il contratto comune. Il getto si espande idealmente al ricevitore; l’impulso stazionario non aggiunge forza assiale o lavoro meccanico.

## Involucro globale del flusso

Il quadrato positivo del suono d’equilibrio dà densità crescente con pressione a entropia fissa; il salto d’entalpia diminuisce. I fattori monotoni limitano il flusso di ogni intervallo, incluse frontiere di fase ed estremi multipli. La separazione e valutazione restituisce limite inferiore campionato e superiore residuo con ampiezza relativa massima 1e-4. Limiti: 512 intervalli sullo stack e 8192 divisioni. Differenze caloriche usano margini d’arrotondamento e limiti integrali monotoni fino a 256 pannelli Riemann e 80 iterazioni. Casi irrisolti rifiutano il lotto. Un massimo interno regolare è sonico; uno alla frontiera può essere non regolare.

```text
g_liquid(T,p) = g_vapor(T,p)
s(p,rho,T,x) = s_source
D(p) = h_source - h(p,s_source) = integral(p,p_source) v(q,s_source) dq
G(p) = rho(p,s_source) sqrt(2 D(p))
drho/dp at fixed s = 1/c_equilibrium^2 > 0
G_upper([p_low,p_high]) = rho(p_high) sqrt(2 D_upper(p_low))
G_lower = max(sampled rho(p) sqrt(2 D_lower(p)))
flux_relative_uncertainty = G_upper/G_lower - 1 <= 1e-4
mass_flow_local = opening Cd A G_lower
velocity_throat = c_equilibrium  (smooth interior maximum only)
```

Sottopassi conservativi del primo ordine trasferiscono la stessa massa ed entalpia e dividono l’energia cinetica fra fluido e parete. Si verificano stati ed entropia totale della coppia. Limiti: 4096 sottopassi, 64 dimezzamenti per candidato, trasferimento iniziale entro 0.1 della massa minore. La chiusura tollera 128 epsilon binary64 per il massimo modulo volumetrico o pressione senza limitare i valori riportati. Un errore annulla l’intero lotto. Controllare passo, inventario, limiti, corsa e area.

## Uso e verifiche

Eseguire `equilibrium-flashing-nozzle` o `equilibrium-pump-needle`. `flux_relative_uncertainty` descrive l’ultimo involucro costitutivo locale accettato e non limita portata media, massa consegnata, integrazione temporale del primo ordine o calibrazione. Si confrontano inversione indipendente di saturazione/entropia, blocco analitico gas/liquido, ricerca globale densa separata, identità energetiche/entropiche e raffinamento analitico dello svuotamento di vapore. Inventari, albero/condotto/ago, rollback, annullamento, rami, allocazioni, asset e MCP reale sono verificati separatamente. Numeri e vecchi confini restano nelle prove di validazione.

[VALIDATION.it.md](VALIDATION.it.md)

## Validità e prossimi passi

I parametri restano `unverified`. Il limite d’equilibrio usa pseudofluido puro, vapore ideale, dilatazione liquida nulla e velocità comune. Esclude tempi finiti di nucleazione/rilassamento, scorrimento, spray, gas portante e ricevitore con combustione. L’involucro usa margini binary64 dichiarati, non una prova formale d’intervalli con arrotondamento diretto. Interpretare la gola vicino alle transizioni insieme all’involucro. Validità misurata e Unity effettivo restano aperti.

Aggiungere rilassamento finito con prove esplicite di residenza e materiale, poi spray risolto e ricevitore con combustione compatibile. Continuare accensione, aspirazione/scarico, perdite meccaniche, attuazione trasmissione ed ECU/TCU. Calibrazione e accettazione Unity restano separate.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
