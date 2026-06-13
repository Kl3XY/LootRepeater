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
using Achievement = Lumina.Excel.Sheets.Achievement;

namespace SamplePlugin.Windows;

public unsafe class MainWindow : Window, IDisposable
{
    private unsafe delegate bool RollItemRaw(Loot* lootIntPtr, RollResult option, uint lootItemIndex);
    private static RollItemRaw _rollItemRaw;
    private readonly Plugin plugin;
    private int t;
    private int tickReset = 30;
    private List<LootItem> items = new();
    
    public MainWindow(Plugin plugin, string goatImagePath)
        : base("Loot", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin.Framework.Update += onFWTick;
        
        this.plugin = plugin;
    }
    
    public void Dispose() {

    }

    public void onFWTick(IFramework framework)
    {
        tickReset--;
        if (tickReset <= 0)
        {
            loadLootTable();
            tickReset = 30;
        }
        
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
                    ImGui.BeginTable("Item", 2);

                    ImGui.TableNextColumn();
                    ImGui.TableSetupColumn("Rolling / Value", default, 128);
                    
                    ImGui.TableNextColumn();
                    ImGui.TableSetupColumn("Item");

                    ImGui.TableHeadersRow();
                    ImGui.TableNextColumn();

                    var hasRolledAllItems = true;
                    for (uint i = 0; i < items.Count; i++)
                    {
                        var loot = items[Convert.ToInt32(i)];
                        var item = Plugin.DataManager.GetExcelSheet<Item>().GetRowOrDefault(loot.ItemId).Value;
                        var gameIcon = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(item.Icon)).GetWrapOrEmpty();
                        
                        /*
                         * Create the Actions row.
                         * Either display the buttons or just display the roll value
                         */

                        if (loot.RollState != RollState.Rolled)
                        {
                            hasRolledAllItems = false;
                            if (ImGui.Button($"Need##{i}") && loot.LootMode == LootMode.Normal)
                            {
                                loot.RollState = RollState.Rolled;
                                RollItem(RollResult.Needed, i);
                            };
                            ImGui.SameLine();
                            if (ImGui.Button($"Greed##{i}"))
                            {
                                loot.RollState = RollState.Rolled;
                                RollItem(RollResult.Greeded, i);
                            };
                            ImGui.SameLine();
                            if (ImGui.Button($"Pass##{i}"))
                            {
                                loot.RollState = RollState.Rolled;
                                loot.RollValue = 0;
                                RollItem(RollResult.Passed, i);
                            };
                        }
                        else
                        {
                            String value = loot.RollValue == 0 ? "PASSED" : loot.RollValue.ToString();
                            ImGui.Text($"({value})");
                        }
                        
                        /*
                         * Display the item in the next row
                         */
                        
                        ImGui.TableNextColumn();
                        
                        ImGui.Image(gameIcon.Handle, new Vector2(16, 16) * ImGuiHelpers.GlobalScale);
                        ImGui.SameLine();
                        ImGui.Text($"{i}. " + item.Name.ToString());
                        ImGui.TableNextColumn();
                    }
                    ImGui.EndTable();
                    if (hasRolledAllItems)
                    {
                        Toggle();
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
