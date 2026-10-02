# 原生 Zig 边界

[English](NATIVE_ZIG.md) · **简体中文** · [Français](NATIVE_ZIG.fr.md) · [Русский](NATIVE_ZIG.ru.md) · [日本語](NATIVE_ZIG.ja.md) · [한국어](NATIVE_ZIG.ko.md) · [Deutsch](NATIVE_ZIG.de.md) · [Español](NATIVE_ZIG.es.md) · [Italiano](NATIVE_ZIG.it.md) · [Português](NATIVE_ZIG.pt-BR.md)

所有者于 2026-09-10 恢复原生语言迁移。`legacy/native` 下的原生原型已迁移到 Zig,包括其测试与宿主。它们仍是独立的研究运行时:`Power.Core` 与 `Power.Assets` 保持无依赖的托管契约与双目标,Unity 继续加载托管程序集。

## 互操作

原生共享库保留带版本的 `pwr_get_api` 入口、定宽标量字段、结构体大小、整数纳秒时间、世代句柄、调用方持有的快照缓冲区与函数表。Zig 的 `extern struct` 声明与 `.c` 调用约定表达现有二进制 ABI;本仓库不因此需要 C 源码或头文件。.NET 构建工具中的 C# P/Invoke 实验运行器仍是这条边界的消费者。原生发动机、排气与变速器原型仍是内部 Zig API,而不是被静默加入托管模型 schema 或公开原生能力。

迁移必须保留方程、模型限制、稳定 ID、诊断、批量回滚、能量与质量账本,以及回归场景。原生原型保真度与 `unverified` 样本标定状态不变。原生验证独立于真实的 Unity 编辑器、Play Mode 与 IL2CPP 证据,也独立于完成完整动力总成目标。

## 溯源

原始 C 实现可从 Git 提交 `c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3` 恢复。其源文件路径与哈希记录在 `legacy/native/migration-manifest.json`。Zig 移植保留原始版权,以及带 Unity 链接例外的 GPL-3.0-or-later 声明。历史设计与研究文档保留原始引文;其中 C 时代的描述并不描述新的构建。

未使用的 LuaInstaller 启动器及其打包 README 于 2026-09-11 停用。它们的原始路径与哈希包含在同一份清单中,并指向同一源修订。该启动器依赖一个未实现的 `power_native` 桥,从未属于可工作的构建。当前 CLI 与模型操作使用现有的 C#/JSON 与 Zig 宿主,加上 C# 构建工具的 P/Invoke ABI 宿主。历史文档中的 Lua 提议是溯源记录,不是当前依赖或实现要求。

<a id="build-and-maintenance"></a>
## 构建与维护

编译器在 `.zig-version` 中锁定为 Zig 0.15.2。构建工具的 `install-zig` 命令(位于 `tools/Build.cs`)使用官方 [Zig 下载元数据](https://ziglang.org/download/index.json),并带有已提交的分平台归档哈希。构建不需要 C 翻译器、头文件、CMake 或 C 源码编译。Linux 不需要 libc;macOS 使用操作系统提供的 `libSystem`。移植最初一次性译出,随后拆成维护中的 Zig 模块,并共享二进制布局。内存、数学函数与原子操作使用 Zig 与平台 OS API。ReleaseSafe 构建中安全检查保持启用。

Zig 源码在所有平台上使用 LF 检出。在 macOS 上,验证工具只在其 Zig 构建子进程中通过设置 `DEVELOPER_DIR=/dev/null` 关闭 Apple SDK 发现。这会选用 Zig 自带的 Darwin 链接器桩,并避开 Zig 0.15.2 与 [Xcode 26.4 及更新 SDK 的不兼容](https://github.com/ghostty-org/ghostty/issues/11991)——那些 SDK 的 `libSystem` 桩使用 arm64e 目标。系统的 Xcode 选择不变;这些原生目标不需要 Apple 框架或 SDK 头文件。

`dotnet run --file tools/Build.cs -- verify` 先运行托管验证,再串行运行原生验证。`native-verify` 只运行原生部分。`native-verify` 的实现(位于 `tools/Build.cs`)拒绝 C/C++ 源码与头文件,以及 Lua 源码、字节码和包。它检查编译器锁定与格式,构建库和两个 Zig 宿主,运行 Zig 套件与 C# P/Invoke ABI 宿主套件,并把电—热实验与其原始 C 基线夹具比较。在 Linux 上,它还检查公开导出的符号只有 `pwr_get_api`,且库没有未解析的外部符号。

基线比较保留模型指纹、单位、通道映射、11 个采样时刻与物理值。跨工具链的值使用显式的绝对/相对容差;回放哈希必须在同一二进制内匹配。`artifacts/reports` 下的报告把执行、KPI/回放结果与未核实标定区分开。被忽略的编译器缓存与私有第三方参考检出不是仓库源码。

## 存档测试修正

三个旧测试入口即使 `CHECK` 宏失败也返回零。移植会传播这些失败。发动机套件先前停在一处隐藏失败上,因为其最终 22 Nm 载荷样本可能处于转速限制器断油。该场景保留为显式的限制器测试;持续燃烧使用合成的 32 Nm 测试载荷。背压比较现在从共享的运行状态开始,并在比较转速之前检查解析泵气扭矩增量,以避免启动/熄火混淆。发动机方程与生产标定值都没有改动。自动变速动力总成覆盖现在也执行回放、差速器能量、欠压与回滚;旧的 CMake 构建省略了整个模块。
