using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CharacterFootPlacementComposition : MonoBehaviour
    {
        [SerializeField] CharacterFootPlacementProfile m_Profile;
        [SerializeField] CharacterFootPlacementRig m_Rig;
        [SerializeField] MonoBehaviour m_SolverAdapter;

        public CharacterFootPlacementProfile Profile => m_Profile;
        public CharacterFootPlacementRig Rig => m_Rig;
        public MonoBehaviour SolverAdapter => m_SolverAdapter;

        public ICharacterFootPlacementSolver RequireSolver(Transform visualRoot)
        {
            if (!m_Profile)
                throw new InvalidOperationException($"Foot Placement Composition '{name}' requires a Profile.");
            if (!m_Rig)
                throw new InvalidOperationException($"Foot Placement Composition '{name}' requires a Rig.");
            if (!m_SolverAdapter)
                throw new InvalidOperationException($"Foot Placement Composition '{name}' requires a Solver adapter.");
            if (m_Rig.transform != transform || m_SolverAdapter.transform != transform)
                throw new InvalidOperationException($"Foot Placement Composition '{name}' requires root-local Rig and Solver components.");
            CharacterFootPlacementRigBinding rig = m_Rig.BuildBinding();
            if (rig.VisualRoot != visualRoot || transform != visualRoot)
                throw new InvalidOperationException($"Foot Placement Composition '{name}' does not match the Presentation VisualRoot.");
            return m_SolverAdapter as ICharacterFootPlacementSolver ??
                   throw new InvalidOperationException($"Foot Placement Composition '{name}' Solver does not implement ICharacterFootPlacementSolver.");
        }
    }
}
