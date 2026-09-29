using System;
using ThirdPersonCharacter.Pipeline.Animation.Resources;

namespace ThirdPersonCharacter.Pipeline.Animation.Sources
{
    internal sealed class CharacterPoseSourceReadinessJournal
    {
        struct DeferredSourceTarget
        {
            internal CharacterPoseSourceReadinessTarget Target;
            internal int ResolutionStart;
            internal int ResolutionCount;
        }

        readonly CharacterPoseSourceResourceResolver m_Resolver;
        readonly CharacterPoseSourceReadinessPage m_Page;
        readonly CharacterPoseSourceResourceResolution[] m_Scratch;
        readonly DeferredSourceTarget[] m_DeferredTargets;
        readonly CharacterPoseSourceResourceResolution[] m_DeferredResolutions;
        int m_DeferredTargetCount;
        ulong m_CompletionIdentity;

        internal CharacterPoseSourceReadinessJournal(
            CharacterAclResourceStore store,
            int sourceCapacity,
            int clipCapacity)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            if (sourceCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(sourceCapacity));
            if (clipCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(clipCapacity));
            int deferredResolutionCapacity =
                checked(sourceCapacity * clipCapacity);
            int pageCapacity = checked(deferredResolutionCapacity * 2);
            m_Resolver = new CharacterPoseSourceResourceResolver(store);
            m_Page = new CharacterPoseSourceReadinessPage(pageCapacity);
            m_Scratch =
                new CharacterPoseSourceResourceResolution[clipCapacity];
            m_DeferredTargets = new DeferredSourceTarget[sourceCapacity];
            m_DeferredResolutions =
                new CharacterPoseSourceResourceResolution[
                    deferredResolutionCapacity];
        }

        internal void BeginDemand(ulong completionIdentity)
        {
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            m_Page.Begin(completionIdentity);
            m_CompletionIdentity = completionIdentity;
            for (int targetIndex = 0;
                 targetIndex < m_DeferredTargetCount;
                 targetIndex++)
            {
                ref readonly DeferredSourceTarget target =
                    ref m_DeferredTargets[targetIndex];
                RecordResolutions(
                    CharacterPoseSourceReadinessCategory.DeferredTarget,
                    in target.Target,
                    m_DeferredResolutions,
                    target.ResolutionStart,
                    target.ResolutionCount,
                    completionIdentity);
            }
            ClearDeferredTargets();
        }

        internal bool TryDefer(
            in CharacterPoseSourceReadinessTarget target)
        {
            return TryDefer(in target, out _);
        }

        internal bool TryDefer(
            in CharacterPoseSourceReadinessTarget target,
            out CharacterPoseSourceResourceResolution resolution)
        {
            int count = m_Resolver.Resolve(
                in target,
                m_Scratch,
                out resolution);
            if (resolution.IsReady)
            {
                if (m_Page.IsOpen)
                {
                    RecordResolutions(
                        CharacterPoseSourceReadinessCategory.DeferredTarget,
                        in target,
                        m_Scratch,
                        0,
                        count,
                        m_CompletionIdentity);
                }
                else
                {
                    RemoveDeferred(in target);
                }
                return false;
            }
            if (m_Page.IsOpen)
            {
                RecordResolutions(
                    CharacterPoseSourceReadinessCategory.DeferredTarget,
                    in target,
                    m_Scratch,
                    0,
                    count,
                    m_CompletionIdentity);
            }
            else
            {
                SnapshotDeferred(in target, m_Scratch, count);
            }
            return true;
        }

        internal CharacterPoseSourceResourceResolution ResolveCurrent(
            ulong completionIdentity,
            in CharacterPoseSourcePreparation preparation)
        {
            if (completionIdentity == 0 ||
                completionIdentity != m_CompletionIdentity ||
                !m_Page.IsOpen)
            {
                throw new InvalidOperationException(
                    "Character Pose source readiness demand is stale.");
            }
            CharacterPoseSourceReadinessTarget target =
                CharacterPoseSourceReadinessTarget.FromPreparation(
                    in preparation);
            int count = m_Resolver.Resolve(
                in target,
                m_Scratch,
                out CharacterPoseSourceResourceResolution resolution);
            RecordResolutions(
                CharacterPoseSourceReadinessCategory.Current,
                in target,
                m_Scratch,
                0,
                count,
                completionIdentity);
            return resolution;
        }

        internal CharacterPoseSourceReadinessPageView Seal(
            ulong completionIdentity,
            in CharacterPoseSourcePreparationView preparations)
        {
            if (!preparations.IsValid ||
                preparations.CompletionIdentity != completionIdentity ||
                completionIdentity != m_CompletionIdentity)
            {
                throw new ArgumentException(
                    "Character Pose source preparation page is stale.",
                    nameof(preparations));
            }
            m_Page.Seal();
            return m_Page.Capture();
        }

        internal void Clear()
        {
            m_Page.Clear();
            ClearDeferredTargets();
            m_CompletionIdentity = 0;
        }

        void SnapshotDeferred(
            in CharacterPoseSourceReadinessTarget target,
            CharacterPoseSourceResourceResolution[] resolutions,
            int count)
        {
            CharacterPoseSourceReadinessKey key = CreateReadinessKey(in target);
            int targetIndex = FindDeferred(in key);
            if (targetIndex < 0)
            {
                if (m_DeferredTargetCount >= m_DeferredTargets.Length)
                    throw new InvalidOperationException(
                        "Character Pose deferred source capacity was exceeded.");
                targetIndex = m_DeferredTargetCount++;
            }
            int start = checked(targetIndex * m_Scratch.Length);
            m_DeferredTargets[targetIndex] = new DeferredSourceTarget
            {
                Target = target,
                ResolutionStart = start,
                ResolutionCount = count
            };
            Array.Copy(resolutions, 0, m_DeferredResolutions, start, count);
        }

        void RecordResolutions(
            CharacterPoseSourceReadinessCategory category,
            in CharacterPoseSourceReadinessTarget target,
            CharacterPoseSourceResourceResolution[] resolutions,
            int start,
            int count,
            ulong completionIdentity)
        {
            CharacterPoseSourceReadinessKey key = CreateReadinessKey(in target);
            m_Page.Remove(category, in key);
            for (int i = 0; i < count; i++)
            {
                ref readonly CharacterPoseSourceResourceResolution resolution =
                    ref resolutions[start + i];
                CharacterPoseSourceReadinessView readiness =
                    resolution.ToReadiness(completionIdentity);
                m_Page.Record(category, in key, in readiness);
            }
        }

        int FindDeferred(in CharacterPoseSourceReadinessKey key)
        {
            for (int i = 0; i < m_DeferredTargetCount; i++)
            {
                CharacterPoseSourceReadinessKey candidate =
                    CreateReadinessKey(in m_DeferredTargets[i].Target);
                if (candidate.Equals(key))
                    return i;
            }
            return -1;
        }

        void RemoveDeferred(in CharacterPoseSourceReadinessTarget target)
        {
            CharacterPoseSourceReadinessKey key = CreateReadinessKey(in target);
            int targetIndex = FindDeferred(in key);
            if (targetIndex < 0)
                return;
            int lastIndex = m_DeferredTargetCount - 1;
            for (int index = targetIndex; index < lastIndex; index++)
            {
                int targetStart = checked(index * m_Scratch.Length);
                int nextStart = checked((index + 1) * m_Scratch.Length);
                DeferredSourceTarget moved = m_DeferredTargets[index + 1];
                moved.ResolutionStart = targetStart;
                m_DeferredTargets[index] = moved;
                Array.Copy(
                    m_DeferredResolutions,
                    nextStart,
                    m_DeferredResolutions,
                    targetStart,
                    m_Scratch.Length);
            }
            m_DeferredTargetCount = lastIndex;
            m_DeferredTargets[lastIndex] = default;
            Array.Clear(
                m_DeferredResolutions,
                checked(lastIndex * m_Scratch.Length),
                m_Scratch.Length);
        }

        void ClearDeferredTargets()
        {
            if (m_DeferredTargetCount == 0)
                return;
            Array.Clear(m_DeferredTargets, 0, m_DeferredTargetCount);
            Array.Clear(
                m_DeferredResolutions,
                0,
                checked(m_DeferredTargetCount * m_Scratch.Length));
            m_DeferredTargetCount = 0;
        }

        static CharacterPoseSourceReadinessKey CreateReadinessKey(
            in CharacterPoseSourceReadinessTarget target) =>
            new CharacterPoseSourceReadinessKey(
                target.Kind,
                target.SourceId,
                target.PoseNodeId,
                target.BindingIndex);
    }
}
