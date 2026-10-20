# Геометрия бака и конечное газовое пространство

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · **Русский** · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## Контракт

`liquid_fuel_tank.parameters.headspace` задаёт `capacity` в `m3` или `l` и `gas_node`. Узел газа не задаёт `storage`: объём равен `capacity - liquid_mass / density`, имеет одного владельца и остаётся положительным. Заданное давление насоса и возврата равно нулю: конечный газ определяет давление входа.

Совместный решатель обменивает работу давления между валом, рампой и газом без внешнего источника давления. Газовые отверстия и тепловые связи задают явную вентиляцию/теплообмен. Читать `pressure`, `fill_fraction`, знаковую накопленную `hydraulic_work` и массу, энергию, объём газа. Доза, жидкость, испарение и горение разделены.

## Доказательства и ограничения

Примеры `vented-tank-liquid-cylinder` и `vented-tank-needle-cylinder` используют бак 1513, газ 1520 и вход вентиляции 960. Asset v31 хранит геометрию и читает v1-v30. Проходят аналитические работа/производные, независимая сходимость ODE, балансы, portable/MCP воспроизведение, откат и шаг без аллокаций.

Жёсткий смешанный бак, несжимаемая жидкость и идеальный газ. Плеск/гидростатическая форма, термодинамика реальных смесей, кавитация, измеренные карты насоса/клапанов, OEM калибровка и реальная Unity Editor/Play/Player/IL2CPP остаются открытыми. Параметры `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.ru.md)

## Равновесие топлива в баке конечного объёма

Задать `liquid_fuel_tank.parameters.headspace.phase_equilibrium` как `true` для испарения и конденсации в баке. Связанная плёнка задаёт свойства `phase_equilibrium`; отслеживаемый газ имеет те же R, cv, теплоту сгорания и стехиометрию. Для влажного бака задаются общая начальная температура и насыщенное парциальное давление; сухой бак начинается не выше насыщения. Общая плотность ниже плотности жидкости.

`evaporated_fuel_mass`, `condensed_fuel_mass` и знаковый `mass_flow` считают нетто-фазовый перенос за полный шаг. Совместно читать `vapor_partial_pressure`, исправленное `saturation_pressure`, жидкий запас и объём газа. Насос забирает только отделённую жидкость; двухфазная подача, унос пузырьков и местный NPSH остаются открытыми.

[Равновесие топлива в баке конечного объёма](TANK_PHASE_EQUILIBRIUM.ru.md)
