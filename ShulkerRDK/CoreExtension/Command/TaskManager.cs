using ShulkerRDK.Shared;

namespace ShulkerRDK.CoreExtension.Command;

public static class TaskManager {
    static Dictionary<string,Action<string[]>>? _subCommands;
    public static void Command(string[] args,ShulkerContext shulkerContext) {
        if (_subCommands == null) {
            _subCommands = [];
            _subCommands.Add("help",_ => {
                Terminal.WriteLine("&e&lTask","Helper");
            });
            _subCommands.Add("list",_ => {
                Terminal.WriteLine("&e&lTask","&e所有可使用的&9&o悬浮方法集");
                string[] files = Directory.GetFiles("./shulker/tasks/", "*.lvt");
                foreach (string lvtFile in files) {
                    Terminal.WriteLine("",$" &8- &6{Path.GetFileName(lvtFile).Replace(".lvt","")} &8[&7{File.ReadAllLines(lvtFile).Length}&8lines]");
                }
            });
        }
        if (!Directory.Exists("./shulker/tasks/")) {
            Directory.CreateDirectory("./shulker/tasks");
        }
        string subCommand = args.Length < 2 ? "help" : args[1];
        if (_subCommands.TryGetValue(subCommand,out Action<string[]>? action)) {
            action(args);
        } else {
            string[] files = Directory.GetFiles("./shulker/tasks/", "*.lvt"); 
            Dictionary<string, string> tasks = [];
            foreach (string lvtFile in files) {
                tasks.Add(Path.GetFileName(lvtFile),lvtFile);
            }
            if (tasks.TryGetValue(subCommand + ".lvt",out string? task)) {
                new LevitateInterpreter(Program.Context).RunFromFile(task);
            } else {
                Terminal.WriteLine("&e&lTask",$"&7未知的次级命令或任务&8[&7{args[0]} &c{subCommand}&8]", Terminal.MessageType.Warn);
            }
        }
    }
}