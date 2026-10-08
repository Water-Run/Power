# Геометрия бака и конечное газовое пространство

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · **Русский** · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## Контракт

`liquid_fuel_tank.parameters.headspace` задаёт `capacity` в `m3` или `l` и `gas_node`. Узел газа не задаёт `storage`: объём равен `capacity - liquid_mass / density`, имеет одного владельца и остаётся положительным. Заданное давление насоса и возврата равно нулю: конечный газ определяет давление входа.

Совместный решатель обменивает работу давления между валом, рампой и газом без внешнего источника давления. Газовые отверстия и тепловые связи задают явную вентиляцию/теплообмен. Читать `pressure`, `fill_fraction`, знаковую накопленную `hydraulic_work` и массу, энергию, объём газа. Доза, жидкость, испарение и горение разделены.

## Доказательства и ограничения

Примеры `vented-tank-liquid-cylinder` и `vented-tank-needle-cylinder` используют бак 1513, газ 1520 и вход вентиляции 960. Asset v29 хранит геометрию и читает v1-v28. Проходят аналитические работа/производные, независимая сходимость ODE, балансы, portable/MCP воспроизведение, откат и шаг без аллокаций.

Жёсткий смешанный бак, несжимаемая жидкость и идеальный газ. Плеск/гидростатическая форма, фазовое равновесие, кавитация, измеренные карты насоса/клапанов, OEM калибровка и реальная Unity Editor/Play/Player/IL2CPP остаются открытыми. Параметры `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.ru.md)
