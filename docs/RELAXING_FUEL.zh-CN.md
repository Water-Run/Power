# 有限速率燃料相变弛豫

[English](RELAXING_FUEL.md) · **简体中文** · [Français](RELAXING_FUEL.fr.md) · [Русский](RELAXING_FUEL.ru.md) · [日本語](RELAXING_FUEL.ja.md) · [한국어](RELAXING_FUEL.ko.md) · [Deutsch](RELAXING_FUEL.de.md) · [Español](RELAXING_FUEL.es.md) · [Italiano](RELAXING_FUEL.it.md) · [Português](RELAXING_FUEL.pt-BR.md)

## 物理模型

`relaxing_fuel` 节点具有独立的蒸气质量分数。液体与蒸气共享压力、温度和速度，化学平衡则需要显式声明的有限时间。`relaxing_fuel_duct` 通过显式网格输运质量、动量、总能量和蒸气质量；停留时间由网格几何与实际流速决定。

闭合关系沿用平衡燃料模型的可压缩液体冷势能与理想蒸气。初始质量分数必须显式给出，不会被投影到平衡态。在质量、体积和内能固定时，质量分数按恒定的 `phase_relaxation_time_ns` 指数趋近平衡目标；潜热保留在内部能量平衡中。化学亲和势为液相减蒸气相化学势，其与相变速率的乘积除以温度给出非负熵产生。这是研究弛豫定律，不是实测成核关联式。

[Lund: A hierarchy of relaxation models for two-phase flow](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## 输运与数值方法

守恒 Rusanov 通量输运四个场。每个全局 CFL 区间依次执行半步局部相变、输运和半步相变；共享储腔每次只接受一次源项，与连接管道数无关。孤立定容状态的源项轨迹为精确解，耦合方法仍具有一阶输运误差。CFL 使用冻结质量分数声速上界，局部相变与管道熵变分别检查；越界状态会回滚整个批次。

```text
x = vapor_mass / total_mass; v = 1/rho
v_liquid(p) = 1 / (rho0 (1 + p/K)); v_vapor(T,p) = R T/p
v = (1-x) v_liquid + x v_vapor
u = (1-x) [c_liquid T + a + u_cold(p)] + x c_vapor T
u_cold(p) = K/rho0 [ln(1+p/K) - (p/K)/(1+p/K)]

dx/dt = (x_equilibrium(rho,u) - x)/tau
x(t+h) = x_equilibrium + (x(t)-x_equilibrium) exp(-h/tau)
d(total_mass)/dt = d(total_internal_energy)/dt = d(volume)/dt = 0  (local source)
affinity = mu_liquid - mu_vapor
(ds/dx) at fixed u,v = affinity/T
ds/dt = affinity (x_equilibrium-x)/(T tau) >= 0

cp_frozen = (1-x) c_liquid + x (c_vapor+R)
alpha = x R/cp_frozen
c_frozen^2 = v^2 / [(1-x) v_liquid/(K+p) + (1-alpha) x R T/p^2]
U = (rho, rho velocity, rho total_specific_energy, rho x)
F = (rho velocity, rho velocity^2+p,
     velocity (rho total_specific_energy+p), rho x velocity)
```

## 使用、证据与限制

运行 `relaxing-fuel-evaporation` 或 `relaxing-fuel-duct`。可观察实际与平衡质量分数、化学亲和势、相变熵、端口蒸气库存和全局蒸气残差。蒸发、凝结历史按每个已接受时间步的净源项转移量正负累计，不表示微观正反向总通量。检查覆盖独立指数解析解、熵导数、冻结声学、刚性平衡极限、时间自细化和独立线性声学网格细化。事务、可移植重放与真实 MCP 分别验证；数值记录在验证证据中。

参数仍为 `unverified`。`initial_vapor_mass_fraction` 必须在 [0,1] 内，弛豫时间必须为 1 ns 至一小时的整数。状态须处于材料温度范围和正压力边界内。模型假设刚性水平等截面管道、单一纯伪流体、液体零热膨胀与理想蒸气，未包含相间滑移、界面传热延迟、表面张力、喷雾或载气。弛豫域与泵、喷嘴、运动腔和燃烧接收腔的直接耦合仍待实现。守恒、重放和细化不证明实测硬件有效性或实际 Unity 验收。 仅适用于远离真实流体临界区的稀薄蒸气；材料需保持正潜焓及分离的两相共存比容。

扩展兼容的有限速率边界耦合并取得材料弛豫实测数据，再加入解析喷雾和燃烧接收腔。继续点火、进排气、机械损失、变速器执行与 ECU/TCU 协同控制；实测标定和实际 Unity 验收分别推进。

[VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)
