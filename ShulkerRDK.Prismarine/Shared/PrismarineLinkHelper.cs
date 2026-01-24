namespace ShulkerRDK.Prismarine.Shared;

public static class PrismarineLinkHelper {

    static Dictionary<PrismarineLinkBreaches,string> _breachesStringLut = new Dictionary<PrismarineLinkBreaches,string> {
        [PrismarineLinkBreaches.InternetUrl] = "net",
        [PrismarineLinkBreaches.TridentPurl] = "tri"
    };
    
    public static string GetPrismarineBreachString(PrismarineLinkBreaches breaches) {
        return breaches == PrismarineLinkBreaches.Undefined 
            ? throw new ArgumentException("Undefined is not a valid breach") 
            : _breachesStringLut[breaches];
    }

    public static PrismarineLinkBreaches? ParsePrismarineBreach(string brs) {
        PrismarineLinkBreaches sel = _breachesStringLut
            .FirstOrDefault(kvp => kvp.Value == brs,
                            new KeyValuePair<PrismarineLinkBreaches,string>
                                ()).Key;
        return sel == PrismarineLinkBreaches.Undefined ? null : sel;
    }
}

public class PrismarineLinkData {
    public required PrismarineLinkBreaches Breach;
    public required string Body;

    public override string ToString() {
        return $"{PrismarineLinkHelper.GetPrismarineBreachString(Breach)}>{Body}";
    }

    public static PrismarineLinkData Parse(string pls) {
        return pls[3] == '>' ? new PrismarineLinkData {
            Breach = PrismarineLinkHelper.ParsePrismarineBreach(pls[..3]) //todo: possible range problem
                       ?? throw new ArgumentException("couldn't parse priBreach from string"),
            Body = pls[4..]
        } : throw new ArgumentException("couldn't identify this string as priLink");
    }
}

public enum PrismarineLinkBreaches {
    Undefined,
    InternetUrl,
    TridentPurl
}