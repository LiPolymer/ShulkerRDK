using System.Reflection;
using System.Runtime.Loader;

namespace ShulkerRDK;

public class ExtensionLoadContext(string extensionPath) : AssemblyLoadContext {
    readonly AssemblyDependencyResolver _resolver = new AssemblyDependencyResolver(extensionPath);
    readonly string _pluginDirectory = Path.GetDirectoryName(Path.GetFullPath(extensionPath))!;

    protected override Assembly Load(AssemblyName assemblyName) {
        string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName)
                               ?? ResolveFromPluginDirectory(assemblyName.Name + ".dll");
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null!;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) {
        string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName)
                              ?? ResolveFromPluginDirectory(unmanagedDllName)
                              ?? ResolveFromPluginDirectory(unmanagedDllName + ".dll");
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }

    // 插件包应包含全部依赖,deps.json 解析失败时在插件包文件夹内按文件名查找(含子文件夹,如 deps/)
    string? ResolveFromPluginDirectory(string fileName) {
        string direct = Path.Combine(_pluginDirectory,fileName);
        if (File.Exists(direct)) return direct;
        if (!Directory.Exists(_pluginDirectory)) return null;
        return Directory.EnumerateFiles(_pluginDirectory,fileName,SearchOption.AllDirectories).FirstOrDefault();
    }
}
