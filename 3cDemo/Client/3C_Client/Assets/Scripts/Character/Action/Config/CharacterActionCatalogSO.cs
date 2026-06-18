using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonAction
{
    [CreateAssetMenu(fileName = "CharacterActionCatalog", menuName = "3C/Action/CharacterActionCatalog")]
    public sealed class CharacterActionCatalogSO : ScriptableObject
    {
        [SerializeField] CharacterActionDefinitionSO[] definitions;

        public IReadOnlyList<CharacterActionDefinitionSO> Definitions => definitions ?? Array.Empty<CharacterActionDefinitionSO>();

        public CharacterActionCatalog ToCatalog(in ActionTimelineCompileContext compileContext)
        {
            IReadOnlyList<CharacterActionDefinitionSO> source = Definitions;
            CharacterActionDefinition[] runtimeDefinitions = new CharacterActionDefinition[source.Count];
            for (int i = 0; i < source.Count; i++)
                runtimeDefinitions[i] = source[i] != null ? source[i].ToDefinition(in compileContext) : default;

            return new CharacterActionCatalog(runtimeDefinitions);
        }

        public CharacterActionCatalogValidationResult Validate(in ActionTimelineCompileContext compileContext)
        {
            CharacterActionCatalogValidationResult result = new CharacterActionCatalogValidationResult();
            IReadOnlyList<CharacterActionDefinitionSO> source = Definitions;
            HashSet<string> actionIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<CharacterActionRequestBinding> bindings = new HashSet<CharacterActionRequestBinding>();

            for (int i = 0; i < source.Count; i++)
            {
                CharacterActionDefinitionSO asset = source[i];
                if (asset == null)
                {
                    result.AddError($"Catalog entry {i} is missing.");
                    continue;
                }

                CharacterActionDefinition definition = asset.ToDefinition(in compileContext);
                asset.ValidateInto(result, asset.name, in compileContext);
                if (definition.ActionState.IsValid && !actionIds.Add(definition.ActionState.Value))
                    result.AddError($"Catalog duplicates action id '{definition.ActionState.Value}'.");
                if (definition.RequestBinding.IsValid && !bindings.Add(definition.RequestBinding))
                    result.AddError($"Catalog duplicates request binding '{definition.RequestBinding}'.");
            }

            CharacterActionCatalog catalog = ToCatalog(in compileContext);
            if (!catalog.TryGetDodgeDefinition(out _))
                result.AddError("Catalog is missing Action.Dodge definition.");

            return result;
        }
    }
}
