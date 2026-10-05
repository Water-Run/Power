# 有限液体燃油箱

[English](LIQUID_FUEL_TANK.md) · **简体中文** · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · [Deutsch](LIQUID_FUEL_TANK.de.md) · [Español](LIQUID_FUEL_TANK.es.md) · [Italiano](LIQUID_FUEL_TANK.it.md) · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## 接口约定

`liquid_fuel_tank` 使用配对喷射器的密度、油膜热参考和热值，存储有限液体质量及热能。补给通过 `tank_component` 选择它，并省略 `supply_temperature`。每个油箱属于一个燃料属性相同的补给。

正向泵流量受已接受时间区间内的剩余库存限制。同一有效充液排量决定轴反力和压力传递，保持轴/流体功守恒。空箱正向转动不输送液体或流体功；带符号的回流将当前燃轨热能混入油箱。

油箱热能和化学能进入完整储能账本。油箱到燃轨的传递不增加外部质量或化学供给。显式设定的泵入口压力保留其压力功边界。气体进气/排气仍可携带化学边界能量。

示例 `finite-tank-liquid-cylinder` 与 `finite-tank-needle-cylinder` 使用油箱 ID 1513 和补给 ID 1511。读取 `mass`、`temperature`、`internal_energy`、`chemical_energy` 与 `tank_state`；0 表示有液体，1 表示空箱。干态温度报告声明的初始参考。

## 证据与限制

资产 v27 保留油箱数据和补给选择，并读取 v1-v26。每个油箱在原有边界内增加 4 个报告状态。独立湿态交换、耗尽压力/轴能解析解、回流混合、完整账本、回滚、独立分支与无分配步进均有检查。

几何容量、通气/顶部气体/晃动、气蚀、实测泵充液/效率/调压和分解喷雾仍待完成。参数为 `unverified`；实际 Unity Editor/Play/Player/IL2CPP 与 OEM 标定仍未验证。

[VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)
