using System;
using System.Globalization;
using System.IO;
using ThirdPersonSimulation.DotRecast;

namespace ThirdPersonSimulation.NavigationBuildTool
{
    static class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length != 25 || !string.Equals(args[0], "build", StringComparison.Ordinal))
                    throw new ArgumentException("Usage: build <navgeom> <navsurface> <cellSize> <cellHeight> <agentHeight> <agentRadius> <agentClimb> <agentSlope> <regionMin> <regionMerge> <edgeLength> <edgeError> <vertsPerPoly> <detailDistance> <detailError> <extentX> <extentY> <extentZ> <projectionTolerance> <heightTolerance> <maxDisplacement> <boundaryInset> <maxVisited> <includeFlags>.");
                string geometryPath = Path.GetFullPath(args[1]);
                string surfacePath = Path.GetFullPath(args[2]);
                NavigationGeometryArtifact geometry = NavigationGeometryArtifactCodec.Read(File.ReadAllBytes(geometryPath));
                var build = new DotRecastBuildProfile(
                    Double(args[3]), Double(args[4]), Double(args[5]), Double(args[6]), Double(args[7]), Double(args[8]),
                    Int(args[9]), Int(args[10]), Double(args[11]), Double(args[12]), Int(args[13]), Double(args[14]), Double(args[15]));
                var query = new DotRecastQueryProfile(
                    Double(args[16]), Double(args[17]), Double(args[18]), Double(args[19]), Double(args[20]), Double(args[21]), Double(args[22]),
                    Int(args[23]), Int(args[24]), 0, new[] { 1d });
                NavigationSurfaceArtifact surface = NavigationSurfaceBuilder.Build(
                    geometry,
                    build,
                    query,
                    new[] { new NavigationAreaDefinition(0, "Ground") });
                byte[] bytes = NavigationSurfaceArtifactCodec.Write(surface);
                Directory.CreateDirectory(Path.GetDirectoryName(surfacePath) ?? throw new InvalidOperationException("Surface output directory is missing."));
                File.WriteAllBytes(surfacePath, bytes);
                NavigationSurfaceArtifact verified = NavigationSurfaceArtifactCodec.Read(File.ReadAllBytes(surfacePath));
                Console.WriteLine($"{verified.ContentHash.Value} {bytes.Length}");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        static double Double(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        static int Int(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}
