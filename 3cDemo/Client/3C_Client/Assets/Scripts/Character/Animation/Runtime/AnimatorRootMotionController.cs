using ThirdPersonDiagnostics;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [DisallowMultipleComponent]
    public sealed class AnimatorRootMotionController : MonoBehaviour
    {
        [SerializeField] Animator animator;

        bool locomotionForceRequested;
        bool locomotionDisableRequested = true;
        bool actionDisableRequested;
        bool hasApplied;
        bool lastManualRootMotionActive;
        bool lastAnimatorApplyRootMotion;

        public bool LocomotionForceRequested => locomotionForceRequested;
        public bool LocomotionDisableRequested => locomotionDisableRequested;
        public bool ActionDisableRequested => actionDisableRequested;
        public bool ManualRootMotionActive => hasApplied ? lastManualRootMotionActive : ResolveManualRootMotionActive();
        public bool AppliedRootMotion => ManualRootMotionActive;

        void Reset()
        {
            animator = GetComponent<Animator>();
        }

        void Awake()
        {
            ResolveAnimator();
            Apply("awake");
        }

        public void SetLocomotionRootMotion(bool forceEnabled, bool disableRequested, string reason)
        {
            bool sameRequest = locomotionForceRequested == forceEnabled && locomotionDisableRequested == disableRequested;
            if (sameRequest && hasApplied && IsAnimatorApplyRootMotionCurrent())
                return;

            locomotionForceRequested = forceEnabled;
            locomotionDisableRequested = disableRequested;
            Apply(reason);
        }

        public void SetActionRootMotionDisabled(bool disabled, string reason)
        {
            bool shouldClearLocomotionForce = disabled && locomotionForceRequested;
            if (actionDisableRequested == disabled && hasApplied && !shouldClearLocomotionForce)
                return;

            actionDisableRequested = disabled;
            if (disabled)
                locomotionForceRequested = false;
            Apply(reason);
        }

        Animator ResolveAnimator()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            return animator;
        }

        void Apply(string reason)
        {
            Animator resolvedAnimator = ResolveAnimator();
            if (resolvedAnimator == null)
                return;

            bool next = ResolveManualRootMotionActive();
            bool animatorApplyRootMotionBefore = resolvedAnimator.applyRootMotion;
            if (animatorApplyRootMotionBefore != next)
                resolvedAnimator.applyRootMotion = next;

            bool animatorApplyRootMotionAfter = resolvedAnimator.applyRootMotion;

            if (hasApplied &&
                lastManualRootMotionActive == next &&
                lastAnimatorApplyRootMotion == animatorApplyRootMotionAfter &&
                animatorApplyRootMotionBefore == animatorApplyRootMotionAfter)
            {
                return;
            }

            hasApplied = true;
            lastManualRootMotionActive = next;
            lastAnimatorApplyRootMotion = animatorApplyRootMotionAfter;
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                RuntimeDiagnosticLogLevel.Trace,
                "animator-root-motion-policy",
                reason ?? string.Empty,
                string.Empty,
                0,
                Time.frameCount,
                $"object={name} locomotionForceRequested={locomotionForceRequested} locomotionDisableRequested={locomotionDisableRequested} actionDisableRequested={actionDisableRequested} animatorApplyRootMotionBefore={animatorApplyRootMotionBefore} animatorApplyRootMotionAfter={animatorApplyRootMotionAfter} manualRootMotionActive={next} applied={next}"));
        }

        bool ResolveManualRootMotionActive()
        {
            return locomotionForceRequested || (!locomotionDisableRequested && !actionDisableRequested);
        }

        bool IsAnimatorApplyRootMotionCurrent()
        {
            Animator resolvedAnimator = ResolveAnimator();
            return resolvedAnimator == null || resolvedAnimator.applyRootMotion == ResolveManualRootMotionActive();
        }

        public static AnimatorRootMotionController Resolve(Animancer.AnimancerComponent animancer)
        {
            if (animancer == null || animancer.Animator == null)
                return null;

            GameObject animatorObject = animancer.Animator.gameObject;
            AnimatorRootMotionController controller = animatorObject.GetComponent<AnimatorRootMotionController>();
            if (controller == null)
                controller = animatorObject.AddComponent<AnimatorRootMotionController>();
            return controller;
        }
    }
}
