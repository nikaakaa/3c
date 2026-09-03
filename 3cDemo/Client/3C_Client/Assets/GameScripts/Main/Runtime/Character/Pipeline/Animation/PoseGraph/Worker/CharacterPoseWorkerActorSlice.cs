using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal unsafe struct CharacterPoseWorkerActorSlice
    {
        CharacterPoseValuePageSlice m_Values;
        [NativeDisableUnsafePtrRestriction] void* m_OperationWeights;
        [NativeDisableUnsafePtrRestriction] void* m_RootOrientationWarpControls;
        [NativeDisableUnsafePtrRestriction] void* m_LinkedPoseActiveFragments;
        int m_OperationCount;
        int m_RootOrientationWarpCount;
        int m_LinkedPoseFragmentCount;

        internal static CharacterPoseWorkerActorSlice Create(
            in CharacterPoseGraphNativeBinding frame,
            CharacterPoseProgramExecutionView executionView,
            NativeArray<float> operationWeights,
            NativeArray<CharacterRootOrientationWarpNativeControl>
                rootOrientationWarpControls,
            NativeArray<byte> linkedPoseActiveFragments)
        {
            if (executionView == null)
                throw new System.ArgumentNullException(nameof(executionView));
            executionView.RequireValid();
            if (!operationWeights.IsCreated ||
                operationWeights.Length != frame.Layout.OperationCount ||
                !rootOrientationWarpControls.IsCreated ||
                rootOrientationWarpControls.Length !=
                    executionView.RootOrientationWarps.Length ||
                !linkedPoseActiveFragments.IsCreated ||
                linkedPoseActiveFragments.Length !=
                    executionView.LinkedPoseFragmentCount)
            {
                throw new System.ArgumentException(
                    "Pose Worker Actor control slice is invalid.");
            }
            CharacterPoseValuePageSlice values =
                CharacterPoseValuePageSlice.Create(
                    in frame,
                    executionView.ValueProducerOperationIndices,
                    executionView.LeftFootBoneIndex,
                    executionView.RightFootBoneIndex,
                    false);
            if (!values.IsValid)
                throw new System.InvalidOperationException(
                    "Pose Worker Actor Value slice is invalid.");
            return new CharacterPoseWorkerActorSlice
            {
                m_Values = values,
                m_OperationWeights =
                    NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(
                        operationWeights),
                m_RootOrientationWarpControls =
                    rootOrientationWarpControls.Length == 0
                        ? null
                        : NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(
                            rootOrientationWarpControls),
                m_LinkedPoseActiveFragments =
                    linkedPoseActiveFragments.Length == 0
                        ? null
                        : NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(
                            linkedPoseActiveFragments),
                m_OperationCount = operationWeights.Length,
                m_RootOrientationWarpCount =
                    rootOrientationWarpControls.Length,
                m_LinkedPoseFragmentCount =
                    linkedPoseActiveFragments.Length
            };
        }

        internal CharacterPoseValuePageSlice Values => m_Values;
        internal ulong WritePageIdentity => m_Values.WritePageIdentity;

        internal CharacterPoseNativeOperationHeader ApplyWeight(
            in CharacterPoseNativeOperationHeader operation) =>
            operation.WithWeight(Read<float>(
                m_OperationWeights,
                operation.Index));

        internal CharacterRootOrientationWarpNativeControl
            RootOrientationWarpControl(int index) =>
            Read<CharacterRootOrientationWarpNativeControl>(
                m_RootOrientationWarpControls,
                index);

        internal bool IsFragmentActive(int index) =>
            index < 0 || index < m_LinkedPoseFragmentCount &&
            Read<byte>(m_LinkedPoseActiveFragments, index) != 0;

        internal bool IsValid =>
            m_Values.IsValid && m_OperationWeights != null &&
            (m_RootOrientationWarpCount == 0 ||
             m_RootOrientationWarpControls != null) &&
            (m_LinkedPoseFragmentCount == 0 ||
             m_LinkedPoseActiveFragments != null) &&
            m_OperationCount > 0 && m_RootOrientationWarpCount >= 0 &&
            m_LinkedPoseFragmentCount >= 0;

        static T Read<T>(void* pointer, int index) where T : struct =>
            UnsafeUtility.ReadArrayElement<T>(pointer, index);
    }
}
