using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Animation.TransitionRouting;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.Animation.TransitionRouting
{
    [Serializable]
    public sealed class AnimationTransitionRoutingFixtureEndpoint
    {
        [SerializeField] string m_EndpointId = "$source-pose";

        public string EndpointId => m_EndpointId ?? string.Empty;
    }

    [Serializable]
    public sealed class AnimationTransitionRoutingFixtureRule
    {
        [SerializeField] string m_RuleId = string.Empty;
        [SerializeField] string m_SourceEndpointId = string.Empty;
        [SerializeField] string m_TargetEndpointId = string.Empty;
        [SerializeField] AnimationTransitionBlendLogic m_BlendLogic = AnimationTransitionBlendLogic.StandardBlend;
        [SerializeField, Min(0f)] float m_DurationSeconds = 0f;
        [SerializeField] string m_BlendCurveId = string.Empty;
        [SerializeField] string m_BlendProfileId = string.Empty;

        public string RuleId => m_RuleId ?? string.Empty;
        public string SourceEndpointId => m_SourceEndpointId ?? string.Empty;
        public string TargetEndpointId => m_TargetEndpointId ?? string.Empty;
        public AnimationTransitionBlendLogic BlendLogic => m_BlendLogic;
        public float DurationSeconds => m_DurationSeconds;
        public string BlendCurveId => m_BlendCurveId ?? string.Empty;
        public string BlendProfileId => m_BlendProfileId ?? string.Empty;
    }

    [Serializable]
    public sealed class AnimationTransitionRoutingFixtureFrame
    {
        [SerializeField, Min(1)] long m_FrameId = 1;
        [SerializeField] string m_CurrentEndpointId = string.Empty;
        [SerializeField] string m_RequestedEndpointId = string.Empty;
        [SerializeField, Min(1)] long m_SelectionGeneration = 1;
        [SerializeField] bool m_TargetReady = true;
        [SerializeField] bool m_CapturePlanReady = true;
        [SerializeField] bool m_CompleteCurrentCapture = false;
        [SerializeField] bool m_CaptureSucceeded = true;
        [SerializeField] bool m_CompleteCurrentRelease = false;
        [SerializeField] bool m_ReleaseSucceeded = true;
        [SerializeField] TransitionRoutingResetReason m_ResetReason = TransitionRoutingResetReason.None;

        public long FrameId => m_FrameId;
        public string CurrentEndpointId => m_CurrentEndpointId ?? string.Empty;
        public string RequestedEndpointId => m_RequestedEndpointId ?? string.Empty;
        public long SelectionGeneration => m_SelectionGeneration;
        public bool TargetReady => m_TargetReady;
        public bool CapturePlanReady => m_CapturePlanReady;
        public bool CompleteCurrentCapture => m_CompleteCurrentCapture;
        public bool CaptureSucceeded => m_CaptureSucceeded;
        public bool CompleteCurrentRelease => m_CompleteCurrentRelease;
        public bool ReleaseSucceeded => m_ReleaseSucceeded;
        public TransitionRoutingResetReason ResetReason => m_ResetReason;
    }

    [CreateAssetMenu(
        fileName = "AnimationTransitionRoutingFixture",
        menuName = "3C/Animation/Transition Routing Fixture")]
    public sealed class AnimationTransitionRoutingFixtureAsset : ScriptableObject
    {
        [SerializeField] int m_SchemaVersion = TransitionRoutingCompiler.CurrentSchemaVersion;
        [SerializeField] string m_DefinitionRevision = "fixture-v1";
        [SerializeField] string m_OwnerNodeId = "fixture-owner";
        [SerializeField, Min(1)] int m_EventCapacity = 128;
        [SerializeField] AnimationTransitionRoutingFixtureEndpoint[] m_Endpoints =
        {
            new AnimationTransitionRoutingFixtureEndpoint()
        };
        [SerializeField] AnimationTransitionRoutingFixtureRule[] m_Rules = Array.Empty<AnimationTransitionRoutingFixtureRule>();
        [SerializeField] AnimationTransitionRoutingFixtureFrame[] m_Frames = Array.Empty<AnimationTransitionRoutingFixtureFrame>();

        public int SchemaVersion => m_SchemaVersion;
        public string DefinitionRevision => m_DefinitionRevision ?? string.Empty;
        public string OwnerNodeId => m_OwnerNodeId ?? string.Empty;
        public int EventCapacity => m_EventCapacity;
        public IReadOnlyList<AnimationTransitionRoutingFixtureEndpoint> Endpoints =>
            m_Endpoints ?? Array.Empty<AnimationTransitionRoutingFixtureEndpoint>();
        public IReadOnlyList<AnimationTransitionRoutingFixtureRule> Rules =>
            m_Rules ?? Array.Empty<AnimationTransitionRoutingFixtureRule>();
        public IReadOnlyList<AnimationTransitionRoutingFixtureFrame> Frames =>
            m_Frames ?? Array.Empty<AnimationTransitionRoutingFixtureFrame>();
    }
}
