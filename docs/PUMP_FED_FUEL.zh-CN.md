# 泵送液体燃油轨

[English](PUMP_FED_FUEL.md) · **简体中文** · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## 接口约定

`liquid_rail_feed` 将液体喷射器与现有排量泵及显式质量/热边界配对。液压出口节点必须与燃轨的顺应性和初始绝对压力一致。配对泵和喷射器管理该压力节点；其他未计入的流体路径会被拒绝。

压力能仅存储在液压节点一次。泵流量和轴反力由守恒耦合求解决定。补入燃料携带热能与化学边界能量，燃轨热储能实现温度混合。带符号的反向流动按当前燃轨温度回输燃料。喷嘴排出、壁面加热、可用蒸气和规定燃烧仍分别记录。

示例 `pump-fed-liquid-cylinder` 与 `pump-fed-needle-cylinder` 保留物理喷射及可选针阀运动。压力 KPI 使用显式单位下声明的纯泵上界。在补给 ID 1511 上读取 `total_fuel_delivered`、`reservoir_enthalpy` 与 `fuel_energy_in`；泵 ID 1510 提供实际轴到流体功。

## 证据与限制

资产 v27 保留补给连接和源温度，并读取 v1-v26。泵轴/压力解析交换、独立联立 ODE 细化、热混合、质量/燃料/能量/体积账本、反向回输与完整回滚各有独立检查。

几何容量、通气/顶部气体/晃动、气蚀、实测泵充液/效率/调压和分解喷雾仍待完成。参数为 `unverified`；实际 Unity Editor/Play/Player/IL2CPP 与 OEM 标定仍未验证。

## 有限液体燃油箱

`liquid_fuel_tank` 使用配对喷射器的密度、油膜热参考和热值，存储有限液体质量及热能。补给通过 `tank_component` 选择它，并省略 `supply_temperature`。每个油箱属于一个燃料属性相同的补给。

油箱热能和化学能进入完整储能账本。油箱到燃轨的传递不增加外部质量或化学供给。显式设定的泵入口压力保留其压力功边界。气体进气/排气仍可携带化学边界能量。

几何容量、通气/顶部气体/晃动、气蚀、实测泵充液/效率/调压和分解喷雾仍待完成。参数为 `unverified`；实际 Unity Editor/Play/Player/IL2CPP 与 OEM 标定仍未验证。

[LIQUID_FUEL_TANK.zh-CN.md](LIQUID_FUEL_TANK.zh-CN.md)
