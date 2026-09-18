using System;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    internal enum CharacterLocomotionPresentationCompositionRole : byte
    {
        Local = 1,
        PredictionOwner = 2,
        ServerAuthorityRemote = 3,
        RollbackLocal = 4,
        RollbackRemote = 5,
        Replay = 6
    }

    internal static class CharacterLocomotionPresentationComposition
    {
        internal static PreparedCharacterLocomotionPresentationBinding PrepareLocal(
            SimulationSessionCompositionDefinition composition,
            CharacterBodyPresentationProfile bodyProfile) =>
            Prepare(
                RequireCompositionIdentity(composition),
                CharacterLocomotionPresentationCompositionRole.Local,
                CharacterLocomotionBodySource.CommittedStream,
                CharacterLocomotionPresentationSourceCapability.CommittedBodyStream,
                bodyProfile);

        internal static PreparedCharacterLocomotionPresentationBinding PreparePredictionOwner(
            SimulationSessionCompositionDefinition composition,
            CharacterBodyPresentationProfile bodyProfile) =>
            Prepare(
                RequireCompositionIdentity(composition),
                CharacterLocomotionPresentationCompositionRole.PredictionOwner,
                CharacterLocomotionBodySource.SelectedStream,
                CharacterLocomotionPresentationSourceCapability.SelectedBodyStream,
                bodyProfile);

        internal static PreparedCharacterLocomotionPresentationBinding PrepareServerAuthorityRemote(
            string compositionIdentity,
            CharacterBodyPresentationProfile bodyProfile) =>
            Prepare(
                compositionIdentity,
                CharacterLocomotionPresentationCompositionRole.ServerAuthorityRemote,
                CharacterLocomotionBodySource.SelectedStream,
                CharacterLocomotionPresentationSourceCapability.SelectedBodyStream,
                bodyProfile);

        internal static PreparedCharacterLocomotionPresentationBinding PrepareRollbackLocal(
            SimulationSessionCompositionDefinition composition,
            CharacterBodyPresentationProfile bodyProfile) =>
            Prepare(
                RequireCompositionIdentity(composition),
                CharacterLocomotionPresentationCompositionRole.RollbackLocal,
                CharacterLocomotionBodySource.CommittedStream,
                CharacterLocomotionPresentationSourceCapability.CommittedBodyStream,
                bodyProfile);

        internal static PreparedCharacterLocomotionPresentationBinding PrepareRollbackRemote(
            SimulationSessionCompositionDefinition composition,
            CharacterBodyPresentationProfile bodyProfile) =>
            Prepare(
                RequireCompositionIdentity(composition),
                CharacterLocomotionPresentationCompositionRole.RollbackRemote,
                CharacterLocomotionBodySource.SelectedStream,
                CharacterLocomotionPresentationSourceCapability.SelectedBodyStream,
                bodyProfile);

        internal static CharacterLocomotionPresentationPreparationResult PrepareReplay(
            string compositionIdentity,
            CharacterLocomotionBodySource bodySource,
            CharacterBodyPresentationProfile bodyProfile,
            in CommittedMovementPlaybackClock movementClock,
            in CommittedLocomotionPlanarMotionTimeline locomotionTimeline)
        {
            CharacterLocomotionPresentationPlan plan = CreatePlan(
                compositionIdentity,
                CharacterLocomotionPresentationCompositionRole.Replay,
                CharacterLocomotionClockMode.CommittedMovement,
                bodySource);
            CharacterLocomotionPresentationFactLineage lineage =
                CharacterLocomotionPresentationFactLineage.Create(in plan, in movementClock);
            CharacterLocomotionPresentationSourceCapability capabilities =
                BodyCapability(bodySource) |
                CharacterLocomotionPresentationSourceCapability.CommittedMovementFact;
            CharacterLocomotionPresentationPreparationResult result =
                CharacterLocomotionPresentationPreparation.Prepare(
                    new CharacterLocomotionPresentationPreparationRequest(
                        plan,
                        bodyProfile,
                        capabilities,
                        lineage));
            if (!result.Succeeded || !locomotionTimeline.IsValid ||
                !locomotionTimeline.Matches(movementClock))
            {
                return result.Succeeded
                    ? new CharacterLocomotionPresentationPreparationResult(
                        default,
                        LocomotionPresentationFailureCode.MovementSegmentMismatch,
                        "Replay locomotion movement timeline does not match its playback clock.")
                    : result;
            }
            return result;
        }

        static PreparedCharacterLocomotionPresentationBinding Prepare(
            string compositionIdentity,
            CharacterLocomotionPresentationCompositionRole role,
            CharacterLocomotionBodySource bodySource,
            CharacterLocomotionPresentationSourceCapability sourceCapabilities,
            CharacterBodyPresentationProfile bodyProfile)
        {
            CharacterLocomotionPresentationPlan plan = CreatePlan(
                compositionIdentity,
                role,
                CharacterLocomotionClockMode.FreeRun,
                bodySource);
            CharacterLocomotionPresentationPreparationResult result =
                CharacterLocomotionPresentationPreparation.Prepare(
                    new CharacterLocomotionPresentationPreparationRequest(
                        plan,
                        bodyProfile,
                        sourceCapabilities));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Locomotion presentation preparation failed: {result.FailureCode} {result.Message}");
            }
            return result.Binding;
        }

        static CharacterLocomotionPresentationPlan CreatePlan(
            string compositionIdentity,
            CharacterLocomotionPresentationCompositionRole role,
            CharacterLocomotionClockMode clockMode,
            CharacterLocomotionBodySource bodySource)
        {
            if (string.IsNullOrWhiteSpace(compositionIdentity))
                throw new ArgumentException("Session Composition identity is missing.", nameof(compositionIdentity));
            return new CharacterLocomotionPresentationPlan(
                string.Concat(
                    compositionIdentity.Trim(),
                    "/locomotion/",
                    role.ToString()),
                clockMode,
                bodySource);
        }

        static CharacterLocomotionPresentationSourceCapability BodyCapability(
            CharacterLocomotionBodySource bodySource) =>
            bodySource == CharacterLocomotionBodySource.CommittedStream
                ? CharacterLocomotionPresentationSourceCapability.CommittedBodyStream
                : CharacterLocomotionPresentationSourceCapability.SelectedBodyStream;

        static string RequireCompositionIdentity(
            SimulationSessionCompositionDefinition composition) =>
            composition ? composition.SessionId : throw new ArgumentNullException(nameof(composition));
    }
}
