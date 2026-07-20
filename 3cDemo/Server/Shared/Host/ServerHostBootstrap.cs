using Fantasy;
using Fantasy.Helper;
using NLog;

namespace ThirdPerson.Server.Host;

public static class ServerHostBootstrap
{
    const string ServerLogRootEnvironmentVariable = "THIRDPERSON_SERVER_LOG_ROOT";

    public static async Task RunAsync(ServerHostProductDefinition product)
    {
        ConfigureRuntimeLogging();
        string publishRoot = Path.GetFullPath(AppContext.BaseDirectory);
        string actualExecutable = Path.GetFileName(Environment.ProcessPath ?? string.Empty);
        if (!string.Equals(actualExecutable, product.ExecutableName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Server product expected executable '{product.ExecutableName}' but started '{actualExecutable}'.");
        ServerProductBuildManifestReader.LoadAndValidate(publishRoot, product);
        product.InstallProductRuntime();
        foreach (ServerEntityModuleDescriptor module in product.EntityModules)
            module.MarkerType.Assembly.EnsureLoaded();
        using var loader = new ProductHotfixModuleLoader();
        loader.Load(publishRoot, product.HotfixModules);
        var logger = new Fantasy.NLog(product.ProductId);
        await Fantasy.Platform.Net.Entry.Start(logger);
    }

    static void ConfigureRuntimeLogging()
    {
        string? configuredRoot = Environment.GetEnvironmentVariable(ServerLogRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredRoot) || !Path.IsPathFullyQualified(configuredRoot))
            throw new InvalidOperationException($"An absolute '{ServerLogRootEnvironmentVariable}' is required.");
        string logRoot = Path.GetFullPath(configuredRoot);
        Directory.CreateDirectory(logRoot);
        var configuration = LogManager.Configuration ??
            throw new InvalidOperationException("NLog configuration is unavailable.");
        configuration.Variables["serverLogRoot"] = logRoot.Replace('\\', '/');
        LogManager.ReconfigExistingLoggers();
    }
}
