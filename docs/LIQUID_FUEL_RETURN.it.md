# Ritorno di scarico carburante tracciato

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · **Italiano** · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## Contratto

`liquid_rail_return` collega un'alimentazione a un `hydraulic_relief` unidirezionale esclusivo. La valvola collega il rail alla stessa pressione d'ingresso prescritta della pompa. Registrare ogni percorso; porte incompatibili, proprietà duplicate e percorsi non tracciati sono rifiutati.

`fluid_heat_fraction` sceglie esplicitamente la quota [0,1] delle perdite portata dal carburante di ritorno. Il resto segue il percorso termico dichiarato. Miscelazione simultanea rail/serbatoio conserva massa, chimica, lavoro di pressione e calore. Il ritorno a sorgente esterna porta massa/energia oltre confine.

Leggere `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` e `mass_flow` su ritorno ID 1515. Alimentazione ID 1511 riporta trasferimento lordo della pompa. La circolazione può superare l'inventario iniziale; inventario attuale è iniziale meno pompa più ritorno.

## Evidenze e limiti

`recirculating-liquid-cylinder` e `recirculating-needle-cylinder` conservano combustibile finito, iniezione reale, evaporazione e ago opzionale. v28 salva collegamenti/quota e legge v1-v27. Ogni ritorno aggiunge 8 stati nei limiti invariati.

Decadimento/lavoro indipendenti, raffinamento meccanica/pressione/termica simultaneo, quote, percorsi, confini esterni, replay, rollback e allocazioni passano. Ebollizione o intervallo non risolto fa fallire l'intero lotto. Geometria/ventilazione, valvole/pompe misurate, cavitazione, spray, calibrazione OEM e Unity Editor/Play/Player/IL2CPP reale restano aperti.

[VALIDATION.it.md](VALIDATION.it.md)
