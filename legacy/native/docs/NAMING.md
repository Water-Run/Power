> Historical native design record. C-era paths and build instructions refer to commit `c342d4c`; see [the native README](../README.md) for the current Zig implementation.

# Power! 项目命名约定

状态：已采用产品名；技术命名基线 v0
日期：2026-08-27

## 1. 重命名决策

项目展示名为 **Power!**，取代早期工作名 `Engine3D`。感叹号属于品牌展示，不进入源代码标识符、文件扩展名、动态库、命令、URL、环境变量或包管理器坐标；这样可避免 shell 历史展开、转义和跨平台工具链差异。

当前仓库尚无已发布 ABI、DSL 或资产，因此本次采用一次性原子迁移，不提供旧 `e3d_*`、`require("engine3d")`、`.e3d.lua` 或 `.e3dpkg` 兼容别名。旧名只允许出现在重命名历史和上游差异记录中。

## 2. 唯一技术标识

| 层面 | 约定 |
|---|---|
| 产品文字 | `Power!` |
| 仓库/发布 slug | `power-sim`，公开托管前确认可用性 |
| C 头文件 | `include/power/`，公开入口 `<power/power.h>` |
| C 标识符 | 函数/类型使用 `pwr_`，宏使用 `PWR_` |
| 库 | `libpower.so`、`libpower.a`；CMake 导出目标 `Power::SDK` |
| 可执行文件 | `power_cli`、`power_studio` 等不含 `!` 的名称 |
| Lua | `local pwr = require("power")`；官方 ID 使用 `power.*` |
| Lua 源/资产包 | `*.power.lua`、`*.powerpkg` |
| 环境变量 | `POWER_*` |
| Issue/Epic | `PWR-001` 形式 |

文档和 UI 必须写 `Power!`；代码示例必须使用上表中的技术标识，不能自行创造 `power!`、`power3d`、`e3d` 等变体。第三方动力总成和材料 ID 应使用反向域名，例如 `org.example.engine.i4_2l`，不依赖产品名称维持唯一性。

## 3. 发布前检查

- 完成商标、搜索结果混淆、代码托管组织、域名和主要软件包注册表检查；这决定发布坐标，不反向改变已冻结的 `pwr_` C 命名空间。
- 在 P0 添加命名一致性检查：除允许的历史文档外，拒绝旧名、旧扩展名和旧符号前缀。
- 模型指纹包含 DSL 与包格式版本。本次重命名前若产生过本地缓存，应直接失效并重建，不尝试猜测迁移。
- 首个公开预览版发布后，任何命名变化都必须走 ABI/DSL 迁移政策，不能再做无记录的全局替换。
