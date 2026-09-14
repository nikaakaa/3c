using System;

namespace ThirdPersonSimulation
{
    public sealed class SimulationCharacterRuntimeDescriptor
    {
        public SimulationCharacterRuntimeDescriptor(
            SimulationExecutionTargetManifest executionTarget,
            GameplayContentHash gameplayContentHash,
            SimulationActorRosterDescriptor roster)
        {
            ExecutionTarget = executionTarget ?? throw new ArgumentNullException(nameof(executionTarget));
            if (!gameplayContentHash.IsValid)
                throw new ArgumentException("Character Runtime GameplayContentHash is invalid.", nameof(gameplayContentHash));
            Roster = roster ?? throw new ArgumentNullException(nameof(roster));
            GameplayContentHash = gameplayContentHash;
            Identity = StableHash.Compute(
                "simulation-character-runtime-descriptor/1",
                ExecutionTarget.Identity.ToString(),
                GameplayContentHash.ToString(),
                Roster.RosterHash.ToString());
        }

        public SimulationExecutionTargetManifest ExecutionTarget { get; }
        public SimulationNumericProfile NumericProfile => ExecutionTarget.NumericProfile;
        public NumericProfileId NumericProfileId => NumericProfile.Id;
        public TargetAbiVersion TargetAbiVersion => NumericProfile.AbiVersion;
        public OperationSetVersion OperationSetVersion => ExecutionTarget.OperationSetVersion;
        public GameplayContentHash GameplayContentHash { get; }
        public SimulationActorRosterDescriptor Roster { get; }
        public StableHash Identity { get; }
    }
}
