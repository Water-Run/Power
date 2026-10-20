# 油箱几何与有限气相空间

[English](TANK_HEADSPACE.md) · **简体中文** · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## 契约

`liquid_fuel_tank.parameters.headspace` 声明单位为 `m3` 或 `l` 的 `capacity` 和 `gas_node`。气体节点省略 `storage`，其体积为 `capacity - liquid_mass / density`，必须有唯一所有者且保持为正。配对泵及泄压回流的设定储液器压力为零，由有限气体决定入口压力。

耦合求解在轴、燃轨和气体之间交换压力功，不引入外部压力源。气体孔口和热连接提供显式通气及传热路径。读取油箱 `pressure`、`fill_fraction`、带符号累计 `hydraulic_work`，以及气相的质量、能量和体积。请求剂量、液体供给、蒸发和燃烧保持独立。

## 证据与限制

示例 `vented-tank-liquid-cylinder` 和 `vented-tank-needle-cylinder` 使用油箱 1513、气相 1520、通气输入 960。资产 v34 保留几何并读取 v1-v33。解析功及导数、独立联立 ODE 细化、完整账本、资产/MCP 回放、回滚和零分配推进检查通过。

该模型采用刚性混合油箱、不可压缩液体和理想气体。晃动/静液压形状、真实流体混合物热力学、空化、实测泵阀图谱、OEM 标定及实际 Unity Editor/Play/Player/IL2CPP 仍待完成。参数仍为 `unverified`。

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.zh-CN.md)

## 有限体积油箱相平衡

将 `liquid_fuel_tank.parameters.headspace.phase_equilibrium` 设为 `true`，以求解油箱蒸发与冷凝。配对油膜提供显式 `phase_equilibrium` 物性，受追踪的气相空间使用相同的 R、cv、热值和化学计量比。湿油箱声明共同气液初温和饱和燃料分压；干油箱初始分压不高于饱和。总库存平均密度须低于液体密度。

油箱 `evaporated_fuel_mass`、`condensed_fuel_mass` 和带符号 `mass_flow` 按完整物理时间步的净相变量累计。结合读取 `vapor_partial_pressure`、压力修正后的 `saturation_pressure`、液体库存及气体体积。泵只吸取已分离的液体；两相泵/燃轨输运、夹带与局部 NPSH 仍待实现。

[有限体积油箱相平衡](TANK_PHASE_EQUILIBRIUM.zh-CN.md)
