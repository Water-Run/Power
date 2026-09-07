cmake_minimum_required(VERSION 3.25)

project(
    Power
    VERSION 0.2.0
    DESCRIPTION "A game-integrable powertrain simulation runtime"
    LANGUAGES C
)

list(APPEND CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/cmake")

include(CTest)
include(GNUInstallDirs)
include(PowerExports)
include(PowerLuaInstaller)
include(PowerWarnings)

option(POWER_BUILD_EXAMPLES "Build the Power! C host example" ON)
option(POWER_WARNINGS_AS_ERRORS "Treat Power! compiler warnings as errors" ON)

set(CMAKE_C_STANDARD 23)
set(CMAKE_C_STANDARD_REQUIRED YES)
set(CMAKE_C_EXTENSIONS NO)

add_library(power_core STATIC
    src/core/pwr_graph.c
    src/core/pwr_graph.h
    src/core/pwr_scheduler.c
    src/core/pwr_scheduler.h
)
set_target_properties(power_core PROPERTIES POSITION_INDEPENDENT_CODE ON)
target_compile_features(power_core PUBLIC c_std_23)
target_include_directories(
    power_core
    PUBLIC "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src/core>"
           "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>"
)
if(NOT MSVC)
    target_link_libraries(power_core PUBLIC m)
endif()
power_enable_strict_warnings(power_core)

add_library(power_models STATIC
    src/models/automatic/pwr_automatic.c
    src/models/automatic/pwr_automatic.h
    src/models/drivetrain/pwr_dct.c
    src/models/drivetrain/pwr_dct.h
    src/models/engine/pwr_si_engine.c
    src/models/engine/pwr_si_engine.h
    src/models/exhaust/pwr_exhaust.c
    src/models/exhaust/pwr_exhaust.h
    src/models/powertrain/pwr_dct_powertrain.c
    src/models/powertrain/pwr_dct_powertrain.h
    src/models/shaft/pwr_shaft_lab.c
    src/models/shaft/pwr_shaft_lab.h
)
set_target_properties(power_models PROPERTIES POSITION_INDEPENDENT_CODE ON)
target_compile_features(power_models PUBLIC c_std_23)
target_include_directories(
    power_models
    PUBLIC
        "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src/models/automatic>"
        "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src/models/drivetrain>"
        "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src/models/engine>"
        "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src/models/exhaust>"
        "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src/models/powertrain>"
        "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src/models/shaft>"
)
target_link_libraries(power_models PUBLIC power_core)
if(NOT MSVC)
    target_link_libraries(power_models PUBLIC m)
endif()
power_enable_strict_warnings(power_models)

add_library(power SHARED)
add_library(Power::power ALIAS power)

target_sources(
    power
    PRIVATE
        src/sdk/pwr_api.c
        src/sdk/pwr_context.c
        src/sdk/pwr_instance.c
        src/sdk/pwr_model.c
        src/sdk/pwr_shaft_backend.c
        src/sdk/pwr_internal.h
    PUBLIC
        FILE_SET public_headers
        TYPE HEADERS
        BASE_DIRS include
        FILES include/power/power.h include/power/model.h
)
target_link_libraries(power PRIVATE power_models)

target_compile_features(power PUBLIC c_std_23)
target_compile_definitions(
    power
    PRIVATE
        PWR_BUILDING_LIBRARY=1
        PWR_BUILD_VERSION="${PROJECT_VERSION}"
)
target_include_directories(
    power
    PUBLIC
        "$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>"
        "$<INSTALL_INTERFACE:${CMAKE_INSTALL_INCLUDEDIR}>"
)
set_target_properties(
    power
    PROPERTIES
        C_VISIBILITY_PRESET hidden
        OUTPUT_NAME power
        SOVERSION 1
        VERSION "${PROJECT_VERSION}"
)

power_enable_strict_warnings(power)
power_limit_public_exports(power)

if(POWER_BUILD_EXAMPLES)
    add_executable(power_c_host examples/c_host.c)
    target_link_libraries(power_c_host PRIVATE Power::power)
    target_compile_features(power_c_host PRIVATE c_std_23)
    power_enable_strict_warnings(power_c_host)

    add_executable(power_model_host examples/model_host.c)
    target_link_libraries(power_model_host PRIVATE Power::power)
    target_compile_features(power_model_host PRIVATE c_std_23)
    power_enable_strict_warnings(power_model_host)
endif()

if(BUILD_TESTING)
    add_executable(power_graph_tests tests/test_graph.c)
    target_link_libraries(power_graph_tests PRIVATE power_core)
    target_compile_features(power_graph_tests PRIVATE c_std_23)
    power_enable_strict_warnings(power_graph_tests)
    add_test(NAME power.graph COMMAND power_graph_tests)

    add_executable(power_model_sdk_tests tests/test_model_sdk.c)
    target_link_libraries(power_model_sdk_tests PRIVATE Power::power)
    if(NOT MSVC)
        target_link_libraries(power_model_sdk_tests PRIVATE m)
    endif()
    target_compile_features(power_model_sdk_tests PRIVATE c_std_23)
    power_enable_strict_warnings(power_model_sdk_tests)
    add_test(NAME power.model_sdk COMMAND power_model_sdk_tests)

    find_package(Python3 COMPONENTS Interpreter QUIET)
    if(Python3_Interpreter_FOUND)
        add_test(NAME power.model_lab
            COMMAND "${Python3_EXECUTABLE}" "${CMAKE_CURRENT_SOURCE_DIR}/tests/test_model_lab.py"
                    "$<TARGET_FILE:power>")
    endif()

    add_executable(power_scheduler_tests tests/test_scheduler.c)
    target_link_libraries(power_scheduler_tests PRIVATE power_core)
    target_compile_features(power_scheduler_tests PRIVATE c_std_23)
    power_enable_strict_warnings(power_scheduler_tests)
    add_test(NAME power.scheduler COMMAND power_scheduler_tests)

    add_executable(power_shaft_tests tests/test_shaft.c)
    target_link_libraries(power_shaft_tests PRIVATE power_models)
    target_compile_features(power_shaft_tests PRIVATE c_std_23)
    power_enable_strict_warnings(power_shaft_tests)
    add_test(NAME power.shaft COMMAND power_shaft_tests)

    add_executable(power_exhaust_tests tests/test_exhaust.c)
    target_link_libraries(power_exhaust_tests PRIVATE power_models)
    target_compile_features(power_exhaust_tests PRIVATE c_std_23)
    power_enable_strict_warnings(power_exhaust_tests)
    add_test(NAME power.exhaust COMMAND power_exhaust_tests)

    add_executable(power_dct_tests tests/test_dct.c)
    target_link_libraries(power_dct_tests PRIVATE power_models)
    target_compile_features(power_dct_tests PRIVATE c_std_23)
    power_enable_strict_warnings(power_dct_tests)
    add_test(NAME power.dct COMMAND power_dct_tests)

    add_executable(power_si_engine_tests tests/test_si_engine.c)
    target_link_libraries(power_si_engine_tests PRIVATE power_models)
    target_compile_features(power_si_engine_tests PRIVATE c_std_23)
    power_enable_strict_warnings(power_si_engine_tests)
    add_test(NAME power.si_engine COMMAND power_si_engine_tests)

    add_executable(power_automatic_tests tests/test_automatic.c)
    target_link_libraries(power_automatic_tests PRIVATE power_models)
    target_compile_features(power_automatic_tests PRIVATE c_std_23)
    power_enable_strict_warnings(power_automatic_tests)
    add_test(NAME power.automatic COMMAND power_automatic_tests)

    add_executable(power_dct_powertrain_tests tests/test_dct_powertrain.c)
    target_link_libraries(power_dct_powertrain_tests PRIVATE power_models)
    target_compile_features(power_dct_powertrain_tests PRIVATE c_std_23)
    power_enable_strict_warnings(power_dct_powertrain_tests)
    add_test(NAME power.dct_powertrain COMMAND power_dct_powertrain_tests)

    add_executable(power_sdk_tests tests/test_sdk.c)
    target_link_libraries(power_sdk_tests PRIVATE Power::power)
    target_compile_features(power_sdk_tests PRIVATE c_std_23)
    power_enable_strict_warnings(power_sdk_tests)

    add_test(NAME power.sdk COMMAND power_sdk_tests)
    if(POWER_BUILD_EXAMPLES)
        add_test(NAME power.c_host COMMAND power_c_host)
        add_test(NAME power.model_host COMMAND power_model_host)
    endif()
    if(UNIX AND NOT APPLE AND CMAKE_NM)
        add_test(
            NAME power.exports
            COMMAND
                "${CMAKE_COMMAND}"
                "-DPOWER_LIBRARY=$<TARGET_FILE:power>"
                "-DPOWER_NM=${CMAKE_NM}"
                -P "${CMAKE_CURRENT_SOURCE_DIR}/cmake/CheckPowerExports.cmake"
        )
    endif()

    # Older tests use assert() for both calls and checks. Keep those checks
    # active in Release so optimized builds exercise the same behavior.
    get_property(power_test_targets DIRECTORY PROPERTY BUILDSYSTEM_TARGETS)
    foreach(power_test_target IN LISTS power_test_targets)
        if(power_test_target MATCHES "_tests$")
            if(MSVC)
                target_compile_options("${power_test_target}" PRIVATE /UNDEBUG)
            else()
                target_compile_options("${power_test_target}" PRIVATE -UNDEBUG)
            endif()
        endif()
    endforeach()
endif()

install(
    TARGETS power
    EXPORT PowerTargets
    FILE_SET public_headers DESTINATION "${CMAKE_INSTALL_INCLUDEDIR}"
    LIBRARY DESTINATION "${CMAKE_INSTALL_LIBDIR}"
    RUNTIME DESTINATION "${CMAKE_INSTALL_BINDIR}"
    ARCHIVE DESTINATION "${CMAKE_INSTALL_LIBDIR}"
)
install(
    EXPORT PowerTargets
    NAMESPACE Power::
    DESTINATION "${CMAKE_INSTALL_LIBDIR}/cmake/Power"
)
