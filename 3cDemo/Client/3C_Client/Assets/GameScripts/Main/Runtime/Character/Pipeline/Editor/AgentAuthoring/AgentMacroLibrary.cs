using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentMacroLibrary
    {
        const string Version = "v5";

        public bool TryExpand(AgentControllerIntent intent, AgentGraphSnapshot snapshot, out AgentPatchIR patch, AgentCompileReport report)
        {
            patch = new AgentPatchIR();
            if (intent == null)
            {
                report?.Error("intent", "missing_intent", "AgentControllerIntent 缺失。");
                return false;
            }

            patch.sourceMacro = intent.macro;
            patch.sourceMacroVersion = Version;

            switch (intent.macro)
            {
                case "single_timeline_action":
                    ExpandSingleTimelineAction(intent, patch);
                    break;
                case "two_hit_combo":
                    ExpandTwoHitCombo(intent, patch);
                    break;
                case "dodge_cancel":
                    ExpandDodgeCancel(intent, patch);
                    break;
                case "hit_reaction":
                    ExpandHitReaction(intent, patch);
                    break;
                case "locomotion_state_machine":
                case "locomotion":
                    ExpandLocomotion(intent, patch);
                    break;
                default:
                    report?.Error("intent.macro", "unknown_macro", $"未知 macro：{intent.macro}");
                    return false;
            }

            return new AgentPatchIdentityBinder().TryBind(patch, snapshot, report);
        }

        void ExpandSingleTimelineAction(AgentControllerIntent intent, AgentPatchIR patch)
        {
            string stateMachine = StateMachineName(intent, "Action StateMachine");
            AddStateMachine(patch, stateMachine);
            AddState(patch, stateMachine, "None", new Vector2(0f, 0f));

            AgentControllerIntentStep step = FirstStep(intent);
            if (step == null)
                return;

            AddActionStateBody(patch, stateMachine, step, intent, 0);
            AddRequestTransition(patch, stateMachine, "None", step.state, Request(step, intent), 10, "attack-start");
            AddCompletionTransition(patch, stateMachine, step.state, "None", 0, "attack-exit");
        }

        void ExpandTwoHitCombo(AgentControllerIntent intent, AgentPatchIR patch)
        {
            string outerStateMachine = StateMachineName(intent, "Action StateMachine");
            string categoryState = string.IsNullOrEmpty(intent.categoryState) ? "Attack" : intent.categoryState;
            string nestedStateMachine = string.IsNullOrEmpty(intent.nestedStateMachine)
                ? $"{categoryState} Combo StateMachine"
                : intent.nestedStateMachine;
            AddStateMachine(patch, outerStateMachine);
            AddState(patch, outerStateMachine, "None", new Vector2(0f, 0f));
            AddState(patch, outerStateMachine, categoryState, new Vector2(280f, 0f));
            AddNestedStateMachine(patch, categoryState, nestedStateMachine);

            for (int i = 0; i < intent.steps.Count; i++)
            {
                AgentControllerIntentStep step = intent.steps[i];
                AddActionStateBody(patch, nestedStateMachine, step, intent, i, true);
            }

            if (intent.steps.Count > 0)
            {
                AddRequestTransition(patch, outerStateMachine, "None", categoryState, Request(intent.steps[0], intent), 10, "combo-category-enter");
                AddTransition(patch, nestedStateMachine, "Enter", intent.steps[0].state, 0, "combo-enter");
            }

            for (int i = 0; i < intent.steps.Count - 1; i++)
                AddComboTransition(patch, nestedStateMachine, intent.steps[i], intent.steps[i + 1], intent, 20 + i, $"combo-next-{i + 1}");

            if (intent.steps.Count == 2)
                AddComboTransition(patch, nestedStateMachine, intent.steps[1], intent.steps[0], intent, 30, "combo-loopback");

            for (int i = 0; i < intent.steps.Count; i++)
                AddCompletionTransition(patch, nestedStateMachine, intent.steps[i].state, "Exit", 0, $"combo-leaf-exit-{i + 1}");

            AddCompletionTransition(patch, outerStateMachine, categoryState, "None", 0, "combo-category-exit");
        }

        void AddNestedStateMachine(AgentPatchIR patch, string categoryState, string nestedStateMachine)
        {
            patch.operations.Add(new AgentPatchOperation
            {
                id = $"{nestedStateMachine}.ensure",
                op = "ensure_state_machine",
                graph = $"{categoryState} State Body",
                stateMachine = nestedStateMachine,
                displayName = nestedStateMachine,
                lifecycleSlot = "Root",
                position = Vector2.zero
            });
        }

        void AddComboTransition(
            AgentPatchIR patch,
            string stateMachine,
            AgentControllerIntentStep from,
            AgentControllerIntentStep to,
            AgentControllerIntent intent,
            int priority,
            string id)
        {
            AgentControllerIntentCancel cancel = intent.cancel?.Find(i =>
                string.Equals(i.from, from.state, StringComparison.Ordinal) &&
                string.Equals(i.to, to.state, StringComparison.Ordinal));
            string blackboardKey = !string.IsNullOrEmpty(cancel?.blackboardKey)
                ? cancel.blackboardKey
                : $"{from.state}Cancel";
            patch.operations.Add(new AgentPatchOperation
            {
                id = id,
                op = "ensure_condition_rule",
                stateMachine = stateMachine,
                from = from.state,
                to = to.state,
                transitionPriority = priority,
                conditionGroups = new List<AgentConditionGroup>
                {
                    new AgentConditionGroup
                    {
                        terms = new List<AgentConditionTerm>
                        {
                            new AgentConditionTerm { kind = "blackboard_bool", blackboardKey = blackboardKey },
                            new AgentConditionTerm { kind = "action_request", request = Request(to, intent) }
                        }
                    }
                }
            });
        }

        void ExpandDodgeCancel(AgentControllerIntent intent, AgentPatchIR patch)
        {
            string stateMachine = StateMachineName(intent, "Action StateMachine");
            AddStateMachine(patch, stateMachine);
            AddState(patch, stateMachine, "Dodge", new Vector2(480f, -160f));

            string context = ActionContext(intent);
            List<string> sources = new List<string>();
            for (int i = 0; i < intent.cancel.Count; i++)
            {
                AgentControllerIntentCancel cancel = intent.cancel[i];
                string from = string.IsNullOrEmpty(cancel.from) ? string.Empty : cancel.from;
                string to = string.IsNullOrEmpty(cancel.to) ? "Dodge" : cancel.to;
                string request = string.IsNullOrEmpty(cancel.request) ? intent.request : cancel.request;
                if (!string.IsNullOrEmpty(from))
                {
                    sources.Add(from);
                    AddRequestTransition(patch, stateMachine, from, to, request, 100 + i, $"dodge-cancel-{i}");
                }
            }

            if (sources.Count == 0)
            {
                for (int i = 0; i < intent.steps.Count; i++)
                    AddRequestTransition(patch, stateMachine, intent.steps[i].state, "Dodge", intent.request, 100 + i, $"dodge-cancel-{i}");
            }

            patch.operations.Add(new AgentPatchOperation
            {
                id = "dodge-cancel-lifecycle",
                op = "ensure_action_lifecycle_transition",
                stateMachine = stateMachine,
                state = "Dodge",
                displayName = "Cancel Active Action",
                lifecycleSlot = "OnEnter",
                lifecycleType = "Cancel",
                reason = FirstCancelReason(intent),
                actionContext = context,
                actionContextAssetPath = intent.actionContextAssetPath,
                actionContextAssetGuid = intent.actionContextAssetGuid,
                position = new Vector2(-120f, -120f)
            });
        }

        void ExpandHitReaction(AgentControllerIntent intent, AgentPatchIR patch)
        {
            string stateMachine = StateMachineName(intent, "Action StateMachine");
            string state = string.IsNullOrEmpty(intent.hitReactionState) ? "HitReaction" : intent.hitReactionState;
            AddStateMachine(patch, stateMachine);
            AddState(patch, stateMachine, state, new Vector2(480f, 160f));

            if (!string.IsNullOrEmpty(intent.hitReactionTimeline))
            {
                AgentControllerIntentStep step = new AgentControllerIntentStep
                {
                    state = state,
                    timeline = intent.hitReactionTimeline,
                    actionProfile = intent.hitReactionActionProfile
                };
                AddTimeline(patch, stateMachine, step, intent, 0);
            }
        }

        void ExpandLocomotion(AgentControllerIntent intent, AgentPatchIR patch)
        {
            string stateMachine = StateMachineName(intent, "Locomotion StateMachine");
            AddStateMachine(patch, stateMachine);

            List<string> states = intent.locomotionStates != null && intent.locomotionStates.Count > 0
                ? intent.locomotionStates
                : new List<string> { "Idle", "WalkStart", "WalkLoop", "WalkEnd", "RunStart", "RunLoop", "RunEnd", "MovingTurn" };

            for (int i = 0; i < states.Count; i++)
                AddState(patch, stateMachine, states[i], new Vector2(i * 220f, 0f));

            if (states.Count > 0)
                AddTransition(patch, stateMachine, "Enter", states[0], 0, "locomotion-enter");
        }

        void AddActionStateBody(
            AgentPatchIR patch,
            string stateMachine,
            AgentControllerIntentStep step,
            AgentControllerIntent intent,
            int index,
            bool completeExitLifecycle = false)
        {
            AddState(patch, stateMachine, step.state, new Vector2(240f + index * 240f, -80f));
            patch.operations.Add(completeExitLifecycle
                ? new AgentPatchOperation
                {
                    id = $"{step.state}.exit-lifecycle",
                    op = "ensure_action_exit_lifecycle",
                    stateMachine = stateMachine,
                    state = step.state,
                    actionContext = ActionContext(intent),
                    actionContextAssetPath = intent.actionContextAssetPath,
                    actionContextAssetGuid = intent.actionContextAssetGuid,
                    reason = "ComboWindow",
                    abortReason = "TreeAbort",
                    completeReason = "TimelineCompleted",
                    cancelGuards = new List<AgentConditionTerm>
                    {
                        new AgentConditionTerm { kind = "blackboard_bool", blackboardKey = $"{step.state}Cancel" }
                    },
                    position = new Vector2(-180f, 120f)
                }
                : new AgentPatchOperation
            {
                id = $"{step.state}.activate",
                op = "ensure_action_activation",
                stateMachine = stateMachine,
                state = step.state,
                displayName = $"Activate {step.actionProfile}",
                sourceInputRequestId = Request(step, intent),
                actionProfile = step.actionProfile,
                actionContext = ActionContext(intent),
                actionContextAssetPath = intent.actionContextAssetPath,
                actionContextAssetGuid = intent.actionContextAssetGuid,
                lifecycleSlot = "OnEnter",
                position = new Vector2(-180f, -120f)
            });
            AddTimeline(patch, stateMachine, step, intent, index);
            patch.operations.Add(new AgentPatchOperation
            {
                id = $"{step.state}.complete",
                op = "ensure_action_lifecycle_transition",
                stateMachine = stateMachine,
                state = step.state,
                displayName = $"Complete {step.actionProfile}",
                lifecycleSlot = "OnExit",
                lifecycleType = "Complete",
                reason = "StateExit",
                actionContext = ActionContext(intent),
                actionContextAssetPath = intent.actionContextAssetPath,
                actionContextAssetGuid = intent.actionContextAssetGuid,
                position = new Vector2(-180f, 120f)
            });
        }

        void AddTimeline(AgentPatchIR patch, string stateMachine, AgentControllerIntentStep step, AgentControllerIntent intent, int index)
        {
            patch.operations.Add(new AgentPatchOperation
            {
                id = $"{step.state}.timeline",
                op = "ensure_timeline_node",
                stateMachine = stateMachine,
                state = step.state,
                displayName = $"Play {step.timeline}",
                timeline = step.timeline,
                timelineOwnership = step.timelineOwnership,
                timelineAssetPath = step.timelineAssetPath,
                timelineAssetGuid = step.timelineAssetGuid,
                actionContext = ActionContext(intent),
                actionContextAssetPath = intent.actionContextAssetPath,
                actionContextAssetGuid = intent.actionContextAssetGuid,
                lifecycleSlot = "Root",
                position = new Vector2(0f, index * 120f)
            });
        }

        void AddStateMachine(AgentPatchIR patch, string stateMachine)
        {
            patch.operations.Add(new AgentPatchOperation
            {
                id = $"{stateMachine}.ensure",
                op = "ensure_state_machine",
                graph = "root",
                stateMachine = stateMachine,
                displayName = stateMachine,
                position = new Vector2(0f, 0f)
            });
        }

        void AddState(AgentPatchIR patch, string stateMachine, string state, Vector2 position)
        {
            patch.operations.Add(new AgentPatchOperation
            {
                id = $"{stateMachine}.{state}.ensure",
                op = "ensure_state",
                stateMachine = stateMachine,
                state = state,
                displayName = state,
                position = position
            });
        }

        void AddTransition(AgentPatchIR patch, string stateMachine, string from, string to, int priority, string id)
        {
            patch.operations.Add(new AgentPatchOperation
            {
                id = id,
                op = "ensure_transition",
                stateMachine = stateMachine,
                from = from,
                to = to,
                transitionPriority = priority
            });
        }

        void AddCompletionTransition(AgentPatchIR patch, string stateMachine, string from, string to, int priority, string id)
        {
            patch.operations.Add(new AgentPatchOperation
            {
                id = id,
                op = "ensure_condition_rule",
                stateMachine = stateMachine,
                from = from,
                to = to,
                conditionGroups = SingleConditionGroup(new AgentConditionTerm
                {
                    kind = "state_root_completed"
                }),
                transitionPriority = priority
            });
        }

        void AddRequestTransition(AgentPatchIR patch, string stateMachine, string from, string to, string request, int priority, string id)
        {
            patch.operations.Add(new AgentPatchOperation
            {
                id = id,
                op = "ensure_condition_rule",
                stateMachine = stateMachine,
                from = from,
                to = to,
                conditionGroups = SingleConditionGroup(new AgentConditionTerm
                {
                    kind = "action_request",
                    request = request
                }),
                transitionPriority = priority
            });
        }

        static List<AgentConditionGroup> SingleConditionGroup(AgentConditionTerm term)
        {
            return new List<AgentConditionGroup>
            {
                new AgentConditionGroup
                {
                    terms = new List<AgentConditionTerm> { term }
                }
            };
        }

        static AgentControllerIntentStep FirstStep(AgentControllerIntent intent)
        {
            return intent.steps != null && intent.steps.Count > 0 ? intent.steps[0] : null;
        }

        static string Request(AgentControllerIntentStep step, AgentControllerIntent intent)
        {
            if (step != null && !string.IsNullOrEmpty(step.request))
                return step.request;
            return intent.request;
        }

        static string ActionContext(AgentControllerIntent intent)
        {
            return string.IsNullOrEmpty(intent.actionContext) ? "ActionContext" : intent.actionContext;
        }

        static string StateMachineName(AgentControllerIntent intent, string fallback)
        {
            return string.IsNullOrEmpty(intent.stateMachine) ? fallback : intent.stateMachine;
        }

        static string FirstCancelReason(AgentControllerIntent intent)
        {
            if (intent.cancel != null && intent.cancel.Count > 0 && !string.IsNullOrEmpty(intent.cancel[0].reason))
                return intent.cancel[0].reason;
            return "DodgeCancel";
        }
    }
}
