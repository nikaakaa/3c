using System;
using System.Collections.Generic;
using BTSMTL.Authoring.Graph;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Authoring.Editor
{
    public sealed class GraphAuthoringUndoBinding :
        IDisposable
    {
        Action m_Reload;

        public GraphAuthoringUndoBinding(Action reload)
        {
            m_Reload = reload ??
                throw new ArgumentNullException(nameof(reload));
            Undo.undoRedoPerformed += Reload;
        }

        public void Dispose()
        {
            if (m_Reload == null)
                return;
            Undo.undoRedoPerformed -= Reload;
            m_Reload = null;
        }

        void Reload()
        {
            m_Reload?.Invoke();
        }
    }

    public sealed class GraphAuthoringSelectionBinding :
        IDisposable
    {
        readonly Action m_Publish;
        IVisualElementScheduledItem m_Scheduled;

        public GraphAuthoringSelectionBinding(
            VisualElement schedulerHost,
            Action publish,
            long intervalMilliseconds = 100)
        {
            if (schedulerHost == null)
                throw new ArgumentNullException(
                    nameof(schedulerHost));
            m_Publish = publish ??
                throw new ArgumentNullException(nameof(publish));
            m_Scheduled = schedulerHost.schedule
                .Execute(Publish)
                .Every(intervalMilliseconds);
        }

        public void PublishNow()
        {
            Publish();
        }

        public void Dispose()
        {
            m_Scheduled?.Pause();
            m_Scheduled = null;
        }

        void Publish()
        {
            m_Publish();
        }
    }
}
