using Dalamud.Configuration;
using Dalamud.Plugin;
using DalamudVox;
using Newtonsoft.Json;

namespace DalamudPluginProjectTemplate
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }

        // Add any other properties or methods here.
        [JsonIgnore] private DalamudPluginInterface pluginInterface;

        public int Volume = 80;
        public bool DisableInInstance = false;

        public VirtualKey.Enum ModifierKey = VirtualKey.Enum.VkControl;
        public VirtualKey.Enum MajorKey = VirtualKey.Enum.VkB;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
