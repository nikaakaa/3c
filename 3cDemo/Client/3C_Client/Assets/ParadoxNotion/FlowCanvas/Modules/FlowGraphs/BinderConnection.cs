#define DO_EDITOR_BINDING //comment this out to test the real performance without editor binding specifics

using NodeCanvas.Framework;
using ParadoxNotion;
using UnityEngine;
using Logger = ParadoxNotion.Services.Logger;

namespace FlowCanvas
{

    public class BinderConnection : Connection
    {

        [ParadoxNotion.Serialization.FullSerializer.fsSerializeAs("_sourcePortName")]
        private string _sourcePortID;
        [ParadoxNotion.Serialization.FullSerializer.fsSerializeAs("_targetPortName")]
        private string _targetPortID;

        [System.NonSerialized]
        private Port _sourcePort;
        [System.NonSerialized]
        private Port _targetPort;

        // 3C: authoring domains can retain their canonical serialized port identities.
        protected virtual string serializedSourcePortID { get => _sourcePortID; set => _sourcePortID = value; }
        protected virtual string serializedTargetPortID { get => _targetPortID; set => _targetPortID = value; }
        protected void InvalidatePortReferences() { _sourcePort = null; _targetPort = null; }

        ///<summary>The source port ID name this binder is connected to</summary>
        public string sourcePortID {
            get { return sourcePort != null ? sourcePort.ID : serializedSourcePortID; }
            private set { serializedSourcePortID = value; }
        }

        ///<summary>The target port ID name this binder is connected to</summary>
        public string targetPortID {
            get { return targetPort != null ? targetPort.ID : serializedTargetPortID; }
            private set { serializedTargetPortID = value; }
        }

        ///<summary>The source Port</summary>
        public Port sourcePort {
            get
            {
                if ( _sourcePort == null ) {
                    if ( sourceNode is FlowNode ) { //In case it's 'MissingNode'
                        _sourcePort = ( sourceNode as FlowNode ).GetOutputPort(serializedSourcePortID);
                    }
                }
                return _sourcePort;
            }
        }

        ///<summary>The target Port</summary>
        public Port targetPort {
            get
            {
                if ( _targetPort == null ) {
                    if ( targetNode is FlowNode ) { //In case it's 'MissingNode'
                        _targetPort = ( targetNode as FlowNode ).GetInputPort(serializedTargetPortID);
                    }
                }
                return _targetPort;
            }
        }

        ///<summary>The binder type. In case of Value connection, BinderConnection<T> is used, else it's basicaly a Flow binding</summary>
        public virtual System.Type bindingType => GetType().RTIsGenericType() ? GetType().RTGetGenericArguments()[0] : typeof(Flow); // 3C: domain semantic port type.

        ///----------------------------------------------------------------------------------------------

        ///<summary>Create a NEW BinderConnection object between two ports</summary>
        public static BinderConnection Create(Port source, Port target) {
            if ( !CanBeBoundVerbosed(source, target, null, out string verbose) ) {
                Logger.LogWarning(verbose, LogTag.EDITOR, source?.parent);
                return null;
            }
#if UNITY_EDITOR
            if (source?.parent?.graph is FlowGraph graph) { return graph.CreatePortConnection(source, target); } // 3C: native UI uses the domain transaction.
#endif
            return CreateValidated(source, target);
        }

        internal static BinderConnection CreateValidated(Port source, Port target) {
            return CreateValidated(source, target, null);
        }

        // 3C: domain graphs create their own connection subclasses through the graph factory.
        public static BinderConnection CreateValidatedForDomain(Port source, Port target, System.Func<BinderConnection> createBinder) {
            return CreateValidated(source, target, createBinder);
        }

        static BinderConnection CreateValidated(Port source, Port target, System.Func<BinderConnection> createBinder) {

            ParadoxNotion.Design.UndoUtility.RecordObject(source.parent.graph, "Connect Ports");

            BinderConnection binder = null;
            if ( createBinder != null ) {
                binder = createBinder();
            } else if ( source is FlowOutput && target is FlowInput ) {
                binder = new BinderConnection();
            }

            if ( binder == null && source is ValueOutput && target is ValueInput ) {
                binder = (BinderConnection)System.Activator.CreateInstance(typeof(BinderConnection<>).RTMakeGenericType(new System.Type[] { target.type }));
            }

            if ( binder != null ) {

                binder.sourcePortID = source.ID;
                binder.targetPortID = target.ID;

                binder.SetSourceNode(source.parent);
                binder.SetTargetNode(target.parent);

                binder.sourcePort.connections++;
                binder.targetPort.connections++;

                binder.sourcePort.parent.OnPortConnected(binder.sourcePort, binder.targetPort);
                binder.targetPort.parent.OnPortConnected(binder.targetPort, binder.sourcePort);

                //for live editing
                if ( Application.isPlaying ) {
                    binder.Bind();
                }
            }

            ParadoxNotion.Design.UndoUtility.SetDirty(source.parent.graph);
            return binder;
        }

        ///<summary>Set binder source port</summary>
        public virtual void SetSourcePort(Port newSourcePort) { // 3C: domain relink transaction.
            if ( newSourcePort == sourcePort ) {
                return;
            }

            if ( newSourcePort == null || !newSourcePort.IsOutputPort() ) {
                return;
            }

            if ( sourcePort != null ) {
                sourcePort.connections--;
                sourcePort.parent.OnPortDisconnected(sourcePort, targetPort);
                if ( Application.isPlaying ) {
                    UnBind();
                }
            }

            _sourcePort = null; //flush
            sourcePortID = newSourcePort.ID;
            base.SetSourceNode(newSourcePort.parent);
            GatherAndValidateSourcePort();
            newSourcePort.parent.OnPortConnected(newSourcePort, targetPort);
            if ( Application.isPlaying ) {
                Bind();
            }
        }

        ///<summary>Set binder target port</summary>
        public virtual void SetTargetPort(Port newTargetPort) { // 3C: domain relink transaction.
            if ( newTargetPort == targetPort ) {
                return;
            }

            if ( newTargetPort == null || !newTargetPort.IsInputPort() ) {
                return;
            }

            if ( targetPort != null ) {
                targetPort.connections--;
                targetPort.parent.OnPortDisconnected(targetPort, sourcePort);
                if ( Application.isPlaying ) {
                    UnBind();
                }
            }

            _targetPort = null; //flush
            targetPortID = newTargetPort.ID;
            base.SetTargetNode(newTargetPort.parent);
            GatherAndValidateTargetPort();
            newTargetPort.parent.OnPortConnected(newTargetPort, sourcePort);
            if ( Application.isPlaying ) {
                Bind();
            }
        }

        ///----------------------------------------------------------------------------------------------

        ///<summary>Called after the node has GatherPorts to gather the references and validate the binding connection</summary>
        public void GatherAndValidateSourcePort() {
            _sourcePort = null; //refetch
            if ( sourcePort != null ) {
                //all good
                if ( TypeConverter.HasConvertion(sourcePort.type, bindingType) ) {
                    sourcePortID = sourcePort.ID;
                    sourcePort.connections++;
                    return;
                }
                //the cast is invalid
                Logger.LogError(string.Format("Output Port with ID '{0}' cast is invalid.", sourcePortID), LogTag.VALIDATION, sourceNode);
                sourcePort.FlagInvalidCast();
                sourcePortID = sourcePort.ID;
                sourcePort.connections++;
                return;
            }

            //the id is missing...
            Logger.LogError(string.Format("Output Port with ID '{0}' is missing on node {1}", sourcePortID, sourceNode.name), LogTag.VALIDATION, sourceNode);
            var source = sourceNode as FlowNode;
            Port missingPort = null;
            if ( bindingType == typeof(Flow) ) {
                missingPort = source.AddFlowOutput(sourcePortID, sourcePortID);
            } else {
                missingPort = source.AddValueOutput(sourcePortID, sourcePortID, typeof(object), () => { throw new System.Exception("Port is missing"); });
            }
            missingPort.FlagMissing();
            missingPort.connections++;
        }

        ///<summary>Called after the node has GatherPorts to gather the references and validate the binding connection</summary>
        public void GatherAndValidateTargetPort() {
            _targetPort = null; //refetch
            if ( targetPort != null ) {
                //all good
                if ( targetPort.type == bindingType ) {
                    targetPortID = targetPort.ID;
                    targetPort.connections++;
                    return;
                }

                //replace binder connection type if possible
                if ( targetPort is ValueInput && sourcePort is ValueOutput ) {
                    if ( TypeConverter.HasConvertion(sourcePort.type, targetPort.type) ) {
                        graph.RemoveConnection(this);
                        Create(sourcePort, targetPort);
                        targetPortID = targetPort.ID;
                        targetPort.connections++;
                        return;
                    }
                    //the cast is invalid
                    Logger.LogError(string.Format("Input Port with ID '{0}' cast is invalid.", targetPortID), LogTag.VALIDATION, targetNode);
                    targetPort.FlagInvalidCast();
                    targetPortID = targetPort.ID;
                    targetPort.connections++;
                    return;
                }
            }

            //the id is missing...
            Logger.LogError(string.Format("Input Port with ID '{0}' is missing on node {1}", targetPortID, targetNode.name), LogTag.VALIDATION, targetNode);
            var target = targetNode as FlowNode;
            Port missingPort = null;
            if ( bindingType == typeof(Flow) ) {
                missingPort = target.AddFlowInput(targetPortID, targetPortID, (f) => { throw new System.Exception("Port is missing"); });
            } else {
                missingPort = target.AddValueInput(targetPortID, targetPortID, typeof(object));
            }
            missingPort.FlagMissing();
            missingPort.connections++;
        }

        ///----------------------------------------------------------------------------------------------

        ///<summary>Return whether or not source can connect to target.</summary>
        public static bool CanBeBound(Port source, Port target, BinderConnection refConnection) { return CanBeBoundVerbosed(source, target, refConnection, out string v); }
        ///<summary>Return whether or not source can connect to target. The return is a string with the reason why NOT, null if possible. Providing an existing ref connection, will bypass source/target validation respectively if that connection is already connected to that source/target port.</summary>
        public static bool CanBeBoundVerbosed(Port source, Port target, BinderConnection refConnection, out string verbose) {
            verbose = CanBeBoundVerbosed_Internal(source, target, refConnection);
            return verbose == null;
        }

        //...
        static string CanBeBoundVerbosed_Internal(Port source, Port target, BinderConnection refConnection) {
            if ( source == null || target == null ) {
                return "A port is null.";
            }

            if (source.parent.graph is FlowGraph authoringGraph && !authoringGraph.CanAuthorConnection(source, target, out string reason)) { return reason; } // 3C

            if ( source == target ) {
                // return "Can't connect port to itself.";
                return string.Empty;
            }

            if ( source.parent == target.parent ) {
                return "Can't connect ports on the same node.";
            }

            if ( source.parent == target.parent ) {
                return "Can't connect ports on the same parent node.";
            }

            if ( source.IsInputPort() && target.IsInputPort() ) {
                return "Can't connect input to input.";
            }

            if ( source.IsOutputPort() && target.IsOutputPort() ) {
                return "Can't connect output to output.";
            }

            if ( source.IsFlowPort() != target.IsFlowPort() ) {
                return "Flow ports can only be connected to other Flow ports.";
            }

            if ( refConnection == null || refConnection.sourcePort != source ) {
                if ( !source.CanAcceptConnections() ) {
                    return "Source port can accept no more out connections.";
                }
            }

            if ( refConnection == null || refConnection.targetPort != target ) {
                if ( !target.CanAcceptConnections() ) {
                    return "Target port can accept no more in connections.";
                }
            }

            if ( !TypeConverter.HasConvertion(source.type, target.type) ) {
                return string.Format("Can't connect ports. Type '{0}' is not assignable from Type '{1}' and there exists no automatic conversion for those types.", target.type.FriendlyName(), source.type.FriendlyName());
            }

            return null;
        }

        ///<summary>Callback from base class. The connection reference is already removed from target and source Nodes</summary>
        public override void OnDestroy() {
            if ( sourcePort != null ) {
                sourcePort.connections--;
                sourcePort.parent.OnPortDisconnected(sourcePort, targetPort);
            }
            if ( targetPort != null ) {
                targetPort.connections--;
                targetPort.parent.OnPortDisconnected(targetPort, sourcePort);
            }

            //for live editing
            if ( Application.isPlaying ) {
                UnBind();
            }
        }

        ///<summary>Called in runtime intialize to actualy bind the delegates</summary>
        virtual public void Bind() {

            if ( !isActive ) {
                return;
            }

            if ( sourcePort is FlowOutput && targetPort is FlowInput ) {
                ( sourcePort as FlowOutput ).BindTo((FlowInput)targetPort);

#if UNITY_EDITOR && DO_EDITOR_BINDING
                ( sourcePort as FlowOutput ).Append(BlinkStatus);
#else
                sourcePort.connections++;
                targetPort.connections++;
#endif
            }
        }

        protected void BindValuePorts() {
            ( targetPort as ValueInput ).BindTo((ValueOutput)sourcePort);
#if !UNITY_EDITOR || !DO_EDITOR_BINDING
            sourcePort.connections++;
            targetPort.connections++;
#endif
        }

        ///<summary>UnBinds the delegates</summary>
        virtual public void UnBind() {
            if ( sourcePort is FlowOutput ) {
                ( sourcePort as FlowOutput ).UnBind();
            }
        }

        ///----------------------------------------------------------------------------------------------
        ///---------------------------------------UNITY EDITOR-------------------------------------------
#if UNITY_EDITOR

        private int lastBlinkFrame;

        public override TipConnectionStyle tipConnectionStyle => TipConnectionStyle.None;
        public override Color defaultColor => bindingType == typeof(Flow) ? base.defaultColor : Color.grey;
        // public override Color defaultColor => bindingType == typeof(Flow) ? base.defaultColor : ParadoxNotion.Design.TypePrefs.GetTypeColor(bindingType);
        public override float defaultSize => bindingType == typeof(Flow) ? base.defaultSize + 1 : base.defaultSize;

        //...
        sealed protected override string GetConnectionInfo() {

            if (graph.editorObservation != null) { return graph.editorObservation.GetConnectionText(UID); }

            var case1 = sourcePort == null || sourcePort.bindStatus != Port.BindStatus.Valid;
            var case2 = targetPort == null || targetPort.bindStatus != Port.BindStatus.Valid;
            if ( case1 || case2 ) { return null; }

            // 3C: domain connections contribute their own always-visible authoring label on the link.
            var domainInfo = GetDomainConnectionInfo();
            if ( domainInfo != null ) { return domainInfo; }

            if ( targetPort.willDraw ) {

                if ( Application.isPlaying ) {
                    return GetTransferDataLabel();
                }

                if ( !targetPort.type.IsAssignableFrom(sourcePort.type) ) {
                    return "<size=14>➲</size>";
                }
            }

            return null;
        }

        // 3C: domain connection subclasses override to render a static authoring label on the link.
        virtual protected string GetDomainConnectionInfo() { return null; }

        //Data label to show on binder info
        virtual protected string GetTransferDataLabel() { return null; }

        //...
        protected override void OnConnectionInspectorGUI() {
            GUI.color = GUI.color.WithAlpha(0.5f);
            GUILayout.Label(string.Format("Binding Type Of {0}", bindingType.FriendlyName()));
            GUI.color = Color.white;

            if ( sourcePort == null || sourcePort.bindStatus != Port.BindStatus.Valid ) {
                UnityEditor.EditorGUILayout.HelpBox(string.Format("Source Port with ID '{0}' is {1} on source node.\nYou should relink the connection or simply remove it.", sourcePortID, sourcePort?.bindStatus), UnityEditor.MessageType.Error);
            }
            if ( targetPort == null || targetPort.bindStatus != Port.BindStatus.Valid ) {
                UnityEditor.EditorGUILayout.HelpBox(string.Format("Target Port with ID '{0}' is {1} on target node.\nYou should relink the connection or simply remove it.", targetPortID, targetPort?.bindStatus), UnityEditor.MessageType.Error);
            }
        }

        //...
        protected override string GetError() {
            var case1 = sourcePort == null || sourcePort.bindStatus != Port.BindStatus.Valid;
            var case2 = targetPort == null || targetPort.bindStatus != Port.BindStatus.Valid;
            if ( case1 || case2 ) { return "Port is not valid"; }
            return null;
        }

        ///<summary>Blinks connection status</summary>
        protected void BlinkStatus(Flow f = default) {
            if ( Application.isPlaying ) {
                lastBlinkFrame = graph.lastUpdateFrame;
                status = Status.Running;
            }
        }

        //reset it back to resting
        protected override void OnBeforeUpdateBlinkStatus() {
            if ( Application.isPlaying ) {
                //give +1 frame forward to handle potential different execution order
                if ( graph.lastUpdateFrame > lastBlinkFrame + 1 || !graph.isRunning ) {
                    status = Status.Resting;
                }
            }
        }

#endif
        ///----------------------------------------------------------------------------------------------

    }
}
