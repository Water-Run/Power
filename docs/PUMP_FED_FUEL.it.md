# Rail di combustibile liquido alimentato da pompa

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · **Italiano** · [Português](PUMP_FED_FUEL.pt-BR.md)

## Contratto

`liquid_rail_feed` associa un iniettore liquido a una pompa volumetrica esistente e a un confine esplicito di materia/calore. Il nodo di uscita idraulica deve corrispondere alla cedevolezza e alla pressione assoluta iniziale del rail. Pompa e iniettore possiedono questo nodo; altri percorsi fluidi non contabilizzati sono rifiutati.

L'energia di pressione è memorizzata una volta nel nodo idraulico. Portata e reazione d'albero seguono la soluzione accoppiata conservativa. Il combustibile entrante porta energia termica e chimica; lo stoccaggio termico del rail miscela la temperatura. Il flusso inverso con segno restituisce combustibile alla temperatura attuale del rail. Scarico, riscaldamento parete, vapore e combustione prescritta rimangono separati.

`pump-fed-liquid-cylinder` e `pump-fed-needle-cylinder` conservano iniezione fisica e movimento opzionale dell'ago. I KPI di pressione usano un limite dichiarato della sola pompa con unità esplicite. Leggere `total_fuel_delivered`, `reservoir_enthalpy` e `fuel_energy_in` sull'ID 1511; la pompa ID 1510 espone il lavoro reale albero-fluido.

## Evidenze e limiti

v28 conserva collegamenti e temperatura sorgente e legge v1-v27. Scambio analitico albero/pressione, raffinamento ODE simultaneo indipendente, miscelazione termica, bilanci massa/combustibile/energia/volume, ritorno e rollback completo hanno verifiche separate.

Capacità geometrica, ventilazione/spazio gas/oscillazione, cavitazione, riempimento/efficienza/regolazione misurati e spray risolto restano aperti. Parametri `unverified`; Unity Editor/Play/Player/IL2CPP reale e calibrazione OEM non sono verificati.

## Serbatoio finito di combustibile liquido

`liquid_fuel_tank` conserva massa liquida finita ed energia termica con densità, riferimento termico del film e potere calorifico dell'iniettore associato. L'alimentazione lo seleziona con `tank_component` e omette `supply_temperature`. Ogni serbatoio appartiene a un'alimentazione compatibile.

Energia termica e chimica del serbatoio entrano nello stoccaggio totale. Il trasferimento interno non aggiunge massa o energia chimica esterna. La pressione prescritta all'ingresso mantiene il confine di lavoro di pressione. Aspirazione/scarico gas possono ancora trasportare energia chimica.

Capacità geometrica, ventilazione/spazio gas/oscillazione, cavitazione, riempimento/efficienza/regolazione misurati e spray risolto restano aperti. Parametri `unverified`; Unity Editor/Play/Player/IL2CPP reale e calibrazione OEM non sono verificati.

[LIQUID_FUEL_TANK.it.md](LIQUID_FUEL_TANK.it.md)

## Ritorno di scarico carburante tracciato

`liquid_rail_return` collega un'alimentazione a un `hydraulic_relief` unidirezionale esclusivo. La valvola collega il rail alla stessa pressione d'ingresso prescritta della pompa. Registrare ogni percorso; porte incompatibili, proprietà duplicate e percorsi non tracciati sono rifiutati.

`fluid_heat_fraction` sceglie esplicitamente la quota [0,1] delle perdite portata dal carburante di ritorno. Il resto segue il percorso termico dichiarato. Miscelazione simultanea rail/serbatoio conserva massa, chimica, lavoro di pressione e calore. Il ritorno a sorgente esterna porta massa/energia oltre confine.

[LIQUID_FUEL_RETURN.it.md](LIQUID_FUEL_RETURN.it.md)
