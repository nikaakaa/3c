using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.Diagnostics;

namespace ThirdPersonPerformance.Instrumentation.Editor
{
    internal static class PerformanceInstrumentationProbeScanner
    {
        public static List<PerformanceInstrumentationProbeTarget> Find(AssemblyDefinition assembly)
        {
            var targets = new List<PerformanceInstrumentationProbeTarget>();
            foreach (ModuleDefinition module in assembly.Modules)
            {
                foreach (TypeDefinition type in AllTypes(module.Types))
                {
                    if (type.IsInterface)
                        continue;
                    foreach (MethodDefinition method in type.Methods)
                    {
                        CustomAttribute probe = FindProbe(method);
                        if (probe != null)
                            targets.Add(new PerformanceInstrumentationProbeTarget(module, method, probe));
                    }
                }
            }
            return targets;
        }

        public static bool TryReadMetricId(CustomAttribute probe, out string metricId)
        {
            metricId = string.Empty;
            if (probe.ConstructorArguments.Count != 1)
                return false;
            metricId = probe.ConstructorArguments[0].Value as string;
            return !string.IsNullOrWhiteSpace(metricId);
        }

        public static bool ValidateMethod(
            MethodDefinition method,
            string assemblyName,
            List<DiagnosticMessage> diagnostics)
        {
            if (!method.HasBody || method.IsAbstract || method.IsPInvokeImpl || method.IsRuntime ||
                method.IsUnmanaged || method.IsConstructor || method.IsSpecialName ||
                method.ReturnType.IsByReference || method.HasGenericParameters ||
                HasFunctionPointer(method))
            {
                diagnostics.Add(Error(
                    assemblyName,
                    method,
                    "Performance probe targets an unsupported method form."));
                return false;
            }
            if (HasAsyncOrIteratorMarker(method))
            {
                diagnostics.Add(Error(
                    assemblyName,
                    method,
                    "Performance probe does not support async or iterator methods."));
                return false;
            }
            return true;
        }

        public static DiagnosticMessage Error(
            string assemblyName,
            MethodDefinition method,
            string message) =>
            new DiagnosticMessage
            {
                DiagnosticType = DiagnosticType.Error,
                File = assemblyName,
                Line = 0,
                Column = 0,
                MessageData = $"{message} Method={method.FullName}."
            };

        public static DiagnosticMessage Error(string assemblyName, string message) =>
            new DiagnosticMessage
            {
                DiagnosticType = DiagnosticType.Error,
                File = assemblyName,
                Line = 0,
                Column = 0,
                MessageData = message
            };

        static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types)
        {
            foreach (TypeDefinition type in types)
            {
                yield return type;
                foreach (TypeDefinition nested in AllTypes(type.NestedTypes))
                    yield return nested;
            }
        }

        static CustomAttribute FindProbe(MethodDefinition method)
        {
            for (int i = 0; i < method.CustomAttributes.Count; i++)
            {
                if (string.Equals(
                    method.CustomAttributes[i].AttributeType.FullName,
                    PerformanceInstrumentationWeaverConstants.ProbeAttributeName,
                    StringComparison.Ordinal))
                {
                    return method.CustomAttributes[i];
                }
            }
            return null;
        }

        static bool HasAsyncOrIteratorMarker(MethodDefinition method)
        {
            for (int i = 0; i < method.CustomAttributes.Count; i++)
            {
                string name = method.CustomAttributes[i].AttributeType.FullName;
                if (name == "System.Runtime.CompilerServices.AsyncStateMachineAttribute" ||
                    name == "System.Runtime.CompilerServices.IteratorStateMachineAttribute")
                {
                    return true;
                }
            }
            return false;
        }

        static bool HasFunctionPointer(MethodDefinition method)
        {
            if (method.ReturnType.IsFunctionPointer)
                return true;
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                if (method.Parameters[i].ParameterType.IsFunctionPointer)
                    return true;
            }
            for (int i = 0; i < method.Body.Variables.Count; i++)
            {
                if (method.Body.Variables[i].VariableType.IsFunctionPointer)
                    return true;
            }
            return false;
        }
    }
}
