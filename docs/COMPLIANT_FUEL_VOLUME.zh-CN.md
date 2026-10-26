# 柔性燃料容积参考模型

[English](COMPLIANT_FUEL_VOLUME.md) · **简体中文** · [Français](COMPLIANT_FUEL_VOLUME.fr.md) · [Русский](COMPLIANT_FUEL_VOLUME.ru.md) · [日本語](COMPLIANT_FUEL_VOLUME.ja.md) · [한국어](COMPLIANT_FUEL_VOLUME.ko.md) · [Deutsch](COMPLIANT_FUEL_VOLUME.de.md) · [Español](COMPLIANT_FUEL_VOLUME.es.md) · [Italiano](COMPLIANT_FUEL_VOLUME.it.md) · [Português](COMPLIANT_FUEL_VOLUME.pt-BR.md)

## 本构范围

`CompliantFuelVolume` 为相平衡燃料网络提供本构闭合关系，根据总质量与温度或储能求解液态、蒸气及带弹性储能的共存。泵供油喷油器路径仍待扩展耦合。 [相平衡燃料输运网络](EQUILIBRIUM_FUEL_NETWORK.zh-CN.md)

## 热力学

液体密度与比热恒定，蒸气采用理想伪组分。两相共用现有油膜物性参考。液体化学势包含 `p/rho_l`，因此 `p_0(T)` 是忽略液体排量的物性参考，并非有限密度纯流体的饱和压力。弹性壁外压为零，压力功只储存一次。

`StoredEnergyJoules` 包含热量与弹性储能；接入图模型时，燃料热值对应的化学能仍使用独立账本。

```text
V = V_0 + C p = m_l / rho_l + m_v R T / p
E = m_l (cl T + a) + m_v cv T + C p^2 / 2
ln(p / p_0(T)) = p / (rho_l R T)       [coexistence]
h = (U_fluid + p V) / M
dE = T dS + mu dM                    [fixed vessel parameters]
```

共存状态选择低密度蒸气根。汽化焓为 `L_u + R T - p/rho_l`，压力导数满足 Clapeyron 关系。固定质量下，报告热容对流体与弹性总能求导，并包含相转移贡献。弹性壁不携带熵。

## Core API

`Evaluate` 接收温度和总质量。`Flash` 接收总质量与流体加弹性储能。`AdvanceHeat` 通过声明的热导和以秒表示的局部时长，采用后向 Euler 与有限热浴交换热量。属性名称明确 SI 单位。这些局部调用不替代 `Simulation` 的有界整数时钟。

`FuelVolumeStatus` 区分无效输入、温度、质量及能量越界和求解失败；拒绝时输出为默认值。`Flash` 保留请求的能量，并以 `CaloricResidualJoules` 报告浮点热量重构误差。实例不持有演化状态，成功调用不分配托管内存。

## 适用范围

显式声明正参考体积、顺应性、液体密度和最大壁压。压力上限覆盖完整共存曲线，并低于 `rho_l R T_min`。最低温度共存状态的壁膨胀相对参考体积至少达到 `128 * epsilon`，其中 `epsilon = 2^-52`。此限制排除无法分辨的壁变形与超出声明模型范围的高密度蒸气分支，不预测临界点。

使用声明的物性温度区间及 `0 < M <= rho_l (V_0 + C p_max)`。真空、载气、真实混合物、热膨胀、有限壁惯性、表面张力和有限速率相变动力学不在此参考范围内。物性与壁参数保持 `unverified`。 轴驱动泵充填、喷油器交接、动量求解和临界流仍需进一步耦合及独立证据。

`AdiabaticReferenceVolume` 在质量和熵不变时，将同一不可变物性移到显式参考体积。返回能量变化包含弹性储能，也就是机械功。变体积计算、闪蒸与有限热浴传热均受声明的物性及壁边界约束。 [轴驱动相变燃料泵](FUEL_CHAMBER_PUMP.zh-CN.md)

## 独立证据

检查比较化学势、Clapeyron 斜率、热量与熵导数，以及包含弹性储能的 Gibbs 关系。独立约束熵最大化不使用饱和求解器。刚性容器、理想气体与广延缩放极限及有限热浴 RK4 细化提供独立参考。热交换保持总能量守恒，并在总熵不减条件下呈现一阶收敛。这些证据不等于实测燃料有效性或 Unity 验收。

[NIST: pure-fluid equilibrium](https://pages.nist.gov/teqp-docs/en/main/algorithms/VLE.html) · [Cantera: constant-volume thermodynamics](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [VALIDATION.md](VALIDATION.zh-CN.md)
