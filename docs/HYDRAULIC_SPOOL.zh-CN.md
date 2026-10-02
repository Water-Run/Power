# 机械滑阀计量与调压

[English](HYDRAULIC_SPOOL.md) · **简体中文** · [Français](HYDRAULIC_SPOOL.fr.md) · [Русский](HYDRAULIC_SPOOL.ru.md) · [日本語](HYDRAULIC_SPOOL.ja.md) · [한국어](HYDRAULIC_SPOOL.ko.md) · [Deutsch](HYDRAULIC_SPOOL.de.md) · [Español](HYDRAULIC_SPOOL.es.md) · [Italiano](HYDRAULIC_SPOOL.it.md) · [Português](HYDRAULIC_SPOOL.pt-BR.md)

`hydraulic_spool_valve` 根据显式 `hydraulic_piston` 的实际位移计量一个液压端口。活塞提供质量、扫过的流体容积、压力力和柔性行程端部;分开的 `linear_spring` 提供回位力、预载和阻尼。多个计量台肩可以引用同一个活塞。

## 方程与边界

关闭位置与全开位置定义有符号行程 L。对位移 x:

```text
opening = clamp((x - closed_position) / L, 0, 1)
d       = pA - pB
Q       = K * opening * d / (d*d + transition_pressure*transition_pressure)^(1/4)
loss    = Q * d >= 0
```

K 是显式全开系数,单位 m3/(s*sqrt(Pa));过渡压力为正。实现缩放分母,以避免对很大的压差求平方。有符号行程支持两个开启方向。关闭的台肩精确封死;泄漏需要额外的显式路径。只有开度饱和:位置、压力、速度和储存能量不被钳位。

台肩是压力平衡的,轴向射流力被忽略。其计量口压差不向活塞施加额外轴向力。显式执行器的前/后腔压力提供其驱动力。节流热以及活塞/弹簧功使用现有守恒账本。本模型排除密封摩擦、液动力动量效应、气蚀、磨损和随温度变化的几何/黏度。它是声明的研究简化,而不是标定阀门。

[MathWorks Spool Orifice (IL)](https://www.mathworks.com/help/hydro/ref/spoolorificeil.html)记录可变开启面积和分开的轴向液动力选项。Power! 使用自己的归一化线性台肩和现有被动节流定律;没有复制几何、流体物性默认值或实现代码。

## 共享求解与契约

阀门在与其流体压力、活塞力和机械约束相同的联合牛顿求解中读取 `x_old + dx/2`。解析导数同时包含压力和台肩位移。在计量行程内,`dOpening/dx=1/L`;在其外导数为零。在每个端点,雅可比使用单侧斜率的平均。这保持同时的反馈回路,而不是延迟的开度指令。

| 参数 | 含义 |
|---|---|
| `piston_component` | 显式液压活塞的稳定 ID |
| `closed_position`、`full_open_position` | 互异位置,单位 m 或 mm,二者都在活塞名义行程内 |
| `coefficient` | 非负全开系数,单位 `m3_s_sqrt_pa` |
| `transition_pressure` | 正的正则化压力,单位 Pa 或 bar |
| `reservoir_pressure` | 液压 B 省略或为零时必需的表压边界 |

液压 A/B 端口和可选热汇遵循节流契约。阀门没有 `input_channel` 或 `initial_input`;观察其 `opening` 通道,并指令实际的执行器回路。平均流量/功率和累计液压热可观测。单位、被引用的组件类型和行程界限产生可操作的校验错误。普通的整批回滚、取消、分支、整数时钟和精确的同一运行时回放契约包含全部状态和历史。现有物理模型指纹保持不变。

资产 v15 增加一条 32 字节计量几何记录。JSON、CLI 和 MCP 保留同一套定义。`get_example_model("spool-regulated-pump")` 演示电动泵、机械调节的旁通,以及排程的压力离合器充油/排油。其 200 N 静态关闭预载来自 1 mm 压缩下 200 kN/m 的回位弹簧,以及显式的 1000 mm2 执行器面积。旋转驱动/制动为 2 N*m;三秒实验留下足够时间让较低压力的离合器捕获。参数是合成的,且为 `unverified`。

## 证据与性能

检查覆盖有符号计量行程、被动双向流动、解析压力/位置导数、独立的稳态压力根、分开的三状态 RK4 瞬态、光滑二阶细化、台肩开启过程中下降的误差、有限端口均衡、扫过容积、独立的运动/流体能量和完整事务。预热后的步进和快照读取分配零托管字节。台肩斜率和牛顿/LU 缓冲区属于每次仿真;不增加新的时钟或工作线程。

测得误差、运行时范围和耗时见 [VALIDATION.md](VALIDATION.zh-CN.md)。Studio 阀门/执行器视图和导入/Play 测试以 C# 9 源码准备;实际的 Unity 编辑器和 Player 证据仍未完成。
