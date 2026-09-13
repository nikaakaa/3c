using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline
{
    public sealed partial class CharacterPipelineDefinition
    {
        public CharacterControlRuntimeBinding BuildControlRuntimeBinding(
            CharacterControlModuleCatalog controlModules)
        {
            if (controlModules == null)
                throw new ArgumentNullException(nameof(controlModules));
            if (string.IsNullOrEmpty(ControlModuleId))
                throw new InvalidOperationException("Character Pipeline Definition control module id is missing.");
            CharacterControlModuleId moduleId = new CharacterControlModuleId(ControlModuleId);
            ICharacterControlModule module = controlModules.Require(moduleId);
            var values = new List<CharacterControlParameterValue>();
            for (int i = 0; i < ControlParameters.Count; i++)
            {
                CharacterControlParameterConfiguration configuration = ControlParameters[i];
                if (configuration == null || string.IsNullOrWhiteSpace(configuration.ParameterId))
                    throw new InvalidOperationException($"Character Pipeline Definition control parameter #{i} is invalid.");
                values.Add(new CharacterControlParameterValue(
                    new CharacterControlParameterId(configuration.ParameterId.Trim()),
                    configuration.ValueKind,
                    configuration.NumericValue));
            }
            if (!module.Contract.TryResolveParameterSet(
                    values,
                    out CharacterControlParameterSet parameters,
                    out IReadOnlyList<string> errors))
                throw new InvalidOperationException(string.Join(" ", errors));
            CharacterControlMotionBindingCatalog motionBindings =
                CharacterControlMotionRuntimeBindingBuilder.Build(this, module.Contract);
            return new CharacterControlRuntimeBinding(
                moduleId,
                module.Contract.SemanticVersion,
                parameters,
                motionBindings);
        }
    }
}
