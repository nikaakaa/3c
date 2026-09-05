using System.Collections.Generic;
using System.Linq;
using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace FlowCanvas
{

    ///<summary>Base class for flow graphs.</summary>
    [GraphInfo(
        packageName = "FlowCanvas",
        docsURL = "https://flowcanvas.paradoxnotion.com/documentation/",
        resourcesURL = "https://flowcanvas.paradoxnotion.com/downloads/"
        )]
    [System.Serializable]
    abstract public class FlowGraph : Graph
    {

        private Dictionary<System.Type, Component> cachedAgentComponents;

        ///----------------------------------------------------------------------------------------------

        ///<summary>Returns cached component type from graph agent</summary>
        public UnityEngine.Object GetAgentComponent(System.Type type) {
            if ( agent == null ) { return null; }
            if ( type == typeof(GameObject) ) { return agent.gameObject; }
            if ( type == typeof(Transform) ) { return agent.transform; }
            if ( type == typeof(Component) ) { return agent; }

            if ( cachedAgentComponents == null ) {
                cachedAgentComponents = new Dictionary<System.Type, Component>();
            }

            if ( cachedAgentComponents.TryGetValue(type, out Component component) ) {
                return component;
            }

            if ( typeof(Component).RTIsAssignableFrom(type) || type.RTIsInterface() ) {
                component = agent.GetComponent(type);
            }

            return cachedAgentComponents[type] = component;
        }


        ///----------------------------------------------------------------------------------------------
        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        ///...
        public T AddFlowNode<T>(Vector2 pos, Port context, object dropInstance) where T : FlowNode { return (T)AddFlowNode(typeof(T), pos, context, dropInstance); }
        public FlowNode AddFlowNode(System.Type type, Vector2 pos, Port context, object dropInstance) {
            var node = (FlowNode)this.AddNode(type, pos);
            FinalizeNodeAdditionToPortAndInstance(node, context, dropInstance);
            return node;
        }

        ///...
        public void FinalizeNodeAdditionToPortAndInstance(FlowNode node, Port context, object dropInstance) {
            node.BindFirstPortOfTypeToOtherPort(context);
            node.SetFirstPortOfTypeToInstance(dropInstance);
            NodeCanvas.Editor.GraphEditorUtility.activeElement = node;
        }


        //Append menu items in canvas right click context menu (provided menu is completely overriden here)
        protected override UnityEditor.GenericMenu OnCanvasContextMenu(UnityEditor.GenericMenu menu, Vector2 mousePos) {
            return GetNodesMenu(mousePos, null, null);
        }

        //...
        virtual public UnityEditor.GenericMenu GetNodesMenu(Vector2 mousePos, Port context, Object dropInstance) {
            return AppendFlowNodesMenu(new UnityEditor.GenericMenu(), "", mousePos, context, dropInstance);
        }

        //...
        public UnityEditor.GenericMenu AppendFlowNodesMenu(UnityEditor.GenericMenu menu, string baseCategory, Vector2 pos, Port contextPort, object dropInstance) {
            var infos = EditorUtils.GetScriptInfosOfType(baseNodeType);
            var generalized = new List<System.Type>();
            foreach ( var _info in infos ) {
                var info = _info;
                if ( contextPort != null ) {

                    if ( generalized.Contains(info.originalType) ) {
                        continue;
                    }

                    if ( contextPort.IsValuePort() && info.originalType.IsGenericTypeDefinition ) {
                        var genericInfo = info.MakeGenericInfo(contextPort.type);
                        if ( genericInfo.isValid ) {
                            info = genericInfo;
                            generalized.Add(info.originalType);
                        }
                    }

                    IEnumerable<System.Type> attributeDefinedTypes = new System.Type[0];
                    if ( contextPort.IsOutputPort() ) {
                        var att = info.type.RTGetAttribute<FlowNode.ContextDefinedInputsAttribute>(true);
                        if ( att != null ) { attributeDefinedTypes = att.types.Select(t => t == typeof(Wild) && info.type.IsGenericType ? info.type.RTGetGenericArguments().First() : t); }
                    }

                    if ( contextPort.IsInputPort() ) {
                        var att = info.type.RTGetAttribute<FlowNode.ContextDefinedOutputsAttribute>(true);
                        if ( att != null ) { attributeDefinedTypes = att.types.Select(t => t == typeof(Wild) && info.type.IsGenericType ? info.type.RTGetGenericArguments().First() : t); }
                    }

                    if ( contextPort is ValueOutput ) {
                        var portTypes = info.type.RTGetFields()
                        .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(ValueInput<>))
                        .Select(f => f.FieldType.RTGetGenericArguments().First())
                        .Union(attributeDefinedTypes);
                        if ( !portTypes.Any(t => t.IsAssignableFrom(contextPort.type)) ) { continue; }
                    }

                    if ( contextPort is ValueInput ) {
                        var portTypes = info.type.RTGetFields()
                        .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(ValueOutput<>))
                        .Select(f => f.FieldType.RTGetGenericArguments().First())
                        .Union(attributeDefinedTypes);
                        if ( !portTypes.Any(t => contextPort.type.IsAssignableFrom(t)) ) { continue; }
                    }

                    if ( contextPort is FlowOutput ) {
                        var portTypes = info.type.RTGetFields().Select(f => f.FieldType).Union(attributeDefinedTypes);
                        if ( !portTypes.Any(t => t == typeof(FlowInput) || t == typeof(Flow)) ) { continue; }
                    }

                    if ( contextPort is FlowInput ) {
                        var portTypes = info.type.RTGetFields().Select(f => f.FieldType).Union(attributeDefinedTypes);
                        if ( !portTypes.Any(t => t == typeof(FlowOutput) || t == typeof(Flow)) ) { continue; }
                    }
                }

                var category = string.Join("/", new string[] { baseCategory, info.category, info.name }).TrimStart('/');
                menu.AddItem(new GUIContent(category), false, (o) => { AddFlowNode((System.Type)o, pos, contextPort, dropInstance); }, info.type);
            }
            return menu;
        }

        //Append buttons to toolbar
        protected override void OnGraphEditorToolbar() {
            UnityEditor.EditorGUIUtility.SetIconSize(new Vector2(14, 14));
            if ( GUILayout.Button(EditorUtils.GetTempContent(NodeCanvas.Editor.StyleSheet.verboseLevel1, "Minimize All/Selected"), UnityEditor.EditorStyles.toolbarButton) ) {
                foreach ( var node in NodeCanvas.Editor.GraphEditorUtility.GetSelectedOrAll(this).OfType<FlowNode>() ) {
                    node.verboseLevel = Node.VerboseLevel.Compact;
                }
            }
            if ( GUILayout.Button(EditorUtils.GetTempContent(NodeCanvas.Editor.StyleSheet.verboseLevel2, "Connected Ports Only on All/Selected"), UnityEditor.EditorStyles.toolbarButton) ) {
                foreach ( var node in NodeCanvas.Editor.GraphEditorUtility.GetSelectedOrAll(this).OfType<FlowNode>() ) {
                    node.verboseLevel = Node.VerboseLevel.Partial;
                }
            }
            if ( GUILayout.Button(EditorUtils.GetTempContent(NodeCanvas.Editor.StyleSheet.verboseLevel3, "Maximize All/Selected\n(You can also hold S Key over a node to temporarily maximize it)"), UnityEditor.EditorStyles.toolbarButton) ) {
                foreach ( var node in NodeCanvas.Editor.GraphEditorUtility.GetSelectedOrAll(this).OfType<FlowNode>() ) {
                    node.verboseLevel = Node.VerboseLevel.Full;
                }
            }
            UnityEditor.EditorGUIUtility.SetIconSize(Vector2.zero);
        }

#endif
        ///----------------------------------------------------------------------------------------------
    }
}