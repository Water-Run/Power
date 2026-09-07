# Copyright (C) 2026 Power! contributors
# Licensed under GPL-3.0-or-later with the Unity Linking Exception.
# See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

function(power_limit_public_exports target_name)
    if(WIN32)
        target_sources("${target_name}" PRIVATE "${PROJECT_SOURCE_DIR}/cmake/power.def")
    elseif(APPLE)
        set(export_file "${PROJECT_SOURCE_DIR}/cmake/power.exports")
        target_link_options(
            "${target_name}"
            PRIVATE "LINKER:-exported_symbols_list,${export_file}"
        )
        set_property(TARGET "${target_name}" APPEND PROPERTY LINK_DEPENDS
                     "${export_file}")
    elseif(CMAKE_C_COMPILER_ID MATCHES "GNU|Clang")
        set(export_file "${PROJECT_SOURCE_DIR}/cmake/power.map")
        target_link_options(
            "${target_name}"
            PRIVATE "LINKER:--version-script=${export_file}"
        )
        set_property(TARGET "${target_name}" APPEND PROPERTY LINK_DEPENDS
                     "${export_file}")
    endif()
endfunction()
