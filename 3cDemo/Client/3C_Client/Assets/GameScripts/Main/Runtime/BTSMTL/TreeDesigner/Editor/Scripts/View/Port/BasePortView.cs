using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public abstract class GraphAuthoringPortViewBase : Port
    {
        protected GraphAuthoringPortViewBase(
            Orientation portOrientation,
            Direction portDirection,
            Capacity portCapacity,
            Type type)
            : base(
                portOrientation,
                portDirection,
                portCapacity,
                type)
        {
            style.height = 25;
        }

        protected void InstallConnector<TEdge>()
            where TEdge : Edge, new()
        {
            m_EdgeConnector =
                new EdgeConnector<TEdge>(
                    new DefaultEdgeConnectorListener());
            this.AddManipulator(m_EdgeConnector);
        }

        public override void OnStartEdgeDragging()
        {
            base.OnStartEdgeDragging();
        }

        public override void OnStopEdgeDragging()
        {
            base.OnStopEdgeDragging();
        }

        sealed class DefaultEdgeConnectorListener :
            IEdgeConnectorListener
        {
            readonly GraphViewChange m_GraphViewChange;
            readonly List<Edge> m_EdgesToCreate;
            readonly List<GraphElement> m_EdgesToDelete;

            public DefaultEdgeConnectorListener()
            {
                m_EdgesToCreate = new List<Edge>();
                m_EdgesToDelete = new List<GraphElement>();
                m_GraphViewChange.edgesToCreate =
                    m_EdgesToCreate;
            }

            public void OnDropOutsidePort(
                Edge edge,
                Vector2 position)
            {
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                m_EdgesToCreate.Clear();
                m_EdgesToCreate.Add(edge);
                m_EdgesToDelete.Clear();
                CollectSingleCapacityConnections(
                    edge.input,
                    edge);
                CollectSingleCapacityConnections(
                    edge.output,
                    edge);

                if (m_EdgesToDelete.Count > 0)
                    graphView.DeleteElements(m_EdgesToDelete);

                List<Edge> edgesToCreate =
                    m_EdgesToCreate;
                if (graphView.graphViewChanged != null)
                {
                    edgesToCreate =
                        graphView.graphViewChanged(
                            m_GraphViewChange)
                        .edgesToCreate;
                }

                foreach (Edge item in edgesToCreate)
                {
                    graphView.AddElement(item);
                    item.input.Connect(item);
                    item.output.Connect(item);
                }
            }

            void CollectSingleCapacityConnections(
                Port port,
                Edge pending)
            {
                if (port.capacity != Capacity.Single)
                    return;
                foreach (Edge connection in port.connections)
                {
                    if (connection != pending)
                        m_EdgesToDelete.Add(connection);
                }
            }
        }
    }

    public class BasePortView : GraphAuthoringPortViewBase
    {
        protected string m_Name;
        public string Name => m_Name;
        public GraphAuthoringPortId AuthoringPortId
        {
            get;
            private set;
        }
        public GraphAuthoringPortDescriptor
            AuthoringDescriptor { get; private set; }
        public GraphAuthoringDynamicPortProjection?
            AuthoringDynamicProjection { get; private set; }

        public BaseNodeView NodeView => node as BaseNodeView;

        public void BindAuthoringPort(
            GraphAuthoringPortId portId)
        {
            AuthoringPortId = portId.IsValid
                ? portId
                : throw new ArgumentException(
                    "BTSMTL authoring port identity is missing.",
                    nameof(portId));
        }

        public void BindFixedAuthoringPort(
            GraphAuthoringPortDescriptor descriptor)
        {
            AuthoringDescriptor = descriptor ??
                throw new ArgumentNullException(
                    nameof(descriptor));
            AuthoringDynamicProjection = null;
            BindAuthoringPort(descriptor.PortId);
            ValidateAuthoringShape(
                descriptor.Direction,
                descriptor.Capacity);
        }

        public void BindDynamicAuthoringPort(
            GraphAuthoringDynamicPortProjection projection)
        {
            AuthoringDescriptor = null;
            AuthoringDynamicProjection = projection;
            BindAuthoringPort(projection.PortId);
            ValidateAuthoringShape(
                projection.Direction,
                projection.Capacity);
        }

        void ValidateAuthoringShape(
            GraphAuthoringPortDirection expectedDirection,
            GraphAuthoringPortCapacity expectedCapacity)
        {
            GraphAuthoringPortDirection actualDirection =
                direction == Direction.Input
                    ? GraphAuthoringPortDirection.Input
                    : GraphAuthoringPortDirection.Output;
            GraphAuthoringPortCapacity actualCapacity =
                capacity == Capacity.Single
                    ? GraphAuthoringPortCapacity.Single
                    : GraphAuthoringPortCapacity.Multiple;
            if (actualDirection != expectedDirection ||
                actualCapacity != expectedCapacity)
            {
                throw new InvalidOperationException(
                    $"BTSMTL Port '{AuthoringPortId}' shape does not match its capability projection.");
            }
        }

        protected BasePortView(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {
        }

        public static BasePortView Create<TEdge>(string name, Orientation orientation, Direction direction, Capacity capacity, Type type) where TEdge : Edge, new()
        {
            BasePortView port = new BasePortView(orientation, direction, capacity, type)
            {
                m_Name = name
            };
            port.InstallConnector<TEdge>();
            return port;
        }
    }
}
