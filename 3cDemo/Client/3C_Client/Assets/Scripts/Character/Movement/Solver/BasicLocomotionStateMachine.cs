using UnityHFSM;

namespace ThirdPersonMovement
{
    public sealed class BasicLocomotionStateMachine
    {
        readonly StateMachine<BasicMovementPhase> fsm;
        readonly BasicMovementPhase initialState;
        BasicMovementSettings settings;
        bool hasMoveIntent;
        float phaseTime;
        float deltaTime;

        public BasicLocomotionStateMachine()
            : this((LocomotionStateGraphConfigSO)null)
        {
        }

        public BasicLocomotionStateMachine(LocomotionStateGraphConfigSO config)
            : this(config != null ? config.ToDefinition() : LocomotionStateGraphDefinition.CreateDefault(), config == null)
        {
        }

        public BasicLocomotionStateMachine(LocomotionStateGraphDefinition definition)
            : this(definition, false)
        {
        }

        BasicLocomotionStateMachine(LocomotionStateGraphDefinition definition, bool usesDefaultGraph)
        {
            UsesDefaultGraph = usesDefaultGraph;
            ValidationResult = LocomotionStateGraphValidator.Validate(definition);
            if (ValidationResult.HasErrors)
                throw new System.InvalidOperationException(ValidationResult.DescribeErrors());

            initialState = definition.InitialState;
            fsm = LocomotionStateGraphBuilder.Build(definition, CreateContext, ResetPhaseTime);
            fsm.Init();
        }

        public BasicMovementPhase Phase => fsm.ActiveStateName;
        public float PhaseTime => phaseTime;
        public string ActivePath => fsm.GetActiveHierarchyPath();
        public bool UsesDefaultGraph { get; }
        public LocomotionStateGraphValidationResult ValidationResult { get; }

        public void Reset()
        {
            fsm.RequestStateChange(initialState, true);
            phaseTime = 0f;
        }

        public BasicMovementPhase Tick(bool hasMoveIntent, float deltaTime, in BasicMovementSettings settings)
        {
            this.hasMoveIntent = hasMoveIntent;
            this.settings = settings;
            this.deltaTime = deltaTime < 0f ? 0f : deltaTime;
            phaseTime += this.deltaTime;
            fsm.OnLogic();
            return Phase;
        }

        LocomotionStateGraphContext CreateContext()
        {
            return new LocomotionStateGraphContext(hasMoveIntent, phaseTime, deltaTime, in settings);
        }

        void ResetPhaseTime()
        {
            phaseTime = 0f;
        }
    }
}
