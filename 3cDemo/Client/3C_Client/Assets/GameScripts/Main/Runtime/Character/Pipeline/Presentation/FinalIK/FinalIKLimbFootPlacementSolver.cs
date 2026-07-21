using System;
using RootMotion.FinalIK;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation.FinalIK
{
    [DisallowMultipleComponent]
    public sealed class FinalIKLimbFootPlacementSolver : MonoBehaviour, ICharacterFootPlacementSolver
    {
        [SerializeField] LimbIK m_LeftLeg;
        [SerializeField] LimbIK m_RightLeg;

        CharacterFootPlacementRigBinding m_Rig;
        Transform m_LeftBendGoal;
        Transform m_RightBendGoal;
        Vector3 m_AnimatedPelvisLocalPosition;
        ulong m_CapturedRenderFrame;
        ulong m_AppliedRenderFrame;
        bool m_HasCapturedPose;
        bool m_Disposed;

        public bool IsInitialized { get; private set; }

        public void RequireValid(CharacterFootPlacementRigBinding rig)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            RequireLimb(m_LeftLeg, rig.LeftHip, rig.LeftKnee, rig.LeftAnkle, "Left");
            RequireLimb(m_RightLeg, rig.RightHip, rig.RightKnee, rig.RightAnkle, "Right");
            if (m_LeftLeg == m_RightLeg)
                throw new InvalidOperationException("Foot Placement requires two distinct LimbIK components.");
        }

        public void Initialize(CharacterFootPlacementSolverContext context)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FinalIKLimbFootPlacementSolver));
            if (IsInitialized)
                throw new InvalidOperationException("Final IK Foot Placement solver is already initialized.");
            RequireValid(context.Rig);
            m_Rig = context.Rig;
            Initiate(m_LeftLeg, m_Rig.VisualRoot);
            Initiate(m_RightLeg, m_Rig.VisualRoot);
            m_LeftBendGoal = CreateBendGoal("FootPlacement.LeftBendGoal", m_Rig.VisualRoot);
            m_RightBendGoal = CreateBendGoal("FootPlacement.RightBendGoal", m_Rig.VisualRoot);
            ConfigureBendGoal(m_LeftLeg.solver, m_LeftBendGoal);
            ConfigureBendGoal(m_RightLeg.solver, m_RightBendGoal);
            IsInitialized = true;
        }

        public CharacterFootPlacementAnimatedPose CaptureAnimatedPose(ulong renderFrame)
        {
            RequireInitialized();
            if (renderFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(renderFrame));
            if (renderFrame == m_CapturedRenderFrame)
                throw new InvalidOperationException($"Final IK Foot Placement captured render frame '{renderFrame}' twice.");
            m_AnimatedPelvisLocalPosition = m_Rig.Pelvis.localPosition;
            m_CapturedRenderFrame = renderFrame;
            m_HasCapturedPose = true;
            return m_Rig.CaptureAnimatedPose(renderFrame);
        }

        public CharacterFootPlacementSolverResult Apply(CharacterFootPlacementPlan plan)
        {
            RequireInitialized();
            if (!plan.IsValid)
                throw new ArgumentException("Final IK Foot Placement received an invalid plan.", nameof(plan));
            if (!m_HasCapturedPose || plan.RenderFrame != m_CapturedRenderFrame)
                throw new InvalidOperationException($"Final IK Foot Placement has no animated pose for render frame '{plan.RenderFrame}'.");
            if (plan.RenderFrame == m_AppliedRenderFrame)
                return new CharacterFootPlacementSolverResult(plan.RenderFrame, false, true, "Duplicate render frame.");

            m_Rig.Pelvis.localPosition = m_AnimatedPelvisLocalPosition +
                                         m_Rig.ResolvePelvisParentLocalVerticalOffset(
                                             plan.PelvisComponentVerticalOffset);
            ApplyLimb(m_LeftLeg.solver, m_LeftBendGoal, plan.Left);
            ApplyLimb(m_RightLeg.solver, m_RightBendGoal, plan.Right);
            m_AppliedRenderFrame = plan.RenderFrame;
            return new CharacterFootPlacementSolverResult(plan.RenderFrame, true, false, string.Empty);
        }

        public void ResetPose(CharacterFootPlacementSolverReset reset)
        {
            if (!IsInitialized || m_Disposed)
                return;
            if (m_HasCapturedPose)
                m_Rig.Pelvis.localPosition = m_AnimatedPelvisLocalPosition;
            ClearLimb(m_LeftLeg.solver);
            ClearLimb(m_RightLeg.solver);
            m_CapturedRenderFrame = 0;
            m_AppliedRenderFrame = 0;
            m_HasCapturedPose = false;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            ResetPose(default);
            DestroyBendGoal(ref m_LeftBendGoal);
            DestroyBendGoal(ref m_RightBendGoal);
            IsInitialized = false;
            m_Rig = null;
            m_Disposed = true;
        }

        static void RequireLimb(
            LimbIK limb,
            Transform hip,
            Transform knee,
            Transform ankle,
            string side)
        {
            if (limb == null)
                throw new InvalidOperationException($"Foot Placement {side} LimbIK is missing.");
            if (limb.enabled)
                throw new InvalidOperationException($"Foot Placement {side} LimbIK must be disabled for explicit pass ownership.");
            if (limb.solver == null ||
                limb.solver.bone1.transform != hip ||
                limb.solver.bone2.transform != knee ||
                limb.solver.bone3.transform != ankle)
            {
                throw new InvalidOperationException($"Foot Placement {side} LimbIK chain does not match the explicit rig.");
            }
            string message = string.Empty;
            if (!limb.solver.IsValid(ref message))
                throw new InvalidOperationException($"Foot Placement {side} LimbIK is invalid: {message}");
        }

        static void Initiate(LimbIK limb, Transform root)
        {
            if (!limb.solver.initiated)
                limb.solver.Initiate(root);
            if (!limb.solver.initiated)
                throw new InvalidOperationException($"LimbIK '{limb.name}' failed to initialize.");
            ClearLimb(limb.solver);
        }

        static void ApplyLimb(IKSolverLimb solver, Transform bendGoal, FootPlacementFootPlan plan)
        {
            solver.IKPosition = plan.Position;
            solver.IKRotation = plan.Rotation;
            solver.IKPositionWeight = plan.PositionWeight;
            solver.IKRotationWeight = plan.RotationWeight;
            bendGoal.position = plan.BendGoalPosition;
            solver.bendModifierWeight = plan.BendGoalWeight;
            solver.Update();
        }

        static void ClearLimb(IKSolverLimb solver)
        {
            solver.IKPositionWeight = 0f;
            solver.IKRotationWeight = 0f;
            solver.bendModifierWeight = 0f;
        }

        static Transform CreateBendGoal(string name, Transform parent)
        {
            var value = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            value.transform.SetParent(parent, false);
            return value.transform;
        }

        static void ConfigureBendGoal(IKSolverLimb solver, Transform bendGoal)
        {
            solver.bendGoal = bendGoal;
            solver.bendModifier = IKSolverLimb.BendModifier.Goal;
            solver.bendModifierWeight = 0f;
        }

        static void DestroyBendGoal(ref Transform value)
        {
            if (value)
                Destroy(value.gameObject);
            value = null;
        }

        void RequireInitialized()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FinalIKLimbFootPlacementSolver));
            if (!IsInitialized)
                throw new InvalidOperationException("Final IK Foot Placement solver is not initialized.");
        }
    }
}
