using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BTSMTL.EventGraphs
{
    public enum EventGraphValueKind : byte
    {
        Bool = 1,
        Int32 = 2,
        Float32 = 3,
        Vector2 = 4,
        Vector3 = 5,
        Quaternion = 6,
        Enum = 7,
        String = 8
    }

    public static class EventGraphValueKinds
    {
        public static bool TryGet(Type type, out EventGraphValueKind kind)
        {
            if (type == typeof(string))
                kind = EventGraphValueKind.String;
            else if (type == typeof(bool))
                kind = EventGraphValueKind.Bool;
            else if (type == typeof(int))
                kind = EventGraphValueKind.Int32;
            else if (type == typeof(float))
                kind = EventGraphValueKind.Float32;
            else if (type == typeof(UnityEngine.Vector2))
                kind = EventGraphValueKind.Vector2;
            else if (type == typeof(UnityEngine.Vector3))
                kind = EventGraphValueKind.Vector3;
            else if (type == typeof(UnityEngine.Quaternion))
                kind = EventGraphValueKind.Quaternion;
            else if (type != null && type.IsEnum)
                kind = EventGraphValueKind.Enum;
            else
            {
                kind = default;
                return false;
            }
            return true;
        }

        public static Type RequireType(EventGraphValueKind kind) => kind switch
        {
            EventGraphValueKind.String => typeof(string),
            EventGraphValueKind.Bool => typeof(bool),
            EventGraphValueKind.Int32 => typeof(int),
            EventGraphValueKind.Float32 => typeof(float),
            EventGraphValueKind.Vector2 => typeof(UnityEngine.Vector2),
            EventGraphValueKind.Vector3 => typeof(UnityEngine.Vector3),
            EventGraphValueKind.Quaternion => typeof(UnityEngine.Quaternion),
            EventGraphValueKind.Enum => typeof(System.Enum),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        public static bool IsSupported(Type type) => TryGet(type, out _);
    }

    public readonly struct EventGraphValue
    {
        readonly string m_StringValue;
        readonly bool m_BoolValue;
        readonly int m_IntValue;
        readonly float m_FloatValue;
        readonly UnityEngine.Vector2 m_Vector2Value;
        readonly UnityEngine.Vector3 m_Vector3Value;
        readonly UnityEngine.Quaternion m_QuaternionValue;
        readonly Type m_EnumType;
        readonly int m_EnumValue;

        EventGraphValue(
            EventGraphValueKind kind,
            bool boolValue,
            int intValue,
            float floatValue,
            UnityEngine.Vector2 vector2Value,
            UnityEngine.Vector3 vector3Value)
            : this(
                kind,
                boolValue,
                intValue,
                floatValue,
                vector2Value,
                vector3Value,
                default,
                null,
                0)
        {
        }

        EventGraphValue(
            EventGraphValueKind kind,
            bool boolValue,
            int intValue,
            float floatValue,
            UnityEngine.Vector2 vector2Value,
            UnityEngine.Vector3 vector3Value,
            UnityEngine.Quaternion quaternionValue,
            Type enumType,
            int enumValue,
            string stringValue = null)
        {
            m_StringValue = stringValue;
            Kind = kind;
            m_BoolValue = boolValue;
            m_IntValue = intValue;
            m_FloatValue = floatValue;
            m_Vector2Value = vector2Value;
            m_Vector3Value = vector3Value;
            m_QuaternionValue = quaternionValue;
            m_EnumType = enumType;
            m_EnumValue = enumValue;
        }

        public EventGraphValueKind Kind { get; }

        public string StringValue
        {
            get { RequireKind(EventGraphValueKind.String); return m_StringValue; }
        }

        public static EventGraphValue FromString(string value) =>
            new EventGraphValue(EventGraphValueKind.String, false, 0, 0f, default, default, default, null, 0,
                value ?? throw new ArgumentNullException(nameof(value)));

        public bool BoolValue
        {
            get
            {
                RequireKind(EventGraphValueKind.Bool);
                return m_BoolValue;
            }
        }

        public int Int32Value
        {
            get
            {
                RequireKind(EventGraphValueKind.Int32);
                return m_IntValue;
            }
        }

        public float Float32Value
        {
            get
            {
                RequireKind(EventGraphValueKind.Float32);
                return m_FloatValue;
            }
        }

        public UnityEngine.Vector2 Vector2Value
        {
            get
            {
                RequireKind(EventGraphValueKind.Vector2);
                return m_Vector2Value;
            }
        }

        public UnityEngine.Vector3 Vector3Value
        {
            get
            {
                RequireKind(EventGraphValueKind.Vector3);
                return m_Vector3Value;
            }
        }

        public UnityEngine.Quaternion QuaternionValue
        {
            get
            {
                RequireKind(EventGraphValueKind.Quaternion);
                return m_QuaternionValue;
            }
        }

        public Type EnumType
        {
            get
            {
                RequireKind(EventGraphValueKind.Enum);
                return m_EnumType;
            }
        }

        public int EnumValue
        {
            get
            {
                RequireKind(EventGraphValueKind.Enum);
                return m_EnumValue;
            }
        }

        public static EventGraphValue FromBool(bool value) =>
            new EventGraphValue(EventGraphValueKind.Bool, value, 0, 0f, default, default);

        public static EventGraphValue FromInt32(int value) =>
            new EventGraphValue(EventGraphValueKind.Int32, false, value, 0f, default, default);

        public static EventGraphValue FromFloat32(float value)
        {
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            return new EventGraphValue(EventGraphValueKind.Float32, false, 0, value, default, default);
        }

        public static EventGraphValue FromVector2(UnityEngine.Vector2 value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y))
                throw new ArgumentOutOfRangeException(nameof(value));
            return new EventGraphValue(EventGraphValueKind.Vector2, false, 0, 0f, value, default);
        }

        public static EventGraphValue FromVector3(UnityEngine.Vector3 value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new ArgumentOutOfRangeException(nameof(value));
            return new EventGraphValue(EventGraphValueKind.Vector3, false, 0, 0f, default, value);
        }

        public static EventGraphValue FromQuaternion(UnityEngine.Quaternion value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || !float.IsFinite(value.w))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            return new EventGraphValue(
                EventGraphValueKind.Quaternion,
                false,
                0,
                0f,
                default,
                default,
                value,
                null,
                0);
        }

        public static EventGraphValue FromEnum(Enum value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return new EventGraphValue(
                EventGraphValueKind.Enum,
                false,
                0,
                0f,
                default,
                default,
                default,
                value.GetType(),
                Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture));
        }

        public static EventGraphValue FromObject(object value)
        {
            if (value is string stringValue)
                return FromString(stringValue);
            if (value is bool boolValue)
                return FromBool(boolValue);
            if (value is int intValue)
                return FromInt32(intValue);
            if (value is float floatValue)
                return FromFloat32(floatValue);
            if (value is UnityEngine.Vector2 vector2Value)
                return FromVector2(vector2Value);
            if (value is UnityEngine.Vector3 vector3Value)
                return FromVector3(vector3Value);
            if (value is UnityEngine.Quaternion quaternionValue)
                return FromQuaternion(quaternionValue);
            if (value is Enum enumValue)
                return FromEnum(enumValue);
            throw new ArgumentException(
                $"Event graph value type '{value?.GetType().FullName ?? "null"}' is not supported.",
                nameof(value));
        }

        public object ToObject() => Kind switch
        {
            EventGraphValueKind.String => m_StringValue,
            EventGraphValueKind.Bool => m_BoolValue,
            EventGraphValueKind.Int32 => m_IntValue,
            EventGraphValueKind.Float32 => m_FloatValue,
            EventGraphValueKind.Vector2 => m_Vector2Value,
            EventGraphValueKind.Vector3 => m_Vector3Value,
            EventGraphValueKind.Quaternion => m_QuaternionValue,
            EventGraphValueKind.Enum => Enum.ToObject(m_EnumType, m_EnumValue),
            _ => throw new InvalidOperationException("Event graph value kind is invalid.")
        };

        public T As<T>()
        {
            if (typeof(T) == typeof(string) && Kind == EventGraphValueKind.String)
                return (T)(object)m_StringValue;
            if (typeof(T) == typeof(bool) && Kind == EventGraphValueKind.Bool)
                return Reinterpret<bool, T>(m_BoolValue);
            if (typeof(T) == typeof(int) && Kind == EventGraphValueKind.Int32)
                return Reinterpret<int, T>(m_IntValue);
            if (typeof(T) == typeof(float) && Kind == EventGraphValueKind.Float32)
                return Reinterpret<float, T>(m_FloatValue);
            if (typeof(T) == typeof(UnityEngine.Vector2) && Kind == EventGraphValueKind.Vector2)
                return Reinterpret<UnityEngine.Vector2, T>(m_Vector2Value);
            if (typeof(T) == typeof(UnityEngine.Vector3) && Kind == EventGraphValueKind.Vector3)
                return Reinterpret<UnityEngine.Vector3, T>(m_Vector3Value);
            if (typeof(T) == typeof(UnityEngine.Quaternion) && Kind == EventGraphValueKind.Quaternion)
                return Reinterpret<UnityEngine.Quaternion, T>(m_QuaternionValue);
            if (typeof(T).IsEnum && Kind == EventGraphValueKind.Enum && m_EnumType == typeof(T))
                return EventGraphEnumValue<T>.FromInt32(m_EnumValue);
            throw new InvalidOperationException(
                $"Event graph value kind '{Kind}' cannot be read as '{typeof(T).FullName}'.");
        }

        static TResult Reinterpret<TValue, TResult>(TValue value) =>
            Unsafe.As<TValue, TResult>(ref value);

        internal static EventGraphValue FromTyped<T>(T value)
        {
            if (typeof(T) == typeof(bool)) return FromBool(Unsafe.As<T, bool>(ref value));
            if (typeof(T) == typeof(int)) return FromInt32(Unsafe.As<T, int>(ref value));
            if (typeof(T) == typeof(float)) return FromFloat32(Unsafe.As<T, float>(ref value));
            if (typeof(T) == typeof(UnityEngine.Vector2)) return FromVector2(Unsafe.As<T, UnityEngine.Vector2>(ref value));
            if (typeof(T) == typeof(UnityEngine.Vector3)) return FromVector3(Unsafe.As<T, UnityEngine.Vector3>(ref value));
            if (typeof(T) == typeof(UnityEngine.Quaternion)) return FromQuaternion(Unsafe.As<T, UnityEngine.Quaternion>(ref value));
            if (typeof(T) == typeof(string)) return FromString(Unsafe.As<T, string>(ref value));
            if (typeof(T).IsEnum)
                return new EventGraphValue(EventGraphValueKind.Enum, false, 0, 0f, default, default,
                    default, typeof(T), EventGraphEnumValue<T>.ToInt32(value));
            throw new InvalidOperationException($"Unsupported EventGraph value type '{typeof(T).FullName}'.");
        }

        void RequireKind(EventGraphValueKind expected)
        {
            if (Kind != expected)
                throw new InvalidOperationException(
                    $"Event graph value kind '{Kind}' cannot be read as '{expected}'.");
        }
    }

    public readonly struct EventGraphVariableReference : IEquatable<EventGraphVariableReference>
    {
        public EventGraphVariableReference(string graphId, string variableId)
        {
            GraphId = RequireIdentity(graphId, nameof(graphId));
            VariableId = RequireIdentity(variableId, nameof(variableId));
        }

        public string GraphId { get; }
        public string VariableId { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(GraphId) && !string.IsNullOrWhiteSpace(VariableId);

        public bool Equals(EventGraphVariableReference other) =>
            string.Equals(GraphId, other.GraphId, StringComparison.Ordinal) &&
            string.Equals(VariableId, other.VariableId, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is EventGraphVariableReference other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(GraphId, VariableId);
        public override string ToString() => $"{GraphId}/{VariableId}";

        static string RequireIdentity(string value, string parameterName) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Event graph identity is missing.", parameterName)
                : value.Trim();
    }

    public sealed class EventGraphVariableDescriptor
    {
        public EventGraphVariableDescriptor(
            EventGraphVariableReference reference,
            string name,
            Type valueType,
            EventGraphValue initialValue,
            bool isExposedPublic = true)
        {
            if (!reference.IsValid)
                throw new ArgumentException("Event graph variable reference is invalid.", nameof(reference));
            if (!EventGraphValueKinds.TryGet(valueType, out EventGraphValueKind kind) ||
                kind != initialValue.Kind ||
                kind == EventGraphValueKind.Enum && initialValue.EnumType != valueType)
            {
                throw new ArgumentException("Event graph variable type and initial value do not match.", nameof(valueType));
            }
            Reference = reference;
            Name = string.IsNullOrWhiteSpace(name) ? reference.VariableId : name.Trim();
            ValueType = valueType;
            ValueKind = kind;
            InitialValue = initialValue;
            IsExposedPublic = isExposedPublic;
        }

        public EventGraphVariableReference Reference { get; }
        public string Name { get; }
        public Type ValueType { get; }
        public EventGraphValueKind ValueKind { get; }
        public EventGraphValue InitialValue { get; }
        public bool IsExposedPublic { get; }
    }

    public sealed class EventGraphVariableLayoutEntry
    {
        public EventGraphVariableLayoutEntry(EventGraphVariableDescriptor descriptor, int index)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            Index = index;
        }

        public EventGraphVariableDescriptor Descriptor { get; }
        public int Index { get; }
    }

    public sealed class EventGraphVariableLayout
    {
        readonly EventGraphVariableLayoutEntry[] m_Entries;

        public EventGraphVariableLayout(
            string graphId,
            string contractRevision,
            IReadOnlyList<EventGraphVariableDescriptor> descriptors)
        {
            GraphId = string.IsNullOrWhiteSpace(graphId)
                ? throw new ArgumentException("Event graph identity is missing.", nameof(graphId))
                : graphId.Trim();
            ContractRevision = string.IsNullOrWhiteSpace(contractRevision)
                ? throw new ArgumentException("Event graph contract revision is missing.", nameof(contractRevision))
                : contractRevision.Trim();
            EventGraphVariableDescriptor[] ordered = (descriptors ?? Array.Empty<EventGraphVariableDescriptor>())
                .Where(value => value == null || value.IsExposedPublic)
                .OrderBy(value => value?.Reference.VariableId, StringComparer.Ordinal)
                .ToArray();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            m_Entries = new EventGraphVariableLayoutEntry[ordered.Length];
            for (int i = 0; i < ordered.Length; i++)
            {
                EventGraphVariableDescriptor descriptor = ordered[i] ??
                    throw new ArgumentException("Event graph variable descriptor is missing.", nameof(descriptors));
                if (!ids.Add(descriptor.Reference.VariableId))
                    throw new ArgumentException("Event graph variable identity is duplicated.", nameof(descriptors));
                m_Entries[i] = new EventGraphVariableLayoutEntry(descriptor, i);
            }
            LayoutId = $"{GraphId}/variables/{ContractRevision}";
        }

        public string GraphId { get; }
        public string ContractRevision { get; }
        public string LayoutId { get; }
        public IReadOnlyList<EventGraphVariableLayoutEntry> Entries => m_Entries;

        public bool TryGet(string variableId, out EventGraphVariableLayoutEntry entry)
        {
            for (int i = 0; i < m_Entries.Length; i++)
            {
                if (string.Equals(m_Entries[i].Descriptor.Reference.VariableId, variableId, StringComparison.Ordinal))
                {
                    entry = m_Entries[i];
                    return true;
                }
            }
            entry = null;
            return false;
        }
    }

    public sealed class EventGraphVariableContract
    {
        readonly EventGraphVariableDescriptor[] m_Descriptors;
        readonly EventGraphVariableDescriptor[] m_PublishedDescriptors;

        public EventGraphVariableContract(
            string graphId,
            string revision,
            IReadOnlyList<EventGraphVariableDescriptor> descriptors)
        {
            GraphId = string.IsNullOrWhiteSpace(graphId)
                ? throw new ArgumentException("Event graph identity is missing.", nameof(graphId))
            : graphId.Trim();
            Revision = string.IsNullOrWhiteSpace(revision)
                ? throw new ArgumentException("Event graph revision is missing.", nameof(revision))
                : revision.Trim();
            m_Descriptors = (descriptors ?? Array.Empty<EventGraphVariableDescriptor>()).ToArray();
            m_PublishedDescriptors = m_Descriptors
                .Where(value => value.IsExposedPublic)
                .OrderBy(value => value.Reference.VariableId, StringComparer.Ordinal)
                .ToArray();
            Layout = new EventGraphVariableLayout(GraphId, Revision, m_Descriptors);
        }

        public string GraphId { get; }
        public string Revision { get; }
        public IReadOnlyList<EventGraphVariableDescriptor> Descriptors => m_Descriptors;
        public IReadOnlyList<EventGraphVariableDescriptor> PublishedDescriptors => m_PublishedDescriptors;
        public EventGraphVariableLayout Layout { get; }

        public EventGraphVariableDescriptor Require(string variableId)
        {
            for (int i = 0; i < m_Descriptors.Length; i++)
            {
                if (string.Equals(m_Descriptors[i].Reference.VariableId, variableId, StringComparison.Ordinal))
                    return m_Descriptors[i];
            }
            throw new InvalidOperationException($"Event graph variable '{variableId}' is not declared.");
        }
    }

    public sealed class EventGraphInputDescriptor
    {
        public EventGraphInputDescriptor(string inputId, Type valueType)
        {
            InputId = string.IsNullOrWhiteSpace(inputId)
                ? throw new ArgumentException("Event graph input identity is missing.", nameof(inputId))
                : inputId.Trim();
            if (!EventGraphValueKinds.TryGet(valueType, out EventGraphValueKind valueKind))
                throw new ArgumentException("Event graph input type is not supported.", nameof(valueType));
            ValueType = valueType;
            ValueKind = valueKind;
        }

        public string InputId { get; }
        public Type ValueType { get; }
        public EventGraphValueKind ValueKind { get; }
        public bool ReadOnly => true;
    }

    public sealed class EventGraphOutputDescriptor
    {
        public EventGraphOutputDescriptor(string variableId, Type valueType)
        {
            VariableId = string.IsNullOrWhiteSpace(variableId)
                ? throw new ArgumentException("Event graph output variable identity is missing.", nameof(variableId))
                : variableId.Trim();
            if (!EventGraphValueKinds.TryGet(valueType, out EventGraphValueKind valueKind))
                throw new ArgumentException("Event graph output type is not supported.", nameof(valueType));
            ValueType = valueType;
            ValueKind = valueKind;
        }

        public string VariableId { get; }
        public Type ValueType { get; }
        public EventGraphValueKind ValueKind { get; }
    }

    public sealed class EventGraphHostContract
    {
        readonly EventGraphInputDescriptor[] m_Inputs;
        readonly EventGraphOutputDescriptor[] m_Outputs;

        public EventGraphHostContract(
            string contractId,
            string revision,
            string updateEventId,
            IReadOnlyList<EventGraphInputDescriptor> inputs,
            IReadOnlyList<EventGraphOutputDescriptor> outputs)
        {
            ContractId = RequireIdentity(contractId, nameof(contractId));
            Revision = RequireIdentity(revision, nameof(revision));
            UpdateEventId = RequireIdentity(updateEventId, nameof(updateEventId));
            m_Inputs = (inputs ?? Array.Empty<EventGraphInputDescriptor>())
                .OrderBy(value => value?.InputId, StringComparer.Ordinal)
                .ToArray();
            m_Outputs = (outputs ?? Array.Empty<EventGraphOutputDescriptor>())
                .OrderBy(value => value?.VariableId, StringComparer.Ordinal)
                .ToArray();
            RequireUniqueInputs();
            RequireUniqueOutputs();
        }

        public string ContractId { get; }
        public string Revision { get; }
        public string UpdateEventId { get; }
        public IReadOnlyList<EventGraphInputDescriptor> Inputs => m_Inputs;
        public IReadOnlyList<EventGraphOutputDescriptor> Outputs => m_Outputs;

        public bool TryGetInput(string inputId, out EventGraphInputDescriptor descriptor)
        {
            for (int i = 0; i < m_Inputs.Length; i++)
            {
                if (string.Equals(m_Inputs[i].InputId, inputId, StringComparison.Ordinal))
                {
                    descriptor = m_Inputs[i];
                    return true;
                }
            }
            descriptor = null;
            return false;
        }

        public bool TryGetOutput(string variableId, out EventGraphOutputDescriptor descriptor)
        {
            for (int i = 0; i < m_Outputs.Length; i++)
            {
                if (string.Equals(m_Outputs[i].VariableId, variableId, StringComparison.Ordinal))
                {
                    descriptor = m_Outputs[i];
                    return true;
                }
            }
            descriptor = null;
            return false;
        }

        void RequireUniqueInputs()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_Inputs.Length; i++)
            {
                EventGraphInputDescriptor descriptor = m_Inputs[i] ??
                    throw new ArgumentException("Event graph input descriptor is missing.", nameof(m_Inputs));
                if (!ids.Add(descriptor.InputId))
                    throw new ArgumentException("Event graph input identity is duplicated.", nameof(m_Inputs));
            }
        }

        void RequireUniqueOutputs()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_Outputs.Length; i++)
            {
                EventGraphOutputDescriptor descriptor = m_Outputs[i] ??
                    throw new ArgumentException("Event graph output descriptor is missing.", nameof(m_Outputs));
                if (!ids.Add(descriptor.VariableId))
                    throw new ArgumentException("Event graph output identity is duplicated.", nameof(m_Outputs));
            }
        }

        static string RequireIdentity(string value, string parameterName) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Event graph contract identity is missing.", parameterName)
                : value.Trim();
    }

    public readonly struct EventGraphInvocationIdentity : IEquatable<EventGraphInvocationIdentity>
    {
        public EventGraphInvocationIdentity(string sourceId, ulong sequence)
        {
            SourceId = string.IsNullOrWhiteSpace(sourceId)
                ? throw new ArgumentException("Event graph invocation source identity is missing.", nameof(sourceId))
                : sourceId.Trim();
            if (sequence == 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            Sequence = sequence;
        }

        public string SourceId { get; }
        public ulong Sequence { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(SourceId) && Sequence != 0;

        public bool Equals(EventGraphInvocationIdentity other) =>
            string.Equals(SourceId, other.SourceId, StringComparison.Ordinal) && Sequence == other.Sequence;
        public override bool Equals(object obj) => obj is EventGraphInvocationIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(SourceId, Sequence);
        public override string ToString() => $"{SourceId}:{Sequence}";
    }

    public readonly struct EventGraphInvocationContext
    {
        public EventGraphInvocationContext(
            EventGraphInvocationIdentity identity,
            string eventId,
            float deltaSeconds)
        {
            if (!identity.IsValid)
                throw new ArgumentException("Event graph invocation identity is invalid.", nameof(identity));
            EventId = string.IsNullOrWhiteSpace(eventId)
                ? throw new ArgumentException("Event graph event identity is missing.", nameof(eventId))
                : eventId.Trim();
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            Identity = identity;
            DeltaSeconds = deltaSeconds;
        }

        public EventGraphInvocationIdentity Identity { get; }
        public string EventId { get; }
        public float DeltaSeconds { get; }
    }

    public interface IEventGraphHostContext
    {
        EventGraphInvocationContext Invocation { get; }
        bool TryRead(string inputId, EventGraphValueKind expectedKind, out EventGraphValue value);
    }

    public sealed class EventGraphExecutionFailure
    {
        public EventGraphExecutionFailure(
            string graphId,
            string eventId,
            string nodeId,
            string message,
            Exception exception)
        {
            GraphId = graphId ?? string.Empty;
            EventId = eventId ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
            Message = string.IsNullOrWhiteSpace(message)
                ? exception?.Message ?? "Event graph execution failed."
                : message.Trim();
            ExceptionType = exception?.GetType().FullName ?? string.Empty;
        }

        public string GraphId { get; }
        public string EventId { get; }
        public string NodeId { get; }
        public string Message { get; }
        public string ExceptionType { get; }

        public override string ToString() =>
            $"EventGraphFailure graph={GraphId} event={EventId} node={NodeId} message={Message}";
    }

    internal static class EventGraphEnumValue<T>
    {
        static readonly TypeCode s_Code = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T)));

        internal static int ToInt32(T value) => s_Code switch
        {
            TypeCode.SByte => Unsafe.As<T, sbyte>(ref value),
            TypeCode.Byte => Unsafe.As<T, byte>(ref value),
            TypeCode.Int16 => Unsafe.As<T, short>(ref value),
            TypeCode.UInt16 => Unsafe.As<T, ushort>(ref value),
            TypeCode.Int32 => Unsafe.As<T, int>(ref value),
            TypeCode.UInt32 => checked((int)Unsafe.As<T, uint>(ref value)),
            TypeCode.Int64 => checked((int)Unsafe.As<T, long>(ref value)),
            TypeCode.UInt64 => checked((int)Unsafe.As<T, ulong>(ref value)),
            _ => throw new InvalidOperationException("Unsupported enum storage.")
        };

        internal static T FromInt32(int value)
        {
            switch (s_Code)
            {
                case TypeCode.SByte: { sbyte v = checked((sbyte)value); return Unsafe.As<sbyte, T>(ref v); }
                case TypeCode.Byte: { byte v = checked((byte)value); return Unsafe.As<byte, T>(ref v); }
                case TypeCode.Int16: { short v = checked((short)value); return Unsafe.As<short, T>(ref v); }
                case TypeCode.UInt16: { ushort v = checked((ushort)value); return Unsafe.As<ushort, T>(ref v); }
                case TypeCode.Int32: return Unsafe.As<int, T>(ref value);
                case TypeCode.UInt32: { uint v = checked((uint)value); return Unsafe.As<uint, T>(ref v); }
                case TypeCode.Int64: { long v = value; return Unsafe.As<long, T>(ref v); }
                case TypeCode.UInt64: { ulong v = checked((ulong)value); return Unsafe.As<ulong, T>(ref v); }
                default: throw new InvalidOperationException("Unsupported enum storage.");
            }
        }
    }

    internal sealed class EventGraphVariableBuffer
    {
        EventGraphValue[] m_Published;
        EventGraphValue[] m_Writing;
        ulong m_Version;

        internal EventGraphVariableBuffer(int count)
        {
            m_Published = new EventGraphValue[count];
            m_Writing = new EventGraphValue[count];
        }

        internal void Write(int index, EventGraphValue value) => m_Writing[index] = value;
        internal EventGraphValue Read(int index) => m_Published[index];
        internal bool IsCurrent(ulong version) => version != 0 && version == m_Version;
        internal void Invalidate() => m_Version = checked(m_Version + 1);

        internal EventGraphVariableFrame Publish(EventGraphVariableContract contract,
            EventGraphInvocationIdentity invocation, ulong resetGeneration)
        {
            (m_Published, m_Writing) = (m_Writing, m_Published);
            Invalidate();
            return new EventGraphVariableFrame(contract, invocation, resetGeneration, this, m_Version);
        }
    }

    public readonly struct EventGraphVariableFrame
    {
        readonly EventGraphVariableBuffer m_Buffer;
        readonly ulong m_Version;

        internal EventGraphVariableFrame(
            EventGraphVariableContract contract,
            EventGraphInvocationIdentity invocation,
            ulong resetGeneration,
            EventGraphVariableBuffer buffer,
            ulong version)
        {
            Contract = contract ?? throw new ArgumentNullException(nameof(contract));
            if (!invocation.IsValid)
                throw new ArgumentException("Event graph frame invocation identity is invalid.", nameof(invocation));
            if (resetGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(resetGeneration));
            Invocation = invocation;
            ResetGeneration = resetGeneration;
            m_Buffer = buffer;
            m_Version = version;
        }

        public EventGraphVariableContract Contract { get; }
        public EventGraphInvocationIdentity Invocation { get; }
        public ulong ResetGeneration { get; }
        public bool IsValid => m_Buffer != null && m_Buffer.IsCurrent(m_Version);
        public string GraphId => Contract.GraphId;
        public string ContractRevision => Contract.Revision;
        public string LayoutId => Contract.Layout.LayoutId;

        public bool TryRead(string variableId, out EventGraphValue value)
        {
            if (IsValid && Contract.Layout.TryGet(variableId, out EventGraphVariableLayoutEntry entry))
            {
                value = m_Buffer.Read(entry.Index);
                return true;
            }
            value = default;
            return false;
        }

        public bool TryRead(EventGraphVariableReference reference, out EventGraphValue value)
        {
            if (IsValid && reference.IsValid &&
                string.Equals(reference.GraphId, GraphId, StringComparison.Ordinal))
                return TryRead(reference.VariableId, out value);
            value = default;
            return false;
        }

        public EventGraphValue Require(string variableId)
        {
            if (TryRead(variableId, out EventGraphValue value))
                return value;
            throw new InvalidOperationException(
                $"Event graph variable '{variableId}' is unavailable in frame '{Invocation}'.");
        }
    }

    public readonly struct NativeEventGraphExecutionResult
    {
        NativeEventGraphExecutionResult(
            bool succeeded,
            EventGraphVariableFrame frame,
            EventGraphExecutionFailure failure)
        {
            Succeeded = succeeded;
            Frame = frame;
            Failure = failure;
        }

        public bool Succeeded { get; }
        public EventGraphVariableFrame Frame { get; }
        public EventGraphExecutionFailure Failure { get; }

        internal static NativeEventGraphExecutionResult Success(EventGraphVariableFrame frame) =>
            new NativeEventGraphExecutionResult(
                true,
                frame.IsValid ? frame : throw new ArgumentException("Event graph frame is invalid.", nameof(frame)),
                null);

        internal static NativeEventGraphExecutionResult Failed(EventGraphExecutionFailure failure) =>
            new NativeEventGraphExecutionResult(
                false,
                default,
                failure ?? throw new ArgumentNullException(nameof(failure)));
    }
}
