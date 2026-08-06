using System;
using RootMotion.FinalIK;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterFullBodyIkFailure : byte
    {
        None = 0,
        NotPrepared = 1,
        InvalidPosePage = 2,
        InvalidGoalWorkspace = 3,
        GoalLineageMismatch = 4,
        DuplicateEffectorSlot = 5,
        UnsupportedPelvisGoal = 6,
        SolverFailure = 7,
        NonFiniteOutput = 8
    }

    public readonly struct CharacterFullBodyIkResult
    {
        CharacterFullBodyIkResult(
            CharacterFullBodyIkFailure failure,
            int failedGoalSetIndex,
            CharacterFullBodyIkEffectorSlot failedSlot,
            int appliedGoalCount)
        {
            Failure = failure;
            FailedGoalSetIndex = failedGoalSetIndex;
            FailedSlot = failedSlot;
            AppliedGoalCount = appliedGoalCount;
        }

        public CharacterFullBodyIkFailure Failure { get; }
        public int FailedGoalSetIndex { get; }
        public CharacterFullBodyIkEffectorSlot FailedSlot { get; }
        public int AppliedGoalCount { get; }
        public bool Succeeded => Failure == CharacterFullBodyIkFailure.None;

        internal static CharacterFullBodyIkResult Success(int appliedGoalCount) =>
            new CharacterFullBodyIkResult(CharacterFullBodyIkFailure.None, -1, default, appliedGoalCount);

        internal static CharacterFullBodyIkResult Fail(
            CharacterFullBodyIkFailure failure,
            int failedGoalSetIndex = -1,
            CharacterFullBodyIkEffectorSlot failedSlot = default) =>
            new CharacterFullBodyIkResult(failure, failedGoalSetIndex, failedSlot, 0);
    }

    public sealed class CharacterFinalIkFullBodySolver
    {
        readonly CharacterAnimationRigPayload m_Rig;
        readonly CharacterFullBodyIkProfile m_Profile;
        readonly CharacterFinalIkPoseBufferBackend m_Backend;
        readonly IndexedBipedReferences m_References;
        readonly IKSolverFullBodyBiped m_Solver = new IKSolverFullBodyBiped();
        readonly FixedString64Bytes m_RigId;
        readonly FixedString64Bytes m_RigRevision;
        readonly CharacterFullBodyIkEffectorDiagnostics[] m_DiagnosticEffectors =
            new CharacterFullBodyIkEffectorDiagnostics[CharacterFullBodyIkGoalSetHeader.MaximumGoalCount];
        readonly CharacterFullBodyIkLimbDiagnostics[] m_DiagnosticLimbs =
            new CharacterFullBodyIkLimbDiagnostics[4];
        CharacterFullBodyIkSolverDiagnostics m_Diagnostics;
        ActiveTuning m_ActiveTuning;
        Vector3 m_DiagnosticPelvisTranslation;
        Vector3 m_LeftFootPlantOffset;
        Vector3 m_RightFootPlantOffset;
        bool m_HasLeftFootPlantOffset;
        bool m_HasRightFootPlantOffset;
        ulong m_DiagnosticFrameSequence;
        int m_DiagnosticEffectorCount;
        bool m_Prepared;

        public CharacterFinalIkFullBodySolver(
            CharacterAnimationRigPayload rig,
            CharacterFullBodyIkProfile profile,
            NativeArray<int> parentIndices,
            NativeArray<CharacterVirtualBoneDescriptor> virtualBones)
        {
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Profile = profile ? profile : throw new ArgumentNullException(nameof(profile));
            m_Rig.RequireValid();
            m_Profile.RequireValid();
            m_Backend = new CharacterFinalIkPoseBufferBackend(rig, parentIndices, virtualBones);
            m_References = CharacterFinalIkPoseBufferBackend.CreateBipedReferences(rig);
            m_RigId = new FixedString64Bytes(rig.RigId);
            m_RigRevision = new FixedString64Bytes(rig.RigRevision);
            m_ActiveTuning = ActiveTuning.FromProfile(m_Profile);
            PrepareReferencePose(parentIndices);
        }

        public string BackendIdentity => CharacterFinalIkPoseBufferBackend.SourceIdentity;
        public string ProfileId => m_Profile.ProfileId;
        public string ProfileRevision => m_Profile.Revision;
        public bool IsPrepared => m_Prepared;
        public CharacterFullBodyIkSolverDiagnostics Diagnostics => m_Diagnostics;
        public int DiagnosticEffectorCount => m_DiagnosticEffectorCount;
        public int DiagnosticLimbCount => m_DiagnosticLimbs.Length;

        public CharacterFullBodyIkEffectorDiagnostics GetDiagnosticEffector(int index)
        {
            if ((uint)index >= (uint)m_DiagnosticEffectorCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_DiagnosticEffectors[index];
        }

        public CharacterFullBodyIkLimbDiagnostics GetDiagnosticLimb(int index)
        {
            if ((uint)index >= (uint)m_DiagnosticLimbs.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_DiagnosticLimbs[index];
        }

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            bool resetOwnerState)
        {
            if (layout == null || block == null)
                return "Full Body IK tuning payload is missing.";
            try
            {
                block.RequireValid(layout);
                var next = m_ActiveTuning;
                string ownerId = $"full-body-ik-profile:{m_Profile.ProfileId}";
                for (int i = 0; i < layout.Entries.Count; i++)
                {
                    CharacterPoseTuningLayoutEntry entry = layout.Entries[i];
                    if (!string.Equals(entry.OwnerId, ownerId, StringComparison.Ordinal) ||
                        entry.Interaction != CharacterPoseTuningInteractionPolicy.TunableDefault)
                        continue;
                    ApplyTuningField(ref next, entry, block.GetValue(entry));
                }
                next.RequireValid();
                m_ActiveTuning = next;
                ApplyProfile();
                if (resetOwnerState)
                    ResetEffectorsToPose();
                return string.Empty;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        public CharacterFullBodyIkResult Prepare(
            NativeSlice<AnimationLocalBonePose> referenceComponentPose)
        {
            if (!IsValidPosePage(referenceComponentPose))
                return CharacterFullBodyIkResult.Fail(CharacterFullBodyIkFailure.InvalidPosePage);
            try
            {
                m_Backend.Bind(referenceComponentPose);
                m_Solver.SetToIndexedReferences(m_Backend, m_References);
                ApplyProfile();
                ResetEffectorsToPose();
                m_Prepared = true;
                return CharacterFullBodyIkResult.Success(0);
            }
            catch (Exception)
            {
                m_Prepared = false;
                return CharacterFullBodyIkResult.Fail(CharacterFullBodyIkFailure.SolverFailure);
            }
        }

        public CharacterFullBodyIkResult Solve(
            NativeSlice<AnimationLocalBonePose> inputComponentPose,
            NativeSlice<AnimationLocalBonePose> pendingOutputComponentPose,
            NativeSlice<int> goalSetValueIndices,
            NativeArray<CharacterFullBodyIkGoalSetHeader> goalSets,
            NativeArray<CharacterFullBodyIkGoal> goalWorkspace,
            ulong frameSequence,
            ulong completionIdentity)
        {
            BeginDiagnostics(frameSequence);
            if (!m_Prepared)
                return CompleteDiagnostics(
                    CharacterFullBodyIkResult.Fail(CharacterFullBodyIkFailure.NotPrepared),
                    goalSetValueIndices,
                    goalSets,
                    goalWorkspace,
                    completionIdentity);
            if (!IsValidPosePage(inputComponentPose) ||
                pendingOutputComponentPose.Length != inputComponentPose.Length)
            {
                return CompleteDiagnostics(
                    CharacterFullBodyIkResult.Fail(CharacterFullBodyIkFailure.InvalidPosePage),
                    goalSetValueIndices,
                    goalSets,
                    goalWorkspace,
                    completionIdentity);
            }
            for (int i = 0; i < inputComponentPose.Length; i++)
                pendingOutputComponentPose[i] = inputComponentPose[i];

            try
            {
                m_Backend.Bind(pendingOutputComponentPose);
                CharacterFullBodyIkResult goalResult = ApplyGoals(
                    goalSetValueIndices,
                    goalSets,
                    goalWorkspace,
                    frameSequence,
                    completionIdentity);
                if (!goalResult.Succeeded)
                    return CompleteDiagnostics(
                        goalResult,
                        goalSetValueIndices,
                        goalSets,
                        goalWorkspace,
                        completionIdentity);
                m_Solver.Update();
                m_Backend.RebuildVirtualBones();
                if (!IsValidPosePage(pendingOutputComponentPose))
                {
                    return CompleteDiagnostics(
                        CharacterFullBodyIkResult.Fail(CharacterFullBodyIkFailure.NonFiniteOutput),
                        goalSetValueIndices,
                        goalSets,
                        goalWorkspace,
                        completionIdentity);
                }
                return CompleteDiagnostics(
                    goalResult,
                    goalSetValueIndices,
                    goalSets,
                    goalWorkspace,
                    completionIdentity);
            }
            catch (Exception)
            {
                return CompleteDiagnostics(
                    CharacterFullBodyIkResult.Fail(CharacterFullBodyIkFailure.SolverFailure),
                    goalSetValueIndices,
                    goalSets,
                    goalWorkspace,
                    completionIdentity);
            }
        }

        public void Reset()
        {
            if (!m_Prepared)
                return;
            for (int i = 0; i < m_Solver.effectors.Length; i++)
            {
                IKEffector effector = m_Solver.effectors[i];
                effector.target = null;
                effector.positionWeight = 0f;
                effector.rotationWeight = 0f;
                effector.positionOffset = Vector3.zero;
            }
            m_Diagnostics = default;
            m_DiagnosticPelvisTranslation = Vector3.zero;
            m_DiagnosticFrameSequence = 0;
            m_DiagnosticEffectorCount = 0;
            m_LeftFootPlantOffset = Vector3.zero;
            m_RightFootPlantOffset = Vector3.zero;
            m_HasLeftFootPlantOffset = false;
            m_HasRightFootPlantOffset = false;
            Array.Clear(m_DiagnosticEffectors, 0, m_DiagnosticEffectors.Length);
            Array.Clear(m_DiagnosticLimbs, 0, m_DiagnosticLimbs.Length);
        }

        void PrepareReferencePose(NativeArray<int> parentIndices)
        {
            var referencePose = new NativeArray<AnimationLocalBonePose>(
                m_Rig.PoseBoneCount,
                Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            try
            {
                for (int bone = 0; bone < referencePose.Length; bone++)
                {
                    AnimationLocalBonePose local = m_Rig.GetReferenceLocalPose(bone);
                    int parent = parentIndices[bone];
                    if (parent < 0)
                    {
                        referencePose[bone] = local;
                        continue;
                    }
                    AnimationLocalBonePose parentPose = referencePose[parent];
                    referencePose[bone] = new AnimationLocalBonePose(
                        parentPose.Position + parentPose.Rotation * Vector3.Scale(parentPose.Scale, local.Position),
                        parentPose.Rotation * local.Rotation,
                        Vector3.Scale(parentPose.Scale, local.Scale));
                }
                CharacterFullBodyIkResult result = Prepare(new NativeSlice<AnimationLocalBonePose>(referencePose));
                if (!result.Succeeded)
                    throw new InvalidOperationException($"FinalIK FBBIK reference preparation failed: {result.Failure}.");
            }
            finally
            {
                referencePose.Dispose();
            }
        }

        CharacterFullBodyIkResult ApplyGoals(
            NativeSlice<int> goalSetValueIndices,
            NativeArray<CharacterFullBodyIkGoalSetHeader> goalSets,
            NativeArray<CharacterFullBodyIkGoal> goalWorkspace,
            ulong frameSequence,
            ulong completionIdentity)
        {
            if (!goalSets.IsCreated || !goalWorkspace.IsCreated ||
                goalSetValueIndices.Length == 0)
                return CharacterFullBodyIkResult.Fail(CharacterFullBodyIkFailure.InvalidGoalWorkspace);
            ushort occupiedSlots = 0;
            int appliedGoalCount = 0;
            for (int setIndex = 0; setIndex < goalSetValueIndices.Length; setIndex++)
            {
                int valueIndex = goalSetValueIndices[setIndex];
                if ((uint)valueIndex >= (uint)goalSets.Length)
                    return CharacterFullBodyIkResult.Fail(CharacterFullBodyIkFailure.InvalidGoalWorkspace, setIndex);
                CharacterFullBodyIkGoalSetHeader header = goalSets[valueIndex];
                if (!header.IsValid ||
                    header.FrameSequence != frameSequence ||
                    header.CompletionIdentity != completionIdentity ||
                    !header.RigId.Equals(m_RigId) ||
                    !header.RigRevision.Equals(m_RigRevision) ||
                    header.GoalOffset > goalWorkspace.Length - header.GoalCount)
                {
                    return CharacterFullBodyIkResult.Fail(
                        CharacterFullBodyIkFailure.GoalLineageMismatch,
                        setIndex);
                }
                if (header.Availability != CharacterFullBodyIkGoalSetAvailability.Ready)
                    return CharacterFullBodyIkResult.Fail(
                        CharacterFullBodyIkFailure.InvalidGoalWorkspace,
                        setIndex);
                for (int localGoalIndex = 0; localGoalIndex < header.GoalCount; localGoalIndex++)
                {
                    CharacterFullBodyIkGoal goal = goalWorkspace[header.GoalOffset + localGoalIndex];
                    if (!goal.IsValid)
                    {
                        return CharacterFullBodyIkResult.Fail(
                            CharacterFullBodyIkFailure.InvalidGoalWorkspace,
                            setIndex,
                            goal.Slot);
                    }
                    int slotBit = 1 << ((int)goal.Slot - 1);
                    if ((occupiedSlots & slotBit) != 0)
                    {
                        return CharacterFullBodyIkResult.Fail(
                            CharacterFullBodyIkFailure.DuplicateEffectorSlot,
                            setIndex,
                            goal.Slot);
                    }
                    occupiedSlots = (ushort)(occupiedSlots | slotBit);
                    appliedGoalCount++;
                }
            }

            ushort identityPoseBoneSlots = CollectIdentityPoseBoneSlots(
                goalSetValueIndices,
                goalSets,
                goalWorkspace);
            CharacterFullBodyIkResult pelvisResult = ApplyPelvisGoal(
                goalSetValueIndices,
                goalSets,
                goalWorkspace);
            if (!pelvisResult.Succeeded)
                return pelvisResult;
            ApplyGroundingPreSolveRotations(
                goalSetValueIndices,
                goalSets,
                goalWorkspace);
            ResetEffectorsToPose();
            for (int setIndex = 0; setIndex < goalSetValueIndices.Length; setIndex++)
            {
                CharacterFullBodyIkGoalSetHeader header = goalSets[goalSetValueIndices[setIndex]];
                for (int localGoalIndex = 0; localGoalIndex < header.GoalCount; localGoalIndex++)
                {
                    CharacterFullBodyIkGoal goal = goalWorkspace[header.GoalOffset + localGoalIndex];
                    if (goal.Slot == CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation)
                        continue;
                    int slotBit = 1 << ((int)goal.Slot - 1);
                    if ((identityPoseBoneSlots & slotBit) != 0)
                        continue;
                    ApplyEffectorGoal(goal);
                }
            }
            return CharacterFullBodyIkResult.Success(appliedGoalCount);
        }

        CharacterFullBodyIkResult ApplyPelvisGoal(
            NativeSlice<int> goalSetValueIndices,
            NativeArray<CharacterFullBodyIkGoalSetHeader> goalSets,
            NativeArray<CharacterFullBodyIkGoal> goalWorkspace)
        {
            for (int setIndex = 0; setIndex < goalSetValueIndices.Length; setIndex++)
            {
                CharacterFullBodyIkGoalSetHeader header = goalSets[goalSetValueIndices[setIndex]];
                for (int localGoalIndex = 0; localGoalIndex < header.GoalCount; localGoalIndex++)
                {
                    CharacterFullBodyIkGoal goal = goalWorkspace[header.GoalOffset + localGoalIndex];
                    if (goal.Slot != CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation)
                        continue;
                    if (goal.Application != CharacterFullBodyIkGoalApplication.PelvisPreSolveTranslation)
                    {
                        return CharacterFullBodyIkResult.Fail(
                            CharacterFullBodyIkFailure.UnsupportedPelvisGoal,
                            setIndex,
                            goal.Slot);
                    }
                    IndexedBoneHandle pelvis = new IndexedBoneHandle(m_Rig.PelvisPhysicalBoneIndex);
                    m_Backend.SetComponentPosition(
                        pelvis,
                        m_Backend.GetComponentPosition(pelvis) + goal.ComponentPosition * goal.PositionWeight);
                    m_DiagnosticPelvisTranslation = goal.ComponentPosition * goal.PositionWeight;
                }
            }
            return CharacterFullBodyIkResult.Success(0);
        }

        void ApplyEffectorGoal(CharacterFullBodyIkGoal goal)
        {
            IKEffector effector = m_Solver.GetEffector(ToFinalIkEffector(goal.Slot));
            switch (goal.Application)
            {
                case CharacterFullBodyIkGoalApplication.AbsoluteEffectorTarget:
                    effector.position = goal.ComponentPosition;
                    effector.rotation = goal.ComponentRotation;
                    effector.positionWeight = goal.PositionWeight;
                    effector.rotationWeight = goal.RotationWeight;
                    break;
                case CharacterFullBodyIkGoalApplication.GroundingEffectorTarget:
                    if (TryGetPlantOffset(goal.Slot, out Vector3 plantOffset))
                        effector.positionOffset += plantOffset;
                    else
                        effector.positionOffset +=
                            (goal.ComponentPosition - m_Backend.GetComponentPosition(effector.boneHandle)) *
                            goal.PositionWeight;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported effector goal application {goal.Application}.");
            }
        }

        void ApplyGroundingPreSolveRotations(
            NativeSlice<int> goalSetValueIndices,
            NativeArray<CharacterFullBodyIkGoalSetHeader> goalSets,
            NativeArray<CharacterFullBodyIkGoal> goalWorkspace)
        {
            m_LeftFootPlantOffset = Vector3.zero;
            m_RightFootPlantOffset = Vector3.zero;
            m_HasLeftFootPlantOffset = false;
            m_HasRightFootPlantOffset = false;
            for (int setIndex = 0; setIndex < goalSetValueIndices.Length; setIndex++)
            {
                CharacterFullBodyIkGoalSetHeader header = goalSets[goalSetValueIndices[setIndex]];
                for (int localGoalIndex = 0; localGoalIndex < header.GoalCount; localGoalIndex++)
                {
                    CharacterFullBodyIkGoal goal = goalWorkspace[header.GoalOffset + localGoalIndex];
                    if (goal.Application != CharacterFullBodyIkGoalApplication.GroundingEffectorTarget ||
                        goal.RotationWeight <= CharacterPoseConstraintMath.Epsilon)
                    {
                        continue;
                    }
                    IKEffector effector = m_Solver.GetEffector(ToFinalIkEffector(goal.Slot));
                    Vector3 currentPosition = m_Backend.GetComponentPosition(effector.boneHandle);
                    Quaternion current = m_Backend.GetComponentRotation(effector.boneHandle);
                    Quaternion offset = goal.ComponentRotation * Quaternion.Inverse(current);
                    Quaternion weightedOffset = Quaternion.Slerp(
                        Quaternion.identity,
                        offset,
                        goal.RotationWeight);
                    Quaternion weightedRotation = (weightedOffset * current).normalized;
                    if (goal.PlantPivotMode == CharacterFullBodyIkPlantPivotMode.Toe)
                    {
                        Vector3 ankleToPivot = Quaternion.Inverse(goal.ComponentRotation) *
                                               (goal.ComponentPlantPivot - goal.ComponentPosition);
                        Vector3 currentPivot = currentPosition + current * ankleToPivot;
                        Vector3 blendedPivot = Vector3.Lerp(
                            currentPivot,
                            goal.ComponentPlantPivot,
                            goal.PositionWeight);
                        Vector3 pivotedPosition = blendedPivot - weightedRotation * ankleToPivot;
                        Vector3 standardPosition = Vector3.Lerp(
                            currentPosition,
                            goal.ComponentPosition,
                            goal.PositionWeight);
                        Vector3 plantOffset = Vector3.Lerp(
                                                 standardPosition,
                                                 pivotedPosition,
                                                 goal.PlantPivotWeight) -
                                             currentPosition;
                        SetPlantOffset(goal.Slot, plantOffset);
                    }
                    m_Backend.SetComponentRotation(
                        effector.boneHandle,
                        weightedRotation);
                }
            }
        }

        void SetPlantOffset(CharacterFullBodyIkEffectorSlot slot, Vector3 offset)
        {
            if (slot == CharacterFullBodyIkEffectorSlot.LeftFoot)
            {
                m_LeftFootPlantOffset = offset;
                m_HasLeftFootPlantOffset = true;
                return;
            }
            if (slot == CharacterFullBodyIkEffectorSlot.RightFoot)
            {
                m_RightFootPlantOffset = offset;
                m_HasRightFootPlantOffset = true;
                return;
            }
            throw new InvalidOperationException("Plant pivot is only valid for foot effectors.");
        }

        bool TryGetPlantOffset(CharacterFullBodyIkEffectorSlot slot, out Vector3 offset)
        {
            if (slot == CharacterFullBodyIkEffectorSlot.LeftFoot && m_HasLeftFootPlantOffset)
            {
                offset = m_LeftFootPlantOffset;
                return true;
            }
            if (slot == CharacterFullBodyIkEffectorSlot.RightFoot && m_HasRightFootPlantOffset)
            {
                offset = m_RightFootPlantOffset;
                return true;
            }
            offset = Vector3.zero;
            return false;
        }

        ushort CollectIdentityPoseBoneSlots(
            NativeSlice<int> goalSetValueIndices,
            NativeArray<CharacterFullBodyIkGoalSetHeader> goalSets,
            NativeArray<CharacterFullBodyIkGoal> goalWorkspace)
        {
            ushort slots = 0;
            for (int setIndex = 0; setIndex < goalSetValueIndices.Length; setIndex++)
            {
                CharacterFullBodyIkGoalSetHeader header = goalSets[goalSetValueIndices[setIndex]];
                for (int localGoalIndex = 0; localGoalIndex < header.GoalCount; localGoalIndex++)
                {
                    CharacterFullBodyIkGoal goal = goalWorkspace[header.GoalOffset + localGoalIndex];
                    if ((goal.SourceKind & CharacterFullBodyIkGoalSourceKind.PoseBone) == 0 ||
                        goal.Slot == CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation)
                    {
                        continue;
                    }
                    IKEffector effector = m_Solver.GetEffector(ToFinalIkEffector(goal.Slot));
                    bool positionUnchanged = goal.PositionWeight <= CharacterPoseConstraintMath.Epsilon ||
                                             (goal.ComponentPosition - m_Backend.GetComponentPosition(effector.boneHandle))
                                             .sqrMagnitude <= CharacterPoseConstraintMath.Epsilon;
                    bool rotationUnchanged = goal.RotationWeight <= CharacterPoseConstraintMath.Epsilon ||
                                             Mathf.Abs(Quaternion.Dot(
                                                 goal.ComponentRotation,
                                                 m_Backend.GetComponentRotation(effector.boneHandle))) >=
                                             1f - CharacterPoseConstraintMath.Epsilon;
                    if (positionUnchanged && rotationUnchanged)
                        slots = (ushort)(slots | 1 << ((int)goal.Slot - 1));
                }
            }
            return slots;
        }

        void ResetEffectorsToPose()
        {
            for (int i = 0; i < m_Solver.effectors.Length; i++)
            {
                IKEffector effector = m_Solver.effectors[i];
                effector.target = null;
                effector.position = m_Backend.GetComponentPosition(effector.boneHandle);
                effector.rotation = m_Backend.GetComponentRotation(effector.boneHandle);
                effector.positionWeight = 0f;
                effector.rotationWeight = 0f;
                effector.positionOffset = Vector3.zero;
            }
        }

        void ApplyProfile()
        {
            m_Solver.iterations = m_ActiveTuning.Iterations;
            m_Solver.FABRIKPass = m_ActiveTuning.FabrikPass;
            m_Solver.spineStiffness = m_ActiveTuning.SpineStiffness;
            m_Solver.pullBodyVertical = m_ActiveTuning.PullBodyVertical;
            m_Solver.pullBodyHorizontal = m_ActiveTuning.PullBodyHorizontal;
            ApplyLimbProfile(FullBodyBipedChain.LeftArm, m_ActiveTuning.LeftArm);
            ApplyLimbProfile(FullBodyBipedChain.RightArm, m_ActiveTuning.RightArm);
            ApplyLimbProfile(FullBodyBipedChain.LeftLeg, m_ActiveTuning.LeftLeg);
            ApplyLimbProfile(FullBodyBipedChain.RightLeg, m_ActiveTuning.RightLeg);
            for (int chainIndex = 0; chainIndex < m_Solver.chain.Length; chainIndex++)
            {
                IKSolver.Node[] nodes = m_Solver.chain[chainIndex].nodes;
                for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
                    nodes[nodeIndex].weight = m_ActiveTuning.NodeWeight;
            }
        }

        void BeginDiagnostics(ulong frameSequence)
        {
            m_Diagnostics = default;
            m_DiagnosticPelvisTranslation = Vector3.zero;
            m_DiagnosticFrameSequence = frameSequence;
            m_DiagnosticEffectorCount = 0;
            Array.Clear(m_DiagnosticEffectors, 0, m_DiagnosticEffectors.Length);
            Array.Clear(m_DiagnosticLimbs, 0, m_DiagnosticLimbs.Length);
            if (frameSequence == 0)
                throw new ArgumentOutOfRangeException(nameof(frameSequence));
        }

        CharacterFullBodyIkResult CompleteDiagnostics(
            CharacterFullBodyIkResult result,
            NativeSlice<int> goalSetValueIndices,
            NativeArray<CharacterFullBodyIkGoalSetHeader> goalSets,
            NativeArray<CharacterFullBodyIkGoal> goalWorkspace,
            ulong completionIdentity)
        {
            if (result.Succeeded)
            {
                for (int setIndex = 0; setIndex < goalSetValueIndices.Length; setIndex++)
                {
                    CharacterFullBodyIkGoalSetHeader header = goalSets[goalSetValueIndices[setIndex]];
                    for (int goalIndex = 0; goalIndex < header.GoalCount; goalIndex++)
                    {
                        CharacterFullBodyIkGoal goal = goalWorkspace[header.GoalOffset + goalIndex];
                        Vector3 solvedPosition;
                        Quaternion solvedRotation;
                        float positionResidual;
                        float rotationResidual;
                        if (goal.Slot == CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation)
                        {
                            IndexedBoneHandle pelvis = new IndexedBoneHandle(m_Rig.PelvisPhysicalBoneIndex);
                            solvedPosition = m_Backend.GetComponentPosition(pelvis);
                            solvedRotation = m_Backend.GetComponentRotation(pelvis);
                            positionResidual = 0f;
                            rotationResidual = 0f;
                        }
                        else
                        {
                            IKEffector effector = m_Solver.GetEffector(ToFinalIkEffector(goal.Slot));
                            solvedPosition = m_Backend.GetComponentPosition(effector.boneHandle);
                            solvedRotation = m_Backend.GetComponentRotation(effector.boneHandle);
                            positionResidual = Vector3.Distance(solvedPosition, goal.ComponentPosition);
                            rotationResidual = Quaternion.Angle(solvedRotation, goal.ComponentRotation);
                        }
                        m_DiagnosticEffectors[m_DiagnosticEffectorCount++] =
                            new CharacterFullBodyIkEffectorDiagnostics(
                                goal,
                                solvedPosition,
                                solvedRotation,
                                positionResidual,
                                rotationResidual);
                    }
                }
            }
            CopyLimbDiagnostics();
            m_Diagnostics = new CharacterFullBodyIkSolverDiagnostics(
                m_DiagnosticFrameSequence,
                completionIdentity,
                result.Succeeded ? completionIdentity : 0,
                BackendIdentity,
                m_Rig.RigId,
                m_Rig.RigRevision,
                ProfileId,
                ProfileRevision,
                m_ActiveTuning.Iterations,
                m_ActiveTuning.FabrikPass,
                m_DiagnosticPelvisTranslation,
                result,
                m_DiagnosticEffectorCount,
                m_DiagnosticLimbs.Length);
            return result;
        }

        void CopyLimbDiagnostics()
        {
            m_DiagnosticLimbs[0] = LimbDiagnostics(CharacterFullBodyIkLimbSlot.LeftArm, m_ActiveTuning.LeftArm);
            m_DiagnosticLimbs[1] = LimbDiagnostics(CharacterFullBodyIkLimbSlot.RightArm, m_ActiveTuning.RightArm);
            m_DiagnosticLimbs[2] = LimbDiagnostics(CharacterFullBodyIkLimbSlot.LeftLeg, m_ActiveTuning.LeftLeg);
            m_DiagnosticLimbs[3] = LimbDiagnostics(CharacterFullBodyIkLimbSlot.RightLeg, m_ActiveTuning.RightLeg);
        }

        static CharacterFullBodyIkLimbDiagnostics LimbDiagnostics(
            CharacterFullBodyIkLimbSlot limb,
            ActiveLimb settings) =>
            new CharacterFullBodyIkLimbDiagnostics(
                limb,
                settings.Pull,
                settings.Reach,
                settings.BendConstraintWeight,
                settings.BendClamp);

        void ApplyLimbProfile(
            FullBodyBipedChain chainId,
            ActiveLimb settings)
        {
            FBIKChain chain = m_Solver.GetChain(chainId);
            chain.pin = settings.Pin;
            chain.pull = settings.Pull;
            chain.push = settings.Push;
            chain.pushParent = settings.PushParent;
            chain.reach = settings.Reach;
            chain.reachSmoothing = ToFinalIkSmoothing(settings.ReachSmoothing);
            chain.pushSmoothing = ToFinalIkSmoothing(settings.PushSmoothing);
            IKMappingLimb mapping = m_Solver.GetLimbMapping(chainId);
            mapping.weight = settings.MappingWeight;
            mapping.maintainRotationWeight = settings.MaintainRotationWeight;
            IKConstraintBend bend = m_Solver.GetBendConstraint(chainId);
            bend.weight = settings.BendConstraintWeight;
            bend.clampF = settings.BendClamp;
        }

        static void ApplyTuningField(
            ref ActiveTuning tuning,
            CharacterPoseTuningLayoutEntry entry,
            CharacterPoseTuningValue value)
        {
            string fieldId = entry.FieldId;
            if (fieldId.EndsWith("/iterations", StringComparison.Ordinal)) tuning.Iterations = value.IntegerValue;
            else if (fieldId.EndsWith("/fabrik-pass", StringComparison.Ordinal)) tuning.FabrikPass = value.BooleanValue;
            else if (fieldId.EndsWith("/spine-stiffness", StringComparison.Ordinal)) tuning.SpineStiffness = value.FloatValue;
            else if (fieldId.EndsWith("/pull-body-vertical", StringComparison.Ordinal)) tuning.PullBodyVertical = value.FloatValue;
            else if (fieldId.EndsWith("/pull-body-horizontal", StringComparison.Ordinal)) tuning.PullBodyHorizontal = value.FloatValue;
            else if (fieldId.EndsWith("/node-weight", StringComparison.Ordinal)) tuning.NodeWeight = value.FloatValue;
            else
            {
                ActiveLimb limb = Contains(fieldId, "/left-arm/") ? tuning.LeftArm :
                    Contains(fieldId, "/right-arm/") ? tuning.RightArm :
                    Contains(fieldId, "/left-leg/") ? tuning.LeftLeg : tuning.RightLeg;
                ApplyLimbField(ref limb, fieldId, value);
                if (Contains(fieldId, "/left-arm/")) tuning.LeftArm = limb;
                else if (Contains(fieldId, "/right-arm/")) tuning.RightArm = limb;
                else if (Contains(fieldId, "/left-leg/")) tuning.LeftLeg = limb;
                else tuning.RightLeg = limb;
            }
        }

        static void ApplyLimbField(ref ActiveLimb limb, string fieldId, CharacterPoseTuningValue value)
        {
            if (fieldId.EndsWith("/pin", StringComparison.Ordinal)) limb.Pin = value.FloatValue;
            else if (fieldId.EndsWith("/pull", StringComparison.Ordinal)) limb.Pull = value.FloatValue;
            else if (fieldId.EndsWith("/push", StringComparison.Ordinal)) limb.Push = value.FloatValue;
            else if (fieldId.EndsWith("/push-parent", StringComparison.Ordinal)) limb.PushParent = value.FloatValue;
            else if (fieldId.EndsWith("/reach", StringComparison.Ordinal)) limb.Reach = value.FloatValue;
            else if (fieldId.EndsWith("/reach-smoothing", StringComparison.Ordinal)) limb.ReachSmoothing = (CharacterFullBodyIkSmoothing)value.EnumValue;
            else if (fieldId.EndsWith("/push-smoothing", StringComparison.Ordinal)) limb.PushSmoothing = (CharacterFullBodyIkSmoothing)value.EnumValue;
            else if (fieldId.EndsWith("/mapping-weight", StringComparison.Ordinal)) limb.MappingWeight = value.FloatValue;
            else if (fieldId.EndsWith("/maintain-rotation-weight", StringComparison.Ordinal)) limb.MaintainRotationWeight = value.FloatValue;
            else if (fieldId.EndsWith("/bend-constraint-weight", StringComparison.Ordinal)) limb.BendConstraintWeight = value.FloatValue;
            else if (fieldId.EndsWith("/bend-clamp", StringComparison.Ordinal)) limb.BendClamp = value.FloatValue;
        }

        static bool Contains(string value, string part) =>
            value.IndexOf(part, StringComparison.Ordinal) >= 0;

        struct ActiveTuning
        {
            internal int Iterations;
            internal bool FabrikPass;
            internal float SpineStiffness;
            internal float PullBodyVertical;
            internal float PullBodyHorizontal;
            internal float NodeWeight;
            internal ActiveLimb LeftArm;
            internal ActiveLimb RightArm;
            internal ActiveLimb LeftLeg;
            internal ActiveLimb RightLeg;

            internal static ActiveTuning FromProfile(CharacterFullBodyIkProfile profile) => new ActiveTuning
            {
                Iterations = profile.Iterations,
                FabrikPass = profile.FabrikPass,
                SpineStiffness = profile.SpineStiffness,
                PullBodyVertical = profile.PullBodyVertical,
                PullBodyHorizontal = profile.PullBodyHorizontal,
                NodeWeight = profile.NodeWeight,
                LeftArm = ActiveLimb.FromProfile(profile.LeftArm),
                RightArm = ActiveLimb.FromProfile(profile.RightArm),
                LeftLeg = ActiveLimb.FromProfile(profile.LeftLeg),
                RightLeg = ActiveLimb.FromProfile(profile.RightLeg)
            };

            internal void RequireValid()
            {
                if (Iterations < 0 || Iterations > 10 ||
                    !IsRange(SpineStiffness, 0f, 1f) ||
                    !IsRange(PullBodyVertical, -1f, 1f) ||
                    !IsRange(PullBodyHorizontal, -1f, 1f) ||
                    !IsRange(NodeWeight, 0f, 1f))
                    throw new InvalidOperationException("Full Body IK tuning values are invalid.");
                LeftArm.RequireValid();
                RightArm.RequireValid();
                LeftLeg.RequireValid();
                RightLeg.RequireValid();
            }
        }

        struct ActiveLimb
        {
            internal float Pin;
            internal float Pull;
            internal float Push;
            internal float PushParent;
            internal float Reach;
            internal CharacterFullBodyIkSmoothing ReachSmoothing;
            internal CharacterFullBodyIkSmoothing PushSmoothing;
            internal float MappingWeight;
            internal float MaintainRotationWeight;
            internal float BendConstraintWeight;
            internal float BendClamp;

            internal static ActiveLimb FromProfile(CharacterFullBodyIkLimbSettings settings) => new ActiveLimb
            {
                Pin = settings.Pin,
                Pull = settings.Pull,
                Push = settings.Push,
                PushParent = settings.PushParent,
                Reach = settings.Reach,
                ReachSmoothing = settings.ReachSmoothing,
                PushSmoothing = settings.PushSmoothing,
                MappingWeight = settings.MappingWeight,
                MaintainRotationWeight = settings.MaintainRotationWeight,
                BendConstraintWeight = settings.BendConstraintWeight,
                BendClamp = settings.BendClamp
            };

            internal void RequireValid()
            {
                if (!IsRange(Pin, 0f, 1f) || !IsRange(Pull, 0f, 1f) ||
                    !IsRange(Push, 0f, 1f) || !IsRange(PushParent, -1f, 1f) ||
                    !IsRange(Reach, 0f, 1f) || !IsRange(MappingWeight, 0f, 1f) ||
                    !IsRange(MaintainRotationWeight, 0f, 1f) ||
                    !IsRange(BendConstraintWeight, 0f, 1f) ||
                    !IsRange(BendClamp, 0f, 1f) ||
                    !Enum.IsDefined(typeof(CharacterFullBodyIkSmoothing), ReachSmoothing) ||
                    !Enum.IsDefined(typeof(CharacterFullBodyIkSmoothing), PushSmoothing))
                    throw new InvalidOperationException("Full Body IK limb tuning values are invalid.");
            }
        }

        static bool IsRange(float value, float minimum, float maximum) =>
            float.IsFinite(value) && value >= minimum && value <= maximum;

        bool IsValidPosePage(NativeSlice<AnimationLocalBonePose> pose)
        {
            if (pose.Length != m_Rig.PoseBoneCount)
                return false;
            for (int i = 0; i < pose.Length; i++)
            {
                if (!pose[i].IsValid)
                    return false;
            }
            return true;
        }

        static FullBodyBipedEffector ToFinalIkEffector(CharacterFullBodyIkEffectorSlot slot)
        {
            switch (slot)
            {
                case CharacterFullBodyIkEffectorSlot.Body: return FullBodyBipedEffector.Body;
                case CharacterFullBodyIkEffectorSlot.LeftShoulder: return FullBodyBipedEffector.LeftShoulder;
                case CharacterFullBodyIkEffectorSlot.RightShoulder: return FullBodyBipedEffector.RightShoulder;
                case CharacterFullBodyIkEffectorSlot.LeftThigh: return FullBodyBipedEffector.LeftThigh;
                case CharacterFullBodyIkEffectorSlot.RightThigh: return FullBodyBipedEffector.RightThigh;
                case CharacterFullBodyIkEffectorSlot.LeftHand: return FullBodyBipedEffector.LeftHand;
                case CharacterFullBodyIkEffectorSlot.RightHand: return FullBodyBipedEffector.RightHand;
                case CharacterFullBodyIkEffectorSlot.LeftFoot: return FullBodyBipedEffector.LeftFoot;
                case CharacterFullBodyIkEffectorSlot.RightFoot: return FullBodyBipedEffector.RightFoot;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        static FBIKChain.Smoothing ToFinalIkSmoothing(CharacterFullBodyIkSmoothing smoothing)
        {
            switch (smoothing)
            {
                case CharacterFullBodyIkSmoothing.None: return FBIKChain.Smoothing.None;
                case CharacterFullBodyIkSmoothing.Exponential: return FBIKChain.Smoothing.Exponential;
                case CharacterFullBodyIkSmoothing.Cubic: return FBIKChain.Smoothing.Cubic;
                default: throw new ArgumentOutOfRangeException(nameof(smoothing));
            }
        }
    }
}
