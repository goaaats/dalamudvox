using System.Linq;
using System.Numerics;
using DalamudVox;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace DalamudPluginProjectTemplate
{
    public class PluginUI
    {
        private readonly Plugin _plugin;
        public bool IsVisible { get; set; }

        public PluginUI(Plugin plugin)
        {
            _plugin = plugin;
        }

        public void Draw()
        {
            if (!IsVisible)
                return;

            var pOpen = true;

            ImGui.Begin("Hey, Dalamud! Config", ref pOpen);

            ImGui.PushItemWidth(100f);
            var kItem1 = VirtualKey.EnumToIndex(_plugin.Config.ModifierKey);
            if (ImGui.Combo("##DVKeybind1", ref kItem1, VirtualKey.Names.Take(3).ToArray(), 3))
            {
                _plugin.Config.ModifierKey = VirtualKey.IndexToEnum(kItem1);
            }
            ImGui.SameLine();
            var kItem2 = VirtualKey.EnumToIndex(_plugin.Config.MajorKey) - 3;
            if (ImGui.Combo("Keybind##DVKeybind2", ref kItem2, VirtualKey.Names.Skip(3).ToArray(), VirtualKey.Names.Length - 3))
            {
                _plugin.Config.MajorKey = VirtualKey.IndexToEnum(kItem2) + 3;
            }
            ImGui.PopItemWidth();

            ImGui.SliderInt("TTS/Sound Volume", ref _plugin.Config.Volume, 0, 100);

            ImGui.Checkbox("Disable Hotword in instances", ref _plugin.Config.DisableInInstance);
            ImGui.Text("This can prevent triggering Hey Dalamud accidentally while in a raid.");
            ImGui.Text("You can always toggle Hey Dalamud with the /hdtoggle command.");

            ImGui.Dummy(new Vector2(10,20));

            if (ImGui.Button("Save & Close"))
            {
                this._plugin.Config.Save();
            }

            ImGui.End();

            IsVisible = pOpen;
        }
    }
}
