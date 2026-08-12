using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed partial class CharacterPresentationPoseGraphEditorWindow
    {
        string m_LastPublishedPoseGraphRevision = string.Empty;
        string m_TuningOnlyAuthoringFingerprint = string.Empty;
        bool m_TuningOnlyAuthoringChange;

        internal void ResetPoseTuningAuthoringState()
        {
            m_LastPublishedPoseGraphRevision = string.Empty;
            m_TuningOnlyAuthoringFingerprint = string.Empty;
            m_TuningOnlyAuthoringChange = false;
        }

        internal void CapturePublishedPoseGraphRevision(CharacterTypedPoseGraph graph)
        {
            if (graph == null || !m_Asset || graph != m_Asset.Graph)
                return;
            if (!m_TuningOnlyAuthoringChange ||
                string.IsNullOrEmpty(m_LastPublishedPoseGraphRevision))
                m_LastPublishedPoseGraphRevision = graph.ContentRevision;
        }

        internal void MarkPoseTuningAuthoringChanged()
        {
            m_TuningOnlyAuthoringChange = true;
            m_TuningOnlyAuthoringFingerprint = ComputePoseTuningAuthoringFingerprint();
            if (m_Status != null)
                m_Status.text = "Unpublished Parameter · published Projection remains active.";
            m_PreviewPanel?.Refresh();
            RefreshSelectedDetails();
        }

        bool IsTuningOnlyAuthoringChange()
        {
            return m_TuningOnlyAuthoringChange &&
                   !string.IsNullOrEmpty(m_TuningOnlyAuthoringFingerprint) &&
                   string.Equals(
                       m_TuningOnlyAuthoringFingerprint,
                       ComputePoseTuningAuthoringFingerprint(),
                       StringComparison.Ordinal);
        }

        void ClearPoseTuningAuthoringChange()
        {
            m_TuningOnlyAuthoringChange = false;
            m_TuningOnlyAuthoringFingerprint = string.Empty;
        }

        string ComputePoseTuningAuthoringFingerprint()
        {
            if (!m_Asset || m_Asset.Graph == null)
                return string.Empty;
            var parts = new List<string>();
            foreach (CharacterTypedPoseGraph graph in m_Asset.EnumerateGraphs()
                         .Where(value => value != null)
                         .OrderBy(value => value.GraphId.Value, StringComparer.Ordinal))
            {
                parts.Add($"graph:{graph.GraphId.Value}:{graph.ContentRevision}");
            }
            foreach (CharacterPoseStateMachineDefinition machine in m_Asset.EnumerateStateMachines()
                         .Where(value => value != null)
                         .OrderBy(value => value.StateMachineId.Value, StringComparer.Ordinal))
            {
                parts.Add($"state-machine:{machine.StateMachineId.Value}:{machine.ContentRevision}");
            }
            var profiles = new Dictionary<string, string>(StringComparer.Ordinal);
            if (m_Profile?.FullBodyIkProfile)
            {
                CharacterFullBodyIkProfile profile = m_Profile.FullBodyIkProfile;
                profiles[$"full-body:{profile.ProfileId}"] = profile.Revision;
            }
            foreach (CharacterTypedPoseGraph graph in m_Asset.EnumerateGraphs()
                         .Where(value => value != null))
            {
                foreach (CharacterTypedPoseNode node in graph.Nodes)
                {
                    if (node?.Payload is CharacterFullBodyIkPosePayload fullBody &&
                        fullBody.Profile)
                    {
                        profiles[$"full-body:{fullBody.Profile.ProfileId}"] =
                            fullBody.Profile.Revision;
                    }
                    if (node?.Payload is CharacterFootPlacementPosePayload foot &&
                        foot.Profile)
                    {
                        profiles[$"foot-placement:{foot.Profile.ProfileId}"] =
                            foot.Profile.Revision;
                    }
                }
            }
            foreach (KeyValuePair<string, string> profile in profiles.OrderBy(
                         value => value.Key,
                         StringComparer.Ordinal))
            {
                parts.Add($"profile:{profile.Key}:{profile.Value}");
            }
            return string.Join("|", parts);
        }
    }
}
