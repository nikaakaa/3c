using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public readonly struct CharacterAnimationClipOpenRequest
    {
        public CharacterAnimationClipOpenRequest(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationProfile profile,
            AnimationClip clip,
            GameObject previewTarget)
        {
            Definition = definition;
            Profile = profile;
            Clip = clip;
            PreviewTarget = previewTarget;
        }

        public CharacterPipelineDefinition Definition { get; }
        public CharacterAnimationPresentationProfile Profile { get; }
        public AnimationClip Clip { get; }
        public GameObject PreviewTarget { get; }

        public void RequireValid()
        {
            if (!Definition || !Profile || !Clip || !PreviewTarget ||
                Definition.AnimationPresentationProfile != Profile ||
                EditorUtility.IsPersistent(PreviewTarget))
            {
                throw new InvalidOperationException(
                    "AnimationClip open request requires one exact Definition, Profile, Clip and scene Preview Target.");
            }
            _ = CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(Clip);
        }
    }

    public static class CharacterAnimationClipAuthoringService
    {
        public static void Open(CharacterAnimationClipOpenRequest request)
        {
            request.RequireValid();
            CharacterAnimationClipAuthoringCurveReceiver[] receivers =
                request.PreviewTarget.GetComponents<CharacterAnimationClipAuthoringCurveReceiver>();
            if (receivers.Length == 0)
                Undo.AddComponent<CharacterAnimationClipAuthoringCurveReceiver>(request.PreviewTarget);
            else if (receivers.Length != 1)
                throw new InvalidOperationException("AnimationClip Preview Target contains duplicate authoring Curve receivers.");
            Selection.activeGameObject = request.PreviewTarget;
            AnimationWindow window = EditorWindow.GetWindow<AnimationWindow>();
            window.animationClip = request.Clip;
            window.Show();
            window.Focus();
        }

        public static void ReplaceRegisteredCurve(
            AnimationClip clip,
            string channelId,
            AnimationCurve curve,
            string undoName)
        {
            if (string.IsNullOrWhiteSpace(undoName))
                throw new ArgumentException("AnimationClip Curve mutation Undo name is required.", nameof(undoName));
            Undo.RecordObject(clip, undoName);
            CharacterAnimationClipRegisteredCurveCatalog.Replace(clip, channelId, curve);
        }
    }
}
