using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Tags;
using ThirdPersonCharacter.Pipeline.Network;

namespace ThirdPersonCharacter.ActionSystem
{
    public sealed class ActionRuntime : IActionRuntimeService
    {
        readonly Dictionary<string, ActionProfile> m_Profiles = new Dictionary<string, ActionProfile>(StringComparer.Ordinal);
        readonly IGameplayTagReader m_TagReader;
        readonly IGameplayTagSourceSink m_TagSourceSink;
        readonly List<ActionActivationRequest> m_DiagnosticActivationRequests = new List<ActionActivationRequest>();
        readonly List<ActionLifecycleTransition> m_DiagnosticLifecycleTransitions = new List<ActionLifecycleTransition>();
        readonly List<ActionWindowSample> m_DiagnosticWindowSamples = new List<ActionWindowSample>();
        readonly List<GameplayCueFact> m_DiagnosticCueEvents = new List<GameplayCueFact>();
        readonly List<GameplayResultEvent> m_DiagnosticGameplayResults = new List<GameplayResultEvent>();
        readonly Dictionary<ulong, ActionProfile> m_InstanceProfiles = new Dictionary<ulong, ActionProfile>();
        readonly HashSet<ulong> m_InstanceProfilesPendingRelease = new HashSet<ulong>();
        ActionProfile m_ActiveProfile;
        ActionInstance m_ActiveInstance;
        ulong m_NextInstanceId = 1;
        ulong m_NextPredictionKey = 1;

        public ActionRuntime(IGameplayTagReader tagReader, IGameplayTagSourceSink tagSourceSink)
        {
            m_TagReader = tagReader ?? throw new ArgumentNullException(nameof(tagReader));
            m_TagSourceSink = tagSourceSink ?? throw new ArgumentNullException(nameof(tagSourceSink));
        }

        public ActionContext ActionContext => new ActionContext(m_ActiveProfile, m_ActiveInstance);
        public bool HasActiveAction => m_ActiveInstance != null;
        public IReadOnlyList<ActionActivationRequest> DiagnosticActivationRequests => m_DiagnosticActivationRequests;
        public IReadOnlyList<ActionLifecycleTransition> DiagnosticLifecycleTransitions => m_DiagnosticLifecycleTransitions;
        public IReadOnlyList<ActionWindowSample> DiagnosticWindowSamples => m_DiagnosticWindowSamples;
        public IReadOnlyList<GameplayCueFact> DiagnosticCueEvents => m_DiagnosticCueEvents;
        public IReadOnlyList<GameplayResultEvent> DiagnosticGameplayResults => m_DiagnosticGameplayResults;

        public void BeginLogicTick()
        {
            foreach (ulong actionInstanceId in m_InstanceProfilesPendingRelease)
                m_InstanceProfiles.Remove(actionInstanceId);
            m_InstanceProfilesPendingRelease.Clear();
            ClearDiagnosticEvents();
        }

        public bool RegisterProfile(ActionProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.ActionId))
                return false;

            m_Profiles[profile.ActionId] = profile;
            return true;
        }

        public bool UnregisterProfile(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                return false;

            if (m_ActiveProfile != null && string.Equals(m_ActiveProfile.ActionId, actionId, StringComparison.Ordinal))
                return false;

            return m_Profiles.Remove(actionId);
        }

        public void ClearProfiles()
        {
            if (m_ActiveInstance != null)
                return;

            m_Profiles.Clear();
        }

        public bool TryGetProfile(string actionId, out ActionProfile profile)
        {
            profile = null;
            return !string.IsNullOrEmpty(actionId) && m_Profiles.TryGetValue(actionId, out profile);
        }

        public bool TryGetProfile(ulong actionInstanceId, out ActionProfile profile)
        {
            profile = null;
            return actionInstanceId != 0 && m_InstanceProfiles.TryGetValue(actionInstanceId, out profile);
        }

        public bool TryGetActionId(ulong actionInstanceId, out string actionId)
        {
            actionId = string.Empty;
            if (!TryGetProfile(actionInstanceId, out ActionProfile profile))
                return false;

            actionId = profile.ActionId;
            return !string.IsNullOrEmpty(actionId);
        }

        public ActionActivationOutcome ActivateAction(ActionActivationRequest request)
        {
            return TryActivate(request);
        }

        public bool ApplyActionLifecycleTransition(ActionLifecycleTransition transition)
        {
            if (transition.IsValid)
                m_DiagnosticLifecycleTransitions.Add(transition);

            if (!transition.IsValid || !IsActive(transition.ActionInstanceId))
                return false;

            m_ActiveInstance.ApplyLifecycleTransition(transition);
            if (transition.IsTerminal)
            {
                m_TagSourceSink.RemoveSource(GameplayTagSourceHandle.ActionInstance(transition.ActionInstanceId));
                m_InstanceProfilesPendingRelease.Add(transition.ActionInstanceId);
                ClearActive();
            }
            return true;
        }

        ActionActivationOutcome TryActivate(ActionActivationRequest request)
        {
            m_DiagnosticActivationRequests.Add(request);
            ActionActivationResult result = ValidateActivation(request, out ActionProfile profile);
            if (result != ActionActivationResult.Activated)
                return new ActionActivationOutcome(result, default);

            ActionLifecycleTransition replacedTransition = default;
            if (m_ActiveInstance != null)
            {
                replacedTransition = new ActionLifecycleTransition(
                    m_ActiveInstance.InstanceId,
                    ActionLifecycleTransitionType.Cancel,
                    request.LocalLogicTick,
                    request.InputSequence,
                    "CancelledByNewAction",
                    request.SourceGraphId,
                    request.SourceNodeId,
                    request.SourceName);
                ApplyActionLifecycleTransition(replacedTransition);
            }

            ActionInstance instance = new ActionInstance(
                m_NextInstanceId++,
                profile.ActionId,
                m_NextPredictionKey++,
                request.SourceInputRequestId,
                request.InputSequence,
                request.LocalLogicTick,
                request.TargetKey,
                request.TargetSnapshot);
            instance.SetSourceIdentity(request.SourceGraphId, request.SourceNodeId, request.SourceName);

            if (!m_TagSourceSink.SetSourceTags(
                    GameplayTagSourceHandle.ActionInstance(instance.InstanceId),
                    profile.Tags))
            {
                throw new InvalidOperationException(
                    $"Action '{profile.ActionId}' contains tags rejected by the configured Gameplay Tag Catalog.");
            }

            m_ActiveProfile = profile;
            m_ActiveInstance = instance;
            m_InstanceProfiles[instance.InstanceId] = profile;
            return new ActionActivationOutcome(ActionActivationResult.Activated, ActionInstanceHandle.From(instance), replacedTransition);
        }

        public ActionActivationResult ValidateActivation(ActionActivationRequest request, out ActionProfile profile)
        {
            profile = null;
            if (!request.IsValid)
                return ActionActivationResult.InvalidRequest;

            if (!m_Profiles.TryGetValue(request.ActionId, out profile))
                return ActionActivationResult.MissingProfile;

            if (profile.BlockTags == null)
                throw new InvalidOperationException($"Action '{profile.ActionId}' BlockTags query is missing.");
            if (!profile.BlockTags.IsEmpty && m_TagReader.Matches(profile.BlockTags))
                return ActionActivationResult.Blocked;

            if (m_ActiveInstance != null && !CanCancelActiveAction(profile))
                return ActionActivationResult.AlreadyActive;

            return ActionActivationResult.Activated;
        }

        public void RecordOutput(ActionWindowSample sample)
        {
            if (sample.ActionInstanceId != 0)
                m_DiagnosticWindowSamples.Add(sample);
        }

        public void RecordOutput(GameplayCueFact cue)
        {
            if (cue.SourceActionInstanceId != 0)
                m_DiagnosticCueEvents.Add(cue);
        }

        public void RecordOutput(GameplayResultEvent resultEvent)
        {
            if (resultEvent.ActionInstanceId != 0)
                m_DiagnosticGameplayResults.Add(resultEvent);
        }

        public void ClearDiagnosticEvents()
        {
            m_DiagnosticActivationRequests.Clear();
            m_DiagnosticLifecycleTransitions.Clear();
            m_DiagnosticWindowSamples.Clear();
            m_DiagnosticCueEvents.Clear();
            m_DiagnosticGameplayResults.Clear();
        }

        public void ResetExecution()
        {
            foreach (ulong actionInstanceId in m_InstanceProfiles.Keys)
                m_TagSourceSink.RemoveSource(GameplayTagSourceHandle.ActionInstance(actionInstanceId));
            ClearActive();
            ClearDiagnosticEvents();
            m_InstanceProfiles.Clear();
            m_InstanceProfilesPendingRelease.Clear();
        }

        public void Reset()
        {
            ResetExecution();
            m_Profiles.Clear();
            m_NextInstanceId = 1;
            m_NextPredictionKey = 1;
        }

        public bool IsActionActive(ulong actionInstanceId)
        {
            return IsActive(actionInstanceId);
        }

        public bool TryGetActiveHandle(ulong actionInstanceId, out ActionInstanceHandle handle)
        {
            handle = default;
            if (!IsActive(actionInstanceId))
                return false;

            handle = ActionInstanceHandle.From(m_ActiveInstance);
            return handle.IsValid;
        }

        public bool SetPhase(ulong actionInstanceId, ActionPhase phase)
        {
            if (!IsActive(actionInstanceId))
                return false;

            m_ActiveInstance.SetPhase(phase);
            return true;
        }

        bool IsActive(ulong actionInstanceId)
        {
            return actionInstanceId != 0 && m_ActiveInstance != null && m_ActiveInstance.InstanceId == actionInstanceId;
        }

        bool CanCancelActiveAction(ActionProfile nextProfile)
        {
            if (m_ActiveProfile == null)
                return true;

            if (nextProfile.CancelTags == null)
                throw new InvalidOperationException($"Action '{nextProfile.ActionId}' CancelTags query is missing.");
            return !nextProfile.CancelTags.IsEmpty &&
                m_TagReader.Matches(nextProfile.CancelTags, m_ActiveProfile.Tags);
        }

        void ClearActive()
        {
            m_ActiveProfile = null;
            m_ActiveInstance = null;
        }

    }
}
