using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using MiniMappingway.Manager;
using MiniMappingway.Model;

namespace MiniMappingway
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public Dictionary<string,SourceData> SourceConfigs { get; set; } = new ();

        public bool Enabled { get; set; } = true;
        

        public void Initialize()
        {

        }

        public void Save()
        {
            ServiceManager.DalamudPluginInterface.SavePluginConfig(this);
        }
    }
}
