-- Copyright (C) 2026 Power! contributors
-- Licensed under GPL-3.0-or-later with the Unity Linking Exception.
-- See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

-- Trusted application entry for a future LuaInstaller distribution.
-- User-authored *.power.lua files are never executed in this Lua state; the
-- native bridge passes them to libpower's separate sandboxed Model VM.

local ok, power_native = pcall(require, "power_native")
if not ok then
    io.stderr:write(
        "Power! native module is unavailable or has the wrong Lua ABI:\n",
        tostring(power_native),
        "\n"
    )
    os.exit(70, true)
end

if type(power_native) ~= "table" or type(power_native.main) ~= "function" then
    io.stderr:write("power_native does not provide the required main(arg) API\n")
    os.exit(70, true)
end

local call_ok, exit_code = xpcall(function()
    return power_native.main(arg)
end, debug.traceback)

if not call_ok then
    io.stderr:write(tostring(exit_code), "\n")
    os.exit(70, true)
end
if exit_code == nil then
    exit_code = 0
end
if type(exit_code) ~= "number" or exit_code % 1 ~= 0
    or exit_code < 0 or exit_code > 255 then
    io.stderr:write("power_native.main returned an invalid exit status\n")
    os.exit(70, true)
end

os.exit(exit_code, true)
