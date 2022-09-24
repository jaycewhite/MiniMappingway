using System;
using System.Numerics;
using Dalamud.Configuration;
using MiniMappingway.Manager;

namespace MiniMappingway
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool ShowFcMembers { get; set; }
        public bool ShowFriends { get; set; } = true;

        public bool Enabled { get; set; } = true;

        public Vector4 FriendColour = new(0, 0, 255, 255);
        public Vector4 FcColour = new(255, 0, 0, 255);
        public int CircleSize = 4;
        

        public void Initialize()
        {

        }

        public void Save()
        {
            ServiceManager.DalamudPluginInterface.SavePluginConfig(this);
        }
    }
}
