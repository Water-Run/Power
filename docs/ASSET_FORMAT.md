# Power 模型资产

`power.model.v1` JSON 是创作入口；`.powerasset` 是供跨运行时加载的模型与实验数据。`Power.Assets` 不依赖 JSON 库、Unity 或第三方包，和核心一起编译为 .NET 10 / .NET Standard 2.1。

CLI 的 `export` 命令和 MCP 的 `export_model_asset` 使用同一个编码器。Unity `ScriptedImporter` 把文件导入为 `PowerModelAsset`，仅序列化数据字节；运行时解码后重新编译模型，不加载任意代码或预存 LU 分解。默认资产由 `tools/Build.cs` 生成，可从 JSON 重建。

## 版本 1 布局

所有整数和 IEEE 754 binary64 都采用小端序。文件最多 1 MiB；字符串为严格 UTF-8。

| 顺序 | 数据 |
|---|---|
| 标识 | 8 字节 ASCII `POWERAST`、int32 格式版本 `1` |
| 模型与时间 | uint64 模型指纹、tick 纳秒数、实验时长、采样间隔 |
| 来源 | uint16 名称字节数、名称、32 字节源 JSON SHA-256 |
| 数量 | 四个 int32：节点数、组件数、输入变更数、KPI 数 |
| 描述 | 每个节点 44 字节；每个组件 156 字节；按对象 ID 排序 |
| 输入 | 每个变更 24 字节：uint64 时刻、uint64 通道、double 值 |
| KPI | 每项 33 字节：uint32 对象、int32 字段、byte 边界标志、三个 double 边界 |
| 完整性 | 前述全部字节的 SHA-256，32 字节 |

节点与组件字段顺序以 `src/Power.Assets/AssetCodec.cs` 的版本 1 编解码实现为准。数量、精确文件长度和摘要在分配描述数组之前校验。随后验证单位、拓扑、时间、事件和 KPI，并核对当前求解器编译出的模型指纹；不一致时需重新导出。

名称最多 128 个 UTF-16 码元且无控制字符；模型限制为 32 节点、64 组件、64 状态；实验最多一小时、千万 tick、万组输入时刻、65,536 条输入变更与 256 个 KPI，同时要求整数商 `duration / sample_every` 不超过 10,000，并受 1 MiB 文件上限约束。事件在 `[0, duration)` 内按绝对时刻排序、tick 对齐，同一时刻不得重复同一通道。

文件末尾摘要用于检测损坏，并非来源认证。导出结果里的 `asset_sha256` 是整个文件的摘要，包含末尾摘要字段；`source_sha256` 标识创作文档，模型指纹标识编译后的语义。修改步长后重新导出可保留原来源摘要，但会改变模型指纹。合成参数的标定状态仍是 `unverified`。

`AssetPlayback` 在时间零应用初始事件，在每次 `Advance` 内使用核心的原子事件批次；失败和取消均保留时间、状态及事件游标。单次最多一百万 tick，调用者负责分批。已验证 CLI 报告边界、资产回放和实际 MCP 导出的一致性；Unity Editor / Mono / IL2CPP 的实际运行证据尚待取得。
