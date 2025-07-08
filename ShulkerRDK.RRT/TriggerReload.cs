using ShulkerRDK.Shared;

namespace ShulkerRDK.RRT;

public static class TriggerReload {
    public static string? Method(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("RRT");
        if (!Tools.CheckParamLength(args,1)) return null;
        if (!Directory.Exists(args[1])) {
            ec.Logger.WriteLine("&7无效的目录",Terminal.MessageType.Error);
            return null;
        }
        ec.Logger.WriteLine("&7触发重载...");
        File.WriteAllText(Path.Combine(args[1],"reload.flag"),string.Empty);
        return null;
    }
}