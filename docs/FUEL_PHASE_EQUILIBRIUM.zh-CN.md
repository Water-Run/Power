# 燃油相平衡与液体供给边界

[English](FUEL_PHASE_EQUILIBRIUM.md) · **简体中文** · [Français](FUEL_PHASE_EQUILIBRIUM.fr.md) · [Русский](FUEL_PHASE_EQUILIBRIUM.ru.md) · [日本語](FUEL_PHASE_EQUILIBRIUM.ja.md) · [한국어](FUEL_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](FUEL_PHASE_EQUILIBRIUM.de.md) · [Español](FUEL_PHASE_EQUILIBRIUM.es.md) · [Italiano](FUEL_PHASE_EQUILIBRIUM.it.md) · [Português](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

## 契约

可选的 `fuel_film.parameters.phase_equilibrium` 声明单位为 `pa` 或 `bar` 的 `reference_pressure`，以及单位为 `k` 的 `minimum_temperature` / `maximum_temperature`。已有 `saturation_temperature` 指参考温度，`latent_internal_energy` 指该温度下蒸气与液体的比内能差。每个平衡接收端由一个油膜拥有。初始快照保留输入状态；接受的推进区间使气液达到平衡，并与有限热容量壁面交换热量。

`mass_flow` 是完整物理时间步的带符号净相变流量。`evaporated_fuel_mass` 与 `condensed_fuel_mass` 分别累计正、负净相变量，避免重复计算分步求解中互相抵消的调整。`vapor_partial_pressure` 给出接收端燃料分压。热能、质量和化学能账本包含两相。

## 热力学参考

模型采用一种理想拟组分、接收端共用气体常数和常比热，并忽略液体位移体积。由两相化学势相等得到下述饱和关系。在固定体积下，总燃料量与内能确定共同温度及蒸气质量。蒸发和冷凝在内部转移化学能库存，反应仍只消耗蒸气。

```text
a = (cv - cl) T_ref - L_u,ref
u_l(T) = cl T + a
u_v(T) = cv T
L_u(T) = (cv - cl) T - a
ln(p_sat(T) / p_ref) = (cv + R - cl) / R * ln(T / T_ref)
                        + a / R * (1/T - 1/T_ref)
m_v(T) = min(M_f, p_sat(T) V / (R T))
U_eq(T) = (M_c + m_v) cv T + (M_f - m_v) u_l(T)
```

## 供给与数值限制

有界标量求解利用单调的平衡能量关系。有限壁面传热采用后向欧拉。与输运、运动气缸功耦合的一阶细化检查通过。液体共存状态保持在声明的温度区间内；在区间内完全蒸发后，干燥气体可以继续过热，饱和曲线不作外推。

默认液体供给模型拒绝低于饱和压力的湿状态。可选有限体积油箱相平衡使用压力修正化学势求解供给端蒸气转移。燃轨和泵吸口仍只输送已分离液体；气泡夹带与空化泵/燃轨流动还需要后续物理模型。

压力比较仅允许 binary64 舍入量：`64 * epsilon * max(p, p_sat)`，其中 `epsilon = 2^-52`；报告的裕量不作钳制。这是主体液体的饱和检查，局部压降与泵 NPSH 仍需分解流动模型。独立检查还要求绝热闪蒸和有限壁面换热的总熵不减少。

## 证据

`equilibrium-fuel-cell` 展示冷凝、通气引起的蒸发及再次冷凝。`equilibrium-injected-cylinder` 在通气油箱发动机中加入接收端可逆相变与供给裕量。验证涵盖独立熵最大化、解析饱和及传热极限、联立 ODE 细化、完整回放、回滚和分配检查。资产 v37 保留这些属性，并读取 v1-v36。

参数仍为 `unverified`。模型未提供实测汽油属性、多组分或有限速率相变动力学、分解液体位移/喷雾及 OEM 标定。实际 Unity Editor/Play/Player/IL2CPP 验收仍待完成。

[Cantera: species thermodynamics](https://www.cantera.org/stable/reference/thermo/species-thermo.html) · [Cantera: equilibrium](https://www.cantera.org/stable/python/thermo.html) · [VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)

## 有限体积油箱相平衡

将 `liquid_fuel_tank.parameters.headspace.phase_equilibrium` 设为 `true`，以求解油箱蒸发与冷凝。配对油膜提供显式 `phase_equilibrium` 物性，受追踪的气相空间使用相同的 R、cv、热值和化学计量比。湿油箱声明共同气液初温和饱和燃料分压；干油箱初始分压不高于饱和。总库存平均密度须低于液体密度。

油箱 `evaporated_fuel_mass`、`condensed_fuel_mass` 和带符号 `mass_flow` 按完整物理时间步的净相变量累计。结合读取 `vapor_partial_pressure`、压力修正后的 `saturation_pressure`、液体库存及气体体积。泵只吸取已分离的液体；两相泵/燃轨输运、夹带与局部 NPSH 仍待实现。

[有限体积油箱相平衡](TANK_PHASE_EQUILIBRIUM.zh-CN.md)
