using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeRoleSession : IDisposable
    {
        readonly CharacterPoseNativeRoleRuntime m_Role;
        readonly CharacterPoseNativeFrameCoordinator m_Frame;
        bool m_Disposed;

        internal CharacterPoseNativeRoleSession(
            CharacterPoseNativeRoleRuntime role)
        {
            m_Role = role ?? throw new ArgumentNullException(nameof(role));
            m_Frame = role.CreateFrameCoordinator();
        }

        internal CharacterPoseNativeRoleRuntime Role => m_Role;
        internal CharacterPoseNativeFrameCoordinator Frame => m_Frame;
        internal ulong InstanceId => m_Role.InstanceId;
        internal ulong ResetGeneration => m_Role.ResetGeneration;
        internal PoseGraphId GraphId => m_Role.GraphId;
        internal string GraphRevision => m_Role.GraphRevision;
        internal string ResourceRevision => m_Role.ResourceRevision;

        internal CharacterPoseNativeResetResult Reset(ulong resetGeneration) =>
            m_Role.Reset(resetGeneration);

        internal bool TryObserve(
            PoseNodeId nodeId,
            PosePortId portId,
            out CharacterPoseNativeNodeObservation observation) =>
            m_Role.TryObserve(nodeId, portId, out observation);

        internal bool TryObserveFinalPose(out ComposedAnimationPoseFrame frame) =>
            m_Role.TryGetCommittedPose(out frame);

        internal void Stop()
        {
            m_Frame.Stop();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Frame.Dispose();
        }
    }

    internal static class CharacterPoseNativeRoleEntry
    {
        internal static CharacterPoseNativeAdoptedResult Create(
            ulong requestId,
            ActorId actorId,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigPayload rig,
            CharacterAnimationInputContract inputContract,
            string resourceRevision,
            in CharacterPoseNativeInstanceContext context,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            CharacterPoseSourceModule source,
            CharacterPoseConstraintRuntime constraints,
            CharacterPoseNativeSourceHandlerComposition sourceHandlers,
            CharacterPoseNativeConstraintHandlerComposition constraintHandlers,
            CharacterPoseNativeManagedHandlerComposition managedHandlers,
            IReadOnlyList<CharacterPresentationAnimationPropertyBinding> animationProperties,
            IReadOnlyList<PoseNodeId> playerNodeIds,
            int contributionCapacity,
            out CharacterPoseNativeRoleSession session)
        {
            CharacterPoseNativeGraphPrepareResult preparation =
                CharacterPoseNativeRoleRuntime.Prepare(
                    requestId,
                    actorId,
                    profile,
                    rig,
                    inputContract,
                    resourceRevision);
            CharacterPoseNativeRoleDependencies dependencies = null;
            if (!preparation.IsReady)
            {
                session = null;
                return CharacterPoseNativeAdoptedResult.Failed(
                    in preparation,
                    resetGeneration,
                    preparation.FailureCode,
                    preparation.Message);
            }
            CharacterPoseNativePreparedBinding preparedBinding =
                preparation.PreparedBinding;
            dependencies = CharacterPoseNativeRoleDependencyFactory.Create(
                in preparedBinding,
                context.RigBinding,
                context.RootHierarchy,
                source,
                constraints,
                sourceHandlers,
                constraintHandlers,
                managedHandlers,
                animationProperties,
                playerNodeIds,
                contributionCapacity);
            CharacterPoseNativeAdoptedResult adopted =
                CharacterPoseNativeRoleRuntime.Create(
                    in preparation,
                    in context,
                    instanceId,
                    resetGeneration,
                    reason,
                    dependencies,
                    out CharacterPoseNativeRoleRuntime role);
            if (!adopted.IsAdopted)
            {
                session = null;
                return adopted;
            }
            try
            {
                session = new CharacterPoseNativeRoleSession(role);
            }
            catch
            {
                role.Dispose();
                session = null;
                throw;
            }
            return adopted;
        }

        internal static CharacterPoseNativeAdoptedResult Replace(
            CharacterPoseNativeRoleSession current,
            ulong requestId,
            ActorId actorId,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigPayload rig,
            CharacterAnimationInputContract inputContract,
            string resourceRevision,
            in CharacterPoseNativeInstanceContext context,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            CharacterPoseSourceModule source,
            CharacterPoseConstraintRuntime constraints,
            CharacterPoseNativeSourceHandlerComposition sourceHandlers,
            CharacterPoseNativeConstraintHandlerComposition constraintHandlers,
            CharacterPoseNativeManagedHandlerComposition managedHandlers,
            IReadOnlyList<CharacterPresentationAnimationPropertyBinding> animationProperties,
            IReadOnlyList<PoseNodeId> playerNodeIds,
            int contributionCapacity,
            out CharacterPoseNativeRoleSession session)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));
            CharacterPoseNativeGraphPrepareResult preparation =
                CharacterPoseNativeRoleRuntime.Prepare(
                    requestId,
                    actorId,
                    profile,
                    rig,
                    inputContract,
                    resourceRevision);
            CharacterPoseNativeRoleDependencies dependencies = null;
            if (!preparation.IsReady)
            {
                session = null;
                return CharacterPoseNativeAdoptedResult.Failed(
                    in preparation,
                    resetGeneration,
                    preparation.FailureCode,
                    preparation.Message);
            }
            CharacterPoseNativePreparedBinding preparedBinding =
                preparation.PreparedBinding;
            dependencies = CharacterPoseNativeRoleDependencyFactory.Create(
                in preparedBinding,
                context.RigBinding,
                context.RootHierarchy,
                source,
                constraints,
                sourceHandlers,
                constraintHandlers,
                managedHandlers,
                animationProperties,
                playerNodeIds,
                contributionCapacity);
            CharacterPoseNativeAdoptedResult adopted =
                CharacterPoseNativeRoleRuntime.Replace(
                    current.Role,
                    in preparation,
                    in context,
                    instanceId,
                    resetGeneration,
                    reason,
                    dependencies,
                    out CharacterPoseNativeRoleRuntime role);
            if (!adopted.IsAdopted)
            {
                session = null;
                return adopted;
            }
            current.Dispose();
            try
            {
                session = new CharacterPoseNativeRoleSession(role);
            }
            catch
            {
                role.Dispose();
                session = null;
                throw;
            }
            return adopted;
        }
    }
}
