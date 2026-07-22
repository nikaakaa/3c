using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct AnimationPoseRequestWorkspaceLayout
    {
        public AnimationPoseRequestWorkspaceLayout(
            int sourceCapacity,
            int clipStride,
            int parameterStride,
            int footPlacementWeightParameterIndex)
        {
            if (sourceCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceCapacity));
            if (clipStride <= 0)
                throw new ArgumentOutOfRangeException(nameof(clipStride));
            if (parameterStride <= 0)
                throw new ArgumentOutOfRangeException(nameof(parameterStride));
            if (footPlacementWeightParameterIndex < 0 || footPlacementWeightParameterIndex >= parameterStride)
                throw new ArgumentOutOfRangeException(nameof(footPlacementWeightParameterIndex));

            SourceCapacity = sourceCapacity;
            ClipStride = clipStride;
            ParameterStride = parameterStride;
            FootPlacementWeightParameterIndex = footPlacementWeightParameterIndex;
            ClipPlanCapacity = checked(sourceCapacity * clipStride);
            PoseParameterCapacity = checked(sourceCapacity * parameterStride);
        }

        public int SourceCapacity { get; }
        public int ClipStride { get; }
        public int ParameterStride { get; }
        public int FootPlacementWeightParameterIndex { get; }
        public int ClipPlanCapacity { get; }
        public int PoseParameterCapacity { get; }

        public bool IsValid => SourceCapacity > 0 && ClipStride > 0 && ParameterStride > 0 &&
                               FootPlacementWeightParameterIndex >= 0 &&
                               FootPlacementWeightParameterIndex < ParameterStride &&
                               ClipPlanCapacity > 0 && PoseParameterCapacity > 0 &&
                               (long)SourceCapacity * ClipStride == ClipPlanCapacity &&
                               (long)SourceCapacity * ParameterStride == PoseParameterCapacity;
    }
}
