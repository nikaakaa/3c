using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    sealed class BtsmtlScenePlayPreviewRenderer
    {
        readonly Dictionary<object, Vector2Int> m_Viewports = new Dictionary<object, Vector2Int>();
        readonly RenderPipeline.StandardRequest m_Request = new RenderPipeline.StandardRequest();
        Camera m_Camera;
        ulong m_RenderedFrame;

        internal event Action Changed;
        internal RenderTexture Texture { get; private set; }

        internal void SetViewport(object owner, Vector2Int size) => m_Viewports[owner] = size;
        internal void RemoveViewport(object owner) => m_Viewports.Remove(owner);

        internal void Render(Camera camera, ulong frame)
        {
            Vector2Int size = default;
            foreach (Vector2Int viewport in m_Viewports.Values)
                if ((long)viewport.x * viewport.y > (long)size.x * size.y)
                    size = viewport;
            if (size == default)
                return;
            bool resize = Texture == null || Texture.width != size.x || Texture.height != size.y;
            if (!resize && m_RenderedFrame == frame)
                return;
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
            Changed?.Invoke();
        }

        internal void Reset()
        {
            m_Viewports.Clear();
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
