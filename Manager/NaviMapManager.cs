using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using MiniMappingway.Model;
using MiniMappingway.Utility;

namespace MiniMappingway.Manager
{
    public unsafe class NaviMapManager : IDisposable
    {

        public ConcurrentDictionary<int, PersonDetails> PersonDict = new();

        public ConcurrentDictionary<string, uint> SourceDataDict = new();

        public int X;

        public int Y;

        public int YOffset = +1;

        public float NaviScale;

        public float ZoneScale;

        public float Rotation;

        public bool Visible;

        public float Zoom;

        public short OffsetX;
        public short OffsetY;

        public bool Loading;

        public string? DebugValue;

        public bool DebugMode = false;

        public bool IsLocked;

        public AtkUnitBase* NaviMapPointer => (AtkUnitBase*) ServiceManager.GameGui.GetAddonByName("_NaviMap", 1);

        public ExcelSheet<Map>? Maps;

        [Signature("44 8B 3D ?? ?? ?? ?? 45 85 FF", ScanType = ScanType.StaticAddress)]
        public readonly uint* MapSig1 = null!;

        [Signature("44 0F 44 3D ?? ?? ?? ??", ScanType = ScanType.StaticAddress)]
        public readonly uint* MapSig2 = null!;

        public Queue<CircleData> CircleData = new();

        public NaviMapManager()
        {
            SignatureHelper.Initialise(this);

            Maps = ServiceManager.DataManager.GetExcelSheet<Map>();
            UpdateNaviMap();
            UpdateMap();
        }

        public bool AddOrUpdateSource(string sourceName, uint colour)
        {
            SourceDataDict.AddOrUpdate(sourceName, colour,(_,_) => colour);
            
            return true;
        }

        public bool AddOrUpdateSource(string sourceName, Vector4 colour)
        {
            var uintColor = ImGui.ColorConvertFloat4ToU32(colour);
            SourceDataDict.AddOrUpdate(sourceName, uintColor, (_, _) => uintColor);
            

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

            var rotationPtr = (float*)((nint)NaviMapPointer + 0x254);
            var naviScalePtr = (float*)((nint)NaviMapPointer + 0x24C);
            if (NaviMapPointer->UldManager.LoadedState != AtkLoadState.Loaded)
            {
                return false;
            }
            try
            {
                Rotation = *rotationPtr;
                Zoom = *naviScalePtr;
            }
            catch
            {
                // ignored
            }

            X = NaviMapPointer->X;
            Y = NaviMapPointer->Y;
            NaviScale = NaviMapPointer->Scale;
            Visible = ((NaviMapPointer->VisibilityFlags & 0x03) == 0);
            return true;
        }

        public bool CheckIfLoading()
        {
            var locationTitle = (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("_LocationTitle", 1);
            var fadeMiddle = (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("FadeMiddle", 1);
            return Loading =
                (locationTitle->IsVisible) ||
                (fadeMiddle->IsVisible);
        }

        public void UpdateMap()
        {
            if (Maps != null)
            {
                PluginLog.Verbose("Updating Map");

                var map = Maps.GetRow(GetMapId());

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
            return *MapSig1 == 0 ? *MapSig2 : *MapSig1;
        }

        public bool ClearPersonBag(string sourceName)
        {
            PersonDict.AsParallel().ForAll(x =>
            {
                if (x.Value.SourceName == sourceName)
                {
                    PersonDict.Remove(x.Key, out _);
                }
            });
            return true;
        }
        public bool OverwriteWholeBag(string sourceName, List<PersonDetails> list)
        {
            ClearPersonBag(sourceName);

            var success = true;
            foreach (var person in list)
            {
                var personIndex = MarkerUtility.GetObjIndexById(person.Id);
                if (personIndex == null)
                {
                    continue;
                }
                if (!PersonDict.TryAdd((int)personIndex, person))
                {
                    success = false;
                }
            }
            return success;
        }

        public bool AddToBag(PersonDetails details)
        {
            var personIndex = MarkerUtility.GetObjIndexById(details.Id);
            if (personIndex == null)
            {
                return false;
            }
            return PersonDict.TryAdd((int)personIndex, details);
        }

        public bool RemoveFromBag(uint id)
        {
                var entry = PersonDict.First(x => x.Value.Id == id);
                return PersonDict.TryRemove(entry);

        }

        public bool RemoveFromBag(string name)
        {
                var entry = PersonDict.First(x => x.Value.Name == name);
                return PersonDict.TryRemove(entry);
            
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
    }
}
