# 发动机开发恢复笔记

[English](NEXT_ENGINE_STEP.md) · **简体中文** · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 接续位置

燃油域泵现在可向物理针阀和冻结汽相质量分数喷嘴供油，并进入有限液体体积的燃烧接收腔。完整库存包括相变、排量功、射流动能和静止壁面冲量。

[轴驱动相变燃料泵](FUEL_CHAMBER_PUMP.zh-CN.md)

`LiquidColumn` 是独立 C# 参考模型，描述水平刚性管中的恒定截面、均匀流速、恒定液体密度与恒定温度。每个局部区间内给定绝对端口压力。线性和二次阻力系数是显式研究输入。它不是图元件，不改变现有燃油网络或喷嘴方程。 [液体入口动量参考](LIQUID_COLUMN.zh-CN.md)

`fuel_inertial_pipe` 连接两个有限燃油节点和有限热壁面。水平刚性管路拥有独立的液体质量、热内能与动能，不替代两端容器库存。管内密度恒定，温度随完全混合库存变化。端口及整条管路必须保持液态；现有准静态连接保留各自的两相行为。 [有限液体燃油管路动量](FUEL_INERTIAL_PIPE.zh-CN.md)

## 下一步开发

在宣称空化入口动力学前，以兼容能量/熵闭合扩展变密度和相变管路动量。随后继续相变弛豫/滑移、点火、进排气、机械损失、变速器驱动及 ECU/TCU 协调控制。实测标定与实际 Unity 验收分别进行。 [有限液体燃油管路动量](FUEL_INERTIAL_PIPE.zh-CN.md)

## 验收

为新方程提供独立解析或极限参考、完整质量/能量账本及适当的步长细化。保持整批回滚、取消、分支、稳定通道、资产回放和旧资产读取。之后继续点火、进/排气、机械损失、变速器驱动与 ECU/TCU 协同控制。

参数保持 `unverified`。执行声明的温度边界、正气体体积和稀薄蒸汽材料域。模型省略局部入口惯性、有限速率成核、相间滑移、解析喷雾、真实流体临界行为和实测硬件标定。规定燃烧不能证明化学平衡或反应熵准确性。继续入口动力学、点火、进排气、机械损失及 ECU/TCU 协调控制。

[DEVELOPMENT_STATUS.zh-CN.md](DEVELOPMENT_STATUS.zh-CN.md) · [ROADMAP.zh-CN.md](ROADMAP.zh-CN.md) · [VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)

[冻结汽相质量分数的燃油喷流与有限体积接收腔](FUEL_DISCHARGE.zh-CN.md)
