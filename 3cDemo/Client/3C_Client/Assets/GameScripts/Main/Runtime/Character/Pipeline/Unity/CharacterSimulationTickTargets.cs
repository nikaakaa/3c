using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonGameplay.Tick;

namespace ThirdPersonCharacter.Pipeline
{
    internal sealed class CharacterPresentationFrameTarget : IGameplayPresentationFrameTarget
    {
        readonly CharacterSimulationPresentationRuntime m_Runtime;
        readonly CharacterPoseWorkerPresentationSession m_Session;
        bool m_Active;

        public CharacterPresentationFrameTarget(ICharacterPresentationRuntime runtime)
        {
            m_Runtime = runtime as CharacterSimulationPresentationRuntime ??
                throw new ArgumentException(
                    "Character Presentation Runtime has no Worker session.",
                    nameof(runtime));
            m_Session = m_Runtime.WorkerPresentationSession;
        }

        internal void Activate()
        {
            if (m_Active)
                return;
            m_Session.Register(this);
            m_Active = true;
        }

        internal void Deactivate()
        {
            if (!m_Active)
                return;
            m_Session.Unregister(this);
            m_Active = false;
        }

        public void PresentationFrame(GameplayPresentationFrameContext context)
        {
            m_Runtime.Present(context);
        }

        internal void BeginFrame(GameplayPresentationFrameContext context) =>
            m_Runtime.BeginPresentationFrame(context);

        internal bool TryAdvanceFrame(
            out CharacterPoseWorkerStageLease workerLease) =>
            m_Runtime.TryAdvancePresentationFrame(out workerLease);

        internal void CompleteFrame() =>
            m_Runtime.CompletePresentationFrame();

        internal Exception AbortFrame() =>
            m_Runtime.AbortPresentationFrame();
    }

    internal sealed class CharacterPoseWorkerPresentationSession :
        IGameplayPresentationFrameTarget,
        IDisposable
    {
        static CharacterPoseWorkerPresentationSession s_Current;
        GameplayTickSystem m_TickSystem;
        readonly List<CharacterPresentationFrameTarget> m_Targets =
            new List<CharacterPresentationFrameTarget>();
        readonly List<CharacterPresentationFrameTarget> m_ReadyTargets =
            new List<CharacterPresentationFrameTarget>();
        readonly List<Exception> m_FrameFailures =
            new List<Exception>();
        readonly CharacterPoseWorkerScheduler m_WorkerScheduler =
            new CharacterPoseWorkerScheduler();
        bool m_Disposed;

        CharacterPoseWorkerPresentationSession()
        {
        }

        internal static CharacterPoseWorkerPresentationSession RequireCurrent()
        {
            GameplayTickSystem tickSystem = GameplayTickSystem.Current;
            if (s_Current != null &&
                s_Current.m_TickSystem != null &&
                tickSystem != null &&
                !ReferenceEquals(s_Current.m_TickSystem, tickSystem))
            {
                s_Current.Dispose();
                s_Current = null;
            }
            return s_Current ??=
                new CharacterPoseWorkerPresentationSession();
        }

        internal CharacterPoseWorkerScheduler WorkerScheduler =>
            m_WorkerScheduler;

        internal void Register(CharacterPresentationFrameTarget target)
        {
            RequireAlive();
            AttachCurrentTickSystem();
            if (target == null || m_Targets.Contains(target))
                throw new InvalidOperationException(
                    "Character Presentation target registration is invalid.");
            m_Targets.Add(target);
            if (m_ReadyTargets.Capacity < m_Targets.Count)
                m_ReadyTargets.Capacity = m_Targets.Count;
        }

        internal void Unregister(CharacterPresentationFrameTarget target)
        {
            if (m_Disposed || target == null)
                return;
            m_Targets.Remove(target);
        }

        public void PresentationFrame(GameplayPresentationFrameContext context)
        {
            RequireAlive();
            m_ReadyTargets.Clear();
            m_FrameFailures.Clear();
            for (int i = 0; i < m_Targets.Count; i++)
            {
                CharacterPresentationFrameTarget target = m_Targets[i];
                try
                {
                    target.BeginFrame(context);
                    m_ReadyTargets.Add(target);
                }
                catch (Exception failure)
                {
                    m_FrameFailures.Add(failure);
                }
            }
            try
            {
                bool submitted;
                do
                {
                    submitted = false;
                    bool batchOpen = false;
                    try
                    {
                        m_WorkerScheduler.BeginBatch();
                        batchOpen = true;
                        for (int i = 0; i < m_ReadyTargets.Count;)
                        {
                            CharacterPresentationFrameTarget target =
                                m_ReadyTargets[i];
                            try
                            {
                                if (target.TryAdvanceFrame(
                                        out CharacterPoseWorkerStageLease lease))
                                {
                                    m_WorkerScheduler.Submit(in lease);
                                    submitted = true;
                                }
                                i++;
                            }
                            catch (Exception failure)
                            {
                                RecordActorFailure(
                                    failure,
                                    target.AbortFrame());
                                m_ReadyTargets.RemoveAt(i);
                            }
                        }
                        m_WorkerScheduler.CompleteBatch();
                        batchOpen = false;
                    }
                    catch
                    {
                        if (batchOpen)
                            m_WorkerScheduler.DiscardBatch();
                        throw;
                    }
                }
                while (submitted);
            }
            catch (Exception batchFailure)
            {
                Exception abortFailure = AbortReadyTargets();
                if (abortFailure != null)
                {
                    throw new AggregateException(
                        "Pose Worker Presentation session and frame abort both failed.",
                        batchFailure,
                        abortFailure);
                }
                throw;
            }
            for (int i = 0; i < m_ReadyTargets.Count; i++)
            {
                CharacterPresentationFrameTarget target = m_ReadyTargets[i];
                try
                {
                    target.CompleteFrame();
                }
                catch (Exception failure)
                {
                    RecordActorFailure(
                        failure,
                        target.AbortFrame());
                }
            }
            m_ReadyTargets.Clear();
            if (m_FrameFailures.Count > 0)
                throw new AggregateException(m_FrameFailures);
        }

        void RecordActorFailure(
            Exception frameFailure,
            Exception abortFailure)
        {
            m_FrameFailures.Add(abortFailure == null
                ? frameFailure
                : new AggregateException(
                    frameFailure,
                    abortFailure));
        }

        Exception AbortReadyTargets()
        {
            Exception failure = null;
            for (int i = m_ReadyTargets.Count - 1; i >= 0; i--)
            {
                Exception actorFailure = m_ReadyTargets[i].AbortFrame();
                if (actorFailure == null)
                    continue;
                failure = failure == null
                    ? actorFailure
                    : new AggregateException(failure, actorFailure);
            }
            m_ReadyTargets.Clear();
            return failure;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_TickSystem?.Unregister(this);
            m_Targets.Clear();
            m_ReadyTargets.Clear();
            m_FrameFailures.Clear();
            m_WorkerScheduler.Dispose();
            m_Disposed = true;
            if (ReferenceEquals(s_Current, this))
                s_Current = null;
        }

        void RequireAlive()
        {
            if (m_Disposed)
            {
                throw new ObjectDisposedException(
                    nameof(CharacterPoseWorkerPresentationSession));
            }
        }

        void AttachCurrentTickSystem()
        {
            GameplayTickSystem tickSystem = GameplayTickSystem.Current ??
                throw new InvalidOperationException(
                    "Gameplay Tick System is not initialized.");
            if (m_TickSystem == null)
            {
                m_TickSystem = tickSystem;
                m_TickSystem.Register(this);
                return;
            }
            if (!ReferenceEquals(m_TickSystem, tickSystem))
            {
                throw new InvalidOperationException(
                    "Pose Worker Presentation session belongs to another Gameplay Tick System.");
            }
        }
    }
}
