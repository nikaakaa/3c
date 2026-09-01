using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPerson.GeneratedDiagnosticSampling
{
    public sealed class DiagnosticCapabilityCompilationClosureDescriptor
    {
        public DiagnosticCapabilityCompilationClosureDescriptor(
            string capabilityId,
            int capabilityRevision,
            IEnumerable<string> assemblyNames,
            IEnumerable<string> scriptingDefines)
        {
            CapabilityId = DiagnosticIdentity.RequireId(capabilityId, nameof(capabilityId));
            CapabilityRevision = DiagnosticIdentity.RequireRevision(
                capabilityRevision,
                nameof(capabilityRevision));
            AssemblyNames = NormalizeNames(assemblyNames, nameof(assemblyNames));
            ScriptingDefines = NormalizeNames(scriptingDefines, nameof(scriptingDefines));
            if (AssemblyNames.Count == 0 || ScriptingDefines.Count == 0)
                throw new ArgumentException("Diagnostic compilation closure is incomplete.");
            Identity = DiagnosticIdentity.Hash(new[]
            {
                CapabilityId,
                CapabilityRevision.ToString(),
                string.Join(",", AssemblyNames),
                string.Join(",", ScriptingDefines)
            });
        }

        public string CapabilityId { get; }
        public int CapabilityRevision { get; }
        public IReadOnlyList<string> AssemblyNames { get; }
        public IReadOnlyList<string> ScriptingDefines { get; }
        public string Identity { get; }

        static IReadOnlyList<string> NormalizeNames(
            IEnumerable<string> values,
            string parameterName)
        {
            if (values == null)
                return Array.Empty<string>();
            return values
                .Select(value => DiagnosticIdentity.RequireText(value, parameterName))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public sealed class DiagnosticCompilationClosureProof
    {
        public DiagnosticCompilationClosureProof(
            DiagnosticCapabilitySet capabilitySet,
            IEnumerable<DiagnosticCapabilityCompilationClosureDescriptor> closures)
        {
            CapabilitySet = capabilitySet ?? throw new ArgumentNullException(nameof(capabilitySet));
            DiagnosticCapabilityCompilationClosureDescriptor[] available =
                DiagnosticIdentity.NormalizeDescriptors(
                    closures,
                    value => value.CapabilityId,
                    nameof(closures))
                .ToArray();
            var included = new List<DiagnosticCapabilityCompilationClosureDescriptor>();
            var excluded = new List<DiagnosticCapabilityCompilationClosureDescriptor>();
            foreach (DiagnosticCapabilityBuildDescriptor capability in capabilitySet.Capabilities)
            {
                DiagnosticCapabilityCompilationClosureDescriptor closure = available
                    .SingleOrDefault(value => string.Equals(
                        value.CapabilityId,
                        capability.CapabilityId,
                        StringComparison.Ordinal));
                if (closure == null || closure.CapabilityRevision != capability.CapabilityRevision)
                {
                    throw new ArgumentException(
                        $"Diagnostic compilation closure '{capability.CapabilityId}' is missing.",
                        nameof(closures));
                }
                if (capability.Mode == DiagnosticCapabilityMode.Capture)
                    included.Add(closure);
                else
                    excluded.Add(closure);
            }
            if (available.Length != capabilitySet.Capabilities.Count)
                throw new ArgumentException("Diagnostic compilation closures do not match the capability set.", nameof(closures));
            Included = included;
            Excluded = excluded;
            AssemblyNames = Included
                .SelectMany(value => value.AssemblyNames)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            ScriptingDefines = Included
                .SelectMany(value => value.ScriptingDefines)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Identity = DiagnosticIdentity.Hash(new[] { capabilitySet.Identity }
                .Concat(Included.Select(value => "capture|" + value.Identity))
                .Concat(Excluded.Select(value => "disabled|" + value.Identity)));
        }

        public DiagnosticCapabilitySet CapabilitySet { get; }
        public IReadOnlyList<DiagnosticCapabilityCompilationClosureDescriptor> Included { get; }
        public IReadOnlyList<DiagnosticCapabilityCompilationClosureDescriptor> Excluded { get; }
        public IReadOnlyList<string> AssemblyNames { get; }
        public IReadOnlyList<string> ScriptingDefines { get; }
        public string Identity { get; }
    }
}
