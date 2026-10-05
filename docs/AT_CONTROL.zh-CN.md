# 液压 AT 反馈控制

[English](AT_CONTROL.md) · **简体中文** · [Français](AT_CONTROL.fr.md) · [Русский](AT_CONTROL.ru.md) · [日本語](AT_CONTROL.ja.md) · [한국어](AT_CONTROL.ko.md) · [Deutsch](AT_CONTROL.de.md) · [Español](AT_CONTROL.es.md) · [Italiano](AT_CONTROL.it.md) · [Português](AT_CONTROL.pt-BR.md)

## 接口约定

`at_controller` 接受 [-1,4] 范围内的整数目标挡位；零表示空挡。它管理五组充油/泄压阀以及可选的变矩器锁止支路。支路顺序为行星架输入、小太阳轮输入、大太阳轮输入、行星架制动、大太阳轮制动，最后是锁止。

施加冲突挡位前，控制器根据实际摩擦片压紧力确认释放。有限幅值的压力 PI 使用实测腔室压力。只有所需接触和离合器实际锁定得到确认，才报告挡位已接合。小数挡位和直接写入受控阀通道会返回可操作的错误。

采样阶段、故障、压力积分和时间状态纳入完整事务状态。取消、后期失败和分支保留相同历史。故障包括释放/接合超时、供压不足、方向切换及已确认锁定丢失。泄压命令无法释放物理上堵塞的排油支路。

可选锁止使用前进挡、输入转速、滑差和驻留时间限制，并设置独立的解锁滞回。输出描述实际 Released/Applying/Locked/Releasing 状态。锁止是物理活塞离合器，不是转速相等命令。

## 证据与限制

示例 `controlled-hydraulic-ravigneaux` 和 `controlled-fired-hydraulic-ravigneaux` 使用请求通道 900、控制器 ID 1400。两者分别保留 99 和 122 个报告状态，未改变 128 状态上限。资产 v25 保留支路、增益和时钟，并可读取 v1-v24。

这是研究控制，参数仍为 `unverified`。协调 ECU 扭矩融合、详细传感器/阀模型、完整车辆故障及 OEM 标定仍待完成。托管和 Standard 检查不能证明实际 Unity Editor/Play/Player/IL2CPP 验收。

