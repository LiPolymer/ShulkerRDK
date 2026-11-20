using System.ComponentModel;
using System.Reflection;

namespace ShulkerRDK.Shared;

public static partial class Tools {
    public static string? GetDescriptionAttribute(Delegate action) {
        return action.Method.GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    public static void DisplayException(Exception e,IChainedLikeTerminal ct,Terminal.MessageType mt = Terminal.MessageType.Critical) {
        ct.WriteLine($"&c发生未处理的异常&8[&c{e.Message}&8]",mt);
        ct.WriteLine($"&7异常类型&8[&7{e.GetType().Name}&8]",mt);
        #if DEBUG
        Terminal.WriteLine($"&8{e.StackTrace}");
        #endif
        //ct.WriteLine( $"&7堆栈跟踪\n&8{e.StackTrace}", mt);
    }
}
