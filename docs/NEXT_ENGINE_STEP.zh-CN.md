# 发动机开发恢复笔记

[English](NEXT_ENGINE_STEP.md) · **简体中文** · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 接续位置

燃油域泵现在可向物理针阀和冻结汽相质量分数喷嘴供油，并进入有限液体体积的燃烧接收腔。完整库存包括相变、排量功、射流动能和静止壁面冲量。

[轴驱动相变燃料泵](FUEL_CHAMBER_PUMP.zh-CN.md)

`LiquidColumn` 是独立 C# 参考模型，描述水平刚性管中的恒定截面、均匀流速、恒定液体密度与恒定温度。每个局部区间内给定绝对端口压力。线性和二次阻力系数是显式研究输入。它不是图元件，不改变现有燃油网络或喷嘴方程。 [液体入口动量参考](LIQUID_COLUMN.zh-CN.md)

`fuel_inertial_pipe` 连接两个有限燃油节点和有限热壁面。水平刚性管路拥有独立的液体质量、热内能与动能，不替代两端容器库存。管内密度恒定，温度随完全混合库存变化。端口及整条管路必须保持液态；现有准静态连接保留各自的两相行为。 [有限液体燃油管路动量](FUEL_INERTIAL_PIPE.zh-CN.md)

`equilibrium_fuel_duct` 通过显式的 `compressible_fuel` 单元输运质量、动量和总能量。液体可压缩，蒸气为理想气体；两相共享压力、温度和速度。局部平衡允许凝结与闪蒸。两个有限静止储罐和一个有限热壁面闭合各项收支。 [可压缩平衡燃料管路](EQUILIBRIUM_FUEL_DUCT.zh-CN.md)

`compressible_fuel_chamber` 将转轴连接到有限的 `compressible_fuel` 储腔。轴运动改变容积，同一压力功同时改变流体能量和轴转矩。储腔可通过 `equilibrium_fuel_duct` 供给或接收燃料，并发生平衡闪蒸与凝结。 [轴驱动可压缩燃料腔室](COMPRESSIBLE_FUEL_CHAMBER.zh-CN.md)

## 下一步开发

增加相容的可压缩方向阀和针阀／喷嘴边界，明确焓、动能与相态假设。之后继续相变弛豫／滑移、点火、进排气、机械损失及 ECU/TCU 协同控制。实测标定和实际 Unity 验收分别开展。 [可压缩平衡燃料管路](EQUILIBRIUM_FUEL_DUCT.zh-CN.md)

## 验收

为新方程提供独立解析或极限参考、完整质量/能量账本及适当的步长细化。保持整批回滚、取消、分支、稳定通道、资产回放和旧资产读取。之后继续点火、进/排气、机械损失、变速器驱动与 ECU/TCU 协同控制。

参数仍为 `unverified`。须处于声明的温压范围内，并满足潜焓为正、两相共存比容分离及稀薄蒸气条件。这是采用瞬时均相平衡的刚性等截面水平管路，未包含有限速率成核、相间滑移、表面张力、重力、管壁弹性或真实流体临界行为。一阶数值扩散会抹平相变及声波前沿；守恒与回放不能证明空间精度或实测硬件有效性。实际 Unity 验收仍待完成。

[DEVELOPMENT_STATUS.zh-CN.md](DEVELOPMENT_STATUS.zh-CN.md) · [ROADMAP.zh-CN.md](ROADMAP.zh-CN.md) · [VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)

[冻结汽相质量分数的燃油喷流与有限体积接收腔](FUEL_DISCHARGE.zh-CN.md)
