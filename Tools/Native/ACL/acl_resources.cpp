#include "acl_native_internal.h"

#include "acl/compression/compression_settings.h"
#include "acl/core/error_result.h"
#include "acl/core/impl/compressed_headers.h"
#include "acl/core/quality_tiers.h"
#include "rtm/quatf.h"

#include <cmath>
#include <cstring>
#include <new>

namespace acl_project_internal
{
    ProjectDatabaseStreamer::ProjectDatabaseStreamer(
        const uint8_t* data,
        uint32_t size)
        : acl::database_streamer(m_requests, 2)
        , m_data(data)
        , m_size(size)
    {
    }

    bool ProjectDatabaseStreamer::is_initialized() const
    {
        return m_size == 0 || m_data != nullptr;
    }

    const uint8_t* ProjectDatabaseStreamer::get_bulk_data(acl::quality_tier tier) const
    {
        if (tier == acl::quality_tier::highest_importance)
            return nullptr;
        return m_data;
    }

    void ProjectDatabaseStreamer::stream_in(
        uint32_t offset,
        uint32_t size,
        bool,
        acl::quality_tier,
        acl::streaming_request_id request_id)
    {
        if (offset > m_size || size > m_size - offset)
        {
            cancel(request_id);
            return;
        }
        complete(request_id);
    }

    void ProjectDatabaseStreamer::stream_out(
        uint32_t offset,
        uint32_t size,
        bool,
        acl::quality_tier,
        acl::streaming_request_id request_id)
    {
        if (offset > m_size || size > m_size - offset)
        {
            cancel(request_id);
            return;
        }
        complete(request_id);
    }

    namespace
    {
        template <typename T>
        bool ReadValue(
            const uint8_t* data,
            uint32_t length,
            size_t offset,
            T& value)
        {
            if (data == nullptr || offset > length || sizeof(T) > length - offset)
                return false;
            std::memcpy(&value, data + offset, sizeof(T));
            return true;
        }

        bool ReadBytes(
            const uint8_t* data,
            uint32_t length,
            size_t offset,
            void* destination,
            size_t size)
        {
            if (data == nullptr || destination == nullptr || offset > length || size > length - offset)
                return false;
            std::memcpy(destination, data + offset, size);
            return true;
        }

        bool IsRange(
            uint32_t length,
            size_t base,
            uint32_t offset,
            size_t size)
        {
            if (offset == UINT32_MAX || base > length || offset > length - base)
                return false;
            size_t start = base + offset;
            return size <= length - start;
        }

        bool IsOptionalRange(
            uint32_t length,
            size_t base,
            uint32_t offset,
            size_t size)
        {
            return offset == UINT32_MAX || IsRange(length, base, offset, size);
        }

        bool ValidateTrackMetadata(
            const uint8_t* data,
            uint32_t length,
            const acl::acl_impl::tracks_header& header,
            bool transform)
        {
            if ((header.misc_packed & (1u << 31)) == 0 ||
                length < sizeof(acl::acl_impl::raw_buffer_header) +
                    sizeof(acl::acl_impl::tracks_header) +
                    sizeof(acl::acl_impl::optional_metadata_header))
                return false;
            size_t metadataOffset = length - sizeof(acl::acl_impl::optional_metadata_header);
            acl::acl_impl::optional_metadata_header metadata;
            if (!ReadValue(data, length, metadataOffset, metadata))
                return false;
            size_t trackCount = header.num_tracks;
            size_t descriptionSize = transform ? sizeof(float) * 12 : sizeof(float);
            if (!IsRange(length, 0, static_cast<uint32_t>(metadata.parent_track_indices),
                    transform ? trackCount * sizeof(uint32_t) : 0) && transform)
                return false;
            if (!IsRange(length, 0, static_cast<uint32_t>(metadata.track_descriptions),
                    trackCount * descriptionSize))
                return false;
            if (!IsOptionalRange(length, 0, static_cast<uint32_t>(metadata.track_name_offsets),
                    trackCount * sizeof(uint32_t)) ||
                !IsOptionalRange(length, 0, static_cast<uint32_t>(metadata.track_list_name), 1) ||
                !IsOptionalRange(length, 0, static_cast<uint32_t>(metadata.contributing_error), 1))
                return false;
            return true;
        }

        bool ValidateCompressedTracksPayload(
            const uint8_t* data,
            uint32_t length,
            acl::track_type8 expectedType,
            bool requireDatabase,
            const acl::compressed_tracks*& tracks)
        {
            tracks = nullptr;
            const size_t rawSize = sizeof(acl::acl_impl::raw_buffer_header);
            const size_t tracksSize = sizeof(acl::acl_impl::tracks_header);
            if (data == nullptr || length < rawSize + tracksSize)
                return false;
            uint32_t declaredSize = 0;
            if (!ReadValue(data, length, 0, declaredSize) || declaredSize != length)
                return false;
            acl::acl_impl::tracks_header header;
            if (!ReadValue(data, length, rawSize, header) ||
                header.tag != static_cast<uint32_t>(acl::buffer_tag32::compressed_tracks) ||
                header.version != acl::compressed_tracks_version16::v02_01_00 ||
                header.track_type != expectedType ||
                header.num_tracks == 0 || header.num_samples < 2 ||
                !std::isfinite(header.sample_rate) || header.sample_rate <= 0.0F)
                return false;
            size_t typeOffset = rawSize + tracksSize;
            if (expectedType == acl::track_type8::float1f)
            {
                acl::acl_impl::scalar_tracks_header scalarHeader;
                    if (!ReadValue(data, length, typeOffset, scalarHeader) ||
                        !IsOptionalRange(length, typeOffset,
                            static_cast<uint32_t>(scalarHeader.metadata_per_track),
                            1) ||
                        !IsOptionalRange(length, typeOffset,
                            static_cast<uint32_t>(scalarHeader.track_constant_values),
                            1) ||
                        !IsOptionalRange(length, typeOffset,
                            static_cast<uint32_t>(scalarHeader.track_range_values),
                            1) ||
                        !IsOptionalRange(length, typeOffset,
                            static_cast<uint32_t>(scalarHeader.track_animated_values),
                            1))
                    return false;
            }
            else if (expectedType == acl::track_type8::qvvf)
            {
                alignas(acl::acl_impl::transform_tracks_header)
                    uint8_t transformHeaderBytes[sizeof(acl::acl_impl::transform_tracks_header)];
                if (!ReadBytes(data, length, typeOffset, transformHeaderBytes,
                        sizeof(transformHeaderBytes)))
                    return false;
                const acl::acl_impl::transform_tracks_header& transformHeader =
                    *reinterpret_cast<const acl::acl_impl::transform_tracks_header*>(transformHeaderBytes);
                if (
                    transformHeader.num_segments == 0 ||
                    transformHeader.num_segments > length / sizeof(acl::acl_impl::segment_header))
                    return false;
                bool stripped = (header.misc_packed & (1u << 10)) != 0;
                size_t segmentSize = stripped
                    ? sizeof(acl::acl_impl::stripped_segment_header_t)
                    : sizeof(acl::acl_impl::segment_header);
                uint32_t segmentOffset = stripped
                    ? static_cast<uint32_t>(transformHeader.stripped_segment_headers_offset)
                    : static_cast<uint32_t>(transformHeader.segment_headers_offset);
                if (!IsRange(length, typeOffset,
                        segmentOffset,
                        transformHeader.num_segments * segmentSize) ||
                    !IsRange(length, typeOffset,
                        static_cast<uint32_t>(transformHeader.sub_track_types_offset),
                        ((header.num_tracks + 15) / 16) * sizeof(acl::acl_impl::packed_sub_track_types)) ||
                    !IsOptionalRange(length, typeOffset,
                        static_cast<uint32_t>(transformHeader.constant_track_data_offset), 1) ||
                    !IsOptionalRange(length, typeOffset,
                        static_cast<uint32_t>(transformHeader.clip_range_data_offset), 1))
                    return false;
                bool hasDatabase = (header.misc_packed & (1u << 8)) != 0;
                if (hasDatabase != requireDatabase)
                    return false;
                if (hasDatabase)
                {
                    if (!IsRange(length, typeOffset,
                            static_cast<uint32_t>(transformHeader.database_header_offset),
                            sizeof(acl::acl_impl::tracks_database_header)))
                        return false;
                    acl::acl_impl::tracks_database_header databaseHeader;
                    size_t databaseOffset = typeOffset +
                        static_cast<uint32_t>(transformHeader.database_header_offset);
                    if (!ReadValue(data, length, databaseOffset, databaseHeader) ||
                        !databaseHeader.clip_header_offset.is_valid())
                        return false;
                }
            }
            else
            {
                return false;
            }
            if (!ValidateTrackMetadata(data, length, header, expectedType == acl::track_type8::qvvf))
                return false;
            acl::error_result error;
            tracks = acl::make_compressed_tracks(data, &error);
            if (tracks == nullptr || !error.empty() ||
                !tracks->is_valid(true).empty() ||
                tracks->get_track_type() != expectedType ||
                tracks->get_version() != acl::compressed_tracks_version16::v02_01_00)
            {
                tracks = nullptr;
                return false;
            }
            for (uint32_t i = 0; i < tracks->get_num_tracks(); i++)
            {
                if (expectedType == acl::track_type8::qvvf)
                {
                    acl::track_desc_transformf description;
                    if (!tracks->get_track_description(i, description) ||
                        description.output_index != i ||
                        (description.parent_index != acl::k_invalid_track_index &&
                            description.parent_index >= tracks->get_num_tracks()) ||
                        description.is_valid().any() ||
                        !std::isfinite(description.precision) || description.precision < 0.0F ||
                        !std::isfinite(description.shell_distance) || description.shell_distance <= 0.0F ||
                        !std::isfinite(rtm::vector_get_x(description.default_value.translation)) ||
                        !std::isfinite(rtm::vector_get_y(description.default_value.translation)) ||
                        !std::isfinite(rtm::vector_get_z(description.default_value.translation)) ||
                        !std::isfinite(rtm::quat_get_x(description.default_value.rotation)) ||
                        !std::isfinite(rtm::quat_get_y(description.default_value.rotation)) ||
                        !std::isfinite(rtm::quat_get_z(description.default_value.rotation)) ||
                        !std::isfinite(rtm::quat_get_w(description.default_value.rotation)) ||
                        static_cast<float>(rtm::quat_length_squared(description.default_value.rotation)) <= 0.0F ||
                        !std::isfinite(rtm::vector_get_x(description.default_value.scale)) ||
                        !std::isfinite(rtm::vector_get_y(description.default_value.scale)) ||
                        !std::isfinite(rtm::vector_get_z(description.default_value.scale)))
                    {
                        tracks = nullptr;
                        return false;
                    }
                }
                else
                {
                    acl::track_desc_scalarf description;
                    if (!tracks->get_track_description(i, description) ||
                        description.output_index != i ||
                        description.is_valid().any())
                    {
                        tracks = nullptr;
                        return false;
                    }
                }
            }
            return true;
        }

        bool ValidateCompressedDatabasePayload(
            const uint8_t* data,
            uint32_t length,
            const uint8_t* medium,
            uint32_t mediumLength,
            const uint8_t* low,
            uint32_t lowLength,
            uint32_t clipCount,
            const acl::compressed_database*& database)
        {
            database = nullptr;
            const size_t rawSize = sizeof(acl::acl_impl::raw_buffer_header);
            const size_t headerSize = sizeof(acl::acl_impl::database_header);
            if (data == nullptr || length < rawSize + headerSize)
                return false;
            uint32_t declaredSize = 0;
            if (!ReadValue(data, length, 0, declaredSize) || declaredSize != length)
                return false;
            acl::acl_impl::database_header header;
            if (!ReadValue(data, length, rawSize, header) ||
                header.tag != static_cast<uint32_t>(acl::buffer_tag32::compressed_database) ||
                header.version != acl::compressed_tracks_version16::v02_01_00 ||
                header.get_is_bulk_data_inline() || header.num_clips != clipCount ||
                header.max_chunk_size < 4096)
                return false;
            size_t descriptionBytes =
                (static_cast<size_t>(header.num_chunks[0]) + header.num_chunks[1]) *
                sizeof(acl::acl_impl::database_chunk_description);
            if (descriptionBytes / sizeof(acl::acl_impl::database_chunk_description) !=
                static_cast<size_t>(header.num_chunks[0]) + header.num_chunks[1] ||
                !IsRange(length, rawSize + headerSize, 0, descriptionBytes) ||
                !IsRange(length, rawSize,
                    static_cast<uint32_t>(header.clip_metadata_offset),
                    static_cast<size_t>(header.num_clips) * sizeof(acl::acl_impl::database_clip_metadata)) ||
                header.bulk_data_size[0] != mediumLength ||
                header.bulk_data_size[1] != lowLength)
                return false;
            for (uint32_t tier = 0; tier < 2; tier++)
            {
                const uint8_t* bulk = tier == 0 ? medium : low;
                uint32_t bulkLength = tier == 0 ? mediumLength : lowLength;
                uint32_t chunkCount = header.num_chunks[tier];
                for (uint32_t i = 0; i < chunkCount; i++)
                {
                    size_t offset = rawSize + headerSize +
                        static_cast<size_t>(i + (tier == 0 ? 0 : header.num_chunks[0])) *
                        sizeof(acl::acl_impl::database_chunk_description);
                    acl::acl_impl::database_chunk_description description;
                    if (!ReadValue(data, length, offset, description) ||
                        !IsRange(bulkLength, 0, static_cast<uint32_t>(description.offset), description.size) ||
                        description.size < sizeof(acl::acl_impl::database_chunk_header))
                        return false;
                    acl::acl_impl::database_chunk_header chunk;
                    if (!ReadValue(bulk, bulkLength,
                            static_cast<uint32_t>(description.offset), chunk) ||
                        chunk.size != description.size ||
                        chunk.num_segments == 0 ||
                        chunk.num_segments > (chunk.size - sizeof(chunk)) /
                            sizeof(acl::acl_impl::database_chunk_segment_header))
                        return false;
                }
            }
            acl::error_result error;
            database = acl::make_compressed_database(data, &error);
            if (database == nullptr || !error.empty() || !database->is_valid(true).empty() ||
                database->get_num_clips() != clipCount ||
                database->get_bulk_data_size(acl::quality_tier::medium_importance) != mediumLength ||
                database->get_bulk_data_size(acl::quality_tier::lowest_importance) != lowLength)
            {
                database = nullptr;
                return false;
            }
            return true;
        }

        bool IsGroupPayloadInputValid(const acl_project_group_payload_input& input)
        {
            return IsStructValid(
                       input.struct_size,
                       input.struct_version,
                       sizeof(acl_project_group_payload_input)) &&
                input.clip_count > 0 &&
                input.transform_payloads != nullptr &&
                IsPayloadViewValid(input.database_header, false) &&
                IsPayloadViewValid(input.bulk_medium, false) &&
                IsPayloadViewValid(input.bulk_low, false);
        }

    }

    const acl::compressed_tracks* ProjectGroup::transform(uint32_t index) const
    {
        return index < m_transform_tracks.size() ? m_transform_tracks[index] : nullptr;
    }

    const acl::compressed_tracks* ProjectGroup::scalar(uint32_t index) const
    {
        return index < m_scalar_tracks.size() ? m_scalar_tracks[index] : nullptr;
    }

    acl_project_error ProjectGroup::initialize(
        const acl_project_group_payload_input& input)
    {
        if (!IsGroupPayloadInputValid(input))
            return ACL_PROJECT_INVALID_ARGUMENT;
        m_clip_count = input.clip_count;
        try
        {
            m_transform_payloads.resize(m_clip_count);
            m_scalar_payloads.resize(m_clip_count);
            for (uint32_t i = 0; i < m_clip_count; i++)
            {
                if (!IsPayloadViewValid(input.transform_payloads[i], true))
                    return ACL_PROJECT_INVALID_ARGUMENT;
                acl_project_error error = m_transform_payloads[i].copy_from(
                    m_allocator,
                    input.transform_payloads[i].data,
                    input.transform_payloads[i].length);
                if (error != ACL_PROJECT_SUCCESS)
                    return error;
                if (input.scalar_payloads != nullptr)
                {
                    if (!IsPayloadViewValid(input.scalar_payloads[i], false))
                        return ACL_PROJECT_INVALID_ARGUMENT;
                    error = m_scalar_payloads[i].copy_from(
                        m_allocator,
                        input.scalar_payloads[i].data,
                        input.scalar_payloads[i].length);
                    if (error != ACL_PROJECT_SUCCESS)
                        return error;
                }
            }
            if (input.database_header.length > 0)
            {
                acl_project_error error = m_database_header.copy_from(
                    m_allocator,
                    input.database_header.data,
                    input.database_header.length);
                if (error != ACL_PROJECT_SUCCESS)
                    return error;
                error = m_bulk_medium.copy_from(
                    m_allocator,
                    input.bulk_medium.data,
                    input.bulk_medium.length);
                if (error != ACL_PROJECT_SUCCESS)
                    return error;
                error = m_bulk_low.copy_from(
                    m_allocator,
                    input.bulk_low.data,
                    input.bulk_low.length);
                if (error != ACL_PROJECT_SUCCESS)
                    return error;
                const acl::compressed_database* database = nullptr;
                if (!ValidateCompressedDatabasePayload(
                        m_database_header.data(),
                        m_database_header.size(),
                        m_bulk_medium.data(),
                        m_bulk_medium.size(),
                        m_bulk_low.data(),
                        m_bulk_low.size(),
                        m_clip_count,
                        database))
                    return ACL_PROJECT_INVALID_PAYLOAD;
                for (uint32_t i = 0; i < m_clip_count; i++)
                {
                    const acl::compressed_tracks* tracks = nullptr;
                    if (!ValidateCompressedTracksPayload(
                            m_transform_payloads[i].data(),
                            m_transform_payloads[i].size(),
                            acl::track_type8::qvvf,
                            true,
                            tracks) ||
                        !database->contains(*tracks))
                        return ACL_PROJECT_INVALID_PAYLOAD;
                    m_transform_tracks.push_back(tracks);
                }
                m_medium_streamer = std::unique_ptr<ProjectDatabaseStreamer>(
                    new ProjectDatabaseStreamer(
                        m_bulk_medium.data(),
                        m_bulk_medium.size()));
                m_low_streamer = std::unique_ptr<ProjectDatabaseStreamer>(
                    new ProjectDatabaseStreamer(
                        m_bulk_low.data(),
                        m_bulk_low.size()));
                m_database_context = std::unique_ptr<acl::database_context<acl::default_database_settings>>(
                    new acl::database_context<acl::default_database_settings>());
                if (!m_database_context->initialize(
                        m_allocator,
                        *database,
                        *m_medium_streamer,
                        *m_low_streamer))
                    return ACL_PROJECT_DATABASE_FAILURE;
                if (database->has_bulk_data(acl::quality_tier::medium_importance))
                {
                    acl::database_stream_request_result status =
                        m_database_context->stream_in(acl::quality_tier::medium_importance);
                    if (status != acl::database_stream_request_result::done &&
                        status != acl::database_stream_request_result::dispatched)
                        return ACL_PROJECT_DATABASE_FAILURE;
                }
                if (database->has_bulk_data(acl::quality_tier::lowest_importance))
                {
                    acl::database_stream_request_result status =
                        m_database_context->stream_in(acl::quality_tier::lowest_importance);
                    if (status != acl::database_stream_request_result::done &&
                        status != acl::database_stream_request_result::dispatched)
                        return ACL_PROJECT_DATABASE_FAILURE;
                }
            }
            else
            {
                if (input.bulk_medium.length != 0 || input.bulk_low.length != 0)
                    return ACL_PROJECT_INVALID_ARGUMENT;
                for (uint32_t i = 0; i < m_clip_count; i++)
                {
                    const acl::compressed_tracks* tracks = nullptr;
                    if (!ValidateCompressedTracksPayload(
                            m_transform_payloads[i].data(),
                            m_transform_payloads[i].size(),
                            acl::track_type8::qvvf,
                            false,
                            tracks))
                        return ACL_PROJECT_INVALID_PAYLOAD;
                    m_transform_tracks.push_back(tracks);
                }
            }
            for (uint32_t i = 0; i < m_clip_count; i++)
            {
                if (m_scalar_payloads[i].empty())
                {
                    m_scalar_tracks.push_back(nullptr);
                    continue;
                }
                const acl::compressed_tracks* tracks = nullptr;
                if (!ValidateCompressedTracksPayload(
                        m_scalar_payloads[i].data(),
                        m_scalar_payloads[i].size(),
                        acl::track_type8::float1f,
                        false,
                        tracks))
                    return ACL_PROJECT_INVALID_PAYLOAD;
                m_scalar_tracks.push_back(tracks);
            }
            return ACL_PROJECT_SUCCESS;
        }
        catch (...)
        {
            return ACL_PROJECT_NATIVE_FAILURE;
        }
    }
}

namespace acl_project_internal
{
    acl_project_error ProjectGroup::retain_decoder()
    {
        if (m_decoder_count == UINT32_MAX)
            return ACL_PROJECT_CAPACITY;
        m_decoder_count++;
        return ACL_PROJECT_SUCCESS;
    }

    acl_project_error ProjectGroup::release_decoder()
    {
        if (m_decoder_count == 0)
            return ACL_PROJECT_INVALID_STATE;
        m_decoder_count--;
        return ACL_PROJECT_SUCCESS;
    }
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_get_abi(
    acl_project_abi_info* info)
{
    if (info == nullptr)
        return ACL_PROJECT_INVALID_ARGUMENT;
    if (!acl_project_internal::IsStructValid(
            info->struct_size,
            info->struct_version,
            sizeof(acl_project_abi_info)))
        return ACL_PROJECT_INVALID_STRUCT;
    std::memset(info, 0, sizeof(*info));
    info->struct_size = sizeof(acl_project_abi_info);
    info->struct_version = ACL_PROJECT_STRUCT_VERSION;
    info->abi_version = ACL_PROJECT_ABI_VERSION;
    info->payload_format_version = ACL_PROJECT_PAYLOAD_FORMAT_VERSION;
    info->capabilities =
        ACL_PROJECT_CAPABILITY_TRANSFORM_COMPRESSION |
        ACL_PROJECT_CAPABILITY_SCALAR_COMPRESSION |
        ACL_PROJECT_CAPABILITY_DATABASE |
        ACL_PROJECT_CAPABILITY_GROUPS |
        ACL_PROJECT_CAPABILITY_DECODER |
        ACL_PROJECT_CAPABILITY_SPLIT_BULK;
    info->transform_sample_size = sizeof(acl_project_transform_sample);
    info->scalar_sample_size = sizeof(float);
    info->payload_view_size = sizeof(acl_project_payload_view);
    info->group_build_input_size = sizeof(acl_project_group_build_input);
    info->group_build_output_size = sizeof(acl_project_group_build_output);
    info->group_payload_input_size = sizeof(acl_project_group_payload_input);
    info->decoder_bind_input_size = sizeof(acl_project_decoder_bind_input);
    return ACL_PROJECT_SUCCESS;
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_group_create(
    const acl_project_group_payload_input* input,
    void** group)
{
    if (input == nullptr || group == nullptr)
        return ACL_PROJECT_INVALID_ARGUMENT;
    *group = nullptr;
    acl_project_internal::ProjectGroup* result =
        new (std::nothrow) acl_project_internal::ProjectGroup();
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
    *group = result;
    return ACL_PROJECT_SUCCESS;
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_group_get_clip_count(
    void* group,
    uint32_t* clip_count)
{
    if (group == nullptr || clip_count == nullptr)
        return ACL_PROJECT_INVALID_ARGUMENT;
    *clip_count = static_cast<acl_project_internal::ProjectGroup*>(group)->clip_count();
    return ACL_PROJECT_SUCCESS;
}

extern "C" ACL_PROJECT_API acl_project_error acl_project_group_release(
    void* group)
{
    if (group == nullptr)
        return ACL_PROJECT_INVALID_ARGUMENT;
    acl_project_internal::ProjectGroup* value =
        static_cast<acl_project_internal::ProjectGroup*>(group);
    if (!value->can_release())
        return ACL_PROJECT_INVALID_STATE;
    delete value;
    return ACL_PROJECT_SUCCESS;
}
