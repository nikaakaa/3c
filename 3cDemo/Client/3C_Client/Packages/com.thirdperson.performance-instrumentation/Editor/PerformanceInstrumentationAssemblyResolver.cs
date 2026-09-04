using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace ThirdPersonPerformance.Instrumentation.Editor
{
    internal sealed class PerformanceInstrumentationAssemblyResolver : IAssemblyResolver
    {
        readonly Dictionary<string, string> m_Paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, AssemblyDefinition> m_Assemblies = new Dictionary<string, AssemblyDefinition>(StringComparer.OrdinalIgnoreCase);
        AssemblyDefinition m_Current;

        public PerformanceInstrumentationAssemblyResolver(string[] references)
        {
            foreach (string reference in references)
                m_Paths.Add(Path.GetFileNameWithoutExtension(reference), reference);
        }

        public void SetCurrent(AssemblyDefinition assembly) => m_Current = assembly;

        public AssemblyDefinition Resolve(AssemblyNameReference name) =>
            Resolve(name, new ReaderParameters());

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            if (m_Current != null && string.Equals(name.Name, m_Current.Name.Name, StringComparison.OrdinalIgnoreCase))
                return m_Current;
            if (m_Assemblies.TryGetValue(name.Name, out AssemblyDefinition assembly))
                return assembly;
            if (!m_Paths.TryGetValue(name.Name, out string path))
                throw new AssemblyResolutionException(name);
            parameters.AssemblyResolver = this;
            assembly = AssemblyDefinition.ReadAssembly(path, parameters);
            m_Assemblies.Add(name.Name, assembly);
            return assembly;
        }

        public void Dispose()
        {
            foreach (AssemblyDefinition assembly in m_Assemblies.Values)
                assembly.Dispose();
            m_Assemblies.Clear();
            m_Current = null;
        }
    }
}
