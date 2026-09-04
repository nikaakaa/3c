using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ThirdPersonPerformance.Instrumentation;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace ThirdPersonPerformance.Instrumentation.Editor
{
    public sealed class ThirdPersonPerformanceInstrumentationIlPostProcessor : ILPostProcessor
    {
        public override ILPostProcessor GetInstance() =>
            new ThirdPersonPerformanceInstrumentationIlPostProcessor();

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            if (compiledAssembly == null || compiledAssembly.Defines == null)
                return false;
            for (int i = 0; i < compiledAssembly.Defines.Length; i++)
            {
                if (compiledAssembly.Defines[i] == PerformanceInstrumentationIdentity.Define)
                    return Array.Exists(compiledAssembly.References, reference =>
                        string.Equals(Path.GetFileNameWithoutExtension(reference),
                            PerformanceInstrumentationIdentity.ContractsAssembly, StringComparison.Ordinal));
            }
            return false;
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            byte[] peData = compiledAssembly.InMemoryAssembly.PeData;
            byte[] pdbData = compiledAssembly.InMemoryAssembly.PdbData;
            var diagnostics = new List<DiagnosticMessage>();
            try
            {
                bool readSymbols = pdbData != null && pdbData.Length > 0;
                using var pdbStream = readSymbols ? new MemoryStream(pdbData, false) : null;
                var readerParameters = new ReaderParameters
                {
                    ReadSymbols = readSymbols,
                    SymbolStream = pdbStream,
                    ReadWrite = false,
                    InMemory = true,
                    SymbolReaderProvider = readSymbols
                        ? new PortablePdbReaderProvider()
                        : null
                };
                using (var peStream = new MemoryStream(peData, false))
                using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(peStream, readerParameters))
                {
                    List<PerformanceInstrumentationProbeTarget> targets =
                        PerformanceInstrumentationProbeScanner.Find(assembly);
                    if (targets.Count == 0)
                        return Original(peData, pdbData, diagnostics);
                    if (!PerformanceInstrumentationBuildInput.TryLoad(
                        out PerformanceInstrumentationBuildInput input,
                        out string inputError))
                    {
                        diagnostics.Add(PerformanceInstrumentationProbeScanner.Error(
                            compiledAssembly.Name,
                            inputError));
                        return Original(peData, pdbData, diagnostics);
                    }

                    var points = new List<PerformanceInstrumentationPointDescriptor>(targets.Count);
                    var pointIds = new HashSet<ulong>();
                    for (int i = 0; i < targets.Count; i++)
                    {
                        PerformanceInstrumentationProbeTarget target = targets[i];
                        MethodDefinition method = target.Method;
                        if (!input.AllowsAssembly(compiledAssembly.Name))
                        {
                            diagnostics.Add(PerformanceInstrumentationProbeScanner.Error(
                                compiledAssembly.Name,
                                method,
                                "Performance probe assembly is not included in the explicit build input."));
                            continue;
                        }
                        if (!PerformanceInstrumentationProbeScanner.TryReadMetricId(
                            target.Probe,
                            out string metricId))
                        {
                            diagnostics.Add(PerformanceInstrumentationProbeScanner.Error(
                                compiledAssembly.Name,
                                method,
                                "Performance probe requires one non-empty MetricId."));
                            continue;
                        }
                        if (!input.TryGetMetric(
                            metricId,
                            out PerformanceInstrumentationMetricDescriptor metric))
                        {
                            diagnostics.Add(PerformanceInstrumentationProbeScanner.Error(
                                compiledAssembly.Name,
                                method,
                                $"Performance probe MetricId '{metricId}' is not present in the build catalog."));
                            continue;
                        }
                        if (!PerformanceInstrumentationProbeScanner.ValidateMethod(
                            method,
                            compiledAssembly.Name,
                            diagnostics))
                        {
                            continue;
                        }

                        ulong pointId = PerformanceInstrumentationWeaverConstants.Hash64(
                            assembly.Name.Name + "|" + method.DeclaringType.FullName + "|" +
                            method.FullName + "|" + metric.MetricId);
                        if (!pointIds.Add(pointId))
                        {
                            diagnostics.Add(PerformanceInstrumentationProbeScanner.Error(
                                compiledAssembly.Name,
                                method,
                                $"Performance probe PointId collision for MetricId '{metric.MetricId}'."));
                            continue;
                        }
                        try
                        {
                            points.Add(PerformanceInstrumentationIlRewriter.Rewrite(
                                target.Module,
                                method,
                                metric.MetricId,
                                metric.ProfilerName,
                                input.Mode));
                        }
                        catch (Exception exception)
                        {
                            diagnostics.Add(PerformanceInstrumentationProbeScanner.Error(
                                compiledAssembly.Name,
                                method,
                                $"Performance probe weaving failed: {exception}"));
                        }
                    }

                    if (HasErrors(diagnostics) || points.Count == 0)
                        return Original(peData, pdbData, diagnostics);

                    using (var outputPe = new MemoryStream())
                    using (var outputPdb = readSymbols ? new MemoryStream() : null)
                    {
                        var writerParameters = new WriterParameters
                        {
                            WriteSymbols = readSymbols,
                            SymbolStream = outputPdb,
                            SymbolWriterProvider = readSymbols
                                ? new PortablePdbWriterProvider()
                                : null
                        };
                        assembly.Write(outputPe, writerParameters);
                        PerformanceInstrumentationPointManifest.Write(
                            input.ManifestDirectory,
                            compiledAssembly.Name,
                            input,
                            points);
                        return new ILPostProcessResult(
                            new InMemoryAssembly(
                                outputPe.ToArray(),
                                outputPdb?.ToArray() ?? pdbData),
                            diagnostics);
                    }
                }
            }
            catch (Exception exception)
            {
                diagnostics.Add(PerformanceInstrumentationProbeScanner.Error(
                    compiledAssembly.Name,
                    $"Performance instrumentation post-processing failed: {exception.Message}"));
                return Original(peData, pdbData, diagnostics);
            }
        }

        static bool HasErrors(List<DiagnosticMessage> diagnostics)
        {
            for (int i = 0; i < diagnostics.Count; i++)
            {
                if (diagnostics[i].DiagnosticType == DiagnosticType.Error)
                    return true;
            }
            return false;
        }

        static ILPostProcessResult Original(
            byte[] peData,
            byte[] pdbData,
            List<DiagnosticMessage> diagnostics) =>
            new ILPostProcessResult(new InMemoryAssembly(peData, pdbData), diagnostics);
    }
}
