using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentSynthesisEvaluator
    {
        readonly AgentGraphSnapshotExporter m_Exporter = new AgentGraphSnapshotExporter();
        readonly AgentMacroLibrary m_Macros = new AgentMacroLibrary();
        readonly AgentPatchCompiler m_Compiler = new AgentPatchCompiler();
        readonly AgentGraphValidator m_Validator = new AgentGraphValidator();

        public AgentCompileReport EvaluateDefaultSamples(CharacterPipelineDefinition definition)
        {
            AgentCompileReport summary = new AgentCompileReport { success = true };
            AgentGraphSnapshot snapshot = m_Exporter.ExportFull(definition);
            List<AgentControllerIntent> samples = CreateDefaultSamples();

            for (int i = 0; i < samples.Count; i++)
            {
                AgentControllerIntent intent = samples[i];
                AgentCompileReport macroReport = new AgentCompileReport { success = true };
                if (!m_Macros.TryExpand(intent, snapshot, out AgentPatchIR patch, macroReport))
                {
                    summary.metrics.schemaInvalidCount++;
                    AppendMessages(summary, macroReport, $"sample[{i}]");
                    continue;
                }

                summary.metrics.schemaValidCount++;
                AgentCompileReport compileReport = m_Compiler.Compile(definition, snapshot, patch, false);
                AppendMessages(summary, compileReport, $"sample[{i}]");
                if (compileReport.HasErrors())
                    summary.metrics.compileFailureCount++;
                else
                    summary.metrics.compileSuccessCount++;
            }

            AgentCompileReport validationReport = m_Validator.Validate(definition);
            AppendMessages(summary, validationReport, "current-graph");
            if (validationReport.HasErrors())
                summary.metrics.semanticInvalidCount++;
            else
                summary.metrics.semanticValidCount++;

            summary.success = !summary.HasErrors();
            return summary;
        }

        static List<AgentControllerIntent> CreateDefaultSamples()
        {
            return new List<AgentControllerIntent>
            {
                new AgentControllerIntent
                {
                    macro = "locomotion_state_machine",
                    stateMachine = "Locomotion StateMachine",
                    locomotionStates = new List<string> { "Idle", "WalkStart", "WalkLoop", "WalkEnd", "RunStart", "RunLoop", "RunEnd", "MovingTurn" }
                },
                new AgentControllerIntent
                {
                    macro = "single_timeline_action",
                    request = "Attack",
                    stateMachine = "Action StateMachine",
                    steps = new List<AgentControllerIntentStep>
                    {
                        new AgentControllerIntentStep { state = "Attack1", request = "Attack", actionProfile = "Attack.Light.01", timeline = "Attack1" }
                    }
                },
                new AgentControllerIntent
                {
                    macro = "two_hit_combo",
                    request = "Attack",
                    stateMachine = "Action StateMachine",
                    steps = new List<AgentControllerIntentStep>
                    {
                        new AgentControllerIntentStep { state = "Attack1", request = "Attack", actionProfile = "Attack.Light.01", timeline = "Attack1" },
                        new AgentControllerIntentStep { state = "Attack2", request = "Attack", actionProfile = "Attack.Light.02", timeline = "Attack2" }
                    }
                },
                new AgentControllerIntent
                {
                    macro = "dodge_cancel",
                    request = "Dodge",
                    stateMachine = "Action StateMachine",
                    steps = new List<AgentControllerIntentStep>
                    {
                        new AgentControllerIntentStep { state = "Attack1" },
                        new AgentControllerIntentStep { state = "Attack2" }
                    },
                    cancel = new List<AgentControllerIntentCancel>
                    {
                        new AgentControllerIntentCancel { from = "Attack1", to = "Dodge", request = "Dodge", reason = "DodgeCancel" },
                        new AgentControllerIntentCancel { from = "Attack2", to = "Dodge", request = "Dodge", reason = "DodgeCancel" }
                    }
                },
                new AgentControllerIntent
                {
                    macro = "hit_reaction",
                    stateMachine = "Action StateMachine",
                    hitReactionState = "HitReaction",
                    hitReactionTimeline = "HitReaction"
                }
            };
        }

        static void AppendMessages(AgentCompileReport target, AgentCompileReport source, string prefix)
        {
            if (source == null)
                return;

            for (int i = 0; i < source.messages.Count; i++)
            {
                AgentCompileMessage message = source.messages[i];
                target.messages.Add(new AgentCompileMessage
                {
                    severity = message.severity,
                    path = $"{prefix}/{message.path}",
                    code = message.code,
                    message = message.message,
                    suggestion = message.suggestion
                });
            }
        }
    }
}
