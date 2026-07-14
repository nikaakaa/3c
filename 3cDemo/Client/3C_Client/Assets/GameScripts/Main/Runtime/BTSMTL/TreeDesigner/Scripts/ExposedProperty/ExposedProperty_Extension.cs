#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    public partial class BaseExposedProperty
    {
        [SerializeField]
        protected int m_Index;
        public int Index { get => m_Index; set => m_Index = value; }

        [SerializeField]
        protected bool m_Expanded;
        public bool Expanded { get => m_Expanded; set => m_Expanded = value; }

        [SerializeField]
        protected bool m_Internal;
        public bool Internal { get => m_Internal; set => m_Internal = value; }

        [SerializeField]
        protected bool m_ShowOutside;
        public bool ShowOutside { get => m_ShowOutside; set => m_ShowOutside = value; }

        [SerializeField]
        protected bool m_CanEdit;
        public bool CanEdit { get => m_CanEdit; set => m_CanEdit = value; }

        public Action OnRemoved;
        public Action OnNameChanged;
        public Action OnSelected;
        public void ClearEvent()
        {
            OnRemoved = null;
            OnNameChanged = null;
            OnSelected = null;
        }
    }
}
#endif