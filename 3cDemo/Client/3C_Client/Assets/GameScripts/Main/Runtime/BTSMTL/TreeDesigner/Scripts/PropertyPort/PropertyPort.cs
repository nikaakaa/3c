using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public partial class PropertyPort
    {
        [SerializeField]
        protected string m_Name;
        public string Name { get => m_Name; set => m_Name = value; }

        [SerializeField]
        protected string m_PortId;
        public string PortId => string.IsNullOrEmpty(m_PortId) ? m_Name : m_PortId;
        public bool HasExplicitPortId => !string.IsNullOrEmpty(m_PortId);

        [SerializeField]
        protected string m_DisplayName;
        public string DisplayName => string.IsNullOrEmpty(m_DisplayName) ? m_Name : m_DisplayName;

        [SerializeField]
        protected string m_OwnerModuleId;
        public string OwnerModuleId => m_OwnerModuleId;

        [SerializeField]
        protected string m_FieldKey;
        public string FieldKey => m_FieldKey;

        [SerializeField]
        protected int m_Index = -1;
        public int Index { get => m_Index; set => m_Index = value; }

        [SerializeField]
        protected bool m_Expanded;
        public bool Expanded { get => m_Expanded; set => m_Expanded = value; }

        [SerializeField]
        protected PortDirection m_Direction;
        public PortDirection Direction { get => m_Direction; set => m_Direction = value; }

        [SerializeField]
        protected string m_InputEdgeGUID;
        public string InputEdgeGUID => m_InputEdgeGUID;

        [SerializeField]
        protected List<string> m_OutputEdgeGUIDs = new List<string>();
        public List<string> OutputEdgeGUIDs => m_OutputEdgeGUIDs;

        [NonSerialized]
        protected BaseNode m_Owner;
        public BaseNode Owner => m_Owner;

        [NonSerialized]
        protected PropertyPort m_SourcePort;
        public PropertyPort SourcePort => m_SourcePort;

        [NonSerialized]
        protected List<PropertyPort> m_TargetPorts = new List<PropertyPort>();
        public List<PropertyPort> TargetPorts => m_TargetPorts;

        public virtual Type ValueType => null;

        public bool InputLinked => !string.IsNullOrEmpty(m_InputEdgeGUID);

        public PropertyPort() { }

        public virtual void ConfigureIdentity(string portId, string fieldKey, string ownerModuleId, string displayName, string legacyName)
        {
            if (!string.IsNullOrEmpty(portId))
                m_PortId = portId;
            if (!string.IsNullOrEmpty(fieldKey))
                m_FieldKey = fieldKey;
            m_OwnerModuleId = ownerModuleId ?? string.Empty;
            if (!string.IsNullOrEmpty(displayName))
                m_DisplayName = displayName;
            if (!string.IsNullOrEmpty(legacyName))
                m_Name = legacyName;
        }

        public virtual void Init(BaseNode node)
        {
            m_Owner = node;
            RebuildPropertyEdgeCache();

            if (!string.IsNullOrEmpty(m_InputEdgeGUID) &&
                m_Owner.Owner.GUIDPropertyEdgeMap.TryGetValue(m_InputEdgeGUID, out PropertyEdge inputEdge))
            {
                m_SourcePort = inputEdge.StartPort;
            }

            m_TargetPorts.Clear();
            m_OutputEdgeGUIDs.ForEach(i =>
            {
                if (m_Owner.Owner.GUIDPropertyEdgeMap.TryGetValue(i, out PropertyEdge outputEdge))
                    m_TargetPorts.Add(outputEdge.EndPort);
            });
        }

        public virtual void RebindReadOnlyViewOwner(BaseNode node)
        {
            m_Owner = node;
            m_SourcePort = null;
            m_TargetPorts.Clear();
        }

        public virtual void Dispose()
        {
            m_Owner = null;
            m_SourcePort = null;
            m_TargetPorts.Clear();
        }
        public virtual void OnAfterDeserialize()
        {
            m_InputEdgeGUID = string.Empty;
            m_OutputEdgeGUIDs.Clear();
            m_Owner = null;
            m_SourcePort = null;
            m_TargetPorts.Clear();
        }

        void RebuildPropertyEdgeCache()
        {
            m_SourcePort = null;
            if (m_OutputEdgeGUIDs == null)
                m_OutputEdgeGUIDs = new List<string>();
            m_OutputEdgeGUIDs.Clear();
            m_InputEdgeGUID = string.Empty;

            if (m_Owner?.Owner == null)
                return;

            foreach (PropertyEdge edge in m_Owner.Owner.PropertyEdges)
            {
                if (edge == null)
                    continue;

                if (m_Direction == PortDirection.Input &&
                    edge.EndNode == m_Owner &&
                    edge.EndPortName == PortId)
                {
                    m_InputEdgeGUID = edge.GUID;
                    continue;
                }

                if (m_Direction == PortDirection.Output &&
                    edge.StartNode == m_Owner &&
                    edge.StartPortName == PortId)
                {
                    m_OutputEdgeGUIDs.Add(edge.GUID);
                }
            }
        }

        public virtual object GetValue()
        {
            return null;
        }
        public virtual void SetValue(object value) { }
        public virtual void GetSourceValue() { }

        public static implicit operator bool(PropertyPort exists) => exists != null;
    }

    [Serializable]
    public partial class PropertyPort<T> : PropertyPort
    {
        [SerializeField]
        protected T m_Value;
        public virtual T Value { get => m_Value; set => m_Value = value; }

        [NonSerialized]
        protected PropertyPort<T> m_SameTypeSourcePropertyPort;
        [NonSerialized]
        protected PropertyPort m_DefferentTypeSourcePropertyPort;
        [NonSerialized]
        protected List<PropertyPort<T>> m_TargetPropertyPorts = new List<PropertyPort<T>>();

        private bool SameTypeSourcePropertyPort;
        public override Type ValueType => typeof(T);

        public override void Init(BaseNode node)
        {
            base.Init(node);
            if (m_SourcePort)
            {
                if (m_SourcePort.ValueType == ValueType)
                {
                    m_SameTypeSourcePropertyPort = m_SourcePort as PropertyPort<T>;
                    SameTypeSourcePropertyPort = true;
                }
                else
                    m_DefferentTypeSourcePropertyPort = m_SourcePort;
            }

            m_TargetPropertyPorts.Clear();
            m_TargetPorts.ForEach(i => m_TargetPropertyPorts.Add(i as PropertyPort<T>));
        }
        public override void Dispose()
        {
            base.Dispose();
            SameTypeSourcePropertyPort = false;
            m_SameTypeSourcePropertyPort = null;
            m_DefferentTypeSourcePropertyPort = null;
            m_TargetPropertyPorts.Clear();
        }
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_SameTypeSourcePropertyPort = null;
            m_DefferentTypeSourcePropertyPort = null;
            m_TargetPropertyPorts.Clear();
        }
        public override object GetValue()
        {
            return m_Value;
        }
        public override void SetValue(object value)
        {
            m_Value = (T)value;
        }
        public override void GetSourceValue()
        {
            if (SameTypeSourcePropertyPort)
                m_Value = m_SameTypeSourcePropertyPort.Value;

            if (m_DefferentTypeSourcePropertyPort)
                m_Value = GetDefferentTypeSourceValue(m_DefferentTypeSourcePropertyPort.GetValue());
        }

        public virtual T GetDefferentTypeSourceValue(object value)
        {
            return (T)value;
        }
    }

    [Serializable]
    [PropertyColor(210, 210, 210)]
    public class objectPropertyPort : PropertyPort<object>
    {
        public objectPropertyPort() { }
    }

    [Serializable]
    [PropertyColor(210, 210, 210)]
    public class BoolPropertyPort : PropertyPort<bool>
    {
        public BoolPropertyPort() { }
    }

    [Serializable]
    [PropertyColor(148, 129, 230)]
    public class IntPropertyPort : PropertyPort<int>
    {
        public IntPropertyPort() { }
    }

    [Serializable]
    [PropertyColor(132, 228, 231)]
    [CompatiblePorts(typeof(int))]
    public class FloatPropertyPort : PropertyPort<float>
    {
        public FloatPropertyPort() { }
        public override float GetDefferentTypeSourceValue(object value)
        {
            if (value is int a)
                return a;
            else
                return 0;
        }
    }

    [Serializable]
    [PropertyColor(252, 218, 110)]
    public class StringPropertyPort : PropertyPort<string>
    {
        public StringPropertyPort() { }
    }

    [Serializable]
    [PropertyColor(246, 255, 154)]
    public class Vector3PropertyPort : PropertyPort<Vector3>
    {
        public Vector3PropertyPort() { }
    }

    [Serializable]
    [PropertyColor(154, 239, 146)]
    public class Vector2PropertyPort : PropertyPort<Vector2>
    {
        public Vector2PropertyPort() { }
    }

    [Serializable]
    [PropertyColor(154, 239, 146)]
    public class TransformProperty : PropertyPort<Transform>
    {
        public TransformProperty() { }
    }


    [Serializable]
    [PropertyColor(148, 129, 230)]
    public class IntListPropertyPort : PropertyPort<List<int>>
    {
        public IntListPropertyPort() { }
    }

    [Serializable]
    [PropertyColor(132, 228, 231)]
    public class FloatListPropertyPort : PropertyPort<List<float>>
    {
        public FloatListPropertyPort() { }
    }

    [Serializable]
    [PropertyColor(252, 218, 110)]
    public class StringListPropertyPort : PropertyPort<List<string>>
    {
        public StringListPropertyPort() { }
    }

    [Serializable]
    [PropertyColor(252, 218, 110)]
    public class AnimationCurvePropertyPort : PropertyPort<AnimationCurve>
    {
        public AnimationCurvePropertyPort() { }
    }

    [Serializable]
    [PropertyColor(239, 163, 146)]
    public class TreePropertyPort : PropertyPort<BaseTree>
    {
        public TreePropertyPort() { }
    }
}
