# 相变弛豫燃料泵与冻结相态计量

[English](RELAXING_FUEL_BOUNDARIES.md) · **简体中文** · [Français](RELAXING_FUEL_BOUNDARIES.fr.md) · [Русский](RELAXING_FUEL_BOUNDARIES.ru.md) · [日本語](RELAXING_FUEL_BOUNDARIES.ja.md) · [한국어](RELAXING_FUEL_BOUNDARIES.ko.md) · [Deutsch](RELAXING_FUEL_BOUNDARIES.de.md) · [Español](RELAXING_FUEL_BOUNDARIES.es.md) · [Italiano](RELAXING_FUEL_BOUNDARIES.it.md) · [Português](RELAXING_FUEL_BOUNDARIES.pt-BR.md)

## 物理模型

`compressible_fuel_chamber` 可以控制 `relaxing_fuel` 储腔的体积。可逆轴功保持蒸气质量分数不变，化学弛豫作为独立的能量守恒源项处理。`compressible_fuel_nozzle` 在 `phase_mode` 为 `frozen` 时连接两个弛豫储腔，同时输运总质量、蒸气质量与滞止焓；实际电磁线圈、弹簧和行程限位可通过物理针阀计量开度。

[NASA: isentropic ideal-gas relations](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## 耦合与数值方法

固定质量分数的熵关系确定体积变化后的压力与温度。做功包括液体冷压缩能，小位移采用稳定的增量函数计算，同一离散功用于轴转矩。每个全局区间在守恒管道及冻结喷嘴输运前后，对各节点仅施加一次相变源项。区间受管道 CFL 和喷嘴两端较小库存的 0.02 比例限制，最多允许 4096 个区间。机械做功与源项演化分开；耦合输运仍为一阶，需要时间细化检查。

```text
x = vapor_mass / total_mass
cp_x = (1-x) c_liquid + x (c_vapor+R)
cv_x = (1-x) c_liquid + x c_vapor
alpha = x R/cp_x
T/T0 = (p/p0)^alpha                         (fixed quality and entropy)
v(p) = (1-x)/(rho0 (1+p/K)) + x R T/p
work_on_fluid = mass [cv_x T0 expm1(alpha ln(p/p0)) + (1-x) delta_u_cold]
fluid_torque = -work_on_fluid/delta_angle   (nonzero angular increment)
delta_vapor_mass_volume_work = 0
delta_vapor_mass_nozzle = transferred_total_mass * source_vapor_quality
donor_delta_energy = -transferred_total_mass * stagnation_enthalpy
receiver_delta_energy = -donor_delta_energy - wall_fraction * jet_kinetic_energy
dx/dt_phase_source = (x_equilibrium(rho,u)-x)/tau
```

## 使用、证据与限制

运行 `relaxing-fuel-chamber` 或 `relaxing-pump-needle`。可观察腔体体积与功、实际及平衡质量分数、相变熵、`total_vapor_delivered`、端口库存和全局蒸气残差。检查对照纯相绝热线、独立固定质量分数压力积分、微小位移功、可逆循环及独立理想蒸气轴动力学 ODE；有限速率耦合细化、共享储腔源项时间、完整账本、取消、回滚、分支、分配、资产重放和真实 MCP 分别验证。数值记录在验证证据中。

参数仍为 `unverified`。喷嘴两端必须具有相同域和材料，弛豫端口拒绝平衡输运模式。有限速率体相弛豫不表示已解析零容积喷嘴内部的化学转化或停留时间。模型不含相间滑移、液体负压张力、成核势垒定律、喷雾、载气或燃烧接收腔。运动储腔保持均匀混合且整体流速为零；管道内部单元仍为刚性且各有一个所有者。仅适用远离临界行为的稀薄理想蒸气，并须满足材料声明的正压、温度边界、正潜焓与分离的共存比容。初始蒸气质量分数是显式研究输入。数值吻合不证明实测标定或实际 Unity 验收。

在材料证据基础上解析喷嘴内部的有限速率停留过程，再加入解析喷雾和相容燃烧接收腔。继续点火、进排气、机械损失、变速器执行与 ECU/TCU 协同控制；实测标定和实际 Unity 验收分别推进。

[VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)
