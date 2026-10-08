# 发动机开发恢复笔记

[English](NEXT_ENGINE_STEP.md) · **简体中文** · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 接续位置

燃油供给已包含有限油箱、守恒泄压回流、几何气相空间与显式通气路径。泵、燃轨和气体交换内部压力功；液体供给、蒸发和预设反应保持独立。从当前契约与验证证据继续。

[油箱几何与有限气相空间](TANK_HEADSPACE.zh-CN.md)

## 下一步开发

下一项燃油开发聚焦采用显式材料属性的压力相关相平衡与空化。缺失的 OEM 测量保持缺失，研究参数保持未验证。实测泵阀充液与调压、电磁/电子驱动和分解喷雾仍需后续开发。

## 验收

为新方程提供独立解析或极限参考、完整质量/能量账本及适当的步长细化。保持整批回滚、取消、分支、稳定通道、资产回放和旧资产读取。之后继续点火、进/排气、机械损失、变速器驱动与 ECU/TCU 协同控制。

该模型采用刚性混合油箱、不可压缩液体和理想气体。晃动/静液压形状、气液相平衡、空化、实测泵阀图谱、OEM 标定及实际 Unity Editor/Play/Player/IL2CPP 仍待完成。参数仍为 `unverified`。

[DEVELOPMENT_STATUS.zh-CN.md](DEVELOPMENT_STATUS.zh-CN.md) · [ROADMAP.zh-CN.md](ROADMAP.zh-CN.md) · [VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)
