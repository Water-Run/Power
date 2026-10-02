# 理想齿轮与行星参考

[English](IDEAL_GEARS.md) · **简体中文** · [Français](IDEAL_GEARS.fr.md) · [Русский](IDEAL_GEARS.ru.md) · [日本語](IDEAL_GEARS.ja.md) · [한국어](IDEAL_GEARS.ko.md) · [Deutsch](IDEAL_GEARS.de.md) · [Español](IDEAL_GEARS.es.md) · [Italiano](IDEAL_GEARS.it.md) · [Português](IDEAL_GEARS.pt-BR.md)

`Power.Core` 提供两个不可变、无分配的恒定载荷参考:`IdealGearPair` 与 `SimplePlanetaryGear`。它们返回构件速度、角位移、反作用扭矩、外部功、动能变化和能量残差。它们为耦合变速器求解器提供独立的解析证据。分开的[耦合齿轮求解器](GEAR_NETWORK.zh-CN.md)现在通过 JSON、可移植资产和 CLI/MCP 暴露永久齿轮与行星组件,包括离合器控制的换挡实验。这些参考类仍是纯粹的局部解析解。

## 物理范围与符号

理想齿轮没有啮合惯量、柔性、齿隙或损失;所给惯量都是连接的转子惯量。齿轮副的两个惯量,或行星排的全部三个构件,都必须为正且有限。不会从零惯量推断接地或无质量节点。这一抽象遵循 Modelica 标准库 [IdealGear](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealGear.html)与 [IdealPlanetary](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealPlanetary.html)的范围。Power! 的实现独立编写;不包含也不调用任何第三方实现。

对齿轮副,有符号速比 `r` 定义为:

```text
omega_A = r omega_B
reaction_A = lambda
reaction_B = -r lambda
```

正速比给出相同的端口方向;负速比使方向相反。反作用是**作用在所连接转子上**的扭矩,不是转子施加给齿轮的扭矩。对相容运动,它们的净功为零。齿轮副壳体可以承受反作用;仅两个转子的普通角动量一般不守恒。广义动量 `r J_A omega_A + J_B omega_B` 随广义外部扭矩 `r T_A + T_B` 变化。

对简单行星排,太阳轮、齿圈和行星架共享一条正方向轴。齿数比 `k = N_ring / N_sun` 必须大于一。运动关系与两个独立自由度同 [MathWorks 行星齿轮方程](https://www.mathworks.com/help/sdl/ref/planetarygear.html)一致。

```text
omega_S + k omega_R = (1+k) omega_C
reaction_S = lambda
reaction_R = k lambda
reaction_C = -(1+k) lambda
```

这些反作用之和为零,净功为零。模型接受连续速比,不推断齿数、模数、轮齿强度或可制造几何。行星轮自转/轨道惯量、损失、轴承、润滑和热行为不在本参考之内。

## 独立的降维坐标解

齿轮副运动以端口 B 为独立坐标:

```text
J_equivalent_B = r^2 J_A + J_B
alpha_B = (r T_A + T_B) / J_equivalent_B
alpha_A = r alpha_B
```

行星排在形成动能质量矩阵之前消去行星架运动。令 `a = 1/(1+k)`、`b = k/(1+k)`:

```text
omega_C = a omega_S + b omega_R

M = [ J_S + a^2 J_C,    ab J_C        ]
    [ ab J_C,           J_R + b^2 J_C ]

M [alpha_S, alpha_R]^T = [T_S + a T_C, T_R + b T_C]^T
alpha_C = a alpha_S + b alpha_R
```

实现缩放这个二乘二矩阵,并把行列式展开成正项,以避免相减几乎相等的乘积。载荷恒定时加速度恒定,因此速度与位移遵循精确的线性/二次时间积分,直至浮点舍入。然后从构件方程恢复反作用扭矩。测试对自由行星排使用独立的加速度约束乘子;它们不把降维矩阵重新用作期望解。

## 状态、单位与失败契约

公开属性名带有 SI 单位:kg m2、rad/s、rad、N m、J 与秒。速比无量纲。`Advance` 接受正的有限局部时长,并返回 `GearStepStatus`。这一局部参考时长不替代 `Simulation` 的有界整数时钟。这些类不持有演化状态。输入是值记录;每次拒绝时输出为默认值,实例可以由独立调用方共享。

初始速度必须已经满足关系。兼容性使用相对舍入检验,binary64 的 epsilon 为 `2.2204460492503131e-16`,没有绝对低速死区。齿轮副的界限是 `64 epsilon (|omega_A| + |r omega_B|)`。行星排额外计入两个加权速度项的幅值,因此抵消是相对于形成行星架速度的那些运算来处理的。这些项在相加前先缩放,以免容差溢出。

校验之后,从独立坐标重建从属速度与角位移。这去掉已接受的舍入残差;它不是有限滑差接合或同步计算。不相容的速度返回 `IncompatibleState`。真实的速度不匹配应使用显式的离合器/冲击模型,而不是丢弃其能量。绝对齿轮相位未规定:只报告角位移。

无效的构造参数抛出可操作的参数异常。非有限或病态的参数组合会被拒绝;缩放后的行星行列式必须超过 `64 epsilon`。区间拒绝区分无效时长、无效状态、不相容状态、无效扭矩和数值失败。算术溢出返回 `NumericalFailure`;仅有有限输入并不能保证导出量可表示。运行时的力平衡检查还会拒绝留下有限但不一致构件反作用的抵消:每个力残差以惯性、施加和反作用扭矩幅值之和的 `512 epsilon` 倍为界。被接受的端点还检查每个构件的冲量平衡,使用旧/新动量以及施加/反作用冲量幅值的 `512 epsilon` 倍。后者检测从属速度重建中的过度抵消。这些检查约束的是残差,不是任意病态参数下的解误差。测试包含一次有限抵消失败,以及一个小反作用必须仍可观察的高速比齿轮副。残差为 `external_work - kinetic_energy_change`;不编造摩擦热。

## 传动状态与证据

测试显式提供保持或锁止扭矩,以建立这些理想极限:

| 施加条件 | 得到的速度关系 |
|---|---|
| 齿圈保持 | `omega_C = omega_S / (1+k)` |
| 太阳轮保持 | `omega_C = k omega_R / (1+k)` |
| 行星架保持 | `omega_S = -k omega_R` |
| 太阳轮与齿圈锁止 | 三个构件速度相等 |

所给制动器在其构件被保持时做功为零;太阳轮/齿圈锁止接受相反扭矩,合成功为零。这些检查建立静态传动状态。本参考不实现换挡、离合器接合、液压回路或 TCU,任意外部扭矩也不会自动保持某个构件。

同样的八组测试针对 `net10.0` 与 `netstandard2.1` 运行:

- 有符号速比与折算惯量;反作用功率和逐构件冲量平衡。
- 自由行星运动对照独立的力乘子解。
- 三种构件保持情形与直接驱动,带显式保持/锁止载荷。
- 恒定载荷的划分不变性,以及速度穿过零的反向。
- 正弦载荷对照两个参考的独立积分;区间减半使速度/角度误差大约降为四分之一。
- 无效的惯量/速比/状态/载荷值、不相容速度、病态条件和溢出。
- 每个参考 2,500 个确定性情形,检查功、动量和可重复性。
- 每个原语重复求值 10,000 次且托管分配为零,外加独立并发调用方对不可变实例的共享使用。

完整的串行验证结果见[验证记录](VALIDATION.zh-CN.md)。Standard 程序集测试运行在 .NET 10 上,不提供 Unity 编辑器/Play/IL2CPP 证据。

## 耦合积分

永久约束现在参与机电/气缸/离合器求解,具有独立仿真工作区、完整回滚以及稳定的反作用/误差通道。JSON/schema、带既有读取器的资产 v8、MCP 发现和回放使用同一拓扑。点火行星实验执行减速/直接驱动升挡和一次降挡。方程与证据见[耦合契约](GEAR_NETWORK.zh-CN.md)。完整的 DCT/AT 拓扑、变矩器、液压、控制、完整发动机行为以及实测车辆标定仍属于 Power! 的完整目标。
