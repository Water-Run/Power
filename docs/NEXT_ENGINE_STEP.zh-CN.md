# 发动机开发恢复笔记

[English](NEXT_ENGINE_STEP.md) · **简体中文** · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 接续位置

燃油域泵现在可向物理针阀和冻结汽相质量分数喷嘴供油，并进入有限液体体积的燃烧接收腔。完整库存包括相变、排量功、射流动能和静止壁面冲量。

[轴驱动相变燃料泵](FUEL_CHAMBER_PUMP.zh-CN.md)

`LiquidColumn` 是独立 C# 参考模型，描述水平刚性管中的恒定截面、均匀流速、恒定液体密度与恒定温度。每个局部区间内给定绝对端口压力。线性和二次阻力系数是显式研究输入。它不是图元件，不改变现有燃油网络或喷嘴方程。 [液体入口动量参考](LIQUID_COLUMN.zh-CN.md)

`fuel_inertial_pipe` 连接两个有限燃油节点和有限热壁面。水平刚性管路拥有独立的液体质量、热内能与动能，不替代两端容器库存。管内密度恒定，温度随完全混合库存变化。端口及整条管路必须保持液态；现有准静态连接保留各自的两相行为。 [有限液体燃油管路动量](FUEL_INERTIAL_PIPE.zh-CN.md)

`equilibrium_fuel_duct` 通过显式的 `compressible_fuel` 单元输运质量、动量和总能量。液体可压缩，蒸气为理想气体；两相共享压力、温度和速度。局部平衡允许凝结与闪蒸。两个有限静止储罐和一个有限热壁面闭合各项收支。 [可压缩平衡燃料管路](EQUILIBRIUM_FUEL_DUCT.zh-CN.md)

`compressible_fuel_chamber` 将转轴连接到有限的 `compressible_fuel` 储腔。轴运动改变容积，同一压力功同时改变流体能量和轴转矩。储腔可通过 `equilibrium_fuel_duct` 供给或接收燃料，并发生平衡闪蒸与凝结。 [轴驱动可压缩燃料腔室](COMPRESSIBLE_FUEL_CHAMBER.zh-CN.md)

`compressible_fuel_nozzle` 在有限 `compressible_fuel` 储腔之间提供单向流动，可连接轴驱动腔室。液体压缩能与理想蒸气使用同一储腔材料。显式声明 `phase_mode` 为 `frozen`：通过喷嘴时蒸气质量分数不变，主体储腔则重新达到相平衡。 [冻结相态可压缩燃料喷嘴](COMPRESSIBLE_FUEL_NOZZLE.zh-CN.md)

将 `compressible_fuel_nozzle` 的 `phase_mode` 设为 `equilibrium`，即可在穿越过程中采用瞬时均相相平衡。液体与蒸气共享压力、温度和速度，蒸气质量分数沿等熵线变化。喷嘴与有限储腔使用相同的可压缩液体冷势能和理想蒸气参考。`frozen` 模式作为另一种明确假设继续支持。 [平衡燃料喷嘴输运](EQUILIBRIUM_FUEL_NOZZLE.zh-CN.md)

`relaxing_fuel` 节点具有独立的蒸气质量分数。液体与蒸气共享压力、温度和速度，化学平衡则需要显式声明的有限时间。`relaxing_fuel_duct` 通过显式网格输运质量、动量、总能量和蒸气质量；停留时间由网格几何与实际流速决定。 [有限速率燃料相变弛豫](RELAXING_FUEL.zh-CN.md)

`compressible_fuel_chamber` 可以控制 `relaxing_fuel` 储腔的体积。可逆轴功保持蒸气质量分数不变，化学弛豫作为独立的能量守恒源项处理。`compressible_fuel_nozzle` 在 `phase_mode` 为 `frozen` 时连接两个弛豫储腔，同时输运总质量、蒸气质量与滞止焓；实际电磁线圈、弹簧和行程限位可通过物理针阀计量开度。 [相变弛豫燃料泵与冻结相态计量](RELAXING_FUEL_BOUNDARIES.zh-CN.md)

## 下一步开发

在材料证据基础上解析喷嘴内部的有限速率停留过程，再加入解析喷雾和相容燃烧接收腔。继续点火、进排气、机械损失、变速器执行与 ECU/TCU 协同控制；实测标定和实际 Unity 验收分别推进。 [可压缩平衡燃料管路](EQUILIBRIUM_FUEL_DUCT.zh-CN.md)

## 验收

为新方程提供独立解析或极限参考、完整质量/能量账本及适当的步长细化。保持整批回滚、取消、分支、稳定通道、资产回放和旧资产读取。之后继续点火、进/排气、机械损失、变速器驱动与 ECU/TCU 协同控制。

参数仍为 `unverified`。喷嘴两端必须具有相同域和材料，弛豫端口拒绝平衡输运模式。有限速率体相弛豫不表示已解析零容积喷嘴内部的化学转化或停留时间。模型不含相间滑移、液体负压张力、成核势垒定律、喷雾、载气或燃烧接收腔。运动储腔保持均匀混合且整体流速为零；管道内部单元仍为刚性且各有一个所有者。仅适用远离临界行为的稀薄理想蒸气，并须满足材料声明的正压、温度边界、正潜焓与分离的共存比容。初始蒸气质量分数是显式研究输入。数值吻合不证明实测标定或实际 Unity 验收。

[DEVELOPMENT_STATUS.zh-CN.md](DEVELOPMENT_STATUS.zh-CN.md) · [ROADMAP.zh-CN.md](ROADMAP.zh-CN.md) · [VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)

[冻结汽相质量分数的燃油喷流与有限体积接收腔](FUEL_DISCHARGE.zh-CN.md)
