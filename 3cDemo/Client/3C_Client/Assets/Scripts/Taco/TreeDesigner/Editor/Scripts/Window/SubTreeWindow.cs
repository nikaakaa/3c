using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner.Editor
{
    public class SubTreeWindow : BaseTreeWindow
    {
        protected override Type m_TreeInspectorViewType => typeof(SubTreeInspectorView);
        public SubTree SubTree => Tree as SubTree;
    }
}