# 平衡燃料喷嘴输运

[English](EQUILIBRIUM_FUEL_NOZZLE.md) · **简体中文** · [Français](EQUILIBRIUM_FUEL_NOZZLE.fr.md) · [Русский](EQUILIBRIUM_FUEL_NOZZLE.ru.md) · [日本語](EQUILIBRIUM_FUEL_NOZZLE.ja.md) · [한국어](EQUILIBRIUM_FUEL_NOZZLE.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NOZZLE.de.md) · [Español](EQUILIBRIUM_FUEL_NOZZLE.es.md) · [Italiano](EQUILIBRIUM_FUEL_NOZZLE.it.md) · [Português](EQUILIBRIUM_FUEL_NOZZLE.pt-BR.md)

## 物理模型

将 `compressible_fuel_nozzle` 的 `phase_mode` 设为 `equilibrium`，即可在穿越过程中采用瞬时均相相平衡。液体与蒸气共享压力、温度和速度，蒸气质量分数沿等熵线变化。喷嘴与有限储腔使用相同的可压缩液体冷势能和理想蒸气参考。`frozen` 模式作为另一种明确假设继续支持。

压力与比熵共同确定稳定液体、蒸气或两相共存状态。闭合关系使用化学势相等条件及既有温压边界；温度端点只允许 binary64 舍入邻域。面积、壁面热分配、开度指令和实际针阀沿用共同喷嘴契约。喷流能量表示完全膨胀到接收腔压力的理想状态；静止喷流冲量不额外产生针阀轴向力或机械功。

## 全局通量包络

平衡声速平方为正，使密度沿等熵线随压力增加；焓降则随压力增加而减小。利用这两个单调因子，可对各压力区间的通量给出上界，覆盖相边界和多个极值。分支定界返回采样流量下界及剩余上界，相对宽度不超过 1e-4。最多使用 512 个栈上区间和 8192 次分裂。热力学端点差使用舍入裕量和单调压力积分界，最多细化 256 个 Riemann 区间及 80 次闭合迭代。无法满足包络时拒绝整个批次。光滑内部极值满足声速条件，相边界极值则可能不光滑。

```text
g_liquid(T,p) = g_vapor(T,p)
s(p,rho,T,x) = s_source
D(p) = h_source - h(p,s_source) = integral(p,p_source) v(q,s_source) dq
G(p) = rho(p,s_source) sqrt(2 D(p))
drho/dp at fixed s = 1/c_equilibrium^2 > 0
G_upper([p_low,p_high]) = rho(p_high) sqrt(2 D_upper(p_low))
G_lower = max(sampled rho(p) sqrt(2 D_lower(p)))
flux_relative_uncertainty = G_upper/G_lower - 1 <= 1e-4
mass_flow_local = opening Cd A G_lower
velocity_throat = c_equilibrium  (smooth interior maximum only)
```

守恒的一阶子步在储腔之间转移相同的质量和焓，并在接收流体与壁面之间分配喷流动能。接受子步前检查两端材料状态与总熵变。每个区间最多 4096 个子步，每个候选最多减半 64 次；初始转移量上限是较小储腔质量的 0.1 倍。闭阀压差允许 128 倍 binary64 epsilon 乘以最大体积模量或端口压力的舍入量，报告压力不作钳制。失败区间会回滚整个批次。应减小时间步并检查库存、材料边界、针阀行程及阀口面积。

## 使用与证据

运行 `equilibrium-flashing-nozzle` 或 `equilibrium-pump-needle`。新增 `flux_relative_uncertainty` 通道描述最近接受的局部本构通量包络，不约束平均流量、累计输送量、一阶时间积分或标定误差。检查采用独立饱和／熵反演、气体和液体解析阻塞、另行实现的稠密全局压力搜索、能量与熵恒等式，以及解析蒸气排空细化。完整库存、轴／管路／针阀组合、回滚、取消、独立分支、内存分配、可移植资产和实际 MCP 分别验证。数值结果及保留的旧边界见验证证据。

[VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)

## 适用范围与后续工作

参数仍为 `unverified`。这一平衡极限采用单一纯伪流体、理想蒸气、零液体热膨胀和共同相速度，未包含有限成核与弛豫时间、相间滑移、喷雾、载气及燃烧接收腔耦合。数值包络使用声明的 binary64 裕量，不是形式化的定向舍入区间证明。临界过渡附近的喉部分类须结合通量包络解释。实测硬件有效性和实际 Unity 验收仍待完成。

在明确的停留时间和材料证据基础上增加有限速率相变弛豫，再加入解析喷雾和相容的燃烧接收腔耦合。继续点火、进排气、机械损失、变速器执行机构及 ECU/TCU 协同控制。实测标定与实际 Unity 验收分别开展。

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
