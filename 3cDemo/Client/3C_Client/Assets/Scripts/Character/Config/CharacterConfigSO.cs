 using ThirdPersonAnimation;
 using ThirdPersonMovement;
 using ThirdPersonCharacterStateMachine;
 using UnityEngine;

 namespace ThirdPersonCharacterConfig
 {
     [CreateAssetMenu(fileName = "CharacterConfig", menuName = "3C/Character/CharacterConfig")]
     public sealed class CharacterConfigSO : ScriptableObject
     {
         [SerializeField] CharacterStateMachineDefinitionSO stateMachine;
         [SerializeField] BasicMovementConfigSO movement;
         [SerializeField] RunLocomotionAnimationConfigSO locomotionAnimation;

         public CharacterStateMachineDefinitionSO StateMachine => stateMachine;
         public BasicMovementConfigSO Movement => movement;
         public RunLocomotionAnimationConfigSO LocomotionAnimation => locomotionAnimation;
     }
 }
