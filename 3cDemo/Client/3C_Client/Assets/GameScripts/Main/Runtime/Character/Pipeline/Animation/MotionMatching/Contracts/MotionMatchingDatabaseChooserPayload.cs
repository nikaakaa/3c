using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingFactPredicatePayload
    {
        public MotionMatchingFactPredicatePayload(
            PresentationFactId factId,
            PresentationFactValueKind valueKind,
            CharacterMotionMatchingChooserPredicateOperator predicateOperator,
            bool boolValue,
            float floatValue,
            Vector2 vector2Value,
            int enumValue,
            ulong uint64Value,
            string identityValue)
        {
            if (!factId.IsValid ||
                CharacterPresentationFactSchema.RequireValueKind(factId) != valueKind ||
                !Enum.IsDefined(typeof(CharacterMotionMatchingChooserPredicateOperator), predicateOperator))
            {
                throw new ArgumentException("Motion Matching chooser predicate payload is invalid.");
            }
            if ((valueKind == PresentationFactValueKind.Bool ||
                 valueKind == PresentationFactValueKind.Vector2 ||
                 valueKind == PresentationFactValueKind.Identity) &&
                predicateOperator != CharacterMotionMatchingChooserPredicateOperator.Equals)
            {
                throw new ArgumentException("Motion Matching chooser predicate operator is invalid for the fact kind.");
            }
            if (valueKind == PresentationFactValueKind.Float && !float.IsFinite(floatValue) ||
                valueKind == PresentationFactValueKind.Vector2 &&
                (!float.IsFinite(vector2Value.x) || !float.IsFinite(vector2Value.y)) ||
                valueKind == PresentationFactValueKind.Identity && string.IsNullOrWhiteSpace(identityValue))
            {
                throw new ArgumentException("Motion Matching chooser predicate value is invalid.");
            }
            FactId = factId;
            ValueKind = valueKind;
            Operator = predicateOperator;
            BoolValue = boolValue;
            FloatValue = floatValue;
            Vector2Value = vector2Value;
            EnumValue = enumValue;
            UInt64Value = uint64Value;
            IdentityValue = identityValue ?? string.Empty;
        }

        public PresentationFactId FactId { get; }
        public PresentationFactValueKind ValueKind { get; }
        public CharacterMotionMatchingChooserPredicateOperator Operator { get; }
        public bool BoolValue { get; }
        public float FloatValue { get; }
        public Vector2 Vector2Value { get; }
        public int EnumValue { get; }
        public ulong UInt64Value { get; }
        public string IdentityValue { get; }

        internal bool Matches(in CharacterPresentationFactFrame frame)
        {
            if (!frame.TryRead(FactId, out CharacterPresentationFactValue value, out _) || value.Kind != ValueKind)
                return false;
            switch (ValueKind)
            {
                case PresentationFactValueKind.Bool:
                    return value.BoolValue == BoolValue;
                case PresentationFactValueKind.Float:
                    return Compare(value.FloatValue, FloatValue);
                case PresentationFactValueKind.Vector2:
                    return value.Vector2Value == Vector2Value;
                case PresentationFactValueKind.Enum:
                    return Compare(value.EnumValue, EnumValue);
                case PresentationFactValueKind.UInt64:
                    return Compare(value.UInt64Value, UInt64Value);
                case PresentationFactValueKind.Identity:
                    return string.Equals(value.IdentityValue, IdentityValue, StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        bool Compare(float left, float right) => Operator switch
        {
            CharacterMotionMatchingChooserPredicateOperator.Equals => left == right,
            CharacterMotionMatchingChooserPredicateOperator.GreaterThanOrEqual => left >= right,
            CharacterMotionMatchingChooserPredicateOperator.LessThanOrEqual => left <= right,
            _ => false
        };

        bool Compare(int left, int right) => Operator switch
        {
            CharacterMotionMatchingChooserPredicateOperator.Equals => left == right,
            CharacterMotionMatchingChooserPredicateOperator.GreaterThanOrEqual => left >= right,
            CharacterMotionMatchingChooserPredicateOperator.LessThanOrEqual => left <= right,
            _ => false
        };

        bool Compare(ulong left, ulong right) => Operator switch
        {
            CharacterMotionMatchingChooserPredicateOperator.Equals => left == right,
            CharacterMotionMatchingChooserPredicateOperator.GreaterThanOrEqual => left >= right,
            CharacterMotionMatchingChooserPredicateOperator.LessThanOrEqual => left <= right,
            _ => false
        };
    }

    public sealed class MotionMatchingDatabaseChooserRulePayload
    {
        readonly MotionMatchingFactPredicatePayload[] m_Predicates;
        readonly int[] m_DatabaseIndices;

        public MotionMatchingDatabaseChooserRulePayload(
            int priority,
            bool exclusive,
            MotionMatchingFactPredicatePayload[] predicates,
            int[] databaseIndices,
            bool shouldSearch,
            CharacterMotionMatchingChooserInterruptMode interruptMode,
            string searchPolicyOverrideId)
        {
            if (predicates == null || predicates.Length == 0 ||
                databaseIndices == null || databaseIndices.Length == 0 ||
                !Enum.IsDefined(typeof(CharacterMotionMatchingChooserInterruptMode), interruptMode))
            {
                throw new ArgumentException("Motion Matching chooser rule payload is incomplete.");
            }
            m_Predicates = (MotionMatchingFactPredicatePayload[])predicates.Clone();
            m_DatabaseIndices = (int[])databaseIndices.Clone();
            for (int i = 0; i < m_DatabaseIndices.Length; i++)
            {
                if (m_DatabaseIndices[i] < 0)
                    throw new ArgumentException("Motion Matching chooser rule Database index is invalid.");
                for (int previous = 0; previous < i; previous++)
                {
                    if (m_DatabaseIndices[previous] == m_DatabaseIndices[i])
                        throw new ArgumentException("Motion Matching chooser rule Database index is duplicated.");
                }
            }
            if (!string.IsNullOrEmpty(searchPolicyOverrideId))
                MotionMatchingAuthoringValidation.RequireIdentity(searchPolicyOverrideId, nameof(searchPolicyOverrideId));
            Priority = priority;
            Exclusive = exclusive;
            ShouldSearch = shouldSearch;
            InterruptMode = interruptMode;
            SearchPolicyOverrideId = searchPolicyOverrideId ?? string.Empty;
        }

        public int Priority { get; }
        public bool Exclusive { get; }
        public bool ShouldSearch { get; }
        public CharacterMotionMatchingChooserInterruptMode InterruptMode { get; }
        public string SearchPolicyOverrideId { get; }
        public int PredicateCount => m_Predicates.Length;
        public int DatabaseCount => m_DatabaseIndices.Length;
        public MotionMatchingFactPredicatePayload GetPredicate(int index) => m_Predicates[index];
        public int GetDatabaseIndex(int index) => m_DatabaseIndices[index];

        internal bool Matches(in CharacterPresentationFactFrame frame)
        {
            for (int i = 0; i < m_Predicates.Length; i++)
            {
                if (!m_Predicates[i].Matches(frame))
                    return false;
            }
            return true;
        }
    }

    public sealed class MotionMatchingDatabaseChooserPayload
    {
        readonly MotionMatchingDatabaseChooserRulePayload[] m_Rules;

        public MotionMatchingDatabaseChooserPayload(
            CharacterMotionMatchingDatabaseChooserId chooserId,
            int chooserRevision,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            MotionMatchingDatabaseChooserRulePayload[] rules)
        {
            if (!chooserId.IsValid || chooserRevision <= 0 || !searchDomainId.IsValid ||
                rules == null || rules.Length == 0)
            {
                throw new ArgumentException("Motion Matching chooser payload is incomplete.");
            }
            m_Rules = (MotionMatchingDatabaseChooserRulePayload[])rules.Clone();
            int previousPriority = int.MaxValue;
            for (int i = 0; i < m_Rules.Length; i++)
            {
                MotionMatchingDatabaseChooserRulePayload rule = m_Rules[i] ??
                    throw new ArgumentException($"Motion Matching chooser rule #{i} is missing.");
                if (rule.Priority > previousPriority)
                    throw new ArgumentException("Motion Matching chooser rules are not in non-increasing priority order.");
                previousPriority = rule.Priority;
            }
            ChooserId = chooserId;
            ChooserRevision = chooserRevision;
            SearchDomainId = searchDomainId;
        }

        public CharacterMotionMatchingDatabaseChooserId ChooserId { get; }
        public int ChooserRevision { get; }
        public CharacterMotionMatchingSearchDomainId SearchDomainId { get; }
        public int RuleCount => m_Rules.Length;
        public MotionMatchingDatabaseChooserRulePayload GetRule(int index) => m_Rules[index];

        public void RequireDatabaseRange(int firstDatabaseIndex, int databaseCount, int totalDatabaseCount)
        {
            if (firstDatabaseIndex < 0 || databaseCount <= 0 || firstDatabaseIndex + databaseCount > totalDatabaseCount)
                throw new InvalidOperationException("Motion Matching chooser Database range is invalid.");
            for (int ruleIndex = 0; ruleIndex < m_Rules.Length; ruleIndex++)
            {
                MotionMatchingDatabaseChooserRulePayload rule = m_Rules[ruleIndex];
                for (int databaseIndex = 0; databaseIndex < rule.DatabaseCount; databaseIndex++)
                {
                    int resolved = rule.GetDatabaseIndex(databaseIndex);
                    if (resolved < firstDatabaseIndex || resolved >= firstDatabaseIndex + databaseCount)
                        throw new InvalidOperationException($"Motion Matching chooser rule #{ruleIndex} references a Database outside its node binding.");
                }
            }
        }

        internal bool TryResolve(
            in CharacterPresentationFactFrame factFrame,
            int[] databaseIndexBuffer,
            out CharacterMotionMatchingDatabaseChooserResolution resolution)
        {
            resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                CharacterMotionMatchingDatabaseChooserResolutionCode.InvalidArgument,
                -1,
                0,
                0,
                false,
                default,
                string.Empty);
            if (databaseIndexBuffer == null || databaseIndexBuffer.Length == 0)
                return false;
            if (!factFrame.IsValid)
            {
                resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                    CharacterMotionMatchingDatabaseChooserResolutionCode.FrameInvalid,
                    -1,
                    0,
                    0,
                    false,
                    default,
                    string.Empty);
                return false;
            }

            int highestPriority = int.MinValue;
            int firstRuleIndex = -1;
            int matchedRuleCount = 0;
            int exclusiveRuleIndex = -1;
            int exclusiveCount = 0;
            for (int i = 0; i < m_Rules.Length; i++)
            {
                MotionMatchingDatabaseChooserRulePayload rule = m_Rules[i];
                if (!rule.Matches(factFrame))
                    continue;
                if (firstRuleIndex < 0 || rule.Priority > highestPriority)
                {
                    highestPriority = rule.Priority;
                    firstRuleIndex = i;
                    matchedRuleCount = 1;
                    exclusiveRuleIndex = rule.Exclusive ? i : -1;
                    exclusiveCount = rule.Exclusive ? 1 : 0;
                }
                else if (rule.Priority == highestPriority)
                {
                    matchedRuleCount++;
                    if (rule.Exclusive)
                    {
                        exclusiveRuleIndex = i;
                        exclusiveCount++;
                    }
                }
            }
            if (firstRuleIndex < 0)
            {
                resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                    CharacterMotionMatchingDatabaseChooserResolutionCode.NoRuleMatched,
                    -1,
                    0,
                    0,
                    false,
                    default,
                    string.Empty);
                return false;
            }
            if (exclusiveCount > 1)
            {
                resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                    CharacterMotionMatchingDatabaseChooserResolutionCode.RuleConflict,
                    firstRuleIndex,
                    matchedRuleCount,
                    0,
                    false,
                    default,
                    string.Empty);
                return false;
            }

            bool hasOutput = false;
            bool shouldSearch = false;
            CharacterMotionMatchingChooserInterruptMode interruptMode = default;
            string searchPolicyOverrideId = string.Empty;
            int databaseCount = 0;
            for (int i = 0; i < m_Rules.Length; i++)
            {
                MotionMatchingDatabaseChooserRulePayload rule = m_Rules[i];
                if (!rule.Matches(factFrame) || rule.Priority != highestPriority ||
                    exclusiveCount == 1 && i != exclusiveRuleIndex)
                {
                    continue;
                }
                if (!hasOutput)
                {
                    shouldSearch = rule.ShouldSearch;
                    interruptMode = rule.InterruptMode;
                    searchPolicyOverrideId = rule.SearchPolicyOverrideId;
                    hasOutput = true;
                }
                else if (shouldSearch != rule.ShouldSearch || interruptMode != rule.InterruptMode ||
                         !string.Equals(searchPolicyOverrideId, rule.SearchPolicyOverrideId, StringComparison.Ordinal))
                {
                    resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                        CharacterMotionMatchingDatabaseChooserResolutionCode.RuleConflict,
                        firstRuleIndex,
                        matchedRuleCount,
                        0,
                        false,
                        default,
                        string.Empty);
                    return false;
                }
                for (int databaseOffset = 0; databaseOffset < rule.DatabaseCount; databaseOffset++)
                {
                    int databaseIndex = rule.GetDatabaseIndex(databaseOffset);
                    if (databaseCount >= databaseIndexBuffer.Length)
                    {
                        resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                            CharacterMotionMatchingDatabaseChooserResolutionCode.CapacityExceeded,
                            firstRuleIndex,
                            matchedRuleCount,
                            0,
                            false,
                            default,
                            string.Empty);
                        return false;
                    }
                    for (int previous = 0; previous < databaseCount; previous++)
                    {
                        if (databaseIndexBuffer[previous] == databaseIndex)
                        {
                            resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                                CharacterMotionMatchingDatabaseChooserResolutionCode.DatabaseDuplicate,
                                firstRuleIndex,
                                matchedRuleCount,
                                0,
                                false,
                                default,
                                string.Empty);
                            return false;
                        }
                    }
                    databaseIndexBuffer[databaseCount++] = databaseIndex;
                }
            }
            resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                CharacterMotionMatchingDatabaseChooserResolutionCode.None,
                firstRuleIndex,
                matchedRuleCount,
                databaseCount,
                shouldSearch,
                interruptMode,
                searchPolicyOverrideId);
            return true;
        }
    }
}
