using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Editor.MotionMatching;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class CharacterPresentationAnimationBlendCompiler
    {
        internal sealed class Compilation
        {
            public Compilation(
                AnimationBlendCurveCatalogPayload curveCatalog,
                AnimationBlendProfileCatalogPayload profileCatalog,
                Dictionary<string, int> curveIndices,
                Dictionary<string, int> profileIndices,
                Dictionary<string, int> profileIndicesByIdentity)
            {
                CurveCatalog = curveCatalog;
                ProfileCatalog = profileCatalog;
                CurveIndices = curveIndices;
                ProfileIndices = profileIndices;
                ProfileIndicesByIdentity = profileIndicesByIdentity;
            }

            public AnimationBlendCurveCatalogPayload CurveCatalog { get; }
            public AnimationBlendProfileCatalogPayload ProfileCatalog { get; }
            public Dictionary<string, int> CurveIndices { get; }
            public Dictionary<string, int> ProfileIndices { get; }
            public Dictionary<string, int> ProfileIndicesByIdentity { get; }
        }

        readonly struct SelectionEndpoint
        {
            public SelectionEndpoint(
                AnimationChannelId channelId,
                string programProducerId,
                AnimationSelectionAvailabilityPolicy availability)
            {
                ChannelId = channelId;
                ProgramProducerId = programProducerId ?? string.Empty;
                Availability = availability;
            }

            public AnimationChannelId ChannelId { get; }
            public string ProgramProducerId { get; }
            public AnimationSelectionAvailabilityPolicy Availability { get; }
        }

        readonly struct BlendSourceEndpoint
        {
            public BlendSourceEndpoint(int sourceOwnerIndex, string identity)
            {
                if (sourceOwnerIndex < 0 ||
                    string.IsNullOrWhiteSpace(identity))
                    throw new ArgumentException(
                        "Blend source endpoint is invalid.");
                SourceOwnerIndex = sourceOwnerIndex;
                Identity = identity.Trim();
            }

            public int SourceOwnerIndex { get; }
            public string Identity { get; }
        }

        sealed class CompiledBlendAuthoringNode
        {
            public CompiledBlendAuthoringNode(PoseNodeId nodeId, CharacterPoseCanvasNode node, SelectionEndpoint selection)
            {
                NodeId = nodeId;
                Node = node;
                Selection = selection;
            }

            public PoseNodeId NodeId { get; }
            public CharacterPoseCanvasNode Node { get; }
            public SelectionEndpoint Selection { get; }
        }

        internal static Compilation CompileCatalog(
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterAnimationRigDefinition rig,
            CharacterPresentationPoseResourceCompilationCatalog resources,
            List<string> errors)
        {
            if (!graphAsset || graphAsset.Graph == null || !rig)
                return null;
            CharacterPoseCanvasGraph graph = graphAsset.Graph;
            var curves = new SortedDictionary<string, AnimationBlendCurvePayload>(StringComparer.Ordinal);
            var profiles = new SortedDictionary<string, AnimationBlendProfilePayload>(StringComparer.Ordinal);
            var profileIdentityKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            List<CompiledBlendAuthoringNode> blendNodes =
                CollectBlendAuthoringNodes(graphAsset);
            for (int i = 0; i < blendNodes.Count; i++)
            {
                CharacterAnimationBlendPolicy policy = resources.BlendPolicy(
                    RequireBlendPolicySlot(blendNodes[i].Node),
                    blendNodes[i].NodeId.Value);
                if (!policy)
                    continue;
                CollectBlendRule(policy.DefaultTransition, rig, curves, profiles, profileIdentityKeys, errors);
                for (int overrideIndex = 0; overrideIndex < policy.Overrides.Count; overrideIndex++)
                    CollectBlendRule(policy.Overrides[overrideIndex]?.Rule, rig, curves, profiles, profileIdentityKeys, errors);
            }
            foreach (CharacterPoseCanvasGraph authoredGraph in graphAsset.EnumerateGraphs())
            {
                for (int nodeIndex = 0; nodeIndex < authoredGraph.Nodes.Count; nodeIndex++)
                {
                    if (authoredGraph.Nodes[nodeIndex]?.Payload is not CharacterMotionMatchingPosePayload motionMatching)
                    {
                        continue;
                    }
                    try
                    {
                        CharacterAnimationBlendPolicy policy = resources.BlendPolicy(
                            motionMatching.JumpBlendPolicySlot,
                            authoredGraph.Nodes[nodeIndex].NodeId.Value);
                        policy.RequireValid(rig);
                        RequireStandardBlendOnly(policy.DefaultTransition, authoredGraph.Nodes[nodeIndex].NodeId);
                        if (policy.StackPolicy.StoredPosePolicy != AnimationStoredPosePolicy.CompressOldest ||
                            policy.Overrides.Count != 0)
                        {
                            throw new InvalidOperationException(
                                $"Motion Matching Pose '{authoredGraph.Nodes[nodeIndex].NodeId}' requires CompressOldest and one default Jump transition without owner overrides.");
                        }
                        CollectBlendRule(
                            policy.DefaultTransition,
                            rig,
                            curves,
                            profiles,
                            profileIdentityKeys,
                            errors);
                    }
                    catch (Exception exception)
                    {
                        errors?.Add(exception.Message);
                    }
                }
            }
            foreach (CharacterPoseCanvasGraph authoredGraph in graphAsset.EnumerateGraphs())
            {
                for (int nodeIndex = 0; nodeIndex < authoredGraph.Nodes.Count; nodeIndex++)
                {
                    CharacterPoseStateMachineDefinition stateMachine =
                        authoredGraph.Nodes[nodeIndex]?.PoseStateMachine;
                    if (stateMachine == null)
                        continue;
                    for (int transitionIndex = 0;
                         transitionIndex < stateMachine.Transitions.Count;
                         transitionIndex++)
                    {
                        CollectPoseTransition(
                            stateMachine,
                            stateMachine.Transitions[transitionIndex],
                            rig,
                            resources,
                            curves,
                            profiles,
                            profileIdentityKeys,
                            errors);
                    }
                }
            }
            List<CharacterPoseInertializationPolicy> inertialPolicies =
                CollectInertializationPolicies(graphAsset, resources);
            for (int i = 0; i < inertialPolicies.Count; i++)
            {
                CharacterPoseInertializationPolicy policy = inertialPolicies[i];
                if (!policy || policy.DirectPlayerRule == null)
                    continue;
                CollectInertialRule(
                    policy.DirectPlayerRule,
                    rig,
                    curves,
                    profiles,
                    profileIdentityKeys,
                    errors);
            }
            if (curves.Count == 0 || profiles.Count == 0)
            {
                errors?.Add("Animation Blend catalogs cannot be empty.");
                return null;
            }

            var curveEntries = new AnimationBlendCurveCatalogEntry[curves.Count];
            var curveIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            int curveIndex = 0;
            foreach (KeyValuePair<string, AnimationBlendCurvePayload> pair in curves)
            {
                curveEntries[curveIndex] = new AnimationBlendCurveCatalogEntry(curveIndex, pair.Value);
                curveIndices.Add(pair.Key, curveIndex);
                curveIndex++;
            }
            var profileEntries = new AnimationBlendProfileCatalogEntry[profiles.Count];
            var profileIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            var profileIndicesByIdentity = new Dictionary<string, int>(StringComparer.Ordinal);
            int profileIndex = 0;
            foreach (KeyValuePair<string, AnimationBlendProfilePayload> pair in profiles)
            {
                profileEntries[profileIndex] = new AnimationBlendProfileCatalogEntry(profileIndex, pair.Value);
                profileIndices.Add(pair.Key, profileIndex);
                profileIndicesByIdentity.Add(pair.Value.ProfileId, profileIndex);
                profileIndex++;
            }
            try
            {
                var curveCatalog = new AnimationBlendCurveCatalogPayload(curveEntries);
                var profileCatalog = new AnimationBlendProfileCatalogPayload(profileEntries);
                profileCatalog.RequireValid(rig.PoseBoneCount, rig.RigId, rig.Revision);
                return new Compilation(
                    curveCatalog,
                    profileCatalog,
                    curveIndices,
                    profileIndices,
                    profileIndicesByIdentity);
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
                return null;
            }
        }

        static void CollectBlendRule(
            CharacterAnimationBlendTransitionRule rule,
            CharacterAnimationRigDefinition rig,
            SortedDictionary<string, AnimationBlendCurvePayload> curves,
            SortedDictionary<string, AnimationBlendProfilePayload> profiles,
            Dictionary<string, string> profileIdentityKeys,
            List<string> errors)
        {
            if (rule == null)
                return;
            try
            {
                rule.RequireValid(rig);
                AnimationBlendCurvePayload curve = rule.CompileCurve();
                string curveKey = AnimationBlendCanonicalPayload.CurveKey(curve);
                if (!curves.ContainsKey(curveKey))
                    curves.Add(curveKey, curve);

                var profile = new AnimationBlendProfilePayload(rule.BlendProfile, rig);
                string profileKey = AnimationBlendCanonicalPayload.ProfileKey(profile);
                if (profileIdentityKeys.TryGetValue(profile.ProfileId, out string existingKey) &&
                    !string.Equals(existingKey, profileKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Animation Blend Profile identity '{profile.ProfileId}' resolves to multiple canonical payloads.");
                }
                profileIdentityKeys[profile.ProfileId] = profileKey;
                if (!profiles.ContainsKey(profileKey))
                    profiles.Add(profileKey, profile);
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
            }
        }

        internal static AnimationBlendNodePayload[] CompileNodes(
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterAnimationRigDefinition rig,
            CharacterPresentationPoseResourceCompilationCatalog resources,
            IReadOnlyList<CharacterPresentationProducerEntry> producers,
            Compilation catalogs,
            List<string> errors,
            bool hasGameplayProducerContract)
        {
            if (!graphAsset || graphAsset.Graph == null || !rig || catalogs == null)
                return Array.Empty<AnimationBlendNodePayload>();
            List<CompiledBlendAuthoringNode> authoredNodes =
                CollectBlendAuthoringNodes(graphAsset)
                .OrderBy(value => value.NodeId)
                .ToList();
            var result = new AnimationBlendNodePayload[authoredNodes.Count];
            for (int nodeIndex = 0; nodeIndex < authoredNodes.Count; nodeIndex++)
            {
                CompiledBlendAuthoringNode authored = authoredNodes[nodeIndex];
                CharacterAnimationBlendPolicy policy = resources.BlendPolicy(
                    RequireBlendPolicySlot(authored.Node),
                    authored.NodeId.Value);
                try
                {
                    policy.RequireValid(rig);
                    if (authored.Node.Kind != CharacterPoseNodeKind.AnimationSlot)
                    {
                        RequireStandardBlendOnly(policy.DefaultTransition, authored.NodeId);
                        for (int overrideIndex = 0; overrideIndex < policy.Overrides.Count; overrideIndex++)
                        {
                            CharacterAnimationBlendTransitionOverride transition = policy.Overrides[overrideIndex];
                            if (transition != null)
                                RequireStandardBlendOnly(transition.Rule, authored.NodeId);
                        }
                    }
                }
                catch (Exception exception)
                {
                    errors?.Add(exception.Message);
                    continue;
                }

                BlendSourceEndpoint[] nodeSources;
                if (authored.Node.Kind ==
                    CharacterPoseNodeKind.AnimationSlot)
                {
                    nodeSources = producers
                        .Where(value =>
                            value != null &&
                            value.Kind ==
                            CharacterPresentationProducerKind
                                .Animation &&
                            value.AnimationChannelId ==
                            authored.Selection.ChannelId)
                        .OrderBy(value =>
                            value.ProgramProducerIndex)
                        .Select(value =>
                            new BlendSourceEndpoint(
                                value.ProgramProducerIndex,
                                value.ProgramProducerIdentity))
                        .ToArray();
                }
                else
                {
                    nodeSources = new[]
                    {
                        new BlendSourceEndpoint(
                            0,
                            CharacterPresentationAssetObjectIdentity.Require(
                                authored.Node.PresentationPoseSourceSlot))
                    };
                }
                var identities = new Dictionary<string, int>(StringComparer.Ordinal);
                AnimationSelectionAvailabilityPolicy outputPolicy =
                    authored.Node.Kind ==
                    CharacterPoseNodeKind.AnimationSlot
                        ? authored.Selection.Availability
                        : AnimationSelectionAvailabilityPolicy
                            .RequireSelection;
                for (int i = 0; i < nodeSources.Length; i++)
                {
                    BlendSourceEndpoint source = nodeSources[i];
                    if (!identities.TryAdd(
                            source.Identity,
                            source.SourceOwnerIndex))
                    {
                        errors?.Add(
                            $"Animation transition owner '{authored.NodeId}' duplicates source identity '{source.Identity}'.");
                    }
                }
                if (nodeSources.Length == 0 && hasGameplayProducerContract)
                    errors?.Add($"Animation transition owner '{authored.NodeId}' has no reachable producer on Animation Channel '{authored.Selection.ChannelId}'.");
                if (hasGameplayProducerContract)
                    ValidateTransitionOverrides(policy, authored, identities, errors);

                var transitions = new List<AnimationBlendTransitionPayload>();
                AnimationBlendTransitionEndpointKind initialEndpointKind =
                    authored.Node.Kind == CharacterPoseNodeKind.AnimationSlot
                        ? AnimationBlendTransitionEndpointKind.SourcePose
                        : AnimationBlendTransitionEndpointKind.NoPose;
                if (authored.Node.Kind == CharacterPoseNodeKind.AnimationSlot)
                {
                    CharacterAnimationBlendTransitionRule rule = policy.DefaultTransition;
                    string curveKey = AnimationBlendCanonicalPayload.CurveKey(rule.CompileCurve());
                    transitions.Add(new AnimationBlendTransitionPayload(
                        -1,
                        AnimationBlendTransitionEndpointKind.SourcePose,
                        string.Empty,
                        -1,
                        AnimationBlendTransitionEndpointKind.SourcePose,
                        string.Empty,
                        AnimationTransitionBlendLogic.StandardBlend,
                        0f,
                        catalogs.CurveIndices[curveKey],
                        catalogs.ProfileIndicesByIdentity[rule.BlendProfile.ProfileId]));
                }
                for (int target = 0; target < nodeSources.Length; target++)
                {
                    BlendSourceEndpoint targetSource =
                        nodeSources[target];
                    transitions.Add(CompileTransition(
                        policy,
                        -1,
                        initialEndpointKind,
                        string.Empty,
                        targetSource.SourceOwnerIndex,
                        AnimationBlendTransitionEndpointKind.SourceOwner,
                        targetSource.Identity,
                        outputPolicy,
                        catalogs));
                }
                for (int source = 0; source < nodeSources.Length; source++)
                {
                    BlendSourceEndpoint sourceEndpoint =
                        nodeSources[source];
                    for (int target = 0; target < nodeSources.Length; target++)
                    {
                        BlendSourceEndpoint targetEndpoint =
                            nodeSources[target];
                        transitions.Add(CompileTransition(
                            policy,
                            sourceEndpoint.SourceOwnerIndex,
                            AnimationBlendTransitionEndpointKind.SourceOwner,
                            sourceEndpoint.Identity,
                            targetEndpoint.SourceOwnerIndex,
                            AnimationBlendTransitionEndpointKind.SourceOwner,
                            targetEndpoint.Identity,
                            outputPolicy,
                            catalogs));
                    }
                    if (outputPolicy ==
                        AnimationSelectionAvailabilityPolicy
                            .AllowEmpty)
                    {
                        transitions.Add(CompileTransition(
                            policy,
                            sourceEndpoint.SourceOwnerIndex,
                            AnimationBlendTransitionEndpointKind.SourceOwner,
                            sourceEndpoint.Identity,
                            -1,
                            AnimationBlendTransitionEndpointKind.SourcePose,
                            string.Empty,
                            outputPolicy,
                            catalogs));
                    }
                }

                AnimationBlendTransitionPayload[] compiledTransitions =
                    transitions.ToArray();
                CompiledTransitionRoutingPlanPayload routingPlan =
                    authored.Node.Kind ==
                    CharacterPoseNodeKind.AnimationSlot
                        ? null
                        : CompileBlendRoutingPlan(
                            authored.NodeId,
                            policy.PolicyId,
                            policy.Revision,
                            compiledTransitions);
                result[nodeIndex] = new AnimationBlendNodePayload(
                    authored.NodeId,
                    policy.PolicyId,
                    policy.Revision,
                    new AnimationBlendStackPolicyPayload(policy.StackPolicy),
                    compiledTransitions,
                    routingPlan);
            }
            return result;
        }

        static void CollectPoseTransition(
            CharacterPoseStateMachineDefinition stateMachine,
            CharacterPoseStateTransition transition,
            CharacterAnimationRigDefinition rig,
            CharacterPresentationPoseResourceCompilationCatalog resources,
            SortedDictionary<string, AnimationBlendCurvePayload> curves,
            SortedDictionary<string, AnimationBlendProfilePayload> profiles,
            Dictionary<string, string> profileIdentityKeys,
            List<string> errors)
        {
            try
            {
                if (transition == null)
                    throw new InvalidOperationException("Pose State Transition is missing.");
                CharacterPoseStateTransition.RequireBlendSettings(
                    transition.BlendLogic,
                    transition.DurationSeconds,
                    transition.BlendMode,
                    transition.CustomBlendCurveSlot,
                    transition.BlendProfileSlot);
                CharacterAnimationBlendCurveAsset customBlendCurve =
                    transition.CustomBlendCurveSlot
                        ? resources.BlendCurve(
                            transition.CustomBlendCurveSlot,
                            transition.TransitionId.Value)
                        : null;
                CharacterAnimationBlendProfile blendProfile =
                    transition.BlendProfileSlot
                        ? resources.BlendProfile(
                            transition.BlendProfileSlot,
                            transition.TransitionId.Value)
                        : null;
                AnimationBlendCurvePayload curve =
                    CharacterAnimationBlendCurveCompiler.Compile(
                        transition.BlendMode,
                        customBlendCurve);
                string curveKey = AnimationBlendCanonicalPayload.CurveKey(curve);
                if (!curves.ContainsKey(curveKey))
                    curves.Add(curveKey, curve);
                if (!blendProfile)
                    return;
                var profile = new AnimationBlendProfilePayload(
                    blendProfile,
                    rig);
                string profileKey = AnimationBlendCanonicalPayload.ProfileKey(profile);
                if (profileIdentityKeys.TryGetValue(profile.ProfileId, out string existingKey) &&
                    !string.Equals(existingKey, profileKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Animation Blend Profile identity '{profile.ProfileId}' resolves to multiple canonical payloads.");
                }
                profileIdentityKeys[profile.ProfileId] = profileKey;
                if (!profiles.ContainsKey(profileKey))
                    profiles.Add(profileKey, profile);
            }
            catch (Exception exception)
            {
                errors?.Add(
                    $"Pose StateMachine '{stateMachine?.StateMachineId}' Transition '{transition?.TransitionId}': {exception.Message}");
            }
        }

        static CompiledTransitionRoutingPlanPayload
            CompileBlendRoutingPlan(
                PoseNodeId nodeId,
                string policyId,
                string policyRevision,
                IReadOnlyList<AnimationBlendTransitionPayload>
                    transitions)
        {
            var identities = new Dictionary<int, string>();
            for (int i = 0; i < transitions.Count; i++)
            {
                AnimationBlendTransitionPayload transition =
                    transitions[i] ??
                    throw new InvalidOperationException(
                        $"Animation Blend Stack '{nodeId}' has a missing transition.");
                CollectBlendRoutingIdentity(
                    transition.SourceOwnerIndex,
                    transition.SourceEndpointKind,
                    transition.SourceOwnerIdentity,
                    identities);
                CollectBlendRoutingIdentity(
                    transition.TargetOwnerIndex,
                    transition.TargetEndpointKind,
                    transition.TargetOwnerIdentity,
                    identities);
            }
            var endpoints = new List<TransitionEndpointId>();
            if (transitions.Any(value =>
                    value.SourceEndpointKind == AnimationBlendTransitionEndpointKind.SourcePose ||
                    value.TargetEndpointKind == AnimationBlendTransitionEndpointKind.SourcePose))
            {
                endpoints.Add(TransitionEndpointId.SourcePose);
            }
            if (transitions.Any(value =>
                    value.SourceEndpointKind == AnimationBlendTransitionEndpointKind.NoPose ||
                    value.TargetEndpointKind == AnimationBlendTransitionEndpointKind.NoPose))
            {
                endpoints.Add(TransitionEndpointId.NoPose);
            }
            var endpointsByOwner =
                new Dictionary<int, TransitionEndpointId>();
            foreach (KeyValuePair<int, string> owner in
                     identities.OrderBy(
                          value => value.Value,
                          StringComparer.Ordinal))
            {
                var endpoint = new TransitionEndpointId(
                    $"animation-blend/{nodeId}/owner/{owner.Value}");
                endpoints.Add(endpoint);
                endpointsByOwner.Add(
                    owner.Key,
                    endpoint);
            }
            var rules =
                new AnimationTransitionRule[transitions.Count];
            for (int i = 0; i < rules.Length; i++)
            {
                AnimationBlendTransitionPayload transition =
                    transitions[i];
                TransitionEndpointId source = ResolveBlendRoutingEndpoint(
                    transition.SourceOwnerIndex,
                    transition.SourceEndpointKind,
                    endpointsByOwner);
                TransitionEndpointId target = ResolveBlendRoutingEndpoint(
                    transition.TargetOwnerIndex,
                    transition.TargetEndpointKind,
                    endpointsByOwner);
                rules[i] = new AnimationTransitionRule(
                    new TransitionRuleId(
                        $"animation-blend/{nodeId}/route/{StableHash.Compute(source.Value, target.Value)}"),
                    source,
                    target,
                    transition.BlendLogic,
                    transition.DurationSeconds,
                    new TransitionBlendCurveId(
                        $"curve/{transition.CurveIndex}"),
                    new TransitionBlendProfileId(
                        $"profile/{transition.BlendProfileIndex}"));
            }
            var revision = new TransitionDefinitionRevision(
                StableHash.Compute(
                    policyId,
                    policyRevision,
                    nodeId.Value).ToString());
            TransitionRoutingCompileResult result =
                TransitionRoutingCompiler.Compile(
                    new TransitionRoutingDefinition(
                        TransitionRoutingCompiler.CurrentSchemaVersion,
                        revision,
                        TransitionRoutingCoveragePolicy.DeclaredRules,
                        endpoints,
                        rules));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Animation Blend Stack '{nodeId}' Transition Routing compile failed: " +
                    string.Join(
                        " | ",
                        result.Diagnostics.Select(
                            value =>
                                $"[{value.Code}] {value.Message}")));
            }
            return new CompiledTransitionRoutingPlanPayload(
                result.Plan);
        }

        static void CollectBlendRoutingIdentity(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind endpointKind,
            string sourceOwnerIdentity,
            Dictionary<int, string> identities)
        {
            if (endpointKind != AnimationBlendTransitionEndpointKind.SourceOwner)
                return;
            if (sourceOwnerIndex < 0 ||
                string.IsNullOrWhiteSpace(sourceOwnerIdentity))
            {
                throw new InvalidOperationException(
                    "Animation Blend Stack transition source owner endpoint is invalid.");
            }
            if (identities.TryGetValue(
                    sourceOwnerIndex,
                    out string existing) &&
                !string.Equals(
                    existing,
                    sourceOwnerIdentity,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Animation Blend Stack source owner index '{sourceOwnerIndex}' resolves to multiple identities.");
            }
            identities[sourceOwnerIndex] = sourceOwnerIdentity;
        }

        static TransitionEndpointId ResolveBlendRoutingEndpoint(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind endpointKind,
            IReadOnlyDictionary<int, TransitionEndpointId> endpointsByOwner) =>
            endpointKind switch
            {
                AnimationBlendTransitionEndpointKind.SourceOwner =>
                    endpointsByOwner[sourceOwnerIndex],
                AnimationBlendTransitionEndpointKind.SourcePose =>
                    TransitionEndpointId.SourcePose,
                AnimationBlendTransitionEndpointKind.NoPose =>
                    TransitionEndpointId.NoPose,
                _ => throw new InvalidOperationException(
                    "Animation Blend transition endpoint kind is invalid.")
            };

        static void RequireStandardBlendOnly(
            CharacterAnimationBlendTransitionRule rule,
            PoseNodeId nodeId)
        {
            if (rule == null || rule.BlendLogic != AnimationTransitionBlendLogic.StandardBlend)
            {
                throw new InvalidOperationException(
                    $"Blend Stack '{nodeId}' cannot own branch-local Inertialization; use PoseStateMachine or AnimationSlot.");
            }
        }

        static void CollectInertialRule(
            CharacterPoseDirectInertializationRule rule,
            CharacterAnimationRigDefinition rig,
            SortedDictionary<string, AnimationBlendCurvePayload> curves,
            SortedDictionary<string, AnimationBlendProfilePayload> profiles,
            Dictionary<string, string> profileIdentityKeys,
            List<string> errors)
        {
            if (rule == null)
                return;
            try
            {
                rule.RequireValid(rig);
                if (rule.Mode == PoseInertializationMode.HardCut)
                    return;
                AnimationBlendCurvePayload curve = rule.CompileCurve();
                string curveKey = AnimationBlendCanonicalPayload.CurveKey(curve);
                if (!curves.ContainsKey(curveKey))
                    curves.Add(curveKey, curve);
                var profile = new AnimationBlendProfilePayload(rule.BlendProfile, rig);
                string profileKey = AnimationBlendCanonicalPayload.ProfileKey(profile);
                if (profileIdentityKeys.TryGetValue(profile.ProfileId, out string existingKey) &&
                    !string.Equals(existingKey, profileKey, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Animation Blend Profile identity '{profile.ProfileId}' resolves to multiple canonical payloads.");
                profileIdentityKeys[profile.ProfileId] = profileKey;
                if (!profiles.ContainsKey(profileKey))
                    profiles.Add(profileKey, profile);
            }
            catch (Exception exception)
            {
                errors?.Add(exception.Message);
            }
        }

        static List<CharacterPoseInertializationPolicy> CollectInertializationPolicies(
            CharacterPresentationPoseGraphAsset graphAsset,
            CharacterPresentationPoseResourceCompilationCatalog resources)
        {
            var result = new List<CharacterPoseInertializationPolicy>();
            CollectInertializationPolicies(
                graphAsset,
                graphAsset.Graph,
                resources,
                result);
            return result;
        }

        static CharacterPoseResourceSlot RequireBlendPolicySlot(
            CharacterPoseCanvasNode node) =>
            node.Payload switch
            {
                CharacterAnimationSlotPosePayload value => value.BlendPolicySlot,
                CharacterBlendStackPosePayload value => value.BlendPolicySlot,
                _ => throw new InvalidOperationException(
                    $"Pose node '{node.NodeId}' does not own a Blend Policy Slot.")
            };

        static void CollectInertializationPolicies(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseCanvasGraph graph,
            CharacterPresentationPoseResourceCompilationCatalog resources,
            List<CharacterPoseInertializationPolicy> result)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterPoseCanvasNode node = graph.Nodes[i];
                if (node.Kind == CharacterPoseNodeKind.Inertialization)
                    result.Add(resources.InertializationPolicy(
                        node.RequirePayload<CharacterInertializationPosePayload>().PolicySlot,
                        node.NodeId.Value));
                if (node.Kind != CharacterPoseNodeKind.PoseSubgraph ||
                    node.Subgraph == null ||
                    !node.Subgraph.PoseGraphId.IsValid)
                    continue;
                CharacterPoseCanvasGraph child =
                    owner.RequireGraph(node.Subgraph.PoseGraphId);
                CollectInertializationPolicies(owner, child, resources, result);
            }
        }

        static void ValidateTransitionOverrides(
            CharacterAnimationBlendPolicy policy,
            CompiledBlendAuthoringNode authored,
            IReadOnlyDictionary<string, int> sourceOwnerIdentities,
            List<string> errors)
        {
            AnimationBlendTransitionEndpointKind initialEndpointKind =
                authored.Node.Kind == CharacterPoseNodeKind.AnimationSlot
                    ? AnimationBlendTransitionEndpointKind.SourcePose
                    : AnimationBlendTransitionEndpointKind.NoPose;
            AnimationSelectionAvailabilityPolicy outputPolicy =
                authored.Node.Kind == CharacterPoseNodeKind.AnimationSlot
                    ? authored.Selection.Availability
                    : AnimationSelectionAvailabilityPolicy.RequireSelection;
            for (int i = 0; i < policy.Overrides.Count; i++)
            {
                CharacterAnimationBlendTransitionOverride transition = policy.Overrides[i];
                if (transition == null)
                    continue;
                if (transition.SourceEndpointKind != AnimationBlendTransitionEndpointKind.SourceOwner &&
                    transition.SourceEndpointKind != initialEndpointKind)
                {
                    errors?.Add(
                        $"Animation transition owner '{authored.NodeId}' transition override #{i} uses endpoint '{transition.SourceEndpointKind}' outside this node type.");
                    continue;
                }
                if (transition.SourceEndpointKind == transition.TargetEndpointKind &&
                    transition.SourceEndpointKind != AnimationBlendTransitionEndpointKind.SourceOwner)
                {
                    errors?.Add(
                        $"Animation transition owner '{authored.NodeId}' transition override #{i} cannot override the unchanged '{transition.SourceEndpointKind}' route.");
                    continue;
                }
                if (transition.SourceEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner &&
                    !sourceOwnerIdentities.ContainsKey(transition.SourceOwnerIdentity))
                    errors?.Add($"Animation transition owner '{authored.NodeId}' transition override #{i} references source owner '{transition.SourceOwnerIdentity}' outside its endpoint set.");
                if (transition.TargetEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner &&
                    !sourceOwnerIdentities.ContainsKey(transition.TargetOwnerIdentity))
                    errors?.Add($"Animation transition owner '{authored.NodeId}' transition override #{i} references target owner '{transition.TargetOwnerIdentity}' outside its endpoint set.");
                if (transition.TargetEndpointKind != AnimationBlendTransitionEndpointKind.SourceOwner &&
                    (transition.TargetEndpointKind != AnimationBlendTransitionEndpointKind.SourcePose ||
                     initialEndpointKind != AnimationBlendTransitionEndpointKind.SourcePose ||
                     outputPolicy != AnimationSelectionAvailabilityPolicy.AllowEmpty))
                {
                    errors?.Add(
                        $"Animation transition owner '{authored.NodeId}' transition override #{i} targets endpoint '{transition.TargetEndpointKind}' outside this node's output contract.");
                }
            }
        }

        static AnimationBlendTransitionPayload CompileTransition(
            CharacterAnimationBlendPolicy policy,
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind sourceEndpointKind,
            string sourceOwnerIdentity,
            int targetOwnerIndex,
            AnimationBlendTransitionEndpointKind targetEndpointKind,
            string targetOwnerIdentity,
            AnimationSelectionAvailabilityPolicy outputPolicy,
            Compilation catalogs)
        {
            CharacterAnimationBlendTransitionRule rule = policy.DefaultTransition;
            for (int i = 0; i < policy.Overrides.Count; i++)
            {
                CharacterAnimationBlendTransitionOverride candidate = policy.Overrides[i];
                if (candidate != null &&
                    candidate.SourceEndpointKind == sourceEndpointKind &&
                    candidate.TargetEndpointKind == targetEndpointKind &&
                    string.Equals(candidate.SourceOwnerIdentity, sourceOwnerIdentity, StringComparison.Ordinal) &&
                    string.Equals(candidate.TargetOwnerIdentity, targetOwnerIdentity, StringComparison.Ordinal))
                {
                    rule = candidate.Rule;
                    break;
                }
            }
            string curveKey = AnimationBlendCanonicalPayload.CurveKey(rule.CompileCurve());
            float durationSeconds =
                outputPolicy == AnimationSelectionAvailabilityPolicy.RequireSelection &&
                sourceEndpointKind == AnimationBlendTransitionEndpointKind.NoPose &&
                targetEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner
                ? 0f
                : rule.DurationSeconds;
            return new AnimationBlendTransitionPayload(
                sourceOwnerIndex,
                sourceEndpointKind,
                sourceOwnerIdentity,
                targetOwnerIndex,
                targetEndpointKind,
                targetOwnerIdentity,
                rule.BlendLogic,
                durationSeconds,
                catalogs.CurveIndices[curveKey],
                catalogs.ProfileIndicesByIdentity[rule.BlendProfile.ProfileId]);
        }

        static List<CompiledBlendAuthoringNode> CollectBlendAuthoringNodes(
            CharacterPresentationPoseGraphAsset owner)
        {
            var result = new List<CompiledBlendAuthoringNode>();
            CollectBlendAuthoringNodes(
                owner,
                owner.Graph,
                string.Empty,
                new Dictionary<PoseInterfacePortId, SelectionEndpoint>(),
                result);
            var identities = new HashSet<PoseNodeId>();
            for (int i = 0; i < result.Count; i++)
            {
                if (!identities.Add(result[i].NodeId))
                    throw new InvalidOperationException($"Pose Graph duplicates compiled Blend Stack '{result[i].NodeId}'.");
            }
            return result;
        }

        static Dictionary<PoseInterfacePortId, SelectionEndpoint> CollectBlendAuthoringNodes(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseCanvasGraph graph,
            string scope,
            IReadOnlyDictionary<PoseInterfacePortId, SelectionEndpoint> imports,
            List<CompiledBlendAuthoringNode> result)
        {
            Dictionary<string, CharacterPoseCanvasConnection> incoming = graph.Edges.ToDictionary(
                edge => edge.TargetNodeId.Value + "\0" + edge.TargetPortId.Value,
                edge => edge,
                StringComparer.Ordinal);
            var values = new Dictionary<string, SelectionEndpoint>(StringComparer.Ordinal);
            var exports = new Dictionary<PoseInterfacePortId, SelectionEndpoint>();
            List<CharacterPoseCanvasNode> ordered = TopologicalPoseNodes(graph);
            for (int nodeIndex = 0; nodeIndex < ordered.Count; nodeIndex++)
            {
                CharacterPoseCanvasNode node = ordered[nodeIndex];
                if (node.Kind ==
                    CharacterPoseNodeKind.ActionPlaybackInput)
                {
                    CharacterPoseNodeDefinition handler =
                        CharacterPoseNodeDefinitionModule.Shared
                            .Require(node.Kind);
                    var endpoint = new SelectionEndpoint(
                        handler.Channel(node.Payload),
                        string.Empty,
                        handler.Availability(node.Payload, false));
                    BindSelectionOutputs(node, scope, endpoint, values);
                    continue;
                }
                if (node.Kind == CharacterPoseNodeKind.GraphInput)
                {
                    IReadOnlyList<CharacterPosePortDefinition> ports =
                        CharacterPoseAuthoringPortProjection.Get(node);
                    for (int i = 0; i < ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = ports[i];
                        if (port != null && IsSelectionPort(port.Kind) &&
                            port.Direction == CharacterPosePortDirection.Output && imports.TryGetValue(port.InterfacePortId, out SelectionEndpoint endpoint))
                            values.Add(ScopedEndpoint(node.NodeId, port.PortId, scope), endpoint);
                    }
                    continue;
                }
                if (node.Kind == CharacterPoseNodeKind.GraphOutput)
                {
                    IReadOnlyList<CharacterPosePortDefinition> ports =
                        CharacterPoseAuthoringPortProjection.Get(node);
                    for (int i = 0; i < ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = ports[i];
                        if (port != null && IsSelectionPort(port.Kind) &&
                            port.Direction == CharacterPosePortDirection.Input &&
                            TryResolveSelection(node, port, incoming, scope, values, out SelectionEndpoint endpoint))
                            exports.Add(port.InterfacePortId, endpoint);
                    }
                    continue;
                }
                if (node.Kind == CharacterPoseNodeKind.PoseSubgraph)
                {
                    var childImports = new Dictionary<PoseInterfacePortId, SelectionEndpoint>();
                    IReadOnlyList<CharacterPosePortDefinition> ports =
                        CharacterPoseAuthoringPortProjection.Get(node);
                    for (int i = 0; i < ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = ports[i];
                        if (port != null && IsSelectionPort(port.Kind) &&
                            port.Direction == CharacterPosePortDirection.Input &&
                            TryResolveSelection(node, port, incoming, scope, values, out SelectionEndpoint endpoint))
                            childImports.Add(port.InterfacePortId, endpoint);
                    }
                    CharacterPoseCanvasGraph child =
                        owner.RequireGraph(node.Subgraph.PoseGraphId);
                    PoseNodeId callSite = ScopePoseNodeId(node.NodeId, scope);
                    string childScope = callSite.Value + "/" + child.GraphId;
                    Dictionary<PoseInterfacePortId, SelectionEndpoint> childExports =
                        CollectBlendAuthoringNodes(
                            owner,
                            child,
                            childScope,
                            childImports,
                            result);
                    for (int i = 0; i < ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = ports[i];
                        if (port != null && IsSelectionPort(port.Kind) &&
                            port.Direction == CharacterPosePortDirection.Output && childExports.TryGetValue(port.InterfacePortId, out SelectionEndpoint endpoint))
                            values.Add(ScopedEndpoint(node.NodeId, port.PortId, scope), endpoint);
                    }
                    continue;
                }
                if (node.Kind ==
                        CharacterPoseNodeKind.PoseStateMachine &&
                    node.PoseStateMachine != null)
                {
                    PoseNodeId stateMachineNodeId =
                        ScopePoseNodeId(node.NodeId, scope);
                    for (int stateIndex = 0;
                         stateIndex <
                         node.PoseStateMachine.States.Count;
                         stateIndex++)
                    {
                        CharacterPoseStateDefinition state =
                            node.PoseStateMachine.States[
                                stateIndex];
                        if (state == null ||
                            !state.PoseGraphId.IsValid)
                            continue;
                        CollectBlendAuthoringNodes(
                            owner,
                            owner.RequireGraph(
                                state.PoseGraphId),
                            stateMachineNodeId.Value +
                            "/state/" +
                            state.StateId.Value,
                            new Dictionary<
                                PoseInterfacePortId,
                                SelectionEndpoint>(),
                            result);
                    }
                    continue;
                }
                if (node.Kind != CharacterPoseNodeKind.BlendStack &&
                    node.Kind != CharacterPoseNodeKind.AnimationSlot)
                    continue;
                if (node.Kind == CharacterPoseNodeKind.BlendStack)
                {
                    if (!node.PresentationPoseSourceSlot)
                    {
                        throw new InvalidOperationException(
                            $"Pose State Blend Stack '{node.NodeId}' has no Source Slot.");
                    }
                    result.Add(new CompiledBlendAuthoringNode(
                        ScopePoseNodeId(node.NodeId, scope),
                        node,
                        default));
                    continue;
                }
                if (!node.AnimationChannelId.IsValid)
                    throw new InvalidOperationException($"Animation Slot '{node.NodeId}' has no Animation Channel identity.");
                SelectionEndpoint selection = new SelectionEndpoint(
                    node.AnimationChannelId,
                    string.Empty,
                    AnimationSelectionAvailabilityPolicy.AllowEmpty);
                result.Add(new CompiledBlendAuthoringNode(ScopePoseNodeId(node.NodeId, scope), node, selection));
            }
            return exports;
        }

        static void BindSelectionOutputs(
            CharacterPoseCanvasNode node,
            string scope,
            SelectionEndpoint endpoint,
            Dictionary<string, SelectionEndpoint> values)
        {
            IReadOnlyList<CharacterPosePortDefinition> ports =
                CharacterPoseAuthoringPortProjection.Get(node);
            for (int i = 0; i < ports.Count; i++)
            {
                CharacterPosePortDefinition port = ports[i];
                if (port != null && IsSelectionPort(port.Kind) && port.Direction == CharacterPosePortDirection.Output)
                    values.Add(ScopedEndpoint(node.NodeId, port.PortId, scope), endpoint);
            }
        }

        static bool IsSelectionPort(CharacterPosePortKind kind) =>
            kind == CharacterPosePortKind.ActionPlayback;

        static bool TryResolveSelection(
            CharacterPoseCanvasNode node,
            CharacterPosePortDefinition port,
            IReadOnlyDictionary<string, CharacterPoseCanvasConnection> incoming,
            string scope,
            IReadOnlyDictionary<string, SelectionEndpoint> values,
            out SelectionEndpoint endpoint)
        {
            endpoint = default;
            return incoming.TryGetValue(node.NodeId.Value + "\0" + port.PortId.Value, out CharacterPoseCanvasConnection edge) &&
                   values.TryGetValue(ScopedEndpoint(edge.SourceNodeId, edge.SourcePortId, scope), out endpoint);
        }

        static List<CharacterPoseCanvasNode> TopologicalPoseNodes(CharacterPoseCanvasGraph graph)
        {
            var nodes = graph.Nodes.ToDictionary(node => node.NodeId);
            var indegree = nodes.Keys.ToDictionary(node => node, _ => 0);
            var outgoing = new Dictionary<PoseNodeId, List<PoseNodeId>>();
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                CharacterPoseCanvasConnection edge = graph.Edges[i];
                indegree[edge.TargetNodeId]++;
                if (!outgoing.TryGetValue(edge.SourceNodeId, out List<PoseNodeId> targets))
                {
                    targets = new List<PoseNodeId>();
                    outgoing.Add(edge.SourceNodeId, targets);
                }
                targets.Add(edge.TargetNodeId);
            }
            var ready = new SortedSet<PoseNodeId>(indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key));
            var result = new List<CharacterPoseCanvasNode>(nodes.Count);
            while (ready.Count > 0)
            {
                PoseNodeId current = ready.Min;
                ready.Remove(current);
                result.Add(nodes[current]);
                if (!outgoing.TryGetValue(current, out List<PoseNodeId> targets))
                    continue;
                targets.Sort();
                for (int i = 0; i < targets.Count; i++)
                {
                    if (--indegree[targets[i]] == 0)
                        ready.Add(targets[i]);
                }
            }
            if (result.Count != nodes.Count)
                throw new InvalidOperationException($"Pose Graph '{graph.GraphId}' contains a cycle.");
            return result;
        }

        static string ScopedEndpoint(PoseNodeId nodeId, PosePortId portId, string scope) =>
            ScopePoseNodeId(nodeId, scope).Value + "\0" +
            (string.IsNullOrEmpty(scope) ? portId.Value : scope + "/" + portId.Value);

        static PoseNodeId ScopePoseNodeId(PoseNodeId nodeId, string scope) =>
            string.IsNullOrEmpty(scope) ? nodeId : new PoseNodeId(scope + "/" + nodeId.Value);
    }
}
