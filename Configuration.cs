using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;

namespace MiniMappingway
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool showFcMembers { get; set; } = false;
        public bool showFriends { get; set; } = true;

        public bool enabled { get; set; } = true;
        public bool minimapLocked { get; set; } = false;

        public Vector4 friendColour = new Vector4(0, 0, 255, 255);
        public Vector4 fcColour = new Vector4(255, 0, 0, 255);
        public int circleSize = 4;




        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface!.SavePluginConfig(this);
        }
    }
}
