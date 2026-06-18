using System;

namespace ThirdPersonCharacterBehavior
{
    public readonly struct CharacterBehaviorRuntimeDefinition
    {
        readonly CharacterBehaviorSourceKind[] leafOrder;
        readonly string diagnostic;

        public CharacterBehaviorRuntimeDefinition(
            CharacterBehaviorSourceId rootId,
            CharacterBehaviorSourceKind[] leafOrder,
            string diagnostic = "")
        {
            RootId = rootId;
            this.leafOrder = leafOrder ?? Array.Empty<CharacterBehaviorSourceKind>();
            diagnostic = diagnostic ?? string.Empty;
            this.diagnostic = Validate(rootId, this.leafOrder, diagnostic);
        }

        public CharacterBehaviorSourceId RootId { get; }
        public bool IsValid => string.IsNullOrWhiteSpace(Diagnostic);
        public string Diagnostic => diagnostic ?? string.Empty;
        public int LeafCount => leafOrder != null ? leafOrder.Length : 0;

        public CharacterBehaviorSourceKind GetLeafAt(int index)
        {
            return leafOrder != null && index >= 0 && index < leafOrder.Length
                ? leafOrder[index]
                : CharacterBehaviorSourceKind.None;
        }

        public bool HasRequiredProductionOrder =>
            LeafCount == 2 &&
            GetLeafAt(0) == CharacterBehaviorSourceKind.Locomotion &&
            GetLeafAt(1) == CharacterBehaviorSourceKind.CommittedAction;

        public static CharacterBehaviorRuntimeDefinition Invalid(string diagnostic)
        {
            return new CharacterBehaviorRuntimeDefinition(default, Array.Empty<CharacterBehaviorSourceKind>(), diagnostic);
        }

        static string Validate(
            CharacterBehaviorSourceId rootId,
            CharacterBehaviorSourceKind[] leaves,
            string existingDiagnostic)
        {
            if (!string.IsNullOrWhiteSpace(existingDiagnostic))
                return existingDiagnostic;
            if (!rootId.IsValid)
                return "behavior-entry-root-missing";
            if (leaves == null || leaves.Length == 0)
                return "behavior-entry-leaves-missing";
            if (leaves.Length != 2)
                return "behavior-entry-leaf-count-invalid";
            if (leaves[0] != CharacterBehaviorSourceKind.Locomotion)
                return "behavior-entry-locomotion-leaf-order-invalid";
            if (leaves[1] != CharacterBehaviorSourceKind.CommittedAction)
                return "behavior-entry-committed-action-leaf-order-invalid";
            return string.Empty;
        }
    }
}
