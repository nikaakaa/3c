using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Editor
{
    internal static class CharacterAnimationBlendCurveAuthoringService
    {
        public static void SetCurve(CharacterAnimationBlendCurveAsset asset, AnimationCurve curve)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            CharacterAnimationBlendCurveAsset.ToCanonicalCurve(curve).RequireValid();
            Undo.RecordObject(asset, "Edit Animation Blend Curve");
            asset.SetCurve(curve);
            EditorUtility.SetDirty(asset);
        }

        public static void RegenerateIdentity(CharacterAnimationBlendCurveAsset asset)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            Undo.RecordObject(asset, "Regenerate Animation Blend Curve Identity");
            asset.RegenerateIdentity();
            EditorUtility.SetDirty(asset);
        }
    }

    [CustomEditor(typeof(CharacterAnimationBlendCurveAsset))]
    public sealed class CharacterAnimationBlendCurveAssetEditor : UnityEditor.Editor
    {
        Label m_CurveId;
        Label m_Revision;
        Label m_Segments;
        HelpBox m_Diagnostic;
        CurveField m_Curve;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            m_CurveId = new Label();
            m_Revision = new Label();
            m_Segments = new Label();
            m_Diagnostic = new HelpBox(string.Empty, HelpBoxMessageType.Error);
            m_Curve = new CurveField("Curve")
            {
                ranges = new Rect(0f, 0f, 1f, 1f)
            };
            m_Curve.RegisterValueChangedCallback(evt => Commit(evt.newValue));

            var regenerate = new Button(() =>
            {
                CharacterAnimationBlendCurveAuthoringService.RegenerateIdentity(Asset);
                Refresh();
            }) { text = "Regenerate Identity" };

            root.Add(m_CurveId);
            root.Add(m_Revision);
            root.Add(m_Curve);
            root.Add(m_Segments);
            root.Add(m_Diagnostic);
            root.Add(regenerate);
            Refresh();
            return root;
        }

        CharacterAnimationBlendCurveAsset Asset => (CharacterAnimationBlendCurveAsset)target;

        void Commit(AnimationCurve draft)
        {
            try
            {
                CharacterAnimationBlendCurveAuthoringService.SetCurve(Asset, draft);
                Refresh();
            }
            catch (Exception exception)
            {
                m_Curve.SetValueWithoutNotify(Asset.Curve);
                m_Diagnostic.text = exception.Message;
                m_Diagnostic.style.display = DisplayStyle.Flex;
            }
        }

        void Refresh()
        {
            CharacterAnimationBlendCurveAsset asset = Asset;
            m_CurveId.text = $"Curve Id: {asset.CurveId}";
            try
            {
                asset.RequireValid();
                AnimationCurve curve = asset.Curve;
                m_Revision.text = $"Revision: {asset.Revision}";
                m_Segments.text = $"Canonical Hermite Segments: {curve.length - 1}";
                m_Curve.SetValueWithoutNotify(curve);
                m_Diagnostic.text = string.Empty;
                m_Diagnostic.style.display = DisplayStyle.None;
            }
            catch (Exception exception)
            {
                m_Revision.text = "Revision: Invalid";
                m_Segments.text = "Canonical Hermite Segments: Unavailable";
                m_Diagnostic.text = exception.Message;
                m_Diagnostic.style.display = DisplayStyle.Flex;
            }
        }
    }
}
