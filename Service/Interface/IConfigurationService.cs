namespace MiniMappingway.Service.Interface;

public interface IConfigurationService
{
    Configuration GetConfiguration();
    void UpdateConfiguration(Configuration configuration);
}