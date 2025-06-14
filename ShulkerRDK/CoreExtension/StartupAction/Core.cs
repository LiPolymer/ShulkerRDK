using ShulkerRDK.Shared;

namespace ShulkerRDK.CoreExtension.StartupAction;

public static class Core {
    public static void Commander(string[] args,ShulkerContext sc) {
        Program.RunCommand(Tools.AliasResolver(Tools.GetSubGroup(args,1),sc.CommandAliases),sc);
    }
}