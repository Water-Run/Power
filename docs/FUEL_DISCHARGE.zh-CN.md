# 冻结汽相质量分数的燃油喷流与有限体积接收腔

[English](FUEL_DISCHARGE.md) · **简体中文** · [Français](FUEL_DISCHARGE.fr.md) · [Русский](FUEL_DISCHARGE.ru.md) · [日本語](FUEL_DISCHARGE.ja.md) · [한국어](FUEL_DISCHARGE.ko.md) · [Deutsch](FUEL_DISCHARGE.de.md) · [Español](FUEL_DISCHARGE.es.md) · [Italiano](FUEL_DISCHARGE.it.md) · [Português](FUEL_DISCHARGE.pt-BR.md)

## 物理模型

`fuel_discharge` 将有限燃油域源连接到 `finite_fuel_film` 的气体节点。将 `phase_mode` 声明为 `frozen`：喷嘴输运过程中汽相质量分数保持不变，两端主体重新达到平衡。均相两相具有共同温度和速度；液体不可压缩，蒸汽是理想伪组分。源与接收腔的材料属性和液体密度必须一致。

给定上游滞止压力和温度，在接收压力与上游压力之间求以下质量通量的最大值。唯一的内部极大值限制阻塞流量。纯液体极限为伯努利关系，纯蒸汽极限为理想气体声速关系。流量系数缩放有效喉部面积。

```text
x = upstream vapor mass fraction; r = p/p0
cp = (1-x) cl + x (cv+R); k = x R/cp; b = (1-x)/rho_l
T(p) = T0 r^k
h0-h(p) = cp T0 (1-r^k) + b p0 (1-r)
v(p) = b + x R T(p)/p
G(p) = sqrt(2 (h0-h(p))) / v(p)
mass_rate = opening Cd A max(G)
jet_kinetic_energy_per_kg = h0-h(p_receiver)
gas_volume = cavity_volume - liquid_mass/rho_l
```

## 能量、体积与动量

源失去输运的滞止焓。`wall_kinetic_fraction` 指定理想完全膨胀射流动能中传给有限静止液膜壁面的比例，其余能量进入接收流体。该参考速度假定理想膨胀到接收压力，不预测实测喷嘴出口速度。射流冲量传给静止接收壁，壳体承受反向反力；这些静止反力不做功，也不增加活塞或针阀力。

显式声明 `finite_volume_density`。气体 `volume` 等于腔体体积减去液体质量/密度；液膜 `volume` 为液体排开体积。运动气缸对整个接收腔执行封闭质量、等熵压力功。排量功留在内部。支持固定腔体和曲柄气缸；线性气体活塞不能拥有此接收腔。

## 使用与证据

运行 `finite-volume-fuel-discharge` 检查混合源阻塞，或运行 `pump-fed-finite-volume-cylinder` 检查电动泵、物理电磁针阀和规定燃烧。可选的 `needle` 运动和 `valve_timing` 与开度相乘。新示例用显式电压脉冲驱动，没有循环目标质量或采样剂量控制器。将 `jet_kinetic_energy`、`jet_impulse`、`force`、`throat_pressure`、`exit_temperature`、`exit_velocity` 和 `choked` 与完整库存一起读取。最后四个值在停流后保留最后接受的流动喷嘴状态。

独立伯努利和理想气体极限、单独的混合物通量极值求解、能量/熵检查及独立积分的液体喷嘴/接收腔 ODE 验证方程。时间细化为一阶。便携资产重放、实际 MCP、回滚、取消、独立分支和分配检查覆盖集成。实际 Unity Editor/Play/Player/IL2CPP 尚未验证。

## 有效范围与后续工作

参数保持 `unverified`。执行声明的温度边界、正气体体积和稀薄蒸汽材料域。模型省略局部入口惯性、有限速率成核、相间滑移、解析喷雾、真实流体临界行为和实测硬件标定。规定燃烧不能证明化学平衡或反应熵准确性。继续入口动力学、点火、进排气、机械损失及 ECU/TCU 协调控制。

[NASA: ideal-gas mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) · [NASA-CR-64098: two-phase mass-limiting experiments](https://ntrs.nasa.gov/citations/19650020869) · [VALIDATION.md](VALIDATION.md)
