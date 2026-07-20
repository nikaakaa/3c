using System.Reflection;
using System.Runtime.Loader;
using Fantasy.Helper;

namespace ThirdPerson.Server.Host;

internal sealed class ProductHotfixModuleLoader : IDisposable
{
    ProductHotfixLoadContext? m_Context;

    public void Load(string publishRoot, IReadOnlyList<ServerHotfixModuleDescriptor> modules)
    {
        foreach (ServerHotfixModuleDescriptor module in modules)
        {
            RequireFile(publishRoot, module.AssemblyFileName);
            RequireFile(publishRoot, module.PdbFileName);
        }
        var candidate = new ProductHotfixLoadContext();
        try
        {
            foreach (ServerHotfixModuleDescriptor module in modules.OrderBy(value => value.LoadOrder))
            {
                using FileStream dll = File.OpenRead(Path.Combine(publishRoot, module.AssemblyFileName));
                using FileStream pdb = File.OpenRead(Path.Combine(publishRoot, module.PdbFileName));
                Assembly assembly = candidate.LoadFromStream(dll, pdb);
                assembly.EnsureLoaded();
            }
        }
        catch
        {
            candidate.Unload();
            throw;
        }
        ProductHotfixLoadContext? previous = m_Context;
        m_Context = candidate;
        if (previous != null)
        {
            previous.Unload();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public void Dispose()
    {
        ProductHotfixLoadContext? context = m_Context;
        m_Context = null;
        context?.Unload();
    }

    static void RequireFile(string root, string relativePath)
    {
        if (!File.Exists(Path.Combine(root, relativePath)))
            throw new FileNotFoundException("Product Hotfix module file is missing.", relativePath);
    }

    sealed class ProductHotfixLoadContext : AssemblyLoadContext
    {
        public ProductHotfixLoadContext() : base("ThirdPerson.Server.ProductHotfix", true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName) =>
            Default.Assemblies.FirstOrDefault(value =>
                string.Equals(value.GetName().Name, assemblyName.Name, StringComparison.Ordinal));
    }
}
