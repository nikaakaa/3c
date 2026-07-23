using System;
using System.Collections.Generic;
using System.Globalization;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Animation.TransitionRouting
{
    public static class TransitionRoutingCompiler
    {
        public const int CurrentSchemaVersion = 1;

        public static TransitionRoutingCompileResult Compile(TransitionRoutingDefinition definition)
        {
            var diagnostics = new List<TransitionRoutingDiagnostic>();
            if (definition == null)
            {
                diagnostics.Add(new TransitionRoutingDiagnostic(
                    TransitionRoutingReasonCode.MissingEndpointCatalog,
                    "Transition routing definition is required."));
                return new TransitionRoutingCompileResult(null, diagnostics.ToArray());
            }

            if (definition.SchemaVersion != CurrentSchemaVersion)
            {
                diagnostics.Add(new TransitionRoutingDiagnostic(
                    TransitionRoutingReasonCode.InvalidSchemaVersion,
                    $"Schema version must be {CurrentSchemaVersion}."));
            }

            if (!definition.DefinitionRevision.IsValid)
            {
                diagnostics.Add(new TransitionRoutingDiagnostic(
                    TransitionRoutingReasonCode.MissingDefinitionRevision,
                    "Definition revision is required."));
            }

            var endpointSet = new HashSet<TransitionEndpointId>();
            bool hasEmptyEndpoint = false;
            for (int i = 0; i < definition.Endpoints.Count; i++)
            {
                TransitionEndpointId endpoint = definition.Endpoints[i];
                if (!endpoint.IsValid)
                {
                    diagnostics.Add(new TransitionRoutingDiagnostic(
                        TransitionRoutingReasonCode.InvalidEndpoint,
                        $"Endpoint at index {i} is invalid."));
                    continue;
                }

                if (!endpointSet.Add(endpoint))
                {
                    diagnostics.Add(new TransitionRoutingDiagnostic(
                        TransitionRoutingReasonCode.DuplicateEndpoint,
                        $"Endpoint '{endpoint}' is duplicated.",
                        sourceEndpoint: endpoint));
                }

                hasEmptyEndpoint |= endpoint.IsEmpty;
            }

            if (definition.Endpoints.Count == 0)
            {
                diagnostics.Add(new TransitionRoutingDiagnostic(
                    TransitionRoutingReasonCode.MissingEndpointCatalog,
                    "At least one endpoint is required."));
            }
            else if (!hasEmptyEndpoint)
            {
                diagnostics.Add(new TransitionRoutingDiagnostic(
                    TransitionRoutingReasonCode.MissingEmptyEndpoint,
                    $"Endpoint catalog must include '{TransitionEndpointId.Empty}'."));
            }

            var ruleIds = new HashSet<TransitionRuleId>();
            var rulesByPair = new Dictionary<TransitionRuleKey, AnimationTransitionRule>();
            for (int i = 0; i < definition.Rules.Count; i++)
            {
                AnimationTransitionRule rule = definition.Rules[i];
                ValidateRule(rule, i, endpointSet, ruleIds, rulesByPair, diagnostics);
            }

            for (int sourceIndex = 0; sourceIndex < definition.Endpoints.Count; sourceIndex++)
            {
                TransitionEndpointId source = definition.Endpoints[sourceIndex];
                if (!source.IsValid)
                    continue;
                for (int targetIndex = 0; targetIndex < definition.Endpoints.Count; targetIndex++)
                {
                    TransitionEndpointId target = definition.Endpoints[targetIndex];
                    if (!target.IsValid)
                        continue;
                    if (!rulesByPair.ContainsKey(new TransitionRuleKey(source, target)))
                    {
                        diagnostics.Add(new TransitionRoutingDiagnostic(
                            TransitionRoutingReasonCode.MissingPair,
                            $"Exact transition rule is missing for '{source}' -> '{target}'.",
                            sourceEndpoint: source,
                            targetEndpoint: target));
                    }
                }
            }

            if (diagnostics.Count > 0)
                return new TransitionRoutingCompileResult(null, diagnostics.ToArray());

            var endpoints = new TransitionEndpointId[definition.Endpoints.Count];
            for (int i = 0; i < endpoints.Length; i++)
                endpoints[i] = definition.Endpoints[i];

            var orderedRules = new AnimationTransitionRule[endpoints.Length * endpoints.Length];
            int ruleIndex = 0;
            for (int sourceIndex = 0; sourceIndex < endpoints.Length; sourceIndex++)
            {
                for (int targetIndex = 0; targetIndex < endpoints.Length; targetIndex++)
                {
                    orderedRules[ruleIndex++] =
                        rulesByPair[new TransitionRuleKey(endpoints[sourceIndex], endpoints[targetIndex])];
                }
            }

            StableHash canonicalHash = ComputeCanonicalHash(definition, endpoints, orderedRules);
            var plan = new CompiledTransitionRoutingPlan(
                new TransitionRoutingPlanId(StableHash.Compute("transition-routing-plan", canonicalHash.ToString())),
                definition.SchemaVersion,
                definition.DefinitionRevision,
                canonicalHash,
                endpoints,
                orderedRules);
            return new TransitionRoutingCompileResult(plan, Array.Empty<TransitionRoutingDiagnostic>());
        }

        static void ValidateRule(
            AnimationTransitionRule rule,
            int index,
            HashSet<TransitionEndpointId> endpointSet,
            HashSet<TransitionRuleId> ruleIds,
            Dictionary<TransitionRuleKey, AnimationTransitionRule> rulesByPair,
            List<TransitionRoutingDiagnostic> diagnostics)
        {
            if (!rule.RuleId.IsValid)
            {
                diagnostics.Add(new TransitionRoutingDiagnostic(
                    TransitionRoutingReasonCode.InvalidRule,
                    $"Rule at index {index} has no stable RuleId.",
                    sourceEndpoint: rule.SourceEndpoint,
                    targetEndpoint: rule.TargetEndpoint));
            }
            else if (!ruleIds.Add(rule.RuleId))
            {
                diagnostics.Add(new TransitionRoutingDiagnostic(
                    TransitionRoutingReasonCode.DuplicateRule,
                    $"RuleId '{rule.RuleId}' is duplicated.",
                    rule.RuleId,
                    rule.SourceEndpoint,
                    rule.TargetEndpoint));
            }

            if (!endpointSet.Contains(rule.SourceEndpoint))
            {
                diagnostics.Add(new TransitionRoutingDiagnostic(
                    TransitionRoutingReasonCode.UnknownSourceEndpoint,
                    $"Rule '{rule.RuleId}' references unknown source '{rule.SourceEndpoint}'.",
                    rule.RuleId,
                    rule.SourceEndpoint,
                    rule.TargetEndpoint));
            }

            if (!endpointSet.Contains(rule.TargetEndpoint))
            {
                diagnostics.Add(new TransitionRoutingDiagnostic(
                    TransitionRoutingReasonCode.UnknownTargetEndpoint,
                    $"Rule '{rule.RuleId}' references unknown target '{rule.TargetEndpoint}'.",
                    rule.RuleId,
                    rule.SourceEndpoint,
                    rule.TargetEndpoint));
            }

            if (!Enum.IsDefined(typeof(AnimationTransitionBlendLogic), rule.BlendLogic))
            {
                diagnostics.Add(new TransitionRoutingDiagnostic(
                    TransitionRoutingReasonCode.InvalidBlendLogic,
                    $"Rule '{rule.RuleId}' has unsupported Blend Logic '{(byte)rule.BlendLogic}'.",
                    rule.RuleId,
                    rule.SourceEndpoint,
                    rule.TargetEndpoint));
            }
            else if (rule.BlendLogic == AnimationTransitionBlendLogic.StandardBlend)
            {
                if (!IsFinite(rule.DurationSeconds) || rule.DurationSeconds < 0d)
                {
                    diagnostics.Add(new TransitionRoutingDiagnostic(
                        TransitionRoutingReasonCode.InvalidStandardBlendDuration,
                        $"Rule '{rule.RuleId}' Standard Blend duration must be finite and non-negative.",
                        rule.RuleId,
                        rule.SourceEndpoint,
                        rule.TargetEndpoint));
                }
            }
            else if (rule.BlendLogic == AnimationTransitionBlendLogic.Inertialization)
            {
                if (!IsFinite(rule.DurationSeconds) || rule.DurationSeconds <= 0d)
                {
                    diagnostics.Add(new TransitionRoutingDiagnostic(
                        TransitionRoutingReasonCode.InvalidInertializationDuration,
                        $"Rule '{rule.RuleId}' Inertialization duration must be finite and positive.",
                        rule.RuleId,
                        rule.SourceEndpoint,
                        rule.TargetEndpoint));
                }

                if (rule.TargetEndpoint.IsEmpty)
                {
                    diagnostics.Add(new TransitionRoutingDiagnostic(
                        TransitionRoutingReasonCode.InertializationTargetsEmpty,
                        $"Rule '{rule.RuleId}' cannot inertialize to Empty.",
                        rule.RuleId,
                        rule.SourceEndpoint,
                        rule.TargetEndpoint));
                }
            }

            var key = new TransitionRuleKey(rule.SourceEndpoint, rule.TargetEndpoint);
            if (!rulesByPair.TryAdd(key, rule))
            {
                diagnostics.Add(new TransitionRoutingDiagnostic(
                    TransitionRoutingReasonCode.DuplicatePair,
                    $"Transition pair '{key}' has more than one exact rule.",
                    rule.RuleId,
                    rule.SourceEndpoint,
                    rule.TargetEndpoint));
            }
        }

        static StableHash ComputeCanonicalHash(
            TransitionRoutingDefinition definition,
            TransitionEndpointId[] endpoints,
            AnimationTransitionRule[] rules)
        {
            var values = new List<string>(4 + endpoints.Length + rules.Length * 7)
            {
                "transition-routing-definition",
                definition.SchemaVersion.ToString(CultureInfo.InvariantCulture),
                definition.DefinitionRevision.ToString(),
                endpoints.Length.ToString(CultureInfo.InvariantCulture)
            };

            for (int i = 0; i < endpoints.Length; i++)
                values.Add(endpoints[i].ToString());

            for (int i = 0; i < rules.Length; i++)
            {
                AnimationTransitionRule rule = rules[i];
                values.Add(rule.RuleId.ToString());
                values.Add(rule.SourceEndpoint.ToString());
                values.Add(rule.TargetEndpoint.ToString());
                values.Add(((byte)rule.BlendLogic).ToString(CultureInfo.InvariantCulture));
                values.Add(rule.DurationSeconds.ToString("R", CultureInfo.InvariantCulture));
                values.Add(rule.BlendCurveId.ToString());
                values.Add(rule.BlendProfileId.ToString());
            }

            return StableHash.Compute(values.ToArray());
        }

        static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
