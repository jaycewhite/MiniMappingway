using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using MiniMappingway.Model;
using MiniMappingway.Service;
using MiniMappingway.Utility;

namespace MiniMappingway.Manager
{
    public unsafe class NaviMapManager : IDisposable
    {

        public readonly ConcurrentDictionary<string, ConcurrentDictionary<int, PersonDetails>> PersonDict = new();

        public readonly ConcurrentDictionary<string, SourceData> SourceDataDict = new();

        public int X;

        public int Y;

        public float NaviScale;

        public float ZoneScale;

        public float Rotation;

        public bool Visible;

        public float Zoom;

        public short OffsetX;
        public short OffsetY;

        public bool Loading;

        public bool DebugMode = false;

        public bool IsLocked;

        public bool InCombat { get; set; }

        private AtkUnitBase* NaviMapPointer => (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("_NaviMap");

        private readonly ExcelSheet<Map>? _maps;

        public readonly ConcurrentDictionary<int,Queue<CircleData>> CircleData = new();

        public NaviMapManager()
        {
            ServiceManager.GameInteropProvider.InitializeFromAttributes(this);

            _maps = ServiceManager.DataManager.GetExcelSheet<Map>();
            UpdateNaviMap();
            UpdateMap();
        }

        public bool AddOrUpdateSource(string sourceName, uint colour)
        {
            if (ServiceManager.Configuration.SourceConfigs.TryGetValue(sourceName, out var source))
            {
                SourceDataDict.AddOrUpdate(sourceName, source, (_, _) => source);
                PersonDict.AddOrUpdate(sourceName, new ConcurrentDictionary<int, PersonDetails>(),
                    (_, _) => new ConcurrentDictionary<int, PersonDetails>());
            }
            else
            {
                var sourceData = new SourceData(colour);

                SourceDataDict.AddOrUpdate(sourceName, sourceData, (_, _) => sourceData);
                PersonDict.AddOrUpdate(sourceName, new ConcurrentDictionary<int, PersonDetails>(),
                    (_, _) => new ConcurrentDictionary<int, PersonDetails>());

            }

            return true;
        }

        public bool AddOrUpdateSource(string sourceName, Vector4 colour)
        {
            if (ServiceManager.Configuration.SourceConfigs.TryGetValue(sourceName, out var source))
            {
                SourceDataDict.AddOrUpdate(sourceName, source, (_, _) => source);
                PersonDict.AddOrUpdate(sourceName, new ConcurrentDictionary<int, PersonDetails>(),
                    (_, _) => new ConcurrentDictionary<int, PersonDetails>());

            }
            else
            {
                var uintColor = ImGui.ColorConvertFloat4ToU32(colour);
                var sourceData = new SourceData(uintColor);
                switch (sourceName)
                {
                    case FinderService.EveryoneKey:
                        sourceData.Priority = 0;
                        sourceData.Enabled = false;
                        break;
                    case FinderService.FcMembersKey:
                        sourceData.Priority = 1;
                        break;
                    case FinderService.FriendKey:
                        sourceData.Priority = 2;
                        break;
                    default:
                        sourceData.Priority = GetNextFreePriority();
                        break;
                }

                ServiceManager.Configuration.SourceConfigs.TryAdd(sourceName, sourceData);
                SourceDataDict.AddOrUpdate(sourceName, sourceData, (_, _) => sourceData);
                PersonDict.AddOrUpdate(sourceName, new ConcurrentDictionary<int, PersonDetails>(),
                    (_, _) => new ConcurrentDictionary<int, PersonDetails>());
            }
            return true;
        }

        public bool UpdateNaviMap()
        {

            if (NaviMapPointer == null)
            {
                return false;
            }
            
            //There's probably a better way of doing this but I don't know it for now
            IsLocked = ((AtkComponentCheckBox*)NaviMapPointer->GetNodeById(4)->GetComponent())->IsChecked;
            
            if (NaviMapPointer->UldManager.LoadedState != AtkLoadState.Loaded)
            {
                return false;
            }
            try
            {
                Rotation = NaviMapPointer->GetNodeById(8)->Rotation;
                Zoom = NaviMapPointer->GetNodeById(18)->GetComponent()->GetImageNodeById(6)->ScaleX;
            }
            catch
            {
                // ignored
            }

            X = NaviMapPointer->X;
            Y = NaviMapPointer->Y;
            NaviScale = NaviMapPointer->Scale;
            Visible = (NaviMapPointer->IsVisible && NaviMapPointer->VisibilityFlags == 0);
            
            return true;
        }

        public bool CheckIfLoading()
        {
            var locationTitle = (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("_LocationTitle");
            var fadeMiddle = (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("FadeMiddle");
            return Loading =
                locationTitle->IsVisible ||
                fadeMiddle->IsVisible;
        }

        public void UpdateMap()
        {
            if (_maps != null)
            {
                var map = _maps.GetRow(GetMapId());

                if (map == null) { return; }

                if (map.SizeFactor != 0)
                {
                    ZoneScale = (float)map.SizeFactor / 100;
                }
                else
                {
                    ZoneScale = 1;
                }
                OffsetX = map.OffsetX;
                OffsetY = map.OffsetY;

            }
        }

        private uint GetMapId()
        {
            return AgentMap.Instance()->CurrentMapId;
        }

        public bool ClearPersonBag(string sourceName)
        {
            PersonDict.TryGetValue(sourceName, out var dict);
            if (dict == null)
            {
                return false;
            }
            dict.Clear();
            return true;
        }
        public bool OverwriteWholeBag(string sourceName, List<PersonDetails> list)
        {
            ClearPersonBag(sourceName);

            var success = true;

            PersonDict.TryGetValue(sourceName, out var dict);

            if (dict == null)
            {
                return false;
            }

            foreach (var person in list)
            {
                var personIndex = MarkerUtility.GetObjIndexById(person.Id);

                if (personIndex == null)
                {
                    continue;
                }
                if (!dict.TryAdd((int)personIndex, person))
                {
                    success = false;
                }
            }
            return success;
        }

        public bool AddToBag(PersonDetails details)
        {
            PersonDict.TryGetValue(details.SourceName, out var dict);

            if (dict == null)
            {
                return false;
            }

            var personIndex = MarkerUtility.GetObjIndexById(details.Id);
            if (personIndex == null)
            {
                return false;
            }
            return dict.TryAdd((int)personIndex, details);
        }

        public bool RemoveFromBag(ulong id, string sourceName)
        {
            PersonDict.TryGetValue(sourceName, out var dict);
            if (dict == null)
            {
                return false;
            }
            var entry = dict.First(x => x.Value.Id == id);
            return dict.TryRemove(entry);

        }

        public bool RemoveFromBag(string name, string sourceName)
        {
            PersonDict.TryGetValue(sourceName, out var dict);
            if (dict == null)
            {
                return false;
            }
            var entry = dict.First(x => x.Value.Name == name);
            return dict.TryRemove(entry);

        }

        public bool RemoveSourceAndPeople(string sourceName)
        {
            var successPerson = ClearPersonBag(sourceName);
            var successSource = SourceDataDict.TryRemove(sourceName, out _);

            return successPerson && successSource;
        }

        public void Dispose()
        {
            PersonDict.Clear();
            SourceDataDict.Clear();
        }

        public int GetNextFreePriority()
        {
            for(var i = 0; i < 99; i++)
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
