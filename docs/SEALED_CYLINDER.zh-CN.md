# 封闭气缸基础

[English](SEALED_CYLINDER.md) · **简体中文** · [Français](SEALED_CYLINDER.fr.md) · [Русский](SEALED_CYLINDER.ru.md) · [日本語](SEALED_CYLINDER.ja.md) · [한국어](SEALED_CYLINDER.ko.md) · [Deutsch](SEALED_CYLINDER.de.md) · [Español](SEALED_CYLINDER.es.md) · [Italiano](SEALED_CYLINDER.it.md) · [Português](SEALED_CYLINDER.pt-BR.md)

`sealed_cylinder` 把刚性滑块—曲柄耦合到一个转动节点。气缸内是固定质量的理想气体,比热比恒定,且没有壁面传热。这是压缩/膨胀基准,不是完整的点火发动机。进气、排气、燃油、燃烧、泄漏、壁面传热、往复惯量与控制事件仍是各自独立的实现工作。当前参数全部是合成的,且为 `unverified`。

初始压力与温度作用于所连接转子的初始角度加上气缸相位之处。改变该初始角度会改变封存质量,除非压力与温度做一致的调整。气体状态由曲轴位置与不可变的初始熵导出;它增加可观测通道,但不增加独立状态变量。这一简化仅对绝热封闭部件有效。

## 几何与气体状态

长度编译为米,压力编译为帕斯卡,相位编译为弧度。输入接受 `m`/`mm`、`pa`/`bar` 与 `rad`/`deg`。温度是开尔文;比气体常数使用 `j_kg_k`。压缩比与 gamma 无量纲。缸径与行程必须为正,连杆长度必须大于行程的一半,压缩比与 gamma 必须大于 1,初始气体压力、温度与气体常数必须为正。背压可以为零。

曲柄半径 `r = stroke/2`,连杆长度 `l`,活塞面积 `A = π bore²/4`,角度 `θ` 自上止点量起:

```text
x(θ) = r (1 - cos θ) + l - sqrt(l² - r² sin² θ)
Vc   = A stroke / (compression_ratio - 1)
V(θ) = Vc + A x(θ)
```

实现采用代数等价形式,以避免在上止点附近发生相消。该几何遵循[科罗拉多州立大学的对中滑块—曲柄容积关系](https://www.engr.colostate.edu/~allan/thermo/page2/page2.html)。

设 `V0`、`P0` 与 `T0` 描述初始状态。可逆理想气体关系为:

```text
m = P0 V0 / (R T0)
P = P0 (V0/V)^gamma
T = T0 (V0/V)^(gamma-1)
U = P V / (gamma-1)
τ = (P - Pback) dV/dθ
```

压力/容积与温度关系遵循 [NASA 的等熵压缩推导](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/isentrophic-compression/)。压缩比为 10、gamma 为 1.4 时,从下止点压缩到上止点使压力约增至 25.119 倍、温度约增至 2.512 倍。这些是理想化比值,不是实测的发动机性能。

## 积分与能量

现有的机电中点求解为每个不同的气缸曲轴提供基解,以及对该曲轴扭矩的预计算响应。缩减的非线性求解确定这些曲轴的角增量。同一曲轴上的气缸计入同一个扭矩和;相互耦合的曲轴一起求解。模型提供商调用、Unity 对象或第三方依赖都不参与物理节拍。

每个气缸使用与功一致的离散扭矩:

```text
τ_discrete Δθ = -(U_next - U_old) - Pback (V_next - V_old)
```

稳定的解析容积差商,以及小自变量的对数/指数求值,用来处理小增量与止点穿越。瞬时 `Torque` 输出仍是 `(P-Pback) dV/dθ`;它不同于用来积分有限节拍的平均扭矩。

全局储存能量变化包含气体内能变化。背压功是外部源功,`-Pback ΔV`,因此账本仍是 `source_work - heat_rejected - stored_energy_change`。轴与电机耗散仍进入热网络或排出热量。气体能量变化直接求值,以避免在 gamma 接近 1 时去减很大的绝对能量。

牛顿迭代最多 16 次,每次迭代最多 10 次线搜索试探。线性预测器与接受的曲轴转角必须保持在每节拍 0.25 弧度以内。非有限值、过大转角或未能收敛会返回 `NumericalFailure`;整个调用,包括已调度输入与账本更新,都会回滚。减小 `step_ns` 并重新创建模型/会话,才能用更小的固定节拍重试。被接受并不保证时间步精度。累积角度非常大时也会损失 binary64 角度分辨率;长时间精度需要单独的证据。

## 可观测且可移植的实验

每个气缸暴露压力(Pa)、气体温度(K)、容积(m³)、固定质量(kg)、绝对内能(J)、活塞相对上止点的位移(m),以及曲轴扭矩(N·m)。通道 ID 保留既有的对象/字段编码。模型报告 `sealed_adiabatic_gas` 保真度,标定状态为 `unverified`。

通过 CLI 运行 `assets/labs/sealed-cylinder.power.json`,或经 MCP 请求 `get_example_model({"name":"sealed-cylinder"})`。该示例使用 100 µs 节拍、0.2 s 时长、两次扭矩变化和 21 个报告边界。声明的 KPI 作用于最终样本,与既有实验相同;核心守恒测试则在整个运行中检查重复的边界。

同一文档导出为 `SealedCylinder.powerasset`。Unity 有由位移通道驱动的示意活塞视图;一个场景单位表示完整行程。物理尺寸与输出仍为 SI。真实的 Editor、Play Mode 与 IL2CPP 证据仍待完成。

`EngineChecks` 运行解析几何与理想气体检查、两秒守恒运行、二阶步长细化、反向旋转、小步长/止点情形、共享曲轴与耦合曲轴上的多个气缸、电气/热耦合、原子失败/恢复、取消、独立分支、资产兼容性以及零分配步进。同一套检查在 .NET 宿主上对两个目标程序集执行。
