using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using BTSMTL.Editor;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;
using TreeDesigner.Editor;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterPipelineAuthoringContext : ITreeInspectorBlackboardAuthoringContext
    {
        public CharacterPipelineAuthoringContext(CharacterPipelineDefinition definition)
        {
            Definition = definition;
        }

        public CharacterPipelineDefinition Definition { get; }
        public CharacterInputProfile InputProfile => Definition ? Definition.InputProfile : null;
        BaseTree RootTree => Definition && Definition.RootTreeAsset ? Definition.RootTreeAsset.Tree : null;

        public IReadOnlyList<PipelineBlackboardVariableScope> GetAllowedBlackboardScopes(BaseTree currentTree)
        {
            List<PipelineBlackboardVariableScope> scopes = new List<PipelineBlackboardVariableScope>
            {
                PipelineBlackboardVariableScope.Graph,
                PipelineBlackboardVariableScope.Frame
            };

            if (RootTree == currentTree)
                scopes.Insert(0, PipelineBlackboardVariableScope.Character);

            if (currentTree is StateBehaviorSubTree)
            {
                scopes.Add(PipelineBlackboardVariableScope.State);
                if (currentTree.Nodes.Any(i => i is ActivateActionInstanceNode))
                    scopes.Add(PipelineBlackboardVariableScope.ActionInstance);
            }

            return scopes;
        }

        public IEnumerable<BaseTree> GetAdditionalVisibleBlackboardSources(BaseTree currentTree)
        {
            if (RootTree && RootTree != currentTree)
                yield return RootTree;
        }
    }

    public sealed class PipelineBlackboardValueNodeView : BaseNodeView
    {
        readonly PipelineBlackboardValueInfoNode m_BlackboardNode;
        readonly Label m_VariableLabel;

        public PipelineBlackboardValueNodeView(BaseNode node, BaseTreeWindow treeWindow) : base(node, treeWindow)
        {
            m_BlackboardNode = node as PipelineBlackboardValueInfoNode;
            m_VariableLabel = new Label();
            m_VariableLabel.tooltip = "Select pipeline blackboard variable";
            m_VariableLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            m_VariableLabel.style.flexGrow = 1f;
            m_VariableLabel.AddManipulator(new DropdownMenuManipulator(BuildVariableMenu, MouseButton.LeftMouse));
            titleContainer.Add(m_VariableLabel);
            RefreshVariableLabel();
        }

        void BuildVariableMenu(DropdownMenu menu)
        {
            foreach (BaseExposedProperty declaration in m_TreeWindow.GetVisibleExposedProperties()
                         .Where(i => i.ValueType == m_BlackboardNode.BlackboardValueType)
                         .OrderBy(i => i.Owner == m_Node.Owner ? 0 : 1)
                         .ThenBy(i => i.BlackboardCategoryPath)
                         .ThenBy(i => i.Index))
            {
                string source = declaration.Owner == m_Node.Owner ? "Local" : $"Inherited/{declaration.Owner?.name}";
                string category = string.IsNullOrEmpty(declaration.BlackboardCategoryPath)
                    ? string.Empty
                    : $"/{declaration.BlackboardCategoryPath}";
                menu.AppendAction($"{source}{category}/{declaration.BlackboardKey}", _ =>
                {
                    m_Node.ApplyModify("Bind Pipeline Blackboard Variable", () =>
                    {
                        m_BlackboardNode.ConfigureAuthoring(declaration);
                        m_Node.GetNewSerializedTree();
                        RefreshVariableLabel();
                    });
                }, _ => declaration.DeclarationId == m_BlackboardNode.BlackboardVariable.DeclarationId
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);
            }
        }

        void RefreshVariableLabel()
        {
            PipelineBlackboardVariableReference reference = m_BlackboardNode.BlackboardVariable;
            m_VariableLabel.text = reference.IsValid ? reference.DisplayKey : "Select Variable";
        }
    }
}
