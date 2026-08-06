using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public enum CharacterMotionMatchingChooserPredicateOperator : byte
    {
        Equals = 1,
        GreaterThanOrEqual = 2,
        LessThanOrEqual = 3
    }

    public enum CharacterMotionMatchingChooserInterruptMode : byte
    {
        PreserveEntry = 1,
        AllowJump = 2,
        ResetEntry = 3
    }

    public enum CharacterMotionMatchingDatabaseChooserResolutionCode : byte
    {
        None = 0,
        InvalidArgument = 1,
        FrameInvalid = 2,
        NoRuleMatched = 3,
        RuleConflict = 4,
        DatabaseOutsideProfile = 5,
        DatabaseDuplicate = 6,
        RigMismatch = 7,
        SearchDomainMismatch = 8,
        SearchPolicyOutsideProfile = 9,
        CapacityExceeded = 10
    }

    [Serializable]
    public sealed class CharacterMotionMatchingFactPredicate
    {
        [SerializeField] string m_FactId = string.Empty;
        [SerializeField] PresentationFactValueKind m_ValueKind;
        [SerializeField] CharacterMotionMatchingChooserPredicateOperator m_Operator;
        [SerializeField] bool m_BoolValue;
        [SerializeField] float m_FloatValue;
        [SerializeField] Vector2 m_Vector2Value;
        [SerializeField] int m_EnumValue;
        [SerializeField] ulong m_UInt64Value;
        [SerializeField] string m_IdentityValue = string.Empty;

        public PresentationFactId FactId => string.IsNullOrWhiteSpace(m_FactId) ? default : new PresentationFactId(m_FactId);
        public PresentationFactValueKind ValueKind => m_ValueKind;
        public CharacterMotionMatchingChooserPredicateOperator Operator => m_Operator;
        public bool BoolValue => m_BoolValue;
        public float FloatValue => m_FloatValue;
        public Vector2 Vector2Value => m_Vector2Value;
        public int EnumValue => m_EnumValue;
        public ulong UInt64Value => m_UInt64Value;
        public string IdentityValue => m_IdentityValue ?? string.Empty;

        public void RequireValid()
        {
            PresentationFactId factId = FactId;
            PresentationFactValueKind expectedKind = CharacterPresentationFactSchema.RequireValueKind(factId);
            if (ValueKind != expectedKind || !Enum.IsDefined(typeof(CharacterMotionMatchingChooserPredicateOperator), Operator))
                throw new InvalidOperationException($"Motion Matching Chooser predicate '{factId}' has an invalid typed operator.");
            if ((ValueKind == PresentationFactValueKind.Bool || ValueKind == PresentationFactValueKind.Vector2 || ValueKind == PresentationFactValueKind.Identity) &&
                Operator != CharacterMotionMatchingChooserPredicateOperator.Equals)
                throw new InvalidOperationException($"Motion Matching Chooser predicate '{factId}' only supports equality.");
            if (ValueKind == PresentationFactValueKind.Float && !float.IsFinite(FloatValue) ||
                ValueKind == PresentationFactValueKind.Vector2 && (!float.IsFinite(Vector2Value.x) || !float.IsFinite(Vector2Value.y)))
                throw new InvalidOperationException($"Motion Matching Chooser predicate '{factId}' has a non-finite value.");
            if (ValueKind == PresentationFactValueKind.Enum &&
                (EnumValue < (int)CharacterPresentationMotionPhase.GroundedStationary ||
                 EnumValue > (int)CharacterPresentationMotionPhase.AirborneFalling))
                throw new InvalidOperationException($"Motion Matching Chooser predicate '{factId}' has an invalid enum value.");
            if (ValueKind == PresentationFactValueKind.Identity && string.IsNullOrWhiteSpace(IdentityValue))
                throw new InvalidOperationException($"Motion Matching Chooser predicate '{factId}' has no identity value.");
        }

        internal bool Matches(in CharacterPresentationFactFrame frame)
        {
            if (!frame.TryRead(FactId, out CharacterPresentationFactValue value, out _))
                return false;
            if (value.Kind != ValueKind)
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

        bool Compare(float left, float right)
        {
            switch (Operator)
            {
                case CharacterMotionMatchingChooserPredicateOperator.Equals:
                    return left == right;
                case CharacterMotionMatchingChooserPredicateOperator.GreaterThanOrEqual:
                    return left >= right;
                case CharacterMotionMatchingChooserPredicateOperator.LessThanOrEqual:
                    return left <= right;
                default:
                    return false;
            }
        }

        bool Compare(int left, int right)
        {
            switch (Operator)
            {
                case CharacterMotionMatchingChooserPredicateOperator.Equals:
                    return left == right;
                case CharacterMotionMatchingChooserPredicateOperator.GreaterThanOrEqual:
                    return left >= right;
                case CharacterMotionMatchingChooserPredicateOperator.LessThanOrEqual:
                    return left <= right;
                default:
                    return false;
            }
        }

        bool Compare(ulong left, ulong right)
        {
            switch (Operator)
            {
                case CharacterMotionMatchingChooserPredicateOperator.Equals:
                    return left == right;
                case CharacterMotionMatchingChooserPredicateOperator.GreaterThanOrEqual:
                    return left >= right;
                case CharacterMotionMatchingChooserPredicateOperator.LessThanOrEqual:
                    return left <= right;
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public sealed class CharacterMotionMatchingDatabaseChooserRule
    {
        [SerializeField] int m_Priority;
        [SerializeField] bool m_Exclusive;
        [SerializeField] CharacterMotionMatchingFactPredicate[] m_Predicates = Array.Empty<CharacterMotionMatchingFactPredicate>();
        [SerializeField] CharacterMotionMatchingDatabaseDefinition[] m_Databases = Array.Empty<CharacterMotionMatchingDatabaseDefinition>();
        [SerializeField] bool m_ShouldSearch = true;
        [SerializeField] CharacterMotionMatchingChooserInterruptMode m_InterruptMode = CharacterMotionMatchingChooserInterruptMode.AllowJump;
        [SerializeField] string m_SearchPolicyOverrideId = string.Empty;

        public int Priority => m_Priority;
        public bool Exclusive => m_Exclusive;
        public IReadOnlyList<CharacterMotionMatchingFactPredicate> Predicates => m_Predicates ?? Array.Empty<CharacterMotionMatchingFactPredicate>();
        public IReadOnlyList<CharacterMotionMatchingDatabaseDefinition> Databases => m_Databases ?? Array.Empty<CharacterMotionMatchingDatabaseDefinition>();
        public bool ShouldSearch => m_ShouldSearch;
        public CharacterMotionMatchingChooserInterruptMode InterruptMode => m_InterruptMode;
        public string SearchPolicyOverrideId => m_SearchPolicyOverrideId ?? string.Empty;

        public void RequireValid()
        {
            if (Predicates.Count == 0 || Databases.Count == 0 || !Enum.IsDefined(typeof(CharacterMotionMatchingChooserInterruptMode), InterruptMode))
                throw new InvalidOperationException("Motion Matching Chooser rule must define predicates, databases, and interrupt mode.");
            if (!string.IsNullOrEmpty(SearchPolicyOverrideId))
                MotionMatchingAuthoringValidation.RequireIdentity(SearchPolicyOverrideId, nameof(SearchPolicyOverrideId));
            var databaseIds = new HashSet<CharacterMotionMatchingDatabaseId>();
            for (int i = 0; i < Predicates.Count; i++)
            {
                CharacterMotionMatchingFactPredicate predicate = Predicates[i];
                if (predicate == null)
                    throw new InvalidOperationException($"Motion Matching Chooser rule predicate #{i} is missing.");
                predicate.RequireValid();
            }
            for (int i = 0; i < Databases.Count; i++)
            {
                CharacterMotionMatchingDatabaseDefinition database = Databases[i];
                if (!database || !databaseIds.Add(database.DatabaseId))
                    throw new InvalidOperationException($"Motion Matching Chooser rule database #{i} is missing or duplicated.");
            }
        }

        internal bool Matches(in CharacterPresentationFactFrame frame)
        {
            for (int i = 0; i < Predicates.Count; i++)
            {
                if (!Predicates[i].Matches(frame))
                    return false;
            }
            return true;
        }
    }

    public readonly struct CharacterMotionMatchingDatabaseChooserResolution
    {
        internal CharacterMotionMatchingDatabaseChooserResolution(
            CharacterMotionMatchingDatabaseChooserResolutionCode code,
            int firstRuleIndex,
            int matchedRuleCount,
            int databaseCount,
            bool shouldSearch,
            CharacterMotionMatchingChooserInterruptMode interruptMode,
            string searchPolicyOverrideId)
        {
            Code = code;
            FirstRuleIndex = firstRuleIndex;
            MatchedRuleCount = matchedRuleCount;
            DatabaseCount = databaseCount;
            ShouldSearch = shouldSearch;
            InterruptMode = interruptMode;
            SearchPolicyOverrideId = searchPolicyOverrideId ?? string.Empty;
        }

        public CharacterMotionMatchingDatabaseChooserResolutionCode Code { get; }
        public bool IsValid => Code == CharacterMotionMatchingDatabaseChooserResolutionCode.None;
        public int FirstRuleIndex { get; }
        public int MatchedRuleCount { get; }
        public int DatabaseCount { get; }
        public bool ShouldSearch { get; }
        public CharacterMotionMatchingChooserInterruptMode InterruptMode { get; }
        public string SearchPolicyOverrideId { get; }
    }

    [CreateAssetMenu(fileName = "CharacterMotionMatchingDatabaseChooser", menuName = "3C/Character/Motion Matching/Database Chooser")]
    public sealed class CharacterMotionMatchingDatabaseChooser : ScriptableObject
    {
        public const string SchemaVersion = "character-motion-matching-database-chooser/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_ChooserId = string.Empty;
        [SerializeField] int m_Revision;
        [SerializeField] string m_SearchDomainId = string.Empty;
        [SerializeField] CharacterMotionMatchingDatabaseChooserRule[] m_Rules = Array.Empty<CharacterMotionMatchingDatabaseChooserRule>();

        public string Schema => m_Schema ?? string.Empty;
        public CharacterMotionMatchingDatabaseChooserId ChooserId => string.IsNullOrWhiteSpace(m_ChooserId) ? default : new CharacterMotionMatchingDatabaseChooserId(m_ChooserId);
        public int Revision => m_Revision;
        public CharacterMotionMatchingSearchDomainId SearchDomainId => string.IsNullOrWhiteSpace(m_SearchDomainId) ? default : new CharacterMotionMatchingSearchDomainId(m_SearchDomainId);
        public IReadOnlyList<CharacterMotionMatchingDatabaseChooserRule> Rules => m_Rules ?? Array.Empty<CharacterMotionMatchingDatabaseChooserRule>();

        public void Configure(
            CharacterMotionMatchingDatabaseChooserId chooserId,
            int revision,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            CharacterMotionMatchingDatabaseChooserRule[] rules)
        {
            if (!chooserId.IsValid || !searchDomainId.IsValid || rules == null || rules.Length == 0)
                throw new ArgumentException("Motion Matching Database Chooser is incomplete.");
            m_Schema = SchemaVersion;
            m_ChooserId = chooserId.Value;
            m_Revision = revision;
            m_SearchDomainId = searchDomainId.Value;
            m_Rules = rules;
            RequireValid();
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || !ChooserId.IsValid || !SearchDomainId.IsValid)
                throw new InvalidOperationException($"Motion Matching Database Chooser '{name}' has an invalid schema or identity.");
            MotionMatchingAuthoringValidation.RequireRevision(Revision, nameof(Revision));
            if (Rules.Count == 0)
                throw new InvalidOperationException($"Motion Matching Database Chooser '{name}' has no rules.");
            int previousPriority = int.MaxValue;
            for (int i = 0; i < Rules.Count; i++)
            {
                CharacterMotionMatchingDatabaseChooserRule rule = Rules[i];
                if (rule == null || rule.Priority > previousPriority)
                    throw new InvalidOperationException($"Motion Matching Database Chooser '{name}' rules must be non-increasing by priority.");
                rule.RequireValid();
                previousPriority = rule.Priority;
            }
        }

        public void RequireValid(CharacterMotionMatchingProfile profile, CharacterAnimationRigDefinition presentationRig)
        {
            RequireValid();
            if (!profile || !presentationRig)
                throw new InvalidOperationException($"Motion Matching Database Chooser '{name}' requires a Profile and Presentation Rig.");
            profile.RequireRigClosure(presentationRig);
            for (int i = 0; i < Rules.Count; i++)
            {
                CharacterMotionMatchingDatabaseChooserRule rule = Rules[i];
                for (int databaseIndex = 0; databaseIndex < rule.Databases.Count; databaseIndex++)
                {
                    CharacterMotionMatchingDatabaseDefinition database = rule.Databases[databaseIndex];
                    if (!profile.ContainsDatabase(database))
                        throw new InvalidOperationException($"Motion Matching Database Chooser '{name}' rule #{i} references a Database outside its Profile.");
                    if (!database.SearchDomainId.Equals(SearchDomainId))
                        throw new InvalidOperationException($"Motion Matching Database Chooser '{name}' rule #{i} references a different Search Domain.");
                }
                if (!string.IsNullOrEmpty(rule.SearchPolicyOverrideId) &&
                    !string.Equals(rule.SearchPolicyOverrideId, profile.SearchPolicy.SearchPolicyId, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Motion Matching Database Chooser '{name}' rule #{i} references a Search Policy outside its Profile.");
            }
        }

        internal bool TryResolve(
            in CharacterPresentationFactFrame factFrame,
            CharacterMotionMatchingProfile profile,
            CharacterAnimationRigDefinition presentationRig,
            CharacterMotionMatchingDatabaseDefinition[] databaseBuffer,
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
            if (!profile || !presentationRig || databaseBuffer == null || databaseBuffer.Length == 0)
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
            for (int i = 0; i < Rules.Count; i++)
            {
                CharacterMotionMatchingDatabaseChooserRule rule = Rules[i];
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
            for (int i = 0; i < Rules.Count; i++)
            {
                CharacterMotionMatchingDatabaseChooserRule rule = Rules[i];
                if (!rule.Matches(factFrame) || rule.Priority != highestPriority || exclusiveCount == 1 && i != exclusiveRuleIndex)
                    continue;
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
                for (int databaseIndex = 0; databaseIndex < rule.Databases.Count; databaseIndex++)
                {
                    CharacterMotionMatchingDatabaseDefinition database = rule.Databases[databaseIndex];
                    if (databaseCount >= databaseBuffer.Length)
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
                    if (!profile.ContainsDatabase(database))
                    {
                        resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                            CharacterMotionMatchingDatabaseChooserResolutionCode.DatabaseOutsideProfile,
                            firstRuleIndex,
                            matchedRuleCount,
                            0,
                            false,
                            default,
                            string.Empty);
                        return false;
                    }
                    if (!database.SearchDomainId.Equals(SearchDomainId))
                    {
                        resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                            CharacterMotionMatchingDatabaseChooserResolutionCode.SearchDomainMismatch,
                            firstRuleIndex,
                            matchedRuleCount,
                            0,
                            false,
                            default,
                            string.Empty);
                        return false;
                    }
                    if (!string.Equals(database.TargetRig.RigId, presentationRig.RigId, StringComparison.Ordinal) ||
                        !string.Equals(database.TargetRig.Revision, presentationRig.Revision, StringComparison.Ordinal))
                    {
                        resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                            CharacterMotionMatchingDatabaseChooserResolutionCode.RigMismatch,
                            firstRuleIndex,
                            matchedRuleCount,
                            0,
                            false,
                            default,
                            string.Empty);
                        return false;
                    }
                    for (int existingIndex = 0; existingIndex < databaseCount; existingIndex++)
                    {
                        if (databaseBuffer[existingIndex].DatabaseId == database.DatabaseId)
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
                    databaseBuffer[databaseCount++] = database;
                }
            }
            if (databaseCount == 0)
            {
                resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                    CharacterMotionMatchingDatabaseChooserResolutionCode.NoRuleMatched,
                    firstRuleIndex,
                    matchedRuleCount,
                    0,
                    false,
                    default,
                    string.Empty);
                return false;
            }
            if (!string.IsNullOrEmpty(searchPolicyOverrideId) &&
                !string.Equals(searchPolicyOverrideId, profile.SearchPolicy.SearchPolicyId, StringComparison.Ordinal))
            {
                resolution = new CharacterMotionMatchingDatabaseChooserResolution(
                    CharacterMotionMatchingDatabaseChooserResolutionCode.SearchPolicyOutsideProfile,
                    firstRuleIndex,
                    matchedRuleCount,
                    0,
                    false,
                    default,
                    string.Empty);
                return false;
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
