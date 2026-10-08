# 可追踪的燃油泄压回流

[English](LIQUID_FUEL_RETURN.md) · **简体中文** · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## 接口约定

`liquid_rail_return` 将补给与独占的单向 `hydraulic_relief` 配对。阀连接燃轨与泵相同的设定入口压力。每条流体路径都须登记；不兼容端口、重复阀所有权和未追踪路径会被拒绝。

`fluid_heat_fraction` 显式选择阀损失中随回流燃油携带的比例，范围为 [0,1]。剩余热量沿阀声明的热路径传递。燃轨/油箱联立热混合保持完整质量、化学、压力功和热账本。外部源模式则由回流跨边界带出质量与能量。

在回流 ID 1515 上读取 `total_fuel_delivered`、`reservoir_enthalpy`、`fuel_energy_in`、`fluid_heat` 和 `mass_flow`。补给 ID 1511 报告泵累计传递量。累计循环量可超过初始油箱库存；当前库存等于初始库存减泵传递量加回流。

## 证据与限制

`recirculating-liquid-cylinder` 与 `recirculating-needle-cylinder` 保留有限燃料、真实喷射、蒸发和可选针阀动力学。资产 v29 保留回流连接与热比例，并读取 v1-v28。每条回流在原有模型边界内增加 8 个状态。

独立泄压衰减/压力功、机械/压力/热联立细化、热比例、多条路径、外部边界、每边界重放、回滚与分配检查均通过。 液体沸腾和未充分解析的输运区间会使整批失败。

该模型采用刚性混合油箱、不可压缩液体和理想气体。晃动/静液压形状、气液相平衡、空化、实测泵阀图谱、OEM 标定及实际 Unity Editor/Play/Player/IL2CPP 仍待完成。参数仍为 `unverified`。

[VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)

## 油箱几何与有限气相空间

`liquid_fuel_tank.parameters.headspace` 声明单位为 `m3` 或 `l` 的 `capacity` 和 `gas_node`。气体节点省略 `storage`，其体积为 `capacity - liquid_mass / density`，必须有唯一所有者且保持为正。配对泵及泄压回流的设定储液器压力为零，由有限气体决定入口压力。

[油箱几何与有限气相空间](TANK_HEADSPACE.zh-CN.md)
