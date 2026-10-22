using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.ActionEffectHandler;

namespace AoAoEnergy;

public unsafe class Plugin : IDalamudPlugin, IDisposable
{
    private const string DrinkVfxPath = "vfx/common/eff/ev_energydrink_01x.avfx";

    private delegate void CreateResultVfxDelegate(ActionEffectHandler* actionHandler, Character* cast, Character* target, uint action, Effect* result);
    private delegate nint CreateVfxDelegate(string path, GameObject* cast, GameObject* target, float speed, char a5, ushort a6, char a7);
    private delegate bool IsShowInCaremaDelegate(Character* chara);
    private delegate nint ActorVfxRemoveDelegate(nint vfx, char a2);

    [Signature("48 85 D2 0F 84 ?? ?? ?? ?? 53 55 57", DetourName = nameof(CreateResultVfxDetour))]
    private Hook<CreateResultVfxDelegate> CreateResultVfxHook = null!;

    private CreateVfxDelegate? CreateVfx;
    private IsShowInCaremaDelegate? IsShowInCarema;
    private Hook<ActorVfxRemoveDelegate>? ActorVfxRemoveHook;

    private const char ImmediateRemovalMode = '\u0001';

    private Configuration Configuration;
    private ConfigurationWindow ConfigurationWindow;
    private readonly ActiveVfxTracker ActiveVfx = new();
    private readonly object LifecycleGate = new();
    private bool IsDisposed;

    public string Name => "AoAoEnergy";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; set; } = null!;
    [PluginService] internal static IFramework Framework { get; set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; set; } = null!;

    internal bool SelfEnabled => Configuration.OffsetProfiles.SelfEnabled;
    internal bool OthersEnabled => Configuration.OffsetProfiles.OthersEnabled;
    internal bool BattleEnabled => Configuration.OffsetProfiles.BattleEnabled;
    internal bool CrafterEnabled => Configuration.OffsetProfiles.CrafterEnabled;
    internal bool GathererEnabled => Configuration.OffsetProfiles.GathererEnabled;

    private void CreateResultVfxDetour(ActionEffectHandler* actionHandler, Character* cast, Character* target, uint action, Effect* result)
    {
        if (result != null && result->Type == 14 && result->Value == 49 && cast != null && target != null)
        {
            var isSelf = ObjectTable.LocalPlayer != null && cast == (Character*)ObjectTable.LocalPlayer.Address;
            var targetEnabled = isSelf ? Configuration.OffsetProfiles.SelfEnabled : Configuration.OffsetProfiles.OthersEnabled;
            if (CreateVfx != null && targetEnabled && IsJobAllowed(cast)
                && (IsShowInCarema == null || IsShowInCarema(cast)))
            {
                PlaySelectedVfx(&cast->GameObject, &target->GameObject);
                return;
            }
        }
        CreateResultVfxHook.Original(actionHandler, cast, target, action, result);
    }

    private bool IsJobAllowed(Character* cast)
    {
        var classJob = cast->CharacterData.ClassJob;
        if (classJob >= 8 && classJob <= 15)
            return Configuration.OffsetProfiles.CrafterEnabled;
        if (classJob >= 16 && classJob <= 18)
            return Configuration.OffsetProfiles.GathererEnabled;
        return Configuration.OffsetProfiles.BattleEnabled;
    }

    public Plugin()
    {
        try
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.OffsetProfiles ??= new OffsetProfileStore();
            ConfigurationWindow = new ConfigurationWindow(this);

            GameInteropProvider.InitializeFromAttributes(this);
            CreateVfx = Marshal.GetDelegateForFunctionPointer<CreateVfxDelegate>(
                SigScanner.ScanText("40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8"));
            IsShowInCarema = Marshal.GetDelegateForFunctionPointer<IsShowInCaremaDelegate>(
                SigScanner.ScanText("40 53 48 83 EC 20 48 8B 01 48 8B D9 FF 50 ?? 83 F8 08 75 ?? 0F B7 83"));

            var leaRel32Addr = (nint)SigScanner.ScanText("0F 11 48 10 48 8D 05") + 7;
            var vfxRemoveAddr = Marshal.ReadIntPtr(leaRel32Addr + Marshal.ReadInt32(leaRel32Addr) + 4);
            ActorVfxRemoveHook = GameInteropProvider.HookFromAddress<ActorVfxRemoveDelegate>(
                (IntPtr)vfxRemoveAddr, ActorVfxRemoveDetour);

            ActorVfxRemoveHook.Enable();

            PluginInterface.UiBuilder.Draw += ConfigurationWindow.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfiguration;
            CreateResultVfxHook.Enable();

            PluginLog.Info("AoAoEnergy initialized.");
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        lock (LifecycleGate)
        {
            if (IsDisposed) return;
            IsDisposed = true;
        }
        if (ConfigurationWindow != null)
            PluginInterface.UiBuilder.Draw -= ConfigurationWindow.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfiguration;
        CreateResultVfxHook?.Dispose();
        ActorVfxRemoveHook?.Dispose();
    }

    internal void SetSelfEnabled(bool enabled) { Configuration.OffsetProfiles.SelfEnabled = enabled; SaveConfiguration(); }
    internal void SetOthersEnabled(bool enabled) { Configuration.OffsetProfiles.OthersEnabled = enabled; SaveConfiguration(); }
    internal void SetBattleEnabled(bool enabled) { Configuration.OffsetProfiles.BattleEnabled = enabled; SaveConfiguration(); }
    internal void SetCrafterEnabled(bool enabled) { Configuration.OffsetProfiles.CrafterEnabled = enabled; SaveConfiguration(); }
    internal void SetGathererEnabled(bool enabled) { Configuration.OffsetProfiles.GathererEnabled = enabled; SaveConfiguration(); }

    internal void PreviewSelectedEffect()
    {
        lock (LifecycleGate)
        {
            if (IsDisposed) return;
        }
        Framework.RunOnFrameworkThread(PreviewOnFrameworkThread);
    }

    internal void ClearActiveEffects()
    {
        lock (LifecycleGate)
        {
            if (IsDisposed) return;
        }
        Framework.RunOnFrameworkThread(ClearActiveEffectsOnFrameworkThread);
    }

    private void PreviewOnFrameworkThread()
    {
        lock (LifecycleGate)
        {
            if (!IsDisposed)
            {
                var localPlayer = ObjectTable.LocalPlayer;
                if (localPlayer == null || localPlayer.Address == IntPtr.Zero)
                {
                    PluginLog.Warning("AoAoEnergy preview skipped because the local player is unavailable.");
                    return;
                }
                var address = (Character*)localPlayer.Address;
                PlaySelectedVfx(&address->GameObject, &address->GameObject);
            }
        }
    }

    private void ToggleConfiguration() => ConfigurationWindow.IsOpen = !ConfigurationWindow.IsOpen;

    private nint ActorVfxRemoveDetour(nint vfx, char a2)
    {
        if (!ActiveVfx.ShouldProcessNaturalRemoval(vfx))
            return IntPtr.Zero;
        return ActorVfxRemoveHook!.Original(vfx, a2);
    }

    private void ClearActiveEffectsOnFrameworkThread()
    {
        lock (LifecycleGate)
        {
            if (IsDisposed) return;
            var list = ActiveVfx.ClaimAllForRemoval();
            try
            {
                foreach (var item in list)
                    ActorVfxRemoveHook!.Original(item, ImmediateRemovalMode);
            }
            finally
            {
                foreach (var item in list)
                    ActiveVfx.CompleteRemoval(item);
            }
        }
    }

    private void SaveConfiguration() => PluginInterface.SavePluginConfig(Configuration);

    private void PlaySelectedVfx(GameObject* cast, GameObject* target)
    {
        lock (LifecycleGate)
        {
            if (IsDisposed) return;
            var handle = CreateVfx?.Invoke(DrinkVfxPath, cast, target, -1f, '\0', (ushort)0, '\0') ?? IntPtr.Zero;
            if (handle == IntPtr.Zero)
                PluginLog.Warning("AoAoEnergy: CreateVfx returned a null handle.");
            ActiveVfx.Track(handle);
        }
    }
}
