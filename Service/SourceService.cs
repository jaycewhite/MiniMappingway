using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using MiniMappingway.Service.Interface;

namespace MiniMappingway.Service
{
    internal class SourceService : ISourceService
    {
        public ConcurrentDictionary<string, SourceData> SourceDataDict { get; set; } = new();
        private readonly IConfigurationService _configurationService;
        private readonly IPersonService _personService;
        public SourceService(IConfigurationService configurationService, IPersonService personService)
        {
            _configurationService = configurationService;
            _personService = personService;
        }

        public bool AddOrUpdateSource(string sourceName, Vector4 colour)
        {
            if (_configurationService.GetConfiguration().SourceConfigs.TryGetValue(sourceName, out var source))
            {
                SourceDataDict.AddOrUpdate(sourceName, source, (_, _) => source);
                _personService.InitialiseBag(sourceName);

            }
            else
            {
                var uintColor = ImGui.ColorConvertFloat4ToU32(colour);
                var sourceData = new SourceData(uintColor);
                switch (sourceName)
                {
                    case IPersonService.EveryoneKey:
                        sourceData.Priority = 0;
                        sourceData.Enabled = false;
                        break;
                    case IPersonService.FcMembersKey:
                        sourceData.Priority = 1;
                        break;
                    case IPersonService.FriendKey:
                        sourceData.Priority = 2;
                        break;
                    default:
                        sourceData.Priority = GetNextFreePriority();
                        break;
                }

                var config = _configurationService.GetConfiguration();
                config.SourceConfigs.TryAdd(sourceName, sourceData);
                config.Save();
                SourceDataDict.AddOrUpdate(sourceName, sourceData, (_, _) => sourceData);
                _personService.InitialiseBag(sourceName);
            }
            return true;
        }

        public bool AddOrUpdateSource(string sourceName, uint colour)
        {
            if (_configurationService.GetConfiguration().SourceConfigs.TryGetValue(sourceName, out var source))
            {
                SourceDataDict.AddOrUpdate(sourceName, source, (_, _) => source);
                _personService.InitialiseBag(sourceName);
            }
            else
            {
                var sourceData = new SourceData(colour);

                SourceDataDict.AddOrUpdate(sourceName, sourceData, (_, _) => sourceData);
                _personService.InitialiseBag(sourceName);

            }

            return true;
        }

        public bool RemoveSourceAndPeople(string sourceName)
        {
            var successPerson = _personService.ClearBag(sourceName);
            var successSource = SourceDataDict.TryRemove(sourceName, out _);

            return successPerson && successSource;
        }

        public int GetNextFreePriority()
        {
            for (var i = 0; i < 99; i++)
            {
                if (SourceDataDict.Values.All(x => x.Priority != i))
                {
                    return i;
                }
            }

            return 1;
        }

    }
}
