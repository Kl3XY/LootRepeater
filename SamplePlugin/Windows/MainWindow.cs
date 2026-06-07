using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly Plugin plugin;

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string goatImagePath)
        : base("Loot", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.goatImagePath = goatImagePath;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public unsafe override void Draw()
    {
        /*
        ImGui.Text($"The random config bool is {plugin.Configuration.SomePropertyToBeSavedAndWithADefault}");

        if (ImGui.Button("Show Settings"))
        {
            plugin.ToggleConfigUi();
        }
        */
        // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
        // ImRaii takes care of this after the scope ends.
        // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        using (var child = ImRaii.Child("SomeChildWithAScrollbar", Vector2.Zero, true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                try
                {
                    /*
                     * Original Snippet from https://github.com/PunishXIV/LazyLoot/blob/master/LazyLoot/Roller.cs#L415
                     */
                    
                    List<Item> items = [];
                    var span = Loot.Instance()->Items;
                    for (var i = 0; i < span.Length; i++)
                    {
                        var loot = span[(int)i];
                        
                        if (loot.ItemId >= 1000000) loot.ItemId -= 1000000;
                        if (loot.ChestObjectId is 0 or 0xE0000000) continue;
                        if (loot.RollResult != RollResult.UnAwarded) continue;
                        if (loot.RollState is RollState.Rolled or RollState.Unavailable or RollState.Unknown) continue;
                        if (loot.ItemId == 0) continue;
                        if (loot.LootMode is LootMode.LootMasterGreedOnly or LootMode.Unavailable) continue;
                        
                        var DBItem = Plugin.DataManager.GetExcelSheet<Item>().GetRowOrDefault(loot.ItemId);
                        if (DBItem != null) items.Add(DBItem.Value);
                    }

                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        var gameIcon = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(item.Icon)).GetWrapOrEmpty();
                        ImGui.Image(gameIcon.Handle, new Vector2(28, 28) * ImGuiHelpers.GlobalScale);
                        ImGui.SameLine();
                        ImGui.Text($"{i}. " + item.Name.ToString() + ": ");
                        ImGui.SameLine();
                        ImGui.Button("Need");
                        ImGui.SameLine();
                        ImGui.Button("Greed");
                        ImGui.SameLine();
                        ImGui.Button("Pass");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                
                
                
                
                var playerState = Plugin.PlayerState;
                
            }
        }
    }
}
