# 有限体积油箱相平衡

[English](TANK_PHASE_EQUILIBRIUM.md) · **简体中文** · [Français](TANK_PHASE_EQUILIBRIUM.fr.md) · [Русский](TANK_PHASE_EQUILIBRIUM.ru.md) · [日本語](TANK_PHASE_EQUILIBRIUM.ja.md) · [한국어](TANK_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](TANK_PHASE_EQUILIBRIUM.de.md) · [Español](TANK_PHASE_EQUILIBRIUM.es.md) · [Italiano](TANK_PHASE_EQUILIBRIUM.it.md) · [Português](TANK_PHASE_EQUILIBRIUM.pt-BR.md)

## 契约

将 `liquid_fuel_tank.parameters.headspace.phase_equilibrium` 设为 `true`，以求解油箱蒸发与冷凝。配对油膜提供显式 `phase_equilibrium` 物性，受追踪的气相空间使用相同的 R、cv、热值和化学计量比。湿油箱声明共同气液初温和饱和燃料分压；干油箱初始分压不高于饱和。总库存平均密度须低于液体密度。

油箱 `evaporated_fuel_mass`、`condensed_fuel_mass` 和带符号 `mass_flow` 按完整物理时间步的净相变量累计。结合读取 `vapor_partial_pressure`、压力修正后的 `saturation_pressure`、液体库存及气体体积。泵只吸取已分离的液体；两相泵/燃轨输运、夹带与局部 NPSH 仍待实现。

## 热力学参考

刚性容器包含不可压缩液体、理想拟组分及载气。液体体积改变气体体积。液体化学势包含 `p/rho`，因此接收端的可忽略位移饱和参考 `p_0(T)` 带有压力修正。`reference_pressure` 保留原有物性参考含义，不会被静默解释成有限密度纯流体的实际饱和压力。

[燃油相平衡与液体供给边界](FUEL_PHASE_EQUILIBRIUM.zh-CN.md)

```text
M_f = m_l + m_v
V_g = V_tank - m_l / rho_l
p = (M_c + m_v) R T / V_g
p_v = m_v R T / V_g
ln(p_v / p_0(T)) = p / (rho_l R T)
U = (M_c + m_v) cv T + m_l (cl T + a)
```

有界的定内能、定体积闪蒸保持总燃料与热能守恒。显式气体孔口传递蒸气、载气与化学能；气体热连接与有限热节点交换热量。泵/回流求解与气体交换位移功，再由相平衡重新分配内能。`hydraulic_work` 记录泵位移功，不包含内部相变功。

## 证据与限制

`equilibrium-tank-supply` 演练泵反转、含燃料通气和有限热壁。`equilibrium-tank-fired-cylinder` 将同类供给接入点火发动机。验证涵盖独立熵最大化、化学势及热容导数、联立 ODE 细化、完整账本、回放、回滚与分配检查。资产 v33 保留该标志，并读取 v1-v32。

泵、传热及通气耦合算例验证了一阶收敛。供给端两相均保持在声明温度区间内，总密度与液体密度之间保留 binary64 舍入裕量。常密度/共用气体属性、瞬时平衡和平面界面是研究近似。真实混合物、表面张力、晃动、有限速率动力学、实测标定与实际 Unity Editor/Play/Player/IL2CPP 仍未验证。

[Cantera: constant-volume species](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [IUPAC: Poynting correction](https://goldbook.iupac.org/terms/view/15347) · [VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)
