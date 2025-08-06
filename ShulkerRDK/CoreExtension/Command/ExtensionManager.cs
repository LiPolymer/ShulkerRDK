using System.ComponentModel;
using ShulkerRDK.Shared;

namespace ShulkerRDK.CoreExtension.Command;

public static class ExtensionManager {
    static Dictionary<string,Action<string[]>>? _subCommands;
    static ChainedTerminal? _myTerminal;
    [Description("扩展管理器")]
    public static void Command(string[] args,ShulkerContext shulkerContext) {
        _myTerminal ??= new ChainedTerminal("&6ExtMgr");
        if (_subCommands == null) {
            _subCommands = [];
            _subCommands.Add("list",ListExtensions);
            _subCommands.Add("lookup",LookupExtension);
        }
        if (args.Length < 2) {
            List<string> argsList = args.ToList();
            argsList.Add("help");
            args = argsList.ToArray();
        }
        Tools.TryRunSub(_subCommands, args, 1, _myTerminal);
    }

    static void ListExtensions(string[] args) {
        _myTerminal!.WriteLine($"&6已安装的扩展&8[&2{Program.Context.Extensions.Count}&8]");
        foreach (KeyValuePair<string,IShulkerExtension> ext in Program.Context.Extensions) {
            Terminal.WriteLine("",$"&8 - &6{ext.Value.Name}&8@{ext.Value.Version}&7#{ext.Key}");
        }
    }

    static void LookupExtension(string[] args) {
        if (args.Length < 3) {
            _myTerminal!.WriteLine($"&7请提供扩展ID&8[&7{args[0]} {args[1]} &c<ID>&8]", Terminal.MessageType.Warn);
            return;
        }
        if (Program.Context.Extensions.TryGetValue(args[2],out IShulkerExtension? extension)) {
            _myTerminal!.WriteLine($"&6{extension.Name}&8@{extension.Version}");
            if (extension.AsciiArt != null) {
                string[] lines = extension.AsciiArt.Split('\n');
                foreach (string line in lines) {
                    Terminal.WriteLine("",$" {line}");
                }
            }
            Terminal.WriteLine("",$" &6 ID &8[&e{extension.Id}&8]");
            Terminal.WriteLine("",$" &6描述&8[&e{extension.Description}&8]");
            Terminal.WriteLine("",$" &6作者&8[&e{extension.Author}&8]");
            Terminal.WriteLine("",$" &6网址&8[&e{extension.Link}&8]");
            if (extension.Document != null) Terminal.WriteLine("",$" &6文档&8[&e{extension.Document}&8]");
            if (extension.Donating != null) Terminal.WriteLine("",$" &6捐助&8[&e{extension.Donating}&8]");
        } else {
            _myTerminal!.WriteLine($"&7没有找到扩展&8[&c{args[2]}&8]", Terminal.MessageType.Warn);
        }
    }
}