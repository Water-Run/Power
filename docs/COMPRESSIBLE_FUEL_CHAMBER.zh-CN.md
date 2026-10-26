# 轴驱动可压缩燃料腔室

[English](COMPRESSIBLE_FUEL_CHAMBER.md) · **简体中文** · [Français](COMPRESSIBLE_FUEL_CHAMBER.fr.md) · [Русский](COMPRESSIBLE_FUEL_CHAMBER.ru.md) · [日本語](COMPRESSIBLE_FUEL_CHAMBER.ja.md) · [한국어](COMPRESSIBLE_FUEL_CHAMBER.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_CHAMBER.de.md) · [Español](COMPRESSIBLE_FUEL_CHAMBER.es.md) · [Italiano](COMPRESSIBLE_FUEL_CHAMBER.it.md) · [Português](COMPRESSIBLE_FUEL_CHAMBER.pt-BR.md)

## 物理契约

`compressible_fuel_chamber` 将转轴连接到有限的 `compressible_fuel` 储腔。轴运动改变容积，同一压力功同时改变流体能量和轴转矩。储腔可通过 `equilibrium_fuel_duct` 供给或接收燃料，并发生平衡闪蒸与凝结。

`node_a` 为转动端口，`node_b` 只能有一个腔室几何所有者。声明 `minimum_reference_volume`、`swept_volume` 和 `phase`；节点初始容积须匹配初始轴角处的几何容积。混合储腔主体速度为零，不能兼作管路内部单元。无质量的正弦机构不增加活塞或凸轮惯量，轴惯量显式给定。外部壳体压力为零。

## 功与输运

闭腔做功步骤保持质量与熵不变。液体冷压缩能和热内能使用与管路一致的平衡势函数。纯液体运动使用精确压力积分，理想蒸气使用解析绝热线；相变运动反演熵并检查最终能量状态。接受的能量差在联合机械求解中产生离散压力转矩。`hydraulic_work` 为正表示轴向流体输入能量。

```text
V(theta) = Vmin + Vs sin^2((theta-phase)/2)
dE_fluid = -p dV; dM = 0; dS = 0
tau_pressure = -Delta_E_fluid / Delta_theta
tau_pressure(Delta_theta=0) = p dV/dtheta
Delta_E_shaft + Delta_E_fluid = 0
p_liquid = K (M/(rho0 V)-1)
Delta_E_liquid = K [Delta_V - (M/rho0) ln(V_next/V_old)]
T_vapor_next = T_old (V_old/V_next)^(R/cv)
Delta_E_vapor = M cv (T_next-T_old)
```

可变容积储腔依次使用半步管路输运、整步轴功、半步输运。输运仍具有一阶数值扩散，对称分裂不会提高其阶数。光滑的纯液体闭腔轴运动呈二阶收敛。每个机械区间的角增量限制为 0.25 rad。管路 CFL、材料边界、完整回滚、取消与独立分支仍适用。静止端口／壳体冲量与活塞压力功分别核算。

## 使用与证据

运行 `compressible-fuel-chamber-duct` 或 `electric-compressible-fuel-duct`。结合各单元状态读取储腔质量、容积、压力、温度、熵、蒸气分数，以及腔室转矩、功率和功。检查采用独立液体功与理想蒸气极限、另行反演的平衡等熵线及压力积分、液体轴运动 ODE 和理想蒸气半离散方程 RK4 细化。守恒、电气／齿轮／液压／离合器组合、精确资产回放、实际 MCP 和无分配事务分别验证。详细数值结果见验证证据。

[VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)

## 适用范围与剩余工作

参数仍为 `unverified`。模型采用平衡纯伪流体：可压缩且零热膨胀的液体、理想蒸气、混合储腔和刚性管路单元。须遵守声明的温压边界。腔室不解析主体动能，也不自动附带止回阀。有限速率成核、相间滑移、真实流体临界行为、实测标定与实际 Unity 验收仍待完成。

增加相容的可压缩方向阀和针阀／喷嘴边界，明确焓、动能与相态假设。之后继续相变弛豫／滑移、点火、进排气、机械损失及 ECU/TCU 协同控制。实测标定和实际 Unity 验收分别开展。

[Modelica: swept volume and pressure work](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [NIST: consistent thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html)
