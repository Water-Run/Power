# Geometria del serbatoio e spazio gassoso finito

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · **Italiano** · [Português](TANK_HEADSPACE.pt-BR.md)

## Contratto

`liquid_fuel_tank.parameters.headspace` dichiara `capacity` in `m3` o `l` e `gas_node`. Il gas omette `storage`: volume `capacity - liquid_mass / density`, un proprietario e volume positivo. Pompa e ritorno usano pressione prescritta nulla: il gas finito determina la pressione d'ingresso.

Il solver accoppiato scambia lavoro di pressione fra albero, rail e gas senza fonte esterna. Orifizi gassosi e collegamenti termici forniscono sfiato/calore espliciti. Leggere `pressure`, `fill_fraction`, `hydraulic_work` cumulativo con segno e massa, energia, volume del gas. Dose, liquido, evaporazione e combustione restano separati.

## Evidenza e limiti

`vented-tank-liquid-cylinder` e `vented-tank-needle-cylinder` usano serbatoio 1513, gas 1520 e ingresso sfiato 960. Asset v36 conserva geometria e legge v1-v35. Passano lavoro/derivate analitici, convergenza ODE indipendente, bilanci, replay portable/MCP, rollback e passi senza allocazioni.

Serbatoio rigido miscelato, liquido incomprimibile e gas ideale. Sloshing/forma idrostatica, termodinamica delle miscele reali, cavitazione, mappe misurate pompa/valvola, calibrazione OEM e Unity Editor/Play/Player/IL2CPP reale restano aperti. Parametri `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.it.md)

## Equilibrio del serbatoio con volume liquido finito

Impostare `liquid_fuel_tank.parameters.headspace.phase_equilibrium` a `true` per evaporazione e condensazione del serbatoio. Il film abbinato fornisce proprietà `phase_equilibrium`; il gas tracciato condivide R, cv, potere calorifico e stechiometria. Un serbatoio bagnato dichiara temperatura iniziale comune e pressione parziale satura, uno asciutto parte al massimo a saturazione. La densità totale resta inferiore a quella liquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` e `mass_flow` con segno contano il trasferimento netto per passo fisico. Leggere insieme `vapor_partial_pressure`, `saturation_pressure` corretta, inventario liquido e volume gas. La pompa aspira liquido separato; trasporto bifase, trascinamento di bolle e NPSH locale restano aperti.

[Equilibrio del serbatoio con volume liquido finito](TANK_PHASE_EQUILIBRIUM.it.md)
