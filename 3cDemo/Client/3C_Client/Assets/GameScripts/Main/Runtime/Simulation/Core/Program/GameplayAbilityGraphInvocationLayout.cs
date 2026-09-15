using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class GameplayAbilityGraphInvocationLayout
    {
        readonly Dictionary<string, ProgramSourceMapEntry> m_Invocations = new(StringComparer.Ordinal);
        readonly Dictionary<string, int> m_GenerationSlots = new(StringComparer.Ordinal);
        readonly int[] m_OperationGenerationSlots;
        readonly int[] m_OperationParentGenerationSlots;

        public GameplayAbilityGraphInvocationLayout(IReadOnlyList<ProgramSourceMapEntry> sources, int operationCount,
            Func<OperationHandle, int> generationSlot)
        {
            m_OperationGenerationSlots = new int[operationCount];
            m_OperationParentGenerationSlots = new int[operationCount];
            Array.Fill(m_OperationGenerationSlots, -1);
            Array.Fill(m_OperationParentGenerationSlots, -1);
            foreach (ProgramSourceMapEntry source in sources)
            {
                if (source.TargetKind != ProgramSourceTargetKind.GraphInvocation)
                    continue;
                if (!m_Invocations.TryAdd(source.GraphInvocationPath, source))
                    throw new InvalidOperationException("图调用路径重复。");
                int slot = generationSlot(new OperationHandle(source.TargetIndex));
                if (slot < 0)
                    throw new InvalidOperationException("图调用缺少已声明的生命周期代次槽。");
                m_GenerationSlots.Add(source.GraphInvocationPath, slot);
            }
            foreach (ProgramSourceMapEntry source in sources)
            {
                if (source.TargetKind != ProgramSourceTargetKind.Operation || string.IsNullOrEmpty(source.GraphInvocationPath))
                    continue;
                if (!m_GenerationSlots.TryGetValue(source.GraphInvocationPath, out int slot))
                    throw new InvalidOperationException("操作引用了未声明的图调用。");
                m_OperationGenerationSlots[source.TargetIndex] = slot;
                string parent = m_Invocations[source.GraphInvocationPath].ParentInvocationPath;
                if (!string.IsNullOrEmpty(parent))
                    m_OperationParentGenerationSlots[source.TargetIndex] = m_GenerationSlots[parent];
            }
            foreach (ProgramSourceMapEntry source in sources)
                if (source.TargetKind == ProgramSourceTargetKind.OperationPort &&
                    m_GenerationSlots[source.SourceInvocationPath] != m_OperationGenerationSlots[source.TargetIndex])
                    throw new InvalidOperationException("端口来源与操作不属于同一执行代次。");
        }

        public int GenerationSlot(OperationHandle operation) => m_OperationGenerationSlots[operation.Value];
        public int ParentGenerationSlot(OperationHandle operation) => m_OperationParentGenerationSlots[operation.Value];
        public ProgramSourceMapEntry Invocation(string path) => m_Invocations[path];
    }
}
