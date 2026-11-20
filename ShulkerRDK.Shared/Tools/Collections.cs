namespace ShulkerRDK.Shared;

public static partial class Tools {
    public static void MergeDict(Dictionary<string,Action<string[],ShulkerContext>> master,Dictionary<string,Action<string[],ShulkerContext>> branch) {
        foreach (KeyValuePair<string,Action<string[],ShulkerContext>> obj in branch) {
            master.Add(obj.Key,obj.Value);
        }
    }

    public static void MergeDict(Dictionary<string,LevitateMethod> master,
        Dictionary<string,LevitateMethod> branch) {
        foreach (KeyValuePair<string,LevitateMethod> obj in branch) {
            master.Add(obj.Key,obj.Value);
        }
    }

    public static void MergeDict(Dictionary<string,string> master,Dictionary<string,string> branch) {
        foreach (KeyValuePair<string,string> obj in branch) {
            master.Add(obj.Key,obj.Value);
        }
    }

    public static string[] GetSubGroup(string[] group,int startIndex) {
        List<string> result = [];
        for (int i = startIndex; i < group.Length; i++) {
            result.Add(group[i]);
        }
        return result.ToArray();
    }
}
