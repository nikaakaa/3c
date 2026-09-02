using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class CharacterPoseCompilerModule
    {
        public static CharacterPoseCompilationResult Compile(
            CharacterPoseCompilationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var diagnostics = new List<CharacterPoseCompilationDiagnostic>();
            IReadOnlyList<string> capabilityErrors =
                CharacterPoseGraphCapabilityValidator.Validate(request.Asset);
            if (capabilityErrors.Count != 0)
            {
                for (int i = 0; i < capabilityErrors.Count; i++)
                {
                    diagnostics.Add(new CharacterPoseCompilationDiagnostic(
                        CharacterPoseCompilationPass.TypedLowering,
                        CharacterPoseCompilationDiagnosticSeverity.Error,
                        "capability-contract-invalid",
                        capabilityErrors[i],
                        request.Asset.Graph.GraphId));
                }
                return new CharacterPoseCompilationResult(null, diagnostics);
            }
            CharacterPoseGraphClosurePassResult closureResult =
                CharacterPoseGraphClosurePass.Run(request);
            if (!closureResult.IsSuccess)
            {
                diagnostics.AddRange(closureResult.Diagnostics);
                return new CharacterPoseCompilationResult(null, diagnostics);
            }
            CharacterPoseTypedLoweringPassResult typedLoweringResult =
                CharacterPoseTypedLoweringPass.Run(
                    request,
                    closureResult.Closure);
            if (!typedLoweringResult.IsSuccess)
            {
                diagnostics.AddRange(typedLoweringResult.Diagnostics);
                return new CharacterPoseCompilationResult(null, diagnostics);
            }
            CharacterPoseTopologyPassResult topologyResult =
                CharacterPoseTopologyPass.Run(
                    request,
                    closureResult.Closure,
                    typedLoweringResult.Catalog);
            if (!topologyResult.IsSuccess)
            {
                diagnostics.AddRange(topologyResult.Diagnostics);
                return new CharacterPoseCompilationResult(null, diagnostics);
            }
            CharacterPoseSymbolicFamilyLoweringPassResult symbolicResult =
                CharacterPoseSymbolicFamilyLoweringPass.Run(
                    request,
                    closureResult.Closure,
                    topologyResult.Catalog);
            if (!symbolicResult.IsSuccess)
            {
                diagnostics.AddRange(symbolicResult.Diagnostics);
                return new CharacterPoseCompilationResult(null, diagnostics);
            }
            try
            {
                CharacterPoseFamilyPayloadBinding binding =
                    CharacterPoseFamilyPayloadBindingPass.Run(
                        request,
                        closureResult.Closure,
                        topologyResult.Catalog,
                        symbolicResult.Program);
                CharacterPoseBoundFamilyPayloads payloads =
                    binding.Payloads;
                CharacterPoseBoundProgramLayout layout =
                    binding.Layout;
                CharacterPoseStageSchedule schedule =
                    CharacterPoseStageSchedulePass.Run(
                        symbolicResult.Program,
                        binding.Operations,
                        payloads.LinkedPoseFragments);
                CharacterPoseValueLifetime valueLifetime =
                    CharacterPoseValueLifetimePass.Run(
                        binding.Operations,
                        schedule,
                        layout.PoseValueCount,
                        payloads.Parameters.Length,
                        layout.FullBodyIkGoalContributionValueCount,
                        layout.FullBodyIkGoalSetValueCount,
                        payloads
                            .FullBodyIkGoalContributionInputValueIndices,
                        payloads.LinkedPoseCalls,
                        payloads.LinkedPoseFragments,
                        layout.OutputOperationIndex);
                CharacterMotionMatchingPosePlanCompilation motionMatching =
                    CharacterMotionMatchingPosePlanCompiler.Compile(
                        binding,
                        request.Asset,
                        request.Rig,
                        request.MotionMatching,
                        request.CurveIndices,
                        request.ProfileIndicesByIdentity);
                CharacterPoseWorkspacePlan workspace =
                    CharacterPoseWorkspacePlanPass.Run(
                        valueLifetime,
                        schedule,
                        request.Rig,
                        binding.Operations,
                        payloads.BlendNodes,
                        layout.PlayerCount,
                        layout.InertializationCount,
                        payloads.StateMachines,
                        layout.PoseSourceCount,
                        payloads.PoseBoneIkGoalSources,
                        payloads.FootPlacements,
                        payloads.FullBodyIks,
                        layout
                            .FullBodyIkGoalContributionGoalWorkspaceCount,
                        motionMatching.ContributionCapacity);
                CharacterPresentationInertializationDescriptor[]
                    inertializations =
                        CharacterPresentationInertializationPlanCompiler
                            .Compile(
                                binding,
                                request.Asset,
                                request.Rig,
                                request.CurveIndices,
                                request.ProfileIndicesByIdentity);
                CharacterPoseProgramImage image =
                    CharacterPoseProgramImageSealPass.Run(
                        request,
                        binding,
                        schedule,
                        workspace,
                        inertializations,
                        motionMatching);
                return new CharacterPoseCompilationResult(
                    image,
                    diagnostics);
            }
            catch (Exception exception)
            {
                diagnostics.Add(new CharacterPoseCompilationDiagnostic(
                    CharacterPoseCompilationPass.SealProgramImage,
                    CharacterPoseCompilationDiagnosticSeverity.Error,
                    "compiler-invariant-invalid",
                    exception.Message,
                    request.Asset.Graph.GraphId));
                return new CharacterPoseCompilationResult(null, diagnostics);
            }
        }
    }
}
