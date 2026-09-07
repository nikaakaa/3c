using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Resources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Unity.Resources;
using ThirdPersonGameplay.Tick;
using TEngine;

namespace ThirdPersonCharacter.Pipeline
{
    public class CharacterPresentationFrameTarget
    {
        readonly CharacterSimulationPresentationRuntime m_Runtime;
        readonly CharacterPoseWorkerPresentationSession m_Session;
        GameplayPresentationFrameContext m_FrameContext;
        bool m_FrameEnabled;
        bool m_Active;

        public CharacterPresentationFrameTarget(ICharacterPresentationRuntime runtime)
        {
            m_Runtime = runtime as CharacterSimulationPresentationRuntime ??
                throw new ArgumentException(
                    "Character Presentation Runtime has no Worker session.",
                    nameof(runtime));
            m_Session = m_Runtime.WorkerPresentationSession;
        }

        public void Activate()
        {
            if (m_Active)
                return;
            m_Session.Register(this);
            m_Active = true;
        }

        public void Deactivate()
        {
            if (!m_Active)
                return;
            m_Session.Unregister(this);
            m_Active = false;
        }

        internal void BeginFrame(GameplayPresentationFrameContext context)
        {
            m_FrameContext = context;
            m_FrameEnabled = PreparePresentationFrame(context);
            if (m_FrameEnabled)
                m_Runtime.BeginPresentationFrame(context);
        }

        internal bool TryAdvanceFrame(
            out CharacterPoseWorkerStageLease workerLease)
        {
            if (!m_FrameEnabled)
            {
                workerLease = default;
                return false;
            }
            return m_Runtime.TryAdvancePresentationFrame(out workerLease);
        }

        internal void CompleteFrame()
        {
            if (!m_FrameEnabled)
            {
                ClearFrame();
                return;
            }
            GameplayPresentationFrameContext context = m_FrameContext;
            m_Runtime.CompletePresentationFrame();
            m_FrameEnabled = false;
            CompletePresentationFrame(context);
            ClearFrame();
        }

        internal Exception AbortFrame()
        {
            Exception failure = m_FrameEnabled
                ? m_Runtime.AbortPresentationFrame()
                : null;
            ClearFrame();
            return failure;
        }

        protected virtual bool PreparePresentationFrame(
            GameplayPresentationFrameContext context) => true;

        protected virtual void CompletePresentationFrame(
            GameplayPresentationFrameContext context)
        {
        }

        void ClearFrame()
        {
            m_FrameContext = default;
            m_FrameEnabled = false;
        }
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
        readonly CharacterPoseWorkerScheduler m_WorkerScheduler;
        readonly CharacterAnimationResourceScope m_AnimationResources;
        readonly IResourceModule m_ResourceModule;
        readonly string m_PackageName;
        bool m_Disposed;

        CharacterPoseWorkerPresentationSession(
            IResourceModule resourceModule,
            string packageName)
        {
            m_ResourceModule = resourceModule ??
                throw new ArgumentNullException(nameof(resourceModule));
            m_PackageName = string.IsNullOrWhiteSpace(packageName)
                ? throw new ArgumentException(
                    "Animation resource package name is required.",
                    nameof(packageName))
                : packageName.Trim();
            m_WorkerScheduler = new CharacterPoseWorkerScheduler();
            try
            {
                m_AnimationResources = new CharacterAnimationResourceScope(
                    new YooAssetCharacterAnimationAssetLoader(
                        m_ResourceModule,
                        m_PackageName),
                    CharacterAnimationResourceSettings.CorinInitial);
            }
            catch
            {
                m_WorkerScheduler.Dispose();
                throw;
            }
        }

        internal static CharacterPoseWorkerPresentationSession RequireCurrent(
            IResourceModule resourceModule,
            string packageName)
        {
            resourceModule = resourceModule ??
                throw new ArgumentNullException(nameof(resourceModule));
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentException(
                    "Animation resource package name is required.",
                    nameof(packageName));
            packageName = packageName.Trim();
            GameplayTickSystem tickSystem = GameplayTickSystem.Current;
            if (s_Current != null &&
                s_Current.m_TickSystem != null &&
                tickSystem != null &&
                !ReferenceEquals(s_Current.m_TickSystem, tickSystem))
            {
                s_Current.Dispose();
                s_Current = null;
            }
            if (s_Current != null &&
                (!ReferenceEquals(s_Current.m_ResourceModule, resourceModule) ||
                 !string.Equals(
                     s_Current.m_PackageName,
                     packageName,
                     StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Pose Worker Presentation session resource configuration does not match.");
            }
            return s_Current ??=
                new CharacterPoseWorkerPresentationSession(
                    resourceModule,
                    packageName);
        }

        internal CharacterPoseWorkerScheduler WorkerScheduler =>
            m_WorkerScheduler;

        internal CharacterAnimationResourceScope AnimationResources =>
            m_AnimationResources;

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
            m_AnimationResources.AdvancePreparation();
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
                    RecordActorFailure(
                        failure,
                        target.AbortFrame());
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
            try
            {
                m_WorkerScheduler.Dispose();
            }
            finally
            {
                m_AnimationResources.Dispose();
            }
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
