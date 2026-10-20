# 发动机开发恢复笔记

[English](NEXT_ENGINE_STEP.md) · **简体中文** · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## 接续位置

有限燃料腔体已与轴交换压力功，并通过理想单向流阻充填和排出，涵盖入口不足时的体相相变。物理喷油器及接收端耦合仍待实现。

[轴驱动相变燃料泵](FUEL_CHAMBER_PUMP.zh-CN.md)

## 下一步开发

将燃料域泵接入物理喷油器及气缸/油膜接收端，显式核算两相输运、排量和动能功。用独立参考求解局部入口惯性及临界流。继续点火、进/排气、机械损失、变速器执行器及 ECU/TCU 协同，保持完整动力总成目标。

## 验收

为新方程提供独立解析或极限参考、完整质量/能量账本及适当的步长细化。保持整批回滚、取消、分支、稳定通道、资产回放和旧资产读取。之后继续点火、进/排气、机械损失、变速器驱动与 ECU/TCU 协同控制。

参数保持 `unverified`。整个循环均遵守物性温度、压力及可分辨体积边界。模型假设不可压缩液体、理想伪组分蒸气、瞬时共同温度平衡及零外部壳体压力。局部入口惯性/NPSH、临界流、成核延迟、阀惯性/滞回、真实燃料混合物及实测标定仍待实现。物理喷油器交接与接收端排量/动能功耦合仍是独立要求。

[DEVELOPMENT_STATUS.zh-CN.md](DEVELOPMENT_STATUS.zh-CN.md) · [ROADMAP.zh-CN.md](ROADMAP.zh-CN.md) · [VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)
