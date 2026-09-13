using System;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterDomainRuntimeInstance
    {
        public FixedCharacterDomainRuntimeInstance(
            SimulationEvaluateRequest request,
            CharacterControlModuleCatalog controlModules)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            Workspace = new FixedEvaluationWorkspace(request.ExecutionLayout);
            Evaluator = new FixedOperationEvaluator(
                request.Program,
                request.ExecutionLayout,
                request.ActorId,
                Workspace,
                controlModules);
        }

        public FixedEvaluationWorkspace Workspace { get; }
        public FixedOperationEvaluator Evaluator { get; }
    }

    internal static class FixedCharacterDomainRuntimeFactory
    {
        public static FixedCharacterDomainRuntimeInstance Create(
            SimulationEvaluateRequest request,
            CharacterControlModuleCatalog controlModules) =>
            new FixedCharacterDomainRuntimeInstance(request, controlModules);
    }
}
