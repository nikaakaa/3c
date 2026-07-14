using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor 
{
    public abstract class TimelineClipInspectorView : VisualElement
    {
        protected TimelineEditorView EditorView { get; private set; }

        public void Initialize(TimelineEditorView editorView)
        {
            EditorView = editorView;
        }
    }
}
