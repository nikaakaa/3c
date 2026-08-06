using System;
using System.Collections.Generic;
using TreeDesigner.Editor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    sealed class CharacterPoseGraphEditorMutationAdapter :
        IGraphAuthoringDomainMutation
    {
        readonly CharacterTypedPoseGraphMutationAdapter m_Authoring;
        readonly Func<IGraphAuthoringDocumentProjection,
            GraphAuthoringMutationRequest,
            bool> m_TryApplyTuning;

        public CharacterPoseGraphEditorMutationAdapter(
            CharacterTypedPoseGraphMutationAdapter authoring,
            Func<IGraphAuthoringDocumentProjection,
                GraphAuthoringMutationRequest,
                bool> tryApplyTuning)
        {
            m_Authoring = authoring ??
                throw new ArgumentNullException(nameof(authoring));
            m_TryApplyTuning = tryApplyTuning ??
                throw new ArgumentNullException(nameof(tryApplyTuning));
        }

        public bool ReadOnly
        {
            get => m_Authoring.ReadOnly;
            set => m_Authoring.ReadOnly = value;
        }

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringMutationRequest request)
        {
            if (ReadOnly)
                throw new InvalidOperationException(
                    "Pose Graph document is read-only.");
            if (m_TryApplyTuning(document, request))
                return;
            m_Authoring.Apply(document, request);
        }

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            IReadOnlyList<GraphAuthoringMutationRequest> requests) =>
            m_Authoring.Apply(document, requests);
    }

    sealed class CharacterPoseStateMachineEditorMutationAdapter :
        IGraphAuthoringDomainMutation
    {
        readonly CharacterPoseStateMachineMutationAdapter m_Authoring;
        readonly Func<IGraphAuthoringDocumentProjection,
            GraphAuthoringMutationRequest,
            bool> m_TryApplyTuning;

        public CharacterPoseStateMachineEditorMutationAdapter(
            CharacterPoseStateMachineMutationAdapter authoring,
            Func<IGraphAuthoringDocumentProjection,
                GraphAuthoringMutationRequest,
                bool> tryApplyTuning)
        {
            m_Authoring = authoring ??
                throw new ArgumentNullException(nameof(authoring));
            m_TryApplyTuning = tryApplyTuning ??
                throw new ArgumentNullException(nameof(tryApplyTuning));
        }

        public bool ReadOnly
        {
            get => m_Authoring.ReadOnly;
            set => m_Authoring.ReadOnly = value;
        }

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringMutationRequest request)
        {
            if (ReadOnly)
                throw new InvalidOperationException(
                    "Pose State Machine is read-only.");
            if (m_TryApplyTuning(document, request))
                return;
            m_Authoring.Apply(document, request);
        }

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            IReadOnlyList<GraphAuthoringMutationRequest> requests) =>
            m_Authoring.Apply(document, requests);
    }
}
