# Serbatoio finito di combustibile liquido

[English](LIQUID_FUEL_TANK.md) · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · [Deutsch](LIQUID_FUEL_TANK.de.md) · [Español](LIQUID_FUEL_TANK.es.md) · **Italiano** · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## Contratto

`liquid_fuel_tank` conserva massa liquida finita ed energia termica con densità, riferimento termico del film e potere calorifico dell'iniettore associato. L'alimentazione lo seleziona con `tank_component` e omette `supply_temperature`. Ogni serbatoio appartiene a un'alimentazione compatibile.

La portata positiva è limitata dall'inventario residuo sull'intervallo accettato. La stessa cilindrata effettiva riempita determina reazione d'albero e trasferimento di pressione, conservando lavoro albero/fluido. Rotazione avanti a vuoto non eroga liquido o lavoro fluido; il ritorno con segno miscela nel serbatoio l'energia termica attuale del rail.

Energia termica e chimica del serbatoio entrano nello stoccaggio totale. Il trasferimento interno non aggiunge massa o energia chimica esterna. La pressione prescritta all'ingresso mantiene il confine di lavoro di pressione. Aspirazione/scarico gas possono ancora trasportare energia chimica.

`finite-tank-liquid-cylinder` e `finite-tank-needle-cylinder` usano serbatoio ID 1513 e alimentazione ID 1511. Leggere `mass`, `temperature`, `internal_energy`, `chemical_energy` e `tank_state`; 0 significa liquido e 1 vuoto. La temperatura asciutta riporta il riferimento iniziale dichiarato.

## Evidenze e limiti

v27 conserva serbatoio e selezione e legge v1-v26. Ogni serbatoio aggiunge 4 stati nei limiti invariati. Scambio umido indipendente, pressione/energia d'albero esaurite analitiche, miscela di ritorno, bilanci completi, rollback, rami e passi senza allocazioni sono verificati.

Capacità geometrica, ventilazione/spazio gas/oscillazione, cavitazione, riempimento/efficienza/regolazione misurati e spray risolto restano aperti. Parametri `unverified`; Unity Editor/Play/Player/IL2CPP reale e calibrazione OEM non sono verificati.

[VALIDATION.it.md](VALIDATION.it.md)
