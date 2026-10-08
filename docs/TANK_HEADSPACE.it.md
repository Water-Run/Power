# Geometria del serbatoio e spazio gassoso finito

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · **Italiano** · [Português](TANK_HEADSPACE.pt-BR.md)

## Contratto

`liquid_fuel_tank.parameters.headspace` dichiara `capacity` in `m3` o `l` e `gas_node`. Il gas omette `storage`: volume `capacity - liquid_mass / density`, un proprietario e volume positivo. Pompa e ritorno usano pressione prescritta nulla: il gas finito determina la pressione d'ingresso.

Il solver accoppiato scambia lavoro di pressione fra albero, rail e gas senza fonte esterna. Orifizi gassosi e collegamenti termici forniscono sfiato/calore espliciti. Leggere `pressure`, `fill_fraction`, `hydraulic_work` cumulativo con segno e massa, energia, volume del gas. Dose, liquido, evaporazione e combustione restano separati.

## Evidenza e limiti

`vented-tank-liquid-cylinder` e `vented-tank-needle-cylinder` usano serbatoio 1513, gas 1520 e ingresso sfiato 960. Asset v29 conserva geometria e legge v1-v28. Passano lavoro/derivate analitici, convergenza ODE indipendente, bilanci, replay portable/MCP, rollback e passi senza allocazioni.

Serbatoio rigido miscelato, liquido incomprimibile e gas ideale. Sloshing/forma idrostatica, equilibrio di fase, cavitazione, mappe misurate pompa/valvola, calibrazione OEM e Unity Editor/Play/Player/IL2CPP reale restano aperti. Parametri `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.it.md)
