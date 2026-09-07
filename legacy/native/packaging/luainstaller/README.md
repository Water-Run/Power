# LuaInstaller entry

`power_cli.lua` 是可信应用 Lua 入口，只能和 Lua 5.5 ABI 匹配的 `power_native` 原生模块一起打包。它不是模型脚本入口，也不会在外层 Lua state 中执行 `.power.lua`。

当前文件是发行接口骨架；`power_native.main(arg)` 尚未实现，因此普通 CMake 构建不会创建 LuaInstaller bundle target。完成 bridge 后，由 `power_add_luainstaller_bundle(...)` 添加显式 onedir/onefile 目标。

完整边界、命令示例、ABI/许可/签名和 clean-environment 门槛见 [发行设计](../../docs/PACKAGING.md)。
