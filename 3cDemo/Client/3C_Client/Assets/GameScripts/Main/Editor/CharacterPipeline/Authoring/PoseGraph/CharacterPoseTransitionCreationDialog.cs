using System;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    sealed class CharacterPoseTransitionCreationDialog : EditorWindow
    {
        CharacterPoseStateMachineDocument m_Document;
        GraphAuthoringElementId m_SourceId;
        GraphAuthoringElementId m_TargetId;
        AnimationTransitionBlendLogic m_BlendLogic =
            AnimationTransitionBlendLogic.StandardBlend;
        float m_DurationSeconds = 0.1f;
        CharacterAnimationBlendMode m_BlendMode =
            CharacterAnimationBlendMode.Linear;
        CharacterAnimationBlendCurveAsset m_CustomBlendCurve;
        CharacterAnimationBlendProfile m_BlendProfile;
        bool m_InitialCondition;
        CharacterPoseStateTransition m_Result;

        public static CharacterPoseStateTransition Show(
            CharacterPoseStateMachineDocument document,
            GraphAuthoringElementId sourceId,
            GraphAuthoringElementId targetId)
        {
            var window =
                CreateInstance<CharacterPoseTransitionCreationDialog>();
            window.m_Document = document ??
                throw new ArgumentNullException(nameof(document));
            window.m_SourceId = sourceId;
            window.m_TargetId = targetId;
            CharacterPoseStateTransition template =
                document.Definition.Transitions
                    .OrderBy(value => value.Priority)
                    .FirstOrDefault();
            if (template != null)
            {
                window.m_BlendLogic = template.BlendLogic;
                window.m_DurationSeconds =
                    template.DurationSeconds;
                window.m_BlendMode = template.BlendMode;
                window.m_CustomBlendCurve =
                    template.CustomBlendCurve;
                window.m_BlendProfile =
                    template.BlendProfile;
            }
            window.titleContent =
                new GUIContent("Create Pose Transition");
            window.minSize = new Vector2(420f, 280f);
            window.maxSize = new Vector2(720f, 420f);
            window.ShowModalUtility();
            CharacterPoseStateTransition result = window.m_Result;
            DestroyImmediate(window);
            return result;
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField(
                $"{m_SourceId.Value} → {m_TargetId.Value}",
                EditorStyles.boldLabel);
            m_BlendLogic =
                (AnimationTransitionBlendLogic)EditorGUILayout.EnumPopup(
                    "Blend Logic",
                    m_BlendLogic);
            m_DurationSeconds = EditorGUILayout.FloatField(
                "Duration Seconds",
                m_DurationSeconds);
            m_BlendMode =
                (CharacterAnimationBlendMode)EditorGUILayout.EnumPopup(
                    "Blend Mode",
                    m_BlendMode);
            if (m_BlendMode == CharacterAnimationBlendMode.Custom)
            {
                m_CustomBlendCurve =
                    (CharacterAnimationBlendCurveAsset)EditorGUILayout.ObjectField(
                        "Custom Blend Curve",
                        m_CustomBlendCurve,
                        typeof(CharacterAnimationBlendCurveAsset),
                        false);
            }
            else
            {
                m_CustomBlendCurve = null;
            }
            m_BlendProfile =
                (CharacterAnimationBlendProfile)EditorGUILayout.ObjectField(
                    "Blend Profile",
                    m_BlendProfile,
                    typeof(CharacterAnimationBlendProfile),
                    false);
            m_InitialCondition = EditorGUILayout.Toggle(
                "Initial Rule Result",
                m_InitialCondition);
            EditorGUILayout.HelpBox(
                "Create writes one formal typed Transition and one explicit BoolLiteral Transition Rule. Edit the rule after creation to replace the literal with business facts.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cancel"))
                    Close();
                using (new EditorGUI.DisabledScope(
                           !CanCreate()))
                {
                    if (GUILayout.Button("Create"))
                    {
                        m_Result = Create();
                        Close();
                    }
                }
            }
        }

        bool CanCreate()
        {
            if (m_Document == null || !m_SourceId.IsValid || !m_TargetId.IsValid)
                return false;
            try
            {
                CharacterPoseStateTransition.RequireBlendSettings(
                    m_BlendLogic,
                    m_DurationSeconds,
                    m_BlendMode,
                    m_CustomBlendCurve,
                    m_BlendProfile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        CharacterPoseStateTransition Create()
        {
            CharacterPoseStateTransitionSource source =
                m_Document.Definition.States.Any(value =>
                    value.StateId.Value == m_SourceId.Value)
                    ? CharacterPoseStateTransitionSource.FromState(
                        new PoseStateId(m_SourceId.Value))
                    : CharacterPoseStateTransitionSource.FromAlias(
                        new PoseStateAliasId(m_SourceId.Value));
            string operationIdentity = Guid.NewGuid().ToString("N");
            var operationId =
                new PoseTransitionRuleOperationId(operationIdentity);
            var rule = new CharacterPoseTransitionRuleGraph(
                new PoseTransitionRuleGraphId(
                    Guid.NewGuid().ToString("N")),
                Guid.NewGuid().ToString("N"),
                new[]
                {
                    new CharacterPoseTransitionRuleOperation(
                        operationId,
                        PoseTransitionRuleOperationKind.BoolLiteral,
                        boolLiteral: m_InitialCondition)
                },
                operationId);
            int priority = m_Document.Definition.Transitions.Count == 0
                ? 0
                : m_Document.Definition.Transitions.Max(
                    value => value.Priority) + 1;
            return new CharacterPoseStateTransition(
                new PoseStateTransitionId(
                    Guid.NewGuid().ToString("N")),
                source,
                new PoseStateId(m_TargetId.Value),
                priority,
                rule,
                m_BlendLogic,
                m_DurationSeconds,
                m_BlendMode,
                m_CustomBlendCurve,
                m_BlendProfile);
        }
    }
}
