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
                CharacterPoseCompilerHandlerRegistry.Shared
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
            return CharacterPoseCompilerHandlerRegistry.Shared
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
            TreeDesigner.Editor.GraphAuthoringFieldDescriptor field,
            JToken token,
            Type expectedType,
            Func<
                TreeDesigner.Editor.GraphAuthoringFieldDescriptor,
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
                TreeDesigner.Editor.GraphAuthoringFieldValueKind.Boolean =>
                    token.Value<bool>(),
                TreeDesigner.Editor.GraphAuthoringFieldValueKind.Integer =>
                    token.Value<int>(),
                TreeDesigner.Editor.GraphAuthoringFieldValueKind.Float =>
                    token.Value<float>(),
                TreeDesigner.Editor.GraphAuthoringFieldValueKind.String or
                    TreeDesigner.Editor.GraphAuthoringFieldValueKind.Enum or
                    TreeDesigner.Editor.GraphAuthoringFieldValueKind
                        .IdentityReference =>
                    token.Value<string>(),
                TreeDesigner.Editor.GraphAuthoringFieldValueKind.Vector2 =>
                    new Vector2(
                        token["x"].Value<float>(),
                        token["y"].Value<float>()),
                TreeDesigner.Editor.GraphAuthoringFieldValueKind.Vector3 =>
                    new Vector3(
                        token["x"].Value<float>(),
                        token["y"].Value<float>(),
                        token["z"].Value<float>()),
                TreeDesigner.Editor.GraphAuthoringFieldValueKind.Quaternion =>
                    new Quaternion(
                        token["x"].Value<float>(),
                        token["y"].Value<float>(),
                        token["z"].Value<float>(),
                        token["w"].Value<float>()),
                TreeDesigner.Editor.GraphAuthoringFieldValueKind
                    .AssetReference =>
                    (decodeAsset ?? throw new ArgumentNullException(
                        nameof(decodeAsset)))(
                        field,
                        token,
                        expectedType),
                TreeDesigner.Editor.GraphAuthoringFieldValueKind.Object =>
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
}
