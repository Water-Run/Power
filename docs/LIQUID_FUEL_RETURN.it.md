# Ritorno di scarico carburante tracciato

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · **Italiano** · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## Contratto

`liquid_rail_return` collega un'alimentazione a un `hydraulic_relief` unidirezionale esclusivo. La valvola collega il rail alla stessa pressione d'ingresso prescritta della pompa. Registrare ogni percorso; porte incompatibili, proprietà duplicate e percorsi non tracciati sono rifiutati.

`fluid_heat_fraction` sceglie esplicitamente la quota [0,1] delle perdite portata dal carburante di ritorno. Il resto segue il percorso termico dichiarato. Miscelazione simultanea rail/serbatoio conserva massa, chimica, lavoro di pressione e calore. Il ritorno a sorgente esterna porta massa/energia oltre confine.

Leggere `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` e `mass_flow` su ritorno ID 1515. Alimentazione ID 1511 riporta trasferimento lordo della pompa. La circolazione può superare l'inventario iniziale; inventario attuale è iniziale meno pompa più ritorno.

## Evidenze e limiti

`recirculating-liquid-cylinder` e `recirculating-needle-cylinder` conservano combustibile finito, iniezione reale, evaporazione e ago opzionale. v37 salva collegamenti/quota e legge v1-v36. Ogni ritorno aggiunge 8 stati nei limiti invariati.

Decadimento/lavoro indipendenti, raffinamento meccanica/pressione/termica simultaneo, quote, percorsi, confini esterni, replay, rollback e allocazioni passano. Ebollizione o intervallo non risolto fa fallire l'intero lotto.

Serbatoio rigido miscelato, liquido incomprimibile e gas ideale. Sloshing/forma idrostatica, termodinamica delle miscele reali, cavitazione, mappe misurate pompa/valvola, calibrazione OEM e Unity Editor/Play/Player/IL2CPP reale restano aperti. Parametri `unverified`.

[VALIDATION.it.md](VALIDATION.it.md)

## Geometria del serbatoio e spazio gassoso finito

`liquid_fuel_tank.parameters.headspace` dichiara `capacity` in `m3` o `l` e `gas_node`. Il gas omette `storage`: volume `capacity - liquid_mass / density`, un proprietario e volume positivo. Pompa e ritorno usano pressione prescritta nulla: il gas finito determina la pressione d'ingresso.

[Geometria del serbatoio e spazio gassoso finito](TANK_HEADSPACE.it.md)

## Equilibrio di fase del carburante e limiti di alimentazione

L'alimentazione predefinita respinge liquido sotto saturazione. Il serbatoio finito opzionale risolve il vapore sorgente con potenziali corretti. Rail e aspirazione trasportano ancora liquido separato; bolle e flusso cavitante richiedono altra fisica.

Il confronto ammette solo arrotondamento binary64: `64 * epsilon * max(p, p_sat)`, con `epsilon = 2^-52`. Il margine riportato non viene limitato. È un controllo di saturazione globale; perdite locali e NPSH della pompa richiedono un modello di flusso risolto. Le verifiche indipendenti richiedono inoltre entropia totale non decrescente nel flash adiabatico e nello scambio con parete finita.

[Equilibrio di fase del carburante e limiti di alimentazione](FUEL_PHASE_EQUILIBRIUM.it.md)

## Equilibrio del serbatoio con volume liquido finito

Impostare `liquid_fuel_tank.parameters.headspace.phase_equilibrium` a `true` per evaporazione e condensazione del serbatoio. Il film abbinato fornisce proprietà `phase_equilibrium`; il gas tracciato condivide R, cv, potere calorifico e stechiometria. Un serbatoio bagnato dichiara temperatura iniziale comune e pressione parziale satura, uno asciutto parte al massimo a saturazione. La densità totale resta inferiore a quella liquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` e `mass_flow` con segno contano il trasferimento netto per passo fisico. Leggere insieme `vapor_partial_pressure`, `saturation_pressure` corretta, inventario liquido e volume gas. La pompa aspira liquido separato; trasporto bifase, trascinamento di bolle e NPSH locale restano aperti.

[Equilibrio del serbatoio con volume liquido finito](TANK_PHASE_EQUILIBRIUM.it.md)
