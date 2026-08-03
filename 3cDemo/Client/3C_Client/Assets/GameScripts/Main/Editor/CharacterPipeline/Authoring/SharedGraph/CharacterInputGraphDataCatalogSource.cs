using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
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
            GraphDataCatalogSourceRegistry.Register(
                new CharacterInputGraphDataCatalogSourceProvider());
        }
    }

    sealed class CharacterInputGraphDataCatalogSourceProvider :
        IGraphDataCatalogSourceProvider
    {
        public int Order => 100;

        public IGraphDataCatalogSource CreateSource()
        {
            return new CharacterInputGraphDataCatalogSource();
        }
    }

    sealed class CharacterInputGraphDataCatalogSource :
        IGraphDataCatalogSource
    {
        sealed class InputValueNodeCreationPayload :
            IBtsmtlNodeCreationPayload
        {
            readonly string m_InputValueId;

            public InputValueNodeCreationPayload(
                Type nodeType,
                string inputValueId)
            {
                NodeType = nodeType ??
                           throw new ArgumentNullException(nameof(nodeType));
                m_InputValueId = string.IsNullOrWhiteSpace(inputValueId)
                    ? throw new ArgumentException(
                        "Input value identity is missing.",
                        nameof(inputValueId))
                    : inputValueId;
            }

            public Type NodeType { get; }

            public void Configure(BaseNode node)
            {
                if (!(node is CharacterInputValueInfoNode input))
                {
                    throw new InvalidOperationException(
                        $"Input value payload cannot configure '{node?.GetType().FullName}'.");
                }
                input.BindInputValue(m_InputValueId);
            }
        }

        sealed class ActionRequestNodeCreationPayload :
            IBtsmtlNodeCreationPayload
        {
            readonly string m_RequestId;

            public ActionRequestNodeCreationPayload(
                Type nodeType,
                string requestId)
            {
                NodeType = nodeType ??
                           throw new ArgumentNullException(nameof(nodeType));
                m_RequestId = string.IsNullOrWhiteSpace(requestId)
                    ? throw new ArgumentException(
                        "Action request identity is missing.",
                        nameof(requestId))
                    : requestId;
            }

            public Type NodeType { get; }

            public void Configure(BaseNode node)
            {
                if (!(node is CharacterActionRequestInfoNode request))
                {
                    throw new InvalidOperationException(
                        $"Action request payload cannot configure '{node?.GetType().FullName}'.");
                }
                request.BindActionRequest(m_RequestId);
            }
        }

        static readonly Color s_BoolColor =
            new Color(0.42f, 0.72f, 0.48f);
        static readonly Color s_FloatColor =
            new Color(0.43f, 0.72f, 0.72f);
        static readonly Color s_VectorColor =
            new Color(0.39f, 0.61f, 0.82f);
        static readonly Color s_RequestColor =
            new Color(0.76f, 0.57f, 0.43f);
        static readonly Color s_StatusColor =
            new Color(0.35f, 0.35f, 0.35f);
        readonly BtsmtlGraphAuthoringCapabilities m_Capabilities =
            new BtsmtlGraphAuthoringCapabilities();

        public CharacterInputGraphDataCatalogSource()
        {
            EditorApplication.projectChanged += OnProjectChanged;
        }

        public event Action Changed;
        public int Order => 100;
        public GraphDataCatalogSourceKind Kind =>
            GraphDataCatalogSourceKind.Input;
        public string DisplayName => "Input";

        public IEnumerable<GraphDataCatalogEntry> GetEntries(
            GraphDataCatalogContext context)
        {
            if (!(context?.AuthoringContext is
                    CharacterPipelineAuthoringContext authoringContext) ||
                !authoringContext.Definition)
            {
                yield return CreateStatus(
                    context,
                    "Missing CharacterPipelineDefinition context.");
                yield break;
            }

            CharacterInputProfile profile = authoringContext.InputProfile;
            if (!profile)
            {
                yield return CreateStatus(
                    context,
                    "InputProfile is missing on CharacterPipelineDefinition.");
                yield break;
            }

            if (profile.InputValues.Count == 0)
            {
                yield return CreateStatus(
                    context,
                    "No input values.",
                    "Input/Values",
                    "input:values:empty");
            }
            else
            {
                for (int i = 0; i < profile.InputValues.Count; i++)
                {
                    CharacterInputValueDefinition inputValue =
                        profile.InputValues[i];
                    if (inputValue != null)
                    {
                        yield return CreateInputValueEntry(
                            context,
                            profile,
                            inputValue,
                            i);
                    }
                }
            }

            if (profile.ActionRequests.Count == 0)
            {
                yield return CreateStatus(
                    context,
                    "No action requests.",
                    "Input/Requests",
                    "input:requests:empty");
            }
            else
            {
                for (int i = 0; i < profile.ActionRequests.Count; i++)
                {
                    CharacterActionRequestDefinition request =
                        profile.ActionRequests[i];
                    if (request != null)
                    {
                        yield return CreateRequestEntry(
                            context,
                            profile,
                            request,
                            i);
                    }
                }
            }
        }

        public VisualElement CreateDetails(
            GraphDataCatalogEntry entry,
            GraphDataCatalogContext context,
            Action requestRefresh)
        {
            CharacterInputProfile profile = ResolveProfile(context);
            VisualElement details =
                GraphDataCatalogDetails.CreateContainer();
            GraphDataCatalogDetails.AddRow(
                details,
                "Profile",
                profile ? profile.name : "Missing");
            GraphDataCatalogDetails.AddRow(
                details,
                "Source Asset",
                profile && profile.SourceAsset
                    ? profile.SourceAsset.name
                    : "Missing");

            if (entry.Payload is CharacterInputValueDefinition inputValue)
            {
                GraphDataCatalogDetails.AddRow(
                    details,
                    "Input Value ID",
                    inputValue.InputValueId);
                GraphDataCatalogDetails.AddRow(
                    details,
                    "Value Type",
                    inputValue.ValueType.ToString());
                GraphDataCatalogDetails.AddRow(
                    details,
                    "Input Action",
                    ResolveActionLabel(inputValue.SourceAction));
            }
            else if (entry.Payload is
                     CharacterActionRequestDefinition request)
            {
                GraphDataCatalogDetails.AddRow(
                    details,
                    "Request ID",
                    request.RequestId);
                GraphDataCatalogDetails.AddRow(
                    details,
                    "Input Action",
                    ResolveActionLabel(request.SourceAction));
                GraphDataCatalogDetails.AddRow(
                    details,
                    "Buffer Seconds",
                    request.BufferSeconds.ToString("0.###"));
                GraphDataCatalogDetails.AddRow(
                    details,
                    "Priority",
                    request.Priority.ToString());
                GraphDataCatalogDetails.AddRow(
                    details,
                    "Timing Class",
                    request.TimingClass.ToString());
            }
            return details;
        }

        public bool CanCreateNode(
            GraphDataCatalogEntry entry,
            GraphDataCatalogContext context,
            TreeBaseTreeView treeView,
            out string reason)
        {
            if (entry == null ||
                entry.ContextGeneration != context?.Generation ||
                treeView?.Tree != context?.Tree)
            {
                reason =
                    "Graph context changed. Refresh the catalog entry.";
                return false;
            }

            if (!TryResolveNodeCapability(
                    entry.Payload,
                    out GraphAuthoringCapabilityDescriptor descriptor))
            {
                reason =
                    "Input definition has no registered BTSMTL capability.";
                return false;
            }

            GraphAuthoringDocumentRoleId documentRole =
                BtsmtlGraphAuthoringCapabilities.SharedRoleId(
                    treeView.Tree);
            if (!descriptor.DomainId.Equals(
                    BtsmtlGraphAuthoringCapabilities.SharedDomain) ||
                !descriptor.Allows(documentRole))
            {
                reason =
                    $"{descriptor.DisplayName} is not available in {documentRole.Value}.";
                return false;
            }

            if (!treeView.Tree.CanCreateNodeType(
                    descriptor.AuthoringType))
            {
                reason =
                    $"{treeView.Tree.GetType().Name} does not accept {descriptor.AuthoringType.Name}.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public bool TryCreateNode(
            GraphDataCatalogEntry entry,
            GraphDataCatalogContext context,
            TreeBaseTreeView treeView,
            Vector2 position,
            out string error)
        {
            if (!CanCreateNode(
                    entry,
                    context,
                    treeView,
                    out error) ||
                !TryResolveNodeCapability(
                    entry.Payload,
                    out GraphAuthoringCapabilityDescriptor descriptor))
            {
                return false;
            }
            Type nodeType = descriptor.AuthoringType;

            IBtsmtlNodeCreationPayload payload;
            if (entry.Payload is CharacterInputValueDefinition inputValue)
            {
                payload = new InputValueNodeCreationPayload(
                    nodeType,
                    inputValue.InputValueId);
            }
            else if (entry.Payload is
                     CharacterActionRequestDefinition request)
            {
                payload = new ActionRequestNodeCreationPayload(
                    nodeType,
                    request.RequestId);
            }
            else
            {
                error = "Input definition is unavailable.";
                return false;
            }

            BaseNode node =
                treeView.CreateNode(nodeType, position, payload);
            if (node == null)
            {
                error =
                    "The shared BTSMTL mutation did not create the input node.";
                return false;
            }

            BaseNodeView nodeView = treeView.FindNodeView(node);
            if (nodeView != null)
            {
                treeView.ClearSelection();
                treeView.AddToSelection(nodeView);
            }
            error = string.Empty;
            return true;
        }

        public bool TryDelete(
            GraphDataCatalogEntry entry,
            GraphDataCatalogContext context,
            out string error)
        {
            error =
                "Input definitions are read only in Graph Data Catalog.";
            return false;
        }

        public void Locate(
            GraphDataCatalogEntry entry,
            GraphDataCatalogContext context)
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

        bool TryResolveNodeCapability(
            object payload,
            out GraphAuthoringCapabilityDescriptor descriptor)
        {
            bool resolved;
            descriptor = null;
            if (payload is CharacterInputValueDefinition inputValue)
            {
                resolved =
                    !string.IsNullOrWhiteSpace(inputValue.InputValueId) &&
                    m_Capabilities.TryResolveInputValueCapability(
                        inputValue.ValueType,
                        out descriptor);
            }
            else if (payload is
                     CharacterActionRequestDefinition request)
            {
                resolved =
                    !string.IsNullOrWhiteSpace(request.RequestId) &&
                    m_Capabilities.TryResolveActionRequestCapability(
                        out descriptor);
            }
            else
            {
                descriptor = null;
                resolved = false;
            }
            return resolved && descriptor?.AuthoringType != null;
        }

        GraphDataCatalogEntry CreateInputValueEntry(
            GraphDataCatalogContext context,
            CharacterInputProfile profile,
            CharacterInputValueDefinition inputValue,
            int index)
        {
            GraphDataCatalogCapability capabilities =
                GraphDataCatalogCapability.ExpandDetails |
                GraphDataCatalogCapability.LocateSource;
            string unavailableReason = string.Empty;
            if (CanCreateInContext(
                    context,
                    inputValue))
            {
                capabilities |=
                    GraphDataCatalogCapability.DragCreateNode;
            }
            else
            {
                unavailableReason =
                    $"{context.Tree?.GetType().Name ?? "Graph"} does not accept this input value capability.";
            }

            string id = string.IsNullOrEmpty(inputValue.InputValueId)
                ? $"missing:{index}"
                : inputValue.InputValueId;
            return new GraphDataCatalogEntry(
                this,
                $"input:{ProfileIdentity(profile)}:value:{id}",
                GraphDataCatalogEntryKind.InputValue,
                string.IsNullOrEmpty(inputValue.InputValueId)
                    ? "<missing>"
                    : inputValue.InputValueId,
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
            GraphDataCatalogCapability capabilities =
                GraphDataCatalogCapability.ExpandDetails |
                GraphDataCatalogCapability.LocateSource;
            string unavailableReason = string.Empty;
            if (CanCreateInContext(
                    context,
                    request))
            {
                capabilities |=
                    GraphDataCatalogCapability.DragCreateNode;
            }
            else
            {
                unavailableReason =
                    $"{context.Tree?.GetType().Name ?? "Graph"} does not accept the action request capability.";
            }

            string id = string.IsNullOrEmpty(request.RequestId)
                ? $"missing:{index}"
                : request.RequestId;
            return new GraphDataCatalogEntry(
                this,
                $"input:{ProfileIdentity(profile)}:request:{id}",
                GraphDataCatalogEntryKind.ActionRequest,
                string.IsNullOrEmpty(request.RequestId)
                    ? "<missing>"
                    : request.RequestId,
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

        bool CanCreateInContext(
            GraphDataCatalogContext context,
            object payload)
        {
            if (context?.Tree == null ||
                !TryResolveNodeCapability(
                    payload,
                    out GraphAuthoringCapabilityDescriptor descriptor) ||
                !descriptor.DomainId.Equals(
                    BtsmtlGraphAuthoringCapabilities.SharedDomain) ||
                !descriptor.Allows(
                    BtsmtlGraphAuthoringCapabilities.SharedRoleId(
                        context.Tree)))
            {
                return false;
            }
            return context.Tree.CanCreateNodeType(
                descriptor.AuthoringType);
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

        static CharacterInputProfile ResolveProfile(
            GraphDataCatalogContext context)
        {
            return (context?.AuthoringContext as
                CharacterPipelineAuthoringContext)?.InputProfile;
        }

        static string ProfileIdentity(
            CharacterInputProfile profile)
        {
            return profile
                ? AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(profile))
                : string.Empty;
        }

        static string ResolveActionLabel(
            UnityEngine.InputSystem.InputActionReference reference)
        {
            UnityEngine.InputSystem.InputAction action =
                reference?.action;
            if (action == null)
                return "Missing";
            return action.actionMap == null
                ? action.name
                : $"{action.actionMap.name}/{action.name}";
        }

        static Color InputColor(
            CharacterInputValueType valueType)
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
}
