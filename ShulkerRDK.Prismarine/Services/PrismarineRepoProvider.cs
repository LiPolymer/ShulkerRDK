using TridentCore.Core.Services;

namespace ShulkerRDK.Prismarine.Services;

public class PrismarineRepoProvider(string? curseForgeApiKey) : IRepositoryProviderAccessor {
    public IReadOnlyList<IRepositoryProviderAccessor.ProviderProfile> Build() {
        List<IRepositoryProviderAccessor.ProviderProfile> profiles = new List<IRepositoryProviderAccessor.ProviderProfile> {
            new IRepositoryProviderAccessor.ProviderProfile(
                                                            "modrinth",
                                                            IRepositoryProviderAccessor.ProviderProfile.DriverType.Modrinth,
                                                            "https://api.modrinth.com",
                                                            null,
                                                            "ShulkerRDK.Prismarine/Trident.Net"
                                                           ),
        };

        if (!string.IsNullOrEmpty(curseForgeApiKey)) {
            profiles.Add(new IRepositoryProviderAccessor.ProviderProfile(
                                                                         "curseforge",
                                                                         IRepositoryProviderAccessor.ProviderProfile.DriverType.CurseForge,
                                                                         "https://api.curseforge.com",
                                                                         ("x-api-key",curseForgeApiKey),
                                                                         "ShulkerRDK.Prismarine/Trident.Net"
                                                                        ));
        }

        return profiles;
    }

    public IReadOnlyList<IRepositoryProviderAccessor.ProviderCustom> BuildCustom() => [];
}