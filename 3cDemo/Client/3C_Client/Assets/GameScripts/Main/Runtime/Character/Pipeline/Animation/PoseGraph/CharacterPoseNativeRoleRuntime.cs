using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeRoleDependencies : IDisposable
    {
        internal CharacterPoseNativeRoleDependencies(
            CharacterPoseSourceModule source,
            CharacterPoseConstraintRuntime constraints,
            ICharacterPoseNativeNodeHandlerFactory handlerFactory,
            CharacterFinalPoseNativePublication publication)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Constraints = constraints ??
                throw new ArgumentNullException(nameof(constraints));
            HandlerFactory = handlerFactory ??
                throw new ArgumentNullException(nameof(handlerFactory));
            Publication = publication ??
                throw new ArgumentNullException(nameof(publication));
        }

        internal CharacterPoseSourceModule Source { get; }
        internal CharacterPoseConstraintRuntime Constraints { get; }
        internal ICharacterPoseNativeNodeHandlerFactory HandlerFactory { get; }
        internal CharacterFinalPoseNativePublication Publication { get; }

        public void Dispose()
        {
            Exception failure = null;
            try
            {
                Publication.Dispose();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            try
            {
                Constraints.Dispose();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
            try
            {
                Source.Dispose();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
            if (failure != null)
                throw failure;
        }
    }

    internal sealed class CharacterPoseNativeRoleRuntime : IDisposable
    {
        readonly CharacterPoseNativeGraphRuntime m_Graph;
        readonly CharacterFinalPoseNativePublication m_Publication;
        readonly CharacterPoseConstraintRuntime m_Constraints;
        readonly CharacterPoseSourceModule m_Source;
        CharacterPoseSourceFrameLease m_SourceLease;
        CharacterPoseConstraintFrameLease m_ConstraintsLease;
        bool m_Disposed;

        CharacterPoseNativeRoleRuntime(
            CharacterPoseNativeGraphRuntime graph,
            CharacterFinalPoseNativePublication publication,
            CharacterPoseConstraintRuntime constraints = null,
            CharacterPoseSourceModule source = null)
        {
            m_Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            m_Publication = publication ??
                throw new ArgumentNullException(nameof(publication));
            m_Constraints = constraints;
            m_Source = source;
        }

        internal CharacterPoseNativeGraphRuntime Graph => m_Graph;
        internal CharacterFinalPoseNativePublication Publication => m_Publication;
        internal ulong InstanceId => m_Graph.InstanceId;
        internal ulong ResetGeneration => m_Graph.ResetGeneration;
        internal PoseGraphId GraphId => m_Graph.PreparedBinding.GraphId;
        internal string GraphRevision => m_Graph.PreparedBinding.GraphRevision;
        internal string ResourceRevision => m_Graph.PreparedBinding.ResourceRevision;
        internal bool IsStarted => m_Graph.IsStarted;
        internal bool HasOpenFrame => m_Graph.HasOpenFrame;
        internal CharacterPoseNativeFrameLineage CurrentLineage => m_Graph.CurrentLineage;
        internal CharacterPoseNativeFrameInput CurrentInput => m_Graph.CurrentInput;
        internal CharacterPoseNativeFrameCoordinator CreateFrameCoordinator() =>
            new CharacterPoseNativeFrameCoordinator(this);
        internal bool TryGetCommittedPose(
            out ComposedAnimationPoseFrame frame) =>
            m_Publication.TryGetCommittedFrame(out frame);
        internal void ResetPublicationToDefaults() =>
            m_Publication.ResetToDefaults();
        internal ThirdPersonCharacter.Pipeline.Animation.Diagnostics.CharacterFootIkPhysicalCapture
            CommittedPhysicalCapture => m_Publication.CommittedPhysicalCapture;
        internal void RestoreInitialPublication() =>
            m_Publication.RestoreInitialAndInvalidate();

        internal static CharacterPoseNativeGraphPrepareResult Prepare(
            ulong requestId,
            ActorId actorId,
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationRigPayload rig,
            CharacterAnimationInputContract inputContract,
            string resourceRevision)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            CharacterPresentationPoseGraphAsset graphAsset = profile.PoseGraph;
            CharacterPoseCanvasGraph graph = graphAsset ? graphAsset.Graph : null;
            var request = new CharacterPoseNativeGraphPrepareRequest(
                requestId,
                actorId,
                graphAsset,
                graph,
                profile,
                rig,
                inputContract,
                resourceRevision,
                CharacterPoseNativeGraphBoundary.Root);
            return CharacterPoseNativeGraphRuntime.Prepare(in request);
        }

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
            ICharacterPoseNativeNodeHandlerFactory handlerFactory,
            CharacterFinalPoseNativePublication publication,
            out CharacterPoseNativeRoleRuntime runtime)
        {
            CharacterPoseNativeGraphPrepareResult preparation = Prepare(
                requestId,
                actorId,
                profile,
                rig,
                inputContract,
                resourceRevision);
            return Create(
                in preparation,
                in context,
                instanceId,
                resetGeneration,
                reason,
                handlerFactory,
                publication,
                out runtime);
        }

        internal static CharacterPoseNativeAdoptedResult Create(
            in CharacterPoseNativeGraphPrepareResult preparation,
            in CharacterPoseNativeInstanceContext context,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            CharacterPoseNativeRoleDependencies dependencies,
            out CharacterPoseNativeRoleRuntime runtime)
        {
            if (dependencies == null)
                throw new ArgumentNullException(nameof(dependencies));
            CharacterPoseNativePreparedBinding preparedBinding =
                preparation.PreparedBinding;
            CharacterPoseNativeGraphRuntime graph = null;
            CharacterPoseNativeAdoptedResult adopted =
                CharacterPoseNativeGraphRuntime.Create(
                    in preparedBinding,
                    in context,
                    instanceId,
                    resetGeneration,
                    reason,
                    dependencies.HandlerFactory,
                    out graph);
            if (!adopted.IsAdopted)
            {
                dependencies.Dispose();
                runtime = null;
                return adopted;
            }
            runtime = new CharacterPoseNativeRoleRuntime(
                graph,
                dependencies.Publication,
                dependencies.Constraints,
                dependencies.Source);
            return adopted;
        }

        internal static CharacterPoseNativeAdoptedResult Create(
            in CharacterPoseNativeGraphPrepareResult preparation,
            in CharacterPoseNativeInstanceContext context,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            ICharacterPoseNativeNodeHandlerFactory handlerFactory,
            CharacterFinalPoseNativePublication publication,
            out CharacterPoseNativeRoleRuntime runtime)
        {
            if (!preparation.IsReady)
            {
                publication?.Dispose();
                runtime = null;
                return CharacterPoseNativeAdoptedResult.Failed(
                    in preparation,
                    resetGeneration,
                    preparation.FailureCode,
                    preparation.Message);
            }
            if (publication == null)
                throw new ArgumentNullException(nameof(publication));
            CharacterPoseNativeGraphRuntime graph = null;
            try
            {
                CharacterPoseNativePreparedBinding preparedBinding =
                    preparation.PreparedBinding;
                CharacterPoseNativeAdoptedResult adopted =
                    CharacterPoseNativeGraphRuntime.Create(
                        in preparedBinding,
                        in context,
                        instanceId,
                        resetGeneration,
                        reason,
                        handlerFactory,
                        out graph);
                if (!adopted.IsAdopted)
                {
                    publication.Dispose();
                    runtime = null;
                    return adopted;
                }
                runtime = new CharacterPoseNativeRoleRuntime(graph, publication);
                return adopted;
            }
            catch
            {
                graph?.Dispose();
                publication.Dispose();
                runtime = null;
                throw;
            }
        }

        internal CharacterPoseNativeFrameLease BeginFrame(
            in CharacterPoseNativeFrameInput input)
        {
            CharacterPoseNativeFrameLease lease = m_Graph.BeginFrame(in input);
            if (m_Source == null)
                return lease;
            CharacterPoseNativeFrameLineage openLineage = lease.Lineage;
            try
            {
                if (m_Constraints != null)
                    m_ConstraintsLease = m_Constraints.BeginFrame(in openLineage);
                if (m_Source != null)
                    m_SourceLease = m_Source.BeginFrame(in openLineage);
                return lease;
            }
            catch
            {
                Discard(lease, CharacterPoseNativeFailureCode.FrameInvalid);
                throw;
            }
        }

        internal CharacterPoseNativePreparationResult PrepareFrame(
            CharacterPoseNativeFrameLease lease) =>
            m_Graph.PrepareFrame(lease);

        internal void EvaluateAnimationGraph() =>
            m_Graph.InstanceContext.Animancer.Evaluate(
                m_Graph.CurrentInput.DeltaSeconds);

        internal void BindGraphInput(
            PosePortId portId,
            CharacterPoseNativePortValue value) =>
            m_Graph.BindGraphInput(portId, value);

        internal void PrepareEvaluation(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            m_Graph.PrepareEvaluation(
                lease,
                in demand,
                barrierIdentity);
            m_Source?.EnterEvaluateBarrier(m_SourceLease);
        }

        internal CharacterPoseNativeEvaluationResult Evaluate(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity) =>
            m_Graph.Evaluate(
                lease,
                in demand,
                barrierIdentity);

        internal CharacterPoseNativeValidationResult ValidatePending(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeEvaluationResult evaluation)
        {
            CharacterPoseNativeValidationResult validation =
                m_Graph.ValidatePending(lease, in evaluation);
            if (!validation.IsValidated || m_Constraints == null)
                return validation;
            CharacterPoseNativeFrameLineage lineage = evaluation.Lineage;
            try
            {
                CharacterPoseConstraintResult constraints = m_Constraints.CompleteFrame(
                    m_ConstraintsLease,
                    in lineage,
                    AnimationPoseAvailability.Pose,
                    AnimationPoseNativeInvalidReason.None,
                    AnimationPoseNativeInvalidReason.None);
                if (!constraints.IsCompleted)
                    throw new InvalidOperationException(
                        $"Pose Constraint completion failed: {constraints.InvalidReason}.");
                return validation;
            }
            catch (Exception exception)
            {
                return CharacterPoseNativeValidationResult.Failed(
                    in lineage,
                    CharacterPoseNativeFailureCode.FrameInvalid,
                    "Pose/Constraint",
                    exception.Message);
            }
        }

        internal CharacterPoseNativePublicationResult Commit(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeEvaluationResult evaluation,
            bool captureFootIkDiagnostics)
        {
            CharacterPoseNativePublicationResult result = m_Graph.Commit(
                lease,
                in evaluation,
                m_Publication,
                captureFootIkDiagnostics);
            if (result.Status != CharacterPoseNativeFrameStatus.Committed)
            {
                DiscardPendingModules();
                return result;
            }
            if (m_Constraints != null)
            {
                m_Constraints.SealFrame(m_ConstraintsLease);
                m_ConstraintsLease = default;
            }
            if (m_Source != null)
            {
                m_Source.CommitFrame(m_SourceLease);
                m_SourceLease = default;
            }
            return result;
        }

        internal void Discard(
            CharacterPoseNativeFrameLease lease,
            CharacterPoseNativeFailureCode reason)
        {
            try
            {
                if (m_Graph.HasOpenFrame)
                    m_Graph.Discard(lease, reason);
            }
            finally
            {
                DiscardPendingModules();
            }
        }

        void DiscardPendingModules()
        {
            CharacterPoseConstraintFrameLease constraintsLease = m_ConstraintsLease;
            CharacterPoseSourceFrameLease sourceLease = m_SourceLease;
            m_ConstraintsLease = default;
            m_SourceLease = default;
            Exception failure = null;
            try
            {
                if (constraintsLease.IsValid)
                    m_Constraints.DiscardFrame(constraintsLease);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            try
            {
                if (sourceLease.IsValid)
                    m_Source.DiscardFrame(sourceLease);
            }
            catch (Exception exception)
            {
                failure = failure == null ? exception : new AggregateException(failure, exception);
            }
            if (failure != null)
                throw failure;
        }

        internal CharacterPoseNativeResetResult Reset(ulong resetGeneration)
        {
            CharacterPoseNativeResetResult result = m_Graph.ResetInstance(resetGeneration);
            if (result.IsReset)
                m_Publication.ResetToDefaults();
            return result;
        }

        internal void Stop()
        {
            try
            {
                m_Graph.StopInstance();
            }
            finally
            {
                DiscardPendingModules();
            }
        }

#if UNITY_EDITOR || KK_DIAGNOSTIC_SAMPLING
        internal bool TryObserve(
            PoseNodeId nodeId,
            PosePortId portId,
            out CharacterPoseNativeNodeObservation observation) =>
            m_Graph.TryObserve(nodeId, portId, out observation);
#endif

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Exception failure = null;
            try
            {
                m_Graph.Dispose();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            try
            {
                m_Publication.Dispose();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
            try
            {
                m_Constraints?.Dispose();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
            try
            {
                m_Source?.Dispose();
            }
            catch (Exception exception)
            {
                failure = failure == null
                    ? exception
                    : new AggregateException(failure, exception);
            }
            if (failure != null)
                throw failure;
        }
    }
}
