# 泵送液体燃油轨

[English](PUMP_FED_FUEL.md) · **简体中文** · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## 接口约定

`liquid_rail_feed` 将液体喷射器与现有排量泵及显式质量/热边界配对。液压出口节点必须与燃轨的顺应性和初始绝对压力一致。配对泵和喷射器管理该压力节点；其他未计入的流体路径会被拒绝。

压力能仅存储在液压节点一次。泵流量和轴反力由守恒耦合求解决定。补入燃料携带热能与化学边界能量，燃轨热储能实现温度混合。带符号的反向流动按当前燃轨温度回输燃料。喷嘴排出、壁面加热、可用蒸气和规定燃烧仍分别记录。

示例 `pump-fed-liquid-cylinder` 与 `pump-fed-needle-cylinder` 保留物理喷射及可选针阀运动。压力 KPI 使用显式单位下声明的纯泵上界。在补给 ID 1511 上读取 `total_fuel_delivered`、`reservoir_enthalpy` 与 `fuel_energy_in`；泵 ID 1510 提供实际轴到流体功。

## 证据与限制

资产 v29 保留补给连接和源温度，并读取 v1-v28。泵轴/压力解析交换、独立联立 ODE 细化、热混合、质量/燃料/能量/体积账本、反向回输与完整回滚各有独立检查。

该模型采用刚性混合油箱、不可压缩液体和理想气体。晃动/静液压形状、气液相平衡、空化、实测泵阀图谱、OEM 标定及实际 Unity Editor/Play/Player/IL2CPP 仍待完成。参数仍为 `unverified`。

## 有限液体燃油箱

`liquid_fuel_tank` 使用配对喷射器的密度、油膜热参考和热值，存储有限液体质量及热能。补给通过 `tank_component` 选择它，并省略 `supply_temperature`。每个油箱属于一个燃料属性相同的补给。

油箱热能和化学能进入完整储能账本。油箱到燃轨的传递不增加外部质量或化学供给。显式设定的泵入口压力保留其压力功边界。气体进气/排气仍可携带化学边界能量。

该模型采用刚性混合油箱、不可压缩液体和理想气体。晃动/静液压形状、气液相平衡、空化、实测泵阀图谱、OEM 标定及实际 Unity Editor/Play/Player/IL2CPP 仍待完成。参数仍为 `unverified`。

[LIQUID_FUEL_TANK.zh-CN.md](LIQUID_FUEL_TANK.zh-CN.md)

## 可追踪的燃油泄压回流

`liquid_rail_return` 将补给与独占的单向 `hydraulic_relief` 配对。阀连接燃轨与泵相同的设定入口压力。每条流体路径都须登记；不兼容端口、重复阀所有权和未追踪路径会被拒绝。

`fluid_heat_fraction` 显式选择阀损失中随回流燃油携带的比例，范围为 [0,1]。剩余热量沿阀声明的热路径传递。燃轨/油箱联立热混合保持完整质量、化学、压力功和热账本。外部源模式则由回流跨边界带出质量与能量。

[LIQUID_FUEL_RETURN.zh-CN.md](LIQUID_FUEL_RETURN.zh-CN.md)

## 油箱几何与有限气相空间

`liquid_fuel_tank.parameters.headspace` 声明单位为 `m3` 或 `l` 的 `capacity` 和 `gas_node`。气体节点省略 `storage`，其体积为 `capacity - liquid_mass / density`，必须有唯一所有者且保持为正。配对泵及泄压回流的设定储液器压力为零，由有限气体决定入口压力。

耦合求解在轴、燃轨和气体之间交换压力功，不引入外部压力源。气体孔口和热连接提供显式通气及传热路径。读取油箱 `pressure`、`fill_fraction`、带符号累计 `hydraulic_work`，以及气相的质量、能量和体积。请求剂量、液体供给、蒸发和燃烧保持独立。

[油箱几何与有限气相空间](TANK_HEADSPACE.zh-CN.md)
