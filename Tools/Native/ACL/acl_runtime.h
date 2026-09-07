#pragma once

#include <stdint.h>

#if defined(_WIN32)
#define ACL_PROJECT_API __declspec(dllexport)
#else
#define ACL_PROJECT_API __attribute__((visibility("default")))
#endif

#define ACL_PROJECT_ABI_VERSION 2
#define ACL_PROJECT_STRUCT_VERSION 1
#define ACL_PROJECT_PAYLOAD_FORMAT_VERSION 10

typedef enum acl_project_error
{
    ACL_PROJECT_SUCCESS = 0,
    ACL_PROJECT_INVALID_ARGUMENT = 1,
    ACL_PROJECT_INVALID_PAYLOAD = 2,
    ACL_PROJECT_UNSUPPORTED_FORMAT = 3,
    ACL_PROJECT_UNSUPPORTED_TRACK_TYPE = 4,
    ACL_PROJECT_CAPACITY = 5,
    ACL_PROJECT_NATIVE_FAILURE = 6,
    ACL_PROJECT_NOT_READY = 7,
    ACL_PROJECT_INVALID_STRUCT = 8,
    ACL_PROJECT_DATABASE_FAILURE = 9,
    ACL_PROJECT_INVALID_STATE = 10,
    ACL_PROJECT_CLIP_INDEX = 11
} acl_project_error;

enum
{
    ACL_PROJECT_CAPABILITY_TRANSFORM_COMPRESSION = 1u << 0,
    ACL_PROJECT_CAPABILITY_SCALAR_COMPRESSION = 1u << 1,
    ACL_PROJECT_CAPABILITY_DATABASE = 1u << 2,
    ACL_PROJECT_CAPABILITY_GROUPS = 1u << 3,
    ACL_PROJECT_CAPABILITY_DECODER = 1u << 4,
    ACL_PROJECT_CAPABILITY_SPLIT_BULK = 1u << 5
};

typedef struct acl_project_abi_info
{
    uint32_t struct_size;
    uint32_t struct_version;
    int32_t abi_version;
    int32_t payload_format_version;
    uint32_t capabilities;
    uint32_t transform_sample_size;
    uint32_t scalar_sample_size;
    uint32_t payload_view_size;
    uint32_t group_build_input_size;
    uint32_t group_build_output_size;
    uint32_t group_payload_input_size;
    uint32_t decoder_bind_input_size;
} acl_project_abi_info;

typedef struct acl_project_transform_sample
{
    float position_x;
    float position_y;
    float position_z;
    float rotation_x;
    float rotation_y;
    float rotation_z;
    float rotation_w;
    float scale_x;
    float scale_y;
    float scale_z;
} acl_project_transform_sample;

typedef struct acl_project_transform_input
{
    uint32_t struct_size;
    uint32_t struct_version;
    uint32_t track_count;
    uint32_t sample_count;
    float sample_rate;
    const float* samples;
    const float* default_values;
    const int32_t* parent_indices;
    float precision;
    float shell_distance;
    uint8_t looping;
    uint8_t reserved[3];
} acl_project_transform_input;

typedef struct acl_project_scalar_input
{
    uint32_t struct_size;
    uint32_t struct_version;
    uint32_t track_count;
    uint32_t sample_count;
    float sample_rate;
    const float* samples;
    float precision;
    uint8_t looping;
    uint8_t reserved[3];
} acl_project_scalar_input;

typedef struct acl_project_payload
{
    const uint8_t* data;
    uint32_t length;
} acl_project_payload;

typedef struct acl_project_payload_view
{
    uint32_t struct_size;
    uint32_t struct_version;
    const uint8_t* data;
    uint32_t length;
} acl_project_payload_view;

typedef struct acl_project_database_settings
{
    uint32_t struct_size;
    uint32_t struct_version;
    float medium_importance_tier_proportion;
    float low_importance_tier_proportion;
    uint32_t max_chunk_size;
} acl_project_database_settings;

typedef struct acl_project_group_build_input
{
    uint32_t struct_size;
    uint32_t struct_version;
    uint32_t clip_count;
    const acl_project_transform_input* transform_inputs;
    const acl_project_scalar_input* scalar_inputs;
    acl_project_database_settings database_settings;
    uint8_t enable_database;
    uint8_t reserved[3];
} acl_project_group_build_input;

typedef struct acl_project_group_build_output
{
    uint32_t struct_size;
    uint32_t struct_version;
    uint32_t clip_count;
    const acl_project_payload* transform_payloads;
    const acl_project_payload* scalar_payloads;
    acl_project_payload database_header;
    acl_project_payload bulk_medium;
    acl_project_payload bulk_low;
} acl_project_group_build_output;

typedef struct acl_project_group_payload_input
{
    uint32_t struct_size;
    uint32_t struct_version;
    uint32_t clip_count;
    const acl_project_payload_view* transform_payloads;
    const acl_project_payload_view* scalar_payloads;
    acl_project_payload_view database_header;
    acl_project_payload_view bulk_medium;
    acl_project_payload_view bulk_low;
} acl_project_group_payload_input;

typedef struct acl_project_decoder_bind_input
{
    uint32_t struct_size;
    uint32_t struct_version;
    void* group;
    uint32_t clip_index;
    uint32_t reserved;
} acl_project_decoder_bind_input;

extern "C"
{
    ACL_PROJECT_API acl_project_error acl_project_get_abi(
        acl_project_abi_info* info);

    ACL_PROJECT_API acl_project_error acl_project_group_build(
        const acl_project_group_build_input* input,
        void** build);

    ACL_PROJECT_API acl_project_error acl_project_group_build_get_output(
        void* build,
        acl_project_group_build_output* output);

    ACL_PROJECT_API acl_project_error acl_project_group_build_release(
        void* build);

    ACL_PROJECT_API acl_project_error acl_project_group_create(
        const acl_project_group_payload_input* input,
        void** group);

    ACL_PROJECT_API acl_project_error acl_project_group_get_clip_count(
        void* group,
        uint32_t* clip_count);

    ACL_PROJECT_API acl_project_error acl_project_group_release(
        void* group);

    ACL_PROJECT_API acl_project_error acl_project_decoder_create(
        void** decoder);

    ACL_PROJECT_API acl_project_error acl_project_decoder_bind(
        void* decoder,
        void* group,
        const acl_project_decoder_bind_input* input);

    ACL_PROJECT_API acl_project_error acl_project_decoder_unbind(
        void* decoder);

    ACL_PROJECT_API acl_project_error acl_project_decoder_sample_transform(
        void* decoder,
        float sample_time,
        acl_project_transform_sample* output,
        int32_t output_count);

    ACL_PROJECT_API acl_project_error acl_project_decoder_sample_scalar(
        void* decoder,
        float sample_time,
        float* output,
        int32_t output_count);

    ACL_PROJECT_API acl_project_error acl_project_decoder_destroy(
        void* decoder);

}
