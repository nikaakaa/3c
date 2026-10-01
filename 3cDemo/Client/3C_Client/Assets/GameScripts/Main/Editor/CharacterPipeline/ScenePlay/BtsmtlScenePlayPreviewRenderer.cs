using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    sealed class BtsmtlScenePlayPreviewRenderer
    {
        const int HistoryFrameCapacity = 64;
        const int HistoryMaximumDimension = 512;

        internal struct RecordedFrame
        {
            internal RenderTexture Texture;
            internal Guid CharacterRuntimeId;
            internal Guid ExecutionBranchId;
            internal ulong RuntimeEpoch;
            internal ulong LogicTick;
            internal ulong PresentationFrame;
        }

        readonly Dictionary<object, Vector2Int> m_Viewports = new Dictionary<object, Vector2Int>();
        readonly RenderPipeline.StandardRequest m_Request = new RenderPipeline.StandardRequest();
        readonly RecordedFrame[] m_History = new RecordedFrame[HistoryFrameCapacity];
        Camera m_Camera;
        ulong m_RenderedFrame;
        Guid m_HistoryCaptureId;
        int m_HistoryCount;
        int m_NextHistoryFrame;

        internal event Action Changed;
        internal RenderTexture Texture { get; private set; }

        internal void SetViewport(object owner, Vector2Int size) => m_Viewports[owner] = size;
        internal void RemoveViewport(object owner) => m_Viewports.Remove(owner);

        internal void Render(Camera camera, ulong frame, RuntimeDiagnosticsContext diagnostics)
        {
            Vector2Int size = default;
            foreach (Vector2Int viewport in m_Viewports.Values)
                if ((long)viewport.x * viewport.y > (long)size.x * size.y)
                    size = viewport;
            if (size == default)
                return;
            bool resize = Texture == null || Texture.width != size.x || Texture.height != size.y;
            if (!resize && m_RenderedFrame == frame)
            {
                RecordFrame(frame, diagnostics);
                return;
            }
            if (resize)
            {
                ReleaseTexture();
                Texture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Workbench Preview",
                    hideFlags = HideFlags.HideAndDontSave
                };
                Texture.Create();
                m_Request.destination = Texture;
            }
            m_Camera = camera;
            m_Camera.targetTexture = Texture;
            RenderPipeline.SubmitRenderRequest(m_Camera, m_Request);
            m_RenderedFrame = frame;
            RecordFrame(frame, diagnostics);
            Changed?.Invoke();
        }

        void RecordFrame(ulong presentationFrame, RuntimeDiagnosticsContext diagnostics)
        {
            if (!diagnostics.Store.IsCaptureRecording)
                return;
            Guid captureId = diagnostics.Store.CaptureStatus.CaptureId;
            if (m_HistoryCaptureId != captureId)
            {
                m_HistoryCaptureId = captureId;
                m_HistoryCount = 0;
                m_NextHistoryFrame = 0;
            }
            if (m_HistoryCount != 0)
            {
                ref RecordedFrame latest = ref m_History[(m_NextHistoryFrame + HistoryFrameCapacity - 1) % HistoryFrameCapacity];
                if (latest.PresentationFrame == presentationFrame && latest.RuntimeEpoch == diagnostics.RuntimeEpoch &&
                    latest.ExecutionBranchId == diagnostics.ExecutionBranchId)
                    return;
            }
            if (m_History[0].Texture == null)
            {
                float scale = Mathf.Min(1f, HistoryMaximumDimension / (float)Mathf.Max(Texture.width, Texture.height));
                int width = Mathf.Max(1, Mathf.RoundToInt(Texture.width * scale));
                int height = Mathf.Max(1, Mathf.RoundToInt(Texture.height * scale));
                for (int i = 0; i < m_History.Length; i++)
                {
                    m_History[i].Texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                    {
                        name = "Workbench Preview History",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    m_History[i].Texture.Create();
                }
            }
            ref RecordedFrame recorded = ref m_History[m_NextHistoryFrame];
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(Texture, recorded.Texture);
            RenderTexture.active = previous;
            recorded.CharacterRuntimeId = diagnostics.CharacterRuntimeId;
            recorded.ExecutionBranchId = diagnostics.ExecutionBranchId;
            recorded.RuntimeEpoch = diagnostics.RuntimeEpoch;
            recorded.LogicTick = diagnostics.LogicTick;
            recorded.PresentationFrame = presentationFrame;
            m_NextHistoryFrame = (m_NextHistoryFrame + 1) % HistoryFrameCapacity;
            m_HistoryCount = Mathf.Min(m_HistoryCount + 1, HistoryFrameCapacity);
        }

        internal bool TryGetHistoryFrame(Guid captureId, in RuntimeTraceEvent trace, out RecordedFrame recorded)
        {
            recorded = default;
            if (captureId != m_HistoryCaptureId)
                return false;
            for (int i = 0; i < m_HistoryCount; i++)
            {
                ref RecordedFrame candidate = ref m_History[(m_NextHistoryFrame + HistoryFrameCapacity - i - 1) % HistoryFrameCapacity];
                if (candidate.CharacterRuntimeId != trace.RuntimeInstance.CharacterRuntimeId ||
                    candidate.ExecutionBranchId != trace.ExecutionBranchId || candidate.RuntimeEpoch != trace.RuntimeEpoch)
                    continue;
                ulong position = trace.Domain == RuntimeTraceDomain.Presentation ? candidate.PresentationFrame : candidate.LogicTick;
                if (position != trace.Position)
                    continue;
                recorded = candidate;
                return true;
            }
            return false;
        }

        internal void Reset()
        {
            m_Viewports.Clear();
            for (int i = 0; i < m_History.Length; i++)
            {
                RenderTexture history = m_History[i].Texture;
                if (history == null)
                    continue;
                history.Release();
                UnityEngine.Object.DestroyImmediate(history);
                m_History[i] = default;
            }
            m_HistoryCaptureId = Guid.Empty;
            m_HistoryCount = 0;
            m_NextHistoryFrame = 0;
            ReleaseTexture();
            Changed?.Invoke();
        }

        void ReleaseTexture()
        {
            if (Texture == null)
                return;
            if (m_Camera && m_Camera.targetTexture == Texture)
                m_Camera.targetTexture = null;
            m_Request.destination = null;
            Texture.Release();
            UnityEngine.Object.DestroyImmediate(Texture);
            Texture = null;
            m_Camera = null;
            m_RenderedFrame = 0;
        }
    }
}
