using System;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterDomainRuntimeInstance
    {
        public Float32CharacterDomainRuntimeInstance(
            SimulationEvaluateRequest request,
            CharacterControlModuleCatalog controlModules)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            Workspace = new Float32EvaluationWorkspace(request.ExecutionLayout);
            Evaluator = new Float32OperationEvaluator(
                request.Program,
                request.ExecutionLayout,
                request.ActorId,
                Workspace,
                controlModules,
                request.ControlRuntimeBinding);
        }

        public Float32EvaluationWorkspace Workspace { get; }
        public Float32OperationEvaluator Evaluator { get; }
    }

    internal static class Float32CharacterDomainRuntimeFactory
    {
        public static Float32CharacterDomainRuntimeInstance Create(
            SimulationEvaluateRequest request,
            CharacterControlModuleCatalog controlModules) =>
            new Float32CharacterDomainRuntimeInstance(request, controlModules);
    }
}
