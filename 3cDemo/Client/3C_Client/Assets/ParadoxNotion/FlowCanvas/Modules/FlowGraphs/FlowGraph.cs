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

        public virtual bool CanAuthorNodeType(System.Type type) => type != null && baseNodeType.IsAssignableFrom(type); // 3C: domain catalogs filter the native node menu.

        public virtual bool CanAuthorConnection(Port source, Port target, out string reason) { // 3C: domain rules apply to native create and relink operations.
            reason = null;
            return true;
        }

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

        public virtual bool usesExplicitPortSelection => false; // 3C: opt into declaration-based creation without changing existing graph domains.

        // 3C: domains route native port creation through their authoring transaction.
        public virtual BinderConnection CreatePortConnection(Port source, Port target) {
            return BinderConnection.CreateValidated(source, target);
        }

        // 3C: a domain disconnects a whole port in one transaction.
        public virtual void DisconnectPort(Port port) {
            foreach (var connection in port.GetPortConnections().ToArray()) { RemoveConnection(connection); }
        }

        ///...
        public T AddFlowNode<T>(Vector2 pos, Port context, object dropInstance) where T : FlowNode { return (T)AddFlowNode(typeof(T), pos, context, dropInstance); }
        public FlowNode AddFlowNode(System.Type type, Vector2 pos, Port context, object dropInstance) {
            var node = (FlowNode)this.AddNode(type, pos);
            FinalizeNodeAdditionToPortAndInstance(node, context, dropInstance);
            return node;
        }

        public virtual void AppendNodeCreationItem(UnityEditor.GenericMenu menu, string category, System.Type type, Vector2 pos, Port context, object dropInstance) { // 3C: domains resolve ambiguous ports before creating a node.
            menu.AddItem(new GUIContent(category), false, () => AddFlowNode(type, pos, context, dropInstance));
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
                if (!CanAuthorNodeType(info.type)) { continue; } // 3C
                if ( contextPort != null && !usesExplicitPortSelection ) { // 3C: domains use the complete registered port shape.

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
                AppendNodeCreationItem(menu, category, info.type, pos, contextPort, dropInstance); // 3C
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
