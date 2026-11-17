# 冻结相态可压缩燃料喷嘴

[English](COMPRESSIBLE_FUEL_NOZZLE.md) · **简体中文** · [Français](COMPRESSIBLE_FUEL_NOZZLE.fr.md) · [Русский](COMPRESSIBLE_FUEL_NOZZLE.ru.md) · [日本語](COMPRESSIBLE_FUEL_NOZZLE.ja.md) · [한국어](COMPRESSIBLE_FUEL_NOZZLE.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_NOZZLE.de.md) · [Español](COMPRESSIBLE_FUEL_NOZZLE.es.md) · [Italiano](COMPRESSIBLE_FUEL_NOZZLE.it.md) · [Português](COMPRESSIBLE_FUEL_NOZZLE.pt-BR.md)

## 物理契约

`compressible_fuel_nozzle` 在有限 `compressible_fuel` 储腔之间提供单向流动，可连接轴驱动腔室。液体压缩能与理想蒸气使用同一储腔材料。显式声明 `phase_mode` 为 `frozen`：通过喷嘴时蒸气质量分数不变，主体储腔则重新达到相平衡。

声明截面积、有效面积流量系数和 `wall_kinetic_fraction`，并连接有限热壁面。`initial_input` 是 [0,1] 内的开度，可选输入通道用于改变它。可选的 `needle_node`、`closed_position` 和 `full_open_position` 将指令乘以实际平动针阀开度。两端主体速度为零，材料与热值一致，且不能是管路内部单元。反向压差会关闭这个理想零开启压差边界。

## 能量与阻塞

冻结两相共享压力、温度和速度，相间质量转移受到抑制。熵关系与液体冷压缩能共同确定等熵焓降。质量通量存在唯一最大值，其条件使用冻结声速，区别于平衡声速。流量系数缩放有效面积。阻塞时，出口能量描述理想喷流完全膨胀到接收腔压力的状态。供给端滞止焓一次性支付接收腔能量和显式壁面喷流热。喷流冲量只表示静止热化过程，不代表完整壳体推力或针阀轴向力。

```text
cp_f = (1-x) cl + x (cv+R); alpha = x R/cp_f
T(p) = T0 (p/p0)^alpha
v(p) = (1-x)/[rho0 (1+p/K)] + x R T(p)/p
Delta_s = cp_f ln(T/T0) - x R ln(p/p0) = 0
D(p) = h0-h(p)
     = cp_f T0 [1-(p/p0)^alpha] + (1-x) K/rho0 ln[(K+p0)/(K+p)]
G(p) = sqrt(2 D(p))/v(p)
c_frozen^2 = -v(p)^2 / (dv/dp at fixed s,x)
F(p) = v(p)^2 + 2 D(p) v'(p); F'(p) = 2 D(p) v''(p) > 0
G_throat = max(G(p), p_receiver <= p <= p0)
mass_flow = opening Cd A G_throat
Delta_E_A = -Delta_m h0
Delta_E_B = Delta_m h0 - xi Delta_m D(p_receiver)
Delta_E_wall = xi Delta_m D(p_receiver)
```

守恒的一阶子步在储腔之间转移相同的质量和焓，并在接收流体与壁面之间分配喷流动能。接受子步前检查两端材料状态与总熵变。每个区间最多 4096 个子步，每个候选最多减半 64 次；初始转移量上限是较小储腔质量的 0.1 倍。闭阀压差允许 128 倍 binary64 epsilon 乘以最大体积模量或端口压力的舍入量，报告压力不作钳制。失败区间会回滚整个批次。应减小时间步并检查库存、材料边界、针阀行程及阀口面积。

## 使用与证据

运行 `compressible-fuel-nozzle` 检查有限蒸气排放，或运行 `compressible-pump-needle` 检查管路供给、轴驱动泵、单向入口和电磁针阀输送。可读取累计质量、焓、喷流动能、壁面热、冲量和成对熵变。平均流量与力覆盖完整时间步，喉部、出口和相分数通道保留最近接受的穿越状态。检查对比精确液体功、气体／液体解析阻塞、独立压力积分、冻结熵和声速导数、稠密通量扫描及解析阻塞排空细化。守恒、实际针阀运动、回放、分支、回滚、取消、内存分配和资产／MCP 操作分别验证。

[VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)

## 适用范围与后续工作

参数仍为 `unverified`。模型采用理想蒸气及可压缩、零热膨胀液体组成的纯伪流体。须遵守声明的温压范围，包括理想喷流温度。冻结穿越可保留过热液体，但不预测实测成核或亚稳态极限。有限速率弛豫、相间滑移、解析喷雾、不凝载气、燃烧接收腔耦合、实测特性图及实际 Unity 验收仍待完成。守恒与回放不能证明硬件有效性。

扩展兼容的有限速率边界耦合并取得材料弛豫实测数据，再加入解析喷雾和燃烧接收腔。继续点火、进排气、机械损失、变速器执行与 ECU/TCU 协同控制；实测标定和实际 Unity 验收分别推进。

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
