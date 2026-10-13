# 发动机开发恢复笔记

[English](NEXT_ENGINE_STEP.md) · **简体中文** · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 接续位置

接收端已支持可逆燃油相平衡及液体供给饱和检查。

[燃油相平衡与液体供给边界](FUEL_PHASE_EQUILIBRIUM.zh-CN.md)

## 下一步开发

下一步使用显式物性和守恒的体积/功耦合，实现油箱蒸气转移及两相泵/燃轨流动。缺失的 OEM 测量保持缺失。点火、进/排气、机械损失和 ECU/TCU 协同仍需分别推进。

## 验收

为新方程提供独立解析或极限参考、完整质量/能量账本及适当的步长细化。保持整批回滚、取消、分支、稳定通道、资产回放和旧资产读取。之后继续点火、进/排气、机械损失、变速器驱动与 ECU/TCU 协同控制。

参数仍为 `unverified`。模型未提供实测汽油属性、多组分或有限速率相变动力学、分解液体位移/喷雾及 OEM 标定。实际 Unity Editor/Play/Player/IL2CPP 验收仍待完成。

[DEVELOPMENT_STATUS.zh-CN.md](DEVELOPMENT_STATUS.zh-CN.md) · [ROADMAP.zh-CN.md](ROADMAP.zh-CN.md) · [VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)
