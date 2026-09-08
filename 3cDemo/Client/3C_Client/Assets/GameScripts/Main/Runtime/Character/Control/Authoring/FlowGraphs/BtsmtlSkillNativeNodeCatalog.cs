#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FlowCanvas.Nodes;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Control.Authoring
{
    public sealed class BtsmtlSkillNativeNodeContract
    {
        internal BtsmtlSkillNativeNodeContract(Type nodeType, string kind, SimulationOperationCode code,
            int variant, IReadOnlyDictionary<string, string> inputs)
        {
            NodeType = nodeType;
            Kind = kind;
            Code = code;
            Variant = variant;
            Inputs = inputs;
        }

        public Type NodeType { get; }
        public string Kind { get; }
        public SimulationOperationCode Code { get; }
        public int Variant { get; }
        public IReadOnlyDictionary<string, string> Inputs { get; }

        public string Input(string nativeId) => Inputs.TryGetValue(nativeId, out string id)
            ? id : throw new InvalidOperationException($"Native skill input '{nativeId}' is not registered for '{Kind}'.");

        public string Output(string nativeId) => nativeId == "Value"
            ? Code == SimulationOperationCode.Compare ? "m_Result" : "m_Output"
            : throw new InvalidOperationException($"Native skill output '{nativeId}' is not registered for '{Kind}'.");
    }

    public static class BtsmtlSkillNativeNodeCatalog
    {
        static readonly IReadOnlyDictionary<Type, BtsmtlSkillNativeNodeContract> s_Nodes = Create();

        public static IEnumerable<BtsmtlSkillNativeNodeContract> All => s_Nodes.Values;
        public static bool TryGet(Type type, out BtsmtlSkillNativeNodeContract contract) => s_Nodes.TryGetValue(type, out contract);

        static IReadOnlyDictionary<Type, BtsmtlSkillNativeNodeContract> Create()
        {
            var nodes = new Dictionary<Type, BtsmtlSkillNativeNodeContract>();
            var boolean = Ports(("a", "m_Input1"), ("b", "m_Input2"));
            var comparison = Ports(("a", "m_InputValue1"), ("b", "m_InputValue2"));
            Add<AND>("and", SimulationOperationCode.And, boolean);
            Add<OR>("or", SimulationOperationCode.Or, boolean);
            Add<NOT>("not", SimulationOperationCode.Not, Ports(("value", "m_Input")));
            Add<FloatEqual>("compare.number.equal", SimulationOperationCode.Compare, comparison, 0);
            Add<FloatNotEqual>("compare.number.not-equal", SimulationOperationCode.Compare, comparison, 1);
            Add<FloatLessThan>("compare.number.less", SimulationOperationCode.Compare, comparison, 2);
            Add<FloatLessEqualThan>("compare.number.less-equal", SimulationOperationCode.Compare, comparison, 3);
            Add<FloatGreaterEqualThan>("compare.number.greater-equal", SimulationOperationCode.Compare, comparison, 4);
            Add<FloatGreaterThan>("compare.number.greater", SimulationOperationCode.Compare, comparison, 5);
            Add<IntegerEqual>("compare.int32.equal", SimulationOperationCode.Compare, comparison, 0);
            Add<IntegerNotEqual>("compare.int32.not-equal", SimulationOperationCode.Compare, comparison, 1);
            Add<IntegerLessThan>("compare.int32.less", SimulationOperationCode.Compare, comparison, 2);
            Add<IntegerLessEqualThan>("compare.int32.less-equal", SimulationOperationCode.Compare, comparison, 3);
            Add<IntegerGreaterEqualThan>("compare.int32.greater-equal", SimulationOperationCode.Compare, comparison, 4);
            Add<IntegerGreaterThan>("compare.int32.greater", SimulationOperationCode.Compare, comparison, 5);
            return new ReadOnlyDictionary<Type, BtsmtlSkillNativeNodeContract>(nodes);

            void Add<T>(string kind, SimulationOperationCode code, IReadOnlyDictionary<string, string> inputs, int variant = 0) where T : SimplexNode
            {
                Type type = typeof(SimplexNodeWrapper<T>);
                nodes.Add(type, new BtsmtlSkillNativeNodeContract(type, kind, code, variant, inputs));
            }
        }

        static IReadOnlyDictionary<string, string> Ports(params (string Native, string Compiled)[] values)
        {
            var ports = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var value in values)
                ports.Add(value.Native, value.Compiled);
            return new ReadOnlyDictionary<string, string>(ports);
        }
    }
}
#endif
