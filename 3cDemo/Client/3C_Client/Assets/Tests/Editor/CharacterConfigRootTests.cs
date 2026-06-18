using System.Reflection;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using ThirdPersonAction;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using ThirdPersonDiagnostics;
using ThirdPersonInput;
using ThirdPersonMovement;
using ThirdPersonAnimation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public sealed class CharacterConfigRootTests
    {
        const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        static readonly string[] LegacyRuntimeConfigFields =
        {
            "runAnimationConfig",
            "config",
            "stateMachineDefinition",
            "interruptPolicySet",
            "dodgeActionConfig"
        };

        [Test]
        public void CharacterConfigEmptyReferenceReturnsNull()
        {
            CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();

            Assert.IsNull(config.StateMachine);
            Assert.IsNull(config.Movement);
            Assert.IsNull(config.LocomotionAnimation);
            Assert.IsNull(config.ActionInterruptPolicy);
            Assert.IsNull(config.BodyClaimPolicy);
            Assert.IsNull(config.ActionCatalog);
            Assert.IsNull(config.InputActions);
            Assert.IsNull(config.MoveAction);
            Assert.IsNull(config.RunAction);
            Assert.IsNull(config.LookAction);
            Assert.IsNull(config.DodgeInputAction);
            Assert.IsNull(config.CameraConfig);
        }

        [Test]
        public void CharacterConfigReportsMissingActionCatalog()
        {
            CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();

            CharacterActionCatalogValidationResult result = config.ValidateActionCatalog();

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("action catalog is missing"));
            Object.DestroyImmediate(config);
        }

        [Test]
        public void CorinRootConfigReferencesLocomotionStateGraph()
        {
            CharacterConfigSO config = AssetDatabase.LoadAssetAtPath<CharacterConfigSO>(
                "Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset");

            Assert.NotNull(config);
            Assert.NotNull(config.StateMachine);
            Assert.AreEqual(
                "Assets/Configs/3C/StateMachine/Locomotion/Corin/CorinLocomotionStateGraph.asset",
                AssetDatabase.GetAssetPath(config.StateMachine));

            CharacterStateMachineDefinition definition = config.StateMachine.ToDefinition();
            foreach (CharacterStateNodeDefinition node in definition.Nodes)
                Assert.False(node.StateId.Value.StartsWith("Action.", System.StringComparison.Ordinal), node.StateId.Value);
            foreach (CharacterStateTransitionDefinition transition in definition.Transitions)
            {
                Assert.False(transition.FromStateId.StartsWith("Action.", System.StringComparison.Ordinal), transition.FromStateId);
                Assert.False(transition.ToStateId.Value.StartsWith("Action.", System.StringComparison.Ordinal), transition.ToStateId.Value);
            }
        }

        [Test]
        public void CorinRootConfigReferencesValidActionCatalog()
        {
            CharacterConfigSO config = AssetDatabase.LoadAssetAtPath<CharacterConfigSO>(
                "Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset");

            Assert.NotNull(config);
            Assert.NotNull(config.ActionCatalog);
            Assert.False(config.ValidateActionCatalog().HasErrors);
            ActionTimelineCompileContext compileContext = CompileContext();
            Assert.True(config.ActionCatalog.ToCatalog(in compileContext).TryGetDodgeDefinition(out CharacterActionDefinition definition));
            Assert.AreEqual(ActionStateIds.Dodge, definition.ActionState);
        }

        [Test]
        public void ActionCatalogReportsDuplicateActionIdAndRequestBinding()
        {
            CharacterActionDefinitionSO first = CreateDodgeDefinitionAsset(30, 20);
            CharacterActionDefinitionSO second = CreateDodgeDefinitionAsset(35, 25);
            CharacterActionCatalogSO catalog = CreateCatalogAsset(first, second);

            ActionTimelineCompileContext compileContext = CompileContext();
            CharacterActionCatalogValidationResult result = catalog.Validate(in compileContext);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("duplicates action id 'Action.Dodge'"));
            Assert.That(result.DescribeErrors(), Does.Contain("duplicates request binding 'Dodge:Dodge'"));
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void ActionCatalogReportsMissingDodgeDefinition()
        {
            CharacterActionCatalogSO catalog = CreateCatalogAsset();

            ActionTimelineCompileContext compileContext = CompileContext();
            CharacterActionCatalogValidationResult result = catalog.Validate(in compileContext);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("missing Action.Dodge definition"));
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void CharacterFrameRuntimeControllerHasNoLegacySerializedConfigFields()
        {
            AssertNoPrivateFields(typeof(CharacterFrameRuntimeController), LegacyRuntimeConfigFields);
        }

        [Test]
        public void CharacterFrameRuntimeControllerAppliesRootInputActionsToInputSource()
        {
            GameObject go = new GameObject("config-input-actions-test");
            CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();
            InputActionAsset inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionReference moveReference = null;
            InputActionReference runReference = null;
            InputActionReference lookReference = null;

            try
            {
                InputActionMap map = inputActions.AddActionMap("Player");
                InputAction move = map.AddAction("Move", InputActionType.Value);
                InputAction run = map.AddAction("Run", InputActionType.Button);
                InputAction look = map.AddAction("Look", InputActionType.Value);
                moveReference = InputActionReference.Create(move);
                runReference = InputActionReference.Create(run);
                lookReference = InputActionReference.Create(look);
                SetPrivateField(config, "inputActions", inputActions);
                SetPrivateField(config, "moveAction", moveReference);
                SetPrivateField(config, "runAction", runReference);
                SetPrivateField(config, "lookAction", lookReference);

                CharacterFrameRuntimeController controller = go.AddComponent<CharacterFrameRuntimeController>();
                UnityInputSystemLocomotionInputSource inputSource = go.AddComponent<UnityInputSystemLocomotionInputSource>();
                inputSource.RunActionName = string.Empty;

                controller.InputSourceBehaviour = inputSource;
                controller.CharacterConfig = config;

                Assert.AreSame(inputActions, inputSource.InputActions);
                Assert.AreEqual("Player", inputSource.ActionMapName);
                Assert.AreEqual("Move", inputSource.MoveActionName);
                Assert.AreEqual("Run", inputSource.RunActionName);
                Assert.AreEqual("Look", inputSource.LookActionName);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(inputActions);
                Object.DestroyImmediate(moveReference);
                Object.DestroyImmediate(runReference);
                Object.DestroyImmediate(lookReference);
            }
        }

        [Test]
        public void RequestBufferAdapterAppliesRootDodgeInputAction()
        {
            GameObject go = new GameObject("config-dodge-input-actions-test");
            CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();
            InputActionAsset inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionReference dodgeReference = null;

            try
            {
                InputActionMap map = inputActions.AddActionMap("Player");
                InputAction dodge = map.AddAction("Dodge", InputActionType.Button);
                dodgeReference = InputActionReference.Create(dodge);
                SetPrivateField(config, "inputActions", inputActions);
                SetPrivateField(config, "dodgeInputAction", dodgeReference);

                UnityInputSystemRequestBufferAdapter adapter = go.AddComponent<UnityInputSystemRequestBufferAdapter>();
                adapter.InputActions = null;
                adapter.ActionMapName = string.Empty;
                adapter.DodgeActionName = string.Empty;

                adapter.ApplyFormalInputConfig(config);

                Assert.AreSame(inputActions, adapter.InputActions);
                Assert.AreEqual("Player", adapter.ActionMapName);
                Assert.AreEqual("Dodge", adapter.DodgeActionName);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(inputActions);
                Object.DestroyImmediate(dodgeReference);
            }
        }

        [Test]
        public void RequestBufferAdapterUsesTickDrivenButtonSamplingAuthority()
        {
            string sourcePath = Path.Combine(Application.dataPath, "Scripts/Input/Runtime/UnityInputSystemRequestBufferAdapter.cs");
            string source = File.ReadAllText(sourcePath).Replace("\r\n", "\n");

            Assert.That(source, Does.Contain("void Update()\n        {\n            if (!advanceStepOnUpdate)\n                return;\n\n            Tick();\n        }"));
            Assert.That(source, Does.Contain("dodge = ReadCurrentDodgeButtonFrame();"));
            Assert.That(source, Does.Contain("PredictionButtonFrame frame = ReadCurrentDodgeButtonFrame();"));
            Assert.That(source, Does.Contain("previousDodgeHeld = frame.Held;"));
        }

        static ActionInterruptPolicySetSO CreateDodgePolicyAsset(int minPriority)
        {
            ActionInterruptPolicySetSO asset = ScriptableObject.CreateInstance<ActionInterruptPolicySetSO>();
            SetPrivateField(asset, "policies", new[]
            {
                new ActionInterruptPolicyDefinition(ActionStateIds.None.Value, ActionStateIds.Dodge.Value, minPriority),
                new ActionInterruptPolicyDefinition(ActionStateIds.Dodge.Value, ActionStateIds.Dodge.Value, minPriority)
            });
            return asset;
        }

        static BodyClaimPolicySO CreateBodyClaimPolicyAsset()
        {
            BodyClaimPolicySO asset = ScriptableObject.CreateInstance<BodyClaimPolicySO>();
            SetPrivateField(asset, "definitions", new[]
            {
                new BodyClaimPolicyDefinition(
                    ActionStateIds.Dodge.Value,
                    BodyOccupancyKind.FullBody,
                    CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation)
            });
            return asset;
        }

        static CharacterActionCatalogSO CreateCatalogAsset(params CharacterActionDefinitionSO[] definitions)
        {
            CharacterActionCatalogSO asset = ScriptableObject.CreateInstance<CharacterActionCatalogSO>();
            SetPrivateField(asset, "definitions", definitions);
            return asset;
        }

        static CharacterActionDefinitionSO CreateDodgeDefinitionAsset(int priority, int resistance)
        {
            CharacterActionDefinitionSO asset = ScriptableObject.CreateInstance<CharacterActionDefinitionSO>();
            SetPrivateField(asset, "actionStateId", ActionStateIds.Dodge.Value);
            SetPrivateField(asset, "requestType", ActionRequestType.Dodge);
            SetPrivateField(asset, "sourceInputKind", InputRequestKind.Dodge);
            SetPrivateField(asset, "motionSourceStateId", CharacterStateIds.Dodge.Value);
            SetPrivateField(asset, "priority", priority);
            SetPrivateField(asset, "resistance", resistance);
            SetPrivateField(asset, "directionalDodge", new DodgeActionVariantAuthoring(
                DodgeActionVariant.Directional,
                0.42f,
                5.5f,
                true,
                ActionAnimationKeys.DodgeDirectional.Value));
            SetPrivateField(asset, "backstepDodge", new DodgeActionVariantAuthoring(
                DodgeActionVariant.Backstep,
                0.61f,
                2.75f,
                false,
                ActionAnimationKeys.DodgeBackstep.Value));
            SetPrivateField(asset, "committedActionBranch", CreateDodgeBranchAuthoring());
            return asset;
        }

        static CommittedActionBranchAuthoring CreateDodgeBranchAuthoring()
        {
            return new CommittedActionBranchAuthoring(
                1,
                true,
                "action.dodge",
                "selector.dodge",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                new[]
                {
                    CommittedActionBranchNodeAuthoring.Selector(
                        "selector.dodge",
                        new[] { "condition.directional", "condition.backstep" },
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.directional",
                        new CommittedActionBranchConditionAuthoring(
                            CommittedActionConditionKind.ActionVariantEquals,
                            CharacterStateVariant.Directional,
                            false),
                        new[] { "timeline.directional" },
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.ConditionNode(
                        "condition.backstep",
                        new CommittedActionBranchConditionAuthoring(
                            CommittedActionConditionKind.ActionVariantEquals,
                            CharacterStateVariant.Backstep,
                            false),
                        new[] { "timeline.backstep" },
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.directional",
                        CreateDodgeTimeline(
                            "timeline.directional",
                            CharacterStateVariant.Directional,
                            ActionAnimationKeys.DodgeDirectional.Value,
                            0.42f,
                            5.5f,
                            true,
                            true),
                        Vector2.zero),
                    CommittedActionBranchNodeAuthoring.TimelineNode(
                        "timeline.backstep",
                        CreateDodgeTimeline(
                            "timeline.backstep",
                            CharacterStateVariant.Backstep,
                            ActionAnimationKeys.DodgeBackstep.Value,
                            0.61f,
                            2.75f,
                            false,
                            false),
                        Vector2.zero)
                });
        }

        static CommittedActionBranchTimelineAuthoring CreateDodgeTimeline(
            string timelineNodeId,
            CharacterStateVariant variant,
            string animationKey,
            float duration,
            float distance,
            bool rotateToDirection,
            bool setRunLatch)
        {
            return new CommittedActionBranchTimelineAuthoring(
                true,
                "action.dodge",
                timelineNodeId,
                duration,
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation,
                new[]
                {
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Animation,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.AnimationKey,
                                0f,
                                duration,
                                ActionTimelineClipPayloadAuthoring.Animation(animationKey))
                        }),
                    new ActionTimelineTrackAuthoring(
                        ActionTimelineTrackKind.Motion,
                        new[]
                        {
                            new ActionTimelineClipAuthoring(
                                ActionTimelineClipKind.Motion,
                                0f,
                                duration,
                                ActionTimelineClipPayloadAuthoring.Motion(
                                    CharacterStateIds.Dodge.Value,
                                    variant,
                                    duration,
                                    distance,
                                    rotateToDirection,
                                    setRunLatch))
                        })
                });
        }

        static ActionTimelineCompileContext CompileContext()
        {
            return new ActionTimelineCompileContext(1f / 60f);
        }

        static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.NotNull(field, fieldName);
            field.SetValue(target, value);
        }

        static void AssertNoPrivateFields(System.Type type, string[] fieldNames)
        {
            foreach (string fieldName in fieldNames)
                Assert.IsNull(type.GetField(fieldName, PrivateInstance), $"{type.Name}.{fieldName}");
        }
    }
}
