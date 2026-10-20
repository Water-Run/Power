# 发动机开发恢复笔记

[English](NEXT_ENGINE_STEP.md) · **简体中文** · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 接续位置

油箱已通过压力修正化学势和分离液体吸取求解有限体积蒸气转移。

[有限体积油箱相平衡](TANK_PHASE_EQUILIBRIUM.zh-CN.md)

纯燃料柔性容器闭合关系已求解液态、蒸气及带弹性储能的两相共存。它是局部本构参考，图级泵与喷嘴输运仍需接入。

[柔性燃料容积参考模型](COMPLIANT_FUEL_VOLUME.zh-CN.md)

## 下一步开发

将此闭合关系接入守恒的两相泵和燃轨输运，再求解局部入口损失、混合物动量及喷嘴排出。保持显式物性与完整体积/功耦合。缺失的 OEM 测量保持缺失。点火、进/排气、机械损失及 ECU/TCU 协同分别继续推进。

## 验收

为新方程提供独立解析或极限参考、完整质量/能量账本及适当的步长细化。保持整批回滚、取消、分支、稳定通道、资产回放和旧资产读取。之后继续点火、进/排气、机械损失、变速器驱动与 ECU/TCU 协同控制。

泵、传热及通气耦合算例验证了一阶收敛。供给端两相均保持在声明温度区间内，总密度与液体密度之间保留 binary64 舍入裕量。常密度/共用气体属性、瞬时平衡和平面界面是研究近似。真实混合物、表面张力、晃动、有限速率动力学、实测标定与实际 Unity Editor/Play/Player/IL2CPP 仍未验证。

[DEVELOPMENT_STATUS.zh-CN.md](DEVELOPMENT_STATUS.zh-CN.md) · [ROADMAP.zh-CN.md](ROADMAP.zh-CN.md) · [VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)
