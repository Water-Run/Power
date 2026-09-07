# Agent 开发接口

`Power.Core`、`Power.Agent` 与 MCP 是同一套物理核心的不同入口。API 不绑定特定 GPT 版本或供应商。先用工具获取版本、能力和 Schema，再生成模型；不要根据名称猜组件已经实现。

## 启动与客户端配置

```sh
dotnet run --file tools/Build.cs -- build
dotnet /absolute/path/to/Power!/src/Power.Mcp/bin/Release/net10.0/Power.Mcp.dll
```

通用 MCP 客户端配置示例，按客户端格式放入 server 配置；路径需要替换：

```json
{
  "mcpServers": {
    "power": {
      "command": "dotnet",
      "args": ["/absolute/path/to/Power!/src/Power.Mcp/bin/Release/net10.0/Power.Mcp.dll"]
    }
  }
}
```

Windows 同样使用 `dotnet` 和 DLL 的绝对路径。生产连接应直接运行构建好的 DLL，避免构建输出混入 stdio 协议。服务无需 Unity、凭据或网络；首次 NuGet 还原需要网络。协议传输与版本兼容由固定的官方 [MCP C# SDK](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/getting-started.html)处理。

## 工具与结果

| 工具 | 用途 |
|---|---|
| `get_capabilities` | 版本、模型能力、规模限制、时间语义与工作流 |
| `get_model_schema` | 完整 `power.model.v1` JSON Schema |
| `get_example_model` | 带事件和 KPI 的可编辑示例 |
| `validate_model` | 校验模型和实验，返回指纹、通道及诊断，不运行时序 |
| `run_experiment` | 完整实验、两个批大小的回放、KPI 与来源；默认紧凑结果 |
| `export_model_asset` | 校验并导出 `.powerasset`，返回 Base64 内容、文件摘要、来源和模型指纹 |
| `create_session` | 创建独立交互仿真，返回初始快照和通道信息 |
| `read_snapshot` | 当前时间、版本、哈希、可选择的输出通道 |
| `set_inputs` | 在当前时刻原子提交输入帧，增加会话版本 |
| `step_session` | 原子推进指定纳秒数，支持取消，增加会话版本 |
| `fork_session` | 复制当前物理状态，创建版本为 0 的新分支 |
| `close_session` | 释放指定会话 |

所有工具都有输入和输出 Schema；成功或领域错误均提供 `structuredContent` 与兼容文本结果。MCP `isError` 对应 `ok=false`。[SDK 结构化工具结果](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/tools/tools.html)。

```json
{"schema":"power.agent.v1","ok":true,"data":{"revision":"2","time_ns":"1000000000","state_hash":"...","values":[]}}
```

```json
{"schema":"power.agent.v1","ok":false,"error":{"code":"revision_conflict","message":"Read the snapshot, then use its current revision.","retryable":true,"current_revision":"2"}}
```

参考 [模型 Schema](../schemas/power.model.v1.schema.json)、[响应 Schema](../schemas/power.agent.v1.schema.json)。模型 Schema 检查结构，编译器继续检查量纲、拓扑、正值、有限值和数值系统；实验校验继续检查 tick 对齐、事件顺序、通道和 KPI 上下界。

## 完整操作序列

1. 调用 `get_capabilities`，确认所需物理组件已支持。
2. 获取示例和 Schema，构造 `document` 对象；参数必须注明单位。
3. `validate_model({"document": ...})`，按 `error.object_id`、`error.field`、`error.code` 修复模型。
4. `run_experiment({"document": ...})`，检查 `data.passed`、`checks`、`replay`、`model.calibration`。`ok=true` 只说明实验完成，KPI 可能失败。
5. 用同一文档 `create_session`，保存 `session_id`、初始 `revision` 和通道映射。
6. 例如 `set_inputs({"session_id":"...","expected_revision":"0","values":[{"channel":"100","value":24}]})`，读取返回的新版本。
7. `step_session({"session_id":"...","expected_revision":"1","delta_ns":"1000000000"})`，取得 1 秒后的快照。
8. `fork_session({"session_id":"...","expected_revision":"2"})`，在子会话用 4 V 制动，保留父会话作为对照。
9. 完成比较后用各自最新版本 `close_session`。

需要在 Unity 中检查模型时，调用 `export_model_asset({"document": ..., "name": "My laboratory"})`。将 `data.content` 按 Base64 解码，核对完整文件的 `data.asset_sha256`，保存为 Unity `Assets` 下的 `.powerasset`，再通过资产 Inspector 的 **Open in Studio** 打开。此工具只返回内容，不写本地文件；导出成功只表示数据合法，KPI 与标定仍需单独检查。格式及限制见 [模型资产](ASSET_FORMAT.md)。

版本从 0 开始，每次成功输入提交和步进增加 1。过期、无效和取消的操作不增加版本。分支父会话版本保持不变。任何传输中断后先读快照确认版本，再决定后续操作；不要直接重发带旧版本的写命令。

会话快照的 `time_ns`、`revision`、通道 ID 都是字符串，避免超过 JavaScript 精确整数范围。模型文档中的实验时间限制在一小时以内；模型输入通道目前使用整数，建议选择不超过 `2^53-1` 的 ID 以保证经过其他 JSON 客户端时精确。输出的高位命名空间 ID 应原样保留为字符串。

`read_snapshot` 的 `channels` 是输出 ID 字符串数组，省略则返回所有输出；不接受输入通道和重复字段。`include_samples=true` 才会让实验返回所有采样边界。

## 错误与修复

| 错误 | 下一步 |
|---|---|
| `model_unit` / `model_connection` / `model_range` | 根据对象和字段修复单位、引用或参数 |
| `invalid_argument` / `invalid_json` | 修正字段、事件顺序、时间或文档结构 |
| `unknown_channel` / `invalid_input` | 从通道表选择输入，消除重复与非有限值 |
| `invalid_time_step` | 使用正的整数 tick，遵守单次一百万 tick 限制 |
| `numerical_failure` | 检查参数尺度、输入和步长；当前状态没有被修改 |
| `revision_conflict` | 先读最新快照，再基于真实状态决定操作 |
| `cancelled` | 整批回滚，可缩小计算批次后重试 |
| `session_capacity` | 关闭不再需要的会话 |
| `unknown_session` | 进程重启或会话已关闭，重新创建并回放 |

会话是进程内对象，尚不支持持久化恢复或连接正在运行的 Unity 场景。MCP 接口当前用于相同核心的无界面实验；以后连接 Unity 时仍需保留版本、时间和原子性契约。

## 作为开发 Agent

组件新增流程是：写清方程与适用范围 → 定义带单位端口/参数 → 在核心中实现 → 用解析解、守恒、步长收敛与故障测试取得证据 → 增加 Schema 和能力发现 → 提供可回放实验 → 接入 Unity 显示。实测来源及不确定度独立登记，不能由测试通过推导“已校准”。
