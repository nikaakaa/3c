using Animancer;
using Animancer.TransitionLibraries;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using UnityEngine;

namespace ThirdPersonAnimation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AnimancerComponent))]
    public sealed class ActionAnimationAnimancerPresenter : MonoBehaviour, IActionAnimationPresenter, IActionAnimationPlaybackProgressController
    {
        [SerializeField] AnimancerComponent animancer;
        [SerializeField] bool disableAnimatorRootMotion = true;

        AnimatorRootMotionController rootMotionController;
        AnimancerState currentState;
        ActionAnimationKey currentKey;

        public ActionAnimationKey CurrentKey => currentKey;
        public float CurrentNormalizedTime => currentState != null ? currentState.NormalizedTime : 0f;
        public bool HasValidPlayback => currentState != null && currentKey.IsValid;
        public ActionAnimationPlaybackProgress CurrentPlaybackProgress =>
            new ActionAnimationPlaybackProgress(currentKey, CurrentNormalizedTime, HasValidPlayback, HasValidPlayback && CurrentNormalizedTime >= 1f);
        public string CurrentAnimationName
        {
            get
            {
                if (currentState == null)
                    return string.Empty;

                Object mainObject = currentState.MainObject;
                if (mainObject != null)
                    return mainObject.name;

                return currentState.Clip != null ? currentState.Clip.name : string.Empty;
            }
        }

        void Reset()
        {
            animancer = GetComponent<AnimancerComponent>();
        }

        void Awake()
        {
            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();

            ApplyRootMotionPolicy();
        }

        public bool Present(in CharacterStateAnimationRequest request)
        {
            if (!request.HasKey)
            {
                LogPlayback("action-animation-missing-key", RuntimeDiagnosticLogLevel.Warning, in request, null);
                return false;
            }

            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();

            ApplyRootMotionPolicy();

            if (animancer == null)
            {
                LogPlayback("action-animation-missing-animancer", RuntimeDiagnosticLogLevel.Warning, in request, null);
                return false;
            }

            AnimancerState nextState = TryPlayKey(request.Key);
            if (nextState == null)
            {
                LogPlayback("action-animation-play-failed", RuntimeDiagnosticLogLevel.Warning, in request, null);
                return false;
            }

            nextState.NormalizedTime = 0f;
            currentKey = request.Key;
            currentState = nextState;
            LogPlayback("action-animation-played", RuntimeDiagnosticLogLevel.Info, in request, nextState);
            return true;
        }

        public void Clear()
        {
            if (currentState == null && !currentKey.IsValid)
                return;

            AnimancerState previousState = currentState;
            ActionAnimationKey previousKey = currentKey;

            currentState = null;
            currentKey = default;
            LogClear(previousKey, previousState);
        }

        public bool RestorePlaybackProgress(in ActionAnimationPlaybackProgress progress, string animationName)
        {
            if (!progress.HasValidPlayback)
            {
                Clear();
                return true;
            }

            if (!EnsureAnimancer())
                return false;

            AnimancerState state = TryPlayKey(progress.Key);
            if (state == null)
                return false;

            currentKey = progress.Key;
            currentState = state;
            currentState.NormalizedTime = progress.NormalizedTime;
            return true;
        }

        AnimancerState TryPlayKey(ActionAnimationKey key)
        {
            if (!key.IsValid)
                return null;

            StringReference libraryKey = StringReference.Get(key.Value);
            return animancer.TryPlay(libraryKey);
        }

        bool EnsureAnimancer()
        {
            if (animancer == null)
                animancer = GetComponent<AnimancerComponent>();

            if (animancer == null)
                return false;

            ApplyRootMotionPolicy();
            return true;
        }

        void ApplyRootMotionPolicy()
        {
            AnimatorRootMotionController controller = ResolveRootMotionController();
            if (controller != null)
                controller.SetActionRootMotionDisabled(disableAnimatorRootMotion, "action-animation");
        }

        AnimatorRootMotionController ResolveRootMotionController()
        {
            if (rootMotionController != null)
                return rootMotionController;

            rootMotionController = AnimatorRootMotionController.Resolve(animancer);
            return rootMotionController;
        }

        void LogPlayback(
            string message,
            RuntimeDiagnosticLogLevel level,
            in CharacterStateAnimationRequest request,
            AnimancerState state)
        {
            CharacterStateAnimationBinding binding = request.Binding;
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                level,
                message,
                request.Key.Value,
                currentKey.Value,
                request.SourceStep,
                Time.frameCount,
                $"key={request.Key.Value} previousKey={currentKey.Value} sourceStep={request.SourceStep} timelineBinding={binding.TimelineBindingKey} debugName={binding.DebugName} currentAnimation={CurrentAnimationName} nextAnimation={AnimationName(state)} normalized={(state != null ? state.NormalizedTime : 0f):F3} rootMotionDisabled={disableAnimatorRootMotion}"));
        }

        void LogClear(ActionAnimationKey previousKey, AnimancerState previousState)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Animation,
                RuntimeDiagnosticLogLevel.Info,
                "action-animation-cleared",
                string.Empty,
                previousKey.Value,
                0,
                Time.frameCount,
                $"previousKey={previousKey.Value} previousAnimation={AnimationName(previousState)} previousNormalized={(previousState != null ? previousState.NormalizedTime : 0f):F3} releasedForLocomotionBlend=True"));
        }

        static string AnimationName(AnimancerState state)
        {
            if (state == null)
                return string.Empty;

            Object mainObject = state.MainObject;
            if (mainObject != null)
                return mainObject.name;

            return state.Clip != null ? state.Clip.name : string.Empty;
        }
    }
}
