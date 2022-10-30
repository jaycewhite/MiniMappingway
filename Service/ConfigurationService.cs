using Dalamud.Plugin;
using MiniMappingway.Service.Interface;

namespace MiniMappingway.Service
{
    internal class ConfigurationService : IConfigurationService
    {
        private Configuration _configuration;

        public ConfigurationService(DalamudPluginInterface dalamudPluginInterface)
        {
            _configuration = dalamudPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        }

        public Configuration GetConfiguration()
        {
            return _configuration;
        }

        public void UpdateConfiguration(Configuration configuration)
        {
            _configuration = configuration;
            _configuration.Save();
        }
    }
}
