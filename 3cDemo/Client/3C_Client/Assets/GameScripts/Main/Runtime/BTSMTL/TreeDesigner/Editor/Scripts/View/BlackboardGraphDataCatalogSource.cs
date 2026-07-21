using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public interface ITreeInspectorBlackboardAuthoringContext
    {
        IReadOnlyList<PipelineBlackboardVariableScope> GetAllowedBlackboardScopes(BaseTree currentTree);
        IEnumerable<BaseTree> GetAdditionalVisibleBlackboardSources(BaseTree currentTree);
        bool IsBlackboardDeclarationTypeAllowed(Type exposedPropertyType, Type valueType);
    }

    public sealed class BlackboardGraphDataCatalogSource : IGraphDataCatalogSource, IGraphDataCatalogCreationSource
    {
        public BlackboardGraphDataCatalogSource()
        {
            EditorApplication.projectChanged += OnProjectChanged;
        }

        public event Action Changed;
        public int Order => 200;
        public GraphDataCatalogSourceKind Kind => GraphDataCatalogSourceKind.Blackboard;
        public string DisplayName => "Blackboard";

        public IEnumerable<GraphDataCatalogEntry> GetEntries(GraphDataCatalogContext context)
        {
            if (context?.Tree == null)
                yield break;

            foreach (BaseExposedProperty declaration in context.VisibleBlackboardSources
                         .SelectMany(i => i.ExposedProperties)
                         .Where(i => i != null)
                         .Where(i => IsTypeAllowed(context, i.GetType(), i.ValueType))
                         .OrderBy(i => i.Owner == context.Tree ? 0 : 1)
                         .ThenBy(i => i.BlackboardCategoryPath)
                         .ThenBy(i => i.Index))
            {
                bool local = declaration.Owner == context.Tree;
                bool editable = local && !declaration.Internal && declaration.CanEdit;
                GraphDataCatalogCapability capabilities = GraphDataCatalogCapability.ExpandDetails;
                if (editable)
                    capabilities |= GraphDataCatalogCapability.Edit | GraphDataCatalogCapability.Delete;
                if (!local)
                    capabilities |= GraphDataCatalogCapability.LocateSource;

                string unavailableReason = string.Empty;
                if (BlackboardGraphDataNodeFactoryRegistry.CanCreate(context, declaration, out unavailableReason))
                    capabilities |= GraphDataCatalogCapability.DragCreateNode;

                string category = NormalizeCategory(declaration.BlackboardCategoryPath);
                bool actionWindow = declaration.BlackboardFactProjection == PipelineBlackboardFactProjectionKind.ActionWindow;
                yield return new GraphDataCatalogEntry(
                    this,
                    $"blackboard:{declaration.Owner?.GraphAuthoringId}:{declaration.DeclarationId}",
                    GraphDataCatalogEntryKind.BlackboardDeclaration,
                    declaration.Name,
                    actionWindow ? "ActionWindow Bool Query" : declaration.ValueType?.Name ?? "Unknown",
                    actionWindow ? $"Blackboard/Action Windows/{category}" : $"Blackboard/{category}",
                    local ? GraphDataCatalogOwnership.Local : GraphDataCatalogOwnership.Inherited,
                    DisplayName,
                    declaration.Owner?.name ?? "Unknown",
                    declaration.Color(),
                    capabilities,
                    declaration,
                    context.Generation,
                    unavailableReason,
                    $"{declaration.BlackboardKey} {declaration.BlackboardFactProjection} {declaration.ActionWindowType} {declaration.ActionWindowId} {declaration.ActionWindowDigest}");
            }
        }

        public VisualElement CreateDetails(GraphDataCatalogEntry entry, GraphDataCatalogContext context, Action requestRefresh)
        {
            if (!(entry?.Payload is BaseExposedProperty declaration))
                return null;

            VisualElement details = GraphDataCatalogDetails.CreateContainer();
            GraphDataCatalogDetails.AddRow(details, "Owner", declaration.Owner?.name ?? "Unknown");
            bool editable = declaration.Owner == context.Tree && !declaration.Internal && declaration.CanEdit;
            if (!editable)
            {
                AddReadOnlyDetails(details, declaration);
                return details;
            }

            AddEditableField(details, declaration, "m_Name", "Name", requestRefresh, true);
            AddEditableField(details, declaration, "m_BlackboardKey", "Blackboard Key", requestRefresh);
            AddEditableField(details, declaration, "m_BlackboardScope", "Scope", requestRefresh);
            AddEditableField(details, declaration, "m_BlackboardLifetime", "Lifetime", requestRefresh);
            AddEditableField(details, declaration, "m_BlackboardAuthority", "Authority", requestRefresh);
            AddEditableField(details, declaration, "m_BlackboardSyncPolicy", "Sync Policy", requestRefresh);
            if (declaration.BlackboardSyncPolicy == PipelineBlackboardVariableSyncPolicy.InputDerived)
                AddEditableField(details, declaration, "m_InputValueId", "Input Value Id", requestRefresh);
            AddEditableField(details, declaration, "m_BlackboardFactProjection", "Projection", requestRefresh);
            if (declaration.BlackboardFactProjection == PipelineBlackboardFactProjectionKind.ActionWindow)
            {
                AddEditableField(details, declaration, "m_ActionWindowType", "Window Type", requestRefresh);
                AddEditableField(details, declaration, "m_ActionWindowId", "Window Id", requestRefresh);
                AddEditableField(details, declaration, "m_ActionWindowDigest", "Digest", requestRefresh);
            }
            if (!PipelineBlackboardFactProjectionPolicy.TryValidate(declaration, out string projectionError))
                GraphDataCatalogDetails.AddRow(details, "Projection Error", projectionError);
            if (!PipelineBlackboardVariablePolicy.TryValidateInputBinding(declaration, out string inputError))
                GraphDataCatalogDetails.AddRow(details, "Input Error", inputError);
            AddEditableField(details, declaration, "m_BlackboardCategoryPath", "Category Path", requestRefresh);
            AddEditableField(details, declaration, "m_Value", declaration.ValueType?.Name ?? "Value", requestRefresh);
            return details;
        }

        public bool CanCreateNode(GraphDataCatalogEntry entry, GraphDataCatalogContext context, BaseTreeView treeView, out string reason)
        {
            if (entry == null || entry.ContextGeneration != context?.Generation || treeView?.Tree != context?.Tree)
            {
                reason = "Graph context changed. Refresh the catalog entry.";
                return false;
            }

            if (!(entry.Payload is BaseExposedProperty declaration))
            {
                reason = "Blackboard declaration is unavailable.";
                return false;
            }

            return BlackboardGraphDataNodeFactoryRegistry.CanCreate(context, declaration, out reason);
        }

        public bool TryCreateNode(
            GraphDataCatalogEntry entry,
            GraphDataCatalogContext context,
            BaseTreeView treeView,
            Vector2 position,
            out string error)
        {
            if (!CanCreateNode(entry, context, treeView, out error))
                return false;

            return BlackboardGraphDataNodeFactoryRegistry.TryCreate(
                context,
                treeView,
                entry.Payload as BaseExposedProperty,
                position,
                out error);
        }

        public bool TryDelete(GraphDataCatalogEntry entry, GraphDataCatalogContext context, out string error)
        {
            error = string.Empty;
            if (!(entry?.Payload is BaseExposedProperty declaration) ||
                declaration.Internal ||
                declaration.Owner != context?.Tree ||
                !(declaration.Owner is BaseTree owner))
            {
                error = "Only editable declarations owned by the current graph can be deleted.";
                return false;
            }

            owner.ApplyModify("Remove Blackboard Declaration", () =>
            {
                owner.DeleteExposedProperty(declaration);
                declaration.OnRemoved?.Invoke();
                owner.GetNewSerializedTree();
                owner.OnExposedPropertyChanged?.Invoke();
            });
            Changed?.Invoke();
            return true;
        }

        public void Locate(GraphDataCatalogEntry entry, GraphDataCatalogContext context)
        {
            if (!(entry?.Payload is BaseExposedProperty declaration))
                return;

            declaration.OnSelected?.Invoke();
            if (declaration.Owner?.SerializedOwner)
            {
                Selection.activeObject = declaration.Owner.SerializedOwner;
                EditorGUIUtility.PingObject(declaration.Owner.SerializedOwner);
            }
        }

        public IReadOnlyList<GraphDataCatalogCreationOption> GetScopeOptions(GraphDataCatalogContext context)
        {
            IReadOnlyList<PipelineBlackboardVariableScope> scopes =
                context?.AuthoringContext is ITreeInspectorBlackboardAuthoringContext source
                    ? source.GetAllowedBlackboardScopes(context.Tree)
                    : new[] { PipelineBlackboardVariableScope.Graph };

            return scopes
                .Distinct()
                .Select(i => new GraphDataCatalogCreationOption(i.ToString(), i.ToString()))
                .ToArray();
        }

        public IReadOnlyList<GraphDataCatalogCreationOption> GetTypeOptions(GraphDataCatalogContext context)
        {
            return ExposedPropertyUtility.ExposedPropertyTypeMap
                .Where(i => IsTypeAllowed(context, i.Key, i.Value.ValueType))
                .OrderBy(i => i.Value.ValueType?.Name)
                .Select(i => new GraphDataCatalogCreationOption(
                    i.Key.AssemblyQualifiedName,
                    i.Value.ValueType?.Name ?? i.Key.Name.Replace("ExposedProperty", string.Empty)))
                .ToArray();
        }

        public bool TryCreate(GraphDataCatalogCreateRequest request, GraphDataCatalogContext context, out string error)
        {
            error = string.Empty;
            if (context?.Tree == null)
            {
                error = "Current graph is missing.";
                return false;
            }

            if (!Enum.TryParse(request?.ScopeId, out PipelineBlackboardVariableScope scope) ||
                !GetScopeOptions(context).Any(i => i.Id == request.ScopeId))
            {
                error = "The selected scope is not valid for the current graph owner.";
                return false;
            }

            Type type = Type.GetType(request.TypeId, false);
            if (type == null || !ExposedPropertyUtility.ExposedPropertyTypeMap.TryGetValue(type, out BaseExposedProperty prototype) ||
                !IsTypeAllowed(context, type, prototype.ValueType))
            {
                error = "The selected declaration type is not available.";
                return false;
            }

            BaseExposedProperty declaration = null;
            context.Tree.ApplyModify("Create Blackboard Declaration", () =>
            {
                declaration = context.Tree.CreateExposedProperty(type);
                declaration.Name = string.IsNullOrWhiteSpace(request.Name)
                    ? declaration.ValueType?.Name ?? type.Name
                    : request.Name.Trim();
                declaration.Name = GetUniqueName(context.Tree, declaration);
                declaration.CanEdit = true;
                declaration.ConfigurePipelineBlackboard(
                    declaration.Name,
                    scope,
                    PipelineBlackboardVariablePolicy.DefaultLifetime(scope),
                    PipelineBlackboardVariableAuthority.LocalOnly,
                    PipelineBlackboardVariableSyncPolicy.None,
                    string.Empty,
                    string.Empty);
                context.Tree.GetNewSerializedTree();
                context.Tree.OnExposedPropertyChanged?.Invoke();
            });
            Changed?.Invoke();
            return declaration != null;
        }

        public void Dispose()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        void OnProjectChanged()
        {
            Changed?.Invoke();
        }

        static string NormalizeCategory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Uncategorized";

            string value = string.Join("/", path.Split('/').Select(i => i.Trim()).Where(i => i.Length > 0));
            return string.IsNullOrEmpty(value) ? "Uncategorized" : value;
        }

        static bool IsTypeAllowed(GraphDataCatalogContext context, Type exposedPropertyType, Type valueType)
        {
            return context?.AuthoringContext is not ITreeInspectorBlackboardAuthoringContext source ||
                   source.IsBlackboardDeclarationTypeAllowed(exposedPropertyType, valueType);
        }

        static void AddReadOnlyDetails(VisualElement details, BaseExposedProperty declaration)
        {
            GraphDataCatalogDetails.AddRow(details, "Name", declaration.Name);
            GraphDataCatalogDetails.AddRow(details, "Blackboard Key", declaration.BlackboardKey);
            GraphDataCatalogDetails.AddRow(details, "Scope", declaration.BlackboardScope.ToString());
            GraphDataCatalogDetails.AddRow(details, "Lifetime", declaration.BlackboardLifetime.ToString());
            GraphDataCatalogDetails.AddRow(details, "Authority", declaration.BlackboardAuthority.ToString());
            GraphDataCatalogDetails.AddRow(details, "Sync Policy", declaration.BlackboardSyncPolicy.ToString());
            if (declaration.BlackboardSyncPolicy == PipelineBlackboardVariableSyncPolicy.InputDerived)
                GraphDataCatalogDetails.AddRow(details, "Input Value Id", declaration.InputValueId);
            GraphDataCatalogDetails.AddRow(details, "Projection", declaration.BlackboardFactProjection.ToString());
            if (declaration.BlackboardFactProjection == PipelineBlackboardFactProjectionKind.ActionWindow)
            {
                GraphDataCatalogDetails.AddRow(details, "Window Type", declaration.ActionWindowType);
                GraphDataCatalogDetails.AddRow(details, "Window Id", declaration.ActionWindowId);
                GraphDataCatalogDetails.AddRow(details, "Digest", declaration.ActionWindowDigest.ToString());
            }
            if (!PipelineBlackboardFactProjectionPolicy.TryValidate(declaration, out string projectionError))
                GraphDataCatalogDetails.AddRow(details, "Projection Error", projectionError);
            if (!PipelineBlackboardVariablePolicy.TryValidateInputBinding(declaration, out string inputError))
                GraphDataCatalogDetails.AddRow(details, "Input Error", inputError);
            GraphDataCatalogDetails.AddRow(details, "Category Path", string.IsNullOrWhiteSpace(declaration.BlackboardCategoryPath) ? "Uncategorized" : declaration.BlackboardCategoryPath);
            GraphDataCatalogDetails.AddRow(details, declaration.ValueType?.Name ?? "Value", declaration.GetValue()?.ToString() ?? "null");
        }

        void AddEditableField(
            VisualElement container,
            BaseExposedProperty declaration,
            string propertyPath,
            string label,
            Action requestRefresh,
            bool normalizeName = false)
        {
            SerializedProperty serializedProperty = declaration.GetExposedPropertySerializedProperty(propertyPath);
            if (serializedProperty == null)
                return;

            PropertyField field = new PropertyField(serializedProperty, label);
            field.AddToClassList("graph-data-detail-field");
            field.BindProperty(serializedProperty);
            field.schedule.Execute(() => field.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                if (normalizeName && declaration.Owner is BaseTree owner)
                {
                    declaration.Name = GetUniqueName(owner, declaration);
                    declaration.OnNameChanged?.Invoke();
                }
                declaration.Owner?.OnExposedPropertyChanged?.Invoke();
                requestRefresh?.Invoke();
            })).ExecuteLater(0);
            container.Add(field);
        }

        static string GetUniqueName(BaseTree owner, BaseExposedProperty declaration)
        {
            string baseName = string.IsNullOrWhiteSpace(declaration.Name)
                ? declaration.ValueType?.Name ?? declaration.GetType().Name
                : declaration.Name.Trim();
            string result = baseName;
            int suffix = 1;
            HashSet<string> names = owner.ExposedProperties
                .Where(i => i != declaration)
                .Select(i => i.Name)
                .ToHashSet(StringComparer.Ordinal);
            while (names.Contains(result))
                result = $"{baseName} ({suffix++})";
            return result;
        }
    }
}
