using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public partial class BaseExposedProperty
    {
        [SerializeField]
        protected string m_GUID;
        public string GUID { get => m_GUID; set => m_GUID = value; }

        [SerializeField]
        protected string m_Name;
        public string Name { get => m_Name; set => m_Name = value; }

        [NonSerialized]
        protected BaseGraph m_Owner;
        public BaseGraph Owner => m_Owner;

        public virtual Type ValueType => null;

        public BaseExposedProperty() { }

        public virtual void Init(BaseGraph tree)
        {
            m_Owner = tree;
        }
        public virtual void Dispose()
        {
            m_Owner = null;
        }
        public virtual object GetValue()
        {
            return null;
        }
        public virtual void SetValue(object value) { }

        public static implicit operator bool(BaseExposedProperty exists) => exists != null;
    }

    [Serializable]
    public abstract class BaseExposedProperty<T> : BaseExposedProperty
    {
        [SerializeField]
        protected T m_Value;
        public T Value { get => m_Value; set => m_Value = value; }

        public override Type ValueType => typeof(T);

        public override object GetValue()
        {
            return m_Value;
        }
        public override void SetValue(object value)
        {
            m_Value = (T)value;
        }
    }

    [Serializable]
    [PropertyColor(210, 210, 210)]
    public class BoolExposedProperty : BaseExposedProperty<bool>
    {
        public BoolExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(148, 129, 230)]
    public class IntExposedProperty : BaseExposedProperty<int>
    {
        public IntExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(132, 228, 231)]
    public class FloatExposedProperty : BaseExposedProperty<float>
    {
        public FloatExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(252, 218, 110)]
    public class StringExposedProperty : BaseExposedProperty<string>
    {
        public StringExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(246, 255, 154)]
    public class Vector3ExposedProperty : BaseExposedProperty<Vector3>
    {
        public Vector3ExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(154, 239, 146)]
    public class Vector2ExposedProperty : BaseExposedProperty<Vector2>
    {
        public Vector2ExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(132, 228, 231)]
    public class FloatListExposedProperty : BaseExposedProperty<List<float>>
    {
        public FloatListExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(252, 218, 110)]
    public class StringListExposedProperty : BaseExposedProperty<List<string>>
    {
        public StringListExposedProperty() { }
    }

    [Serializable]
    [PropertyColor(252, 218, 110)]
    public class AnimationCurveExposedProperty : BaseExposedProperty<AnimationCurve>
    {
        public AnimationCurveExposedProperty() { }
    }
}
