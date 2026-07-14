using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace TreeDesigner.Editor
{
    public class PropertyEdgeView : BaseEdgeView
    {
        public PropertyEdge PropertyEdge => m_Edge as PropertyEdge;
        public PropertyPortView StartPropertyPortView => StartPortView as PropertyPortView;
        public PropertyPortView EndPropertyPortView => EndPortView as PropertyPortView;
    }
}