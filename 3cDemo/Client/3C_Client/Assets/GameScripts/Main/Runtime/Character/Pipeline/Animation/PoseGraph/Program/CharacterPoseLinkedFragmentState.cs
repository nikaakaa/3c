using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class CharacterPoseLinkedFragmentState
    {
        readonly CharacterPoseProgramImage m_Image;
        readonly bool[] m_ActiveFragments;
        readonly bool[] m_ResetFragments;
        readonly int[] m_PlayerFragmentIndices;
        readonly int[] m_StateMachineFragmentIndices;
        readonly int[] m_RootOrientationWarpFragmentIndices;
        readonly int[] m_InertializationFragmentIndices;

        internal CharacterPoseLinkedFragmentState(
            CharacterPoseProgramImage image)
        {
            m_Image = image ?? throw new ArgumentNullException(nameof(image));
            m_ActiveFragments = new bool[image.LinkedPoseFragments.Count];
            m_ResetFragments = new bool[image.LinkedPoseFragments.Count];
            m_PlayerFragmentIndices = BuildPlayerFragmentIndices(image);
            m_StateMachineFragmentIndices =
                BuildStateMachineFragmentIndices(image);
            m_RootOrientationWarpFragmentIndices =
                BuildRootOrientationWarpFragmentIndices(image);
            m_InertializationFragmentIndices =
                BuildInertializationFragmentIndices(image);
        }

        internal bool IsPlayerActive(int playerIndex) =>
            IsFragmentActive(RequirePlayerFragmentIndex(playerIndex));

        internal bool HasFragments => m_ResetFragments.Length != 0;

        internal bool RequiresPlayerReset(int playerIndex) =>
            RequiresFragmentReset(RequirePlayerFragmentIndex(playerIndex));

        internal bool IsStateMachineActive(int stateMachineIndex) =>
            IsFragmentActive(
                m_StateMachineFragmentIndices[stateMachineIndex]);

        internal bool RequiresStateMachineReset(int stateMachineIndex) =>
            RequiresFragmentReset(
                m_StateMachineFragmentIndices[stateMachineIndex]);

        internal bool IsRootOrientationWarpActive(int index) =>
            IsFragmentActive(m_RootOrientationWarpFragmentIndices[index]);

        internal bool RequiresRootOrientationWarpReset(int index) =>
            RequiresFragmentReset(
                m_RootOrientationWarpFragmentIndices[index]);

        internal bool RequiresInertializationReset(int index) =>
            RequiresFragmentReset(m_InertializationFragmentIndices[index]);

        internal void ApplySelection(
            in CharacterLinkedPoseGenerationHandle selection)
        {
            if (!selection.IsValid)
            {
                throw new ArgumentException(
                    "Linked Pose generation selection is invalid.",
                    nameof(selection));
            }
            int activeCount = 0;
            for (int fragmentIndex = 0;
                 fragmentIndex < m_Image.LinkedPoseFragments.Count;
                 fragmentIndex++)
            {
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                    m_Image.LinkedPoseFragments[fragmentIndex];
                if (fragment.GroupId != selection.GroupId)
                    continue;
                if (selection.PoseDiscontinuity)
                    m_ResetFragments[fragmentIndex] = true;
                if (fragment.ImplementationId != selection.ImplementationId)
                    continue;
                m_ActiveFragments[fragmentIndex] = true;
                activeCount++;
            }
            if (activeCount == 0)
            {
                throw new InvalidOperationException(
                    $"Linked Pose Group '{selection.GroupId}' selection '{selection.ImplementationId}' has no Entry fragments.");
            }
        }

        internal void Clear()
        {
            Array.Clear(m_ActiveFragments, 0, m_ActiveFragments.Length);
            Array.Clear(m_ResetFragments, 0, m_ResetFragments.Length);
        }

        internal int RequirePlayerFragmentIndex(int playerIndex)
        {
            if ((uint)playerIndex >= (uint)m_PlayerFragmentIndices.Length)
            {
                throw new InvalidOperationException(
                    $"Pose Player #{playerIndex} is outside the compiled Linked Pose ownership table.");
            }
            return m_PlayerFragmentIndices[playerIndex];
        }

        internal bool IsFragmentActive(int fragmentIndex) =>
            fragmentIndex < 0 || m_ActiveFragments[fragmentIndex];

        internal bool RequiresFragmentReset(int fragmentIndex) =>
            fragmentIndex >= 0 && m_ResetFragments[fragmentIndex];

        static int[] BuildPlayerFragmentIndices(
            CharacterPoseProgramImage image)
        {
            var result = CreateUnassignedOwnership(image.PlayerCount);
            for (int i = 0; i < image.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation =
                    image.Operations[i];
                if (operation.PlayerIndex < 0)
                    continue;
                SetOwnership(
                    result,
                    operation.PlayerIndex,
                    operation.LinkedPoseFragmentIndex,
                    "Player");
            }
            RequireCompleteOwnership(result, "Player");
            return result;
        }

        static int[] BuildStateMachineFragmentIndices(
            CharacterPoseProgramImage image)
        {
            var result = CreateUnassignedOwnership(
                image.StateMachines.Count);
            for (int i = 0; i < image.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation =
                    image.Operations[i];
                if (operation.Code !=
                    CharacterPoseOperationCode.PoseStateMachine)
                {
                    continue;
                }
                SetOwnership(
                    result,
                    operation.StateMachineIndex,
                    operation.LinkedPoseFragmentIndex,
                    "StateMachine");
            }
            RequireCompleteOwnership(result, "StateMachine");
            return result;
        }

        static int[] BuildRootOrientationWarpFragmentIndices(
            CharacterPoseProgramImage image)
        {
            var result = CreateUnassignedOwnership(
                image.RootOrientationWarps.Count);
            for (int i = 0; i < image.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation =
                    image.Operations[i];
                if (operation.Code !=
                    CharacterPoseOperationCode.RootOrientationWarp)
                {
                    continue;
                }
                SetOwnership(
                    result,
                    operation.RootOrientationWarpIndex,
                    operation.LinkedPoseFragmentIndex,
                    "Root Orientation Warp");
            }
            RequireCompleteOwnership(result, "Root Orientation Warp");
            return result;
        }

        static int[] BuildInertializationFragmentIndices(
            CharacterPoseProgramImage image)
        {
            var result = CreateUnassignedOwnership(
                image.Inertializations.Count);
            for (int i = 0; i < image.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation =
                    image.Operations[i];
                if (operation.Code !=
                    CharacterPoseOperationCode.Inertialization)
                {
                    continue;
                }
                SetOwnership(
                    result,
                    operation.InertializationIndex,
                    operation.LinkedPoseFragmentIndex,
                    "Inertialization");
            }
            RequireCompleteOwnership(result, "Inertialization");
            return result;
        }

        static int[] CreateUnassignedOwnership(int count)
        {
            var result = new int[count];
            for (int i = 0; i < result.Length; i++)
                result[i] = int.MinValue;
            return result;
        }

        static void SetOwnership(
            int[] ownership,
            int index,
            int fragmentIndex,
            string kind)
        {
            if ((uint)index >= (uint)ownership.Length ||
                ownership[index] != int.MinValue)
            {
                throw new InvalidOperationException(
                    $"{kind} #{index} has invalid Linked Pose ownership.");
            }
            ownership[index] = fragmentIndex;
        }

        static void RequireCompleteOwnership(
            int[] ownership,
            string kind)
        {
            for (int i = 0; i < ownership.Length; i++)
            {
                if (ownership[i] == int.MinValue)
                {
                    throw new InvalidOperationException(
                        $"{kind} #{i} has no compiled Linked Pose ownership.");
                }
            }
        }
    }
}
