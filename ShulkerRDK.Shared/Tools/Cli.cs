namespace ShulkerRDK.Shared;

public static partial class Tools {
    // 尝试/检查 获取/执行
    public static bool TryRunSub(Dictionary<string,LevitateMethod> collection,string[] args,int depth,LevitateExecutionContext ec) {
        return TryRunSub(collection,args,depth,ec,out _);
    }

    public static bool TryRunSub(Dictionary<string,LevitateMethod> collection,string[] args,int depth,LevitateExecutionContext ec,out string? result) {
        LevitateMethod? method = TryGetSub(collection,args,depth,ec);
        if (method == null) {
            result = null;
            return false;
        }
        result = method(args,ec);
        return true;
    }

    public static bool TryRunSub(Dictionary<string,Action<string[]>> collection,string[] args,int depth,ChainedTerminal? ct = null) {
        Action<string[]>? action = TryGetSub(collection,args,depth,ct);
        if (action == null) {
            return false;
        }
        action(args);
        return true;
    }

    public static LevitateMethod? TryGetSub(Dictionary<string,LevitateMethod> collection,string[] args,int depth,LevitateExecutionContext ec) {
        if (!CheckParamLength(args,depth,ec)) return null;
        if (collection.TryGetValue(args[depth],out LevitateMethod? subM)) {
            return subM;
        }
        DisplayUnknownParam(args,depth,ec);
        return null;
    }

    public static bool TryGetSub(List<string> collection,string[] args,int depth,LevitateExecutionContext ec) {
        return TryGetSub(collection,args,depth,ec.Logger);
    }

    public static bool TryGetSub(List<string> collection,string[] args,int depth,IChainedLikeTerminal ct) {
        if (!CheckParamLength(args,depth,ct)) {
            DisplayAvailableCandidates(ct,collection);
            return false;
        }
        if (collection.Contains(args[depth])) {
            return true;
        }
        DisplayUnknownParam(args,depth,ct,collection);
        return false;
    }

    public static Action<string[]>? TryGetSub(Dictionary<string,Action<string[]>> collection,string[] args,int depth,ChainedTerminal? ct = null) {
        if (!CheckParamLength(args,depth,ct)) return null;
        if (collection.TryGetValue(args[depth],out Action<string[]>? subA)) {
            return subA;
        }
        if (ct != null) {
            DisplayUnknownParam(args,depth,ct,collection.Keys.ToList());
        }
        return null;
    }

    public static bool CheckParamLength(string[] args,int depth,LevitateExecutionContext? ec) {
        return CheckParamLength(args,depth,ec?.Logger);
    }

    public static bool CheckParamLength(string[] args,int depth,IChainedLikeTerminal? ct = null) {
        if (args.Length >= depth + 1) return true;
        if (ct != null) {
            DisplayMissingParam(args,depth,ct);
        }
        return false;
    }

    static void DisplayMissingParam(string[] args,int depth,IChainedLikeTerminal ct) {
        List<string> argList = args.ToList();
        argList.Add("_");
        ct.WriteLine($"缺少参数&8[{HighLightParam(argList.ToArray(),depth)}&8]",Terminal.MessageType.Error);
    }

    static void DisplayUnknownParam(string[] args,int depth,LevitateExecutionContext ec,List<string>? candidates = null) {
        ec.Logger.WriteLine($"未知参数&8[{HighLightParam(args,depth)}&8]",Terminal.MessageType.Error);
        if (candidates != null) DisplayAvailableCandidates(ec.Logger,candidates);
    }

    static void DisplayUnknownParam(string[] args,int depth,IChainedLikeTerminal ct,List<string>? candidates = null) {
        ct.WriteLine($"未知参数&8[{HighLightParam(args,depth)}&8]",Terminal.MessageType.Error);
        if (candidates != null) DisplayAvailableCandidates(ct,candidates);
    }

    static void DisplayAvailableCandidates(IChainedLikeTerminal ct,List<string> candidates) {
        ct.WriteLine($"&7可用项有&8[&7{string.Join("&8,&7",candidates)}&8]");
    }

    static string HighLightParam(string[] args,int depth) {
        string buffer = "&7";
        for (int i = 0; i < args.Length; i++) {
            if (i == depth) {
                buffer += "&c";
            }
            buffer += $"{args[i]}&7 ";
        }
        return buffer.TrimEnd();
    }
}
