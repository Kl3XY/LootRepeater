using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace SamplePlugin.Windows;

public unsafe class MainWindow : Window, IDisposable
{
    private unsafe delegate bool RollItemRaw(Loot* lootIntPtr, RollResult option, uint lootItemIndex);
    private static RollItemRaw _rollItemRaw;
    private readonly Plugin plugin;
    private int t;
    private List<LootItem> items = new();
    
    public MainWindow(Plugin plugin, string goatImagePath)
        : base("Loot", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        
        
        this.plugin = plugin;
    }
    
    public void Dispose() {

    }

    public void loadLootTable()
    {
        items.Clear();
        var span = Loot.Instance()->Items;
        for (var i = 0; i < span.Length; i++)
        {
            var loot = span[(int)i];
                   
            /*
             * Original Snippet from https://github.com/PunishXIV/LazyLoot/blob/master/LazyLoot/Roller.cs#L415
             *
             * Gets items that are currently in the loot table.
             */
            
            if (loot.ItemId >= 1000000) loot.ItemId -= 1000000;
            if (loot.ChestObjectId is 0 or 0xE0000000) continue;
            if (loot.ItemId == 0) continue;

            items.Add(loot);
        }
    }
    
    public unsafe override void OnOpen()
    {
        loadLootTable();
    }

    private unsafe void RollItem(RollResult option, uint index)
    {
        try
        {
            _rollItemRaw ??=
                Marshal.GetDelegateForFunctionPointer<RollItemRaw>(
                    Plugin.SigScanner.ScanText("41 83 F8 ?? 0F 83 ?? ?? ?? ?? 48 89 5C 24 08"));
            _rollItemRaw?.Invoke(Loot.Instance(), option, index);
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "Warning at roll");
        }
        
        
    }
    
    public override void OnClose()
    {
        base.OnClose();
        items.Clear();
        
    }

    
    
    public override void Draw()
    {
        using (var child = ImRaii.Child("SomeChildWithAScrollbar", Vector2.Zero, true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                try
                {
                    for (uint i = 0; i < items.Count; i++)
                    {
                        var loot = items[Convert.ToInt32(i)];
                        var item = Plugin.DataManager.GetExcelSheet<Item>().GetRowOrDefault(loot.ItemId).Value;
                        var gameIcon = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(item.Icon)).GetWrapOrEmpty();
                        if (loot.RollState != RollState.Rolled) {
                            if (ImGui.Button($"Need##{i}"))
                            {
                                loot.RollState = RollState.UpToNeed;
                                RollItem(RollResult.Needed, i);
                            };
                            ImGui.SameLine();
                            if (ImGui.Button($"Greed##{i}"))
                            {
                                loot.RollState = RollState.UpToGreed;
                                RollItem(RollResult.Greeded, i);
                            };
                            ImGui.SameLine();
                            if (ImGui.Button($"Pass##{i}"))
                            {
                                loot.RollState = RollState.UpToPass;
                                loot.RollValue = 0;
                                RollItem(RollResult.Passed, i);
                            };
                        }
                        ImGui.SameLine();
                        ImGui.Image(gameIcon.Handle, new Vector2(16, 16) * ImGuiHelpers.GlobalScale);
                        ImGui.SameLine();
                        ImGui.Text($"{i}. " + item.Name.ToString());
                        if (loot.RollState == RollState.Rolled)
                        {
                            ImGui.SameLine();
                            ImGui.Text($"({loot.RollValue})");
                        }
                        ImGui.NewLine();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
