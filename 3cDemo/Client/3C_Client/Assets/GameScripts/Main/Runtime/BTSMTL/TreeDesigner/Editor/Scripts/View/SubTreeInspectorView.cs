using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace TreeDesigner.Editor
{
    public class SubTreeInspectorView : BaseTreeInspectorView
    {
        public SubTree SubTree => Tree as SubTree;
    }
}