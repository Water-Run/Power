# 轴驱动液压供油

[English](HYDRAULIC_PUMP.md) · **简体中文** · [Français](HYDRAULIC_PUMP.fr.md) · [Русский](HYDRAULIC_PUMP.ru.md) · [日本語](HYDRAULIC_PUMP.ja.md) · [한국어](HYDRAULIC_PUMP.ko.md) · [Deutsch](HYDRAULIC_PUMP.de.md) · [Español](HYDRAULIC_PUMP.es.md) · [Italiano](HYDRAULIC_PUMP.it.md) · [Português](HYDRAULIC_PUMP.pt-BR.md)

托管图支持理想可逆容积泵和准稳态单向泄压。[fired-pump 实验室](../assets/labs/fired-pump.power.json)把曲轴接到柔性供油管路、换挡阀门和压力驱动离合器。其参数是合成的,且为 `unverified`。

## 方程与功率

排量 `D > 0`,单位 m³/rad。正的轴速度把参考容积从入口送到出口:

```
Q = D * omega
shaft reaction = -D * (p_out - p_in)
shaft-to-fluid power = Q * (p_out - p_in) = -shaft reaction * omega
```

允许反向流动和液压马达工况。不推断单向阀、泄漏、摩擦或效率图谱。惯量属于显式轴节点。有限入口恰好失去送到出口的容积。油箱入口把 `p_in * Q` 贡献给外部液压功;轴到流体的功是内部传递,不加进全局源功。

泄压使用显式的线性超压特性:

```
Q_A_to_B = G * max(p_A - p_B - p_crack, 0)
heat = Q_A_to_B * (p_A - p_B)
```

`G >= 0` 的单位是 m³/(s·Pa),`p_crack >= 0` 是压差。低于阈值时它精确封死。有限流量需要超压;压力从不被钳位到设定值。这是本构近似,不是滑阀力学,也不是拟合的阀门开度面积曲线。其全部压降都产生热,包括开启压力那一部分。

理想泵方程遵循 [MathWorks 定量泵说明](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html)的零损失极限。阈值行为与[泄压阀说明](https://www.mathworks.com/help/hydro/ref/pressurereliefvalveil.html)一致;Power! 的线性超压定律是显式的、更简单的建模选择。这些参考提供方程和范围,不提供 OEM 参数测量或源代码。

## 共享契约

`hydraulic_pump` 需要转动 `node_a`、液压出口 `node_b`,以及 `parameters.inlet_node`(零选择油箱)。入口必须与出口不同。参数包括正的 `displacement`,单位 `m3_rad`;仅当入口为零时,再加上显式的 `reservoir_pressure`,单位 `pa` 或 `bar`。它没有输入、热汇或行星 `node_c`。

`hydraulic_relief` 使用液压 `node_a`、可选液压 `node_b`(零或省略选择油箱)、可选热 `heat_node`,以及参数 `coefficient`、`cracking_pressure`,和仅油箱使用的 `reservoir_pressure`。它没有开度输入。阀门时序仍使用分开的受控节流。

泵输出是上一节拍的平均 `volume_flow`、轴反作用 `torque`、有符号 `hydraulic_power`,以及累计有符号 `hydraulic_work`。初始历史为零;输入变化不改变已接受的平均值。全局 `hydraulic_work` 仍是外部油箱功。泄压输出复用节流流量、平均热功率和累计流体热。全部输入、历史和补偿项参与分支、哈希、取消和整批回滚。资产 v11 保留新定义和全部 v1–v10 读取器。Agent 0.13.0 通告 `shaft_driven_hydraulics` 和 `fired-pump`。

## 数值证据与限制

泵速度、腔室压力、变矩器端口速度和气缸功共享一个牛顿系统,使用经齿轮投影的机械响应。压力离合器容量在有界约束迭代内刷新。被接受的流体传输更新两个端口和参考容积账本。不含泵的模型保留现有的独立液压路径,从而保持先前的回放哈希。

联合牛顿求解允许 24 次迭代和 12 次线搜索折半。液压压力残差容差为 `2e-7 Pa + 64*epsilon*(|p_mid|+|p_old|)`;机械和离合器容差保留其现有契约。非有限状态、被接受的负表压或耗尽的求解器预算拒绝完整调用。重试前请减小 `step_ns`,并检查压力、柔度、排量、惯量和离合器尺度。没有气蚀钳位。

测试覆盖解析的轴/柔度振荡和二阶细化、封闭入口守恒、反向马达工况、带齿轮的泵反作用、解析泄压衰减、调节后的稳态轴载荷,以及独立解析的压力相关滑摩离合器解。捕获、分支、取消、延迟失败、重试和无分配步进都有检查。可移植/MCP 回放比较全部 89 个 fired-pump 边界;畸形契约和版本降级被拒绝。

在 0.8 s 点火实验中,轴向流体交付 53.94250162 J,外部液压功为零,泄压耗散 45.02640514 J。初始液压能量显式为 3 J。最终管路压力为 1.06972624 MPa,曲轴/涡轮转速 69.75553569 rad/s,负载转速 6.64338435 rad/s。总能量残差约为 `1.07e-9 J`;参考容积残差为 `3.05e-20 m³`。指纹 `d0bd8f29a706fd89`,最终哈希 `572150ab5d66a2f6`。

实测损失图谱、排量控制、电池与电压控制动力学、滑阀和活塞行程/惯量、气体蓄能器、气蚀、随温度变化的物性以及 ECU/TCU 协调仍然开放。这一检查点不确立完整的 DCT/AT、标定的车辆性能或 Unity 编辑器/Player 验收。

## 显式泄漏、轴摩擦与电气供油

`HydraulicPumpAssembly` 提供可复用的常系数泵简化。它接受排量 D,单位 m³/rad;泄漏导纳 G,单位 m³/(s·Pa);黏性轴摩擦 B,单位 N·m·s/rad。D 必须为正;G 与 B 必须非负且有限。不推断名义效率或油液物性。

对压差 `dp = p_out - p_in`:

```text
net inlet-to-outlet flow = D*omega - G*dp
shaft reaction          = -D*dp - B*omega
leakage heat power      = G*dp^2
shaft friction heat     = B*omega^2
absorbed shaft power    = net fluid power + leakage heat + shaft friction heat
```

这些符号支持任一方向的泵送和液压马达工况,以及停转泵的泄漏。即使泄漏超过排量流量,泄漏仍是被动的出口到入口路径。常导纳泄漏简化遵循 [MathWorks 泵参考](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html)中的解析损失描述。线性黏性阻力是 Power! 的显式本构选择;它不是该参考的压力相关摩擦模型,也不是 OEM 效率图谱。

`TryEvaluate` 返回瞬时净流量、总轴反作用、有符号轴/流体功率,以及两个非负损失功率。它拒绝负表压、非有限输入和溢出,且不返回部分反作用。

`CreateComponents` 返回一个不可变列表,为理想 `hydraulic_pump`、从出口到入口的固定开度 `hydraulic_resistance`,以及从泵轴到地面的零刚度 `shaft` 指定显式且互异的 ID。指定热汇,或让损失进入外部排热。模型编译器检查端口、域、单位和全局 ID。普通组件保留耦合中点求解、事务、通道、JSON schema 和资产 v11;没有隐藏的装配状态或新格式。泵通道描述理想支路。减去泄漏流量得到装配输出;解释总轴载荷时计入轴阻力。不要把理想泵功同时计成外部源功和内部传递。

`fired-pump-losses` 把泄漏和阻力接到现有的点火变速器。分开的泵热节点接收两种损失。零损失极限在物理容差内复现 `fired-pump` 的全部共享可观测量。常数 G 和 B 是研究输入,仍为 `unverified`。

`electric-pump` 把一台 12 V RL 直流电机接到分开的泵轴,带有显式反电动势、电感、扭矩和铜损热。柔性供油管路、泄压和排程的充油/排油阀在被驱动轴与负载之间驱动一个离合器。电压变化和阀门事件使用精确节拍。泵没有曲轴连接,外部液压功为零。电功计入全局源功;被驱动轴和负载扭矩是分开的外部功率边界。给定的电压和阀门指令不实现电池、ECU/TCU 或闭环调节器。

解析的阻尼轴/压力运动,以及独立积分的三状态 RL 电机/轴/压力 ODE,检查光滑的二阶细化。封闭入口、油箱功、有符号运行、被动损失、热路径、零分配、分支、取消和延迟失败回滚都对照两个 Core 程序集检查。JSON、可移植资产和 MCP 比较全部 89 个点火损失边界和 106 个电动报告边界。已准备的 Unity 导入/Play 测试需要分开的编辑器执行。

<a id="sampled-pressure-regulation"></a>
## 采样压力调节

`pressure_controller` 读取一个液压表压节点,并拥有一个现有直流电机电压通道。它是离散 PI 控制器,比例增益单位 `v_pa`,积分增益单位 `v_pa_s`,并有电压界限和初始积分电压。设定值输入具有压力单位。其整数 `sample_period_ns` 为 1 ns..1 s,且必须是模型节拍的精确整数倍。增益和压力设定值非负;电压界限有限且严格递增。不推断整定。

```text
error = setpoint - sampled_pressure
I_candidate = I + Ki * sample_period_s * error
raw_voltage = Kp * error + I_candidate
if raw_voltage exceeds a limit and the integral increment pushes farther outside:
    retain the preceding integral state
command = clamp(Kp * error + accepted_integral, minimum_voltage, maximum_voltage)
```

条件积分是 [MathWorks 控制参考](https://www.mathworks.com/help/simulink/slref/anti-windup-control-using-a-pid-controller.html)所述的钳位抗积分饱和策略。上面 Power! 精确的离散转移是其声明的模型,不是复制的实现代码,也不是 OEM 整定的证据。仅有饱和并不确立跟踪:不可达目标可以成功执行并回放,同时未通过 KPI。

采样发生在时刻零和所配置周期的绝对整数倍。第一次采样保留显式给出的初始积分;其后的采样使用该周期。指令在采样之间保持。同一节拍上的精确节拍事件在采样之前施加。调用端点处的事件在快照之前更新设定值;该端点的采样只在下一个物理节拍开始时发生。内部离合器捕获/反向区间不触发额外的控制器更新。

编译器检查目标是直流电机电压输入且恰好有一个所有者,传感器是液压的,以及初始电机电压在界限内。被拥有的电压通道仍在组件定义中,但不在外部输入列表里。直接写入和排程的电压覆盖被拒绝;改为改变控制器的 `pressure_setpoint` 输入。其他电机、泵和阀门通道保留其现有语义。多个独立回路可以共享一个压力传感器。

可观测通道为 `sampled_pressure`、`pressure_error`、`integral_voltage` 和 `command_voltage`。压力/误差历史从零开始;初始指令是电机配置的电压,初始积分是显式的。历史描述最近一次采样,而不是连续重算的压力误差。全部四个控制器状态和保持的电机输入参与哈希、分支和完整批次回滚。采样和成功步进在预热之后不分配托管内存。非有限的 PI 运算拒绝完整调用;请检查增益、设定值和积分尺度。

控制器不增加物理储存能量或功率边界。它的指令改变现有电机的电压边界,其电流、功和铜损热仍在耦合求解和守恒账本中。不含控制器的模型保留其指纹和步进。受控模型增加指纹标签 13。资产 v12 保留完整的控制器定义;一份真实的 v11 泵夹具在升级后保留其原始摘要、指纹和同一运行时回放。

`pressure-regulated-pump` 使用 5 ms 控制器和 100 µs 物理节拍,带有排程的离合器充油/排油扰动以及 300/350/200 kPa 目标。增益、执行器限制和所有其他参数仍为 `unverified`。它有 757 个一致的 JSON、资产和 MCP 报告边界。测试比较分开的采样控制器/RK4 被控对象,检查固定控制器周期下的物理节拍细化、精确的时钟/端点规则、饱和恢复、单位/所有权诊断、控制器状态回滚、分支、零分配,以及完整的电气/液压功账本。

这提供一个压力反馈回路。电池与 PWM/电流环动力学、传感器滤波/延迟/量化、阀门/滑阀/活塞动力学、ECU/TCU 协调、完整 DCT/AT、故障和实测标定仍是分开的未完成工作。

<a id="finite-battery-supply-and-duty-regulation"></a>
## 有限电池供油与占空比调节

`battery` 节点拥有两个状态:电荷比例 z 和极化电压 v_p。其储存是显式电荷容量 Q,单位 C 或 Ah(1 Ah = 3600 C),初始状态是 `fraction` 单位的荷电状态,位置是初始极化电压,单位 V。其电池记录提供空电/满电开路电压、串联电阻 R0、极化电阻 Rp、电容 Cp,以及热汇或外部排热。开路电压对荷电状态是仿射的:

```text
E(z)       = V_empty + (V_full - V_empty)*z
z'         = -I_battery / Q
v_p'       = I_battery / Cp - v_p/(Rp*Cp)
V_bus      = E(z) - v_p - R0*I_battery
U_chemical = Q*(V_empty*z + (V_full - V_empty)*z^2/2)
U_polar    = Cp*v_p^2/2
heat power = R0*I_battery^2 + v_p^2/Rp
```

正电流放电;负电流充电。拓扑遵循[电池等效电路说明](https://www.mathworks.com/help/simscape-battery/ref/batteryequivalentcircuit.html)。仿射开路电压和常数参数是 Power! 的显式简化,而不是温度/老化表、实测化学、容量衰减或 BMS。荷电状态保持在 [0,1] 内。超出电荷存量或得到负母线电压会拒绝整批;没有悄悄钳位,也不编造储备。请缩短批次、停止放电/充电,或提供不同的声明初始条件。

`battery_motor` 把转动轴接到电池母线,并保留显式电机电阻、电感、扭矩/反电动势系数和初始电流。其平均双向占空比输入位于 [-1,1]:电机电压是占空比乘母线电压,电池侧电流是占空比乘电机电流。该功率传递是内部的,不加进 `source_work`。电机电感能量和电池极化/化学能量都参与总储存能量。铜损、串联和极化热送到各自的显式汇。这是理想平均变换器,不是 PWM 开关、变换器损失、接触器或电流控制环。

`resistive_load` 提供显式正电阻、可选的 [0,1] 开度输入和热汇。开度缩放导纳;零开度精确断开。对总负载导纳 G 和电机侧母线电流 I_m:

```text
V_bus     = (E(z) - v_p - R0*I_m)/(1 + R0*G)
I_battery = I_m + G*V_bus
load heat = opening*V_bus^2/R_load
```

共享的电池电阻耦合全部用电器。耦合中点矩阵包含电荷、极化、电机电流和机械响应。当占空比、附件开度或内部区间时长变化时,仿真拥有的因子会更新。齿轮、气缸、变矩器和离合器响应使用这些相同的因子。未供电的更早模型保留其先前的求解器路径和指纹。仿射化学能和 RC 能量是二次的,因此中点电传递有独立的守恒检查。同一套物理支持电机再生。

`pressure_duty_controller` 使用现有的整数时钟 PI/钳位转移,增益单位为 `fraction_pa` 和 `fraction_pa_s`,显式占空比界限在 [-1,1] 内,并有初始积分占空比。它拥有一个 `battery_motor` 占空比通道。智能体改变 `pressure_setpoint`;直接的占空比覆盖返回 `controlled_input`。读取 `sampled_pressure`、`pressure_error`、`integral_duty` 和 `command_duty`。保持的占空比、电荷、极化和控制记忆共享快照、分支、取消和完整回滚。采样和成功步进保持无分配。

`battery-regulated-pump` 把有限电池供油、附件负载脉冲和 5 ms 占空比调节器与压力离合器实验室组合在一起。其 50 C 容量是很小的合成测试存量,不是车辆电池测量。在 15 s,荷电状态从 0.8 降到约 0.627,而压力对 200 kPa 目标结束于约 200.828 kPa。全部 761 个 JSON/资产/MCP 边界一致。测试分别检查解析 RC 弛豫、电阻负载存量、独立的电机/电路 RK4 积分、物理节拍细化、有符号占空比和再生、并联绕组等效、齿轮/离合器/泵耦合、延迟耗尽回滚、分支、取消和零分配。

电池 BMS/化学/老化和温度反馈、故障/接触器、PWM/电流控制、传感器动力学、执行器力学、完整 ECU/TCU 和标定仍然开放。电池参数和全部实验室输入仍为 `unverified`。
