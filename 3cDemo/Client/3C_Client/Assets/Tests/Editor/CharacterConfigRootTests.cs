 using NUnit.Framework;
 using System.Collections.Generic;
 using ThirdPersonCharacterConfig;
 using ThirdPersonCharacterStateMachine;
 using ThirdPersonDiagnostics;
 using ThirdPersonMovement;
 using ThirdPersonAnimation;
 using UnityEditor;
 using UnityEngine;
 using UnityEngine.TestTools;

 namespace Tests.Editor
 {
     public sealed class CharacterConfigRootTests
     {
         [Test]
         public void CharacterConfigEmptyReferenceReturnsNull()
         {
             CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();

             Assert.IsNull(config.StateMachine);
             Assert.IsNull(config.Movement);
             Assert.IsNull(config.LocomotionAnimation);
         }

         [Test]
         public void PlayerLocomotionControllerResolvesFromRootConfig()
         {
             GameObject go = new GameObject("config-root-test");
             try
             {
                 PlayerLocomotionController controller = go.AddComponent<PlayerLocomotionController>();
                 CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();

                 CharacterStateMachineDefinitionSO stateMachineDef = ScriptableObject.CreateInstance<CharacterStateMachineDefinitionSO>();
                 BasicMovementConfigSO movementConfig = ScriptableObject.CreateInstance<BasicMovementConfigSO>();
                 RunLocomotionAnimationConfigSO animConfig = ScriptableObject.CreateInstance<RunLocomotionAnimationConfigSO>();

                 var field = config.GetType().GetField("stateMachine",
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 field.SetValue(config, stateMachineDef);

                 field = config.GetType().GetField("movement",
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 field.SetValue(config, movementConfig);

                 field = config.GetType().GetField("locomotionAnimation",
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 field.SetValue(config, animConfig);

                 var controllerField = controller.GetType().GetField("characterConfig",
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 controllerField.SetValue(controller, config);

                 Assert.IsNotNull(controller.StateMachineDefinition);
                 Assert.IsNotNull(controller.Config);
                 Assert.IsNotNull(controller.RunAnimationConfig);
             }
             finally
             {
                 Object.DestroyImmediate(go);
             }
         }

         [Test]
         public void OldFieldsDoNotFallbackWhenRootConfigIsNull()
         {
             GameObject go = new GameObject("config-fallback-test");
             try
             {
                 PlayerLocomotionController controller = go.AddComponent<PlayerLocomotionController>();

                 CharacterStateMachineDefinitionSO stateMachineDef = ScriptableObject.CreateInstance<CharacterStateMachineDefinitionSO>();
                 BasicMovementConfigSO movementConfig = ScriptableObject.CreateInstance<BasicMovementConfigSO>();
                 RunLocomotionAnimationConfigSO animConfig = ScriptableObject.CreateInstance<RunLocomotionAnimationConfigSO>();

                 var field = controller.GetType().GetField("stateMachineDefinition",
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 field.SetValue(controller, stateMachineDef);

                 field = controller.GetType().GetField("config",
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 field.SetValue(controller, movementConfig);

                 field = controller.GetType().GetField("runAnimationConfig",
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 field.SetValue(controller, animConfig);

                 Assert.IsNull(controller.StateMachineDefinition);
                 Assert.IsNull(controller.Config);
                 Assert.IsNull(controller.RunAnimationConfig);
             }
             finally
             {
                 Object.DestroyImmediate(go);
             }
         }

         [Test]
         public void MissingFormalMovementConfigReportsDiagnostic()
         {
             GameObject go = new GameObject("config-missing-diagnostic-test");
             try
             {
                 PlayerLocomotionController controller = go.AddComponent<PlayerLocomotionController>();
                 CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();
                 CharacterStateMachineDefinitionSO stateMachineDef = AssetDatabase.LoadAssetAtPath<CharacterStateMachineDefinitionSO>(
                     "Assets/Configs/3C/Statemachine/DefaultCharacterStateMachine.asset");
                 Assert.NotNull(stateMachineDef);

                 var field = config.GetType().GetField("stateMachine",
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 field.SetValue(config, stateMachineDef);
                 controller.CharacterConfig = config;

                 List<RuntimeDiagnosticLogEvent> events = new List<RuntimeDiagnosticLogEvent>();
                 using (RuntimeDiagnosticLog.Capture(events.Add))
                 {
                     LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*movement-config-missing.*"));
                     Assert.False(controller.TryPrepareDecisionFrame(
                         new BasicLocomotionInputSnapshot(0.1f, Vector2.up, Vector2.zero),
                         new CharacterStateMachineRunner(stateMachineDef.ToDefinition()),
                         12,
                         out _));
                 }

                 Assert.That(events.Exists(item => item.Message == "movement-config-missing"), Is.True);
             }
             finally
             {
                 Object.DestroyImmediate(go);
             }
         }
     }
 }
