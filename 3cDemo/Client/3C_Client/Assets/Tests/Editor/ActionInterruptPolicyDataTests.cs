using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonAction.Tests
{
    public sealed class ActionInterruptPolicyDataTests
    {
        static readonly ActionStateId Attack01 = new ActionStateId("Action.Attack01");
        static readonly ActionStateId Dodge = new ActionStateId("Action.Dodge");
        static readonly ActionStateId HitReact = new ActionStateId("Action.HitReact");

        [Test]
        public void EmptyPolicySetIsValidAndCompilesEmpty()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(null);

            ActionInterruptPolicyValidationResult validation = ActionInterruptPolicyValidator.Validate(set);
            var policies = ActionInterruptPolicySetCompiler.Compile(set);

            Assert.False(validation.HasErrors);
            Assert.AreEqual(0, policies.Count);
        }

        [Test]
        public void SingleDefinitionCompilesToRuntimePolicy()
        {
            ActionInterruptPolicyDefinition definition = Definition(
                "Action.Attack01",
                "Action.Dodge",
                30,
                ActionInterruptTimingRule.DuringElapsedTimeWindow,
                0.18f,
                0.42f,
                force: true);

            var policies = ActionInterruptPolicySetCompiler.Compile(new ActionInterruptPolicySet(new[] { definition }));

            Assert.AreEqual(1, policies.Count);
            Assert.AreEqual(Attack01, policies[0].FromState);
            Assert.AreEqual(Dodge, policies[0].TargetState);
            Assert.AreEqual(30, policies[0].MinPriority);
            Assert.AreEqual(ActionInterruptTimingRule.DuringElapsedTimeWindow, policies[0].TimingRule);
            Assert.AreEqual(0.18f, policies[0].WindowStart);
            Assert.AreEqual(0.42f, policies[0].WindowEnd);
            Assert.True(policies[0].Force);
            Assert.False(policies[0].RequiredFactId.IsValid);
        }

        [Test]
        public void DefinitionCompilesRequiredFactId()
        {
            ActionInterruptPolicyDefinition definition = Definition(
                "Action.Attack01",
                "Action.Dodge",
                30,
                requiredFactId: TimelineFactIds.CancelableToDodge.Value);

            var policies = ActionInterruptPolicySetCompiler.Compile(new ActionInterruptPolicySet(new[] { definition }));

            Assert.AreEqual(TimelineFactIds.CancelableToDodge, policies[0].RequiredFactId);
            Assert.True(policies[0].RequiresTimelineFact);
        }

        [Test]
        public void MultipleDefinitionsKeepStableOrder()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", "Action.Dodge", 30),
                Definition("Action.Attack01", "Action.HitReact", 80)
            });

            var policies = ActionInterruptPolicySetCompiler.Compile(set);

            Assert.AreEqual(Dodge, policies[0].TargetState);
            Assert.AreEqual(HitReact, policies[1].TargetState);
        }

        [Test]
        public void ValidatorReportsInvalidFromStateId()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("", "Action.Dodge", 30)
            });

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("from state"));
        }

        [Test]
        public void ValidatorReportsInvalidTargetStateId()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", " ", 30)
            });

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("target state"));
        }

        [Test]
        public void ValidatorReportsNegativeMinPriority()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", "Action.Dodge", -1)
            });

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("min priority"));
        }

        [Test]
        public void ValidatorReportsNegativeAfterElapsedTime()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", "Action.Dodge", 30, ActionInterruptTimingRule.AfterElapsedTime, -0.1f)
            });

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("elapsed time"));
        }

        [Test]
        public void ValidatorReportsInvalidDuringWindow()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", "Action.Dodge", 30, ActionInterruptTimingRule.DuringElapsedTimeWindow, 0.4f, 0.2f)
            });

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("window end"));
        }

        [Test]
        public void ValidatorWarnsAboutDuplicateDefinitions()
        {
            ActionInterruptPolicyDefinition definition = Definition("Action.Attack01", "Action.Dodge", 30);
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[] { definition, definition });

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set);

            Assert.False(result.HasErrors);
            Assert.That(result.Warnings, Is.Not.Empty);
        }

        [Test]
        public void ValidatorReportsMissingTimelineWindowReference()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", "Action.Dodge", 30, windowId: "attack-dodge-cancel")
            });
            StateTimelinePolicyDefinition[] timelinePolicies =
            {
                new StateTimelinePolicyDefinition(
                    "Action.Attack01",
                    0,
                    0,
                    new[]
                    {
                        new StateTimelineWindowDefinition(
                            "attack-exit",
                            StateTimelineWindowKind.Exit,
                            StateTimelineTimeDomain.Normalized,
                            0.8f,
                            1f,
                            factId: TimelineFactIds.NaturalExitReady.Value)
                    })
            };

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set, timelinePolicies);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("missing timeline window"));
        }

        [Test]
        public void ValidatorRejectsPolicyReferenceToNaturalExitWindow()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", "Action.Dodge", 30, windowId: "attack-exit")
            });
            StateTimelinePolicyDefinition[] timelinePolicies =
            {
                new StateTimelinePolicyDefinition(
                    "Action.Attack01",
                    0,
                    0,
                    new[]
                    {
                        new StateTimelineWindowDefinition(
                            "attack-exit",
                            StateTimelineWindowKind.Exit,
                            StateTimelineTimeDomain.Normalized,
                            0.8f,
                            1f,
                            factId: TimelineFactIds.NaturalExitReady.Value)
                    })
            };

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set, timelinePolicies);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("non-request timeline window"));
        }

        [Test]
        public void ValidatorAcceptsPolicyReferenceToRequestWindow()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", "Action.Dodge", 30, windowId: "attack-dodge-cancel")
            });
            StateTimelinePolicyDefinition[] timelinePolicies =
            {
                new StateTimelinePolicyDefinition(
                    "Action.Attack01",
                    0,
                    0,
                    new[]
                    {
                        new StateTimelineWindowDefinition(
                            "attack-dodge-cancel",
                            StateTimelineWindowKind.Cancel,
                            StateTimelineTimeDomain.Normalized,
                            0.2f,
                            0.6f,
                            requestType: ActionRequestType.Dodge,
                            factId: TimelineFactIds.CancelableToDodge.Value)
                    })
            };

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set, timelinePolicies);

            Assert.False(result.HasErrors);
        }

        [Test]
        public void ValidatorReportsMissingTimelineFactReference()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", "Action.Dodge", 30, requiredFactId: TimelineFactIds.CancelableToDodge.Value)
            });
            StateTimelinePolicyDefinition[] timelinePolicies =
            {
                new StateTimelinePolicyDefinition(
                    "Action.Attack01",
                    0,
                    0,
                    new[]
                    {
                        new StateTimelineWindowDefinition(
                            "attack-exit",
                            StateTimelineWindowKind.Exit,
                            StateTimelineTimeDomain.Normalized,
                            0.8f,
                            1f,
                            factId: TimelineFactIds.NaturalExitReady.Value)
                    })
            };

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set, timelinePolicies);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("missing timeline fact"));
        }

        [Test]
        public void ValidatorRejectsPolicyReferenceToNaturalExitFact()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", "Action.Dodge", 30, requiredFactId: TimelineFactIds.NaturalExitReady.Value)
            });
            StateTimelinePolicyDefinition[] timelinePolicies =
            {
                new StateTimelinePolicyDefinition(
                    "Action.Attack01",
                    0,
                    0,
                    new[]
                    {
                        new StateTimelineWindowDefinition(
                            "attack-exit",
                            StateTimelineWindowKind.Exit,
                            StateTimelineTimeDomain.Normalized,
                            0.8f,
                            1f,
                            factId: TimelineFactIds.NaturalExitReady.Value)
                    })
            };

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set, timelinePolicies);

            Assert.True(result.HasErrors);
            Assert.That(result.DescribeErrors(), Does.Contain("non-request timeline fact"));
        }

        [Test]
        public void ValidatorAcceptsPolicyReferenceToRequestFact()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", "Action.Dodge", 30, requiredFactId: TimelineFactIds.CancelableToDodge.Value)
            });
            StateTimelinePolicyDefinition[] timelinePolicies =
            {
                new StateTimelinePolicyDefinition(
                    "Action.Attack01",
                    0,
                    0,
                    new[]
                    {
                        new StateTimelineWindowDefinition(
                            "attack-dodge-cancel",
                            StateTimelineWindowKind.Cancel,
                            StateTimelineTimeDomain.Normalized,
                            0.2f,
                            0.6f,
                            requestType: ActionRequestType.Dodge,
                            factId: TimelineFactIds.CancelableToDodge.Value)
                    })
            };

            ActionInterruptPolicyValidationResult result = ActionInterruptPolicyValidator.Validate(set, timelinePolicies);

            Assert.False(result.HasErrors);
        }

        [Test]
        public void ScriptableObjectConvertsToPolicySet()
        {
            ActionInterruptPolicySetSO asset = ScriptableObject.CreateInstance<ActionInterruptPolicySetSO>();
            try
            {
                SetAssetPolicies(asset, new[]
                {
                    Definition("Action.Attack01", "Action.Dodge", 30)
                });

                ActionInterruptPolicySet set = asset.ToPolicySet();
                var policies = asset.CompilePolicies();
                ActionInterruptPolicyValidationResult result = asset.Validate();

                Assert.AreEqual(1, set.Count);
                Assert.AreEqual(1, policies.Count);
                Assert.False(result.HasErrors);
                Assert.AreEqual(Dodge, policies[0].TargetState);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void DefaultTurnBackPoliciesAllowMoveStartAndMoveLoopThroughTimelineWindow()
        {
            ActionInterruptPolicySetSO asset = AssetDatabase.LoadAssetAtPath<ActionInterruptPolicySetSO>(
                "Assets/Configs/3C/Action/DefaultDodgeInterruptPolicySet.asset");
            Assert.NotNull(asset);

            ActionInterruptPolicy[] turnBackPolicies = asset.CompilePolicies()
                .Where(policy => policy.TargetState.Value == CharacterStateIds.TurnBack.Value)
                .ToArray();

            CollectionAssert.AreEquivalent(
                new[] { CharacterStateIds.MoveStart.Value, CharacterStateIds.MoveLoop.Value },
                turnBackPolicies.Select(policy => policy.FromState.Value).ToArray());
            Assert.True(turnBackPolicies.All(policy => policy.MinPriority == 20));
            Assert.True(turnBackPolicies.All(policy => policy.WindowId == "turnback-enter"));
            Assert.True(turnBackPolicies.All(policy => policy.RequiredFactId == TimelineFactIds.TurnBackEnterOpen));
        }

        [Test]
        public void CompiledPoliciesCanBeConsumedByArbiter()
        {
            ActionInterruptPolicySet set = new ActionInterruptPolicySet(new[]
            {
                Definition("Action.Attack01", "Action.Dodge", 30, ActionInterruptTimingRule.DuringElapsedTimeWindow, 0.1f, 0.4f)
            });

            var policies = ActionInterruptPolicySetCompiler.Compile(set);
            ActionInterruptRequest request = new ActionInterruptRequest(
                requestId: 1,
                requestType: ActionRequestType.Dodge,
                targetState: Dodge,
                priority: 30,
                sourceOrder: 0,
                originTick: 0);
            ActionInterruptContext context = new ActionInterruptContext(Attack01, 0.2f, 0, 0);

            ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(context, new[] { request }, policies);

            Assert.True(decision.Accepted);
            Assert.AreEqual(Dodge, decision.TargetState);
        }

        [Test]
        public void PolicyDataDoesNotNeedUnitySceneObjects()
        {
            AssertNoUnityObjectFields(typeof(ActionInterruptPolicyDefinition));
            AssertNoUnityObjectFields(typeof(ActionInterruptPolicySet));
            AssertNoDeclaredUnityObjectFields(typeof(ActionInterruptPolicySetSO));
        }

        [Test]
        public void ActionPolicyDataDoesNotReferenceForbiddenRuntimeTypes()
        {
            string actionRoot = Path.Combine(Application.dataPath, "Scripts/Character/Action");
            string combined = string.Join("\n", Directory.GetFiles(actionRoot, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

            Assert.That(combined, Does.Not.Contain("Animancer"));
            Assert.That(combined, Does.Not.Contain("AnimationClip"));
            Assert.That(combined, Does.Not.Contain("Animator"));
            Assert.That(combined, Does.Not.Contain("CharacterController"));
            Assert.That(combined, Does.Not.Contain("Cinemachine"));
            Assert.That(combined, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(combined, Does.Not.Contain("BBBNexus"));
        }

        [Test]
        public void LocomotionRuntimeDoesNotDependOnPolicySet()
        {
            string movementRoot = Path.Combine(Application.dataPath, "Scripts/Character/Movement");
            string stateMachineRoot = Path.Combine(Application.dataPath, "Scripts/Character/StateMachine");
            string stateMachine = string.Join("\n", Directory.GetFiles(stateMachineRoot, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
            string controller = File.ReadAllText(Path.Combine(movementRoot, "Runtime/PlayerLocomotionController.cs"));
            string presenter = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs"));

            Assert.That(stateMachine, Does.Not.Contain("ActionInterruptPolicySet"));
            Assert.That(controller, Does.Not.Contain("ActionInterruptPolicySet"));
            Assert.That(presenter, Does.Not.Contain("ActionInterruptPolicySet"));
        }

        [Test]
        public void DodgeToDodgePolicyHasAfterElapsedTimeProtection()
        {
            ActionInterruptPolicySetSO asset = ScriptableObject.CreateInstance<ActionInterruptPolicySetSO>();
            try
            {
                ActionInterruptPolicyDefinition dodgeToDodge = Definition(
                    ActionStateIds.Dodge.Value,
                    ActionStateIds.Dodge.Value,
                    30,
                    ActionInterruptTimingRule.AfterElapsedTime,
                    0.35f);
                SetAssetPolicies(asset, new[]
                {
                    Definition(ActionStateIds.None.Value, ActionStateIds.Dodge.Value, 30, ActionInterruptTimingRule.AfterElapsedTime, 0.35f),
                    dodgeToDodge
                });

                var policies = asset.CompilePolicies();
                ActionInterruptRequest request = new ActionInterruptRequest(
                    requestId: 1,
                    requestType: ActionRequestType.Dodge,
                    targetState: Dodge,
                    priority: 30,
                    sourceOrder: 0,
                    originTick: 0);

                ActionInterruptContext tooEarly = new ActionInterruptContext(Dodge, 0.1f, 0, 0);
                ActionInterruptDecision early = ActionInterruptArbiter.Arbitrate(tooEarly, new[] { request }, policies);
                Assert.False(early.Accepted);
                Assert.AreEqual(ActionInterruptRejectReason.TimingNotSatisfied, early.RejectReason);

                ActionInterruptContext onTime = new ActionInterruptContext(Dodge, 0.35f, 0, 0);
                ActionInterruptDecision accepted = ActionInterruptArbiter.Arbitrate(onTime, new[] { request }, policies);
                Assert.True(accepted.Accepted);
                Assert.AreEqual(Dodge, accepted.TargetState);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        static ActionInterruptPolicyDefinition Definition(
            string from,
            string target,
            int minPriority,
            ActionInterruptTimingRule timingRule = ActionInterruptTimingRule.Always,
            float windowStart = 0f,
            float windowEnd = 0f,
            bool force = false,
            string windowId = "",
            string requiredFactId = "")
        {
            return new ActionInterruptPolicyDefinition(from, target, minPriority, timingRule, windowStart, windowEnd, force, windowId, requiredFactId);
        }

        static void SetAssetPolicies(ActionInterruptPolicySetSO asset, ActionInterruptPolicyDefinition[] policies)
        {
            FieldInfo field = typeof(ActionInterruptPolicySetSO).GetField("policies", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(asset, policies);
        }

        static void AssertNoUnityObjectFields(System.Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
                Assert.False(typeof(Object).IsAssignableFrom(fields[i].FieldType), $"{type.Name}.{fields[i].Name}");
        }

        static void AssertNoDeclaredUnityObjectFields(System.Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < fields.Length; i++)
                Assert.False(typeof(Object).IsAssignableFrom(fields[i].FieldType), $"{type.Name}.{fields[i].Name}");
        }
    }
}
