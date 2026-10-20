# 可追踪的燃油泄压回流

[English](LIQUID_FUEL_RETURN.md) · **简体中文** · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## 接口约定

`liquid_rail_return` 将补给与独占的单向 `hydraulic_relief` 配对。阀连接燃轨与泵相同的设定入口压力。每条流体路径都须登记；不兼容端口、重复阀所有权和未追踪路径会被拒绝。

`fluid_heat_fraction` 显式选择阀损失中随回流燃油携带的比例，范围为 [0,1]。剩余热量沿阀声明的热路径传递。燃轨/油箱联立热混合保持完整质量、化学、压力功和热账本。外部源模式则由回流跨边界带出质量与能量。

在回流 ID 1515 上读取 `total_fuel_delivered`、`reservoir_enthalpy`、`fuel_energy_in`、`fluid_heat` 和 `mass_flow`。补给 ID 1511 报告泵累计传递量。累计循环量可超过初始油箱库存；当前库存等于初始库存减泵传递量加回流。

## 证据与限制

`recirculating-liquid-cylinder` 与 `recirculating-needle-cylinder` 保留有限燃料、真实喷射、蒸发和可选针阀动力学。资产 v33 保留回流连接与热比例，并读取 v1-v32。每条回流在原有模型边界内增加 8 个状态。

独立泄压衰减/压力功、机械/压力/热联立细化、热比例、多条路径、外部边界、每边界重放、回滚与分配检查均通过。 液体沸腾和未充分解析的输运区间会使整批失败。

该模型采用刚性混合油箱、不可压缩液体和理想气体。晃动/静液压形状、真实流体混合物热力学、空化、实测泵阀图谱、OEM 标定及实际 Unity Editor/Play/Player/IL2CPP 仍待完成。参数仍为 `unverified`。

[VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)

## 油箱几何与有限气相空间

`liquid_fuel_tank.parameters.headspace` 声明单位为 `m3` 或 `l` 的 `capacity` 和 `gas_node`。气体节点省略 `storage`，其体积为 `capacity - liquid_mass / density`，必须有唯一所有者且保持为正。配对泵及泄压回流的设定储液器压力为零，由有限气体决定入口压力。

[油箱几何与有限气相空间](TANK_HEADSPACE.zh-CN.md)

## 燃油相平衡与液体供给边界

默认液体供给模型拒绝低于饱和压力的湿状态。可选有限体积油箱相平衡使用压力修正化学势求解供给端蒸气转移。燃轨和泵吸口仍只输送已分离液体；气泡夹带与空化泵/燃轨流动还需要后续物理模型。

压力比较仅允许 binary64 舍入量：`64 * epsilon * max(p, p_sat)`，其中 `epsilon = 2^-52`；报告的裕量不作钳制。这是主体液体的饱和检查，局部压降与泵 NPSH 仍需分解流动模型。独立检查还要求绝热闪蒸和有限壁面换热的总熵不减少。

[燃油相平衡与液体供给边界](FUEL_PHASE_EQUILIBRIUM.zh-CN.md)

## 有限体积油箱相平衡

将 `liquid_fuel_tank.parameters.headspace.phase_equilibrium` 设为 `true`，以求解油箱蒸发与冷凝。配对油膜提供显式 `phase_equilibrium` 物性，受追踪的气相空间使用相同的 R、cv、热值和化学计量比。湿油箱声明共同气液初温和饱和燃料分压；干油箱初始分压不高于饱和。总库存平均密度须低于液体密度。

油箱 `evaporated_fuel_mass`、`condensed_fuel_mass` 和带符号 `mass_flow` 按完整物理时间步的净相变量累计。结合读取 `vapor_partial_pressure`、压力修正后的 `saturation_pressure`、液体库存及气体体积。泵只吸取已分离的液体；两相泵/燃轨输运、夹带与局部 NPSH 仍待实现。

[有限体积油箱相平衡](TANK_PHASE_EQUILIBRIUM.zh-CN.md)
