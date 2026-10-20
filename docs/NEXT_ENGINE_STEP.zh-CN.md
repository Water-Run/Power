# 发动机开发恢复笔记

[English](NEXT_ENGINE_STEP.md) · **简体中文** · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 接续位置

燃料域容器已通过守恒耗散流阻与有限壁热链接交换液/汽混合物。泵充填、轴耦合和喷油器排出仍待接入。

[相平衡燃料输运网络](EQUILIBRIUM_FUEL_NETWORK.zh-CN.md)

## 下一步开发

将燃料域容器接入轴驱动泵充填与物理喷油器排出，保持物质、动量和功的守恒耦合。用独立参考求解局部入口惯性与临界流。继续点火、进/排气、机械损失、变速器执行器与 ECU/TCU 协同，不将此网络视为完整动力总成。

## 验收

为新方程提供独立解析或极限参考、完整质量/能量账本及适当的步长细化。保持整批回滚、取消、分支、稳定通道、资产回放和旧资产读取。之后继续点火、进/排气、机械损失、变速器驱动与 ECU/TCU 协同控制。

本构闭合关系的物性及可分辨壁变形边界仍适用。参数保持 `unverified`。泵充填与轴功、喷油器交接、局部流体惯性、临界流、成核延迟、相间滑移及真实混合物测量仍需单独推进。现有泵供油喷油器燃轨在完成这些耦合前仍沿用分离液体路径。

[DEVELOPMENT_STATUS.zh-CN.md](DEVELOPMENT_STATUS.zh-CN.md) · [ROADMAP.zh-CN.md](ROADMAP.zh-CN.md) · [VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)
