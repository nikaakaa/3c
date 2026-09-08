using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner.Authoring;
using TreeDesigner.Editor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal sealed class CharacterPoseDetailsFieldEditors : IGraphAuthoringDetailsFieldEditor
    {
        readonly Func<CharacterPresentationPoseGraphAsset> m_Asset;
        readonly Func<CharacterAnimationRigDefinition> m_Rig;

        internal CharacterPoseDetailsFieldEditors(Func<CharacterPresentationPoseGraphAsset> asset, Func<CharacterAnimationRigDefinition> rig)
        {
            m_Asset = asset;
            m_Rig = rig;
        }

        public VisualElement CreateField(IGraphAuthoringDocumentProjection document, GraphAuthoringElementId elementId,
            GraphAuthoringFieldDescriptor field, object value, Action<object> apply) => field.PickerKind switch
        {
            "pose-parameter-policy" => ParameterPolicies((CharacterPoseParameterPolicy[])value, apply),
            "full-body-ik-goal-binding" => GoalBindings((CharacterPoseBoneIkGoalBinding[])value, apply),
            _ => null
        };

        VisualElement ParameterPolicies(CharacterPoseParameterPolicy[] policies, Action<object> apply)
        {
            var root = new VisualElement();
            for (int i = 0; i < policies.Length; i++)
            {
                int index = i;
                CharacterPoseParameterPolicy policy = policies[i];
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                var field = new EnumField(policy.ParameterId.Value, policy.Policy);
                field.style.flexGrow = 1;
                field.style.minWidth = 0;
                field.RegisterValueChangedCallback(evt => CharacterPoseCanvasInteraction.Apply(() =>
                {
                    var next = policies.ToArray();
                    next[index] = new CharacterPoseParameterPolicy(policy.ParameterId, (PoseParameterResolvePolicy)evt.newValue);
                    apply(next);
                }));
                row.Add(field);
                row.Add(new Button(() => CharacterPoseCanvasInteraction.Apply(() => apply(policies.Where((_, n) => n != index).ToArray()))) { text = "−" });
                root.Add(row);
            }
            var used = policies.Select(value => value.ParameterId.Value).ToHashSet(StringComparer.Ordinal);
            var choices = m_Asset().Graph.Parameters.Select(value => value.ParameterId.Value).Where(value => !used.Contains(value)).ToList();
            if (choices.Count != 0)
            {
                var parameter = new DropdownField("参数", choices, 0);
                var policy = new EnumField("混合策略", PoseParameterResolvePolicy.Weighted);
                root.Add(parameter);
                root.Add(policy);
                root.Add(new Button(() => CharacterPoseCanvasInteraction.Apply(() => apply(policies.Append(
                    new CharacterPoseParameterPolicy(new PoseParameterId(parameter.value), (PoseParameterResolvePolicy)policy.value)).ToArray()))) { text = "添加参数策略" });
            }
            return root;
        }

        VisualElement GoalBindings(CharacterPoseBoneIkGoalBinding[] bindings, Action<object> apply)
        {
            var root = new VisualElement();
            CharacterAnimationRigDefinition rig = m_Rig();
            if (!rig)
            {
                root.Add(new HelpBox("从角色 Definition 打开此图后选择目标骨骼。", HelpBoxMessageType.Info));
                return root;
            }
            List<string> bones = rig.PhysicalBones.Select(value => value.BoneId.Value)
                .Concat(rig.VirtualBones.Select(value => value.VirtualBoneId.Value)).ToList();
            for (int i = 0; i < bindings.Length; i++)
            {
                int index = i;
                CharacterPoseBoneIkGoalBinding binding = bindings[i];
                var section = new Foldout { text = binding.EffectorSlot.ToString(), value = true };
                var effector = new EnumField("效应器", binding.EffectorSlot);
                var bone = new DropdownField("目标骨骼", bones, bones.IndexOf(binding.TargetPoseBoneId.Value));
                var position = new Vector3Field("位置偏移") { value = binding.PositionOffset };
                var rotation = new Vector3Field("旋转偏移") { value = binding.RotationOffset.eulerAngles };
                var positionWeight = new FloatField("位置权重") { value = binding.PositionWeight, isDelayed = true, tooltip = "0 到 1" };
                var rotationWeight = new FloatField("旋转权重") { value = binding.RotationWeight, isDelayed = true, tooltip = "0 到 1" };
                position.Query<FloatField>().ForEach(field => field.isDelayed = true);
                rotation.Query<FloatField>().ForEach(field => field.isDelayed = true);
                void Commit() => CharacterPoseCanvasInteraction.Apply(() =>
                {
                    var next = bindings.ToArray();
                    next[index] = new CharacterPoseBoneIkGoalBinding((CharacterFullBodyIkEffectorSlot)effector.value,
                        new AnimationBoneId(bone.value), position.value, rotation.value, positionWeight.value, rotationWeight.value);
                    apply(next);
                });
                effector.RegisterValueChangedCallback(_ => Commit());
                bone.RegisterValueChangedCallback(_ => Commit());
                position.RegisterValueChangedCallback(_ => Commit());
                rotation.RegisterValueChangedCallback(_ => Commit());
                positionWeight.RegisterValueChangedCallback(_ => Commit());
                rotationWeight.RegisterValueChangedCallback(_ => Commit());
                section.Add(effector);
                section.Add(bone);
                section.Add(position);
                section.Add(rotation);
                section.Add(positionWeight);
                section.Add(rotationWeight);
                section.Add(new Button(() => CharacterPoseCanvasInteraction.Apply(() => apply(bindings.Where((_, n) => n != index).ToArray()))) { text = "删除绑定" });
                root.Add(section);
            }
            var slots = Enum.GetValues(typeof(CharacterFullBodyIkEffectorSlot)).Cast<CharacterFullBodyIkEffectorSlot>()
                .Where(value => value >= CharacterFullBodyIkEffectorSlot.Body && value <= CharacterFullBodyIkEffectorSlot.RightFoot &&
                    bindings.All(binding => binding.EffectorSlot != value)).ToArray();
            if (slots.Length != 0 && bones.Count != 0)
            {
                var effector = new DropdownField("新效应器", slots.Select(value => value.ToString()).ToList(), 0);
                var bone = new DropdownField("目标骨骼", bones, 0);
                root.Add(effector);
                root.Add(bone);
                root.Add(new Button(() => CharacterPoseCanvasInteraction.Apply(() => apply(bindings.Append(
                    new CharacterPoseBoneIkGoalBinding(slots[effector.index], new AnimationBoneId(bone.value), Vector3.zero,
                        Vector3.zero, 1, 1)).ToArray()))) { text = "添加效应器绑定" });
            }
            return root;
        }
    }
}
