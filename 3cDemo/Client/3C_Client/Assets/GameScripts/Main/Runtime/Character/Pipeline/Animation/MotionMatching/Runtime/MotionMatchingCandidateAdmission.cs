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

        public bool Admit(MotionMatchingQuery query, int sampleIndex, out MotionMatchingCandidateRejectReason rejectReason)
        {
            if ((uint)sampleIndex >= (uint)m_Database.SampleCount)
                throw new ArgumentOutOfRangeException(nameof(sampleIndex));
            MotionMatchingSamplePayload candidate = m_Database.GetSample(sampleIndex);
            if (!query.DatabaseIdentity.EqualsExact(m_Database.ArtifactIdentity))
                return Reject(MotionMatchingCandidateRejectReason.IdentityMismatch, out rejectReason);
            if (!candidate.SearchDomainId.Equals(query.SearchDomainId) || !query.SearchDomainId.Equals(m_Database.SearchDomainId))
                return Reject(MotionMatchingCandidateRejectReason.SearchDomainMismatch, out rejectReason);
            bool continuation = !query.Initialization && query.CurrentSampleIndex >= 0 &&
                m_Database.GetSample(query.CurrentSampleIndex).NextSampleIndex == sampleIndex;
            if (query.Initialization && !candidate.CanInitialize)
                return Reject(MotionMatchingCandidateRejectReason.InitializationNotAllowed, out rejectReason);
            if (!query.Initialization && !continuation && !candidate.CanJumpInto)
                return Reject(MotionMatchingCandidateRejectReason.JumpNotAllowed, out rejectReason);
            if (candidate.EntryExcluded)
                return Reject(MotionMatchingCandidateRejectReason.EntryExcluded, out rejectReason);
            if (candidate.ExitExcluded)
                return Reject(MotionMatchingCandidateRejectReason.ExitExcluded, out rejectReason);
            if (!query.Initialization && !continuation && query.SecondsSinceLastJump < m_Database.SearchPolicy.MinimumJumpInterval)
                return Reject(MotionMatchingCandidateRejectReason.MinimumJumpInterval, out rejectReason);
            if (!CanCoverPlanHorizon(sampleIndex, out bool brokenContinuation))
                return Reject(brokenContinuation ? MotionMatchingCandidateRejectReason.BrokenContinuation : MotionMatchingCandidateRejectReason.InsufficientPlanHorizon, out rejectReason);
            if (!ContactMaskCompatible(query.ContactProtection.ProtectedMask, candidate.ContactMask, MotionMatchingFootContactMask.Left))
                return Reject(MotionMatchingCandidateRejectReason.LeftContactMismatch, out rejectReason);
            if (!ContactMaskCompatible(query.ContactProtection.ProtectedMask, candidate.ContactMask, MotionMatchingFootContactMask.Right))
                return Reject(MotionMatchingCandidateRejectReason.RightContactMismatch, out rejectReason);
            float positionLimit = m_Database.SearchPolicy.ProtectedFootPositionJumpLimit;
            float velocityLimit = m_Database.SearchPolicy.ProtectedFootVelocityJumpLimit;
            if ((query.ContactProtection.ProtectedMask & MotionMatchingFootContactMask.Left) != 0)
            {
                if ((candidate.LeftFootRootPosition - query.ContactProtection.LeftRootPosition).magnitude > positionLimit)
                    return Reject(MotionMatchingCandidateRejectReason.LeftContactPositionJump, out rejectReason);
                if ((candidate.LeftFoot.SoleLocalVelocity - query.ContactProtection.LeftRootVelocity).magnitude > velocityLimit)
                    return Reject(MotionMatchingCandidateRejectReason.LeftContactVelocityJump, out rejectReason);
            }
            if ((query.ContactProtection.ProtectedMask & MotionMatchingFootContactMask.Right) != 0)
            {
                if ((candidate.RightFootRootPosition - query.ContactProtection.RightRootPosition).magnitude > positionLimit)
                    return Reject(MotionMatchingCandidateRejectReason.RightContactPositionJump, out rejectReason);
                if ((candidate.RightFoot.SoleLocalVelocity - query.ContactProtection.RightRootVelocity).magnitude > velocityLimit)
                    return Reject(MotionMatchingCandidateRejectReason.RightContactVelocityJump, out rejectReason);
            }
            try
            {
                MotionMatchingClipBindingPayload clip = m_Database.GetClipBinding(candidate.ClipBindingIndex);
                if (clip == null || !clip.RootLocked || !clip.Clip)
                    return Reject(MotionMatchingCandidateRejectReason.MissingClipBinding, out rejectReason);
            }
            catch (ArgumentOutOfRangeException)
            {
                return Reject(MotionMatchingCandidateRejectReason.MissingClipBinding, out rejectReason);
            }
            rejectReason = MotionMatchingCandidateRejectReason.None;
            return true;
        }

        bool CanCoverPlanHorizon(int entrySampleIndex, out bool brokenContinuation)
        {
            int sampleIndex = entrySampleIndex;
            brokenContinuation = false;
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
            }
            return true;
        }

        static bool ContactMaskCompatible(MotionMatchingFootContactMask protectedMask, MotionMatchingFootContactMask candidateMask, MotionMatchingFootContactMask foot) =>
            (protectedMask & foot) == 0 || (candidateMask & foot) != 0;

        static bool Reject(MotionMatchingCandidateRejectReason reason, out MotionMatchingCandidateRejectReason rejectReason)
        {
            rejectReason = reason;
            return false;
        }
    }
}
