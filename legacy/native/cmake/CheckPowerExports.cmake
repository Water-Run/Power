# Copyright (C) 2026 Power! contributors
# Licensed under GPL-3.0-or-later with the Unity Linking Exception.
# See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

if(NOT DEFINED POWER_LIBRARY OR NOT DEFINED POWER_NM)
    message(FATAL_ERROR "POWER_LIBRARY and POWER_NM are required")
endif()

execute_process(
    COMMAND "${POWER_NM}" -D --defined-only "${POWER_LIBRARY}"
    RESULT_VARIABLE nm_result
    OUTPUT_VARIABLE nm_output
    ERROR_VARIABLE nm_error
)
if(NOT nm_result EQUAL 0)
    message(FATAL_ERROR "Unable to inspect Power! exports: ${nm_error}")
endif()

string(REGEX MATCHALL "pwr_[A-Za-z0-9_]+" pwr_exports "${nm_output}")
list(REMOVE_DUPLICATES pwr_exports)
list(LENGTH pwr_exports pwr_export_count)
if(NOT pwr_export_count EQUAL 1 OR NOT "pwr_get_api" IN_LIST pwr_exports)
    message(FATAL_ERROR "Unexpected Power! public symbols:\n${nm_output}")
endif()
