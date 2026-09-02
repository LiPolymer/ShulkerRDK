namespace ShulkerRDK.Shared;

public static class StaticContext {
    public static class Paths {
        public static string ProjectConfig { get => "./shulker/proj.json"; }
        public static string LocalConfig { get => "./shulker/local/shulker.json"; }
        public static string ExtensionsPath { get => "./shulker/local/extensions"; }
        public static string LegacyExtensionsPath { get => "./shulker/extensions"; }
        public static string LibsPath { get => "./shulker/local/libs"; }
    }
}