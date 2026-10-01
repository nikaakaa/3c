using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeSourceResourceCatalog
    {
        readonly CharacterAnimationRigPayload m_Rig;
        readonly Dictionary<int, CharacterPresentationPoseSourcePlan> m_Plans;
        readonly Dictionary<AnimationClip, CharacterActionAnimationSourcePlan> m_ActionPlans;
        readonly Dictionary<(int ResourceIndex, int ClipIndex), CharacterActionAnimationSourcePlan> m_AclActionPlans;
        readonly Dictionary<int, CharacterAnimationCompiledResourceDescriptor> m_Descriptors;
        readonly Dictionary<int, CharacterPoseFootMotionSource> m_PlanFootMotion;
        readonly Dictionary<CharacterActionAnimationSourcePlan, CharacterPoseFootMotionSource> m_ActionFootMotion;
        readonly string[] m_FootMotionSourceNames;

        internal CharacterPoseNativeSourceResourceCatalog(
            CharacterAnimationRigPayload rig,
            CharacterAnimationResourceScope resourceScope,
            IReadOnlyList<CharacterPresentationPoseSourcePlan> sourcePlans,
            IReadOnlyList<CharacterActionAnimationSourcePlan> actionSourcePlans,
            IReadOnlyList<CharacterAnimationCompiledResourceDescriptor> resourceDescriptors)
        {
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            if (resourceScope == null)
                throw new ArgumentNullException(nameof(resourceScope));
            m_Plans = BuildIndex(sourcePlans, value => value.SourceIndex.Value);
            m_ActionPlans = BuildActionIndex(actionSourcePlans);
            m_AclActionPlans = new Dictionary<(int, int), CharacterActionAnimationSourcePlan>(m_ActionPlans.Count);
            m_Descriptors = BuildIndex(resourceDescriptors, value => value.ResourceIndex);
            m_PlanFootMotion = new Dictionary<int, CharacterPoseFootMotionSource>(m_Plans.Count);
            m_ActionFootMotion = new Dictionary<CharacterActionAnimationSourcePlan, CharacterPoseFootMotionSource>(m_ActionPlans.Count);
            m_FootMotionSourceNames = new string[m_Plans.Count + m_ActionPlans.Count];
            int footMotionIndex = 0;
            foreach (CharacterPresentationPoseSourcePlan plan in m_Plans.Values)
            {
                plan.RequireValid();
                m_FootMotionSourceNames[footMotionIndex] = plan.DisplayName.Trim();
                m_PlanFootMotion.Add(plan.SourceIndex.Value, new CharacterPoseFootMotionSource(
                    footMotionIndex++, (ulong)plan.ContentRevision.GetHashCode(), plan.FootStepObservation));
                if (!string.Equals(plan.RigId, m_Rig.RigId, StringComparison.Ordinal) ||
                    !string.Equals(plan.RigRevision, m_Rig.RigRevision, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Pose source plan '{plan.SourceIndex}' Rig identity is stale.");
                if (plan.Backend == CharacterAnimationSamplingBackendKind.Acl &&
                    !m_Descriptors.ContainsKey(plan.ResourceCatalogIndex))
                    throw new InvalidOperationException($"Pose source plan '{plan.SourceIndex}' ACL resource binding is missing.");
            }
            foreach (KeyValuePair<int, CharacterAnimationCompiledResourceDescriptor> pair in m_Descriptors)
            {
                if (pair.Key != pair.Value.ResourceIndex)
                    throw new InvalidOperationException("Pose ACL resource catalog indexes are not contiguous.");
                pair.Value.RequireValid();
                resourceScope.Request(resourceScope.Register(pair.Value));
            }
            foreach (CharacterActionAnimationSourcePlan plan in m_ActionPlans.Values)
            {
                plan.RequireValid();
                m_FootMotionSourceNames[footMotionIndex] = plan.ClipIdentity.Trim();
                m_ActionFootMotion.Add(plan, new CharacterPoseFootMotionSource(
                    footMotionIndex++, (ulong)plan.FullDependencyHash.GetHashCode(), plan.FootStepObservation));
                if (plan.Backend != CharacterAnimationSamplingBackendKind.Acl)
                    continue;
                CharacterAnimationCompiledResourceDescriptor descriptor =
                    RequireDescriptor(plan.ResourceCatalogIndex);
                CharacterAclAnimationResourceManifest manifest =
                    descriptor.RequireManifest(plan.GroupClipIndex);
                if (!string.Equals(
                        manifest.FormalClipIdentity,
                        plan.ClipIdentity,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        manifest.SourceDependencyHash,
                        plan.FullDependencyHash,
                        StringComparison.Ordinal) ||
                    !string.Equals(manifest.RigId, m_Rig.RigId, StringComparison.Ordinal) ||
                    !string.Equals(manifest.RigRevision, m_Rig.RigRevision, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Action animation source plan '{plan.ClipIdentity}' does not match its ACL manifest.");
                }
                m_AclActionPlans.TryAdd((plan.ResourceCatalogIndex, plan.GroupClipIndex), plan);
            }
        }

        internal string RequireFootMotionSourceName(int index) => m_FootMotionSourceNames[index];

        internal CharacterPoseFootMotionSource ResolveFootMotion(
            in AnimationPoseSourceId sourceId,
            in ClipSamplePlan clipSample) =>
            sourceId.SourceKind == AnimationPoseSourceKind.Timeline
                ? m_ActionFootMotion[RequireActionPlan(in clipSample)]
                : m_PlanFootMotion[sourceId.PresentationPoseSourceIndex.Value];

        internal CharacterPresentationPoseSourcePlan RequirePlan(PresentationPoseSourceIndex sourceIndex)
        {
            if (!sourceIndex.IsValid || !m_Plans.TryGetValue(sourceIndex.Value, out CharacterPresentationPoseSourcePlan plan))
                throw new InvalidOperationException($"Pose source plan '{sourceIndex}' is not registered.");
            return plan;
        }

        internal CharacterAnimationCompiledResourceDescriptor RequireDescriptor(int resourceCatalogIndex)
        {
            if (!m_Descriptors.TryGetValue(resourceCatalogIndex, out CharacterAnimationCompiledResourceDescriptor descriptor))
                throw new InvalidOperationException($"Pose ACL resource '{resourceCatalogIndex}' is not registered.");
            return descriptor;
        }

        internal CharacterActionAnimationSourcePlan RequireActionPlan(
            in ClipSamplePlan sample)
        {
            if (!sample.IsAcl)
                return RequireActionPlan(sample.ClipRef);
            if (m_AclActionPlans.TryGetValue(
                    (sample.ResourceCatalogIndexRef, sample.GroupClipIndexRef),
                    out CharacterActionAnimationSourcePlan plan))
                return plan;
            throw new InvalidOperationException("Action ACL sample has no compiled source plan.");
        }

        internal CharacterActionAnimationSourcePlan RequireActionPlan(
            AnimationClip authoringClipIdentity)
        {
            if (!authoringClipIdentity ||
                !m_ActionPlans.TryGetValue(
                    authoringClipIdentity,
                    out CharacterActionAnimationSourcePlan plan))
            {
                throw new InvalidOperationException(
                    $"Action animation Clip '{authoringClipIdentity?.name ?? "missing"}' has no compiled source plan.");
            }
            return plan;
        }

        internal bool HasActionPlan(AnimationClip authoringClipIdentity) =>
            m_ActionPlans.ContainsKey(authoringClipIdentity);

        static Dictionary<int, TValue> BuildIndex<TValue>(
            IReadOnlyList<TValue> values,
            Func<TValue, int> keySelector)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            var result = new Dictionary<int, TValue>();
            for (int i = 0; i < values.Count; i++)
            {
                TValue value = values[i] ?? throw new ArgumentException($"Pose source resource #{i} is missing.", nameof(values));
                if (!result.TryAdd(keySelector(value), value))
                    throw new InvalidOperationException("Pose source resource identity is duplicated.");
            }
            return result;
        }

        static Dictionary<AnimationClip, CharacterActionAnimationSourcePlan>
            BuildActionIndex(
                IReadOnlyList<CharacterActionAnimationSourcePlan> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            var result = new Dictionary<AnimationClip, CharacterActionAnimationSourcePlan>();
            for (int i = 0; i < values.Count; i++)
            {
                CharacterActionAnimationSourcePlan value = values[i] ??
                    throw new ArgumentException(
                        $"Action animation source resource #{i} is missing.",
                        nameof(values));
                if (!result.TryAdd(value.AuthoringClipIdentity, value))
                    throw new InvalidOperationException(
                        "Action animation source identity is duplicated.");
            }
            return result;
        }
    }

    internal sealed class CharacterPoseNativeFootPlacementResource
    {
        readonly CharacterFootPlacementProfile m_Profile;
        readonly CharacterFootPlacementRigCalibration m_Calibration;
        readonly CharacterWorldAwarePresentationBinding m_World;
        readonly ICharacterFutureBodyTranslationSource m_FutureBodyTranslationSource;
        readonly ICharacterFootPlacementWorldQuery m_WorldQuery;

        internal CharacterPoseNativeFootPlacementResource(
            CharacterFootPlacementProfile profile,
            CharacterFootPlacementRigCalibration calibration,
            CharacterWorldAwarePresentationBinding world,
            ICharacterFutureBodyTranslationSource futureBodyTranslationSource,
            ICharacterFootPlacementWorldQuery worldQuery)
        {
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            m_Calibration = calibration ? calibration : throw new ArgumentNullException(nameof(calibration));
            m_World = world ? world : throw new ArgumentNullException(nameof(world));
            m_FutureBodyTranslationSource = futureBodyTranslationSource;
            m_WorldQuery = worldQuery ?? throw new ArgumentNullException(nameof(worldQuery));
            m_Profile.RequireValid();
            m_Calibration.RequireValid();
        }

        internal string PosePlanHash { get; private set; }

        internal CharacterFootPlacementModule CreateModule(
            ActorId actorId,
            CharacterAnimationRigPayload rig,
            CharacterAnimationRigBinding rigBinding,
            string posePlanHash)
        {
            if (string.IsNullOrWhiteSpace(posePlanHash))
                throw new ArgumentException("Pose Plan identity is missing.", nameof(posePlanHash));
            PosePlanHash = posePlanHash.Trim();
            var poseRig = new CharacterFootPlacementPoseRig(
                m_Calibration,
                rig,
                rigBinding,
                m_World);
            var settings = new CharacterFootPlacementModuleSettings(
                m_Profile.ProfileId,
                m_Profile.Revision,
                PosePlanHash,
                m_Profile.CurrentSupportQuery.Build(),
                m_Profile.LandingPrediction.Build(),
                m_Profile.GroundDetection.Build(),
                m_Profile.FootMotion.Build());
            return new CharacterFootPlacementModule(
                actorId,
                settings,
                poseRig,
                m_FutureBodyTranslationSource,
                m_WorldQuery);
        }
    }

    internal sealed class CharacterPoseNativeConstraintResourceCatalog : IDisposable
    {
        readonly NativeArray<CharacterPoseBoneIkGoalDescriptor> m_PoseBoneDescriptors;
        readonly NativeArray<int> m_GoalAssemblerContributions;

        internal CharacterPoseNativeConstraintResourceCatalog(
            IReadOnlyList<CharacterPoseBoneIkGoalDescriptor> poseBoneDescriptors,
            IReadOnlyList<int> goalAssemblerContributions,
            int goalSetValueCount)
        {
            if (poseBoneDescriptors == null)
                throw new ArgumentNullException(nameof(poseBoneDescriptors));
            if (goalAssemblerContributions == null)
                throw new ArgumentNullException(nameof(goalAssemblerContributions));
            if (goalSetValueCount < 0)
                throw new ArgumentOutOfRangeException(nameof(goalSetValueCount));
            m_PoseBoneDescriptors = new NativeArray<CharacterPoseBoneIkGoalDescriptor>(
                poseBoneDescriptors.Count,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_GoalAssemblerContributions = new NativeArray<int>(
                goalAssemblerContributions.Count,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            try
            {
                for (int i = 0; i < poseBoneDescriptors.Count; i++)
                {
                    if (!poseBoneDescriptors[i].IsValid)
                        throw new ArgumentException($"Pose Bone IK Goal descriptor #{i} is invalid.");
                    m_PoseBoneDescriptors[i] = poseBoneDescriptors[i];
                }
                for (int i = 0; i < goalAssemblerContributions.Count; i++)
                {
                    if (goalAssemblerContributions[i] < 0)
                        throw new ArgumentException($"Goal Assembler contribution #{i} is invalid.");
                    m_GoalAssemblerContributions[i] = goalAssemblerContributions[i];
                }
                PoseBoneContributions = new CharacterPoseBoneContributionCatalog(m_PoseBoneDescriptors);
                GoalAssemblers = new CharacterFullBodyIkGoalAssemblerCatalog(
                    m_GoalAssemblerContributions,
                    goalSetValueCount);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal CharacterPoseBoneContributionCatalog PoseBoneContributions { get; }
        internal CharacterFullBodyIkGoalAssemblerCatalog GoalAssemblers { get; }

        public void Dispose()
        {
            if (m_PoseBoneDescriptors.IsCreated)
                m_PoseBoneDescriptors.Dispose();
            if (m_GoalAssemblerContributions.IsCreated)
                m_GoalAssemblerContributions.Dispose();
        }
    }

    internal sealed class CharacterPoseNativeLinkedPoseImplementation
    {
        internal CharacterPoseNativeLinkedPoseImplementation(
            LinkedPoseGroupId groupId,
            CharacterLinkedPoseImplementationAsset implementation)
        {
            GroupId = groupId.IsValid ? groupId : throw new ArgumentException("Linked Pose Group identity is invalid.", nameof(groupId));
            Implementation = implementation ? implementation : throw new ArgumentNullException(nameof(implementation));
            Implementation.RequireValid();
        }

        internal LinkedPoseGroupId GroupId { get; }
        internal CharacterLinkedPoseImplementationAsset Implementation { get; }
    }

    internal sealed class CharacterPoseNativeManagedSourceResourceCatalog : IDisposable
    {
        readonly CharacterAnimationRigPayload m_Rig;
        readonly Dictionary<string, CharacterPoseNativeLinkedPoseImplementation> m_LinkedPoseImplementations;
        readonly Dictionary<string, CharacterMotionMatchingRuntimeDatabase> m_MotionMatchingDatabases;
        readonly Dictionary<string, RootMotionCurveAsset> m_RootOrientationCurves;

        internal CharacterPoseNativeManagedSourceResourceCatalog(
            CharacterAnimationRigPayload rig,
            IReadOnlyList<CharacterPoseNativeLinkedPoseImplementation> linkedPoseImplementations,
            IReadOnlyList<CharacterMotionMatchingRuntimeDatabase> motionMatchingDatabases,
            IReadOnlyList<RootMotionCurveAsset> rootOrientationCurves)
        {
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_LinkedPoseImplementations = BuildIndex(
                linkedPoseImplementations,
                value => value.GroupId.Value);
            m_MotionMatchingDatabases = BuildIndex(
                motionMatchingDatabases,
                value => value.ArtifactIdentity.ContentHash.ToString());
            m_RootOrientationCurves = BuildIndex(
                rootOrientationCurves,
                value => value.name);
        }

        internal CharacterPoseNativeStateMachineSource CreateStateMachine(
            CharacterPoseCanvasNode node,
            in CharacterPoseNativePreparedBinding preparedBinding,
            ICharacterPoseNativeNodeHandlerFactory factory,
            int contributionCapacity) =>
            new CharacterPoseNativeStateMachineSource(
                node,
                in preparedBinding,
                preparedBinding.Profile,
                factory,
                contributionCapacity);

        internal CharacterPoseNativeLinkedPoseImplementation RequireLinkedPose(LinkedPoseGroupId groupId)
        {
            if (!groupId.IsValid || !m_LinkedPoseImplementations.TryGetValue(groupId.Value, out CharacterPoseNativeLinkedPoseImplementation implementation))
                throw new InvalidOperationException($"Linked Pose group '{groupId}' has no implementation.");
            return implementation;
        }

        internal CharacterMotionMatchingSelectionRuntime CreateMotionMatchingSelection(string artifactIdentity)
        {
            if (!m_MotionMatchingDatabases.TryGetValue(artifactIdentity, out CharacterMotionMatchingRuntimeDatabase database))
                throw new InvalidOperationException($"Motion Matching database '{artifactIdentity}' is not registered.");
            return new CharacterMotionMatchingSelectionRuntime(database);
        }

        internal MotionMatchingPoseSourceRuntime CreateMotionMatchingSource(string artifactIdentity)
        {
            if (!m_MotionMatchingDatabases.TryGetValue(artifactIdentity, out CharacterMotionMatchingRuntimeDatabase database))
                throw new InvalidOperationException($"Motion Matching database '{artifactIdentity}' is not registered.");
            return new MotionMatchingPoseSourceRuntime(database);
        }

        internal RootMotionCurveAsset RequireRootOrientationCurve(string curveName)
        {
            if (string.IsNullOrWhiteSpace(curveName) || !m_RootOrientationCurves.TryGetValue(curveName, out RootMotionCurveAsset curve))
                throw new InvalidOperationException($"Root Orientation curve '{curveName}' is not registered.");
            return curve;
        }

        static Dictionary<string, TValue> BuildIndex<TValue>(
            IReadOnlyList<TValue> values,
            Func<TValue, string> keySelector)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            var result = new Dictionary<string, TValue>(StringComparer.Ordinal);
            for (int i = 0; i < values.Count; i++)
            {
                TValue value = values[i] ?? throw new ArgumentException($"Managed Pose source resource #{i} is missing.", nameof(values));
                string key = keySelector(value);
                if (string.IsNullOrWhiteSpace(key) || !result.TryAdd(key, value))
                    throw new InvalidOperationException("Managed Pose source resource identity is missing or duplicated.");
            }
            return result;
        }

        public void Dispose()
        {
            foreach (CharacterMotionMatchingRuntimeDatabase database in m_MotionMatchingDatabases.Values)
                database.Dispose();
        }
    }
}
