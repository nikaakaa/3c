using UnityEngine;
using Source = ThirdPersonCharacter.Pipeline.Editor.CharacterFootLandingPredictionSampler.RootHierarchyCapture;
using Column = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvColumn<ThirdPersonCharacter.Pipeline.Editor.CharacterFootLandingPredictionSampler.RootHierarchyCapture, ThirdPersonCharacter.Pipeline.Editor.CharacterFootRootHierarchySample>;
using Codecs = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvCodecs;
using Unit = ThirdPersonCharacter.Pipeline.Editor.CharacterFootCsvUnit;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterFootRootHierarchySample
    {
        internal Vector3 LogicRootPosition;
        internal Quaternion LogicRootRotation;
        internal Vector3 VisualRootLocalPosition;
        internal Quaternion VisualRootLocalRotation;
        internal Vector3 VisualRootWorldPosition;
        internal Quaternion VisualRootWorldRotation;
        internal Vector3 PoseRootLocalPosition;
        internal Quaternion PoseRootLocalRotation;
        internal Vector3 PoseRootWorldPosition;
        internal Quaternion PoseRootWorldRotation;
    }

    internal static class CharacterFootRootHierarchyColumns
    {
        internal static readonly CharacterFootCsvGroup<Source, CharacterFootRootHierarchySample> Schema =
            new CharacterFootCsvGroup<Source, CharacterFootRootHierarchySample>(
                "RootHierarchy", () => new CharacterFootRootHierarchySample(), new Column[]
                {
                    Column.Create("LogicRootPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.LogicRootPosition, (target, value) => target.LogicRootPosition = value),
                    Column.Create("LogicRootRotation", Codecs.Rotation, Unit.Unitless,
                        (in Source source) => source.LogicRootRotation, (target, value) => target.LogicRootRotation = value),
                    Column.Create("VisualRootLocalPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.VisualRootLocalPosition, (target, value) => target.VisualRootLocalPosition = value),
                    Column.Create("VisualRootLocalRotation", Codecs.Rotation, Unit.Unitless,
                        (in Source source) => source.VisualRootLocalRotation, (target, value) => target.VisualRootLocalRotation = value),
                    Column.Create("VisualRootWorldPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.VisualRootWorldPosition, (target, value) => target.VisualRootWorldPosition = value),
                    Column.Create("VisualRootWorldRotation", Codecs.Rotation, Unit.Unitless,
                        (in Source source) => source.VisualRootWorldRotation, (target, value) => target.VisualRootWorldRotation = value),
                    Column.Create("PoseRootLocalPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PoseRootLocalPosition, (target, value) => target.PoseRootLocalPosition = value),
                    Column.Create("PoseRootLocalRotation", Codecs.Rotation, Unit.Unitless,
                        (in Source source) => source.PoseRootLocalRotation, (target, value) => target.PoseRootLocalRotation = value),
                    Column.Create("PoseRootWorldPosition", Codecs.Vector, Unit.Metres,
                        (in Source source) => source.PoseRootWorldPosition, (target, value) => target.PoseRootWorldPosition = value),
                    Column.Create("PoseRootWorldRotation", Codecs.Rotation, Unit.Unitless,
                        (in Source source) => source.PoseRootWorldRotation, (target, value) => target.PoseRootWorldRotation = value),
                });
    }
}
