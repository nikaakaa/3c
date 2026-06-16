 using ThirdPersonAction;
 using ThirdPersonAnimation;
 using ThirdPersonMovement;
 using ThirdPersonCharacterStateMachine;
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
         [SerializeField] ActionInterruptPolicySetSO fullBodyStateRequestPolicy;
         [SerializeField] DodgeActionConfigSO dodgeAction;
         [SerializeField] Object fullBodyActionAnimation;
         [SerializeField] Object animancerRigVariant;
         [SerializeField] InputActionAsset inputActions;
         [SerializeField] InputActionReference moveAction;
         [SerializeField] InputActionReference runAction;
         [SerializeField] InputActionReference lookAction;
         [SerializeField] Object cameraConfig;

         public CharacterStateMachineDefinitionSO StateMachine => stateMachine;
         public BasicMovementConfigSO Movement => movement;
         public RunLocomotionAnimationConfigSO LocomotionAnimation => locomotionAnimation;
         public ActionInterruptPolicySetSO FullBodyStateRequestPolicy => fullBodyStateRequestPolicy;
         public DodgeActionConfigSO DodgeAction => dodgeAction;
         public Object FullBodyActionAnimation => fullBodyActionAnimation;
         public Object AnimancerRigVariant => animancerRigVariant;
         public InputActionAsset InputActions => inputActions;
         public InputActionReference MoveAction => moveAction;
         public InputActionReference RunAction => runAction;
         public InputActionReference LookAction => lookAction;
         public Object CameraConfig => cameraConfig;
     }
 }
