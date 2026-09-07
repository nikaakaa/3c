#include "acl_native_internal.h"

#include "acl/compression/compression_settings.h"
#include "acl/compression/track.h"
#include "acl/compression/track_array.h"
#include "acl/core/impl/compressed_headers.h"
#include "acl/core/track_desc.h"
#include "acl/core/track_types.h"
#include "rtm/qvvf.h"
#include "rtm/quatf.h"
#include "rtm/vector4f.h"

#include <cmath>
#include <cstring>
#include <new>

#if defined(_WIN32)
#include <malloc.h>
#endif

namespace acl_project_internal
{
    void* ProjectAllocator::allocate(size_t size, size_t alignment)
    {
#if defined(_WIN32)
        return _aligned_malloc(size, alignment);
#else
        void* result = nullptr;
        return posix_memalign(&result, alignment, size) == 0 ? result : nullptr;
#endif
    }

    void ProjectAllocator::deallocate(void* ptr, size_t)
    {
        if (ptr == nullptr)
            return;
#if defined(_WIN32)
        _aligned_free(ptr);
#else
        free(ptr);
#endif
    }

    OwnedBuffer::~OwnedBuffer()
    {
        reset();
    }

    OwnedBuffer::OwnedBuffer(OwnedBuffer&& other) noexcept
        : m_allocator(other.m_allocator)
        , m_data(other.m_data)
        , m_size(other.m_size)
    {
        other.m_allocator = nullptr;
        other.m_data = nullptr;
        other.m_size = 0;
    }

    OwnedBuffer& OwnedBuffer::operator=(OwnedBuffer&& other) noexcept
    {
        if (this == &other)
            return *this;
        reset();
        m_allocator = other.m_allocator;
        m_data = other.m_data;
        m_size = other.m_size;
        other.m_allocator = nullptr;
        other.m_data = nullptr;
        other.m_size = 0;
        return *this;
    }

    acl_project_error OwnedBuffer::copy_from(
        ProjectAllocator& allocator,
        const void* source,
        uint32_t size)
    {
        reset();
        if (size == 0)
            return ACL_PROJECT_SUCCESS;
        if (source == nullptr)
            return ACL_PROJECT_INVALID_ARGUMENT;
        m_data = static_cast<uint8_t*>(allocator.allocate(size, 16));
        if (m_data == nullptr)
            return ACL_PROJECT_CAPACITY;
        std::memcpy(m_data, source, size);
        m_allocator = &allocator;
        m_size = size;
        return ACL_PROJECT_SUCCESS;
    }

    void OwnedBuffer::reset()
    {
        if (m_allocator != nullptr && m_data != nullptr)
            m_allocator->deallocate(m_data, m_size);
        m_allocator = nullptr;
        m_data = nullptr;
        m_size = 0;
    }

    bool IsStructValid(uint32_t actual_size, uint32_t version, size_t expected_size)
    {
        return actual_size == expected_size && version == ACL_PROJECT_STRUCT_VERSION;
    }

    bool IsPayloadViewValid(const acl_project_payload_view& view, bool required)
    {
        if (!IsStructValid(view.struct_size, view.struct_version, sizeof(acl_project_payload_view)))
            return false;
        if (view.length == 0)
            return !required && view.data == nullptr;
        return view.data != nullptr;
    }

    void ReleaseCompressedTracks(ProjectAllocator& allocator, acl::compressed_tracks* tracks)
    {
        if (tracks != nullptr)
            allocator.deallocate(tracks, tracks->get_size());
    }

    acl_project_error CopyCompressedTracks(
        ProjectAllocator& allocator,
        acl::compressed_tracks* tracks,
        OwnedBuffer& destination)
    {
        if (tracks == nullptr)
            return ACL_PROJECT_INVALID_PAYLOAD;
        uint32_t size = tracks->get_size();
        acl_project_error result = destination.copy_from(allocator, tracks, size);
        ReleaseCompressedTracks(allocator, tracks);
        return result;
    }

    namespace
    {
        bool IsFiniteRange(const float* values, size_t count)
        {
            if (values == nullptr)
                return false;
            for (size_t i = 0; i < count; i++)
            {
                if (!std::isfinite(values[i]))
                    return false;
            }
            return true;
        }

        bool IsTransformInputValid(
            const acl_project_transform_input& input,
            bool enableDatabaseSupport)
        {
            if (!IsStructValid(input.struct_size, input.struct_version, sizeof(acl_project_transform_input)) ||
                input.track_count == 0 || input.sample_count < 2 ||
                !std::isfinite(input.sample_rate) || input.sample_rate <= 0.0F ||
                !std::isfinite(input.precision) || input.precision < 0.0F ||
                !std::isfinite(input.shell_distance) || input.shell_distance <= 0.0F ||
                input.samples == nullptr || input.default_values == nullptr ||
                input.parent_indices == nullptr || input.looping > 1)
                return false;
            size_t track_count = static_cast<size_t>(input.track_count);
            size_t sample_count = static_cast<size_t>(input.sample_count);
            if (sample_count > SIZE_MAX / track_count ||
                sample_count * track_count > SIZE_MAX / 10)
                return false;
            if (!IsFiniteRange(input.samples, sample_count * track_count * 10) ||
                !IsFiniteRange(input.default_values, track_count * 10))
                return false;
            for (uint32_t i = 0; i < input.track_count; i++)
            {
                int32_t parent = input.parent_indices[i];
                if (parent < -1 || parent >= static_cast<int32_t>(i))
                    return false;
            }
            return true;
        }

        bool IsScalarInputValid(const acl_project_scalar_input& input)
        {
            if (!IsStructValid(input.struct_size, input.struct_version, sizeof(acl_project_scalar_input)) ||
                input.track_count == 0 || input.sample_count < 2 ||
                !std::isfinite(input.sample_rate) || input.sample_rate <= 0.0F ||
                !std::isfinite(input.precision) || input.precision < 0.0F ||
                input.samples == nullptr || input.looping > 1)
                return false;
            size_t track_count = static_cast<size_t>(input.track_count);
            size_t sample_count = static_cast<size_t>(input.sample_count);
            if (sample_count > SIZE_MAX / track_count)
                return false;
            return IsFiniteRange(input.samples, sample_count * track_count);
        }

        bool IsDatabaseSettingsValid(
            const acl_project_database_settings& settings)
        {
            return IsStructValid(
                       settings.struct_size,
                       settings.struct_version,
                       sizeof(acl_project_database_settings)) &&
                std::isfinite(settings.medium_importance_tier_proportion) &&
                std::isfinite(settings.low_importance_tier_proportion) &&
                settings.medium_importance_tier_proportion >= 0.0F &&
                settings.low_importance_tier_proportion >= 0.0F &&
                settings.medium_importance_tier_proportion <= 1.0F &&
                settings.low_importance_tier_proportion <= 1.0F &&
                settings.medium_importance_tier_proportion +
                    settings.low_importance_tier_proportion <= 1.0F &&
                settings.max_chunk_size >= 4096;
        }

        bool IsGroupBuildInputValid(
            const acl_project_group_build_input& input)
        {
            return IsStructValid(
                       input.struct_size,
                       input.struct_version,
                       sizeof(acl_project_group_build_input)) &&
                input.clip_count > 0 &&
                input.transform_inputs != nullptr &&
                input.enable_database <= 1 &&
                IsDatabaseSettingsValid(input.database_settings);
        }

        void ReleaseTrackVector(
            ProjectAllocator& allocator,
            std::vector<acl::compressed_tracks*>& tracks)
        {
            for (acl::compressed_tracks* track : tracks)
                ReleaseCompressedTracks(allocator, track);
            tracks.clear();
        }
    }

    acl_project_error CompressTransformInput(
        const acl_project_transform_input& input,
        bool enableDatabaseSupport,
        ProjectAllocator& allocator,
        acl::compressed_tracks*& output)
    {
        output = nullptr;
        if (!IsTransformInputValid(input, enableDatabaseSupport))
            return ACL_PROJECT_INVALID_ARGUMENT;
        try
        {
            acl::track_array_qvvf tracks(allocator, input.track_count);
            for (uint32_t track_index = 0; track_index < input.track_count; track_index++)
            {
                acl::track_desc_transformf description;
                description.output_index = track_index;
                description.parent_index = input.parent_indices[track_index] < 0
                    ? acl::k_invalid_track_index
                    : static_cast<uint32_t>(input.parent_indices[track_index]);
                description.precision = input.precision;
                description.shell_distance = input.shell_distance;
                const float* default_value = input.default_values +
                    static_cast<size_t>(track_index) * 10;
                description.default_value = rtm::qvv_set(
                    rtm::quat_load(default_value + 3),
                    rtm::vector_load3(default_value),
                    rtm::vector_load3(default_value + 7));
                acl::track_qvvf track = acl::track_qvvf::make_reserve(
                    description,
                    allocator,
                    input.sample_count,
                    input.sample_rate);
                for (uint32_t sample_index = 0; sample_index < input.sample_count; sample_index++)
                {
                    const float* sample = input.samples +
                        (static_cast<size_t>(sample_index) * input.track_count + track_index) * 10;
                    track[sample_index] = rtm::qvv_set(
                        rtm::quat_load(sample + 3),
                        rtm::vector_load3(sample),
                        rtm::vector_load3(sample + 7));
                }
                tracks[track_index] = std::move(track);
            }
            tracks.set_looping_policy(input.looping != 0
                ? acl::sample_looping_policy::wrap
                : acl::sample_looping_policy::clamp);
            acl::compression_settings settings = acl::get_default_compression_settings();
            acl::qvvf_transform_error_metric metric;
            settings.error_metric = &metric;
            settings.enable_database_support = enableDatabaseSupport;
            settings.keyframe_stripping = acl::compression_keyframe_stripping_settings();
            settings.optimize_loops = false;
            settings.metadata.include_track_descriptions = true;
            settings.metadata.include_contributing_error = enableDatabaseSupport;
            acl::compressed_tracks* result = nullptr;
            acl::output_stats stats;
            acl::error_result error = acl::compress_track_list(
                allocator,
                tracks,
                settings,
                result,
                stats);
            if (!error.empty() || result == nullptr || !result->is_valid(true).empty())
            {
                ReleaseCompressedTracks(allocator, result);
                return ACL_PROJECT_NATIVE_FAILURE;
            }
            output = result;
            return ACL_PROJECT_SUCCESS;
        }
        catch (...)
        {
            ReleaseCompressedTracks(allocator, output);
            output = nullptr;
            return ACL_PROJECT_NATIVE_FAILURE;
        }
    }

    acl_project_error CompressScalarInput(
        const acl_project_scalar_input& input,
        ProjectAllocator& allocator,
        acl::compressed_tracks*& output)
    {
        output = nullptr;
        if (!IsScalarInputValid(input))
            return ACL_PROJECT_INVALID_ARGUMENT;
        try
        {
            acl::track_array tracks(allocator, input.track_count);
            for (uint32_t track_index = 0; track_index < input.track_count; track_index++)
            {
                acl::track_desc_scalarf description;
                description.output_index = track_index;
                description.precision = input.precision;
                acl::track_float1f track = acl::track_float1f::make_reserve(
                    description,
                    allocator,
                    input.sample_count,
                    input.sample_rate);
                for (uint32_t sample_index = 0; sample_index < input.sample_count; sample_index++)
                    track[sample_index] = input.samples[
                        static_cast<size_t>(sample_index) * input.track_count + track_index];
                tracks[track_index] = std::move(track);
            }
            tracks.set_looping_policy(input.looping != 0
                ? acl::sample_looping_policy::wrap
                : acl::sample_looping_policy::clamp);
            acl::compression_settings settings = acl::get_default_compression_settings();
            settings.metadata.include_track_descriptions = true;
            acl::compressed_tracks* result = nullptr;
            acl::output_stats stats;
            acl::error_result error = acl::compress_track_list(
                allocator,
                tracks,
                settings,
                result,
                stats);
            if (!error.empty() || result == nullptr || !result->is_valid(true).empty() ||
                result->get_track_type() == acl::track_type8::qvvf)
            {
                ReleaseCompressedTracks(allocator, result);
                return ACL_PROJECT_NATIVE_FAILURE;
            }
            output = result;
            return ACL_PROJECT_SUCCESS;
        }
        catch (...)
        {
            ReleaseCompressedTracks(allocator, output);
            output = nullptr;
            return ACL_PROJECT_NATIVE_FAILURE;
        }
    }

    void ProjectGroupBuild::reset_output()
    {
        m_clip_count = 0;
        m_transform_payloads.clear();
        m_scalar_payloads.clear();
        m_database_header.reset();
        m_bulk_medium.reset();
        m_bulk_low.reset();
        m_transform_views.clear();
        m_scalar_views.clear();
    }

    void ProjectGroupBuild::release_tracks(
        std::vector<acl::compressed_tracks*>& tracks)
    {
        ReleaseTrackVector(m_allocator, tracks);
    }

    void ProjectGroupBuild::rebuild_output_views()
    {
        m_transform_views.resize(m_transform_payloads.size());
        m_scalar_views.resize(m_scalar_payloads.size());
        for (size_t i = 0; i < m_transform_payloads.size(); i++)
        {
            m_transform_views[i].data = m_transform_payloads[i].data();
            m_transform_views[i].length = m_transform_payloads[i].size();
        }
        for (size_t i = 0; i < m_scalar_payloads.size(); i++)
        {
            m_scalar_views[i].data = m_scalar_payloads[i].data();
            m_scalar_views[i].length = m_scalar_payloads[i].size();
        }
    }

    acl_project_error ProjectGroupBuild::initialize(
        const acl_project_group_build_input& input)
    {
        reset_output();
        if (!IsGroupBuildInputValid(input))
            return ACL_PROJECT_INVALID_ARGUMENT;
        m_clip_count = input.clip_count;
        std::vector<acl::compressed_tracks*> raw_transform(m_clip_count, nullptr);
        std::vector<acl::compressed_tracks*> raw_scalar(m_clip_count, nullptr);
        try
        {
            for (uint32_t i = 0; i < m_clip_count; i++)
            {
                acl_project_error error = CompressTransformInput(
                    input.transform_inputs[i],
                    input.enable_database != 0,
                    m_allocator,
                    raw_transform[i]);
                if (error != ACL_PROJECT_SUCCESS)
                {
                    release_tracks(raw_transform);
                    release_tracks(raw_scalar);
                    return error;
                }
                if (input.scalar_inputs != nullptr && input.scalar_inputs[i].track_count > 0)
                {
                    error = CompressScalarInput(
                        input.scalar_inputs[i],
                        m_allocator,
                        raw_scalar[i]);
                    if (error != ACL_PROJECT_SUCCESS)
                    {
                        release_tracks(raw_transform);
                        release_tracks(raw_scalar);
                        return error;
                    }
                }
            }

            if (input.enable_database != 0)
            {
                std::vector<const acl::compressed_tracks*> database_inputs(m_clip_count);
                std::vector<acl::compressed_tracks*> database_outputs(m_clip_count, nullptr);
                for (uint32_t i = 0; i < m_clip_count; i++)
                    database_inputs[i] = raw_transform[i];
                acl::compression_database_settings database_settings;
                database_settings.medium_importance_tier_proportion =
                    input.database_settings.medium_importance_tier_proportion;
                database_settings.low_importance_tier_proportion =
                    input.database_settings.low_importance_tier_proportion;
                database_settings.max_chunk_size = input.database_settings.max_chunk_size;
                acl::compressed_database* database = nullptr;
                acl::error_result error = acl::build_database(
                    m_allocator,
                    database_settings,
                    database_inputs.data(),
                    m_clip_count,
                    database_outputs.data(),
                    database);
                release_tracks(raw_transform);
                if (!error.empty() || database == nullptr)
                {
                    release_tracks(database_outputs);
                    release_tracks(raw_scalar);
                    if (database != nullptr)
                        m_allocator.deallocate(database, database->get_size());
                    return ACL_PROJECT_DATABASE_FAILURE;
                }
                m_transform_payloads.resize(m_clip_count);
                for (uint32_t i = 0; i < m_clip_count; i++)
                {
                    acl_project_error copy_error = CopyCompressedTracks(
                        m_allocator,
                        database_outputs[i],
                        m_transform_payloads[i]);
                    database_outputs[i] = nullptr;
                    if (copy_error != ACL_PROJECT_SUCCESS)
                    {
                        release_tracks(database_outputs);
                        release_tracks(raw_scalar);
                        m_allocator.deallocate(database, database->get_size());
                        return copy_error;
                    }
                }
                release_tracks(database_outputs);
                acl::compressed_database* split_database = nullptr;
                uint8_t* bulk_medium = nullptr;
                uint8_t* bulk_low = nullptr;
                error = acl::split_database_bulk_data(
                    m_allocator,
                    *database,
                    split_database,
                    bulk_medium,
                    bulk_low);
                uint32_t medium_size = database->get_bulk_data_size(
                    acl::quality_tier::medium_importance);
                uint32_t low_size = database->get_bulk_data_size(
                    acl::quality_tier::lowest_importance);
                m_allocator.deallocate(database, database->get_size());
                if (!error.empty() || split_database == nullptr)
                {
                    if (split_database != nullptr)
                        m_allocator.deallocate(split_database, split_database->get_size());
                    if (bulk_medium != nullptr)
                        m_allocator.deallocate(bulk_medium, medium_size);
                    if (bulk_low != nullptr)
                        m_allocator.deallocate(bulk_low, low_size);
                    release_tracks(raw_scalar);
                    return ACL_PROJECT_DATABASE_FAILURE;
                }
                acl_project_error copy_error = m_database_header.copy_from(
                    m_allocator,
                    split_database,
                    split_database->get_size());
                if (copy_error == ACL_PROJECT_SUCCESS)
                    copy_error = m_bulk_medium.copy_from(
                        m_allocator,
                        bulk_medium,
                        medium_size);
                if (copy_error == ACL_PROJECT_SUCCESS)
                    copy_error = m_bulk_low.copy_from(
                        m_allocator,
                        bulk_low,
                        low_size);
                m_allocator.deallocate(split_database, split_database->get_size());
                if (bulk_medium != nullptr)
                    m_allocator.deallocate(bulk_medium, medium_size);
                if (bulk_low != nullptr)
                    m_allocator.deallocate(bulk_low, low_size);
                if (copy_error != ACL_PROJECT_SUCCESS)
                {
                    release_tracks(raw_scalar);
                    return copy_error;
                }
            }
            else
            {
                m_transform_payloads.resize(m_clip_count);
                for (uint32_t i = 0; i < m_clip_count; i++)
                {
                    acl_project_error error = CopyCompressedTracks(
                        m_allocator,
                        raw_transform[i],
                        m_transform_payloads[i]);
                    raw_transform[i] = nullptr;
                    if (error != ACL_PROJECT_SUCCESS)
                    {
                        release_tracks(raw_transform);
                        release_tracks(raw_scalar);
                        return error;
                    }
                }
                release_tracks(raw_transform);
            }

            m_scalar_payloads.resize(m_clip_count);
            for (uint32_t i = 0; i < m_clip_count; i++)
            {
                if (raw_scalar[i] == nullptr)
                    continue;
                acl_project_error error = CopyCompressedTracks(
                    m_allocator,
                    raw_scalar[i],
                    m_scalar_payloads[i]);
                raw_scalar[i] = nullptr;
                if (error != ACL_PROJECT_SUCCESS)
                {
                    release_tracks(raw_scalar);
                    return error;
                }
            }
            release_tracks(raw_scalar);
            rebuild_output_views();
            return ACL_PROJECT_SUCCESS;
        }
        catch (...)
        {
            release_tracks(raw_transform);
            release_tracks(raw_scalar);
            reset_output();
            return ACL_PROJECT_NATIVE_FAILURE;
        }
    }

    acl_project_error ProjectGroupBuild::get_output(
        acl_project_group_build_output& output) const
    {
        if (!IsStructValid(
                output.struct_size,
                output.struct_version,
                sizeof(acl_project_group_build_output)))
            return ACL_PROJECT_INVALID_STRUCT;
        if (m_clip_count == 0 || m_transform_views.size() != m_clip_count)
            return ACL_PROJECT_INVALID_STATE;
        output.clip_count = m_clip_count;
        output.transform_payloads = m_transform_views.data();
        output.scalar_payloads = m_scalar_views.data();
        output.database_header.data = m_database_header.data();
        output.database_header.length = m_database_header.size();
        output.bulk_medium.data = m_bulk_medium.data();
        output.bulk_medium.length = m_bulk_medium.size();
        output.bulk_low.data = m_bulk_low.data();
        output.bulk_low.length = m_bulk_low.size();
        return ACL_PROJECT_SUCCESS;
    }
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_group_build(
    const acl_project_group_build_input* input,
    void** build)
{
    if (input == nullptr || build == nullptr)
        return ACL_PROJECT_INVALID_ARGUMENT;
    *build = nullptr;
    acl_project_internal::ProjectGroupBuild* result =
        new (std::nothrow) acl_project_internal::ProjectGroupBuild();
    if (result == nullptr)
        return ACL_PROJECT_CAPACITY;
    acl_project_error error;
    try
    {
        error = result->initialize(*input);
    }
    catch (...)
    {
        delete result;
        return ACL_PROJECT_NATIVE_FAILURE;
    }
    if (error != ACL_PROJECT_SUCCESS)
    {
        delete result;
        return error;
    }
    *build = result;
    return ACL_PROJECT_SUCCESS;
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_group_build_get_output(
    void* build,
    acl_project_group_build_output* output)
{
    if (build == nullptr || output == nullptr)
        return ACL_PROJECT_INVALID_ARGUMENT;
    try
    {
        return static_cast<acl_project_internal::ProjectGroupBuild*>(build)->get_output(*output);
    }
    catch (...)
    {
        return ACL_PROJECT_NATIVE_FAILURE;
    }
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_group_build_release(
    void* build)
{
    delete static_cast<acl_project_internal::ProjectGroupBuild*>(build);
    return ACL_PROJECT_SUCCESS;
}
