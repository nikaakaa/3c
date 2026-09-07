#include "acl_native_internal.h"

#include "acl/core/track_desc.h"
#include "acl/core/track_types.h"
#include "rtm/quatf.h"
#include "rtm/vector4f.h"

#include <cmath>
#include <new>

namespace acl_project_internal
{
    namespace
    {
        struct TransformWriter final : acl::track_writer
        {
            const acl::compressed_tracks* tracks = nullptr;
            acl_project_transform_sample* output = nullptr;

            static constexpr acl::default_sub_track_mode get_default_rotation_mode()
            {
                return acl::default_sub_track_mode::variable;
            }

            static constexpr acl::default_sub_track_mode get_default_translation_mode()
            {
                return acl::default_sub_track_mode::variable;
            }

            static constexpr acl::default_sub_track_mode get_default_scale_mode()
            {
                return acl::default_sub_track_mode::variable;
            }

            rtm::quatf RTM_SIMD_CALL get_variable_default_rotation(uint32_t index) const
            {
                acl::track_desc_transformf description;
                tracks->get_track_description(index, description);
                return description.default_value.rotation;
            }

            rtm::vector4f RTM_SIMD_CALL get_variable_default_translation(uint32_t index) const
            {
                acl::track_desc_transformf description;
                tracks->get_track_description(index, description);
                return description.default_value.translation;
            }

            rtm::vector4f RTM_SIMD_CALL get_variable_default_scale(uint32_t index) const
            {
                acl::track_desc_transformf description;
                tracks->get_track_description(index, description);
                return description.default_value.scale;
            }

            void RTM_SIMD_CALL write_rotation(uint32_t index, rtm::quatf_arg0 value)
            {
                rtm::quat_store(value, &output[index].rotation_x);
            }

            void RTM_SIMD_CALL write_translation(uint32_t index, rtm::vector4f_arg0 value)
            {
                rtm::vector_store3(value, &output[index].position_x);
            }

            void RTM_SIMD_CALL write_scale(uint32_t index, rtm::vector4f_arg0 value)
            {
                rtm::vector_store3(value, &output[index].scale_x);
            }
        };

        struct ScalarWriter final : acl::track_writer
        {
            float* output = nullptr;

            void RTM_SIMD_CALL write_float1(uint32_t index, rtm::scalarf_arg0 value)
            {
                output[index] = rtm::scalar_cast(value);
            }
        };
    }

    acl_project_error ProjectDecoder::unbind()
    {
        ProjectGroup* group = m_group;
        if (group != nullptr)
        {
            acl_project_error release_error = group->release_decoder();
            if (release_error != ACL_PROJECT_SUCCESS)
                return release_error;
        }
        m_scalar_context.reset();
        m_transform_context.reset();
        m_clip_index = 0;
        m_bound = false;
        m_group = nullptr;
        return ACL_PROJECT_SUCCESS;
    }

    ProjectDecoder::~ProjectDecoder()
    {
        unbind();
    }

    acl_project_error ProjectDecoder::bind(
        const acl_project_decoder_bind_input& input)
    {
        if (!IsStructValid(
                input.struct_size,
                input.struct_version,
                sizeof(acl_project_decoder_bind_input)))
            return ACL_PROJECT_INVALID_STRUCT;
        ProjectGroup* group = static_cast<ProjectGroup*>(input.group);
        if (group == nullptr || input.clip_index >= group->clip_count())
            return ACL_PROJECT_CLIP_INDEX;
        acl_project_error unbind_error = unbind();
        if (unbind_error != ACL_PROJECT_SUCCESS)
            return unbind_error;
        const acl::compressed_tracks* transform = group->transform(input.clip_index);
        if (transform == nullptr)
            return ACL_PROJECT_INVALID_PAYLOAD;
        bool initialized = group->has_database()
            ? m_transform_context.initialize(*transform, *group->database())
            : m_transform_context.initialize(*transform);
        if (!initialized)
            return ACL_PROJECT_INVALID_PAYLOAD;
        const acl::compressed_tracks* scalar = group->scalar(input.clip_index);
        if (scalar != nullptr && !m_scalar_context.initialize(*scalar))
        {
            m_transform_context.reset();
            return ACL_PROJECT_INVALID_PAYLOAD;
        }
        acl_project_error retain_error = group->retain_decoder();
        if (retain_error != ACL_PROJECT_SUCCESS)
        {
            m_scalar_context.reset();
            m_transform_context.reset();
            return retain_error;
        }
        m_clip_index = input.clip_index;
        m_group = group;
        m_bound = true;
        return ACL_PROJECT_SUCCESS;
    }

    acl_project_error ProjectDecoder::sample_transform(
        float sample_time,
        acl_project_transform_sample* output,
        int32_t output_count)
    {
        if (!m_bound || output == nullptr || output_count <= 0 ||
            !std::isfinite(sample_time))
            return ACL_PROJECT_INVALID_ARGUMENT;
        const acl::compressed_tracks* tracks = m_group->transform(m_clip_index);
        if (tracks == nullptr || static_cast<uint32_t>(output_count) != tracks->get_num_tracks())
            return ACL_PROJECT_INVALID_ARGUMENT;
        try
        {
            m_transform_context.seek(sample_time, acl::sample_rounding_policy::none);
            TransformWriter writer;
            writer.tracks = tracks;
            writer.output = output;
            m_transform_context.decompress_tracks(writer);
            return ACL_PROJECT_SUCCESS;
        }
        catch (...)
        {
            return ACL_PROJECT_NATIVE_FAILURE;
        }
    }

    acl_project_error ProjectDecoder::sample_scalar(
        float sample_time,
        float* output,
        int32_t output_count)
    {
        if (!m_bound || output_count < 0 || !std::isfinite(sample_time))
            return ACL_PROJECT_INVALID_ARGUMENT;
        const acl::compressed_tracks* tracks = m_group->scalar(m_clip_index);
        if (tracks == nullptr)
            return output_count == 0 ? ACL_PROJECT_SUCCESS : ACL_PROJECT_UNSUPPORTED_TRACK_TYPE;
        if (output == nullptr || output_count <= 0 ||
            static_cast<uint32_t>(output_count) != tracks->get_num_tracks())
            return ACL_PROJECT_INVALID_ARGUMENT;
        try
        {
            m_scalar_context.seek(sample_time, acl::sample_rounding_policy::none);
            ScalarWriter writer;
            writer.output = output;
            m_scalar_context.decompress_tracks(writer);
            return ACL_PROJECT_SUCCESS;
        }
        catch (...)
        {
            return ACL_PROJECT_NATIVE_FAILURE;
        }
    }
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_decoder_create(
    void** decoder)
{
    if (decoder == nullptr)
        return ACL_PROJECT_INVALID_ARGUMENT;
    *decoder = nullptr;
    try
    {
        *decoder = new (std::nothrow) acl_project_internal::ProjectDecoder();
        return *decoder == nullptr
            ? ACL_PROJECT_CAPACITY
            : ACL_PROJECT_SUCCESS;
    }
    catch (...)
    {
        return ACL_PROJECT_NATIVE_FAILURE;
    }
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_decoder_bind(
    void* decoder,
    void* group,
    const acl_project_decoder_bind_input* input)
{
    if (decoder == nullptr || input == nullptr)
        return ACL_PROJECT_INVALID_ARGUMENT;
    try
    {
        acl_project_decoder_bind_input bind = *input;
        bind.group = group;
        return static_cast<acl_project_internal::ProjectDecoder*>(decoder)->bind(bind);
    }
    catch (...)
    {
        return ACL_PROJECT_NATIVE_FAILURE;
    }
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_decoder_unbind(
    void* decoder)
{
    if (decoder == nullptr)
        return ACL_PROJECT_INVALID_ARGUMENT;
    try
    {
        return static_cast<acl_project_internal::ProjectDecoder*>(decoder)->unbind();
    }
    catch (...)
    {
        return ACL_PROJECT_NATIVE_FAILURE;
    }
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_decoder_sample_transform(
    void* decoder,
    float sample_time,
    acl_project_transform_sample* output,
    int32_t output_count)
{
    if (decoder == nullptr)
        return ACL_PROJECT_INVALID_ARGUMENT;
    return static_cast<acl_project_internal::ProjectDecoder*>(decoder)->sample_transform(
        sample_time,
        output,
        output_count);
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_decoder_sample_scalar(
    void* decoder,
    float sample_time,
    float* output,
    int32_t output_count)
{
    if (decoder == nullptr)
        return ACL_PROJECT_INVALID_ARGUMENT;
    return static_cast<acl_project_internal::ProjectDecoder*>(decoder)->sample_scalar(
        sample_time,
        output,
        output_count);
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_decoder_destroy(
    void* decoder)
{
    delete static_cast<acl_project_internal::ProjectDecoder*>(decoder);
    return ACL_PROJECT_SUCCESS;
}
