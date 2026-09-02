using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterFootPoseWatchDiscovery
    {
        internal static IReadOnlyList<AnimationPoseWatchIdentity> Build(
            AnimationPresentationRuntimeSnapshot snapshot)
        {
            var result = new List<AnimationPoseWatchIdentity>(4);
            AnimationReadOnlyBuffer<AnimationPoseOperationSnapshot> operations =
                snapshot.Operations;
            for (int i = 0; i < operations.Count; i++)
            {
                AnimationPoseOperationSnapshot operation = operations[i];
                if (operation.Code != CharacterPoseOperationCode.FootPlacement &&
                    operation.Code != CharacterPoseOperationCode.FullBodyIK)
                {
                    continue;
                }
                result.Add(new AnimationPoseWatchIdentity(
                    operation.GraphId,
                    snapshot.PoseGraphRevision,
                    operation.NodeId,
                    operation.CallSite));
            }
            return result;
        }
    }
}
