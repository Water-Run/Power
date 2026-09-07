include_guard(GLOBAL)

include(CMakeParseArguments)

# Add an explicit, opt-in LuaInstaller bundle target for a trusted Lua entry.
# The native Lua module must already be a CMake target built against the same
# Lua major.minor ABI selected by LUA_EXECUTABLE and LUA_PREFIX.
function(power_add_luainstaller_bundle)
    set(options)
    set(one_value_args
        TARGET
        ENTRY
        NATIVE_TARGET
        LUA_EXECUTABLE
        LUA_PREFIX
        OUTPUT
        MODE
        MAX_DEPS
        LUAINSTALLER_EXECUTABLE
    )
    set(multi_value_args DEPENDS)
    cmake_parse_arguments(
        POWER_LUAI
        "${options}"
        "${one_value_args}"
        "${multi_value_args}"
        ${ARGN}
    )

    foreach(required_argument IN ITEMS TARGET ENTRY NATIVE_TARGET
                                       LUA_EXECUTABLE LUA_PREFIX OUTPUT)
        if(NOT POWER_LUAI_${required_argument})
            message(
                FATAL_ERROR
                "power_add_luainstaller_bundle requires ${required_argument}"
            )
        endif()
    endforeach()

    if(POWER_LUAI_UNPARSED_ARGUMENTS)
        message(
            FATAL_ERROR
            "Unknown power_add_luainstaller_bundle arguments: "
            "${POWER_LUAI_UNPARSED_ARGUMENTS}"
        )
    endif()
    if(NOT TARGET "${POWER_LUAI_NATIVE_TARGET}")
        message(
            FATAL_ERROR
            "LuaInstaller native target does not exist: "
            "${POWER_LUAI_NATIVE_TARGET}"
        )
    endif()

    cmake_path(
        ABSOLUTE_PATH POWER_LUAI_ENTRY
        BASE_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}"
        NORMALIZE
    )
    if(NOT EXISTS "${POWER_LUAI_ENTRY}")
        message(FATAL_ERROR "LuaInstaller entry does not exist: ${POWER_LUAI_ENTRY}")
    endif()
    cmake_path(
        ABSOLUTE_PATH POWER_LUAI_OUTPUT
        BASE_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}"
        NORMALIZE
    )

    if(NOT POWER_LUAI_MODE)
        set(POWER_LUAI_MODE onedir)
    endif()
    if(POWER_LUAI_MODE STREQUAL "onedir")
        set(bundle_mode --dir)
    elseif(POWER_LUAI_MODE STREQUAL "onefile")
        set(bundle_mode --file)
    else()
        message(FATAL_ERROR "LuaInstaller MODE must be onedir or onefile")
    endif()

    if(NOT POWER_LUAI_MAX_DEPS)
        set(POWER_LUAI_MAX_DEPS 120)
    endif()
    if(NOT POWER_LUAI_MAX_DEPS MATCHES "^[1-9][0-9]*$")
        message(FATAL_ERROR "LuaInstaller MAX_DEPS must be a positive integer")
    endif()

    if(NOT POWER_LUAI_LUAINSTALLER_EXECUTABLE)
        find_program(
            POWER_LUAI_LUAINSTALLER_EXECUTABLE
            NAMES luainstaller
            DOC "LuaInstaller executable used for Power! application bundles"
        )
    endif()
    if(NOT POWER_LUAI_LUAINSTALLER_EXECUTABLE)
        message(FATAL_ERROR "luainstaller was not found")
    endif()
    if(NOT EXISTS "${POWER_LUAI_LUA_EXECUTABLE}")
        message(FATAL_ERROR "Lua executable does not exist: ${POWER_LUAI_LUA_EXECUTABLE}")
    endif()
    if(NOT IS_DIRECTORY "${POWER_LUAI_LUA_PREFIX}")
        message(FATAL_ERROR "Lua prefix is not a directory: ${POWER_LUAI_LUA_PREFIX}")
    endif()

    set(
        native_cpath
        "$<TARGET_FILE_DIR:${POWER_LUAI_NATIVE_TARGET}>/?${CMAKE_SHARED_MODULE_SUFFIX};;"
    )
    set(common_arguments
        "${POWER_LUAI_ENTRY}"
        --lua "${POWER_LUAI_LUA_EXECUTABLE}"
        --lua-prefix "${POWER_LUAI_LUA_PREFIX}"
        --max-deps "${POWER_LUAI_MAX_DEPS}"
    )

    add_custom_target(
        "${POWER_LUAI_TARGET}"
        COMMAND
            "${CMAKE_COMMAND}" -E env
            "LUA_PATH="
            "LUA_CPATH=${native_cpath}"
            "${POWER_LUAI_LUAINSTALLER_EXECUTABLE}"
            analyze ${common_arguments}
        COMMAND
            "${CMAKE_COMMAND}" -E env
            "LUA_PATH="
            "LUA_CPATH=${native_cpath}"
            "${POWER_LUAI_LUAINSTALLER_EXECUTABLE}"
            build "${bundle_mode}" ${common_arguments}
            -o "${POWER_LUAI_OUTPUT}"
        DEPENDS
            "${POWER_LUAI_NATIVE_TARGET}"
            "${POWER_LUAI_ENTRY}"
            ${POWER_LUAI_DEPENDS}
        COMMENT
            "Building ${POWER_LUAI_MODE} LuaInstaller bundle "
            "${POWER_LUAI_OUTPUT}"
        VERBATIM
        USES_TERMINAL
    )
endfunction()
