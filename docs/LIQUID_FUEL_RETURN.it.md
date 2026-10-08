# Ritorno di scarico carburante tracciato

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · **Italiano** · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## Contratto

`liquid_rail_return` collega un'alimentazione a un `hydraulic_relief` unidirezionale esclusivo. La valvola collega il rail alla stessa pressione d'ingresso prescritta della pompa. Registrare ogni percorso; porte incompatibili, proprietà duplicate e percorsi non tracciati sono rifiutati.

`fluid_heat_fraction` sceglie esplicitamente la quota [0,1] delle perdite portata dal carburante di ritorno. Il resto segue il percorso termico dichiarato. Miscelazione simultanea rail/serbatoio conserva massa, chimica, lavoro di pressione e calore. Il ritorno a sorgente esterna porta massa/energia oltre confine.

Leggere `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` e `mass_flow` su ritorno ID 1515. Alimentazione ID 1511 riporta trasferimento lordo della pompa. La circolazione può superare l'inventario iniziale; inventario attuale è iniziale meno pompa più ritorno.

## Evidenze e limiti

`recirculating-liquid-cylinder` e `recirculating-needle-cylinder` conservano combustibile finito, iniezione reale, evaporazione e ago opzionale. v29 salva collegamenti/quota e legge v1-v28. Ogni ritorno aggiunge 8 stati nei limiti invariati.

Decadimento/lavoro indipendenti, raffinamento meccanica/pressione/termica simultaneo, quote, percorsi, confini esterni, replay, rollback e allocazioni passano. Ebollizione o intervallo non risolto fa fallire l'intero lotto.

Serbatoio rigido miscelato, liquido incomprimibile e gas ideale. Sloshing/forma idrostatica, equilibrio di fase, cavitazione, mappe misurate pompa/valvola, calibrazione OEM e Unity Editor/Play/Player/IL2CPP reale restano aperti. Parametri `unverified`.

[VALIDATION.it.md](VALIDATION.it.md)

## Geometria del serbatoio e spazio gassoso finito

`liquid_fuel_tank.parameters.headspace` dichiara `capacity` in `m3` o `l` e `gas_node`. Il gas omette `storage`: volume `capacity - liquid_mass / density`, un proprietario e volume positivo. Pompa e ritorno usano pressione prescritta nulla: il gas finito determina la pressione d'ingresso.

[Geometria del serbatoio e spazio gassoso finito](TANK_HEADSPACE.it.md)
