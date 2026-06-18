using System;
using System.Collections.Generic;

namespace ThirdPersonCharacterBehavior
{
    public sealed class CharacterBehaviorSubmissionSet
    {
        readonly List<BehaviorRequestSubmission> requests = new List<BehaviorRequestSubmission>();
        readonly List<BehaviorOutputSubmission> outputs = new List<BehaviorOutputSubmission>();
        readonly List<BehaviorCueSubmission> cues = new List<BehaviorCueSubmission>();
        readonly List<BehaviorMotionChannelSubmission> motionChannels = new List<BehaviorMotionChannelSubmission>();
        readonly List<BehaviorAnimationChannelSubmission> animationChannels = new List<BehaviorAnimationChannelSubmission>();
        readonly List<BehaviorWindowFactsChannelSubmission> windowFactsChannels = new List<BehaviorWindowFactsChannelSubmission>();
        readonly List<BehaviorClaimSubmission> claims = new List<BehaviorClaimSubmission>();
        readonly List<BehaviorDiagnosticSubmission> diagnostics = new List<BehaviorDiagnosticSubmission>();
        readonly List<BehaviorStateWriteSubmission> stateWrites = new List<BehaviorStateWriteSubmission>();

        public IReadOnlyList<BehaviorRequestSubmission> Requests => requests;
        public IReadOnlyList<BehaviorOutputSubmission> Outputs => outputs;
        public IReadOnlyList<BehaviorCueSubmission> Cues => cues;
        public IReadOnlyList<BehaviorMotionChannelSubmission> MotionChannels => motionChannels;
        public IReadOnlyList<BehaviorAnimationChannelSubmission> AnimationChannels => animationChannels;
        public IReadOnlyList<BehaviorWindowFactsChannelSubmission> WindowFactsChannels => windowFactsChannels;
        public IReadOnlyList<BehaviorClaimSubmission> Claims => claims;
        public IReadOnlyList<BehaviorDiagnosticSubmission> Diagnostics => diagnostics;
        public IReadOnlyList<BehaviorStateWriteSubmission> StateWrites => stateWrites;
        public bool IsEmpty =>
            requests.Count == 0 &&
            outputs.Count == 0 &&
            cues.Count == 0 &&
            motionChannels.Count == 0 &&
            animationChannels.Count == 0 &&
            windowFactsChannels.Count == 0 &&
            claims.Count == 0 &&
            diagnostics.Count == 0 &&
            stateWrites.Count == 0;

        public void Add(in BehaviorRequestSubmission submission)
        {
            if (submission.HasRequest)
                AddSorted(requests, submission, submission.Source);
        }

        public void Add(in BehaviorOutputSubmission submission)
        {
            if (submission.HasOutput || submission.Required)
                AddSorted(outputs, submission, submission.Source);
        }

        public void Add(in BehaviorCueSubmission submission)
        {
            if (submission.HasCue)
                AddSorted(cues, submission, submission.Source);
        }

        public void Add(in BehaviorMotionChannelSubmission submission)
        {
            if (submission.HasMotion)
                AddSorted(motionChannels, submission, submission.Source);
        }

        public void Add(in BehaviorAnimationChannelSubmission submission)
        {
            if (submission.HasAnimation)
                AddSorted(animationChannels, submission, submission.Source);
        }

        public void Add(in BehaviorWindowFactsChannelSubmission submission)
        {
            if (submission.HasFacts)
                AddSorted(windowFactsChannels, submission, submission.Source);
        }

        public void Add(in BehaviorClaimSubmission submission)
        {
            if (submission.HasClaim)
                AddSorted(claims, submission, submission.Source);
        }

        public void Add(in BehaviorDiagnosticSubmission submission)
        {
            if (submission.HasDiagnostic)
                AddSorted(diagnostics, submission, submission.Source);
        }

        public void Add(in BehaviorStateWriteSubmission submission)
        {
            if (submission.HasWrite)
                AddSorted(stateWrites, submission, submission.Source);
        }

        public CharacterBehaviorSubmissionSet QueryByPass(CharacterBehaviorEvaluationPass pass)
        {
            CharacterBehaviorSubmissionSet result = new CharacterBehaviorSubmissionSet();
            for (int i = 0; i < requests.Count; i++)
                if (requests[i].Pass == pass)
                    result.Add(requests[i]);
            for (int i = 0; i < outputs.Count; i++)
                if (outputs[i].Pass == pass)
                    result.Add(outputs[i]);
            for (int i = 0; i < cues.Count; i++)
                if (cues[i].Pass == pass)
                    result.Add(cues[i]);
            for (int i = 0; i < motionChannels.Count; i++)
                if (motionChannels[i].Pass == pass)
                    result.Add(motionChannels[i]);
            for (int i = 0; i < animationChannels.Count; i++)
                if (animationChannels[i].Pass == pass)
                    result.Add(animationChannels[i]);
            for (int i = 0; i < windowFactsChannels.Count; i++)
                if (windowFactsChannels[i].Pass == pass)
                    result.Add(windowFactsChannels[i]);
            for (int i = 0; i < claims.Count; i++)
                if (claims[i].Pass == pass)
                    result.Add(claims[i]);
            for (int i = 0; i < diagnostics.Count; i++)
                if (diagnostics[i].Pass == pass)
                    result.Add(diagnostics[i]);
            for (int i = 0; i < stateWrites.Count; i++)
                if (stateWrites[i].Pass == pass)
                    result.Add(stateWrites[i]);
            return result;
        }

        public CharacterBehaviorSubmissionSet QueryBySource(CharacterBehaviorSourceId sourceId)
        {
            CharacterBehaviorSubmissionSet result = new CharacterBehaviorSubmissionSet();
            for (int i = 0; i < requests.Count; i++)
                if (requests[i].Source.NodeId == sourceId)
                    result.Add(requests[i]);
            for (int i = 0; i < outputs.Count; i++)
                if (outputs[i].Source.NodeId == sourceId)
                    result.Add(outputs[i]);
            for (int i = 0; i < cues.Count; i++)
                if (cues[i].Source.NodeId == sourceId)
                    result.Add(cues[i]);
            for (int i = 0; i < motionChannels.Count; i++)
                if (motionChannels[i].Source.NodeId == sourceId)
                    result.Add(motionChannels[i]);
            for (int i = 0; i < animationChannels.Count; i++)
                if (animationChannels[i].Source.NodeId == sourceId)
                    result.Add(animationChannels[i]);
            for (int i = 0; i < windowFactsChannels.Count; i++)
                if (windowFactsChannels[i].Source.NodeId == sourceId)
                    result.Add(windowFactsChannels[i]);
            for (int i = 0; i < claims.Count; i++)
                if (claims[i].Source.NodeId == sourceId)
                    result.Add(claims[i]);
            for (int i = 0; i < diagnostics.Count; i++)
                if (diagnostics[i].Source.NodeId == sourceId)
                    result.Add(diagnostics[i]);
            for (int i = 0; i < stateWrites.Count; i++)
                if (stateWrites[i].Source.NodeId == sourceId)
                    result.Add(stateWrites[i]);
            return result;
        }

        static void AddSorted<T>(List<T> list, T item, CharacterBehaviorSubmissionSource source)
        {
            int index = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                CharacterBehaviorSubmissionSource existing = ResolveSource(list[i]);
                if (source.CompareTo(existing) < 0)
                {
                    index = i;
                    break;
                }
            }

            list.Insert(index, item);
        }

        static CharacterBehaviorSubmissionSource ResolveSource<T>(T item)
        {
            if (item is BehaviorRequestSubmission request)
                return request.Source;
            if (item is BehaviorOutputSubmission output)
                return output.Source;
            if (item is BehaviorCueSubmission cue)
                return cue.Source;
            if (item is BehaviorMotionChannelSubmission motion)
                return motion.Source;
            if (item is BehaviorAnimationChannelSubmission animation)
                return animation.Source;
            if (item is BehaviorWindowFactsChannelSubmission windowFacts)
                return windowFacts.Source;
            if (item is BehaviorClaimSubmission claim)
                return claim.Source;
            if (item is BehaviorDiagnosticSubmission diagnostic)
                return diagnostic.Source;
            if (item is BehaviorStateWriteSubmission stateWrite)
                return stateWrite.Source;

            return default;
        }

        public static CharacterBehaviorSubmissionSet Empty => new CharacterBehaviorSubmissionSet();
    }

    public static class CharacterBehaviorSubmissionRules
    {
        public static CharacterBehaviorSubmissionConsumer AllowedConsumers(CharacterBehaviorSubmissionKind kind)
        {
            switch (kind)
            {
                case CharacterBehaviorSubmissionKind.Request:
                    return CharacterBehaviorSubmissionConsumer.RequestArbiter |
                           CharacterBehaviorSubmissionConsumer.ActionRequestContext |
                           CharacterBehaviorSubmissionConsumer.FrameContextWriter |
                           CharacterBehaviorSubmissionConsumer.Diagnostics;
                case CharacterBehaviorSubmissionKind.Output:
                    return CharacterBehaviorSubmissionConsumer.BehaviorSubmissionComposer |
                           CharacterBehaviorSubmissionConsumer.FramePlanInput |
                           CharacterBehaviorSubmissionConsumer.Diagnostics;
                case CharacterBehaviorSubmissionKind.Cue:
                    return CharacterBehaviorSubmissionConsumer.CueQueue |
                           CharacterBehaviorSubmissionConsumer.BehaviorSubmissionComposer |
                           CharacterBehaviorSubmissionConsumer.Diagnostics;
                case CharacterBehaviorSubmissionKind.MotionChannel:
                case CharacterBehaviorSubmissionKind.AnimationChannel:
                case CharacterBehaviorSubmissionKind.WindowFactsChannel:
                case CharacterBehaviorSubmissionKind.Claim:
                    return CharacterBehaviorSubmissionConsumer.BehaviorSubmissionComposer |
                           CharacterBehaviorSubmissionConsumer.FramePlanInput |
                           CharacterBehaviorSubmissionConsumer.Diagnostics;
                case CharacterBehaviorSubmissionKind.Diagnostic:
                    return CharacterBehaviorSubmissionConsumer.Diagnostics;
                case CharacterBehaviorSubmissionKind.StateWrite:
                    return CharacterBehaviorSubmissionConsumer.StateOwner |
                           CharacterBehaviorSubmissionConsumer.FrameContextWriter |
                           CharacterBehaviorSubmissionConsumer.Diagnostics;
                default:
                    return CharacterBehaviorSubmissionConsumer.None;
            }
        }

        public static bool CanConsume(CharacterBehaviorSubmissionKind kind, CharacterBehaviorSubmissionConsumer consumer)
        {
            return (AllowedConsumers(kind) & consumer) != 0;
        }

        public static bool RequiresConsumption(CharacterBehaviorSubmissionKind kind)
        {
            return kind == CharacterBehaviorSubmissionKind.Request ||
                   kind == CharacterBehaviorSubmissionKind.Output ||
                   kind == CharacterBehaviorSubmissionKind.MotionChannel ||
                   kind == CharacterBehaviorSubmissionKind.AnimationChannel ||
                   kind == CharacterBehaviorSubmissionKind.WindowFactsChannel ||
                   kind == CharacterBehaviorSubmissionKind.Claim ||
                   kind == CharacterBehaviorSubmissionKind.StateWrite;
        }

        public static bool IsPayloadAllowedForPass(
            CharacterBehaviorEvaluationPass pass,
            CharacterBehaviorSubmissionKind kind)
        {
            if (pass == CharacterBehaviorEvaluationPass.RequestPass)
            {
                return kind == CharacterBehaviorSubmissionKind.Request ||
                       kind == CharacterBehaviorSubmissionKind.Diagnostic ||
                       kind == CharacterBehaviorSubmissionKind.StateWrite;
            }

            if (pass == CharacterBehaviorEvaluationPass.OutputPass)
            {
                return kind == CharacterBehaviorSubmissionKind.Output ||
                       kind == CharacterBehaviorSubmissionKind.Cue ||
                       kind == CharacterBehaviorSubmissionKind.MotionChannel ||
                       kind == CharacterBehaviorSubmissionKind.AnimationChannel ||
                       kind == CharacterBehaviorSubmissionKind.WindowFactsChannel ||
                       kind == CharacterBehaviorSubmissionKind.Claim ||
                       kind == CharacterBehaviorSubmissionKind.Diagnostic ||
                       kind == CharacterBehaviorSubmissionKind.StateWrite;
            }

            return kind == CharacterBehaviorSubmissionKind.Diagnostic;
        }
    }

    public sealed class CharacterBehaviorSubmissionAudit
    {
        readonly List<string> diagnostics = new List<string>();

        public IReadOnlyList<string> Diagnostics => diagnostics;
        public bool HasDiagnostics => diagnostics.Count > 0;

        public void RequireAllowedConsumer(CharacterBehaviorSubmissionKind kind, CharacterBehaviorSubmissionConsumer consumer)
        {
            if (!CharacterBehaviorSubmissionRules.CanConsume(kind, consumer))
                diagnostics.Add($"consumer-not-allowed:{kind}:{consumer}");
        }

        public void RequireConsumed(CharacterBehaviorSubmissionKind kind, bool consumed)
        {
            if (!consumed && CharacterBehaviorSubmissionRules.RequiresConsumption(kind))
                diagnostics.Add($"submission-unconsumed:{kind}");
        }

        public void RequirePassBoundary(CharacterBehaviorEvaluationPass pass, CharacterBehaviorSubmissionKind kind)
        {
            if (!CharacterBehaviorSubmissionRules.IsPayloadAllowedForPass(pass, kind))
                diagnostics.Add($"pass-payload-not-allowed:{pass}:{kind}");
        }
    }
}
