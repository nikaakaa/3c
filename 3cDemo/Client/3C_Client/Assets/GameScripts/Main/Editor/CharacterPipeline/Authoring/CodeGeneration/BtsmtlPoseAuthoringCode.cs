#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public static class BtsmtlPoseAuthoringCode
    {
        public static T CreateSourceSlot<T>(string name)
            where T : CharacterPresentationPoseSourceSlot
        {
            if (typeof(T).IsAbstract)
                throw new InvalidOperationException($"Pose Source Slot type '{typeof(T).FullName}' is abstract.");
            T slot = ScriptableObject.CreateInstance<T>();
            slot.name = string.IsNullOrWhiteSpace(name) ? typeof(T).Name : name.Trim();
            slot.RequireValid();
            return slot;
        }

        public static CharacterPoseResourceSlot CreateResourceSlot(
            CharacterPoseResourceKind kind,
            string name)
        {
            CharacterPoseResourceSlot slot = CharacterPoseResourceSlot.Create(kind);
            slot.name = string.IsNullOrWhiteSpace(name) ? kind.ToString() : name.Trim();
            slot.RequireValid();
            return slot;
        }

        public static CharacterPoseSubgraphReference CreateSubgraphReference(string graphId)
        {
            var reference = new CharacterPoseSubgraphReference();
            reference.Assign(new PoseGraphId(graphId));
            return reference;
        }

        public static CharacterPresentationPoseGraphAsset EnsureRoot(
            BtsmtlAuthoringGenerationContext context,
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationProfile profile,
            CharacterPoseCanvasGraph[] graphs,
            CharacterPresentationPoseSourceSlot[] sourceSlots,
            CharacterPoseResourceSlot[] resourceSlots,
            int[] sourceBindingIndices,
            int[] resourceBindingIndices,
            CharacterPoseStateMachineLayout[] stateMachineLayouts)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!definition || !profile)
                throw new InvalidOperationException("Pose C# authoring requires a Definition and Presentation Profile.");
            if (!ReferenceEquals(definition.AnimationPresentationProfile, profile))
                throw new InvalidOperationException("Pose C# authoring Profile is not the Definition Presentation Profile.");
            CharacterPoseCanvasGraph[] graphValues = graphs ?? Array.Empty<CharacterPoseCanvasGraph>();
            if (graphValues.Length == 0 || graphValues[0] == null)
                throw new InvalidOperationException("Pose C# authoring requires a root Graph.");
            if (graphValues.Any(value => value == null))
                throw new InvalidOperationException("Pose C# authoring Graph catalog contains a missing Graph.");
            if (graphValues.Select(value => value.GraphId).Distinct().Count() != graphValues.Length)
                throw new InvalidOperationException("Pose C# authoring Graph catalog contains duplicate identities.");
            CharacterPresentationPoseSourceSlot[] sourceValues =
                sourceSlots ?? Array.Empty<CharacterPresentationPoseSourceSlot>();
            CharacterPoseResourceSlot[] resourceValues =
                resourceSlots ?? Array.Empty<CharacterPoseResourceSlot>();
            int[] sourceIndices = sourceBindingIndices ?? Array.Empty<int>();
            int[] resourceIndices = resourceBindingIndices ?? Array.Empty<int>();
            if (sourceIndices.Length != sourceValues.Length || resourceIndices.Length != resourceValues.Length)
                throw new InvalidOperationException("Pose C# authoring Profile binding indexes do not match the slot catalog.");

            string outputPath = context.OutputAssetPath.Replace('\\', '/');
            if (!outputPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputPath) is not null &&
                AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(outputPath) == null)
            {
                throw new InvalidOperationException($"Pose C# authoring output '{outputPath}' is not a Pose Graph asset path.");
            }
            string outputDirectory = Path.GetDirectoryName(outputPath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(outputDirectory) || !AssetDatabase.IsValidFolder(outputDirectory))
                throw new InvalidOperationException($"Pose C# authoring output folder '{outputDirectory}' does not exist.");

            CharacterPresentationPoseGraphAsset asset =
                AssetDatabase.LoadAssetAtPath<CharacterPresentationPoseGraphAsset>(outputPath);
            if (!asset)
            {
                asset = ScriptableObject.CreateInstance<CharacterPresentationPoseGraphAsset>();
                asset.name = Path.GetFileNameWithoutExtension(outputPath);
                AssetDatabase.CreateAsset(asset, outputPath);
            }
            if (asset.Graph != null && asset.Graph.GraphId != graphValues[0].GraphId)
                throw new InvalidOperationException(
                    $"Pose output root identity '{asset.Graph.GraphId}' does not match '{graphValues[0].GraphId}'.");

            CharacterPoseGraphAssetMutationOwner owner =
                new CharacterPoseGraphAssetMutationOwner(asset, profile);
            var existingGraphs = asset.EnumerateGraphs().Where(value => value != null).ToArray();
            var desiredGraphIds = new HashSet<PoseGraphId>(graphValues.Select(value => value.GraphId));
            var undoObjects = new List<UnityEngine.Object> { asset };
            undoObjects.AddRange(existingGraphs);
            undoObjects.AddRange(asset.SourceSlots.Where(value => value));
            undoObjects.AddRange(asset.ResourceSlots.Where(value => value));
            undoObjects.Add(profile);
            undoObjects.AddRange(profile.PoseSourceBindings.Where(value => value));
            Undo.RegisterCompleteObjectUndo(undoObjects.Distinct().ToArray(), "执行 Pose C# authoring");

            if (asset.Graph == null)
            {
                Undo.RegisterCreatedObjectUndo(graphValues[0], "创建 Pose 根图");
                owner.ApplyGraphCatalogMutation(new CreatePoseGraphMutation(outputPath, graphValues[0]));
            }
            else
            {
                owner.ReplacePoseGraph(graphValues[0]);
            }

            foreach (CharacterPoseCanvasGraph current in asset.GraphCatalog.ToArray())
            {
                if (!desiredGraphIds.Contains(current.GraphId))
                    owner.ApplyGraphCatalogMutation(new DeletePoseGraphMutation(outputPath, current.GraphId));
            }
            foreach (CharacterPoseCanvasGraph graph in graphValues.Skip(1))
            {
                if (asset.TryGetGraph(graph.GraphId, out CharacterPoseCanvasGraph current))
                    owner.ReplacePoseGraph(graph);
                else
                {
                    Undo.RegisterCreatedObjectUndo(graph, "创建 Pose 子图");
                    owner.ApplyGraphCatalogMutation(new CreatePoseGraphMutation(outputPath, graph));
                }
            }

            foreach (CharacterPresentationPoseSourceSlot slot in sourceValues)
            {
                Undo.RegisterCreatedObjectUndo(slot, "创建 Pose Source Slot");
                owner.ApplyGraphCatalogMutation(new CreatePoseSourceSlotMutation(outputPath, slot));
            }
            foreach (CharacterPoseResourceSlot slot in resourceValues)
            {
                Undo.RegisterCreatedObjectUndo(slot, "创建 Pose Resource Slot");
                owner.ApplyGraphCatalogMutation(new CreatePoseResourceSlotMutation(outputPath, slot));
            }

            RebindSourceSlots(profile, sourceValues, sourceIndices);
            RebindResourceSlots(profile, resourceValues, resourceIndices);
            profile.SetPresentationGraph(asset, profile.RigDefinition);
            EditorUtility.SetDirty(profile);

            ApplyStateMachineLayouts(owner, asset, stateMachineLayouts);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static void RebindSourceSlots(
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<CharacterPresentationPoseSourceSlot> slots,
            IReadOnlyList<int> indices)
        {
            CharacterPresentationPoseSourceBinding[] bindings =
                profile.PoseSourceBindings.ToArray();
            var next = new List<CharacterPresentationPoseSourceBinding>();
            for (int i = 0; i < slots.Count; i++)
            {
                int index = indices[i];
                if ((uint)index >= (uint)bindings.Length)
                    throw new InvalidOperationException($"Pose Source Slot '{slots[i].name}' binding index {index} is invalid.");
                CharacterPresentationPoseSourceBinding binding = bindings[index];
                ConfigureSourceBinding(binding, slots[i]);
                next.Add(binding);
            }
            profile.SetPoseSourceBindings(next.ToArray());
        }

        static void ConfigureSourceBinding(
            CharacterPresentationPoseSourceBinding binding,
            CharacterPresentationPoseSourceSlot slot)
        {
            switch (binding)
            {
                case CharacterClipPoseSourceBinding clip when slot is CharacterClipPoseSourceSlot clipSlot:
                    clip.Configure(clipSlot, clip.Clip);
                    return;
                case CharacterBlendSpacePoseSourceBinding blendSpace when slot is CharacterBlendSpacePoseSourceSlot blendSpaceSlot:
                    blendSpace.Configure(
                        blendSpaceSlot,
                        blendSpace.BlendSpace,
                        blendSpace.Rig,
                        blendSpace.FootAnalysisIdentity);
                    return;
                case CharacterMotionMatchingPoseSourceBinding motionMatching when slot is CharacterMotionMatchingPoseSourceSlot motionMatchingSlot:
                    motionMatching.Configure(
                        motionMatchingSlot,
                        motionMatching.Profile,
                        motionMatching.Rig,
                        motionMatching.SearchDomainId,
                        motionMatching.Databases.ToArray(),
                        motionMatching.FootAnalysisIdentity);
                    return;
                default:
                    throw new InvalidOperationException(
                        $"Pose Source Slot '{slot.name}' does not match binding '{binding?.GetType().FullName}'.");
            }
        }

        static void RebindResourceSlots(
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<CharacterPoseResourceSlot> slots,
            IReadOnlyList<int> indices)
        {
            CharacterPoseResourceBinding[] bindings = profile.PoseResourceBindings.ToArray();
            var next = new List<CharacterPoseResourceBinding>();
            for (int i = 0; i < slots.Count; i++)
            {
                int index = indices[i];
                if ((uint)index >= (uint)bindings.Length)
                    throw new InvalidOperationException($"Pose Resource Slot '{slots[i].name}' binding index {index} is invalid.");
                CharacterPoseResourceBinding binding = bindings[index];
                if (binding == null)
                    throw new InvalidOperationException($"Pose Resource Slot '{slots[i].name}' binding is missing.");
                binding.Configure(slots[i], binding.Resource);
                next.Add(binding);
            }
            profile.SetPoseResourceBindings(next.ToArray());
        }

        static void ApplyStateMachineLayouts(
            CharacterPoseGraphAssetMutationOwner owner,
            CharacterPresentationPoseGraphAsset asset,
            IReadOnlyList<CharacterPoseStateMachineLayout> layouts)
        {
            CharacterPoseStateMachineLayout[] desired =
                (layouts ?? Array.Empty<CharacterPoseStateMachineLayout>()).ToArray();
            var desiredMachines = new HashSet<PoseStateMachineId>(desired.Select(value => value.StateMachineId));
            var machines = asset.EnumerateStateMachines().ToArray();
            foreach (CharacterPoseStateMachineLayout current in asset.StateMachineLayouts)
            {
                if (!desiredMachines.Contains(current.StateMachineId))
                {
                    if (machines.All(value => !value.StateMachineId.Equals(current.StateMachineId)))
                        throw new InvalidOperationException(
                            $"Pose StateMachine layout '{current.StateMachineId}' has no generated StateMachine owner.");
                    foreach (CharacterPoseStateMachineLayoutElement element in current.Elements)
                        owner.ApplyStateMachineMutation(
                            new RemovePoseStateMachineLayoutElementMutation(
                                current.StateMachineId.Value,
                                element.ElementId));
                }
                else
                {
                    CharacterPoseStateMachineLayout next = desired.Single(value => value.StateMachineId.Equals(current.StateMachineId));
                    var nextIds = new HashSet<string>(next.Elements.Select(value => value.ElementId), StringComparer.Ordinal);
                    foreach (CharacterPoseStateMachineLayoutElement element in current.Elements)
                        if (!nextIds.Contains(element.ElementId))
                            owner.ApplyStateMachineMutation(
                                new RemovePoseStateMachineLayoutElementMutation(
                                    current.StateMachineId.Value,
                                    element.ElementId));
                }
            }
            foreach (CharacterPoseStateMachineLayout layout in desired)
            {
                if (machines.All(value => !value.StateMachineId.Equals(layout.StateMachineId)))
                    throw new InvalidOperationException(
                        $"Pose StateMachine layout '{layout.StateMachineId}' has no generated StateMachine owner.");
                foreach (CharacterPoseStateMachineLayoutElement element in layout.Elements)
                    owner.ApplyStateMachineMutation(
                        new SetPoseStateMachineLayoutElementMutation(
                            layout.StateMachineId.Value,
                            element.ElementId,
                            element.Position));
            }
        }
    }
}
#endif
