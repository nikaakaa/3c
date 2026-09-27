using System;
using System.Collections.Generic;
using BTSMTL.Authoring.Graph;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Authoring.Editor
{
    public sealed class GraphAuthoringStateMachineBinding
    {
        public GraphAuthoringStateMachineBinding(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringCapabilityCatalog capabilities,
            IGraphAuthoringDomainMutation mutation,
            IGraphAuthoringStateMachinePolicy policy)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
            Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            if (document.SemanticKind != policy.SemanticKind)
                throw new InvalidOperationException($"StateMachine semantic '{document.SemanticKind}' cannot use policy '{policy.SemanticKind}'.");
            GraphAuthoringStateMachineProjectionValidator.RequireValid(document);
            policy.ValidateDocument(document);
        }

        public IGraphAuthoringStateMachineProjection Document { get; }
        public GraphAuthoringCapabilityCatalog Capabilities { get; }
        public IGraphAuthoringDomainMutation Mutation { get; }
        public IGraphAuthoringStateMachinePolicy Policy { get; }
    }
}
