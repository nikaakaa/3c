using ThirdPersonSimulation;
using System;
using System.IO;

namespace ThirdPersonSimulation.Fixed
{
	public readonly struct AbilityStateValue
	{
		readonly BlackboardOwnerToken m_BlackboardOwnerToken;
		readonly BlackboardWriteStamp m_BlackboardWriteStamp;

		AbilityStateValue(
			ProgramStateValueKind kind,
			bool boolean,
			int int32,
			ulong uint64,
			FixedScalar scalar,
			FixedVector2 vector2,
			FixedVector3 vector3,
			FixedYaw yaw,
			string identity,
			BlackboardOwnerToken blackboardOwnerToken,
			BlackboardWriteStamp blackboardWriteStamp,
            SimulationActionTargetSnapshot actionTargetSnapshot)
		{
			Kind = kind;
			Boolean = boolean;
			Int32 = int32;
			UInt64 = uint64;
			Scalar = scalar;
			Vector2 = vector2;
			Vector3 = vector3;
			Yaw = yaw;
			Identity = identity ?? string.Empty;
			m_BlackboardOwnerToken = blackboardOwnerToken;
			m_BlackboardWriteStamp = blackboardWriteStamp;
			ActionTargetSnapshot = actionTargetSnapshot;
		}

		public ProgramStateValueKind Kind { get; }
		public bool Boolean { get; }
		public int Int32 { get; }
		public ulong UInt64 { get; }
		public FixedScalar Scalar { get; }
		public FixedVector2 Vector2 { get; }
		public FixedVector3 Vector3 { get; }
		public FixedYaw Yaw { get; }
		public string Identity { get; }
		public BlackboardOwnerToken BlackboardOwnerToken => Require(ProgramStateValueKind.BlackboardOwnerToken, m_BlackboardOwnerToken);
		public BlackboardWriteStamp BlackboardWriteStamp => Require(ProgramStateValueKind.BlackboardWriteStamp, m_BlackboardWriteStamp);
		public SimulationActionTargetSnapshot ActionTargetSnapshot { get; }
		public bool Equals(AbilityStateValue other) =>
			Kind == other.Kind &&
			Boolean == other.Boolean &&
			Int32 == other.Int32 &&
			UInt64 == other.UInt64 &&
			Scalar.Equals(other.Scalar) &&
			Vector2.Equals(other.Vector2) &&
			Vector3.Equals(other.Vector3) &&
			Yaw.Equals(other.Yaw) &&
			string.Equals(Identity, other.Identity, StringComparison.Ordinal) &&
			m_BlackboardOwnerToken.Equals(other.m_BlackboardOwnerToken) &&
			m_BlackboardWriteStamp.Equals(other.m_BlackboardWriteStamp) &&
			ActionTargetSnapshot.Equals(other.ActionTargetSnapshot);
		public static AbilityStateValue FromBoolean(bool value) => Create(ProgramStateValueKind.Boolean, boolean: value);
		public static AbilityStateValue FromInt32(int value) => Create(ProgramStateValueKind.Int32, int32: value);
		public static AbilityStateValue FromUInt64(ulong value) => Create(ProgramStateValueKind.UInt64, uint64: value);
		public static AbilityStateValue FromScalar(FixedScalar value) => Create(ProgramStateValueKind.Scalar, scalar: value);
		public static AbilityStateValue FromVector2(FixedVector2 value) => Create(ProgramStateValueKind.Vector2, vector2: value);
		public static AbilityStateValue FromVector3(FixedVector3 value) => Create(ProgramStateValueKind.Vector3, vector3: value);
		public static AbilityStateValue FromYaw(FixedYaw value) => Create(ProgramStateValueKind.Yaw, yaw: value);
		public static AbilityStateValue FromIdentity(string value) => Create(ProgramStateValueKind.Identity, identity: value);
		public static AbilityStateValue FromBlackboardOwnerToken(BlackboardOwnerToken value) => Create(ProgramStateValueKind.BlackboardOwnerToken, blackboardOwnerToken: value);
		public static AbilityStateValue FromBlackboardWriteStamp(BlackboardWriteStamp value) => Create(ProgramStateValueKind.BlackboardWriteStamp, blackboardWriteStamp: value);
		public static AbilityStateValue FromActionTargetSnapshot(SimulationActionTargetSnapshot value) => Create(ProgramStateValueKind.ActionTargetSnapshot, actionTargetSnapshot: value);
		public static AbilityStateValue Default(ProgramStateValueKind kind)
		{
			return kind switch
			{
				ProgramStateValueKind.Boolean => FromBoolean(false),
				ProgramStateValueKind.Int32 => FromInt32(0),
				ProgramStateValueKind.UInt64 => FromUInt64(0),
				ProgramStateValueKind.Scalar => FromScalar(FixedScalar.Zero),
				ProgramStateValueKind.Vector2 => FromVector2(FixedVector2.Zero),
				ProgramStateValueKind.Vector3 => FromVector3(FixedVector3.Zero),
				ProgramStateValueKind.Yaw => FromYaw(FixedYaw.Zero),
				ProgramStateValueKind.Identity => FromIdentity(string.Empty),
				ProgramStateValueKind.BlackboardOwnerToken => FromBlackboardOwnerToken(default),
				ProgramStateValueKind.BlackboardWriteStamp => FromBlackboardWriteStamp(default),
				ProgramStateValueKind.ActionTargetSnapshot => FromActionTargetSnapshot(SimulationActionTargetSnapshot.None),
				_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
			};
		}

		public static AbilityStateValue FromConstant(ProgramConstant constant, ProgramStateValueKind expectedKind)
		{
			if (constant == null)
				throw new ArgumentNullException(nameof(constant));
			AbilityStateValue value = expectedKind switch
			{
				ProgramStateValueKind.Boolean when constant.Kind == ProgramConstantKind.Boolean => FromBoolean(constant.Boolean),
				ProgramStateValueKind.Int32 when constant.Kind == ProgramConstantKind.Int32 => FromInt32(constant.Int32),
				ProgramStateValueKind.UInt64 when constant.Kind == ProgramConstantKind.UInt64 => FromUInt64(constant.UInt64),
				ProgramStateValueKind.Scalar when constant.Kind == ProgramConstantKind.Scalar => FromScalar(constant.Scalar),
				ProgramStateValueKind.Vector2 when constant.Kind == ProgramConstantKind.Vector2 => FromVector2(constant.Vector2),
				ProgramStateValueKind.Vector3 when constant.Kind == ProgramConstantKind.Vector3 => FromVector3(constant.Vector3),
				ProgramStateValueKind.Yaw when constant.Kind == ProgramConstantKind.Yaw => FromYaw(constant.Yaw),
				ProgramStateValueKind.Identity when constant.Kind == ProgramConstantKind.String => FromIdentity(constant.Text),
				ProgramStateValueKind.ActionTargetSnapshot when constant.Kind == ProgramConstantKind.Bytes =>
					FromActionTargetSnapshot(SimulationActionTargetSnapshotCodec.Read(constant.Bytes.ToArray())),
				_ => throw new InvalidDataException(
					$"Constant '{constant.Identity}' kind '{constant.Kind}' does not match state kind '{expectedKind}'.")
			};
			return value;
		}

		static AbilityStateValue Create(
			ProgramStateValueKind kind,
			bool boolean = default,
			int int32 = default,
			ulong uint64 = default,
			FixedScalar scalar = default,
			FixedVector2 vector2 = default,
			FixedVector3 vector3 = default,
			FixedYaw yaw = default,
			string identity = null,
			BlackboardOwnerToken blackboardOwnerToken = default,
			BlackboardWriteStamp blackboardWriteStamp = default,
			SimulationActionTargetSnapshot actionTargetSnapshot = default)
		{
			return new AbilityStateValue(
				kind,
				boolean,
				int32,
				uint64,
				scalar,
				vector2,
				vector3,
				yaw,
				identity,
				blackboardOwnerToken,
				blackboardWriteStamp,
				actionTargetSnapshot);
		}

		T Require<T>(ProgramStateValueKind expected, T value)
		{
			if (Kind != expected)
				throw new InvalidOperationException($"State value is '{Kind}', expected '{expected}'.");
			return value;
		}
	}
}
