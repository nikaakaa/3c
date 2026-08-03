using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CompiledTransitionRoutingRulePayload
    {
        [SerializeField] string m_RuleId = string.Empty;
        [SerializeField] string m_SourceEndpointId = string.Empty;
        [SerializeField] string m_TargetEndpointId = string.Empty;
        [SerializeField] AnimationTransitionBlendLogic m_BlendLogic;
        [SerializeField] double m_DurationSeconds;
        [SerializeField] string m_BlendCurveId = string.Empty;
        [SerializeField] string m_BlendProfileId = string.Empty;

        public CompiledTransitionRoutingRulePayload(
            in AnimationTransitionRule rule)
        {
            m_RuleId = rule.RuleId.Value ?? string.Empty;
            m_SourceEndpointId =
                rule.SourceEndpoint.Value ?? string.Empty;
            m_TargetEndpointId =
                rule.TargetEndpoint.Value ?? string.Empty;
            m_BlendLogic = rule.BlendLogic;
            m_DurationSeconds = rule.DurationSeconds;
            m_BlendCurveId =
                rule.BlendCurveId.Value ?? string.Empty;
            m_BlendProfileId =
                rule.BlendProfileId.Value ?? string.Empty;
            RequireValid();
        }

        internal AnimationTransitionRule Load()
        {
            RequireValid();
            return new AnimationTransitionRule(
                new TransitionRuleId(m_RuleId),
                new TransitionEndpointId(m_SourceEndpointId),
                new TransitionEndpointId(m_TargetEndpointId),
                m_BlendLogic,
                m_DurationSeconds,
                new TransitionBlendCurveId(m_BlendCurveId),
                new TransitionBlendProfileId(m_BlendProfileId));
        }

        void RequireValid()
        {
            if (string.IsNullOrWhiteSpace(m_RuleId) ||
                string.IsNullOrWhiteSpace(m_SourceEndpointId) ||
                string.IsNullOrWhiteSpace(m_TargetEndpointId) ||
                !Enum.IsDefined(
                    typeof(AnimationTransitionBlendLogic),
                    m_BlendLogic) ||
                !double.IsFinite(m_DurationSeconds) ||
                m_DurationSeconds < 0d ||
                string.IsNullOrWhiteSpace(m_BlendCurveId) ||
                string.IsNullOrWhiteSpace(m_BlendProfileId))
            {
                throw new InvalidOperationException(
                    "Compiled Transition Routing rule payload is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CompiledTransitionRoutingPlanPayload
    {
        public const string SchemaVersion =
            "compiled-transition-routing-plan/v1";

        [SerializeField] string m_SchemaVersion = SchemaVersion;
        [SerializeField] string m_PlanId = string.Empty;
        [SerializeField] int m_RoutingSchemaVersion;
        [SerializeField] string m_DefinitionRevision = string.Empty;
        [SerializeField] TransitionRoutingCoveragePolicy m_CoveragePolicy;
        [SerializeField] string m_CanonicalHash = string.Empty;
        [SerializeField] string[] m_EndpointIds = Array.Empty<string>();
        [SerializeField] CompiledTransitionRoutingRulePayload[] m_Rules =
            Array.Empty<CompiledTransitionRoutingRulePayload>();

        public CompiledTransitionRoutingPlanPayload(
            CompiledTransitionRoutingPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            m_PlanId = plan.PlanId.ToString();
            m_RoutingSchemaVersion = plan.SchemaVersion;
            m_DefinitionRevision =
                plan.DefinitionRevision.ToString();
            m_CoveragePolicy = plan.CoveragePolicy;
            m_CanonicalHash = plan.CanonicalHash.ToString();
            m_EndpointIds = new string[plan.Endpoints.Count];
            for (int i = 0; i < m_EndpointIds.Length; i++)
                m_EndpointIds[i] = plan.Endpoints[i].Value;
            m_Rules =
                new CompiledTransitionRoutingRulePayload[
                    plan.Rules.Count];
            for (int i = 0; i < m_Rules.Length; i++)
            {
                AnimationTransitionRule rule = plan.Rules[i];
                m_Rules[i] =
                    new CompiledTransitionRoutingRulePayload(
                        in rule);
            }
            Load();
        }

        public string PlanId => m_PlanId ?? string.Empty;
        public string DefinitionRevision =>
            m_DefinitionRevision ?? string.Empty;

        public CompiledTransitionRoutingPlan Load()
        {
            RequireHeader();
            var endpoints =
                new TransitionEndpointId[m_EndpointIds.Length];
            var endpointSet =
                new HashSet<TransitionEndpointId>();
            for (int i = 0; i < endpoints.Length; i++)
            {
                endpoints[i] =
                    new TransitionEndpointId(m_EndpointIds[i]);
                if (!endpoints[i].IsValid ||
                    !endpointSet.Add(endpoints[i]))
                {
                    throw new InvalidOperationException(
                        "Compiled Transition Routing plan contains an invalid or duplicate endpoint.");
                }
            }

            var rules =
                new AnimationTransitionRule[m_Rules.Length];
            var ruleIds = new HashSet<TransitionRuleId>();
            var pairs = new HashSet<TransitionRuleKey>();
            for (int i = 0; i < rules.Length; i++)
            {
                CompiledTransitionRoutingRulePayload payload =
                    m_Rules[i] ??
                    throw new InvalidOperationException(
                        "Compiled Transition Routing plan contains a missing rule.");
                rules[i] = payload.Load();
                AnimationTransitionRule rule = rules[i];
                if (!ruleIds.Add(rule.RuleId) ||
                    !endpointSet.Contains(rule.SourceEndpoint) ||
                    !endpointSet.Contains(rule.TargetEndpoint) ||
                    !pairs.Add(
                        new TransitionRuleKey(
                            rule.SourceEndpoint,
                            rule.TargetEndpoint)))
                {
                    throw new InvalidOperationException(
                        "Compiled Transition Routing plan contains an invalid rule identity or endpoint pair.");
                }
            }

            var canonicalHash = new StableHash(m_CanonicalHash);
            var planId =
                new TransitionRoutingPlanId(
                    new StableHash(m_PlanId));
            var expectedPlanId =
                new TransitionRoutingPlanId(
                    StableHash.Compute(
                        "transition-routing-plan",
                        canonicalHash.ToString()));
            if (!canonicalHash.IsValid ||
                !planId.IsValid ||
                planId != expectedPlanId)
            {
                throw new InvalidOperationException(
                    "Compiled Transition Routing plan identity is invalid.");
            }
            return new CompiledTransitionRoutingPlan(
                planId,
                m_RoutingSchemaVersion,
                new TransitionDefinitionRevision(
                    m_DefinitionRevision),
                m_CoveragePolicy,
                canonicalHash,
                endpoints,
                rules);
        }

        void RequireHeader()
        {
            if (!string.Equals(
                    m_SchemaVersion,
                    SchemaVersion,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(m_PlanId) ||
                m_RoutingSchemaVersion <= 0 ||
                string.IsNullOrWhiteSpace(
                    m_DefinitionRevision) ||
                !Enum.IsDefined(
                    typeof(TransitionRoutingCoveragePolicy),
                    m_CoveragePolicy) ||
                string.IsNullOrWhiteSpace(m_CanonicalHash) ||
                m_EndpointIds == null ||
                m_EndpointIds.Length == 0 ||
                m_Rules == null ||
                m_Rules.Length == 0)
            {
                throw new InvalidOperationException(
                    "Compiled Transition Routing plan payload is invalid.");
            }
        }
    }
}
