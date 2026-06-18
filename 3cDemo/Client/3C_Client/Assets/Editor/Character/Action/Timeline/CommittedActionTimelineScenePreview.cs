using System;
using System.Collections.Generic;
using Animancer;
using Animancer.TransitionLibraries;
using ThirdPersonAction;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacterBehavior.Editor.ActionTimeline
{
    public static class CommittedActionTimelinePreviewLogger
    {
        public const string Prefix = "[ActionTimelinePreview]";

        public static bool Enabled { get; set; } = true;

        public static void Log(string message, UnityEngine.Object context = null)
        {
            if (!Enabled)
                return;

            Debug.Log($"{Prefix} {message}", context);
        }

        public static void Warning(string message, UnityEngine.Object context = null)
        {
            if (!Enabled)
                return;

            Debug.LogWarning($"{Prefix} {message}", context);
        }
    }

    public static class CommittedActionTimelineMotionPreviewOverlay
    {
        static bool registered;
        static bool active;
        static GameObject target;
        static Vector3 startPosition;
        static Vector3 currentPosition;
        static Vector3 endPosition;
        static Vector3 direction;
        static string label = string.Empty;

        public static bool IsActive => active && target != null;
        public static Vector3 StartPosition => startPosition;
        public static Vector3 CurrentPosition => currentPosition;
        public static Vector3 EndPosition => endPosition;

        public static void Show(GameObject previewTarget, ActionMotionSpec motionSpec, float localTimeSeconds)
        {
            if (previewTarget == null || !motionSpec.HasSpec || motionSpec.Distance <= 0f)
            {
                Clear();
                return;
            }

            EnsureRegistered();
            target = previewTarget;
            startPosition = previewTarget.transform.position;
            direction = ResolveDirection(motionSpec, previewTarget.transform);
            float progress = ResolveProgress(motionSpec, localTimeSeconds);
            endPosition = startPosition + direction * motionSpec.Distance;
            currentPosition = Vector3.Lerp(startPosition, endPosition, progress);
            label = $"Motion preview {motionSpec.Distance * progress:0.##}m / {motionSpec.Distance:0.##}m";
            active = true;
            SceneView.RepaintAll();
        }

        public static void Clear()
        {
            if (!active && target == null)
                return;

            active = false;
            target = null;
            label = string.Empty;
            SceneView.RepaintAll();
        }

        internal static Vector3 ResolveDirection(ActionMotionSpec motionSpec, Transform fallbackTransform)
        {
            Vector3 value = motionSpec.LockedWorldDirection;
            value.y = 0f;
            if (value.sqrMagnitude > 0.000001f)
                return value.normalized;

            if (fallbackTransform == null)
                return Vector3.forward;

            value = fallbackTransform.forward;
            value.y = 0f;
            return value.sqrMagnitude > 0.000001f ? value.normalized : Vector3.forward;
        }

        internal static float ResolveProgress(ActionMotionSpec motionSpec, float localTimeSeconds)
        {
            return motionSpec.Duration > 0f
                ? Mathf.Clamp01(Mathf.Max(0f, localTimeSeconds) / motionSpec.Duration)
                : 0f;
        }

        static void EnsureRegistered()
        {
            if (registered)
                return;

            SceneView.duringSceneGui += Draw;
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            registered = true;
        }

        static void Draw(SceneView sceneView)
        {
            if (!IsActive)
                return;

            Vector3 drawDirection = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.forward;
            Quaternion arrowRotation = Quaternion.LookRotation(drawDirection, Vector3.up);
            Color pathColor = new Color(0.1f, 0.8f, 1f, 0.95f);
            Color markerColor = new Color(1f, 0.95f, 0.15f, 0.95f);
            UnityEngine.Rendering.CompareFunction previousZTest = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.color = pathColor;
            Handles.DrawAAPolyLine(8f, startPosition, endPosition);
            Handles.ArrowHandleCap(0, endPosition, arrowRotation, 0.6f, EventType.Repaint);
            Handles.color = markerColor;
            Handles.DrawSolidDisc(currentPosition, Vector3.up, 0.12f);
            Handles.Label(currentPosition + Vector3.up * 1.1f, label, EditorStyles.boldLabel);
            Handles.zTest = previousZTest;
        }
    }

    public enum CommittedActionTimelineScenePreviewBindingState
    {
        Unbound,
        Bound,
        Invalid,
        PlayModeDisabled
    }

    public readonly struct CommittedActionTimelineScenePreviewBinding
    {
        public CommittedActionTimelineScenePreviewBinding(
            GameObject target,
            Animator animator,
            CommittedActionTimelineScenePreviewBindingState state,
            string status)
        {
            Target = target;
            Animator = animator;
            State = state;
            Status = status ?? string.Empty;
        }

        public GameObject Target { get; }
        public Animator Animator { get; }
        public CommittedActionTimelineScenePreviewBindingState State { get; }
        public string Status { get; }
        public bool CanSample => State == CommittedActionTimelineScenePreviewBindingState.Bound && Animator != null;

        public static CommittedActionTimelineScenePreviewBinding FromTarget(GameObject target)
        {
            return FromTarget(target, EditorApplication.isPlayingOrWillChangePlaymode);
        }

        public static CommittedActionTimelineScenePreviewBinding FromTarget(GameObject target, bool playMode)
        {
            if (target == null)
                return new CommittedActionTimelineScenePreviewBinding(null, null, CommittedActionTimelineScenePreviewBindingState.Unbound, "preview-binding-unbound");

            if (playMode)
                return new CommittedActionTimelineScenePreviewBinding(target, null, CommittedActionTimelineScenePreviewBindingState.PlayModeDisabled, "preview-visual-disabled-playmode");

            if (EditorUtility.IsPersistent(target))
                return new CommittedActionTimelineScenePreviewBinding(target, null, CommittedActionTimelineScenePreviewBindingState.Invalid, "preview-target-not-scene-object");

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
                animator = target.GetComponentInChildren<Animator>(true);

            if (animator == null)
                return new CommittedActionTimelineScenePreviewBinding(target, null, CommittedActionTimelineScenePreviewBindingState.Invalid, "preview-target-missing-animator");

            return new CommittedActionTimelineScenePreviewBinding(target, animator, CommittedActionTimelineScenePreviewBindingState.Bound, $"preview-target-bound:{animator.name}");
        }
    }

    public readonly struct CommittedActionTimelineAnimationResolveResult
    {
        public CommittedActionTimelineAnimationResolveResult(
            bool resolved,
            AnimationClip clip,
            string status,
            string clipName)
        {
            Resolved = resolved;
            Clip = clip;
            Status = status ?? string.Empty;
            ClipName = clipName ?? string.Empty;
        }

        public bool Resolved { get; }
        public AnimationClip Clip { get; }
        public string Status { get; }
        public string ClipName { get; }
        public bool CanSample => Resolved && Clip != null;

        public static CommittedActionTimelineAnimationResolveResult Fail(string status)
        {
            return new CommittedActionTimelineAnimationResolveResult(false, null, status, string.Empty);
        }
    }

    public interface ICommittedActionTimelineAnimationResolver
    {
        CommittedActionTimelineAnimationResolveResult Resolve(
            CommittedActionTimelineScenePreviewBinding binding,
            ActionAnimationKey key);
    }

    public sealed class CommittedActionTimelineAnimancerLibraryResolver : ICommittedActionTimelineAnimationResolver
    {
        public CommittedActionTimelineAnimationResolveResult Resolve(
            CommittedActionTimelineScenePreviewBinding binding,
            ActionAnimationKey key)
        {
            if (!binding.CanSample)
                return CommittedActionTimelineAnimationResolveResult.Fail(binding.Status);

            if (!key.IsValid)
                return CommittedActionTimelineAnimationResolveResult.Fail("preview-animation-key-missing");

            AnimancerComponent animancer = ResolveAnimancer(binding);
            if (animancer == null)
                return CommittedActionTimelineAnimationResolveResult.Fail("preview-animation-library-missing");

            TransitionLibrary library = animancer.Graph.Transitions;
            if (library == null)
                return CommittedActionTimelineAnimationResolveResult.Fail("preview-animation-library-missing");

            StringReference libraryKey = StringReference.Get(key.Value);
            if (!library.TryGetTransition(libraryKey, out TransitionModifierGroup group) || group == null)
                return CommittedActionTimelineAnimationResolveResult.Fail($"preview-animation-transition-missing:{key.Value}");

            ITransition transition = group.Transition;
            if (transition is TransitionAssetBase asset)
                transition = asset.GetTransition();

            if (transition is not ClipTransition clipTransition)
                return CommittedActionTimelineAnimationResolveResult.Fail($"preview-animation-transition-unsupported:{transition?.GetType().Name ?? "null"}");

            AnimationClip clip = clipTransition.Clip;
            if (clip == null)
                return CommittedActionTimelineAnimationResolveResult.Fail($"preview-animation-clip-missing:{key.Value}");

            return new CommittedActionTimelineAnimationResolveResult(true, clip, "preview-animation-resolved", clip.name);
        }

        static AnimancerComponent ResolveAnimancer(CommittedActionTimelineScenePreviewBinding binding)
        {
            AnimancerComponent animancer = binding.Animator != null
                ? binding.Animator.GetComponent<AnimancerComponent>()
                : null;
            if (animancer != null)
                return animancer;

            return binding.Target != null
                ? binding.Target.GetComponentInChildren<AnimancerComponent>(true)
                : null;
        }
    }

    public readonly struct CommittedActionTimelineVisualPreviewResult
    {
        public CommittedActionTimelineVisualPreviewResult(
            string bindingStatus,
            string resolvedClipName,
            string visualPreviewStatus,
            float clipTimeSeconds,
            bool sampled)
        {
            BindingStatus = bindingStatus ?? string.Empty;
            ResolvedClipName = resolvedClipName ?? string.Empty;
            VisualPreviewStatus = visualPreviewStatus ?? string.Empty;
            ClipTimeSeconds = Mathf.Max(0f, clipTimeSeconds);
            Sampled = sampled;
        }

        public string BindingStatus { get; }
        public string ResolvedClipName { get; }
        public string VisualPreviewStatus { get; }
        public float ClipTimeSeconds { get; }
        public bool Sampled { get; }

        public static CommittedActionTimelineVisualPreviewResult NotSampled(string bindingStatus, string visualPreviewStatus)
        {
            return new CommittedActionTimelineVisualPreviewResult(bindingStatus, string.Empty, visualPreviewStatus, 0f, false);
        }
    }

    public sealed class CommittedActionTimelinePlayablePreviewSession : IDisposable
    {
        static readonly List<CommittedActionTimelinePlayablePreviewSession> ActiveSessions = new List<CommittedActionTimelinePlayablePreviewSession>();
        static bool reloadHookRegistered;

        Animator animator;
        AnimationClip activeClip;
        PlayableGraph graph;
        AnimationClipPlayable clipPlayable;
        AnimatorStateSnapshot snapshot;
        bool hasSnapshot;
        bool disposed;

        public bool IsGraphValid => graph.IsValid();
        public string CurrentClipName => activeClip != null ? activeClip.name : string.Empty;
        public float LastSampleTimeSeconds { get; private set; }

        public CommittedActionTimelinePlayablePreviewSession()
        {
            EnsureReloadHook();
            ActiveSessions.Add(this);
        }

        public CommittedActionTimelineVisualPreviewResult Sample(
            CommittedActionTimelineScenePreviewBinding binding,
            CommittedActionTimelineAnimationResolveResult animation,
            float localTimeSeconds)
        {
            if (!binding.CanSample)
            {
                CommittedActionTimelinePreviewLogger.Warning($"sample blocked binding={binding.Status}", binding.Target);
                Dispose();
                return CommittedActionTimelineVisualPreviewResult.NotSampled(binding.Status, binding.Status);
            }

            if (!animation.CanSample)
            {
                CommittedActionTimelinePreviewLogger.Warning($"sample blocked animation={animation.Status} binding={binding.Status}", binding.Target);
                DestroyGraph();
                return CommittedActionTimelineVisualPreviewResult.NotSampled(binding.Status, animation.Status);
            }

            EnsureGraph(binding.Animator, animation.Clip);
            float clipTime = ResolveClipTime(animation.Clip, localTimeSeconds);
            clipPlayable.SetTime(clipTime);
            clipPlayable.SetSpeed(0);
            graph.Evaluate(0f);
            if (hasSnapshot)
                snapshot.RestoreBlendShapeWeights();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            LastSampleTimeSeconds = clipTime;
            return new CommittedActionTimelineVisualPreviewResult(
                binding.Status,
                animation.ClipName,
                "preview-visual-sampled",
                clipTime,
                true);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            ActiveSessions.Remove(this);
            DestroyGraph();
            RestoreAnimatorState();
        }

        public static float ResolveClipTime(AnimationClip clip, float localTimeSeconds)
        {
            if (clip == null)
                return 0f;

            return Mathf.Clamp(Mathf.Max(0f, localTimeSeconds), 0f, Mathf.Max(0f, clip.length));
        }

        static void EnsureReloadHook()
        {
            if (reloadHookRegistered)
                return;

            AssemblyReloadEvents.beforeAssemblyReload += DisposeActiveSessions;
            reloadHookRegistered = true;
        }

        static void DisposeActiveSessions()
        {
            for (int i = ActiveSessions.Count - 1; i >= 0; i--)
                ActiveSessions[i].Dispose();
        }

        void EnsureGraph(Animator targetAnimator, AnimationClip clip)
        {
            if (animator != targetAnimator)
            {
                RestoreAnimatorState();
                animator = targetAnimator;
                snapshot = AnimatorStateSnapshot.Capture(animator);
                hasSnapshot = true;
                DestroyGraph();
                CommittedActionTimelinePreviewLogger.Log($"captured animator state target={animator.name}", animator);
            }

            if (graph.IsValid() && activeClip == clip)
                return;

            DestroyGraph();
            activeClip = clip;
            graph = PlayableGraph.Create("3C Committed Action Timeline Preview");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            clipPlayable = AnimationClipPlayable.Create(graph, clip);
            clipPlayable.SetSpeed(0);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);
            output.SetSourcePlayable(clipPlayable);
            graph.Play();
            CommittedActionTimelinePreviewLogger.Log($"graph created target={animator.name} clip={clip.name} length={clip.length:0.###}", animator);
        }

        void DestroyGraph()
        {
            if (graph.IsValid())
            {
                CommittedActionTimelinePreviewLogger.Log($"graph destroyed clip={CurrentClipName} lastTime={LastSampleTimeSeconds:0.###}", animator);
                graph.Destroy();
            }

            activeClip = null;
            clipPlayable = default;
            LastSampleTimeSeconds = 0f;
        }

        void RestoreAnimatorState()
        {
            if (!hasSnapshot)
                return;

            snapshot.Restore();
            hasSnapshot = false;
            CommittedActionTimelinePreviewLogger.Log($"restored animator state target={animator.name}", animator);
            animator = null;
        }

        readonly struct AnimatorStateSnapshot
        {
            readonly Animator animator;
            readonly Vector3 localPosition;
            readonly Quaternion localRotation;
            readonly Vector3 localScale;
            readonly RuntimeAnimatorController controller;
            readonly bool enabled;
            readonly bool applyRootMotion;
            readonly AnimatorCullingMode cullingMode;
            readonly AnimatorUpdateMode updateMode;
            readonly BlendShapeSnapshot[] blendShapes;

            AnimatorStateSnapshot(Animator animator)
            {
                this.animator = animator;
                Transform transform = animator.transform;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
                controller = animator.runtimeAnimatorController;
                enabled = animator.enabled;
                applyRootMotion = animator.applyRootMotion;
                cullingMode = animator.cullingMode;
                updateMode = animator.updateMode;
                blendShapes = CaptureBlendShapes(animator);
            }

            public static AnimatorStateSnapshot Capture(Animator animator)
            {
                return new AnimatorStateSnapshot(animator);
            }

            public void Restore()
            {
                if (animator == null)
                    return;

                animator.runtimeAnimatorController = controller;
                animator.enabled = enabled;
                animator.applyRootMotion = applyRootMotion;
                animator.cullingMode = cullingMode;
                animator.updateMode = updateMode;
                Transform transform = animator.transform;
                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
                animator.Rebind();
                animator.Update(0f);
                RestoreBlendShapeWeights();
            }

            public void RestoreBlendShapeWeights()
            {
                if (blendShapes == null)
                    return;

                for (int i = 0; i < blendShapes.Length; i++)
                    blendShapes[i].Restore();
            }

            static BlendShapeSnapshot[] CaptureBlendShapes(Animator animator)
            {
                SkinnedMeshRenderer[] renderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                List<BlendShapeSnapshot> snapshots = new List<BlendShapeSnapshot>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    SkinnedMeshRenderer renderer = renderers[i];
                    Mesh mesh = renderer != null ? renderer.sharedMesh : null;
                    int count = mesh != null ? mesh.blendShapeCount : 0;
                    if (count <= 0)
                        continue;

                    float[] weights = new float[count];
                    for (int shape = 0; shape < count; shape++)
                        weights[shape] = renderer.GetBlendShapeWeight(shape);
                    snapshots.Add(new BlendShapeSnapshot(renderer, weights));
                }

                return snapshots.ToArray();
            }

            readonly struct BlendShapeSnapshot
            {
                readonly SkinnedMeshRenderer renderer;
                readonly float[] weights;

                public BlendShapeSnapshot(SkinnedMeshRenderer renderer, float[] weights)
                {
                    this.renderer = renderer;
                    this.weights = weights;
                }

                public void Restore()
                {
                    if (renderer == null || weights == null)
                        return;

                    Mesh mesh = renderer.sharedMesh;
                    int count = mesh != null ? Mathf.Min(mesh.blendShapeCount, weights.Length) : 0;
                    for (int i = 0; i < count; i++)
                        renderer.SetBlendShapeWeight(i, weights[i]);
                }
            }
        }

    }
}
