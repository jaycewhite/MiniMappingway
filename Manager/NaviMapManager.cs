using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using MiniMappingway.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Dalamud.Utility.Signatures;
using System.Linq;
using System.Numerics;

namespace MiniMappingway.Manager
{
    public unsafe class NaviMapManager
    {

        public ConcurrentDictionary<string, ConcurrentDictionary<string, IntPtr>> personListsDict = new ConcurrentDictionary<string, ConcurrentDictionary<string,IntPtr>>();

        public ConcurrentDictionary<string, uint> sourceDataDict = new ConcurrentDictionary<string, uint>();

        public int X;

        public int Y;

        public int yOffset = +1;

        public float naviScale;

        public float zoneScale;

        public float rotation;

        public bool visible;

        public float zoom;

        public short offsetX;
        public short offsetY;

        public bool loading;

        public string? debugValue;

        public bool debugMode = false;

        public bool isLocked = false;

        public AtkUnitBase* naviMapPointer => (AtkUnitBase*) ServiceManager.GameGui.GetAddonByName("_NaviMap", 1);

        public ExcelSheet<Map>? Maps;

        [Signature("44 8B 3D ?? ?? ?? ?? 45 85 FF", ScanType = ScanType.StaticAddress)]
        public readonly uint* MapSig1 = null!;

        [Signature("44 0F 44 3D ?? ?? ?? ??", ScanType = ScanType.StaticAddress)]
        public readonly uint* MapSig2 = null!;

        public List<CircleData> CircleData = new List<CircleData>();

        public uint[] Colours = new uint[2];

        public NaviMapManager()
        {
            SignatureHelper.Initialise(this);

            Maps = ServiceManager.DataManager.GetExcelSheet<Map>();
            updateNaviMap();
            updateMap();
            updateColourArray();
        }

        public bool AddOrUpdateSource(string sourceName, uint colour)
        {
            sourceDataDict.AddOrUpdate(sourceName, colour,(x,y) => colour);

            personListsDict.TryAdd(sourceName, new ConcurrentDictionary<string,IntPtr>());
            return true;
        }

        public bool AddOrUpdateSource(string sourceName, Vector4 colour)
        {
            var uintColor = ImGui.ColorConvertFloat4ToU32(colour);
            sourceDataDict.AddOrUpdate(sourceName, uintColor, (x, y) => uintColor);

            personListsDict.TryAdd(sourceName, new ConcurrentDictionary<string, IntPtr>());

            return true;
        }

        public void updateColourArray()
        {
            Colours[0] = ImGui.ColorConvertFloat4ToU32(ServiceManager.Configuration.friendColour);
            Colours[1] = ImGui.ColorConvertFloat4ToU32(ServiceManager.Configuration.fcColour);
        }

        public bool updateNaviMap()
        {

            if (naviMapPointer == null)
            {
                return false;
            }

            unsafe
            {

                //There's probably a better way of doing this but I don't know it for now
                isLocked = ((AtkComponentCheckBox*)naviMapPointer->GetNodeById(4)->GetComponent())->IsChecked; 

                var rotationPtr = (float*)((nint)naviMapPointer + 0x254);
                var naviScalePtr = (float*)((nint)naviMapPointer + 0x24C);
                if (naviMapPointer->UldManager.LoadedState != AtkLoadState.Loaded)
                {
                    return false;
                }
                try
                {
                    rotation = *rotationPtr;
                    zoom = *naviScalePtr;
                }
                catch
                {

                }

                X = naviMapPointer->X;
                Y = naviMapPointer->Y;
                naviScale = naviMapPointer->Scale;
                visible = ((naviMapPointer->VisibilityFlags & 0x03) == 0);
            }
            return true;
        }

        public unsafe void CheckIfLoading()
        {
            var LocationTitle = (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("_LocationTitle", 1);
            var FadeMiddle = (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("FadeMiddle", 1);
            loading =
                (LocationTitle != null && LocationTitle->IsVisible) ||
                (FadeMiddle != null && FadeMiddle->IsVisible);
        }

        public void updateMap()
        {
            if (Maps != null)
            {
                Dalamud.Logging.PluginLog.Verbose("Updating Map");

                var map = Maps.GetRow(getMapId());

                if (map == null) { return; }

                if (map.SizeFactor != 0)
                {
                    zoneScale = (float)map.SizeFactor / 100;
                }
                else
                {
                    zoneScale = 1;
                }
                offsetX = map.OffsetX;
                offsetY = map.OffsetY;

            }
        }

        private unsafe uint getMapId()
        {
            return *MapSig1 == 0 ? *MapSig2 : *MapSig1;
        }

        public bool ClearPersonBag(string sourceName)
        {
            if(personListsDict.TryGetValue(sourceName, out var personBag))
            {
                personBag.Clear();
                return true;
            }
            return false;
        }
        public bool OverwriteWholeBag(string sourceName, List<PersonDetails> list) 
        {
            if(personListsDict.TryGetValue(sourceName,out var personBag))
            {
                var success = true;
                personBag.Clear();
                foreach (var person in list)
                {
                    if(!personBag.TryAdd(person.Name, person.Ptr))
                    {
                        success = false;
                    }
                }
                return success;
            }
            return false;
        }

        public bool AddToBag(string sourceName, PersonDetails details)
        {
            if(personListsDict.TryGetValue(sourceName,out var personList))
            {
                return personList.TryAdd(details.Name, details.Ptr);
            }
            return false;
        }

        public bool RemoveFromBag(string sourceName, IntPtr ptr)
        {
            if (personListsDict.TryGetValue(sourceName, out var personList))
            {
                var entry = personList.First(x => x.Value == ptr);
                return personList.TryRemove(entry);
            }
            return false;

        }

        public bool RemoveFromBag(string sourceName, string Name)
        {
            if (personListsDict.TryGetValue(sourceName, out var personList))
            {
                var entry = personList.First(x => x.Key == Name);
                return personList.TryRemove(entry);
            }
            return false;
        }

        public bool RemoveSourceAndPeople(string sourceName)
        {
            var successPerson = personListsDict.TryRemove(sourceName, out _);
            var successSource = sourceDataDict.TryRemove(sourceName, out _);

            return successPerson && successSource;
        }

    }
}
