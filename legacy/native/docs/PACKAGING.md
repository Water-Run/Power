> Historical native design record. C-era paths and build instructions refer to commit `c342d4c`; see [the native README](../README.md) for the current Zig implementation.
> The unused LuaInstaller launcher and its packaging README were retired on 2026-09-11. The proposals below are preserved for provenance; the current project has no Lua runtime or packaging dependency.

# Power! 发行与 LuaInstaller 打包设计

状态：发行架构已选定；LuaInstaller 接入骨架已加入，`power_native` Lua 5.5 桥接模块尚未实现  
目标工具：[Water-Run/luainstaller](https://github.com/Water-Run/luainstaller) 1.3.x  
目标 Lua ABI：官方 Lua 5.5（首个 vendoring 基线为 5.5.1）

## 1. 结论

Power! 可以通过 LuaInstaller 生成 onedir 和 onefile 的独立命令行/桌面启动程序，但 LuaInstaller 只负责“可信 Lua 应用层”的可执行分发，不能成为所有产物的唯一包格式。

发行物分成三类：

| 产物 | 发行机制 | 原因 |
|---|---|---|
| `power_cli` / 可选可信工具壳 | LuaInstaller onedir 为发布基线，onefile 为便利下载 | Lua 入口适合组织 CLI、工具流程和原生 `power_native` 模块；最终机器不需要系统 `lua` |
| `libpower`、头文件和游戏引擎插件 | CMake install + CPack/平台 SDK 包 | 游戏宿主需要 `.so/.dll/.dylib`、导入库、头、CMake config 和符号/ABI 元数据，而不是一个自解压 Lua 可执行文件 |
| `.power.lua` 模型/完整动力总成资产 | Power! 自己的签名/哈希化 `.pwrpkg`（待实现） | 模型由私有受限 Model VM 读取；资产必须保留来源、单位、依赖、模型 ABI、标定状态和 VFS 沙箱边界，不能当作可信应用入口执行 |

因此，“支持 LuaInstaller 打包”不等于让游戏插件依赖外部 Lua，也不等于把 DJS/DQ200 或 EC5/AT8 资产编译成不可检查的 executable。

## 2. 双 Lua 平面

LuaInstaller 会创建自己的官方 Lua state 并打开标准库。Power! 仍需要一个独立、受限的 Model VM；两者的权限和生命周期不可混用：

```text
LuaInstaller executable
└─ Trusted App Lua 5.5
   ├─ CLI / desktop orchestration / filesystem UI
   └─ require("power_native")       # Lua C 模块，只包装公开 C ABI
      └─ libpower / Power! core
         └─ private prefixed Lua 5.5 Model VM
            └─ untrusted *.power.lua (VFS、内存/指令限额、无 io/os/debug/loadlib)
```

- 外层 `power_native` 不暴露内部 `lua_State*`，只把 Lua table/标量转换为版本化 `pwr_*` C API 请求。
- 内部 Lua 符号隐藏或前缀化，避免与 LuaInstaller launcher 的 Lua 符号互相解析。
- 外层 Lua 是受信任应用代码，可以使用必要的文件/终端功能；用户模型只能进入私有 Model VM。
- 游戏引擎直接调用 C ABI，不创建外层 LuaInstaller state。
- 外层模块名固定为 `power_native`；内部模型 DSL 仍使用 `require("power")`，避免名字碰撞。

## 3. ABI 与原生依赖约束

LuaInstaller 要求分析器所用解释器、Lua 头文件、链接运行库和所有复制的 Lua C 模块具有同一 `major.minor` ABI。Power! 发布构建必须同时固定：

- 官方 Lua `5.5.x` 解释器和 `LUAI_LUA_PREFIX`；
- LuaInstaller 精确版本/rockspec 与源码 hash；
- 构建 `power_native` 所用 Lua 头和 ABI；
- OS family、CPU architecture、编译器/系统 ABI；
- `libpower` ABI、模型 ABI、资产 schema 和构建 ID。

LuaInstaller 不进行交叉编译，也不会递归收齐原生模块依赖的所有系统动态库。发行策略因此是：

1. 在目标同类 OS/架构的原生 runner 上构建；
2. `power_native` 优先静态链接 Power! headless runtime，避免漏掉相邻 `libpower`；若必须动态链接，则由 Power! 的 staging/audit 明确复制依赖并设置可搬迁 RPATH；
3. SDL、GPU、音频和平台库只进入 Studio 发行物，不污染 headless CLI/SDK；
4. 对最终目录运行 `ldd`/`readelf`、`otool` 或 PE dependency audit，并在无开发树/无系统 Lua 环境中启动测试。

本机当前 `luainstaller 1.3.0` 安装在 Lua 5.4 tree 中，只能用于研究其接口；它不能生成 Power! 的正式 Lua 5.5 包。正式构建必须先提供匹配的 Lua 5.5 解释器、头和运行库。

## 4. 构建流水线

计划中的 onedir 流水线：

```bash
# 1. 用同一 Lua 5.5 prefix 构建并测试 power_native
cmake -S . -B build-release \
  -DCMAKE_BUILD_TYPE=Release \
  -DPOWER_BUILD_LUA_NATIVE=ON \
  -DPOWER_LUA_EXECUTABLE=/opt/power-lua55/bin/lua \
  -DPOWER_LUA_PREFIX=/opt/power-lua55
cmake --build build-release --parallel
ctest --test-dir build-release --output-on-failure

# 2. 先分析，再生成可检查的目录包
LUA_PATH='' \
LUA_CPATH='build-release/lua/?.so;;' \
luainstaller analyze packaging/luainstaller/power_cli.lua \
  --lua /opt/power-lua55/bin/lua \
  --lua-prefix /opt/power-lua55 \
  --max-deps 120

LUA_PATH='' \
LUA_CPATH='build-release/lua/?.so;;' \
luainstaller build --dir packaging/luainstaller/power_cli.lua \
  --lua /opt/power-lua55/bin/lua \
  --lua-prefix /opt/power-lua55 \
  --max-deps 120 \
  -o dist/power-cli

# 3. 在完全清空 Lua 搜索路径的环境运行包内 executable
env -u LUA_PATH -u LUA_CPATH dist/power-cli/power-cli --version
```

onedir 是发布/诊断基线，因为其中的 manifest、原生模块、generated C、许可和 relinking 资料可检查。只有同一 onedir 已通过后才生成 onefile；onefile 启动时会解压到受控缓存，不能假设“真正不落盘”。

仓库中的 `power_add_luainstaller_bundle(...)` CMake helper 将来由 `power_native` 目标调用，自动添加 analyze/build 目标；它默认不参加普通 C23 构建，避免开发者仅构建 `libpower` 时被外部 Lua 工具阻塞。

## 5. 发行门槛

每个 LuaInstaller 产物必须通过：

- LuaInstaller、Lua、`power_native` 与打包 native module 的 ABI 一致性探针；
- onedir 在 `LUA_PATH`/`LUA_CPATH` 清空、系统 `lua` 不可见且工作目录任意时启动；
- manifest 中入口、Lua/native 模块、平台、Lua ABI 和 SHA-256 完整；
- 生成两次的内容可复现性检查，记录编译器与 build ID；
- 原生依赖闭包、RPATH、导出符号、ASan/UBSan/长稳与恶意输入测试；
- `THIRD_PARTY_NOTICES`、Lua MIT、LuaInstaller LGPL/GPL 文本、generated C、完整应用/模块对应源码和 relinking 说明随包保留；
- onefile/目录可执行文件由发行渠道额外签名或提供校验清单；LuaInstaller 的 payload ID 不是真实性/防篡改边界；
- 命名样例的 `Research / Unverified calibration` 状态、来源清单与能力降级信息不能被打包流程剥离。

LuaInstaller 生成的是应用分发形式，不是 DEB/RPM/MSI/PKG。系统安装、桌面文件、图标、卸载、SDK 发现与包管理器元数据由 CPack/平台打包层继续负责。

## 6. 仍需实现

- vendor 并前缀化官方 Lua 5.5.1，建立与 LuaInstaller 共存的符号测试；
- 新建 `power_runtime` 静态对象层和 Lua 5.5 `power_native` bridge；
- 实现可信 CLI 的 context/model/instance/run/validate/replay 子命令；
- 加入 LuaInstaller onedir/onefile CI matrix 和 clean-environment smoke test；
- 为模型资产定义与 executable 分离的 `.pwrpkg` manifest、签名、依赖锁与 VFS 加载；
- 为 SDK/Studio 增加 CPack 组件包和 SBOM/许可证聚合。
