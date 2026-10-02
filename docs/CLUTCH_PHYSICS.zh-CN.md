# 托管干式离合器物理

[English](CLUTCH_PHYSICS.md) · **简体中文** · [Français](CLUTCH_PHYSICS.fr.md) · [Русский](CLUTCH_PHYSICS.ru.md) · [日本語](CLUTCH_PHYSICS.ja.md) · [한국어](CLUTCH_PHYSICS.ko.md) · [Deutsch](CLUTCH_PHYSICS.de.md) · [Español](CLUTCH_PHYSICS.es.md) · [Italiano](CLUTCH_PHYSICS.it.md) · [Português](CLUTCH_PHYSICS.pt-BR.md)

`Power.Core` 提供不可变的 `DryClutch` 摩擦定律,以及在恒定外部扭矩与接合下、针对两个惯量的 `ClutchPair` 参考积分器。二者都面向 `net10.0` 与 `netstandard2.1` 编译,且不含第三方依赖。

这些原语为现已集成的[离合器图组件](CLUTCH_NETWORK.zh-CN.md)提供独立参考。该图耦合轴、电机与气缸,支持多个离合器和热路径,并在内部事件下保持整批回滚。JSON、CLI/MCP、资产 v8 与 Studio 使用该图定义。本文记录的独立配对仍是恒定载荷参考;它本身不推进已编译网络。

## 摩擦契约

所有容量与反作用都表示在端口 A。有符号速比 `r` 采用与现有轴组件相同的功率约定:

```text
g       = omega_A - r * omega_B                  [rad/s]
tau_B   = -r * tau_A                            [Nm]
P_heat  = -tau_A * g                            [W]
C_s     = engagement * static_capacity          [Nm]
C_k     = engagement * sliding_capacity         [Nm]
```

接合比例位于 `[0,1]`。静态容量非负,且不小于滑摩容量。二者都可以为零。有效静态容量为零时离合器分离。这里不推断夹紧压力、摩擦系数、摩擦片几何、温度衰减、磨损、拖曳或执行器延迟。

滑差非零时,`tau_A = -sign(g) * C_k`。滑差恰好为零时,积分系统必须提供使相对加速度保持为零所需的扭矩。若其幅值不超过 `C_s`,离合器在该反作用下锁止,且不产生摩擦热。否则它沿不平衡载荷方向开始滑摩,并使用 `C_k`。恰好等于静态极限时仍保持锁止。该定律没有速度死区,也不会把很小的相对速度悄悄变成黏着约束。

动摩擦与受约束静态反作用之间的这一理想区分,遵循主要参考文献所述的力学:[Modelica Clutch](https://doc.modelica.org/om/Modelica.Mechanics.Rotational.Components.Clutch.html)与 [MathWorks Fundamental Friction Clutch](https://www.mathworks.com/help/sdl/ref/fundamentalfrictionclutch.html)。Power! 的实现独立编写,使用显式扭矩容量;它不复现任一实现,也不声称涵盖它们更广的本构模型。

`ClutchMode` 区分 `Disengaged`、`Locked`、`SlippingPositive` 与 `SlippingNegative`。零速度下的模式在外部载荷超过静态容量时可以是正在脱离的滑摩状态。`HeatFlowWatts` 是瞬时量;在这种零速脱离处其值为零,尽管随后的热量是正的。

## 精确恒定载荷配对

对两个正惯量 `J_A`、`J_B`,以及恒定外部扭矩 `T_A`、`T_B`:

```text
J_A * domega_A/dt = T_A + tau_A
J_B * domega_B/dt = T_B - r * tau_A
D                 = 1/J_A + r*r/J_B
b                 = T_A/J_A - r*T_B/J_B
required_tau_A    = -b/D
dg/dt             = b + D*tau_A
```

每个滑摩阶段加速度恒定。若相对速度在所请求区间内到达零,求解器精确推进到 `t_zero = -g / (dg/dt)`,并计算静态反作用。随后它把剩余时间积分为锁止,或沿相反方向滑摩。恒定外力最多只允许一次这样的到达,因此求解至多两个阶段,没有收敛循环或时间细分。恰好落在区间端点的事件返回其右端反作用模式。

锁止轨迹服从 `omega_A = r*omega_B`,且

```text
domega_B/dt = (r*T_A + T_B) / (r*r*J_A + J_B).
```

在计算出的到达时刻,保动量投影消除 binary64 事件舍入残差。它使用有界的惯性权重,而不是形成很大的惯量加权和。一旦锁止,速度约束被显式构造。这是已解析事件处的舍入修正,不是有限滑差的非弹性瞬时接合。角位移对每个恒定加速度阶段积分。外部功为 `T_A*delta_theta_A + T_B*delta_theta_B`;摩擦热是 `-tau_A*g` 的积分。结果包含 A 处的有符号扭矩冲量,以及可独立核对的动能变化。能量残差为 `external_work - heat - delta_kinetic`。

该配对支持非零有限 `r` 的任一符号。其广义动量 `r*J_A*omega_A + J_B*omega_B` 只通过 `r*T_A + T_B` 变化。当 `r = 1` 时适用普通角动量守恒;速比表示理想机械变换器,其支承可以承受反作用扭矩。对地制动时,构造 `ClutchPair.Brake(J, friction)`。此时端口 B 的速度与外部扭矩固定为零,且 `r = 1`。无穷大不用作惯量哨兵值。

## API 与失败行为

```csharp
var pair = new ClutchPair(0.2, 0.8, new DryClutch(20, 10));
var status = pair.Advance(new ClutchPairState(100, 0),
    externalTorqueANewtonMeters: 0,
    externalTorqueBNewtonMeters: 0,
    engagement: 1,
    durationSeconds: 4,
    out var step);
```

该示例在 1.6 s 后两端口都达到 20 rad/s,并产生 800 J 热量。四秒内的角位移为 144 rad 与 64 rad。所有数字都是合成值,不声称车辆标定。

两个类都不可变。`ClutchPairState` 与 `ClutchPairStep` 是值类型。`Advance` 既不修改调用方状态,也不分配内存。独立调用方可以共享同一个配对。没有保留的阶段历史或全局仿真时钟;所给速度与新的恒定载荷决定下一区间。

| 状态 | 含义与恢复 |
|---|---|
| `Ok` | 已得到完整的有限局部结果;守恒与模型适用性需另行评估 |
| `InvalidDuration` | 提供有限且严格为正的秒级区间 |
| `InvalidEngagement` | 提供 `[0,1]` 内的有限比例 |
| `InvalidState` | 提供有限速度;对地制动要求速度 B 等于零 |
| `InvalidTorque` | 提供有限外部扭矩;对地制动要求扭矩 B 等于零 |
| `NumericalFailure` | 导出的运动、事件时刻或能量超出所支持的 binary64 范围;检查单位/尺度,并缩短或改写区间 |

任一拒绝时输出为 `default`;没有部分发布的状态。无效的不可变参数在构造时抛出 `ArgumentException` 或其子类。`DryClutch.Evaluate` 同样拒绝无效输入或溢出的瞬时热量。下溢到零的事件时刻会失败,而不是悄悄丢弃有限的相对动能。物理结果仍受浮点舍入影响;仅有有限输入并不能保证导出量可表示。

`ZeroSlipTimeSeconds` 是第一次接合到达零相对速度的时刻,若区间从该处开始则为零。没有这样的到达时它为空,包括分离运动。它不意味着黏着:很大的外部载荷可以导致立即反向。`SlippingDurationSeconds` 包含正在脱离的滑摩阶段;分离运动被排除。`EndReaction` 是最终状态处的瞬时量,而热量、功、冲量与角位移是在完整区间上积分的。

局部秒参数不替代 `Simulation` 的固定、有界纳秒时钟。图积分保持精确的外部节拍/事件边界、状态哈希、分支独立性、取消以及完整的多节拍回滚。

## 证据与边界

[ClutchChecks.cs](../tests/Power.Tests/ClutchChecks.cs) 对两个 Core 目标程序集运行同样的十组检查:

- 封闭形式的双惯量同步时间、速度、角位移、冲量、动量与损失动能,含全接合与部分接合。
- 精确的静态载荷分担、含边界的脱离阈值、更低的动摩擦容量、无死区的非零滑差,以及零动摩擦容量的静态闩锁。
- 区间内与端点处的反向,以及对地制动、保持和过载脱离。
- 正/负速比、广义动量与独立计算的能量变化。两千个确定性组合扫描惯量、速比、速度、外部载荷、容量与时长。
- 恒定外力下跨越混合事件的划分不变性。变化正弦载荷的中点采样收敛于独立解析的速度、角度与热量积分。这证明该载荷采样示例的二阶行为;图有自己单独的耦合与混合收敛检查。
- 无效输入、溢出、无法解析的事件、失败时的默认输出、独立重复求值,以及在 10,000 个成功区间上的零分配。

配对把热量作为生成的能量返回;图组件把它送到热节点或外部账本。两个 API 都不实现 DCT/AT 拓扑、选挡、变矩器、液压执行器、ECU/TCU 协调、离合器材料辨识或实测标定。这些边界仍在[路线图](ROADMAP.zh-CN.md)中。
