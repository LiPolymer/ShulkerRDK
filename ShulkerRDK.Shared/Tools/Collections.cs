namespace ShulkerRDK.Shared;

public static partial class Tools {
    public static void MergeDict<TKey,TValue>(Dictionary<TKey,TValue> master,Dictionary<TKey,TValue> branch) where TKey : notnull {
        foreach (KeyValuePair<TKey,TValue> obj in branch) {
            master.Add(obj.Key,obj.Value);
        }
    }

    public static string[] GetSubGroup(string[] group,int startIndex) {
        if (startIndex <= 0) return [..group];
        if (startIndex >= group.Length) return [];
        return group[startIndex..];
    }
}
