#pragma once

#include "acl_runtime.h"

#include "acl/compression/compress.h"
#include "acl/core/compressed_database.h"
#include "acl/core/compressed_tracks.h"
#include "acl/core/iallocator.h"
#include "acl/decompression/database/database.h"
#include "acl/decompression/database/database_settings.h"
#include "acl/decompression/database/database_streamer.h"
#include "acl/decompression/decompress.h"

#include <cstddef>
#include <cstdint>
#include <memory>
#include <utility>
#include <vector>

namespace acl_project_internal
{
    class ProjectAllocator final : public acl::iallocator
    {
    public:
        void* allocate(size_t size, size_t alignment) override;
        void deallocate(void* ptr, size_t size) override;
    };

    class OwnedBuffer final
    {
    public:
        OwnedBuffer() = default;
        ~OwnedBuffer();
        OwnedBuffer(const OwnedBuffer&) = delete;
        OwnedBuffer& operator=(const OwnedBuffer&) = delete;
        OwnedBuffer(OwnedBuffer&& other) noexcept;
        OwnedBuffer& operator=(OwnedBuffer&& other) noexcept;

        acl_project_error copy_from(
            ProjectAllocator& allocator,
            const void* source,
            uint32_t size);
        void reset();

        uint8_t* data() { return m_data; }
        const uint8_t* data() const { return m_data; }
        uint32_t size() const { return m_size; }
        bool empty() const { return m_size == 0; }

    private:
        ProjectAllocator* m_allocator = nullptr;
        uint8_t* m_data = nullptr;
        uint32_t m_size = 0;
    };

    struct ProjectTransformDecompressionSettings final : acl::default_transform_decompression_settings
    {
        using database_settings_type = acl::default_database_settings;
    };

    class ProjectDatabaseStreamer final : public acl::database_streamer
    {
    public:
        ProjectDatabaseStreamer(const uint8_t* data, uint32_t size);

        bool is_initialized() const override;
        const uint8_t* get_bulk_data(acl::quality_tier tier) const override;
        void stream_in(
            uint32_t offset,
            uint32_t size,
            bool can_allocate_bulk_data,
            acl::quality_tier tier,
            acl::streaming_request_id request_id) override;
        void stream_out(
            uint32_t offset,
            uint32_t size,
            bool can_deallocate_bulk_data,
            acl::quality_tier tier,
            acl::streaming_request_id request_id) override;

    private:
        const uint8_t* m_data;
        uint32_t m_size;
        acl::streaming_request m_requests[2];
    };

    acl_project_error CompressTransformInput(
        const acl_project_transform_input& input,
        bool enableDatabaseSupport,
        ProjectAllocator& allocator,
        acl::compressed_tracks*& output);

    acl_project_error CompressScalarInput(
        const acl_project_scalar_input& input,
        ProjectAllocator& allocator,
        acl::compressed_tracks*& output);

    class ProjectGroupBuild final
    {
    public:
        acl_project_error initialize(const acl_project_group_build_input& input);
        acl_project_error get_output(acl_project_group_build_output& output) const;

    private:
        void reset_output();
        void release_tracks(std::vector<acl::compressed_tracks*>& tracks);
        void rebuild_output_views();

        ProjectAllocator m_allocator;
        uint32_t m_clip_count = 0;
        std::vector<OwnedBuffer> m_transform_payloads;
        std::vector<OwnedBuffer> m_scalar_payloads;
        OwnedBuffer m_database_header;
        OwnedBuffer m_bulk_medium;
        OwnedBuffer m_bulk_low;
        mutable std::vector<acl_project_payload> m_transform_views;
        mutable std::vector<acl_project_payload> m_scalar_views;
    };

    class ProjectGroup final
    {
    public:
        acl_project_error initialize(const acl_project_group_payload_input& input);
        acl_project_error retain_decoder();
        acl_project_error release_decoder();
        bool can_release() const { return m_decoder_count == 0; }

        uint32_t clip_count() const { return m_clip_count; }
        const acl::compressed_tracks* transform(uint32_t index) const;
        const acl::compressed_tracks* scalar(uint32_t index) const;
        bool has_database() const { return m_database_context != nullptr; }
        const acl::database_context<acl::default_database_settings>* database() const
        {
            return m_database_context.get();
        }

    private:
        ProjectAllocator m_allocator;
        uint32_t m_clip_count = 0;
        std::vector<OwnedBuffer> m_transform_payloads;
        std::vector<OwnedBuffer> m_scalar_payloads;
        OwnedBuffer m_database_header;
        OwnedBuffer m_bulk_medium;
        OwnedBuffer m_bulk_low;
        std::unique_ptr<ProjectDatabaseStreamer> m_medium_streamer;
        std::unique_ptr<ProjectDatabaseStreamer> m_low_streamer;
        std::unique_ptr<acl::database_context<acl::default_database_settings>> m_database_context;
        std::vector<const acl::compressed_tracks*> m_transform_tracks;
        std::vector<const acl::compressed_tracks*> m_scalar_tracks;
        uint32_t m_decoder_count = 0;
    };

    class ProjectDecoder final
    {
    public:
        ProjectDecoder() = default;
        ~ProjectDecoder();

        acl_project_error bind(const acl_project_decoder_bind_input& input);
        acl_project_error unbind();
        acl_project_error sample_transform(
            float sample_time,
            acl_project_transform_sample* output,
            int32_t output_count);
        acl_project_error sample_scalar(
            float sample_time,
            float* output,
            int32_t output_count);

    private:
        ProjectGroup* m_group = nullptr;
        uint32_t m_clip_index = 0;
        bool m_bound = false;
        acl::decompression_context<ProjectTransformDecompressionSettings> m_transform_context;
        acl::decompression_context<acl::default_scalar_decompression_settings> m_scalar_context;
    };

    bool IsStructValid(uint32_t actual_size, uint32_t version, size_t expected_size);
    bool IsPayloadViewValid(const acl_project_payload_view& view, bool required);
    acl_project_error CopyCompressedTracks(
        ProjectAllocator& allocator,
        acl::compressed_tracks* tracks,
        OwnedBuffer& destination);
    void ReleaseCompressedTracks(ProjectAllocator& allocator, acl::compressed_tracks* tracks);
}
