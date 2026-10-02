# 电磁针阀驱动与采样剂量反馈

[English](NEEDLE_ACTUATION.md) · **简体中文** · [Français](NEEDLE_ACTUATION.fr.md) · [Русский](NEEDLE_ACTUATION.ru.md) · [日本語](NEEDLE_ACTUATION.ja.md) · [한국어](NEEDLE_ACTUATION.ko.md) · [Deutsch](NEEDLE_ACTUATION.de.md) · [Español](NEEDLE_ACTUATION.es.md) · [Italiano](NEEDLE_ACTUATION.it.md) · [Português](NEEDLE_ACTUATION.pt-BR.md)

被驱动的液体喷油器读取平动针阀的升程,而不是在请求剂量处关闭一个理想质量门。位置相关的电磁铁、显式针阀质量、回位弹簧/阻尼以及弹性行程止挡提供该运动。采样驱动器拥有线圈电压,并在循环窗口关闭或测得的供给达到锁存请求时停止其指令。

电流衰减、机械关闭延迟与阀座回弹可以在该指令之后继续供给。实际燃油仍在源/油膜/气体账本中;请求剂量是控制目标,不是强加的物理截止。这是研究用执行器,以及简单的通断反馈。非线性磁图谱、饱和、磁滞/涡流损耗、温度相关电阻、开关/续流、电池供电、轴向流体力以及标定喷射仍未完成。

## 互易的磁能与机械能

`solenoid` 使用给定的恒定绕组电阻与线性电感:

```text
L(x) = L_reference + gradient (x - x_reference) > 0
lambda = L(x) i
W_magnetic = lambda^2 / (2 L(x))
d(lambda)/dt = V - R i
F_magnetic = gradient i^2 / 2
```

正向升程增大电感,磁力沿该方向作用。两种电流极性都吸引衔铁。该力遵循磁共能,如 [Modelica 的磁阻力指南](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Magnetic.FluxTubes.UsersGuide.ReluctanceForceCalculation.html)与 [Simscape 电磁铁方程](https://www.mathworks.com/help/simscape-electrical/ref/solenoid.html)所述。Power! 使用自己简化的本构关系与积分;不引入 Modelica 或 Simscape 依赖。电感在所有已接受位置与推测位置上都必须保持为正。该模型不会把负电感钳掉,也不会用标定图谱代替缺失的磁测量。

磁状态是磁链。对于区间 `h`、端点电感 `L0,L1` 与保持的电压,对称离散梯度给出:

```text
A = (1/L0 + 1/L1) / 4
lambda1 = ((1 - h R A) lambda0 + h V) / (1 + h R A)
i_bar = A (lambda0 + lambda1)
F_bar = gradient (lambda0^2 + lambda1^2) / (4 L0 L1)
W_supply = h V i_bar
Q_copper = h R i_bar^2
delta W_magnetic = W_supply - Q_copper - F_bar (x1 - x0)
```

对于提议的端点位置,磁通被解析消去。剩余的力及其解析位置导数,加入与气缸、变矩器、液压活塞和推测离合器相同的非线性机械求解。被接受的运动一次性提交磁通、电功、铜损热与平均力。铜损热进入所声明的热节点或外部排出热量;磁储存能与机械储存能保持分开。

独立的联立 ODE 积分验证光滑的二阶细化。静止 RL 极限也有解析电流参考。能量共轭步进本身并不能在粗时间步上确立准确的运动;电气时间常数、行程位移与接触事件仍需要分辨率。

## 针阀质量、弹簧与弹性止挡

针阀是普通的平动节点,单位为 kg、m 与 m/s。普通的 `linear_spring` 提供预载与阻尼,并带有显式的热量去向。`travel_stop` 在名义行程极限处加入可逆的单向能量:

```text
E_stop = K/2 [max(0,x_min-x)^2 + max(0,x-x_max)^2]
```

其离散反力是已接受端点之间能量梯度的负值。穿透储存能量,而不是钳位位置。解析导数参与同一机械求解;每个区间的名义行程位移限制为跨距的四分之一。一个滑块只有一个止挡拥有者,包括液压活塞已经拥有的止挡。液压与电磁铁仍可以共享坐标,各自的力贡献到同一坐标。

在这一弹性简化之内,阀座回弹是物理的。某一快照上的零开度并不能证明其后区间内流量为零。接触阻尼、密封摩擦、冲击恢复以及实测阀座/针阀行为仍未完成。

## 物理开度与供给

`liquid_fuel_injector` 上可选的 `parameters.needle` 包含平动 `needle_node`,以及以 m/mm 计的 `closed_position` 与 `full_open_position`。实际开度是有界的线性升程比:

```text
opening = clamp((x - x_closed)/(x_full_open - x_closed), 0, 1)
```

被动的[液轨/喷嘴定律](LIQUID_FUEL_INJECTION.zh-CN.md)用该有效开度积分压头。它保留有限存量与压力能界限,但不会把物理供给限制在请求剂量,也不会在曲轴窗口关闭或反转时抹去流动。开启的针阀即使在曲轴停转或请求剂量为零时也可以放入燃油。曲轴窗口仍为反馈锁存目标历史;新观测窗口之外的供给仍属于最近的已观测循环历史与总历史。

没有 `needle` 时,保留先前的理想配额限制喷油器路径,模型指纹与回放不变。带针阀的模型声明其不同的保真度。本增量中针阀是压力平衡的;不推断轴向压力或射流力。既有的、液体容积可忽略的接收端显式导出其排开压力功。

## 整数时钟驱动器与输入归属

`needle_driver` 指定一个被驱动的喷油器、其电磁铁以及同一根定时曲轴。它需要显式的正 `sample_period_ns`,对齐到物理节拍且不大于一秒,以及以 V 计的正 `drive_voltage`。被拥有的线圈从零电压开始。在每个到期采样,驱动器记录锁存剂量与实际供给质量,然后在正向窗口仍有剩余目标供给时保持驱动电压;否则保持零电压。

驱动器拥有电磁铁电压通道。智能体写入喷油器的 `kg` 请求;直接写电压会返回 `controlled_input`,指出可写的指令通道,并保留状态与修订。初始写入与事件写入不会推进控制历史。采样相位跟随整数仿真时间。该驱动器不实现峰值/保持电流调节、预测性关闭补偿、PWM/续流或完整的 ECU/TCU 行为。

## 定义、通道与事务

| 部件 | 参数与端口 |
|---|---|
| `solenoid` | 平动 A 节点;V 输入;非负的 Ohm 电阻,正的 H 参考电感与 H/m 梯度(`h_m`),m/mm 参考位置,A 初始电流;可选热汇 |
| `travel_stop` | 平动 A 节点;递增的 m/mm 极限与正的 N/m 刚度 |
| `needle_driver` | 转动定时 A 节点;稳定的喷油器/电磁铁 ID、整数采样周期与 V 驱动电平 |

定义会拒绝无关的量、错误的单位/域、无效的初始电感、重复的止挡/电压归属、不匹配的针阀/线圈/曲轴,以及未对齐的采样周期。Core 客户端使用 `SolenoidCoil`、`StrokeStop`、`NeedleDrive`、`InjectorNeedleDefinition`,以及独立的磁定律与接触定律。

电磁铁输出暴露瞬时 `current`、上一节拍的离散平均 `force`、磁 `internal_energy`、累积 `copper_heat` 与电 `source_work`。止挡输出暴露弹性能与瞬时反力。驱动器输出暴露所保持的 `command_voltage`,以及上一采样的请求剂量与供给剂量。喷油器 `opening` 是实际位置开度,并带有实际的上一节拍平均供给。全部 ID 与单位都可以通过校验和创建会话发现。

每个电磁铁四个、每个驱动器三个被报告的状态项加入有界状态预算。磁通、平均力、补偿后的热与功、采样控制状态、保持的输入、针阀/止挡以及全部源与相历史,都随完整仿真复制、哈希与回滚。成功的活动步进与快照不分配托管内存。取消、失败批次与独立分支一同保留电气、机械、热和控制器历史。

资产 v20 增加带类型的磁、止挡、针阀与驱动器表,同时保留 v1-v19 读取器。有界长度/计数、摘要、单位、互异归属与伪造降级防护都会检查。一份真实的 v19 液体喷射夹具保留其指纹与同一运行时的升级回放。参见 [ASSET_FORMAT.md](ASSET_FORMAT.zh-CN.md)。

## 实验与验收

`needle-actuated-cylinder` 把执行器与采样反馈连接到有限油轨/油膜的点火气缸。其 0.6 s 边界可以在最后的关闭/蒸发瞬态期间保留液体油膜。验证的是完整的源/油膜/气体/反应存量,而不是假定油膜干燥或供给恰好达到目标。JSON、可移植资产与真实的 MCP 子服务器共享相同的定义与回放。

孤立执行器请求 8 mg,并经电流衰减、关闭运动以及其后较小的阀座回弹观测到过量供给。这些量是研究结果,不是标定的喷油器定时,也不是已被接受的剂量跟踪控制器。[VALIDATION.md](VALIDATION.zh-CN.md)记载数值证据与界限。已准备的 Unity 线圈、止挡、控制器与按比例缩放的针阀视图仍需要真实的 Editor/Play 验证。完整动力总成、实测驱动、更精细的磁、电子与流体力、油轨回充以及 ECU/TCU 仍未完成。
