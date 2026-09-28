using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterFinalPosePropertyWriter
    {
        readonly struct RendererTarget
        {
            internal RendererTarget(SkinnedMeshRenderer renderer, Mesh mesh)
            {
                Renderer = renderer;
                Mesh = mesh;
            }

            internal SkinnedMeshRenderer Renderer { get; }
            internal Mesh Mesh { get; }
            internal void RequireCurrent()
            {
                if (!Renderer || !Mesh || Renderer.sharedMesh != Mesh)
                    throw new InvalidOperationException("Final animation property Renderer target is stale.");
            }
        }

        readonly CharacterPresentationAnimationPropertyBinding[] m_Bindings;
        readonly SkinnedMeshRenderer[] m_Renderers;
        readonly RendererTarget[] m_RendererTargets;
        readonly float[] m_InitialWeights;

        internal CharacterFinalPosePropertyWriter(
            CharacterAnimationRigBinding rigBinding,
            CharacterAnimationInputContract inputContract,
            IReadOnlyList<CharacterPresentationAnimationPropertyBinding> bindings)
        {
            if (!rigBinding)
                throw new ArgumentNullException(nameof(rigBinding));
            Transform root = rigBinding.Animator ? rigBinding.Animator.transform : null;
            if (inputContract == null)
                throw new ArgumentNullException(nameof(inputContract));
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));
            m_Bindings = new CharacterPresentationAnimationPropertyBinding[bindings.Count];
            m_Renderers = new SkinnedMeshRenderer[bindings.Count];
            m_InitialWeights = new float[bindings.Count];
            var bindingIds = new HashSet<string>(StringComparer.Ordinal);
            var renderers = new HashSet<SkinnedMeshRenderer>();
            var rendererTargets = new List<RendererTarget>();
            for (int i = 0; i < m_Bindings.Length; i++)
            {
                CharacterPresentationAnimationPropertyBinding binding = bindings[i] ??
                    throw new InvalidOperationException("Final animation property binding is missing.");
                ValidateNativeBinding(binding, inputContract);
                if (!bindingIds.Add(binding.BindingId))
                    throw new InvalidOperationException("Final animation property binding identity is duplicated.");
                CharacterAnimationRendererBinding renderer =
                    rigBinding.FindRendererBinding(binding.RendererBindingId);
                if (renderer == null)
                    throw new InvalidOperationException($"Final animation property '{binding.BindingId}' has no Renderer binding '{binding.RendererBindingId}'.");
                m_Bindings[i] = binding;
                renderer.RequireValid(root);
                m_Renderers[i] = renderer.Renderer;
                ValidateBinding(binding, renderer);
                if (renderers.Add(renderer.Renderer))
                {
                    rendererTargets.Add(new RendererTarget(
                        renderer.Renderer, binding.ExpectedMesh));
                }
                float initialWeight = renderer.Renderer.GetBlendShapeWeight(binding.BlendShapeIndex);
                if (!float.IsFinite(initialWeight))
                    throw new InvalidOperationException($"Final animation property '{binding.BindingId}' initial BlendShape weight is not finite.");
                m_InitialWeights[i] = initialWeight;
            }
            m_RendererTargets = rendererTargets.ToArray();
        }

        internal int BindingCount => m_Bindings.Length;

        void RequireCurrentTargets()
        {
            for (int i = 0; i < m_RendererTargets.Length; i++)
                m_RendererTargets[i].RequireCurrent();
        }

        static void ValidateBinding(
            CharacterPresentationAnimationPropertyBinding binding,
            CharacterAnimationRendererBinding renderer)
        {
            Mesh mesh = renderer.Renderer.sharedMesh;
            if (renderer.ExpectedMesh != binding.ExpectedMesh ||
                !string.Equals(renderer.MeshContentHash, binding.MeshContentHash, StringComparison.Ordinal) ||
                mesh != binding.ExpectedMesh ||
                mesh.blendShapeCount <= binding.BlendShapeIndex)
                throw new InvalidOperationException($"Final animation property '{binding.BindingId}' Renderer binding is stale.");
        }

        static void ValidateNativeBinding(
            CharacterPresentationAnimationPropertyBinding binding,
            CharacterAnimationInputContract inputContract)
        {
            if (string.IsNullOrWhiteSpace(binding.BindingId) ||
                !binding.ParameterId.IsValid ||
                binding.ParameterIndex < 0 ||
                binding.ParameterIndex >= inputContract.Parameters.Count ||
                !float.IsFinite(binding.DefaultValue) ||
                string.IsNullOrWhiteSpace(binding.RendererBindingId) ||
                !binding.ExpectedMesh ||
                !CharacterAclHash.IsSha256(binding.MeshContentHash) ||
                string.IsNullOrWhiteSpace(binding.BlendShapeName) ||
                binding.BlendShapeIndex < 0 ||
                binding.BlendShapeIndex >= binding.ExpectedMesh.blendShapeCount ||
                !string.Equals(
                    binding.ExpectedMesh.GetBlendShapeName(binding.BlendShapeIndex),
                    binding.BlendShapeName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Native animation property binding is invalid.");
            }
            CharacterPoseParameterDeclaration parameter =
                inputContract.Parameters[binding.ParameterIndex];
            if (parameter == null ||
                !parameter.ParameterId.Equals(binding.ParameterId) ||
                parameter.Usage != CharacterPoseParameterUsage.AnimatedProperty ||
                parameter.ValueType != PoseParameterValueType.Float ||
                !string.Equals(parameter.Unit, binding.Unit, StringComparison.Ordinal) ||
                parameter.DefaultValue != binding.DefaultValue)
            {
                throw new InvalidOperationException(
                    $"Native animation property '{binding.BindingId}' does not match its input parameter.");
            }
        }

        internal void ValidateBeforeWrite(in ComposedAnimationPoseFrame frame)
        {
            RequireCurrentTargets();
            AnimationReadOnlyBuffer<byte> availability = frame.PoseParameterAvailability;
            for (int i = 0; i < m_Bindings.Length; i++)
            {
                CharacterPresentationAnimationPropertyBinding binding = m_Bindings[i];
                if (availability[binding.ParameterIndex] != 1)
                    throw new InvalidOperationException($"Final animation property '{binding.BindingId}' has no valid committed source value.");
            }
        }

        internal void Write(in ComposedAnimationPoseFrame frame)
        {
            AnimationReadOnlyBuffer<float> parameters = frame.PoseParameters;
            for (int i = 0; i < m_Bindings.Length; i++)
            {
                CharacterPresentationAnimationPropertyBinding binding = m_Bindings[i];
                m_Renderers[i].SetBlendShapeWeight(
                    binding.BlendShapeIndex,
                    parameters[binding.ParameterIndex]);
            }
        }

        internal void WriteDefaults()
        {
            RequireCurrentTargets();
            for (int i = 0; i < m_Bindings.Length; i++)
            {
                CharacterPresentationAnimationPropertyBinding binding = m_Bindings[i];
                m_Renderers[i].SetBlendShapeWeight(
                    binding.BlendShapeIndex,
                    binding.DefaultValue);
            }
        }

        internal void RestoreInitial()
        {
            RequireCurrentTargets();
            for (int i = 0; i < m_Bindings.Length; i++)
            {
                m_Renderers[i].SetBlendShapeWeight(
                    m_Bindings[i].BlendShapeIndex,
                    m_InitialWeights[i]);
            }
        }
    }
}
