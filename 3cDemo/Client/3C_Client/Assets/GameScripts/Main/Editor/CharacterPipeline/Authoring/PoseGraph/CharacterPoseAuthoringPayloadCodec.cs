using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterPoseAuthoringPayloadInput
    {
        readonly Func<string, Type, object> m_Read;
        readonly Func<CharacterPoseStateMachineDefinition> m_ReadStateMachine;

        public CharacterPoseAuthoringPayloadInput(
            Func<string, Type, object> read,
            Func<CharacterPoseStateMachineDefinition> readStateMachine = null)
        {
            m_Read = read ?? throw new ArgumentNullException(nameof(read));
            m_ReadStateMachine = readStateMachine;
        }

        public T Require<T>(string field)
        {
            object value = m_Read(field, typeof(T));
            if (value is T typed)
                return typed;
            if (value == null && !typeof(T).IsValueType)
                return default;
            throw new InvalidOperationException(
                $"Pose field '{field}' requires '{typeof(T).Name}'.");
        }

        public CharacterPoseStateMachineDefinition RequireStateMachine() =>
            m_ReadStateMachine?.Invoke() ??
            throw new InvalidOperationException(
                "Pose StateMachine payload requires one child document.");
    }

    public static class CharacterPoseAuthoringPayloadCodec
    {
        public static CharacterPoseNodePayload Create(
            CharacterPoseNodeKind kind,
            CharacterPoseAuthoringPayloadInput input)
        {
            CharacterPoseNodePayload payload =
                CharacterPoseNodeDefinitionModule.Shared
                    .Require(kind)
                    .CreatePayload(
                input ?? throw new ArgumentNullException(nameof(input)));
            if (payload == null ||
                CharacterPoseGraphAuthoringCapabilities
                    .RequireKind(payload) != kind)
            {
                throw new InvalidOperationException(
                    $"Pose capability '{kind}' returned an invalid typed payload.");
            }
            return payload;
        }

        public static object Read(
            CharacterPoseNodePayload payload,
            string field)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            return CharacterPoseNodeDefinitionModule.Shared
                .Require(payload.Kind)
                .ReadField(payload, field);
        }

        public static JToken EncodeValue(
            object value,
            Func<UnityEngine.Object, JToken> encodeAsset)
        {
            return value switch
            {
                null => JValue.CreateNull(),
                UnityEngine.Object asset =>
                    (encodeAsset ?? throw new ArgumentNullException(
                        nameof(encodeAsset)))(asset),
                Vector2 vector => new JObject
                {
                    ["x"] = vector.x,
                    ["y"] = vector.y
                },
                Vector3 vector => new JObject
                {
                    ["x"] = vector.x,
                    ["y"] = vector.y,
                    ["z"] = vector.z
                },
                Quaternion rotation => new JObject
                {
                    ["x"] = rotation.x,
                    ["y"] = rotation.y,
                    ["z"] = rotation.z,
                    ["w"] = rotation.w
                },
                CharacterPoseParameterPolicy[] policies =>
                    EncodePolicies(policies),
                IReadOnlyList<CharacterPoseParameterPolicy> policies =>
                    EncodePolicies(policies),
                _ => JToken.FromObject(value)
            };
        }

        public static object DecodeValue(
            TreeDesigner.Authoring.GraphAuthoringFieldDescriptor field,
            JToken token,
            Type expectedType,
            Func<
                TreeDesigner.Authoring.GraphAuthoringFieldDescriptor,
                JToken,
                Type,
                UnityEngine.Object> decodeAsset)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (token == null)
            {
                if (field.Optional)
                    return field.DefaultValue;
                throw new InvalidOperationException(
                    $"Pose field '{field.FieldId}' has no value.");
            }
            return field.ValueKind switch
            {
                TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Boolean =>
                    token.Value<bool>(),
                TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Integer =>
                    token.Value<int>(),
                TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Float =>
                    token.Value<float>(),
                TreeDesigner.Authoring.GraphAuthoringFieldValueKind.String or
                    TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Enum or
                    TreeDesigner.Authoring.GraphAuthoringFieldValueKind
                        .IdentityReference =>
                    token.Value<string>(),
                TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Vector2 =>
                    new Vector2(
                        token["x"].Value<float>(),
                        token["y"].Value<float>()),
                TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Vector3 =>
                    new Vector3(
                        token["x"].Value<float>(),
                        token["y"].Value<float>(),
                        token["z"].Value<float>()),
                TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Quaternion =>
                    new Quaternion(
                        token["x"].Value<float>(),
                        token["y"].Value<float>(),
                        token["z"].Value<float>(),
                        token["w"].Value<float>()),
                TreeDesigner.Authoring.GraphAuthoringFieldValueKind
                    .AssetReference =>
                    (decodeAsset ?? throw new ArgumentNullException(
                        nameof(decodeAsset)))(
                        field,
                        token,
                        expectedType),
                TreeDesigner.Authoring.GraphAuthoringFieldValueKind.Object =>
                    DecodeObject(field.FieldId.Value, token, expectedType),
                _ => throw new InvalidOperationException(
                    $"Unsupported Pose field kind '{field.ValueKind}'.")
            };
        }

        static JArray EncodePolicies(
            IEnumerable<CharacterPoseParameterPolicy> policies) =>
            new JArray((policies ??
                        throw new ArgumentNullException(nameof(policies)))
                .Select(policy => new JObject
                {
                    ["parameterId"] = policy.ParameterId.Value,
                    ["policy"] = policy.Policy.ToString()
                }));

        static object DecodeObject(
            string field,
            JToken token,
            Type expectedType)
        {
            if (string.Equals(
                    field,
                    "parameter-policies",
                    StringComparison.Ordinal))
            {
                return token.Select(value =>
                    new CharacterPoseParameterPolicy(
                        new PoseParameterId(
                            value["parameterId"].Value<string>()),
                        Enum.Parse<PoseParameterResolvePolicy>(
                            value["policy"].Value<string>(),
                            false))).ToArray();
            }
            return token.ToObject(
                expectedType ?? throw new ArgumentNullException(
                    nameof(expectedType)));
        }
    }

    internal readonly struct CharacterPoseAuthoringNodeMetadata
    {
        readonly CharacterPoseNodeDefinition m_Definition;

        internal CharacterPoseAuthoringNodeMetadata(
            CharacterPoseNodeDefinition definition)
        {
            m_Definition = definition ??
                throw new ArgumentNullException(nameof(definition));
        }

        public CharacterPoseNodeKind Kind => m_Definition.Kind;
        public string CapabilityIdentity => m_Definition.CapabilityIdentity;
        public IReadOnlyCollection<GraphAuthoringFieldDescriptor> Fields =>
            m_Definition.Capability.Fields;
        public CharacterPoseOperationFamily OperationFamily => m_Definition.OperationFamily;
        public CharacterPoseNativeNodeRole NativeRole => m_Definition.NativeRole;
        public CharacterPoseOperationCode OperationCode => m_Definition.OperationCode;
        public bool WorkerThreadSafe => m_Definition.WorkerThreadSafe;
        public CharacterPoseWorkerKernelId WorkerKernel => m_Definition.WorkerKernel;
        public bool UsesPoseSourceSlot => m_Definition.UsesPoseSourceSlot;
        public bool UsesAnimationChannel => m_Definition.UsesAnimationChannel;

        public CharacterPresentationPoseSourceSlot Source(
            CharacterPoseNodePayload payload) =>
            m_Definition.Source(payload);

        public AnimationChannelId Channel(
            CharacterPoseNodePayload payload) =>
            m_Definition.Channel(payload);

        public IReadOnlyList<GraphAuthoringDynamicPortProjection>
            ProjectPortShape(CharacterPoseCanvasNode node) =>
            m_Definition.ProjectPortShape(node);

        public object ReadField(
            CharacterPoseNodePayload payload,
            string field) =>
            m_Definition.ReadField(payload, field);

        public string ProjectChildDocumentId(
            CharacterPoseNodePayload payload) =>
            m_Definition.ProjectChildDocumentId(payload);

        public IReadOnlyList<CharacterPoseGraphDependency>
            ProjectGraphDependencies(CharacterPoseNodePayload payload) =>
            m_Definition.ProjectGraphDependencies(payload);

        public IReadOnlyList<CharacterPoseResourceSlot>
            ProjectResourceSlots(CharacterPoseNodePayload payload) =>
            m_Definition.ProjectResourceSlots(payload);

        public CharacterAnimationBlendSpaceInputRangePolicy InputRange(
            CharacterPoseNodePayload payload) =>
            m_Definition.InputRange(payload);
    }

    internal static class CharacterPoseAuthoringMetadata
    {
        public static IReadOnlyList<CharacterPoseAuthoringNodeMetadata> All =>
            CharacterPoseNodeDefinitionModule.Shared.All
                .Select(value => new CharacterPoseAuthoringNodeMetadata(value))
                .ToArray();

        public static CharacterPoseAuthoringNodeMetadata Require(
            CharacterPoseNodeKind kind) =>
            new CharacterPoseAuthoringNodeMetadata(
                CharacterPoseNodeDefinitionModule.Shared.Require(kind));

        public static CharacterPoseAuthoringNodeMetadata RequireCapability(
            string capabilityIdentity) =>
            new CharacterPoseAuthoringNodeMetadata(
                CharacterPoseNodeDefinitionModule.Shared.RequireCapability(
                    capabilityIdentity));
    }
}
