cmake_minimum_required(VERSION 3.20)
project(3c_acl_runtime LANGUAGES CXX)

include(FetchContent)

FetchContent_Declare(
    acl
    GIT_REPOSITORY https://github.com/nfrechette/acl.git
    GIT_TAG 414689d5cff4286a7898487a46dc5e48005d38da
    GIT_SHALLOW TRUE)
FetchContent_Declare(
    rtm
    GIT_REPOSITORY https://github.com/nfrechette/rtm.git
    GIT_TAG d7982f2b2524feeb322f424b29cf43df30b5d5b7
    GIT_SHALLOW TRUE)
FetchContent_GetProperties(acl)
if(NOT acl_POPULATED)
    FetchContent_Populate(acl)
endif()
FetchContent_GetProperties(rtm)
if(NOT rtm_POPULATED)
    FetchContent_Populate(rtm)
endif()

add_library(3c_acl_runtime SHARED
    acl_compression.cpp
    acl_decoder.cpp
    acl_resources.cpp)
target_compile_features(3c_acl_runtime PRIVATE cxx_std_17)
target_include_directories(3c_acl_runtime PRIVATE
    ${acl_SOURCE_DIR}/includes
    ${rtm_SOURCE_DIR}/includes)
target_compile_definitions(3c_acl_runtime PRIVATE ACL_NO_ASSERT_CHECKS)

if(MSVC)
    target_compile_options(3c_acl_runtime PRIVATE /fp:precise /EHsc /GR- /permissive-)
else()
    target_compile_options(3c_acl_runtime PRIVATE -ffp-model=precise)
endif()

set_target_properties(3c_acl_runtime PROPERTIES OUTPUT_NAME "3c_acl_runtime")
