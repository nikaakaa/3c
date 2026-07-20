using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TreeBaseTreeView = TreeDesigner.Editor.BaseTreeView;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class CharacterGraphDataCatalogRegistration
    {
        static CharacterGraphDataCatalogRegistration()
        {
            GraphDataCatalogSourceRegistry.Register(new CharacterInputGraphDataCatalogSourceProvider());
            BlackboardGraphDataNodeFactoryRegistry.Register(new CharacterPipelineBlackboardNodeFactory());
        }
    }

    sealed class CharacterInputGraphDataCatalogSourceProvider : IGraphDataCatalogSourceProvider
    {
        public int Order => 100;

        public IGraphDataCatalogSource CreateSource()
        {
            return new CharacterInputGraphDataCatalogSource();
        }
    }

    sealed class CharacterInputGraphDataCatalogSource : IGraphDataCatalogSource
    {
        static readonly Color s_BoolColor = new Color(0.42f, 0.72f, 0.48f);
        static readonly Color s_FloatColor = new Color(0.43f, 0.72f, 0.72f);
        static readonly Color s_VectorColor = new Color(0.39f, 0.61f, 0.82f);
        static readonly Color s_RequestColor = new Color(0.76f, 0.57f, 0.43f);
        static readonly Color s_StatusColor = new Color(0.35f, 0.35f, 0.35f);

        public CharacterInputGraphDataCatalogSource()
        {
            EditorApplication.projectChanged += OnProjectChanged;
        }

        public event Action Changed;
        public int Order => 100;
        public GraphDataCatalogSourceKind Kind => GraphDataCatalogSourceKind.Input;
        public string DisplayName => "Input";

        public IEnumerable<GraphDataCatalogEntry> GetEntries(GraphDataCatalogContext context)
        {
            if (!(context?.AuthoringContext is CharacterPipelineAuthoringContext authoringContext) || !authoringContext.Definition)
            {
                yield return CreateStatus(context, "Missing CharacterPipelineDefinition context.");
                yield break;
            }

            CharacterInputProfile profile = authoringContext.InputProfile;
            if (!profile)
            {
                yield return CreateStatus(context, "InputProfile is missing on CharacterPipelineDefinition.");
                yield break;
            }

            if (profile.InputValues.Count == 0)
            {
                yield return CreateStatus(context, "No input values.", "Input/Values", "input:values:empty");
            }
            else
            {
                for (int i = 0; i < profile.InputValues.Count; i++)
                {
                    CharacterInputValueDefinition inputValue = profile.InputValues[i];
                    if (inputValue == null)
                        continue;
                    yield return CreateInputValueEntry(context, profile, inputValue, i);
                }
            }

            if (profile.ActionRequests.Count == 0)
            {
                yield return CreateStatus(context, "No action requests.", "Input/Requests", "input:requests:empty");
            }
            else
            {
                for (int i = 0; i < profile.ActionRequests.Count; i++)
                {
                    CharacterActionRequestDefinition request = profile.ActionRequests[i];
                    if (request == null)
                        continue;
                    yield return CreateRequestEntry(context, profile, request, i);
                }
            }
        }

        public VisualElement CreateDetails(GraphDataCatalogEntry entry, GraphDataCatalogContext context, Action requestRefresh)
        {
            CharacterInputProfile profile = ResolveProfile(context);
            VisualElement details = GraphDataCatalogDetails.CreateContainer();
            GraphDataCatalogDetails.AddRow(details, "Profile", profile ? profile.name : "Missing");
            GraphDataCatalogDetails.AddRow(details, "Source Asset", profile && profile.SourceAsset ? profile.SourceAsset.name : "Missing");

            if (entry.Payload is CharacterInputValueDefinition inputValue)
            {
                GraphDataCatalogDetails.AddRow(details, "Input Value ID", inputValue.InputValueId);
                GraphDataCatalogDetails.AddRow(details, "Value Type", inputValue.ValueType.ToString());
                GraphDataCatalogDetails.AddRow(details, "Input Action", ResolveActionLabel(inputValue.SourceAction));
            }
            else if (entry.Payload is CharacterActionRequestDefinition request)
            {
                GraphDataCatalogDetails.AddRow(details, "Request ID", request.RequestId);
                GraphDataCatalogDetails.AddRow(details, "Input Action", ResolveActionLabel(request.SourceAction));
                GraphDataCatalogDetails.AddRow(details, "Buffer Seconds", request.BufferSeconds.ToString("0.###"));
                GraphDataCatalogDetails.AddRow(details, "Priority", request.Priority.ToString());
            }
            return details;
        }

        public bool CanCreateNode(GraphDataCatalogEntry entry, GraphDataCatalogContext context, TreeBaseTreeView treeView, out string reason)
        {
            if (entry == null || entry.ContextGeneration != context?.Generation || treeView?.Tree != context?.Tree)
            {
                reason = "Graph context changed. Refresh the catalog entry.";
                return false;
            }

            if (entry.Payload is CharacterInputValueDefinition inputValue)
            {
                bool canCreate = CharacterInputInfoNodeFactory.CanCreate(treeView.Tree, inputValue);
                reason = canCreate ? string.Empty : $"{treeView.Tree.GetType().Name} does not accept {inputValue.ValueType} input value nodes.";
                return canCreate;
            }

            if (entry.Payload is CharacterActionRequestDefinition request)
            {
                bool canCreate = CharacterInputInfoNodeFactory.CanCreate(treeView.Tree, request);
                reason = canCreate ? string.Empty : $"{treeView.Tree.GetType().Name} does not accept action request nodes.";
                return canCreate;
            }

            reason = "Input definition is unavailable.";
            return false;
        }

        public bool TryCreateNode(
            GraphDataCatalogEntry entry,
            GraphDataCatalogContext context,
            TreeBaseTreeView treeView,
            Vector2 position,
            out string error)
        {
            if (!CanCreateNode(entry, context, treeView, out error))
                return false;

            BaseNode node = entry.Payload is CharacterInputValueDefinition inputValue
                ? CharacterInputInfoNodeFactory.Create(treeView, inputValue, position)
                : CharacterInputInfoNodeFactory.Create(treeView, entry.Payload as CharacterActionRequestDefinition, position);
            if (node != null)
                return true;

            error = "The input node factory did not create a node.";
            return false;
        }

        public bool TryDelete(GraphDataCatalogEntry entry, GraphDataCatalogContext context, out string error)
        {
            error = "Input definitions are read only in Graph Data Catalog.";
            return false;
        }

        public void Locate(GraphDataCatalogEntry entry, GraphDataCatalogContext context)
        {
            CharacterInputProfile profile = ResolveProfile(context);
            if (!profile)
                return;

            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }

        public void Dispose()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        void OnProjectChanged()
        {
            Changed?.Invoke();
        }

        GraphDataCatalogEntry CreateInputValueEntry(
            GraphDataCatalogContext context,
            CharacterInputProfile profile,
            CharacterInputValueDefinition inputValue,
            int index)
        {
            GraphDataCatalogCapability capabilities = GraphDataCatalogCapability.ExpandDetails |
                                                      GraphDataCatalogCapability.LocateSource;
            string unavailableReason = string.Empty;
            if (CharacterInputInfoNodeFactory.CanCreate(context.Tree, inputValue))
                capabilities |= GraphDataCatalogCapability.DragCreateNode;
            else
                unavailableReason = $"{context.Tree?.GetType().Name ?? "Graph"} does not accept {inputValue.ValueType} input value nodes.";

            string id = string.IsNullOrEmpty(inputValue.InputValueId) ? $"missing:{index}" : inputValue.InputValueId;
            return new GraphDataCatalogEntry(
                this,
                $"input:{ProfileIdentity(profile)}:value:{id}",
                GraphDataCatalogEntryKind.InputValue,
                string.IsNullOrEmpty(inputValue.InputValueId) ? "<missing>" : inputValue.InputValueId,
                inputValue.ValueType.ToString(),
                "Input/Values",
                GraphDataCatalogOwnership.External,
                DisplayName,
                profile.name,
                InputColor(inputValue.ValueType),
                capabilities,
                inputValue,
                context.Generation,
                unavailableReason,
                ResolveActionLabel(inputValue.SourceAction));
        }

        GraphDataCatalogEntry CreateRequestEntry(
            GraphDataCatalogContext context,
            CharacterInputProfile profile,
            CharacterActionRequestDefinition request,
            int index)
        {
            GraphDataCatalogCapability capabilities = GraphDataCatalogCapability.ExpandDetails |
                                                      GraphDataCatalogCapability.LocateSource;
            string unavailableReason = string.Empty;
            if (CharacterInputInfoNodeFactory.CanCreate(context.Tree, request))
                capabilities |= GraphDataCatalogCapability.DragCreateNode;
            else
                unavailableReason = $"{context.Tree?.GetType().Name ?? "Graph"} does not accept action request nodes.";

            string id = string.IsNullOrEmpty(request.RequestId) ? $"missing:{index}" : request.RequestId;
            return new GraphDataCatalogEntry(
                this,
                $"input:{ProfileIdentity(profile)}:request:{id}",
                GraphDataCatalogEntryKind.ActionRequest,
                string.IsNullOrEmpty(request.RequestId) ? "<missing>" : request.RequestId,
                "Request",
                "Input/Requests",
                GraphDataCatalogOwnership.External,
                DisplayName,
                profile.name,
                s_RequestColor,
                capabilities,
                request,
                context.Generation,
                unavailableReason,
                ResolveActionLabel(request.SourceAction));
        }

        GraphDataCatalogEntry CreateStatus(
            GraphDataCatalogContext context,
            string message,
            string groupPath = "Input",
            string stableId = "input:status")
        {
            return new GraphDataCatalogEntry(
                this,
                stableId,
                GraphDataCatalogEntryKind.Status,
                message,
                string.Empty,
                groupPath,
                GraphDataCatalogOwnership.External,
                DisplayName,
                string.Empty,
                s_StatusColor,
                GraphDataCatalogCapability.None,
                null,
                context?.Generation ?? 0,
                message);
        }

        static CharacterInputProfile ResolveProfile(GraphDataCatalogContext context)
        {
            return (context?.AuthoringContext as CharacterPipelineAuthoringContext)?.InputProfile;
        }

        static string ProfileIdentity(CharacterInputProfile profile)
        {
            return profile ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile)) : string.Empty;
        }

        static string ResolveActionLabel(UnityEngine.InputSystem.InputActionReference reference)
        {
            UnityEngine.InputSystem.InputAction action = reference?.action;
            if (action == null)
                return "Missing";
            return action.actionMap == null ? action.name : $"{action.actionMap.name}/{action.name}";
        }

        static Color InputColor(CharacterInputValueType valueType)
        {
            switch (valueType)
            {
                case CharacterInputValueType.Bool:
                    return s_BoolColor;
                case CharacterInputValueType.Float:
                    return s_FloatColor;
                default:
                    return s_VectorColor;
            }
        }
    }

    sealed class CharacterPipelineBlackboardNodeFactory : IBlackboardGraphDataNodeFactory
    {
        static readonly Dictionary<Type, Type> s_NodeTypes = new Dictionary<Type, Type>
        {
            { typeof(bool), typeof(PipelineBlackboardBoolInfoNode) },
            { typeof(int), typeof(PipelineBlackboardIntInfoNode) },
            { typeof(float), typeof(PipelineBlackboardFloatInfoNode) },
            { typeof(string), typeof(PipelineBlackboardStringInfoNode) },
            { typeof(Vector2), typeof(PipelineBlackboardVector2InfoNode) },
            { typeof(Vector3), typeof(PipelineBlackboardVector3InfoNode) }
        };

        public int Order => 100;

        public bool CanCreate(GraphDataCatalogContext context, BaseExposedProperty declaration, BaseTree tree)
        {
            if (context?.AuthoringContext is CharacterPipelineAuthoringContext &&
                declaration?.BlackboardFactProjection == PipelineBlackboardFactProjectionKind.ActionWindow)
                return declaration.ValueType == typeof(bool) &&
                       !string.IsNullOrWhiteSpace(declaration.ActionWindowType) &&
                       tree != null &&
                       tree.CanCreateNodeType(typeof(ActionWindowActiveInfoNode));

            return context?.AuthoringContext is CharacterPipelineAuthoringContext &&
                   declaration?.ValueType != null &&
                   tree != null &&
                   s_NodeTypes.TryGetValue(declaration.ValueType, out Type nodeType) &&
                   tree.CanCreateNodeType(nodeType);
        }

        public bool TryCreate(TreeBaseTreeView treeView, BaseExposedProperty declaration, Vector2 position, out string error)
        {
            error = string.Empty;
            if (declaration?.BlackboardFactProjection == PipelineBlackboardFactProjectionKind.ActionWindow)
            {
                if (treeView?.Tree == null || declaration.ValueType != typeof(bool) ||
                    string.IsNullOrWhiteSpace(declaration.ActionWindowType) ||
                    !treeView.Tree.CanCreateNodeType(typeof(ActionWindowActiveInfoNode)))
                {
                    error = "The current graph does not accept this typed ActionWindow query.";
                    return false;
                }

                ActionWindowActiveInfoNode windowNode = treeView.CreateNode(typeof(ActionWindowActiveInfoNode), position) as ActionWindowActiveInfoNode;
                if (windowNode == null)
                {
                    error = "Could not create ActionWindowActiveInfoNode.";
                    return false;
                }
                windowNode.ApplyModify("Bind ActionWindow Type", () => windowNode.ConfigureAuthoring(declaration.ActionWindowType));
                BaseNodeView windowView = treeView.FindNodeView(windowNode);
                if (windowView != null)
                {
                    treeView.ClearSelection();
                    treeView.AddToSelection(windowView);
                }
                return true;
            }

            if (treeView?.Tree == null || declaration?.ValueType == null ||
                !s_NodeTypes.TryGetValue(declaration.ValueType, out Type nodeType) ||
                !treeView.Tree.CanCreateNodeType(nodeType))
            {
                error = "The current graph does not accept this pipeline blackboard value node.";
                return false;
            }

            PipelineBlackboardValueInfoNode node = treeView.CreateNode(nodeType, position) as PipelineBlackboardValueInfoNode;
            if (node == null)
            {
                error = $"Could not create {nodeType.Name}.";
                return false;
            }

            node.ApplyModify("Bind Pipeline Blackboard Declaration", () => node.ConfigureAuthoring(declaration));
            BaseNodeView nodeView = treeView.FindNodeView(node);
            if (nodeView != null)
            {
                treeView.ClearSelection();
                treeView.AddToSelection(nodeView);
            }
            return true;
        }
    }
}
