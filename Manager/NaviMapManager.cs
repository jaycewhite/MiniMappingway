using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using MiniMappingway.Model;
using System;
using System.Collections.Generic;

namespace MiniMappingway.Manager
{
    public class NaviMapManager
    {
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

        public IntPtr naviMapPointer = IntPtr.Zero;

        public ExcelSheet<Map>? Maps;

        public IntPtr MapSig1;

        public IntPtr MapSig2;

        public List<CircleData> CircleData = new List<CircleData>();

        public uint[] Colours = new uint[2];

        public NaviMapManager()
        {
            MapSig1 = ServiceManager.SigScanner.GetStaticAddressFromSig("44 8B 3D ?? ?? ?? ?? 45 85 FF");
            MapSig2 = ServiceManager.SigScanner.GetStaticAddressFromSig("44 0F 44 3D ?? ?? ?? ??");
            UpdateNavMapPointer();
            Maps = ServiceManager.DataManager.GetExcelSheet<Map>();
            updateNaviMap();
            updateMap();
            updateColourArray();
        }

        public void updateColourArray()
        {
            Colours[0] = ImGui.ColorConvertFloat4ToU32(ServiceManager.Configuration.friendColour);
            Colours[1] = ImGui.ColorConvertFloat4ToU32(ServiceManager.Configuration.fcColour);
        }

        public void UpdateNavMapPointer()
        {
            if(ServiceManager.GameGui == null) { return; }
            naviMapPointer = ServiceManager.GameGui.GetAddonByName("_NaviMap", 1);
        }

        public bool updateNaviMap()
        {

            if (naviMapPointer == IntPtr.Zero)
            {
                return false;
            }

            unsafe
            {
                var _naviMapPtr = (AtkUnitBase*)naviMapPointer;

                var rotationPtr = (float*)((nint)_naviMapPtr + 0x254);
                var naviScalePtr = (float*)((nint)_naviMapPtr + 0x24C);
                if (_naviMapPtr->UldManager.LoadedState != AtkLoadState.Loaded)
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

                X = _naviMapPtr->X;
                Y = _naviMapPtr->Y;
                naviScale = _naviMapPtr->Scale;
                visible = ((_naviMapPtr->VisibilityFlags & 0x03) == 0);
            }

            return true;
        }

        public void updateOncePerZone()
        {
            Dalamud.Logging.PluginLog.Verbose($"{naviMapPointer == IntPtr.Zero}");
            if (naviMapPointer == IntPtr.Zero)
            {
                Dalamud.Logging.PluginLog.Verbose("Updating NavMapPointer");

                UpdateNavMapPointer();
            }

            updateMap();
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
            try
            {
                return *(uint*)MapSig1 == 0 ? *(uint*)MapSig2 : *(uint*)MapSig1;

            }
            catch
            {
                return 0;
            }
        }

    }
}
