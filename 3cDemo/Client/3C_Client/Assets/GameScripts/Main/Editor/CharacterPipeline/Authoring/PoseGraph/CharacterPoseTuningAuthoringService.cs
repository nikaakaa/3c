using System;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using TreeDesigner.Editor;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class CharacterPoseTuningAuthoringService
    {
        public static bool TryApply(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile,
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value,
            out string error)
        {
            error = string.Empty;
            try
            {
                Apply(asset, profile, entry, value);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TryReadCurrentValue(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseTuningLayoutEntry entry,
            out CharacterPoseTuningValue value,
            out string error)
        {
            value = default;
            error = string.Empty;
            try
            {
                if (!asset)
                    throw new InvalidOperationException("Pose tuning requires an exact Pose Graph owner.");
                if (entry == null)
                    throw new InvalidOperationException("Pose tuning field is missing.");
                value = ReadCurrentValue(asset, entry);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static bool TryCompileCurrentBlock(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPresentationProjection projection,
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock source,
            out CharacterPoseTuningParameterBlock block,
            out string error)
        {
            block = null;
            error = string.Empty;
            try
            {
                if (!asset || projection == null || layout == null || source == null)
                    throw new InvalidOperationException("Pose tuning candidate context is incomplete.");
                layout.RequireValid();
                source.RequireValid(layout);
                CharacterPoseTuningCompilationResult currentProfiles =
                    CharacterPoseTuningLayoutCompiler.Compile(
                        layout.ProgramId,
                        projection);
                if (!string.Equals(
                        currentProfiles.Layout.LayoutHash,
                        layout.LayoutHash,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Pose tuning layout changed and requires an explicit Character Build.");
                }

                CharacterPoseTuningParameterBlock result = source.Clone();
                for (int i = 0; i < layout.Entries.Count; i++)
                {
                    CharacterPoseTuningLayoutEntry entry = layout.Entries[i];
                    if (entry.Interaction !=
                        CharacterPoseTuningInteractionPolicy.TunableDefault)
                        continue;
                    CharacterPoseTuningValue value = ReadCurrentValue(
                        asset,
                        entry);
                    result = CharacterPoseTuningCandidateCompiler.CompileBlock(
                        layout,
                        result,
                        entry,
                        value);
                }
                result.RequireValid(layout);
                block = result;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        static void Apply(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile,
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            if (!asset)
                throw new InvalidOperationException("Pose tuning requires an exact Pose Graph owner.");
            if (entry == null || entry.Interaction != CharacterPoseTuningInteractionPolicy.TunableDefault)
                throw new InvalidOperationException("The selected Pose tuning field is not writable.");
            if (entry.OwnerId.StartsWith("pose-node:", StringComparison.Ordinal))
            {
                ApplyPoseNode(asset, profile, entry, value);
                return;
            }
            if (entry.OwnerId.StartsWith("pose-state-machine:", StringComparison.Ordinal))
            {
                ApplyTransition(asset, profile, entry, value);
                return;
            }
            if (entry.OwnerId.StartsWith("full-body-ik-profile:", StringComparison.Ordinal))
            {
                CharacterFullBodyIkProfile owner = FindFullBodyIkProfile(asset, entry.OwnerId);
                ApplyProfile(owner, entry, value);
                return;
            }
            if (entry.OwnerId.StartsWith("foot-placement-profile:", StringComparison.Ordinal))
            {
                CharacterFootPlacementProfile owner = FindFootPlacementProfile(asset, entry.OwnerId);
                ApplyProfile(owner, entry, value);
                return;
            }
            if (entry.OwnerId.StartsWith("animation-blend-policy:", StringComparison.Ordinal))
            {
                CharacterAnimationBlendPolicy owner = FindBlendPolicy(asset, entry.OwnerId);
                ApplyPolicy(owner, entry, value);
                return;
            }
            if (entry.OwnerId.StartsWith("pose-inertialization-policy:", StringComparison.Ordinal))
            {
                CharacterPoseInertializationPolicy owner = FindInertializationPolicy(asset, entry.OwnerId);
                ApplyPolicy(owner, entry, value);
                return;
            }
            throw new InvalidOperationException($"Pose tuning owner '{entry.OwnerId}' is not registered.");
        }

        static CharacterPoseTuningValue ReadCurrentValue(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseTuningLayoutEntry entry)
        {
            if (entry.OwnerId.StartsWith(
                    "full-body-ik-profile:",
                    StringComparison.Ordinal))
            {
                return ReadFullBodyIkValue(
                    FindFullBodyIkProfile(asset, entry.OwnerId),
                    entry.FieldId.Substring(entry.OwnerId.Length + 1));
            }
            if (entry.OwnerId.StartsWith(
                    "foot-placement-profile:",
                    StringComparison.Ordinal))
            {
                return ReadFootPlacementValue(
                    FindFootPlacementProfile(asset, entry.OwnerId),
                    entry.FieldId.Substring(entry.OwnerId.Length + 1));
            }
            if (entry.OwnerId.StartsWith(
                    "animation-blend-policy:",
                    StringComparison.Ordinal))
            {
                CharacterAnimationBlendPolicy policy =
                    FindBlendPolicy(asset, entry.OwnerId);
                string fieldId = entry.FieldId.Substring(
                    entry.OwnerId.Length + 1);
                return fieldId switch
                {
                    "max-active-source-entries" =>
                        CharacterPoseTuningValue.Integer(
                            policy.StackPolicy.MaxActiveSourceEntries),
                    "stored-pose-policy" =>
                        CharacterPoseTuningValue.Enum(
                            (int)policy.StackPolicy.StoredPosePolicy),
                    "max-blend-in-time-to-replace-newest" =>
                        CharacterPoseTuningValue.Float(
                            policy.StackPolicy.MaxBlendInTimeToReplaceNewest),
                    "depth-blend-time-multiplier" =>
                        CharacterPoseTuningValue.Float(
                            policy.StackPolicy.DepthBlendTimeMultiplier),
                    _ => throw new InvalidOperationException(
                        $"Animation Blend Policy field '{entry.FieldId}' is unsupported.")
                };
            }
            if (entry.OwnerId.StartsWith(
                    "pose-inertialization-policy:",
                    StringComparison.Ordinal))
            {
                CharacterPoseInertializationPolicy policy =
                    FindInertializationPolicy(asset, entry.OwnerId);
                if (!entry.FieldId.EndsWith(
                        "/duration-seconds",
                        StringComparison.Ordinal) ||
                    policy.DirectPlayerRule == null)
                {
                    throw new InvalidOperationException(
                        $"Pose Inertialization Policy field '{entry.FieldId}' is unsupported.");
                }
                return CharacterPoseTuningValue.Float(
                    policy.DirectPlayerRule.DurationSeconds);
            }
            if (entry.OwnerId.StartsWith("pose-node:", StringComparison.Ordinal))
            {
                string nodeId = entry.OwnerId.Substring("pose-node:".Length);
                CharacterTypedPoseNode node = asset.EnumerateGraphs()
                    .Where(candidate => candidate != null)
                    .SelectMany(candidate => candidate.Nodes)
                    .Single(candidate => candidate != null &&
                                         candidate.NodeId.Value == nodeId);
                string fieldId = entry.FieldId.Substring(entry.OwnerId.Length + 1);
                return ToTuningValue(
                    entry,
                    CharacterPoseAuthoringPayloadCodec.Read(
                        node.Payload,
                        fieldId));
            }
            if (entry.OwnerId.StartsWith(
                    "pose-state-machine:",
                    StringComparison.Ordinal))
            {
                string machineId = entry.OwnerId.Substring(
                    "pose-state-machine:".Length);
                const string marker = "/transition:";
                int markerIndex = entry.FieldId.IndexOf(
                    marker,
                    StringComparison.Ordinal);
                int valueStart = markerIndex + marker.Length;
                int valueEnd = entry.FieldId.IndexOf('/', valueStart);
                if (markerIndex < 0 || valueEnd <= valueStart ||
                    !entry.FieldId.EndsWith("/duration", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Pose StateMachine tuning field '{entry.FieldId}' is unsupported.");
                }
                string transitionId = entry.FieldId.Substring(
                    valueStart,
                    valueEnd - valueStart);
                CharacterPoseStateTransition transition = asset
                    .EnumerateStateMachines()
                    .Single(candidate => candidate != null &&
                                         candidate.StateMachineId.Value == machineId)
                    .Transitions
                    .Single(candidate => candidate != null &&
                                         candidate.TransitionId.Value == transitionId);
                return CharacterPoseTuningValue.Float(
                    transition.DurationSeconds);
            }
            throw new InvalidOperationException(
                $"Pose tuning owner '{entry.OwnerId}' is not registered.");
        }

        static CharacterPoseTuningValue ReadFullBodyIkValue(
            CharacterFullBodyIkProfile profile,
            string fieldPath)
        {
            if (!profile)
                throw new InvalidOperationException("Full Body IK Profile tuning owner is missing.");
            if (fieldPath.StartsWith("left-arm/", StringComparison.Ordinal))
                return ReadFullBodyIkLimbValue(profile.LeftArm, fieldPath.Substring("left-arm/".Length));
            if (fieldPath.StartsWith("right-arm/", StringComparison.Ordinal))
                return ReadFullBodyIkLimbValue(profile.RightArm, fieldPath.Substring("right-arm/".Length));
            if (fieldPath.StartsWith("left-leg/", StringComparison.Ordinal))
                return ReadFullBodyIkLimbValue(profile.LeftLeg, fieldPath.Substring("left-leg/".Length));
            if (fieldPath.StartsWith("right-leg/", StringComparison.Ordinal))
                return ReadFullBodyIkLimbValue(profile.RightLeg, fieldPath.Substring("right-leg/".Length));
            return fieldPath switch
            {
                "iterations" => CharacterPoseTuningValue.Integer(profile.Iterations),
                "fabrik-pass" => CharacterPoseTuningValue.Boolean(profile.FabrikPass),
                "spine-stiffness" => CharacterPoseTuningValue.Float(profile.SpineStiffness),
                "pull-body-vertical" => CharacterPoseTuningValue.Float(profile.PullBodyVertical),
                "pull-body-horizontal" => CharacterPoseTuningValue.Float(profile.PullBodyHorizontal),
                "node-weight" => CharacterPoseTuningValue.Float(profile.NodeWeight),
                _ => throw new InvalidOperationException($"Full Body IK tuning field '{fieldPath}' is not declared.")
            };
        }

        static CharacterPoseTuningValue ReadFullBodyIkLimbValue(
            CharacterFullBodyIkLimbSettings limb,
            string fieldPath)
        {
            if (limb == null)
                throw new InvalidOperationException("Full Body IK limb tuning owner is missing.");
            return fieldPath switch
            {
                "pin" => CharacterPoseTuningValue.Float(limb.Pin),
                "pull" => CharacterPoseTuningValue.Float(limb.Pull),
                "push" => CharacterPoseTuningValue.Float(limb.Push),
                "push-parent" => CharacterPoseTuningValue.Float(limb.PushParent),
                "reach" => CharacterPoseTuningValue.Float(limb.Reach),
                "reach-smoothing" => CharacterPoseTuningValue.Enum((int)limb.ReachSmoothing),
                "push-smoothing" => CharacterPoseTuningValue.Enum((int)limb.PushSmoothing),
                "mapping-weight" => CharacterPoseTuningValue.Float(limb.MappingWeight),
                "maintain-rotation-weight" => CharacterPoseTuningValue.Float(limb.MaintainRotationWeight),
                "bend-constraint-weight" => CharacterPoseTuningValue.Float(limb.BendConstraintWeight),
                "bend-clamp" => CharacterPoseTuningValue.Float(limb.BendClamp),
                _ => throw new InvalidOperationException($"Full Body IK limb tuning field '{fieldPath}' is not declared.")
            };
        }

        static CharacterPoseTuningValue ReadFootPlacementValue(
            CharacterFootPlacementProfile profile,
            string fieldPath)
        {
            if (!profile)
                throw new InvalidOperationException("Foot Placement Profile tuning owner is missing.");
            if (string.Equals(
                    fieldPath,
                    "landing-prediction/hit-capacity",
                    StringComparison.Ordinal))
            {
                return CharacterPoseTuningValue.Integer(
                    profile.LandingPrediction.Build().HitCapacity);
            }
            throw new InvalidOperationException($"Foot Placement tuning field '{fieldPath}' is not declared.");
        }

        static void ApplyPoseNode(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile,
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            string nodeId = entry.OwnerId.Substring("pose-node:".Length);
            CharacterTypedPoseGraph graph = asset.EnumerateGraphs()
                .Where(candidate => candidate != null)
                .SingleOrDefault(candidate => candidate.Nodes.Any(node =>
                    node != null && node.NodeId.Value == nodeId));
            if (graph == null)
                throw new InvalidOperationException($"Pose node '{nodeId}' is not in the current Pose Graph asset.");
            string fieldId = entry.FieldId.Substring(entry.OwnerId.Length + 1);
            var owner = new CharacterPoseGraphAssetMutationOwner(asset, profile);
            var document = new CharacterTypedPoseGraphDocument(
                owner,
                graph.GraphId.Value,
                ResolveRole(asset, graph),
                graph.GraphId.Value);
            var mutation = new CharacterTypedPoseGraphMutationAdapter();
            mutation.Apply(
                document,
                new GraphAuthoringMutationRequest(
                    GraphAuthoringMutationKind.SetField,
                    new GraphAuthoringElementId(nodeId),
                fieldId: new GraphAuthoringFieldId(fieldId),
                value: ToAuthoringValue(value)));
        }

        static GraphAuthoringDocumentRoleId ResolveRole(
            CharacterPresentationPoseGraphAsset asset,
            CharacterTypedPoseGraph graph)
        {
            if (ReferenceEquals(graph, asset.Graph))
                return CharacterPoseGraphAuthoringCapabilities.RootGraph;
            bool stateOwned = asset.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Nodes)
                .Select(value => value?.Payload)
                .OfType<CharacterPoseStateMachineNodePayload>()
                .Any(value => value.StateMachine != null &&
                              value.StateMachine.States.Any(state =>
                                  state.PoseGraphId == graph.GraphId));
            return stateOwned
                ? CharacterPoseGraphAuthoringCapabilities.StatePoseGraph
                : CharacterPoseGraphAuthoringCapabilities.Subgraph;
        }

        static void ApplyTransition(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile,
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            const string prefix = "pose-state-machine:";
            string ownerId = entry.OwnerId.Substring(prefix.Length);
            string transitionMarker = "/transition:";
            int markerIndex = entry.FieldId.IndexOf(transitionMarker, StringComparison.Ordinal);
            if (markerIndex < 0)
                throw new InvalidOperationException($"Pose StateMachine tuning field '{entry.FieldId}' has no transition identity.");
            int transitionStart = markerIndex + transitionMarker.Length;
            int separator = entry.FieldId.IndexOf('/', transitionStart);
            if (separator <= transitionStart || !entry.FieldId.EndsWith("/duration", StringComparison.Ordinal))
                throw new InvalidOperationException($"Pose StateMachine tuning field '{entry.FieldId}' is derived or unsupported.");
            if (value.Kind != CharacterPoseTuningValueKind.Float)
                throw new InvalidOperationException($"Pose StateMachine tuning field '{entry.FieldId}' requires a float.");
            string transitionId = entry.FieldId.Substring(transitionStart, separator - transitionStart);
            var owner = new CharacterPoseGraphAssetMutationOwner(asset, profile);
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Edit Pose Transition Duration");
            transaction.Add(new SetPoseTransitionFieldMutation(
                ownerId,
                new PoseStateTransitionId(transitionId),
                "duration-seconds",
                value.FloatValue));
            new CharacterPresentationMutationService().Apply(owner, transaction);
        }

        static void ApplyProfile(
            CharacterFullBodyIkProfile profile,
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            if (!profile)
                throw new InvalidOperationException($"Full Body IK Profile owner for '{entry.OwnerId}' is missing.");
            Undo.RegisterCompleteObjectUndo(profile, "Edit Full Body IK Tuning");
            profile.ApplyTuning(entry.FieldId.Substring(entry.OwnerId.Length + 1), value);
            EditorUtility.SetDirty(profile);
        }

        static void ApplyProfile(
            CharacterFootPlacementProfile profile,
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            if (!profile)
                throw new InvalidOperationException($"Foot Placement Profile owner for '{entry.OwnerId}' is missing.");
            Undo.RegisterCompleteObjectUndo(profile, "Edit Foot Placement Tuning");
            profile.ApplyTuning(entry.FieldId.Substring(entry.OwnerId.Length + 1), value);
            EditorUtility.SetDirty(profile);
        }

        static void ApplyPolicy(
            CharacterAnimationBlendPolicy policy,
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            if (!policy)
                throw new InvalidOperationException(
                    $"Animation Blend Policy owner for '{entry.OwnerId}' is missing.");
            Undo.RegisterCompleteObjectUndo(
                policy,
                "Edit Animation Blend Stack Tuning");
            policy.StackPolicy.ApplyTuning(
                entry.FieldId.Substring(entry.OwnerId.Length + 1),
                value);
            EditorUtility.SetDirty(policy);
        }

        static void ApplyPolicy(
            CharacterPoseInertializationPolicy policy,
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            if (!policy || policy.DirectPlayerRule == null)
                throw new InvalidOperationException(
                    $"Pose Inertialization Policy owner for '{entry.OwnerId}' is missing.");
            if (!entry.FieldId.EndsWith(
                    "/duration-seconds",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Pose Inertialization Policy field '{entry.FieldId}' is not tunable.");
            }
            Undo.RegisterCompleteObjectUndo(
                policy,
                "Edit Pose Inertialization Tuning");
            policy.DirectPlayerRule.ApplyDurationTuning(value);
            EditorUtility.SetDirty(policy);
        }

        static CharacterFullBodyIkProfile FindFullBodyIkProfile(
            CharacterPresentationPoseGraphAsset asset,
            string ownerId)
        {
            return asset.EnumerateGraphs()
                .Where(graph => graph != null)
                .SelectMany(graph => graph.Nodes)
                .Where(node => node?.Payload is CharacterFullBodyIkPosePayload)
                .Select(node => ((CharacterFullBodyIkPosePayload)node.Payload).Profile)
                .Where(profile => profile && $"full-body-ik-profile:{profile.ProfileId}" == ownerId)
                .Distinct()
                .SingleOrDefault();
        }

        static CharacterFootPlacementProfile FindFootPlacementProfile(
            CharacterPresentationPoseGraphAsset asset,
            string ownerId)
        {
            return asset.EnumerateGraphs()
                .Where(graph => graph != null)
                .SelectMany(graph => graph.Nodes)
                .Where(node => node?.Payload is CharacterFootPlacementPosePayload)
                .Select(node => ((CharacterFootPlacementPosePayload)node.Payload).Profile)
                .Where(profile => profile && $"foot-placement-profile:{profile.ProfileId}" == ownerId)
                .Distinct()
                .SingleOrDefault();
        }

        static CharacterAnimationBlendPolicy FindBlendPolicy(
            CharacterPresentationPoseGraphAsset asset,
            string ownerId)
        {
            return asset.EnumerateGraphs()
                .Where(graph => graph != null)
                .SelectMany(graph => graph.Nodes)
                .Select(node => node?.Payload switch
                {
                    CharacterAnimationSlotPosePayload payload =>
                        payload.BlendPolicy,
                    CharacterBlendStackPosePayload payload =>
                        payload.BlendPolicy,
                    CharacterMotionMatchingPosePayload payload =>
                        payload.JumpBlendPolicy,
                    _ => null
                })
                .Where(policy => policy &&
                    $"animation-blend-policy:{policy.PolicyId}" == ownerId)
                .Distinct()
                .SingleOrDefault();
        }

        static CharacterPoseInertializationPolicy FindInertializationPolicy(
            CharacterPresentationPoseGraphAsset asset,
            string ownerId)
        {
            return asset.EnumerateGraphs()
                .Where(graph => graph != null)
                .SelectMany(graph => graph.Nodes)
                .Select(node =>
                    (node?.Payload as CharacterInertializationPosePayload)?.Policy)
                .Where(policy => policy &&
                    $"pose-inertialization-policy:{policy.PolicyId}" == ownerId)
                .Distinct()
                .SingleOrDefault();
        }

        static object ToAuthoringValue(CharacterPoseTuningValue value) => value.Kind switch
        {
            CharacterPoseTuningValueKind.Float => value.FloatValue,
            CharacterPoseTuningValueKind.Integer => value.IntegerValue,
            CharacterPoseTuningValueKind.Boolean => value.BooleanValue,
            CharacterPoseTuningValueKind.Enum => value.EnumValue,
            _ => throw new InvalidOperationException("Pose tuning value kind is invalid.")
        };

        static CharacterPoseTuningValue ToTuningValue(
            CharacterPoseTuningLayoutEntry entry,
            object value) => entry.ValueKind switch
        {
            CharacterPoseTuningValueKind.Float =>
                CharacterPoseTuningValue.Float(Convert.ToSingle(value)),
            CharacterPoseTuningValueKind.Integer =>
                CharacterPoseTuningValue.Integer(Convert.ToInt32(value)),
            CharacterPoseTuningValueKind.Boolean =>
                CharacterPoseTuningValue.Boolean(Convert.ToBoolean(value)),
            CharacterPoseTuningValueKind.Enum =>
                CharacterPoseTuningValue.Enum(Convert.ToInt32(value)),
            _ => throw new InvalidOperationException(
                $"Pose tuning field '{entry.FieldId}' has an invalid value kind.")
        };
    }
}
