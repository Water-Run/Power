# Copyright (C) 2026 Power! contributors
# Licensed under GPL-3.0-or-later with the Unity Linking Exception.
# See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

function(power_enable_strict_warnings target_name)
    if(MSVC)
        target_compile_options(
            "${target_name}"
            PRIVATE
                /W4
                /permissive-
                $<$<BOOL:${POWER_WARNINGS_AS_ERRORS}>:/WX>
        )
        return()
    endif()

    if(CMAKE_C_COMPILER_ID MATCHES "GNU|Clang|AppleClang")
        target_compile_options(
            "${target_name}"
            PRIVATE
                -Wall
                -Wextra
                -Wpedantic
                -Wconversion
                -Wsign-conversion
                -Wshadow
                -Wstrict-prototypes
                -Wmissing-prototypes
                -Wformat=2
                -Wundef
                -Wvla
                $<$<BOOL:${POWER_WARNINGS_AS_ERRORS}>:-Werror>
        )
    endif()
endfunction()
