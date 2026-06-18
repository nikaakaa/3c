using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Animancer;
using Animancer.TransitionLibraries;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterBehavior.Editor.ActionTimeline;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tests.Editor.Character.Action.Timeline
{
    public sealed class CommittedActionTimelineEditorAdapterTests
    {
        const string DodgeAssetPath = "Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset";

        [SetUp]
        public void SetUp()
        {
            CommittedActionTimelinePreviewLogger.Enabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            CommittedActionTimelineMotionPreviewOverlay.Clear();
            CommittedActionTimelinePreviewLogger.Enabled = true;
        }

        [Test]
        public void AdapterReadsFormalDodgeActionDefinition()
        {
            CharacterActionDefinitionSO asset = LoadFormalDodgeAction();
            CommittedActionTimelineSerializedAdapter adapter = new CommittedActionTimelineSerializedAdapter(asset);

            Assert.True(adapter.IsValid);
            Assert.True(adapter.IsDodge);
            Assert.True(adapter.TryGetTimelineProperty(CommittedActionTimelineVariant.Directional, out SerializedProperty directional, out string directionalDiagnostic), directionalDiagnostic);
            Assert.True(adapter.TryGetTimelineProperty(CommittedActionTimelineVariant.Backstep, out SerializedProperty backstep, out string backstepDiagnostic), backstepDiagnostic);
            Assert.AreEqual(0.35f, directional.FindPropertyRelative("durationSeconds").floatValue, 0.0001f);
            Assert.AreEqual(0.35f, backstep.FindPropertyRelative("durationSeconds").floatValue, 0.0001f);
            Assert.False(CommittedActionTimelineEditorValidator.Validate(adapter).HasErrors);
        }

        [Test]
        public void AdapterAddsRemovesAndReordersFormalTracks()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionTimelineSerializedAdapter adapter = new CommittedActionTimelineSerializedAdapter(clone);
                int originalCount = TrackCount(adapter, CommittedActionTimelineVariant.Directional);

                Assert.True(adapter.AddTrack(CommittedActionTimelineVariant.Directional, ActionTimelineTrackKind.Cue, out string addDiagnostic), addDiagnostic);
                Assert.AreEqual(originalCount + 1, TrackCount(adapter, CommittedActionTimelineVariant.Directional));
                int addedIndex = originalCount;
                Assert.True(adapter.ReorderTrack(CommittedActionTimelineVariant.Directional, addedIndex, 0, out string reorderDiagnostic), reorderDiagnostic);
                Assert.AreEqual(ActionTimelineTrackKind.Cue, TrackKindAt(adapter, CommittedActionTimelineVariant.Directional, 0));
                Assert.True(adapter.RemoveTrack(CommittedActionTimelineVariant.Directional, 0, out string removeDiagnostic), removeDiagnostic);
                Assert.AreEqual(originalCount, TrackCount(adapter, CommittedActionTimelineVariant.Directional));
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void AdapterAddsMovesResizesDeletesClipAndWritesPayload()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionTimelineSerializedAdapter adapter = new CommittedActionTimelineSerializedAdapter(clone);
                Assert.True(adapter.AddTrack(CommittedActionTimelineVariant.Directional, ActionTimelineTrackKind.Animation, out string addTrackDiagnostic), addTrackDiagnostic);
                int trackIndex = TrackCount(adapter, CommittedActionTimelineVariant.Directional) - 1;

                Assert.True(adapter.AddClip(CommittedActionTimelineVariant.Directional, trackIndex, ActionTimelineClipKind.AnimationKey, 2f / 60f, 5f / 60f, out string addClipDiagnostic), addClipDiagnostic);
                Assert.True(adapter.SetAnimationKey(CommittedActionTimelineVariant.Directional, trackIndex, 0, new ActionAnimationKey("Action.Dodge.EditorTest"), out string payloadDiagnostic), payloadDiagnostic);
                Assert.True(adapter.MoveClip(CommittedActionTimelineVariant.Directional, trackIndex, 0, 4f / 60f, out string moveDiagnostic), moveDiagnostic);
                Assert.True(adapter.ResizeClip(CommittedActionTimelineVariant.Directional, trackIndex, 0, 4f / 60f, 9f / 60f, out string resizeDiagnostic), resizeDiagnostic);

                SerializedProperty clip = ClipAt(adapter, CommittedActionTimelineVariant.Directional, trackIndex, 0);
                Assert.AreEqual(4f / 60f, clip.FindPropertyRelative("startSeconds").floatValue, 0.0001f);
                Assert.AreEqual(9f / 60f, clip.FindPropertyRelative("endSeconds").floatValue, 0.0001f);
                Assert.AreEqual("Action.Dodge.EditorTest", clip.FindPropertyRelative("payload").FindPropertyRelative("animationKey").stringValue);
                Assert.True(adapter.MoveClipRange(CommittedActionTimelineVariant.Directional, trackIndex, 0, 6f / 60f, 11f / 60f, out string moveRangeDiagnostic), moveRangeDiagnostic);
                Assert.AreEqual(6f / 60f, clip.FindPropertyRelative("startSeconds").floatValue, 0.0001f);
                Assert.AreEqual(11f / 60f, clip.FindPropertyRelative("endSeconds").floatValue, 0.0001f);
                Assert.True(adapter.MoveClip(CommittedActionTimelineVariant.Directional, trackIndex, 0, -2f / 60f, out string moveToZeroDiagnostic), moveToZeroDiagnostic);
                Assert.AreEqual(0f, clip.FindPropertyRelative("startSeconds").floatValue, 0.0001f);
                Assert.AreEqual(5f / 60f, clip.FindPropertyRelative("endSeconds").floatValue, 0.0001f);
                Assert.True(adapter.ResizeClip(CommittedActionTimelineVariant.Directional, trackIndex, 0, -3f / 60f, 3f / 60f, out string resizeToZeroDiagnostic), resizeToZeroDiagnostic);
                Assert.AreEqual(0f, clip.FindPropertyRelative("startSeconds").floatValue, 0.0001f);
                Assert.AreEqual(3f / 60f, clip.FindPropertyRelative("endSeconds").floatValue, 0.0001f);
                Assert.True(adapter.RemoveClip(CommittedActionTimelineVariant.Directional, trackIndex, 0, out string removeClipDiagnostic), removeClipDiagnostic);
                Assert.AreEqual(0, ClipCount(adapter, CommittedActionTimelineVariant.Directional, trackIndex));
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void TimelineModelCreatesStableClipIdentitiesAndResolvesAfterTrackReorder()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionTimelineSerializedAdapter adapter = new CommittedActionTimelineSerializedAdapter(clone);
                CommittedActionTimelineEditorModel model = new CommittedActionTimelineEditorModel(adapter);
                CommittedActionTimelineEditorSnapshot before = model.Capture(CommittedActionTimelineVariant.Directional);
                Assert.That(before.Tracks.Count, Is.GreaterThan(1));
                Assert.That(before.Clips.Count, Is.GreaterThan(0));

                CommittedActionTimelineClipSnapshot clip = before.Clips[0];
                Assert.False(string.IsNullOrWhiteSpace(clip.TrackStableId));
                Assert.False(string.IsNullOrWhiteSpace(clip.ClipStableId));
                Assert.True(model.TryResolveClip(clip.Identity, out int originalTrackIndex, out int originalClipIndex, out string originalPath));
                Assert.AreEqual(clip.TrackIndex, originalTrackIndex);
                Assert.AreEqual(clip.ClipIndex, originalClipIndex);
                Assert.False(string.IsNullOrWhiteSpace(originalPath));

                int targetIndex = originalTrackIndex == 0 ? before.Tracks.Count - 1 : 0;
                Assert.True(adapter.ReorderTrack(
                    CommittedActionTimelineVariant.Directional,
                    originalTrackIndex,
                    targetIndex,
                    out string reorderDiagnostic), reorderDiagnostic);

                CommittedActionTimelineEditorModel afterModel = new CommittedActionTimelineEditorModel(adapter);
                Assert.True(afterModel.TryResolveClip(clip.Identity, out int movedTrackIndex, out int movedClipIndex, out string movedPath));
                Assert.AreEqual(targetIndex, movedTrackIndex);
                Assert.AreEqual(originalClipIndex, movedClipIndex);
                Assert.False(string.IsNullOrWhiteSpace(movedPath));
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void TimelineModelReloadsSerializedWriteBack()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionTimelineSerializedAdapter adapter = new CommittedActionTimelineSerializedAdapter(clone);
                Assert.True(adapter.AddTrack(CommittedActionTimelineVariant.Backstep, ActionTimelineTrackKind.Cue, out string addTrackDiagnostic), addTrackDiagnostic);
                int trackIndex = TrackCount(adapter, CommittedActionTimelineVariant.Backstep) - 1;
                Assert.True(adapter.AddClip(CommittedActionTimelineVariant.Backstep, trackIndex, ActionTimelineClipKind.Cue, 3f / 60f, 4f / 60f, out string addClipDiagnostic), addClipDiagnostic);
                Assert.True(adapter.SetCuePayload(CommittedActionTimelineVariant.Backstep, trackIndex, 0, "cue.editor.reload", out string cueDiagnostic), cueDiagnostic);

                CommittedActionTimelineSerializedAdapter reloaded = new CommittedActionTimelineSerializedAdapter(clone);
                CommittedActionTimelineEditorModel model = new CommittedActionTimelineEditorModel(reloaded);
                CommittedActionTimelineEditorSnapshot snapshot = model.Capture(CommittedActionTimelineVariant.Backstep);

                Assert.That(snapshot.Tracks.Any(track => track.Kind == ActionTimelineTrackKind.Cue), Is.True);
                Assert.That(snapshot.Clips.Any(clip => clip.Kind == ActionTimelineClipKind.Cue &&
                                                        Mathf.Abs(clip.StartSeconds - 3f / 60f) < 0.0001f &&
                                                        Mathf.Abs(clip.EndSeconds - 4f / 60f) < 0.0001f), Is.True);
                SerializedProperty clipProperty = ClipAt(reloaded, CommittedActionTimelineVariant.Backstep, trackIndex, 0);
                Assert.AreEqual("cue.editor.reload", clipProperty.FindPropertyRelative("payload").FindPropertyRelative("cueId").stringValue);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void PayloadWriteBackCompilesDirectionalAndBackstepRuntimeDefinition()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionTimelineSerializedAdapter adapter = new CommittedActionTimelineSerializedAdapter(clone);
                int directionalAnimationTrack = FindTrack(adapter, CommittedActionTimelineVariant.Directional, ActionTimelineTrackKind.Animation);
                int backstepMotionTrack = FindTrack(adapter, CommittedActionTimelineVariant.Backstep, ActionTimelineTrackKind.Motion);

                Assert.True(adapter.SetAnimationKey(
                    CommittedActionTimelineVariant.Directional,
                    directionalAnimationTrack,
                    0,
                    new ActionAnimationKey("Action.Dodge.EditorDirectional"),
                    out string animationDiagnostic), animationDiagnostic);
                Assert.True(adapter.SetMotionPayload(
                    CommittedActionTimelineVariant.Backstep,
                    backstepMotionTrack,
                    0,
                    CharacterStateIds.Dodge,
                    CharacterStateVariant.Backstep,
                    0.5f,
                    8f,
                    false,
                    false,
                    out string motionDiagnostic), motionDiagnostic);

                ActionTimelineCompileContext compileContext = CompileContext();
                CharacterActionDefinition definition = clone.ToDefinition(in compileContext);
                Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));
                CommittedActionBranchOutcome directional = CommittedActionBranchEvaluator.Evaluate(
                    new CommittedActionBranchEvaluationInput(
                        branch,
                        0,
                        42,
                        Context(CharacterStateVariant.Directional, Vector3.forward)));
                CommittedActionBranchOutcome backstep = CommittedActionBranchEvaluator.Evaluate(
                    new CommittedActionBranchEvaluationInput(
                        branch,
                        0,
                        43,
                        Context(CharacterStateVariant.Backstep, Vector3.back)));

                Assert.AreEqual("timeline.dodge.directional", directional.SelectedNodeId.Value);
                Assert.AreEqual(new ActionAnimationKey("Action.Dodge.EditorDirectional"), directional.TimelineOutcome.AnimationKey);
                Assert.AreEqual("timeline.dodge.backstep", backstep.SelectedNodeId.Value);
                Assert.AreEqual(0.5f, backstep.TimelineOutcome.MotionSpec.Duration, 0.0001f);
                Assert.AreEqual(8f, backstep.TimelineOutcome.MotionSpec.Distance, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void InvalidTimelineReportsEditorValidationError()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionTimelineSerializedAdapter adapter = new CommittedActionTimelineSerializedAdapter(clone);
                Assert.True(adapter.AddTrack(CommittedActionTimelineVariant.Directional, ActionTimelineTrackKind.Animation, out _));
                int trackIndex = TrackCount(adapter, CommittedActionTimelineVariant.Directional) - 1;
                Assert.True(adapter.AddClip(CommittedActionTimelineVariant.Directional, trackIndex, ActionTimelineClipKind.AnimationKey, 0f, 3f / 60f, out _));

                CommittedActionTimelineEditorValidationResult validation = CommittedActionTimelineEditorValidator.Validate(adapter);

                Assert.True(validation.HasErrors);
                Assert.True(validation.Errors.Any(error => error.Contains("Directional:clip-invalid")));
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void PreviewAdapterMatchesCommittedActionBranchEvaluator()
        {
            CharacterActionDefinitionSO asset = LoadFormalDodgeAction();
            CommittedActionTimelinePreviewAdapter previewAdapter = new CommittedActionTimelinePreviewAdapter();
            ActionTimelineCompileContext compileContext = CompileContext();
            CharacterActionDefinition definition = asset.ToDefinition(in compileContext);
            Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));
            CommittedActionBranchOutcome expected = CommittedActionBranchEvaluator.Evaluate(
                new CommittedActionBranchEvaluationInput(
                    branch,
                    0,
                    51,
                    Context(CharacterStateVariant.Directional, Vector3.forward)));

            CommittedActionTimelinePreviewResult preview = previewAdapter.Preview(
                asset,
                CommittedActionTimelineVariant.Directional,
                0f,
                51);

            Assert.True(preview.HasPreview);
            Assert.AreEqual(expected.SelectedNodeId, preview.SelectedNodeId);
            Assert.AreEqual(expected.TimelineOutcome.AnimationKey, preview.AnimationKey);
            Assert.AreEqual(expected.TimelineOutcome.MotionSpec.Distance, preview.MotionSpec.Distance, 0.0001f);
            Assert.AreEqual(0, preview.LocalTick);
            Assert.AreEqual("preview-binding-unbound", preview.BindingStatus);
            Assert.AreEqual("preview-visual-unbound", preview.VisualPreviewStatus);
        }

        [Test]
        public void PreviewBindingReportsUnboundInvalidBoundAndPlayModeStates()
        {
            CommittedActionTimelineScenePreviewBinding unbound =
                CommittedActionTimelineScenePreviewBinding.FromTarget(null, false);
            Assert.AreEqual(CommittedActionTimelineScenePreviewBindingState.Unbound, unbound.State);
            Assert.False(unbound.CanSample);
            Assert.AreEqual("preview-binding-unbound", unbound.Status);

            GameObject invalid = new GameObject("preview-target-without-animator");
            GameObject valid = new GameObject("preview-target-with-animator");
            try
            {
                valid.AddComponent<Animator>();

                CommittedActionTimelineScenePreviewBinding invalidBinding =
                    CommittedActionTimelineScenePreviewBinding.FromTarget(invalid, false);
                CommittedActionTimelineScenePreviewBinding validBinding =
                    CommittedActionTimelineScenePreviewBinding.FromTarget(valid, false);
                CommittedActionTimelineScenePreviewBinding playModeBinding =
                    CommittedActionTimelineScenePreviewBinding.FromTarget(valid, true);

                Assert.AreEqual(CommittedActionTimelineScenePreviewBindingState.Invalid, invalidBinding.State);
                Assert.AreEqual("preview-target-missing-animator", invalidBinding.Status);
                Assert.AreEqual(CommittedActionTimelineScenePreviewBindingState.Bound, validBinding.State);
                Assert.True(validBinding.CanSample);
                Assert.AreEqual(CommittedActionTimelineScenePreviewBindingState.PlayModeDisabled, playModeBinding.State);
                Assert.AreEqual("preview-visual-disabled-playmode", playModeBinding.Status);
            }
            finally
            {
                Object.DestroyImmediate(invalid);
                Object.DestroyImmediate(valid);
            }
        }

        [Test]
        public void PreviewResolverReadsAnimancerLibraryClipWithoutPlayingPresenter()
        {
            CreatePreviewRig(
                out GameObject rig,
                out _,
                ActionAnimationKeys.DodgeDirectional,
                CreateClip("DodgeDirectionalPreview", 1.2f));
            try
            {
                CommittedActionTimelineScenePreviewBinding binding =
                    CommittedActionTimelineScenePreviewBinding.FromTarget(rig, false);
                CommittedActionTimelineAnimancerLibraryResolver resolver = new CommittedActionTimelineAnimancerLibraryResolver();

                CommittedActionTimelineAnimationResolveResult result =
                    resolver.Resolve(binding, ActionAnimationKeys.DodgeDirectional);

                Assert.True(result.CanSample);
                Assert.AreEqual("DodgeDirectionalPreview", result.ClipName);
                Assert.AreEqual("preview-animation-resolved", result.Status);
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }

        [Test]
        public void PreviewResolverReportsMissingLibraryTransitionAndClip()
        {
            GameObject noLibrary = new GameObject("preview-no-library");
            GameObject noTransition = new GameObject("preview-no-transition");
            GameObject noClip = new GameObject("preview-no-clip");
            try
            {
                noLibrary.AddComponent<Animator>();
                noTransition.AddComponent<Animator>();
                AnimancerComponent transitionAnimancer = noTransition.AddComponent<AnimancerComponent>();
                transitionAnimancer.Graph.Transitions = new TransitionLibrary();

                noClip.AddComponent<Animator>();
                AnimancerComponent noClipAnimancer = noClip.AddComponent<AnimancerComponent>();
                TransitionLibrary noClipLibrary = new TransitionLibrary();
                noClipLibrary.AddTransition(
                    StringReference.Get(ActionAnimationKeys.DodgeDirectional.Value),
                    new ClipTransition());
                noClipAnimancer.Graph.Transitions = noClipLibrary;

                CommittedActionTimelineAnimancerLibraryResolver resolver = new CommittedActionTimelineAnimancerLibraryResolver();

                Assert.AreEqual(
                    "preview-animation-library-missing",
                    resolver.Resolve(CommittedActionTimelineScenePreviewBinding.FromTarget(noLibrary, false), ActionAnimationKeys.DodgeDirectional).Status);
                Assert.That(
                    resolver.Resolve(CommittedActionTimelineScenePreviewBinding.FromTarget(noTransition, false), ActionAnimationKeys.DodgeDirectional).Status,
                    Does.StartWith("preview-animation-transition-missing"));
                Assert.That(
                    resolver.Resolve(CommittedActionTimelineScenePreviewBinding.FromTarget(noClip, false), ActionAnimationKeys.DodgeDirectional).Status,
                    Does.StartWith("preview-animation-clip-missing"));
            }
            finally
            {
                Object.DestroyImmediate(noLibrary);
                Object.DestroyImmediate(noTransition);
                Object.DestroyImmediate(noClip);
            }
        }

        [Test]
        public void PreviewSessionCreatesGraphSamplesClipTimeAndCleansUp()
        {
            AnimationClip clip = CreateClip("PreviewSessionClip", 0.5f);
            CreatePreviewRig(out GameObject rig, out _, ActionAnimationKeys.DodgeDirectional, clip);
            CommittedActionTimelinePlayablePreviewSession session = new CommittedActionTimelinePlayablePreviewSession();
            try
            {
                CommittedActionTimelineScenePreviewBinding binding =
                    CommittedActionTimelineScenePreviewBinding.FromTarget(rig, false);
                CommittedActionTimelineAnimationResolveResult animation =
                    new CommittedActionTimelineAnimationResolveResult(true, clip, "preview-animation-resolved", clip.name);

                CommittedActionTimelineVisualPreviewResult visual = session.Sample(binding, animation, 0.75f);

                Assert.True(visual.Sampled);
                Assert.True(session.IsGraphValid);
                Assert.AreEqual("PreviewSessionClip", session.CurrentClipName);
                Assert.AreEqual(0.5f, visual.ClipTimeSeconds, 0.0001f);
                Assert.AreEqual(0.5f, session.LastSampleTimeSeconds, 0.0001f);

                session.Dispose();

                Assert.False(session.IsGraphValid);
            }
            finally
            {
                session.Dispose();
                Object.DestroyImmediate(rig);
            }
        }

        [Test]
        public void PreviewSessionSamplesBodyPoseWithoutOverwritingBlendShapes()
        {
            GameObject rig = new GameObject("preview-body-with-face-rig");
            rig.AddComponent<Animator>();
            GameObject body = new GameObject("BodyBone");
            body.transform.SetParent(rig.transform, false);
            GameObject face = new GameObject("Face");
            face.transform.SetParent(rig.transform, false);
            SkinnedMeshRenderer faceRenderer = face.AddComponent<SkinnedMeshRenderer>();
            Mesh faceMesh = CreateBlendShapeMesh("PreviewFaceMesh", "FaceGone");
            faceRenderer.sharedMesh = faceMesh;
            faceRenderer.SetBlendShapeWeight(0, 12f);
            AnimationClip clip = new AnimationClip { name = "PreviewBodyFaceClip" };
            clip.SetCurve("BodyBone", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            clip.SetCurve("Face", typeof(SkinnedMeshRenderer), "blendShape.FaceGone", AnimationCurve.Linear(0f, 0f, 1f, 100f));
            CommittedActionTimelinePlayablePreviewSession session = new CommittedActionTimelinePlayablePreviewSession();
            try
            {
                CommittedActionTimelineScenePreviewBinding binding =
                    CommittedActionTimelineScenePreviewBinding.FromTarget(rig, false);
                CommittedActionTimelineAnimationResolveResult animation =
                    new CommittedActionTimelineAnimationResolveResult(true, clip, "preview-animation-resolved", clip.name);

                CommittedActionTimelineVisualPreviewResult visual = session.Sample(binding, animation, 1f);

                Assert.True(visual.Sampled);
                Assert.Greater(body.transform.localPosition.x, 0.5f);
                Assert.AreEqual(12f, faceRenderer.GetBlendShapeWeight(0), 0.0001f);
            }
            finally
            {
                session.Dispose();
                Object.DestroyImmediate(rig);
                Object.DestroyImmediate(faceMesh);
            }
        }

        [Test]
        public void TimelineViewVisualPreviewUsesFormalEvaluatorOutcome()
        {
            CharacterActionDefinitionSO asset = LoadFormalDodgeAction();
            CreatePreviewRig(
                out GameObject rig,
                out _,
                ActionAnimationKeys.DodgeDirectional,
                CreateClip("DodgeDirectionalViewPreview", 1f));
            try
            {
                CommittedActionRefPortedTimelineView view = new CommittedActionRefPortedTimelineView();
                view.SetScenePreviewTarget(rig);
                view.Populate(new SerializedObject(asset));
                view.SetPreviewFrame(0);

                Label summary = view.Q<Label>("preview-summary");

                Assert.NotNull(summary);
                Assert.That(summary.text, Does.Contain(ActionAnimationKeys.DodgeDirectional.Value));
                Assert.That(summary.text, Does.Contain("DodgeDirectionalViewPreview"));
                Assert.That(summary.text, Does.Contain("visual sampled"));
                view.DisposeScenePreview();
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }

        [Test]
        public void TimelineViewDrawsMotionGhostPathWithoutMovingPreviewTarget()
        {
            CharacterActionDefinitionSO asset = LoadFormalDodgeAction();
            CreatePreviewRig(
                out GameObject rig,
                out _,
                ActionAnimationKeys.DodgeDirectional,
                CreateChildClip("DodgeDirectionalMotionGhostPreview", "PreviewBone", 1f));
            GameObject bone = new GameObject("PreviewBone");
            bone.transform.SetParent(rig.transform, false);
            rig.transform.position = new Vector3(1f, 0f, 2f);
            Vector3 originalPosition = rig.transform.position;
            try
            {
                CommittedActionRefPortedTimelineView view = new CommittedActionRefPortedTimelineView();
                view.SetScenePreviewTarget(rig);
                view.Populate(new SerializedObject(asset));
                view.SetPreviewFrame(13);

                Label summary = view.Q<Label>("preview-summary");

                Assert.NotNull(summary);
                Assert.That(summary.text, Does.Contain("motion ghost/path"));
                Assert.True(CommittedActionTimelineMotionPreviewOverlay.IsActive);
                Assert.AreEqual(originalPosition, rig.transform.position);
                Assert.Greater(
                    Vector3.Distance(
                        CommittedActionTimelineMotionPreviewOverlay.StartPosition,
                        CommittedActionTimelineMotionPreviewOverlay.EndPosition),
                    0.1f);
                Assert.Greater(
                    Vector3.Distance(
                        CommittedActionTimelineMotionPreviewOverlay.StartPosition,
                        CommittedActionTimelineMotionPreviewOverlay.CurrentPosition),
                    0.01f);

                view.DisposeScenePreview();

                Assert.False(CommittedActionTimelineMotionPreviewOverlay.IsActive);
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }

        [Test]
        public void TimelineWindowBindsSelectedScenePreviewTarget()
        {
            GameObject rig = new GameObject("window-bind-selection-preview-rig");
            rig.AddComponent<Animator>();
            ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow window = null;
            GameObject previousSelection = Selection.activeGameObject;
            try
            {
                window = EditorWindow.GetWindow<ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow>();
                Selection.activeGameObject = rig;
                MethodInfo bind = typeof(ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow)
                    .GetMethod("BindSelectedPreviewTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo viewField = typeof(ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow)
                    .GetField("timelineView", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo targetField = typeof(ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow)
                    .GetField("previewTargetField", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(bind);
                Assert.NotNull(viewField);
                Assert.NotNull(targetField);

                bind.Invoke(window, null);

                CommittedActionRefPortedTimelineView view = viewField.GetValue(window) as CommittedActionRefPortedTimelineView;
                ObjectField targetFieldValue = targetField.GetValue(window) as ObjectField;

                Assert.NotNull(view);
                Assert.NotNull(targetFieldValue);
                Assert.AreSame(rig, targetFieldValue.value);
                Assert.AreEqual(CommittedActionTimelineScenePreviewBindingState.Bound, view.ScenePreviewBinding.State);
            }
            finally
            {
                Selection.activeGameObject = previousSelection;
                if (window != null)
                    window.Close();
                Object.DestroyImmediate(rig);
            }
        }

        [Test]
        public void TimelineWindowPreservesPreviewTargetAcrossDisableEnable()
        {
            GameObject rig = new GameObject("window-preserve-preview-target-rig");
            rig.AddComponent<Animator>();
            ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow window = null;
            GameObject previousSelection = Selection.activeGameObject;
            try
            {
                window = EditorWindow.GetWindow<ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow>();
                Selection.activeGameObject = rig;
                MethodInfo bind = typeof(ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow)
                    .GetMethod("BindSelectedPreviewTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo onDisable = typeof(ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow)
                    .GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo onEnable = typeof(ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow)
                    .GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo viewField = typeof(ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow)
                    .GetField("timelineView", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo targetField = typeof(ThirdPersonCharacterBehavior.Editor.ActionTimeline.CommittedActionTimelineEditorWindow)
                    .GetField("previewTargetField", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(bind);
                Assert.NotNull(onDisable);
                Assert.NotNull(onEnable);
                Assert.NotNull(viewField);
                Assert.NotNull(targetField);

                bind.Invoke(window, null);
                onDisable.Invoke(window, null);
                onEnable.Invoke(window, null);

                CommittedActionRefPortedTimelineView view = viewField.GetValue(window) as CommittedActionRefPortedTimelineView;
                ObjectField targetFieldValue = targetField.GetValue(window) as ObjectField;

                Assert.NotNull(view);
                Assert.NotNull(targetFieldValue);
                Assert.AreSame(rig, targetFieldValue.value);
                Assert.AreEqual(CommittedActionTimelineScenePreviewBindingState.Bound, view.ScenePreviewBinding.State);
            }
            finally
            {
                Selection.activeGameObject = previousSelection;
                if (window != null)
                    window.Close();
                Object.DestroyImmediate(rig);
            }
        }

        [Test]
        public void RuntimeBoundaryDoesNotReferenceRefTimelineRunner()
        {
            string runtimeSource = ReadSource("Scripts/Character");
            string editorSource = ReadSource("Editor/Character/Action/Timeline");
            string changeDocs = ReadRepoTextFiles(
                "openspec/changes/port-ref-timeline-ui-to-unity-2022-compatible-editor",
                "*.md");

            Assert.That(runtimeSource, Does.Not.Contain("TimelinePlayer"));
            Assert.That(runtimeSource, Does.Not.Contain("PlayableGraph"));
            Assert.That(runtimeSource, Does.Not.Contain("RunnableTree"));
            Assert.That(runtimeSource, Does.Not.Contain("RunnableNode"));
            Assert.That(runtimeSource, Does.Not.Contain("TreeRunner"));
            Assert.That(runtimeSource, Does.Not.Contain("BaseTree"));
            Assert.That(runtimeSource, Does.Not.Contain("CommittedActionTimelineScenePreviewBinding"));
            Assert.That(runtimeSource, Does.Not.Contain("CommittedActionTimelinePlayablePreviewSession"));
            Assert.That(runtimeSource, Does.Not.Contain("AnimationPlayableOutput"));
            Assert.That(runtimeSource, Does.Not.Contain("AnimationClipPlayable"));
            Assert.That(editorSource, Does.Contain("Committed Action Timeline Editor"));
            Assert.That(editorSource, Does.Not.Contain("Skill" + " Editor"));
            Assert.That(changeDocs, Does.Not.Contain("Skill" + " Editor"));
        }

        [Test]
        public void TimelineEditorResourcesAreUnity2022Safe()
        {
            string resourceSource = ReadTextFiles("Editor/Character/Action/Timeline/RefPortedResources", "*.uxml", "*.uss");
            string editorSource = ReadSource("Editor/Character/Action/Timeline");
            string visualTreeRoot = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "Editor/Character/Action/Timeline/RefPortedResources/VisualTree"));

            Assert.That(resourceSource, Does.Not.Contain("project://database/Assets/Addon/Taco"));
            Assert.That(resourceSource, Does.Not.Contain("Taco.Editor.SplitView"));
            Assert.That(editorSource, Does.Not.Contain("TrackHandleUxmlPath"));
            Assert.That(editorSource, Does.Not.Contain("TrackViewUxmlPath"));
            Assert.False(File.Exists(Path.Combine(visualTreeRoot, "CommittedActionTimelineTrackHandle.uxml")));
            Assert.False(File.Exists(Path.Combine(visualTreeRoot, "CommittedActionTimelineTrackView.uxml")));
        }

        [Test]
        public void TimelineViewBuildsVisibleEditingControls()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                SerializedObject serializedObject = new SerializedObject(clone);
                CommittedActionRefPortedTimelineView view = new CommittedActionRefPortedTimelineView();
                view.Populate(serializedObject);

                VisualElement clip = view.Q("timeline-clip-view");
                VisualElement track = view.Q("timeline-track-view");
                VisualElement trackHandle = view.Q("timeline-track-handle");
                VisualElement dropArea = view.Q("drop-area");

                Assert.NotNull(view.Q("add-track-menu"), "add-track-menu");
                Assert.NotNull(view.Q("left-panel-resizer"), "left-panel-resizer");
                Assert.NotNull(view.Q("inspector-resizer"), "inspector-resizer");
                Assert.NotNull(view.Q("delete-track-button"), "delete-track-button");
                Assert.NotNull(track, "timeline-track-view");
                Assert.NotNull(trackHandle, "timeline-track-handle");
                Assert.NotNull(dropArea, "drop-area");
                Assert.NotNull(clip, "timeline-clip-view");
                Assert.AreEqual(PickingMode.Position, track.pickingMode);
                Assert.AreEqual(PickingMode.Position, dropArea.pickingMode);
                Assert.AreEqual(PickingMode.Position, clip.pickingMode);
                Assert.That(track.userData as string, Is.Not.Empty);
                Assert.That(trackHandle.userData as string, Is.Not.Empty);
                Assert.AreEqual(30f, clip.style.height.value.value, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void TimelineFieldAndClipSourceExposeRefPortedInteractions()
        {
            string editorSource = ReadSource("Editor/Character/Action/Timeline");
            string timelineViewSource = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "Editor/Character/Action/Timeline/CommittedActionRefPortedTimelineView.cs"),
                Encoding.UTF8);
            string clipTree = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "Editor/Character/Action/Timeline/RefPortedResources/VisualTree/CommittedActionTimelineClipView.uxml"),
                Encoding.UTF8);
            string clipStyle = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "Editor/Character/Action/Timeline/RefPortedResources/StyleSheet/CommittedActionTimelineClipView.uss"),
                Encoding.UTF8);
            string editorStyle = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "Editor/Character/Action/Timeline/RefPortedResources/StyleSheet/CommittedActionTimelineEditorWindow.uss"),
                Encoding.UTF8);
            string fieldStyle = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "Editor/Character/Action/Timeline/RefPortedResources/StyleSheet/CommittedActionTimelineFieldView.uss"),
                Encoding.UTF8);

            Assert.That(editorSource, Does.Contain("BuildRuler"));
            Assert.That(editorSource, Does.Contain("RebuildFramePositions"));
            Assert.That(editorSource, Does.Contain("FramePosition"));
            Assert.That(editorSource, Does.Contain("GetClosestFrame"));
            Assert.That(editorSource, Does.Contain("BeginRectangleSelection"));
            Assert.That(editorSource, Does.Contain("FocusSelection"));
            Assert.That(editorSource, Does.Contain("RegisterCallback<WheelEvent>"));
            Assert.That(editorSource, Does.Contain("RegisterCallback<PointerMoveEvent>"));
            Assert.That(editorSource, Does.Contain("AddTrack"));
            Assert.That(editorSource, Does.Contain("RemoveTrack"));
            Assert.That(editorSource, Does.Contain("ReorderTrack"));
            Assert.That(editorSource, Does.Contain("AddClip"));
            Assert.That(editorSource, Does.Contain("RemoveClip"));
            Assert.That(editorSource, Does.Contain("MoveClip"));
            Assert.That(editorSource, Does.Contain("ResizeClip"));
            Assert.That(editorSource, Does.Contain("selectedClips"));
            Assert.That(editorSource, Does.Contain("CommittedActionTimelineEditorModel"));
            Assert.That(editorSource, Does.Contain("CommittedActionTimelineDragManipulator"));
            Assert.That(editorSource, Does.Contain("CommittedActionTimelineDragLineManipulator"));
            Assert.That(editorSource, Does.Contain("DragBeginForce"));
            Assert.That(editorSource, Does.Contain("OnResizeDrag"));
            Assert.That(editorSource, Does.Contain("ResolveFrameFromPixelDelta"));
            Assert.That(editorSource, Does.Contain("ResolveFrameDeltaFromPixelDelta"));
            Assert.That(editorSource, Does.Contain("ClampLeftResizeTargetFrame"));
            Assert.That(editorSource, Does.Contain("ClampRightResizeTargetFrame"));
            Assert.That(editorSource, Does.Contain("CapturePointer(evt.pointerId)"));
            Assert.That(editorSource, Does.Contain("ReleasePointer(evt.pointerId)"));
            Assert.That(timelineViewSource, Does.Contain("ResolvePointerModeFromHandleBounds"));
            Assert.That(timelineViewSource, Does.Contain("ShouldApplyPointerDelta"));
            Assert.That(timelineViewSource, Does.Contain("public enum ResizeMode"));
            Assert.That(editorSource, Does.Contain("QueueInspectorTimelineRefresh"));
            Assert.That(editorSource, Does.Contain("RefreshTimelineFromSerializedPreservingSelection"));
            Assert.That(timelineViewSource, Does.Contain("AddClipTimingFields(timing, clip)"));
            Assert.That(timelineViewSource, Does.Contain("AddDelayedInspectorFloatField(parent, \"clip-start-seconds-field\", \"Start Seconds\""));
            Assert.That(timelineViewSource, Does.Contain("AddDelayedInspectorFloatField(parent, \"clip-duration-seconds-field\", \"Duration Seconds\""));
            Assert.That(timelineViewSource, Does.Contain("field.RegisterCallback<FocusOutEvent>(_ => QueueInspectorTimelineRefresh())"));
            Assert.That(timelineViewSource, Does.Not.Contain("field.RegisterCallback<SerializedPropertyChangeEvent>(_ => QueueInspectorTimelineRefresh())"));
            Assert.That(timelineViewSource, Does.Not.Contain("[DEBUG-TL-6F2E]"));
            Assert.That(timelineViewSource, Does.Not.Contain("CommittedActionTimelineDebugLog"));
            Assert.That(timelineViewSource, Does.Contain("serializedAsset.ApplyModifiedProperties();"));
            Assert.That(timelineViewSource, Does.Contain("timelineModel = new CommittedActionTimelineEditorModel(adapter);"));
            Assert.That(timelineViewSource, Does.Contain("SelectClip(restoreVariant, trackIndex, clipIndex, clipPath)"));
            Assert.That(editorSource, Does.Not.Contain("RegisterClipPointerTarget(this.Q(\"content\")"));
            Assert.That(editorSource, Does.Not.Contain("RegisterClipPointerTarget(this.Q(\"left-mixer\")"));
            Assert.That(editorSource, Does.Contain("RegisterPaneResizer"));
            Assert.That(editorSource, Does.Contain("onTrackSelected?.Invoke"));
            Assert.That(editorSource, Does.Contain("ApplyTrackSelectionStyles"));
            Assert.That(editorSource, Does.Contain("ResolvePaneResizeWidthFromParentLocalX"));
            Assert.That(editorSource, Does.Contain("PaneResizeAbsoluteMode.ParentLocalX"));
            Assert.That(editorSource, Does.Contain("PaneResizeAbsoluteMode.ParentWidthMinusLocalX"));
            Assert.That(editorSource, Does.Contain("RegisterPaneResizer(inspectorResizer, inspectorScroll, 260f, 960f, PaneResizeAbsoluteMode.ParentWidthMinusLocalX)"));
            Assert.That(editorSource, Does.Contain("dropArea.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown)"));
            Assert.That(editorSource, Does.Contain("delete-track-button"));
            Assert.That(editorSource, Does.Contain("add-track-menu"));
            Assert.That(editorSource, Does.Not.Contain("ResolveResizeMode"));
            Assert.That(editorSource, Does.Contain("BuildTrackInspector"));
            Assert.That(editorSource, Does.Contain("BuildClipInspector"));
            Assert.That(editorSource, Does.Contain("BuildClipPayloadInspector"));
            Assert.That(editorSource, Does.Contain("ActionTimelineClipKind.HitboxWindow"));
            Assert.That(editorSource, Does.Contain("AddBoundField(payloadSection, payload, \"factId\", \"Hitbox Fact Id\")"));
            Assert.That(editorSource, Does.Contain("AddMotionWarpInspector"));
            Assert.That(editorSource, Does.Not.Contain("new PropertyField(track,"));
            Assert.That(editorSource, Does.Not.Contain("new PropertyField(clip,"));
            Assert.That(editorSource, Does.Contain("ResolveSelectionMoveDeltaFrames"));
            Assert.That(editorSource, Does.Not.Contain("ClampMoveDeltaFrames"));
            Assert.That(clipTree, Does.Contain("ease-in-handler\" picking-mode=\"Ignore\""));
            Assert.That(clipTree, Does.Contain("ease-out-handler\" picking-mode=\"Ignore\""));
            Assert.That(clipStyle, Does.Contain("height: 30px;"));
            Assert.That(clipStyle, Does.Contain("width: 100%;"));
            Assert.That(clipStyle, Does.Contain("cursor: resize-horizontal;"));
            Assert.That(clipStyle, Does.Contain(".selected.previewActive.timelineClip"));
            Assert.That(editorStyle, Does.Contain("cursor: resize-horizontal;"));
            Assert.That(editorStyle, Does.Contain("max-width: 960px;"));
            Assert.That(editorStyle, Does.Contain("flex-grow: 0;"));
            Assert.That(editorStyle, Does.Contain("flex-shrink: 0;"));
            Assert.That(fieldStyle, Does.Contain("#inspector-scroll"));
            Assert.That(fieldStyle, Does.Contain("flex-grow: 0;"));
            Assert.That(fieldStyle, Does.Contain("flex-shrink: 0;"));
            Assert.That(fieldStyle, Does.Contain(".timelineInspectorSection"));
            Assert.That(fieldStyle, Does.Contain(".timelineInspectorFoldout"));
            Assert.That(fieldStyle, Does.Not.Contain("#inspector-scroll {\r\n    flex-grow: 1;"));
            Assert.That(fieldStyle, Does.Not.Contain("#inspector-scroll {\n    flex-grow: 1;"));
        }

        [Test]
        public void ClipManipulatorsResolveDeltaFromTheirOwnDragStart()
        {
            Assert.AreEqual(
                new Vector2(18f, -4f),
                CommittedActionTimelineDragLineManipulator.ResolveDelta(new Vector2(120f, 46f), new Vector2(102f, 50f)));
            Assert.AreEqual(
                new Vector2(-6f, 11f),
                CommittedActionTimelineDragManipulator.ResolveDelta(new Vector2(14f, 19f), new Vector2(20f, 8f)));
        }

        [Test]
        public void ClipManipulatorsUseStablePanelPointerPosition()
        {
            string manipulatorSource = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "Editor/Character/Action/Timeline/CommittedActionTimelineRefManipulators.cs"),
                Encoding.UTF8);
            string timelineViewSource = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "Editor/Character/Action/Timeline/CommittedActionRefPortedTimelineView.cs"),
                Encoding.UTF8);

            Assert.That(manipulatorSource, Does.Contain("DragBeginForce(evt, evt.position)"));
            Assert.That(manipulatorSource, Does.Contain("OnDragMove?.Invoke(ResolveDelta(evt.position, offset))"));
            Assert.That(manipulatorSource, Does.Contain("start = evt.position;"));
            Assert.That(manipulatorSource, Does.Contain("onDragMove?.Invoke(ResolveDelta(evt.position, start))"));
            Assert.That(manipulatorSource, Does.Not.Contain("DragBeginForce(evt, evt.localPosition)"));
            Assert.That(timelineViewSource, Does.Contain("moveDrag.DragBeginForce(evt, evt.position)"));
            Assert.That(timelineViewSource, Does.Not.Contain("moveDrag.DragBeginForce(evt, this.WorldToLocal(evt.position))"));
        }

        [Test]
        public void TimelineClipMoveWritesTickStableRange()
        {
            string timelineViewSource = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "Editor/Character/Action/Timeline/CommittedActionRefPortedTimelineView.cs"),
                Encoding.UTF8);
            string adapterSource = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "Editor/Character/Action/Timeline/CommittedActionTimelineEditorAdapters.cs"),
                Encoding.UTF8);

            Assert.That(adapterSource, Does.Contain("public bool MoveClipRange"));
            Assert.That(timelineViewSource, Does.Contain("clipMoveEndTicks.TryGetValue(selection.SelectionKey, out int endTick)"));
            Assert.That(timelineViewSource, Does.Contain("adapter.MoveClipRange("));
            Assert.That(timelineViewSource, Does.Contain("TickToSeconds(endTick + clampedDeltaFrames)"));
            Assert.That(timelineViewSource, Does.Not.Contain("adapter.MoveClip(selection.Variant, selection.TrackIndex, selection.ClipIndex, TickToSeconds(startTick + clampedDeltaFrames), out _)"));
        }

        [Test]
        public void TimelineClipSourceSeparatesMoveAndResizeManipulators()
        {
            string timelineViewSource = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "Editor/Character/Action/Timeline/CommittedActionRefPortedTimelineView.cs"),
                Encoding.UTF8);

            Assert.That(timelineViewSource, Does.Contain("moveDrag.DragBeginForce"));
            Assert.That(timelineViewSource, Does.Contain("new CommittedActionTimelineDragLineManipulator"));
            Assert.That(timelineViewSource, Does.Contain("delta => OnResizeDrag(0, delta)"));
            Assert.That(timelineViewSource, Does.Contain("delta => OnResizeDrag(1, delta)"));
            Assert.That(timelineViewSource, Does.Contain("ResolvePointerModeFromHandleBounds"));
            Assert.That(timelineViewSource, Does.Contain("ShouldApplyPointerDelta"));
            Assert.That(timelineViewSource, Does.Contain("public enum ResizeMode"));
        }

        [Test]
        public void ClipPixelDeltaUsesRefFramePositionAnchor()
        {
            float frameWidth = 22f;
            float fieldOffset = 6f;

            Assert.AreEqual(94f, CommittedActionTimelineClipView.FramePosition(4, frameWidth, fieldOffset), 0.0001f);
            Assert.AreEqual(5, CommittedActionTimelineClipView.ResolveFrameFromPixelDelta(4, 12f, frameWidth, fieldOffset));
            Assert.AreEqual(0, CommittedActionTimelineClipView.ResolveFrameFromPixelDelta(4, -120f, frameWidth, fieldOffset));
            Assert.AreEqual(1, CommittedActionTimelineClipView.ResolveFrameDeltaFromPixelDelta(4, 12f, frameWidth, fieldOffset));
            Assert.AreEqual(-4, CommittedActionTimelineClipView.ResolveFrameDeltaFromPixelDelta(4, -120f, frameWidth, fieldOffset));
        }

        [Test]
        public void ClipResizeFrameClampKeepsRefBoundaries()
        {
            Assert.AreEqual(0, CommittedActionTimelineClipView.ClampLeftResizeTargetFrame(-4, 5));
            Assert.AreEqual(4, CommittedActionTimelineClipView.ClampLeftResizeTargetFrame(8, 5));
            Assert.AreEqual(3, CommittedActionTimelineClipView.ClampRightResizeTargetFrame(-1, 2));
            Assert.AreEqual(9, CommittedActionTimelineClipView.ClampRightResizeTargetFrame(9, 2));
        }

        [Test]
        public void ClipFrameDeltaConvertsNegativeTicksForDragAndResize()
        {
            Assert.AreEqual(-1f / 60f, CommittedActionTimelineClipView.TickToSeconds(-1), 0.0001f);
            Assert.AreEqual(-4f / 60f, CommittedActionTimelineClipView.TickToSeconds(-4), 0.0001f);
            Assert.AreEqual(0f, CommittedActionTimelineClipView.TickToSeconds(0), 0.0001f);
            Assert.AreEqual(3f / 60f, CommittedActionTimelineClipView.TickToSeconds(3), 0.0001f);
        }

        [Test]
        public void ClipTimingInspectorEditsMapToTimelineRange()
        {
            CommittedActionRefPortedTimelineView.ResolveStartEdit(
                2f / 60f,
                5f / 60f,
                4f / 60f,
                out float movedStart,
                out float movedEnd);

            Assert.AreEqual(4f / 60f, movedStart, 0.0001f);
            Assert.AreEqual(7f / 60f, movedEnd, 0.0001f);
            Assert.AreEqual(3f / 60f, CommittedActionRefPortedTimelineView.ResolveEndEdit(3f / 60f, -1f), 0.0001f);
            Assert.AreEqual(9f / 60f, CommittedActionRefPortedTimelineView.ResolveDurationEdit(3f / 60f, 6f / 60f), 0.0001f);
        }

        [Test]
        public void PaneResizeMatchesFieldViewLayout()
        {
            Assert.AreEqual(
                640f,
                CommittedActionRefPortedTimelineView.ResolvePaneResizeWidthFromParentLocalX(
                    1200f,
                    640f,
                    CommittedActionRefPortedTimelineView.PaneResizeAbsoluteMode.ParentLocalX,
                    180f,
                    960f),
                0.001f);
            Assert.AreEqual(
                960f,
                CommittedActionRefPortedTimelineView.ResolvePaneResizeWidthFromParentLocalX(
                    1200f,
                    1100f,
                    CommittedActionRefPortedTimelineView.PaneResizeAbsoluteMode.ParentLocalX,
                    180f,
                    960f),
                0.001f);
            Assert.AreEqual(
                500f,
                CommittedActionRefPortedTimelineView.ResolvePaneResizeWidthFromParentLocalX(
                    1200f,
                    700f,
                    CommittedActionRefPortedTimelineView.PaneResizeAbsoluteMode.ParentWidthMinusLocalX,
                    180f,
                    960f),
                0.001f);
            Assert.AreEqual(
                960f,
                CommittedActionRefPortedTimelineView.ResolvePaneResizeWidthFromParentLocalX(
                    1200f,
                    100f,
                    CommittedActionRefPortedTimelineView.PaneResizeAbsoluteMode.ParentWidthMinusLocalX,
                    180f,
                    960f),
                0.001f);
            Assert.AreEqual(
                180f,
                CommittedActionRefPortedTimelineView.ResolvePaneResizeWidthFromParentLocalX(
                    1200f,
                    1100f,
                    CommittedActionRefPortedTimelineView.PaneResizeAbsoluteMode.ParentWidthMinusLocalX,
                    180f,
                    960f),
                0.001f);
        }

        static CharacterActionDefinitionSO LoadFormalDodgeAction()
        {
            CharacterActionDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterActionDefinitionSO>(DodgeAssetPath);
            Assert.NotNull(asset, DodgeAssetPath);
            return asset;
        }

        static CharacterActionDefinitionSO CloneDodgeAction()
        {
            CharacterActionDefinitionSO clone = Object.Instantiate(LoadFormalDodgeAction());
            clone.name = "DodgeEditorAdapterTest";
            return clone;
        }

        static int TrackCount(
            CommittedActionTimelineSerializedAdapter adapter,
            CommittedActionTimelineVariant variant)
        {
            Assert.True(adapter.TryGetTimelineProperty(variant, out SerializedProperty timeline, out string diagnostic), diagnostic);
            return timeline.FindPropertyRelative("tracks").arraySize;
        }

        static int ClipCount(
            CommittedActionTimelineSerializedAdapter adapter,
            CommittedActionTimelineVariant variant,
            int trackIndex)
        {
            return TrackAt(adapter, variant, trackIndex).FindPropertyRelative("clips").arraySize;
        }

        static ActionTimelineTrackKind TrackKindAt(
            CommittedActionTimelineSerializedAdapter adapter,
            CommittedActionTimelineVariant variant,
            int trackIndex)
        {
            return (ActionTimelineTrackKind)TrackAt(adapter, variant, trackIndex).FindPropertyRelative("kind").enumValueIndex;
        }

        static SerializedProperty TrackAt(
            CommittedActionTimelineSerializedAdapter adapter,
            CommittedActionTimelineVariant variant,
            int trackIndex)
        {
            Assert.True(adapter.TryGetTimelineProperty(variant, out SerializedProperty timeline, out string diagnostic), diagnostic);
            return timeline.FindPropertyRelative("tracks").GetArrayElementAtIndex(trackIndex);
        }

        static SerializedProperty ClipAt(
            CommittedActionTimelineSerializedAdapter adapter,
            CommittedActionTimelineVariant variant,
            int trackIndex,
            int clipIndex)
        {
            return TrackAt(adapter, variant, trackIndex)
                .FindPropertyRelative("clips")
                .GetArrayElementAtIndex(clipIndex);
        }

        static int FindTrack(
            CommittedActionTimelineSerializedAdapter adapter,
            CommittedActionTimelineVariant variant,
            ActionTimelineTrackKind kind)
        {
            Assert.True(adapter.TryGetTimelineProperty(variant, out SerializedProperty timeline, out string diagnostic), diagnostic);
            SerializedProperty tracks = timeline.FindPropertyRelative("tracks");
            for (int i = 0; i < tracks.arraySize; i++)
            {
                if ((ActionTimelineTrackKind)tracks.GetArrayElementAtIndex(i).FindPropertyRelative("kind").enumValueIndex == kind)
                    return i;
            }

            Assert.Fail($"Track not found: {kind}");
            return -1;
        }

        static CommittedActionBranchEvaluationContext Context(CharacterStateVariant variant, Vector3 direction)
        {
            CharacterInputRequestFact request = new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                1,
                4,
                30,
                variant,
                direction);
            return new CommittedActionBranchEvaluationContext(1, default, request, default, default);
        }

        static string ReadSource(string relativeFromAssets)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, relativeFromAssets));
            return string.Join(
                "\n",
                Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                    .Select(path => File.ReadAllText(path, Encoding.UTF8)));
        }

        static string ReadTextFiles(string relativeFromAssets, params string[] patterns)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, relativeFromAssets));
            return string.Join(
                "\n",
                patterns.SelectMany(pattern => Directory.GetFiles(root, pattern, SearchOption.AllDirectories))
                    .Select(path => File.ReadAllText(path, Encoding.UTF8)));
        }

        static string ReadRepoTextFiles(string relativeFromRepo, params string[] patterns)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../..", relativeFromRepo));
            if (!Directory.Exists(root))
                return string.Empty;
            return string.Join(
                "\n",
                patterns.SelectMany(pattern => Directory.GetFiles(root, pattern, SearchOption.AllDirectories))
                    .Select(path => File.ReadAllText(path, Encoding.UTF8)));
        }

        static ActionTimelineCompileContext CompileContext()
        {
            return new ActionTimelineCompileContext(1f / 60f);
        }

        static void CreatePreviewRig(
            out GameObject rig,
            out AnimancerComponent animancer,
            ActionAnimationKey key,
            AnimationClip clip)
        {
            rig = new GameObject("timeline-scene-preview-rig");
            rig.AddComponent<Animator>();
            animancer = rig.AddComponent<AnimancerComponent>();
            TransitionLibrary library = new TransitionLibrary();
            library.AddTransition(StringReference.Get(key.Value), CreateClipTransition(clip));
            animancer.Graph.Transitions = library;
        }

        static AnimationClip CreateClip(string name, float length)
        {
            AnimationClip clip = new AnimationClip { name = name };
            AnimationCurve curve = AnimationCurve.Linear(0f, 0f, Mathf.Max(0.01f, length), 1f);
            clip.SetCurve(string.Empty, typeof(Transform), "localPosition.x", curve);
            return clip;
        }

        static AnimationClip CreateChildClip(string name, string childPath, float length)
        {
            AnimationClip clip = new AnimationClip { name = name };
            AnimationCurve curve = AnimationCurve.Linear(0f, 0f, Mathf.Max(0.01f, length), 1f);
            clip.SetCurve(childPath, typeof(Transform), "localPosition.x", curve);
            return clip;
        }

        static Mesh CreateBlendShapeMesh(string name, string blendShapeName)
        {
            Mesh mesh = new Mesh { name = name };
            mesh.vertices = new[]
            {
                Vector3.zero,
                Vector3.right,
                Vector3.up
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            Vector3[] deltaVertices =
            {
                Vector3.forward * 0.01f,
                Vector3.forward * 0.01f,
                Vector3.forward * 0.01f
            };
            Vector3[] deltaNormals = new Vector3[3];
            Vector3[] deltaTangents = new Vector3[3];
            mesh.AddBlendShapeFrame(blendShapeName, 100f, deltaVertices, deltaNormals, deltaTangents);
            return mesh;
        }

        static ClipTransition CreateClipTransition(AnimationClip clip)
        {
            return new ClipTransition
            {
                Clip = clip,
                FadeDuration = 0.05f
            };
        }
    }
}
