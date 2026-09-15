using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeRoleRuntime : IDisposable
    {
        readonly CharacterPoseNativeGraphRuntime m_Graph;
        readonly CharacterFinalPoseNativePublication m_Publication;
        bool m_Disposed;

        CharacterPoseNativeRoleRuntime(
            CharacterPoseNativeGraphRuntime graph,
            CharacterFinalPoseNativePublication publication)
        {
            m_Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            m_Publication = publication ??
                throw new ArgumentNullException(nameof(publication));
        }

        internal CharacterPoseNativeGraphRuntime Graph => m_Graph;
        internal CharacterFinalPoseNativePublication Publication => m_Publication;
        internal ulong InstanceId => m_Graph.InstanceId;
        internal ulong ResetGeneration => m_Graph.ResetGeneration;
        internal bool IsStarted => m_Graph.IsStarted;

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
                resourceRevision);
            return CharacterPoseNativeGraphRuntime.Prepare(in request);
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
                CharacterPoseNativeAdoptedResult adopted =
                    CharacterPoseNativeGraphRuntime.Create(
                        in preparation.PreparedBinding,
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

        internal static CharacterPoseNativeAdoptedResult Replace(
            CharacterPoseNativeRoleRuntime current,
            in CharacterPoseNativeGraphPrepareResult preparation,
            in CharacterPoseNativeInstanceContext context,
            ulong instanceId,
            ulong resetGeneration,
            string reason,
            ICharacterPoseNativeNodeHandlerFactory handlerFactory,
            CharacterFinalPoseNativePublication publication,
            out CharacterPoseNativeRoleRuntime runtime)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));
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
                CharacterPoseNativeAdoptedResult adopted =
                    CharacterPoseNativeGraphRuntime.Replace(
                        current.m_Graph,
                        in preparation.PreparedBinding,
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
                current.Dispose();
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
            in CharacterPoseNativeFrameInput input) =>
            m_Graph.BeginFrame(in input);

        internal CharacterPoseNativePreparationResult PrepareFrame(
            CharacterPoseNativeFrameLease lease) =>
            m_Graph.PrepareFrame(lease);

        internal void BindGraphInput(
            PosePortId portId,
            CharacterPoseNativePortValue value) =>
            m_Graph.BindGraphInput(portId, value);

        internal void PrepareEvaluation(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity) =>
            m_Graph.PrepareEvaluation(
                lease,
                in demand,
                barrierIdentity);

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
            in CharacterPoseNativeEvaluationResult evaluation) =>
            m_Graph.ValidatePending(lease, in evaluation);

        internal CharacterPoseNativePublicationResult Commit(
            CharacterPoseNativeFrameLease lease,
            in CharacterPoseNativeEvaluationResult evaluation,
            bool captureFootIkDiagnostics) =>
            m_Graph.Commit(
                lease,
                in evaluation,
                m_Publication,
                captureFootIkDiagnostics);

        internal void Discard(
            CharacterPoseNativeFrameLease lease,
            CharacterPoseNativeFailureCode reason) =>
            m_Graph.Discard(lease, reason);

        internal CharacterPoseNativeResetResult Reset(ulong resetGeneration) =>
            m_Graph.ResetInstance(resetGeneration);

        internal void Stop() => m_Graph.StopInstance();

        internal bool TryObserve(
            PoseNodeId nodeId,
            PosePortId portId,
            out CharacterPoseNativeNodeObservation observation) =>
            m_Graph.TryObserve(nodeId, portId, out observation);

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Exception failure = null;
            try
            {
                m_Publication.Dispose();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            try
            {
                m_Graph.Dispose();
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
