using System;
using System.Globalization;
using System.IO;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.FixedProgramBuildTool
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            if (args == null || args.Length != 2)
            {
                Console.Error.WriteLine("Usage: ThirdPersonSimulation.FixedProgramBuildTool <input.csir> <output.fixed-program>");
                return 2;
            }

            try
            {
                Build(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]));
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
                return 1;
            }
        }

        static void Build(string inputPath, string outputPath)
        {
            byte[] semanticBytes = File.ReadAllBytes(inputPath);
            CharacterGameplaySemanticIrArtifactHeader header = CharacterGameplaySemanticIrCodec.ReadArtifactHeader(semanticBytes);
            ValidatedSemanticIrArtifact artifact = CharacterGameplaySemanticIrCodec.ReadValidatedArtifact(
                semanticBytes,
                new SemanticIrLoadExpectation(
                    header.ProgramId,
                    header.CompilerVersion,
                    header.OperationSetVersion,
                    header.TickRate,
                    header.SourceRevision,
                    header.SemanticHash));

            FixedProgramArtifactCompilationResult result = FixedCharacterSimulationTargetCompiler.CompileArtifact(artifact);
            byte[] programBytes = result.CopyCanonicalBytes();
            CharacterSimulationProgram loaded = result.Program;

            string directory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("Output directory is absent.");
            Directory.CreateDirectory(directory);
            string temporaryPath = outputPath + ".tmp";
            File.WriteAllBytes(temporaryPath, programBytes);
            File.Move(temporaryPath, outputPath, true);

            Console.WriteLine($"ProgramId: {loaded.Manifest.ProgramId.Value}");
            Console.WriteLine($"SemanticHash: {loaded.Manifest.SemanticHash}");
            Console.WriteLine($"NumericProfile: {loaded.Manifest.NumericProfile.Id.Value}");
            Console.WriteLine($"TargetAbiVersion: {loaded.Manifest.NumericProfile.AbiVersion.Value.ToString(CultureInfo.InvariantCulture)}");
            Console.WriteLine($"ProgramHash: {loaded.ProgramHash}");
            Console.WriteLine($"LayoutHash: {loaded.LayoutHash}");
            Console.WriteLine($"Operations: {loaded.Operations.Count.ToString(CultureInfo.InvariantCulture)}");
            Console.WriteLine($"StateSlots: {loaded.StateSlots.Count.ToString(CultureInfo.InvariantCulture)}");
            Console.WriteLine($"Conversions: {result.Conversions.Count.ToString(CultureInfo.InvariantCulture)}");
            Console.WriteLine($"Bytes: {programBytes.Length.ToString(CultureInfo.InvariantCulture)}");
            Console.WriteLine($"Output: {outputPath}");
        }
    }
}
