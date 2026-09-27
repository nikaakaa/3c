using BTSMTL.Authoring.Graph;
using UnityEngine.UIElements;

namespace BTSMTL.Authoring.Editor
{
    public interface IGraphAuthoringReadOnlyPanel
    {
        VisualElement View { get; }
        void Bind(IGraphAuthoringDocumentProjection document);
        void Refresh();
        void Unbind();
    }
}
