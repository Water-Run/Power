# Serbatoio finito di combustibile liquido

[English](LIQUID_FUEL_TANK.md) · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · [Deutsch](LIQUID_FUEL_TANK.de.md) · [Español](LIQUID_FUEL_TANK.es.md) · **Italiano** · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## Contratto

`liquid_fuel_tank` conserva massa liquida finita ed energia termica con densità, riferimento termico del film e potere calorifico dell'iniettore associato. L'alimentazione lo seleziona con `tank_component` e omette `supply_temperature`. Ogni serbatoio appartiene a un'alimentazione compatibile.

La portata positiva è limitata dall'inventario residuo sull'intervallo accettato. La stessa cilindrata effettiva riempita determina reazione d'albero e trasferimento di pressione, conservando lavoro albero/fluido. Rotazione avanti a vuoto non eroga liquido o lavoro fluido; il ritorno con segno miscela nel serbatoio l'energia termica attuale del rail.

Energia termica e chimica del serbatoio entrano nello stoccaggio totale. Il trasferimento interno non aggiunge massa o energia chimica esterna. La pressione prescritta all'ingresso mantiene il confine di lavoro di pressione. Aspirazione/scarico gas possono ancora trasportare energia chimica.

`finite-tank-liquid-cylinder` e `finite-tank-needle-cylinder` usano serbatoio ID 1513 e alimentazione ID 1511. Leggere `mass`, `temperature`, `internal_energy`, `chemical_energy` e `tank_state`; 0 significa liquido e 1 vuoto. La temperatura asciutta riporta il riferimento iniziale dichiarato.

## Geometria del serbatoio e spazio gassoso finito

`liquid_fuel_tank.parameters.headspace` dichiara `capacity` in `m3` o `l` e `gas_node`. Il gas omette `storage`: volume `capacity - liquid_mass / density`, un proprietario e volume positivo. Pompa e ritorno usano pressione prescritta nulla: il gas finito determina la pressione d'ingresso.

[Geometria del serbatoio e spazio gassoso finito](TANK_HEADSPACE.it.md)

## Evidenze e limiti

v29 conserva serbatoio e selezione e legge v1-v28. Ogni serbatoio aggiunge 4 stati nei limiti invariati. Scambio umido indipendente, pressione/energia d'albero esaurite analitiche, miscela di ritorno, bilanci completi, rollback, rami e passi senza allocazioni sono verificati.

Serbatoio rigido miscelato, liquido incomprimibile e gas ideale. Sloshing/forma idrostatica, equilibrio di fase, cavitazione, mappe misurate pompa/valvola, calibrazione OEM e Unity Editor/Play/Player/IL2CPP reale restano aperti. Parametri `unverified`.

[VALIDATION.it.md](VALIDATION.it.md)

## Ritorno di scarico carburante tracciato

`liquid_rail_return` collega un'alimentazione a un `hydraulic_relief` unidirezionale esclusivo. La valvola collega il rail alla stessa pressione d'ingresso prescritta della pompa. Registrare ogni percorso; porte incompatibili, proprietà duplicate e percorsi non tracciati sono rifiutati.

`fluid_heat_fraction` sceglie esplicitamente la quota [0,1] delle perdite portata dal carburante di ritorno. Il resto segue il percorso termico dichiarato. Miscelazione simultanea rail/serbatoio conserva massa, chimica, lavoro di pressione e calore. Il ritorno a sorgente esterna porta massa/energia oltre confine.

[LIQUID_FUEL_RETURN.it.md](LIQUID_FUEL_RETURN.it.md)
