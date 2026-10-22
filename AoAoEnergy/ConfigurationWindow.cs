using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace AoAoEnergy;

internal sealed class ConfigurationWindow
{
    private readonly Plugin plugin;

    public bool IsOpen { get; set; }

    public ConfigurationWindow(Plugin plugin)
    {
        this.plugin = plugin;
    }

    public void Draw()
    {
        if (!IsOpen) return;

        var isOpen = IsOpen;
        if (!ImGui.Begin("AoAoEnergy 设置", ref isOpen))
        {
            IsOpen = isOpen;
            ImGui.End();
            return;
        }
        IsOpen = isOpen;

        var headerColor = new Vector4(0.4f, 0.7f, 1f, 1f);

        ImGui.PushStyleColor(ImGuiCol.Text, headerColor);
        ImGui.Text("目标设置");
        ImGui.PopStyleColor();
        ImGui.Indent();
        var selfEnabled = plugin.SelfEnabled;
        if (ImGui.Checkbox("自己", ref selfEnabled))
            plugin.SetSelfEnabled(selfEnabled);
        var othersEnabled = plugin.OthersEnabled;
        if (ImGui.Checkbox("其他人", ref othersEnabled))
            plugin.SetOthersEnabled(othersEnabled);
        ImGui.Unindent();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Text, headerColor);
        ImGui.Text("职业设置");
        ImGui.PopStyleColor();
        ImGui.Indent();
        var battle = plugin.BattleEnabled;
        if (ImGui.Checkbox("战斗职业", ref battle))
            plugin.SetBattleEnabled(battle);
        var crafter = plugin.CrafterEnabled;
        if (ImGui.Checkbox("生产职业", ref crafter))
            plugin.SetCrafterEnabled(crafter);
        var gatherer = plugin.GathererEnabled;
        if (ImGui.Checkbox("采集职业", ref gatherer))
            plugin.SetGathererEnabled(gatherer);
        ImGui.Unindent();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("预览当前效果"))
            plugin.PreviewSelectedEffect();
        ImGui.SameLine();
        if (ImGui.Button("清除预览效果"))
            plugin.ClearActiveEffects();
        ImGui.End();
    }
}
