using System;
using System.Collections.Generic;
using BTSMTL.Authoring.Graph;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Authoring.Editor
{
    public sealed class GraphAuthoringStateMachineBinding
    {
        public GraphAuthoringStateMachineBinding(
            IGraphAuthoringStateMachineProjection document,
            GraphAuthoringCapabilityCatalog capabilities,
            IGraphAuthoringDomainMutation mutation,
            IGraphAuthoringStateMachinePolicy policy)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
            Mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            if (document.SemanticKind != policy.SemanticKind)
                throw new InvalidOperationException($"StateMachine semantic '{document.SemanticKind}' cannot use policy '{policy.SemanticKind}'.");
            GraphAuthoringStateMachineProjectionValidator.RequireValid(document);
            policy.ValidateDocument(document);
        }

        public IGraphAuthoringStateMachineProjection Document { get; }
        public GraphAuthoringCapabilityCatalog Capabilities { get; }
        public IGraphAuthoringDomainMutation Mutation { get; }
        public IGraphAuthoringStateMachinePolicy Policy { get; }
    }

    public sealed class GraphAuthoringPageStack
    {
        readonly List<GraphAuthoringPageProjection> m_Pages = new List<GraphAuthoringPageProjection>();

        public IReadOnlyList<GraphAuthoringPageProjection> Pages => m_Pages;
        public GraphAuthoringPageProjection Current => m_Pages.Count == 0 ? default : m_Pages[m_Pages.Count - 1];

        public void Reset(GraphAuthoringPageProjection root)
        {
            m_Pages.Clear();
            m_Pages.Add(root);
        }

        public void Push(GraphAuthoringPageProjection page)
        {
            int existing = m_Pages.FindIndex(value => value.PageId.Equals(page.PageId));
            if (existing >= 0)
                m_Pages.RemoveRange(existing, m_Pages.Count - existing);
            m_Pages.Add(page);
        }

        public bool Pop()
        {
            if (m_Pages.Count <= 1)
                return false;
            m_Pages.RemoveAt(m_Pages.Count - 1);
            return true;
        }

        public void NavigateTo(int index)
        {
            if (index < 0 || index >= m_Pages.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index + 1 < m_Pages.Count)
                m_Pages.RemoveRange(index + 1, m_Pages.Count - index - 1);
        }
    }
}
