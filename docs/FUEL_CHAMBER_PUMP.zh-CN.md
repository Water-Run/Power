# 轴驱动相变燃料泵

[English](FUEL_CHAMBER_PUMP.md) · **简体中文** · [Français](FUEL_CHAMBER_PUMP.fr.md) · [Русский](FUEL_CHAMBER_PUMP.ru.md) · [日本語](FUEL_CHAMBER_PUMP.ja.md) · [한국어](FUEL_CHAMBER_PUMP.ko.md) · [Deutsch](FUEL_CHAMBER_PUMP.de.md) · [Español](FUEL_CHAMBER_PUMP.es.md) · [Italiano](FUEL_CHAMBER_PUMP.it.md) · [Português](FUEL_CHAMBER_PUMP.pt-BR.md)

## 物理契约

`fuel_chamber` 将实际转轴与有限燃料域腔体耦合。进出口 `fuel_check_valve` 通过压差完成充填与排出。液体、蒸气和弹性壁储能使用同一物性闭合关系。入口供给不足时，腔体可在吸入期间闪蒸；交付燃料由实际库存和压力决定。

`node_a` 为转动节点，`node_b` 为仅有一个几何所有者的燃料节点。显式声明以 `m3` 或 `l` 表示的 `minimum_reference_volume`、`swept_volume`，以及以 `rad` 或 `deg` 表示的 `phase`。燃料节点储存的参考体积在 binary64 舍入范围内匹配初始轴角几何。正弦机构不增加活塞或凸轮惯性，轴惯性显式给出。

```text
V0(theta) = Vmin + Vs sin^2((theta - phase) / 2)
V = V0(theta) + C p
E = U_fluid + C p^2 / 2
dE = T dS + mu dM - p dV0
torque_pressure = -delta_E_adiabatic / delta_theta
```

## 功与端口

封闭机械子步保持质量与熵不变。储能包含流体和弹性壁，因此轴功使用几何参考体积的变化。同一组已接受能量变化在联合机械求解中确定压力转矩，并包含齿轮、变矩器、液压和离合器试探区间。膨胀推动轴；正 `hydraulic_work` 表示轴向腔体做功。不添加额外外部压力源。

`fuel_check_valve` 使用以 `m3_s_pa` 表示的显式流导及可选比例开度。仅当 A 压力较高时，均匀供体流体才从 A 流向 B。这是无质量、零开启压差的理想单向阀。守恒焓流与有限壁传热沿用燃料网络契约。

## 使用与证据

通过 MCP 请求 `equilibrium-fuel-pump` 或 `electric-equilibrium-fuel-pump`，也可用 CLI 运行其 JSON。联合读取腔体质量、温度、压力、蒸气比例、能量、阀交付量与轴速。组件 `volume` 是几何参考体积，节点 `volume` 包含壁膨胀。腔体 `torque`、`hydraulic_power`、`hydraulic_work` 公开内部轴功交换。

独立液体压力/功、理想蒸气绝热关系和两相熵反演检查本构关系。封闭液体轴运动验证二阶细化；完整相变充填/排出实验相对独立反演的 RK4 参考验证一阶细化。入口不足、充分液体供给、电驱动、齿轮/液压/离合器组合、精确回放、回滚和分配检查覆盖集成。资产 v35 保存几何并读取 v1-v34。实际 Unity Editor/Play/Player/IL2CPP 仍未验证。

## 后续工作

参数保持 `unverified`。整个循环均遵守物性温度、压力及可分辨体积边界。模型假设不可压缩液体、理想伪组分蒸气、瞬时共同温度平衡及零外部壳体压力。局部入口惯性/NPSH、临界流、成核延迟、阀惯性/滞回、真实燃料混合物及实测标定仍待实现。

[Modelica: swept volume](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [Cantera: reactor walls](https://cantera.org/dev/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.zh-CN.md)

燃油域泵现在可向物理针阀和冻结汽相质量分数喷嘴供油，并进入有限液体体积的燃烧接收腔。完整库存包括相变、排量功、射流动能和静止壁面冲量。 [冻结汽相质量分数的燃油喷流与有限体积接收腔](FUEL_DISCHARGE.zh-CN.md)
