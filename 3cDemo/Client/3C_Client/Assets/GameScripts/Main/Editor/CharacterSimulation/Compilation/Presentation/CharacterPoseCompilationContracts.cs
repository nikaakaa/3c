using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal enum CharacterPoseCompilationPass : byte
    {
        Request = 1,
        GraphClosure = 2,
        TypedLowering = 3,
        Topology = 4,
        SymbolicFamilyLowering = 5,
        StageSchedule = 6,
        ValueLifetime = 7,
        WorkspacePlan = 8,
        BindFamilyPayload = 9,
        SealProgramImage = 10
    }

    internal enum CharacterPoseCompilationDiagnosticSeverity : byte
    {
        Error = 1,
        Warning = 2
    }

    internal sealed class CharacterPoseCompilationDiagnostic
    {
        public CharacterPoseCompilationDiagnostic(
            CharacterPoseCompilationPass pass,
            CharacterPoseCompilationDiagnosticSeverity severity,
            string reason,
            string message,
            PoseGraphId graphId = default,
            PoseNodeId nodeId = default,
            PosePortId portId = default,
            string callSite = "",
            string sourcePath = "",
            IReadOnlyList<string> relatedIdentities = null)
        {
            if (!Enum.IsDefined(typeof(CharacterPoseCompilationPass), pass))
                throw new ArgumentOutOfRangeException(nameof(pass));
            if (!Enum.IsDefined(typeof(CharacterPoseCompilationDiagnosticSeverity), severity))
                throw new ArgumentOutOfRangeException(nameof(severity));
            Pass = pass;
            Severity = severity;
            Reason = PoseIdentity.Require(reason, nameof(reason));
            Message = PoseIdentity.Require(message, nameof(message));
            GraphId = graphId;
            NodeId = nodeId;
            PortId = portId;
            CallSite = callSite ?? string.Empty;
            SourcePath = sourcePath ?? string.Empty;
            RelatedIdentities = (relatedIdentities ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        public CharacterPoseCompilationPass Pass { get; }
        public CharacterPoseCompilationDiagnosticSeverity Severity { get; }
        public string Reason { get; }
        public string Message { get; }
        public PoseGraphId GraphId { get; }
        public PoseNodeId NodeId { get; }
        public PosePortId PortId { get; }
        public string CallSite { get; }
        public string SourcePath { get; }
        public IReadOnlyList<string> RelatedIdentities { get; }

        public string Format()
        {
            var location = new List<string>(4);
            if (GraphId.IsValid)
                location.Add($"graph={GraphId}");
            if (NodeId.IsValid)
                location.Add($"node={NodeId}");
            if (PortId.IsValid)
                location.Add($"port={PortId}");
            if (!string.IsNullOrEmpty(CallSite))
                location.Add($"call-site={CallSite}");
            string suffix = location.Count == 0
                ? string.Empty
                : $" [{string.Join(", ", location)}]";
            if (!string.IsNullOrEmpty(SourcePath))
                suffix += $" [{SourcePath}]";
            return $"Pose compiler {Pass}/{Reason}: {Message}{suffix}";
        }
    }

    internal sealed class CharacterPoseCompilationRequest
    {
        public CharacterPoseCompilationRequest(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationRigDefinition rig,
            IReadOnlyCollection<AnimationChannelId> reachableAnimationChannels,
            AnimationBlendNodePayload[] blendNodes,
            CharacterPresentationPoseSourcePlan[] poseSources,
            AnimationClipPhasePlan[] clipPhasePlans,
            AnimationSourcePhasePlan[] sourcePhasePlans,
            AnimationFootPhaseValidationDescriptor[] clipPhaseValidations,
            IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex> sourceIndices,
            IReadOnlyDictionary<string, int> curveIndices,
            IReadOnlyDictionary<string, int> profileIndicesByIdentity,
            CharacterAnimationPresentationProfile profile,
            CharacterLinkedPoseProjectionPayload linkedPose,
            CharacterFootPlacementAnalysisCompilation footAnalysis)
        {
            Asset = asset ? asset : throw new ArgumentNullException(nameof(asset));
            Rig = rig ? rig : throw new ArgumentNullException(nameof(rig));
            ReachableAnimationChannels = reachableAnimationChannels ??
                Array.Empty<AnimationChannelId>();
            BlendNodes = blendNodes ?? Array.Empty<AnimationBlendNodePayload>();
            PoseSources = poseSources ?? Array.Empty<CharacterPresentationPoseSourcePlan>();
            ClipPhasePlans = clipPhasePlans ?? Array.Empty<AnimationClipPhasePlan>();
            SourcePhasePlans = sourcePhasePlans ?? Array.Empty<AnimationSourcePhasePlan>();
            ClipPhaseValidations = clipPhaseValidations ??
                Array.Empty<AnimationFootPhaseValidationDescriptor>();
            SourceIndices = sourceIndices ??
                new Dictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex>();
            CurveIndices = curveIndices ?? throw new ArgumentNullException(nameof(curveIndices));
            ProfileIndicesByIdentity = profileIndicesByIdentity ??
                throw new ArgumentNullException(nameof(profileIndicesByIdentity));
            Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            LinkedPose = linkedPose ?? throw new ArgumentNullException(nameof(linkedPose));
            FootAnalysis = footAnalysis ?? throw new ArgumentNullException(nameof(footAnalysis));
        }

        public CharacterPresentationPoseGraphAsset Asset { get; }
        public CharacterAnimationRigDefinition Rig { get; }
        public IReadOnlyCollection<AnimationChannelId> ReachableAnimationChannels { get; }
        public AnimationBlendNodePayload[] BlendNodes { get; }
        public CharacterPresentationPoseSourcePlan[] PoseSources { get; }
        public AnimationClipPhasePlan[] ClipPhasePlans { get; }
        public AnimationSourcePhasePlan[] SourcePhasePlans { get; }
        public AnimationFootPhaseValidationDescriptor[] ClipPhaseValidations { get; }
        public IReadOnlyDictionary<CharacterPresentationPoseSourceSlot, PresentationPoseSourceIndex> SourceIndices { get; }
        public IReadOnlyDictionary<string, int> CurveIndices { get; }
        public IReadOnlyDictionary<string, int> ProfileIndicesByIdentity { get; }
        public CharacterAnimationPresentationProfile Profile { get; }
        public CharacterLinkedPoseProjectionPayload LinkedPose { get; }
        public CharacterFootPlacementAnalysisCompilation FootAnalysis { get; }
    }

    internal sealed class CharacterPoseCompilationResult
    {
        public CharacterPoseCompilationResult(
            CharacterPoseProgramImage programImage,
            IReadOnlyList<CharacterPoseCompilationDiagnostic> diagnostics)
        {
            ProgramImage = programImage;
            Diagnostics = diagnostics ?? Array.Empty<CharacterPoseCompilationDiagnostic>();
            IsSuccess = programImage != null &&
                        Diagnostics.All(value =>
                            value.Severity != CharacterPoseCompilationDiagnosticSeverity.Error);
            if (!IsSuccess && programImage != null)
                throw new ArgumentException("Failed Pose compilation cannot publish a Program Image.", nameof(programImage));
        }

        public bool IsSuccess { get; }
        public CharacterPoseProgramImage ProgramImage { get; }
        public IReadOnlyList<CharacterPoseCompilationDiagnostic> Diagnostics { get; }

        public void CopyMessagesTo(List<string> errors)
        {
            if (errors == null)
                return;
            for (int i = 0; i < Diagnostics.Count; i++)
            {
                CharacterPoseCompilationDiagnostic diagnostic = Diagnostics[i];
                if (diagnostic.Severity == CharacterPoseCompilationDiagnosticSeverity.Error)
                    errors.Add(diagnostic.Format());
            }
        }
    }
}
