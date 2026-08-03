using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Animation.TransitionRouting;

namespace ThirdPersonCharacter.Editor.Animation.TransitionRouting
{
    public readonly struct AnimationTransitionRoutingFixtureFrameRecord
    {
        public AnimationTransitionRoutingFixtureFrameRecord(
            int sequenceIndex,
            TransitionRoutingFrameInput input,
            TransitionRoutingFrameOutput output,
            TransitionRoutingRuntimeSnapshot snapshot)
        {
            SequenceIndex = sequenceIndex;
            Input = input;
            Output = output;
            Snapshot = snapshot;
        }

        public int SequenceIndex { get; }
        public TransitionRoutingFrameInput Input { get; }
        public TransitionRoutingFrameOutput Output { get; }
        public TransitionRoutingRuntimeSnapshot Snapshot { get; }
    }

    public sealed class AnimationTransitionRoutingFixtureSession
    {
        readonly List<AnimationTransitionRoutingFixtureFrameRecord> m_Records = new List<AnimationTransitionRoutingFixtureFrameRecord>();
        AnimationTransitionRoutingFixtureAsset m_Asset;
        TransitionRoutingCompileResult m_CompileResult;
        TransitionRoutingWorkspace m_Workspace;
        int m_NextFrameIndex;
        int m_RecordCapacity = 128;

        public AnimationTransitionRoutingFixtureAsset Asset => m_Asset;
        public TransitionRoutingCompileResult CompileResult => m_CompileResult;
        public CompiledTransitionRoutingPlan Plan => m_CompileResult != null && m_CompileResult.Succeeded
            ? m_CompileResult.Plan
            : null;
        public bool HasCompiledPlan => Plan != null;
        public int NextFrameIndex => m_NextFrameIndex;
        public IReadOnlyList<AnimationTransitionRoutingFixtureFrameRecord> Records => m_Records;
        public TransitionRoutingWorkspace Workspace => m_Workspace;
        public TransitionRoutingRuntimeSnapshot Snapshot => m_Workspace == null
            ? default
            : m_Workspace.Snapshot;

        public void SetAsset(AnimationTransitionRoutingFixtureAsset asset)
        {
            m_Asset = asset;
            m_CompileResult = null;
            m_Workspace = null;
            m_NextFrameIndex = 0;
            m_Records.Clear();
        }

        public void Compile()
        {
            if (m_Asset == null)
                return;

            var endpoints = new TransitionEndpointId[m_Asset.Endpoints.Count];
            for (int i = 0; i < endpoints.Length; i++)
            {
                AnimationTransitionRoutingFixtureEndpoint row = m_Asset.Endpoints[i];
                endpoints[i] = new TransitionEndpointId(row == null ? string.Empty : row.EndpointId);
            }

            var rules = new AnimationTransitionRule[m_Asset.Rules.Count];
            for (int i = 0; i < rules.Length; i++)
            {
                AnimationTransitionRoutingFixtureRule row = m_Asset.Rules[i];
                if (row == null)
                {
                    rules[i] = default;
                    continue;
                }

                rules[i] = new AnimationTransitionRule(
                    new TransitionRuleId(row.RuleId),
                    new TransitionEndpointId(row.SourceEndpointId),
                    new TransitionEndpointId(row.TargetEndpointId),
                    row.BlendLogic,
                    row.DurationSeconds,
                    new TransitionBlendCurveId(row.BlendCurveId),
                    new TransitionBlendProfileId(row.BlendProfileId));
            }

            var definition = new TransitionRoutingDefinition(
                m_Asset.SchemaVersion,
                new TransitionDefinitionRevision(m_Asset.DefinitionRevision),
                TransitionRoutingCoveragePolicy.CompleteMatrix,
                endpoints,
                rules);
            m_CompileResult = TransitionRoutingCompiler.Compile(definition);
            m_RecordCapacity = Math.Max(1, m_Asset.EventCapacity);
            m_Workspace = m_CompileResult.Succeeded
                ? new TransitionRoutingWorkspace(m_RecordCapacity)
                : null;
            m_NextFrameIndex = 0;
            m_Records.Clear();
        }

        public void ResetRuntime()
        {
            if (!HasCompiledPlan)
                return;
            m_Workspace = new TransitionRoutingWorkspace(m_RecordCapacity);
            m_NextFrameIndex = 0;
            m_Records.Clear();
        }

        public bool StepNext()
        {
            if (!HasCompiledPlan || m_Asset == null || m_NextFrameIndex >= m_Asset.Frames.Count)
                return false;
            return Step(m_NextFrameIndex);
        }

        public void RunSequence()
        {
            if (!HasCompiledPlan || m_Asset == null)
                return;
            ResetRuntime();
            while (m_NextFrameIndex < m_Asset.Frames.Count)
            {
                if (!Step(m_NextFrameIndex))
                    break;
                if (m_Records.Count > 0 && m_Records[m_Records.Count - 1].Output.IsInvalid)
                    break;
            }
        }

        public void ClearTimeline()
        {
            m_Records.Clear();
            m_Workspace?.ClearEvents();
        }

        bool Step(int sequenceIndex)
        {
            AnimationTransitionRoutingFixtureFrame row = m_Asset.Frames[sequenceIndex];
            if (row == null)
                return false;

            TransitionRoutingRuntimeSnapshot before = m_Workspace.Snapshot;
            TransitionCompletionFact capture = CreateCompletion(
                row.CompleteCurrentCapture,
                row.CaptureSucceeded,
                before);
            TransitionCompletionFact release = CreateCompletion(
                row.CompleteCurrentRelease,
                row.ReleaseSucceeded,
                before);
            var input = new TransitionRoutingFrameInput(
                Plan.PlanId,
                new TransitionFrameId(row.FrameId > 0 ? (ulong)row.FrameId : 0),
                new TransitionRouteOwnerId(m_Asset.OwnerNodeId),
                new TransitionEndpointId(row.CurrentEndpointId),
                new TransitionEndpointId(row.RequestedEndpointId),
                new TransitionSelectionGeneration(row.SelectionGeneration > 0 ? (ulong)row.SelectionGeneration : 0),
                row.TargetReady,
                row.CapturePlanReady,
                capture,
                release,
                row.ResetReason);
            TransitionRoutingFrameOutput output = TransitionRoutingRuntime.Step(Plan, m_Workspace, input);
            AddRecord(new AnimationTransitionRoutingFixtureFrameRecord(
                sequenceIndex,
                input,
                output,
                m_Workspace.Snapshot));
            m_NextFrameIndex = sequenceIndex + 1;
            return true;
        }

        void AddRecord(AnimationTransitionRoutingFixtureFrameRecord record)
        {
            if (m_Records.Count == m_RecordCapacity)
                m_Records.RemoveAt(0);
            m_Records.Add(record);
        }

        static TransitionCompletionFact CreateCompletion(
            bool requested,
            bool succeeded,
            in TransitionRoutingRuntimeSnapshot snapshot)
        {
            if (!requested)
                return TransitionCompletionFact.None;
            if (!snapshot.HasActiveRequest)
                return new TransitionCompletionFact(true, default, default, succeeded);
            return new TransitionCompletionFact(
                true,
                snapshot.ActiveRequest.RequestEventId,
                snapshot.ActiveRequest.RequestGeneration,
                succeeded);
        }
    }
}
