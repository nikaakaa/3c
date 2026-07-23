using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public sealed class MotionMatchingCandidateAdmission
    {
        readonly CharacterMotionMatchingRuntimeDatabase m_Database;

        public MotionMatchingCandidateAdmission(CharacterMotionMatchingRuntimeDatabase database)
        {
            m_Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public bool Admit(MotionMatchingQuery query, int sampleIndex, out MotionMatchingCandidateRejectDetail rejectDetail)
        {
            if ((uint)sampleIndex >= (uint)m_Database.SampleCount)
                throw new ArgumentOutOfRangeException(nameof(sampleIndex));
            MotionMatchingSamplePayload candidate = m_Database.GetSample(sampleIndex);
            if (!query.DatabaseIdentity.EqualsExact(m_Database.ArtifactIdentity))
                return Reject(MotionMatchingCandidateRejectReason.IdentityMismatch, 0f, 1f, out rejectDetail);
            if (!candidate.SearchDomainId.Equals(query.SearchDomainId) || !query.SearchDomainId.Equals(m_Database.SearchDomainId))
                return Reject(MotionMatchingCandidateRejectReason.SearchDomainMismatch, 0f, 1f, out rejectDetail);
            bool continuation = !query.Initialization && query.CurrentSelectionInDatabase && query.CurrentSampleIndex >= 0 &&
                m_Database.GetSample(query.CurrentSampleIndex).NextSampleIndex == sampleIndex;
            if (query.Initialization && !candidate.CanInitialize)
                return Reject(MotionMatchingCandidateRejectReason.InitializationNotAllowed, candidate.CanInitialize ? 1f : 0f, 1f, out rejectDetail);
            if (!query.Initialization && !continuation && !candidate.CanJumpInto)
                return Reject(MotionMatchingCandidateRejectReason.JumpNotAllowed, candidate.CanJumpInto ? 1f : 0f, 1f, out rejectDetail);
            if (candidate.EntryExcluded)
                return Reject(MotionMatchingCandidateRejectReason.EntryExcluded, 1f, 0f, out rejectDetail);
            if (candidate.ExitExcluded)
                return Reject(MotionMatchingCandidateRejectReason.ExitExcluded, 1f, 0f, out rejectDetail);
            if (!query.Initialization && !continuation && query.SecondsSinceLastJump < m_Database.SearchPolicy.MinimumJumpInterval)
                return Reject(MotionMatchingCandidateRejectReason.MinimumJumpInterval, query.SecondsSinceLastJump, m_Database.SearchPolicy.MinimumJumpInterval, out rejectDetail);
            if (!CanCoverPlanHorizon(sampleIndex, out bool brokenContinuation, out int coveredSamples))
            {
                return Reject(
                    brokenContinuation ? MotionMatchingCandidateRejectReason.BrokenContinuation : MotionMatchingCandidateRejectReason.InsufficientPlanHorizon,
                    coveredSamples,
                    m_Database.SearchPolicy.PlanSampleCount,
                    out rejectDetail);
            }
            if (!ContactMaskCompatible(query.ContactProtection.ProtectedMask, candidate.ContactMask, MotionMatchingFootContactMask.Left))
                return Reject(MotionMatchingCandidateRejectReason.LeftContactMismatch, (float)(candidate.ContactMask & MotionMatchingFootContactMask.Left), (float)MotionMatchingFootContactMask.Left, out rejectDetail);
            if (!ContactMaskCompatible(query.ContactProtection.ProtectedMask, candidate.ContactMask, MotionMatchingFootContactMask.Right))
                return Reject(MotionMatchingCandidateRejectReason.RightContactMismatch, (float)(candidate.ContactMask & MotionMatchingFootContactMask.Right), (float)MotionMatchingFootContactMask.Right, out rejectDetail);
            float positionLimit = m_Database.SearchPolicy.ProtectedFootPositionJumpLimit;
            float velocityLimit = m_Database.SearchPolicy.ProtectedFootVelocityJumpLimit;
            if ((query.ContactProtection.ProtectedMask & MotionMatchingFootContactMask.Left) != 0)
            {
                float positionJump = (candidate.LeftFootRootPosition - query.ContactProtection.LeftRootPosition).magnitude;
                if (positionJump > positionLimit)
                    return Reject(MotionMatchingCandidateRejectReason.LeftContactPositionJump, positionJump, positionLimit, out rejectDetail);
                float velocityJump = (candidate.LeftFoot.SoleLocalVelocity - query.ContactProtection.LeftRootVelocity).magnitude;
                if (velocityJump > velocityLimit)
                    return Reject(MotionMatchingCandidateRejectReason.LeftContactVelocityJump, velocityJump, velocityLimit, out rejectDetail);
            }
            if ((query.ContactProtection.ProtectedMask & MotionMatchingFootContactMask.Right) != 0)
            {
                float positionJump = (candidate.RightFootRootPosition - query.ContactProtection.RightRootPosition).magnitude;
                if (positionJump > positionLimit)
                    return Reject(MotionMatchingCandidateRejectReason.RightContactPositionJump, positionJump, positionLimit, out rejectDetail);
                float velocityJump = (candidate.RightFoot.SoleLocalVelocity - query.ContactProtection.RightRootVelocity).magnitude;
                if (velocityJump > velocityLimit)
                    return Reject(MotionMatchingCandidateRejectReason.RightContactVelocityJump, velocityJump, velocityLimit, out rejectDetail);
            }
            try
            {
                MotionMatchingClipBindingPayload clip = m_Database.GetClipBinding(candidate.ClipBindingIndex);
                if (clip == null || !clip.RootLocked || !clip.Clip)
                    return Reject(MotionMatchingCandidateRejectReason.MissingClipBinding, 0f, 1f, out rejectDetail);
            }
            catch (ArgumentOutOfRangeException)
            {
                return Reject(MotionMatchingCandidateRejectReason.MissingClipBinding, 0f, 1f, out rejectDetail);
            }
            rejectDetail = default;
            return true;
        }

        bool CanCoverPlanHorizon(int entrySampleIndex, out bool brokenContinuation, out int coveredSamples)
        {
            int sampleIndex = entrySampleIndex;
            brokenContinuation = false;
            coveredSamples = 1;
            for (int step = 1; step < m_Database.SearchPolicy.PlanSampleCount; step++)
            {
                MotionMatchingSamplePayload sample = m_Database.GetSample(sampleIndex);
                if (sample.NextSampleIndex < 0)
                {
                    if (sample.Terminal)
                        return true;
                    brokenContinuation = true;
                    return false;
                }
                if ((uint)sample.NextSampleIndex >= (uint)m_Database.SampleCount)
                {
                    brokenContinuation = true;
                    return false;
                }
                sampleIndex = sample.NextSampleIndex;
                coveredSamples++;
            }
            return true;
        }

        static bool ContactMaskCompatible(MotionMatchingFootContactMask protectedMask, MotionMatchingFootContactMask candidateMask, MotionMatchingFootContactMask foot) =>
            (protectedMask & foot) == 0 || (candidateMask & foot) != 0;

        static bool Reject(
            MotionMatchingCandidateRejectReason reason,
            float value,
            float limit,
            out MotionMatchingCandidateRejectDetail rejectDetail)
        {
            rejectDetail = new MotionMatchingCandidateRejectDetail(reason, value, limit);
            return false;
        }
    }
}
