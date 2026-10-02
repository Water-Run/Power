# 分解的 Ravigneaux 行星运动

[English](RESOLVED_PLANETS.md) · **简体中文** · [Français](RESOLVED_PLANETS.fr.md) · [Русский](RESOLVED_PLANETS.ru.md) · [日本語](RESOLVED_PLANETS.ja.md) · [한국어](RESOLVED_PLANETS.ko.md) · [Deutsch](RESOLVED_PLANETS.de.md) · [Español](RESOLVED_PLANETS.es.md) · [Italiano](RESOLVED_PLANETS.it.md) · [Português](RESOLVED_PLANETS.pt-BR.md)

分解装配包含两套内部行星轮的绝对自转,以及它们绕行星架的轨道质量惯量。四条物理啮合约束连接六个转子。五个挡域离合器/制动器和外部变矩器仍是普通组件。四构件简化仍作为分开声明的简化可用;它不提供行星轮自转证据。

啮合连接和节圆关系有分开的[结构参考](https://www.mathworks.com/help/sdl/ref/ravigneauxgear.html)。Power! 推导并实现自己的守恒转子图和独立质量矩阵检查。不包含供应商实现或模型包。

## 几何与能量

对齿圈节圆半径 `R`、大/小太阳轮比 `kL` 与 `kS`,刚性节圆几何为:

```text
large sun radius = R/kL
small sun radius = R/kS
outer planet radius = (R-large sun radius)/2
inner planet radius = (large sun radius-small sun radius)/2
outer orbit radius = (R+large sun radius)/2
inner orbit radius = (large sun radius+small sun radius)/2
```

`RavigneauxPlanetParameters` 要求 SI 齿圈半径、正的单行星轮质量和自转惯量,以及 1..32 对同步相等的行星轮。均匀间隔必须使两套行星轮都能放下,且节圆不重叠。几何与集总惯量必须保持可表示。这些输入都是显式研究属性;辅助函数不提供测量值。

对 `n` 对相等行星轮,行星架得到轨道惯量 `n (mInner orbitInner^2 + mOuter orbitOuter^2)`。在这条分解路径上,现有的 `CarrierInertia` 参数是行星架结构惯量。每个新转子具有其单行星轮自转惯量的 `n` 倍。它们的速度是绝对角速度,因此动能是普通的 `J omega^2/2`;共转保留行星轮自转能量。若对这种对角存储使用相对自转,就会漏掉行星架耦合。

## 啮合契约

`carrier_gear` 施加 `A - ratio B + (ratio-1) C = 0`,其中 C 是实际运动的行星架。外部啮合使用负的节圆半径比;齿圈/外行星轮内部啮合使用正速比。支持有限、非零的有符号速比,包括一。需要三个不同的转动端口和相容的初始速度。

四条啮合是大太阳轮/外行星轮、小太阳轮/内行星轮、齿圈/外行星轮和内行星轮/外行星轮。三个反作用扭矩都进入同一个中点投影,端口功率之和为零,扭矩之和为零。行星架反作用不会被悄悄送到静止地面。有界的相对残差细化改善小的力响应。中点求解强制下一端点的速度残差为零,避免对先前舍入的重复反射。两项操作都使用实际的约束力响应,并把修正乘子保留在实际反作用历史中。临时工作区属于每次仿真;编译因子保持不可变。归一化行、补偿坐标和完整反作用历史保持相位、分支、取消和批次回滚。

独立的自由参考使用齿圈/行星架坐标。令 `aOuter = R/outerRadius`、`aInner = R/innerRadius`:

```text
outer planet speed = aOuter ring + (1-aOuter) carrier
inner planet speed = -aInner ring + (1+aInner) carrier
M = sum over rotors of J [ring coefficient, carrier coefficient]^T
                         [ring coefficient, carrier coefficient]
```

这同时包含两种自转能量和单独加入的轨道惯量。独立的广义载荷、全部前进/倒挡路径的折算惯量、角动量以及行星架捕获冲量/热检查所装配的求解。

## 共享图与证据

`CreateResolvedGraph` 接受原始端口、四个不同的行星轮节点/啮合 ID,以及声明的行星轮属性。它返回不可变的普通定义:六个内部转子、四个行星架啮合、一个主减速和五个摩擦元件。扁平 JSON 保留总转子惯量和有符号啮合速比;示例说明记录生成几何和单行星轮属性。源摘要保留这一声明的编写证据。

`resolved-ravigneaux-transmission` 检验全部前进升/降挡交接。`fired-resolved-ravigneaux-converter` 加入发动机、有符号变矩器图谱和锁止。二者都声明三对行星轮,R=0.1 m,内/外质量 0.3/1 kg,单行星轮自转惯量 0.000015/0.0005 kg m2。行星架结构为 0.03 kg m2;显式轨道附加量为 0.0184375 kg m2。这些是研究输入。

可移植资产 v24 保留有符号行星架啮合,并读取更早版本。该原语增加指纹标签 28;先前的图保留其指纹和回放。已准备的 Studio 标记标识全部三个啮合端口。实际的 Unity 编辑器/Play/Player/IL2CPP 验证仍然分开。运行所需的串行命令 `dotnet run --file tools/Build.cs -- verify`;数值结果和范围在 [VALIDATION.md](VALIDATION.zh-CN.md)。

## 剩余行为

同步的刚性相等行星轮组不建模轮齿柔性、制造载荷分担、间隙、啮合损失、润滑或随温度变化的物性。[泵供油的液压活塞驱动](AT_HYDRAULIC_ACTUATION.zh-CN.md)可用。完整的换挡控制、ECU 协调以及实测 OEM 几何/图谱仍未完成。这一通用装配不证明 PSA AT8/AL4 的同一性。样本边界和缺失的测量仍保持完整。
