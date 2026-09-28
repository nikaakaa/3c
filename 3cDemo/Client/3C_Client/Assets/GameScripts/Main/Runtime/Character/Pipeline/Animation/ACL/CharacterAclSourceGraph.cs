using System;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal sealed class CharacterAclSourceGraph : IDisposable
    {
        readonly PlayableGraph m_Graph;
        readonly AnimationScriptPlayable[] m_ClipOutputs;
        readonly int[] m_ActiveClipIndices;
        readonly NativeArray<CharacterComponentBonePose> m_ComponentScratch;
        AnimationMixerPlayable m_Mixer;
        AnimationScriptPlayable m_Capture;
        AnimationSourcePoseCaptureJob m_CaptureJob;
        int m_ActiveClipCount;
        bool m_Disposed;

        internal CharacterAclSourceGraph(
            PlayableGraph graph,
            CharacterAnimationRigPayload rig,
            NativeArray<TransformStreamHandle> handles,
            NativeArray<AnimationLocalBonePose> referencePose,
            NativeArray<int> physicalParentIndices,
            NativeArray<CharacterVirtualBoneDescriptor> virtualBones,
            NativeArray<int> trackByPoseBone,
            NativeArray<CharacterAclNativeTransformSample>[] transformValues,
            int clipCapacity)
        {
            if (!graph.IsValid() || rig == null || !handles.IsCreated ||
                !referencePose.IsCreated || !physicalParentIndices.IsCreated ||
                !virtualBones.IsCreated || !trackByPoseBone.IsCreated ||
                transformValues == null || transformValues.Length != clipCapacity ||
                clipCapacity <= 0)
                throw new ArgumentException("ACL source graph input is invalid.");
            m_Graph = graph;
            m_ActiveClipIndices = new int[clipCapacity];
            m_ComponentScratch = new NativeArray<CharacterComponentBonePose>(
                rig.PhysicalBoneCount,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            try
            {
                m_Mixer = AnimationMixerPlayable.Create(graph, clipCapacity);
                m_ClipOutputs = new AnimationScriptPlayable[clipCapacity];
                for (int i = 0; i < clipCapacity; i++)
                {
                    m_ClipOutputs[i] = AnimationScriptPlayable.Create(
                        graph,
                        new CharacterAclClipPoseJob(
                            handles,
                            transformValues[i],
                            trackByPoseBone),
                        1);
                    graph.Connect(m_ClipOutputs[i], 0, m_Mixer, i);
                    m_Mixer.SetInputWeight(i, 0f);
                }
                m_CaptureJob =
                    new AnimationSourcePoseCaptureJob(
                        rig.BoneCounts,
                        handles,
                        referencePose,
                        physicalParentIndices,
                        virtualBones,
                        m_ComponentScratch,
                        rig.RootPhysicalBoneIndex,
                        rig.RootBonePolicy,
                        rig.ScalePolicy);
                m_Capture = AnimationScriptPlayable.Create(graph, m_CaptureJob, 1);
                graph.Connect(m_Mixer, 0, m_Capture, 0);
                m_Capture.SetInputWeight(0, 1f);
            }
            catch
            {
                DestroyGraph();
                if (m_ComponentScratch.IsCreated)
                    m_ComponentScratch.Dispose();
                throw;
            }
        }

        internal AnimationScriptPlayable Output => m_Capture;

        internal void Apply(
            CharacterClipSampleBatch batch,
            in AnimationPoseSourceCaptureBinding capture)
        {
            RequireAlive();
            if (batch == null || batch.Count <= 0 || batch.Count > m_ClipOutputs.Length)
                throw new ArgumentException("ACL source graph sample batch does not match its catalog.");
            ClearInputs();
            for (int i = 0; i < batch.Count; i++)
            {
                int clipIndex = batch[i].ClipBindingIndex;
                m_Mixer.SetInputWeight(
                    clipIndex,
                    batch.GetNormalizedWeight(i));
                m_ActiveClipIndices[m_ActiveClipCount++] = clipIndex;
            }
            m_CaptureJob.BindFrame(in capture);
            m_Capture.SetJobData(m_CaptureJob);
        }

        internal void ClearInputs()
        {
            RequireAlive();
            for (int i = 0; i < m_ActiveClipCount; i++)
                m_Mixer.SetInputWeight(m_ActiveClipIndices[i], 0f);
            m_ActiveClipCount = 0;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            DestroyGraph();
            if (m_ComponentScratch.IsCreated)
                m_ComponentScratch.Dispose();
            m_Disposed = true;
        }

        void DestroyGraph()
        {
            if (!m_Graph.IsValid())
                return;
            if (((Playable)m_Capture).IsValid())
                m_Graph.DestroyPlayable(m_Capture);
            if (m_Mixer.IsValid())
                m_Graph.DestroyPlayable(m_Mixer);
            if (m_ClipOutputs != null)
            {
                for (int i = 0; i < m_ClipOutputs.Length; i++)
                {
                    if (((Playable)m_ClipOutputs[i]).IsValid())
                        m_Graph.DestroyPlayable(m_ClipOutputs[i]);
                }
            }
            m_Capture = default;
            m_Mixer = default;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterAclSourceGraph));
        }
    }
}
