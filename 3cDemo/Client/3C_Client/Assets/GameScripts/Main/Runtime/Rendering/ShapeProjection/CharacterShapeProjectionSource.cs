using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdPersonRendering.ShapeProjection
{
    [DisallowMultipleComponent]
    public sealed class CharacterShapeProjectionSource : MonoBehaviour
    {
        [Serializable]
        public struct RendererBinding
        {
            [SerializeField] string slotId;
            [SerializeField] SkinnedMeshRenderer renderer;

            public string SlotId => slotId;
            public SkinnedMeshRenderer Renderer => renderer;
        }

        ShapeProjectionSourceId sourceId;
        [SerializeField] CharacterShapeProjectionProfile profile;
        [SerializeField] CharacterShapeProjectionArtifact artifact;
        [SerializeField] RendererBinding[] rendererBindings = Array.Empty<RendererBinding>();
        [SerializeField] bool projectionEnabled = true;
        [SerializeField] bool renderInGameCamera = true;
        [SerializeField] ShapeProjectionDebugView debugView;
        [SerializeField, HideInInspector] ShapeProjectionRuntimeState runtimeState = ShapeProjectionRuntimeState.Stale;
        [SerializeField, HideInInspector] string fault;
        [SerializeField, HideInInspector] ShapeProjectionDiagnosticsSnapshot diagnostics;

        int generation;

        public ShapeProjectionSourceId SourceId => sourceId;
        public CharacterShapeProjectionProfile Profile => profile;
        public CharacterShapeProjectionArtifact Artifact => artifact;
        public RendererBinding[] RendererBindings => rendererBindings;
        public bool ProjectionEnabled => projectionEnabled;
        public bool RenderInGameCamera => renderInGameCamera;
        public ShapeProjectionDebugView DebugView => debugView;
        public ShapeProjectionRuntimeState RuntimeState => runtimeState;
        public string Fault => fault;
        public int Generation => generation;
        public ShapeProjectionDiagnosticsSnapshot Diagnostics => diagnostics;
        public bool IsPrepared => projectionEnabled && (runtimeState == ShapeProjectionRuntimeState.Ready
                                  || runtimeState == ShapeProjectionRuntimeState.WaitingForFirstCompatibleResult);

        void OnEnable()
        {
            generation++;
            EnsureRuntimeIdentity();
            ApplyRendererPublishingMode(projectionEnabled);
            if (!projectionEnabled)
            {
                MarkDisabled();
                return;
            }
            PrepareAndRegister();
        }

        void OnDisable()
        {
            CharacterShapeProjectionRegistry.Unregister(this);
            generation++;
            ApplyRendererPublishingMode(false);
            MarkDisabled();
        }

        public void SetProjectionEnabled(bool value)
        {
            if (projectionEnabled == value)
            {
                ApplyRendererPublishingMode(value);
                if (!value)
                    MarkDisabled();
                return;
            }

            CharacterShapeProjectionRegistry.Unregister(this);
            generation++;
            projectionEnabled = value;
            ApplyRendererPublishingMode(value);
            if (!value)
            {
                MarkDisabled();
                return;
            }

            runtimeState = ShapeProjectionRuntimeState.Stale;
            if (Application.isPlaying && isActiveAndEnabled)
                PrepareAndRegister();
        }

        public ShapeProjectionValidationResult ValidateSource()
        {
            if (!sourceId.IsValid)
                return ShapeProjectionValidationResult.Fail("SourceId为空");
            if (profile == null || artifact == null)
                return ShapeProjectionValidationResult.Fail("Source缺少Profile或Artifact");
            if (!renderInGameCamera)
                return ShapeProjectionValidationResult.Fail("Source必须显式参与正式Game Camera");
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsAsyncGPUReadback)
                return ShapeProjectionValidationResult.Fail("当前图形设备不支持Compute Shader或Async GPU Readback");

            ShapeProjectionValidationResult profileResult = profile.ValidateProfile();
            if (!profileResult.IsValid)
                return profileResult;
            ShapeProjectionValidationResult artifactResult = artifact.ValidateArtifact();
            if (!artifactResult.IsValid)
                return artifactResult;
            if (!profile.ProfileId.Equals(artifact.ProfileId) || profile.Revision != artifact.ProfileRevision
                                                               || profile.ContentHash != artifact.ProfileContentHash)
                return ShapeProjectionValidationResult.Fail("Profile与Artifact lineage不一致");
            if (!profile.Capacity.Equals(artifact.Capacity))
                return ShapeProjectionValidationResult.Fail("Profile与Artifact固定容量不一致");
            if (rendererBindings == null || rendererBindings.Length != artifact.Renderers.Length)
                return ShapeProjectionValidationResult.Fail("Renderer绑定数量与Artifact不一致");

            for (int i = 0; i < rendererBindings.Length; i++)
            {
                RendererBinding binding = rendererBindings[i];
                ShapeProjectionRendererRecord record = artifact.Renderers[i];
                if (binding.Renderer == null || string.IsNullOrWhiteSpace(binding.SlotId)
                                             || !string.Equals(binding.SlotId, record.SlotId, StringComparison.Ordinal))
                    return ShapeProjectionValidationResult.Fail($"Renderer绑定{i}与Artifact Slot不一致");
                if (binding.Renderer.sharedMesh != record.SourceMesh)
                    return ShapeProjectionValidationResult.Fail($"Renderer绑定{i}的Mesh与Artifact不一致");
                if (!binding.Renderer.transform.IsChildOf(transform))
                    return ShapeProjectionValidationResult.Fail($"Renderer绑定{i}不属于当前Source Root");
                ShadowCastingMode expectedMode = projectionEnabled ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
                if (binding.Renderer.shadowCastingMode != expectedMode)
                    return ShapeProjectionValidationResult.Fail(projectionEnabled
                        ? $"Renderer绑定{i}必须使用ShadowsOnly并停止Forward彩色发布"
                        : $"Renderer绑定{i}必须恢复普通Forward彩色发布");

                Material[] materials = binding.Renderer.sharedMaterials;
                Material[] expected = record.SourceMaterials;
                if (materials == null || expected == null || materials.Length != expected.Length)
                    return ShapeProjectionValidationResult.Fail($"Renderer绑定{i}的材质集合与Artifact不一致");
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    if (materials[materialIndex] != expected[materialIndex])
                        return ShapeProjectionValidationResult.Fail($"Renderer绑定{i}的材质{materialIndex}与Artifact不一致");
                }

                for (int j = i + 1; j < rendererBindings.Length; j++)
                {
                    if (rendererBindings[j].Renderer == binding.Renderer
                        || string.Equals(rendererBindings[j].SlotId, binding.SlotId, StringComparison.Ordinal))
                        return ShapeProjectionValidationResult.Fail($"Renderer绑定{i}存在重复Renderer或SlotId");
                }
            }

            return ShapeProjectionValidationResult.Success;
        }

        public void MarkWaitingForFirstResult()
        {
            if (runtimeState != ShapeProjectionRuntimeState.Faulted)
                runtimeState = ShapeProjectionRuntimeState.WaitingForFirstCompatibleResult;
        }

        public void MarkReady()
        {
            if (runtimeState != ShapeProjectionRuntimeState.Faulted)
                runtimeState = ShapeProjectionRuntimeState.Ready;
        }

        public void SetFault(string message)
        {
            fault = message ?? "未知Shape Projection错误";
            runtimeState = ShapeProjectionRuntimeState.Faulted;
            CharacterShapeProjectionRegistry.Unregister(this);
        }

        public void PublishDiagnostics(ShapeProjectionDiagnosticsSnapshot snapshot)
        {
            diagnostics = snapshot;
        }

#if UNITY_EDITOR
        public void EnsureIdentity()
        {
            EnsureRuntimeIdentity();
        }
#endif

        void PrepareAndRegister()
        {
            EnsureRuntimeIdentity();
            fault = string.Empty;
            ShapeProjectionValidationResult result = ValidateSource();
            if (!result.IsValid)
            {
                SetFault(result.Error);
                return;
            }

            runtimeState = ShapeProjectionRuntimeState.WaitingForFirstCompatibleResult;
            if (!CharacterShapeProjectionRegistry.TryRegister(this, out string error))
                SetFault(error);
        }

        void EnsureRuntimeIdentity()
        {
            if (!sourceId.IsValid)
                sourceId = new ShapeProjectionSourceId(Guid.NewGuid().ToString("N"));
        }

        void ApplyRendererPublishingMode(bool useShapeProjection)
        {
            if (rendererBindings == null)
                return;
            ShadowCastingMode mode = useShapeProjection ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            for (int i = 0; i < rendererBindings.Length; i++)
            {
                SkinnedMeshRenderer renderer = rendererBindings[i].Renderer;
                if (renderer != null)
                    renderer.shadowCastingMode = mode;
            }
        }

        void MarkDisabled()
        {
            fault = string.Empty;
            runtimeState = ShapeProjectionRuntimeState.Disabled;
            diagnostics = new ShapeProjectionDiagnosticsSnapshot
            {
                SourceId = sourceId,
                SourceGeneration = generation,
                RendererCount = rendererBindings?.Length ?? 0,
                State = ShapeProjectionRuntimeState.Disabled
            };
        }
    }
}
