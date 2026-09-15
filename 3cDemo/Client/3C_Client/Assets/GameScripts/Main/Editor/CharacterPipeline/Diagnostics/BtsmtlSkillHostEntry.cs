using System;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal static class BtsmtlSkillHostEntry
    {
        internal static CharacterPipelineDefinition ResolveDefinition(UnityEngine.Object host)
        {
            if (host is FixedCharacterHost fixedHost)
                return fixedHost.CharacterDefinition;
            return null;
        }

        internal static void ShowHostMenu(int hostInstanceId)
        {
            CharacterPipelineDefinition definition = ResolveDefinition(EditorUtility.InstanceIDToObject(hostInstanceId));
            var menu = new GenericMenu();
            if (!definition)
                menu.AddDisabledItem(new GUIContent("当前Host没有可定位的技能作者Definition"));
            else
            {
                RuntimeDebugSession session = RuntimeDebugSession.Shared;
                if (Application.isPlaying && (!session.ViewModel.Attached || session.ViewModel.Target.HostInstanceId != hostInstanceId))
                    session.AttachToHost(hostInstanceId);
                foreach (BtsmtlSkillFlowGraph graph in definition.SkillGraphs.Where(value => value))
                {
                    string prefix = Label(graph.name) + " [" + graph.AuthoringId + "]/";
                    if (!Application.isPlaying)
                        menu.AddItem(new GUIContent(prefix + "编辑技能图"), false, () =>
                            RuntimeDebugSourceNavigator.Open(definition, RuntimeSourceElementKey.Graph(graph.AuthoringId)));
                    else if (!session.ViewModel.Attached || session.ViewModel.Target.HostInstanceId != hostInstanceId)
                        menu.AddDisabledItem(new GUIContent(prefix + "当前角色尚未注册运行诊断"));
                    else
                        AddInstances(menu, prefix, definition, graph, session, session.ViewModel.Target.CharacterRuntimeId, default);
                }
                if (definition.SkillGraphs.Count == 0)
                    menu.AddDisabledItem(new GUIContent("尚未配置原生技能根"));
            }
            menu.ShowAsContext();
        }

        internal static void ShowInstances(CharacterPipelineDefinition definition, FlowGraph graph,
            RuntimeDebugSession session, Guid actor, RuntimeInstanceKey selected)
        {
            var menu = new GenericMenu();
            AddInstances(menu, string.Empty, definition, graph, session, actor, selected);
            menu.ShowAsContext();
        }

        static void AddInstances(GenericMenu menu, string prefix, CharacterPipelineDefinition definition,
            FlowGraph graph, RuntimeDebugSession session, Guid actor, RuntimeInstanceKey selected)
        {
            RuntimeDebugViewModel view = session.ViewModel;
            string graphId = ((IBtsmtlSkillFlowGraph)graph).AuthoringId;
            if (!view.Valid || view.Target.CharacterRuntimeId != actor)
            {
                menu.AddDisabledItem(new GUIContent(prefix + "当前诊断目标与此角色不一致，请从Host重新选择"));
                return;
            }
            RuntimeInstanceKey[] instances = view.GetGraphInstances(graphId)
                .Where(value => value.Kind == RuntimeInstanceKind.SkillExecution && value.CharacterRuntimeId == actor)
                .OrderByDescending(view.InvocationSequence).ToArray();
            Guid sessionId = view.Target.SessionId;
            foreach (RuntimeInstanceKey instance in instances)
            {
                string label = $"{prefix}释放 {instance.ActionInstanceId} · 代次 {instance.ActivationGeneration} · 调用 {instance.InvocationGeneration} · {Label(instance.CallSiteId)}";
                menu.AddItem(new GUIContent(label), instance.Equals(selected), () =>
                {
                    if (session.ViewModel.Target.CharacterRuntimeId != actor || session.ViewModel.Target.SessionId != sessionId)
                        return;
                    RuntimeDebugSourceNavigator.Open(definition, RuntimeSourceElementKey.Graph(graphId), instance);
                });
            }
            if (instances.Length == 0)
                menu.AddDisabledItem(new GUIContent(prefix + "尚无已采集的技能执行记录"));
        }

        static string Label(string value) => (value ?? string.Empty).Replace('/', '→');
    }
}
