 using ThirdPersonAction;
 using ThirdPersonAnimation;
 using ThirdPersonCharacterBehavior;
using ThirdPersonMovement;
using ThirdPersonCharacterStateMachine;
using ThirdPersonSimulation;
 using UnityEngine;
 using UnityEngine.InputSystem;

 namespace ThirdPersonCharacterConfig
 {
     [CreateAssetMenu(fileName = "CharacterConfig", menuName = "3C/Character/CharacterConfig")]
     public sealed class CharacterConfigSO : ScriptableObject
     {
         [SerializeField] CharacterStateMachineDefinitionSO stateMachine;
         [SerializeField] BasicMovementConfigSO movement;
         [SerializeField] RunLocomotionAnimationConfigSO locomotionAnimation;
         [SerializeField] ActionInterruptPolicySetSO actionInterruptPolicy;
         [SerializeField] BodyClaimPolicySO bodyClaimPolicy;
         [SerializeField] CharacterActionCatalogSO actionCatalog;
         [SerializeField] CharacterBehaviorRuntimeDefinitionSO behaviorRuntimeDefinition;
         [SerializeField] InputActionAsset inputActions;
         [SerializeField] InputActionReference moveAction;
         [SerializeField] InputActionReference runAction;
         [SerializeField] InputActionReference lookAction;
         [SerializeField] InputActionReference dodgeInputAction;
         [SerializeField] Object cameraConfig;

         public CharacterStateMachineDefinitionSO StateMachine => stateMachine;
         public BasicMovementConfigSO Movement => movement;
         public RunLocomotionAnimationConfigSO LocomotionAnimation => locomotionAnimation;
         public ActionInterruptPolicySetSO ActionInterruptPolicy => actionInterruptPolicy;
         public BodyClaimPolicySO BodyClaimPolicy => bodyClaimPolicy;
         public CharacterActionCatalogSO ActionCatalog => actionCatalog;
         public CharacterBehaviorRuntimeDefinitionSO BehaviorRuntimeDefinition => behaviorRuntimeDefinition;
         public InputActionAsset InputActions => inputActions;
         public InputActionReference MoveAction => moveAction;
         public InputActionReference RunAction => runAction;
         public InputActionReference LookAction => lookAction;
         public InputActionReference DodgeInputAction => dodgeInputAction;
         public Object CameraConfig => cameraConfig;

         public CharacterActionCatalogValidationResult ValidateActionCatalog()
         {
             if (actionCatalog != null)
             {
                 ActionTimelineCompileContext compileContext = ActionTimelineCompileContext.FromTickRate(SimulationTickRate.Default);
                 return actionCatalog.Validate(in compileContext);
             }

             CharacterActionCatalogValidationResult result = new CharacterActionCatalogValidationResult();
             result.AddError("Character config action catalog is missing.");
             return result;
         }
     }
 }
