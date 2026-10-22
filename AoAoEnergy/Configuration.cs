using Dalamud.Configuration;

namespace AoAoEnergy;

internal sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public OffsetProfileStore OffsetProfiles { get; set; } = new();
}

internal sealed class OffsetProfileStore
{
    public bool SelfEnabled { get; set; } = true;
    public bool OthersEnabled { get; set; } = true;
    public bool BattleEnabled { get; set; } = true;
    public bool CrafterEnabled { get; set; }
    public bool GathererEnabled { get; set; }
}
