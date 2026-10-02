# 曲轴转角气门定时

[English](VALVE_TIMING.md) · **简体中文** · [Français](VALVE_TIMING.fr.md) · [Русский](VALVE_TIMING.ru.md) · [日本語](VALVE_TIMING.ja.md) · [한국어](VALVE_TIMING.ko.md) · [Deutsch](VALVE_TIMING.de.md) · [Español](VALVE_TIMING.es.md) · [Italiano](VALVE_TIMING.it.md) · [Português](VALVE_TIMING.pt-BR.md)

`gas_orifice` 上可选的 `valve_timing` 用周期性曲轴转角包络乘以其开度。它通过同一套 Core、JSON、CLI、MCP 与可移植资产定义,支持定容气体容器与运动气缸。加速、停转与反转期间,包络跟随实际曲轴位置。它表示有效流通面积;并不建模凸轮接触、实际气门升程、弹簧力或摩擦。

## 契约与相位

```json
"valve_timing": {
  "crank_node": 1,
  "cycle_angle": { "value": 720, "unit": "deg" },
  "open_angle": { "value": 520, "unit": "deg" },
  "duration_angle": { "value": 200, "unit": "deg" }
}
```

`crank_node` 必须标识一个转动节点。三个角度都需要显式的 `deg` 或 `rad` 单位。循环角恰好为 360 或 720 度;持续角介于 1e-6 弧度与循环角之间。开启角为有限值,并按循环取模归一化。支持负角度,以及跨越循环边界的凸角。多个气门可以引用同一曲轴,包括相互重叠的凸角。

相位相对于所引用曲轴的角度,包括其初始位置。气缸的几何相位**不会**自动加上:编写者必须为每个气缸选择合适的气门开启角。720 度循环区分相继的曲轴转。不会从活塞位置、转速或经过时间推断隐含的四冲程相位。

对于循环 `C`、开启角 `a`、持续角 `D`、峰值开度 `u` 与曲轴角 `theta`:

```text
s = modulo(theta - a, C)       // 位于 [0, C)
opening = u sin²(pi s / D)     // 0 < s < D
opening = 0                   // 其余情况,包括两个边界
A_effective = A_orifice * opening
```

该型线及其一阶导数在凸角边界处连续。反转会沿原型线返回;停转的曲轴保持当前开度,并且可以继续流动。开度不选择流动方向:既有的压力驱动、双向孔口定律仍然适用。其流量系数仍是单独的乘数。

对于定时节流,`initial_input` 及其可选输入通道指定**峰值开度**,即 [0, 1] 内的分数。通道量是 `peak_opening`;零会禁用该凸角。输出量 `effective_opening` 使用 `Field.Opening`(JSON KPI 字段 `opening`),并报告实际分数。`mass_flow` 用该分数求值。非定时节流保留既有的输入量与语义。已调度与交互式的峰值变化保留原子输入校验与修订检查。

## 积分与恢复

定时模型使用对称的气体半步 / 曲轴功整步 / 气体半步积分,包括由独立曲轴驱动的定容容器。前半步使用节拍开始时的曲轴角,后半步使用得到的角度。气体求解器在每个半步内自行解算质量/能量动力学。它不会连续定位气门沿,也不会自适应外层机械节拍。

对每个活动凸角,外层节拍必须满足:

```text
limit = min(0.25 rad, D / 8)
max(abs(theta_next - theta_old), dt * max(abs(omega_old), abs(omega_next))) <= limit
8 * binary64_epsilon * max(abs(theta_old), abs(theta_next)) <= limit
```

端点转速界限也覆盖净转角变化很小的反转。精度界限防止展开角失去其凸角所需的分辨率。分辨率不足的节拍即使两端点都处于关闭也会失败;它不能悄然跳过整个狭窄开启。被禁用的峰值不要求凸角分辨率。这些是数值防护,不是误差容限,也不保证任意动力学。

失败返回 `NumericalFailure` / `numerical_failure`,并且不提交调用方批次的任何部分,包括更早的节拍与已调度输入。减小 `step_ns` 并重新创建模型/会话;确保事件仍对齐到新节拍。初始角度非常大时,选择一个与每个相连部件相位都一致的等价角度。既有的气缸与气体界限同样适用。不增加隐藏的可变凸轮状态;曲轴位置与峰值输入已经参与快照、哈希与分支。

光滑、无壁面的参考情形表现为二阶收敛。壁温在外层节拍上保持固定,因此与壁面耦合的模型仍是一阶。既有的近平衡流动限制可能降低局部阶数。守恒与回放本身并不能证明时间精度。

## 证据与兼容性

检查包括解析包络值、显式循环、相位回绕、加速、反转、静止曲轴、被禁用的峰值、未分辨的整段凸角穿越、取消、整批回滚、独立分支,以及无分配的步进/快照。

定容容器泄放测试独立积分正弦平方开启暴露,并使用封闭形式的绝热临界排放解。正向与反向情形都在节拍细化下收敛。另一项运动气缸测试用独立编写的 RK4 ODE 积分质量、内能、曲轴运动与随角度变化的节流,并穿越两个凸角边界。参考细化先确立自身精度,再与 Core 结果比较。阈值见[验证记录](VALIDATION.zh-CN.md)。

只有定时模型加入指纹标签 6、目标部件/曲轴 ID 以及归一化的型线参数。非定时模型保留先前的指纹与步进。资产 v5 增加有界定时记录,并保留 v1–v4 读取器;更早的真实夹具检查指纹与升级后的回放。参见[资产布局](ASSET_FORMAT.zh-CN.md)。

[曲轴定时气缸实验室](../assets/labs/crank-timed-cylinder.power.json)用进气与排气型线,把一个合成气室拖动通过重复的 720 度循环。两次已调度的扭矩变化改变曲轴转速;气门定时本身没有时间表。JSON/CLI、MCP 与资产回放在全部 63 个报告边界上一致。构建导出 `CrankTimedCylinder.powerasset`;Studio 用有效开度通道驱动示意标记动画。Editor/Play/IL2CPP 执行仍待完成。

Cantera 的[内燃反应器示例](https://cantera.org/stable/examples/python/reactors/ic_engine.html)是曲轴转角端口控制的概念性参考。其定转速假设、气门定律与示例参数并不作为 Power! 求解器的标定或验证而采用。本实现使用本项目的守恒曲轴耦合与双向喷管定律。单独的[预混燃烧](PREMIXED_COMBUSTION.zh-CN.md)现在加入燃油与化学能核算。全部示例参数仍为 `unverified`;完整发动机行为与实测车辆标定仍未完成。
