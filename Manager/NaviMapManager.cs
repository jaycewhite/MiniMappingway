using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Gui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;

namespace MiniMappingway.Manager
{
    internal static class NaviMapManager
    {
        public static int X;

        public static int Y;

        public static int yOffset = +1;

        public static float naviScale;

        public static float zoneScale;

        public static float rotation;

        public static bool visible;

        public static float zoom;

        public static short offsetX;

        public static short offsetY;

        public static bool loading;

        public static string? debugValue;

        public static IntPtr naviMapPointer = IntPtr.Zero;

        public static ExcelSheet<Map>? Maps;

        public static IntPtr MapSig1;

        public static IntPtr MapSig2;

        private static GameGui? gameGui;


        public static void Init(GameGui _gameGui, SigScanner sigScanner, DataManager dataManager)
        {
            gameGui = _gameGui;
            MapSig1 = sigScanner.GetStaticAddressFromSig("44 8B 3D ?? ?? ?? ?? 45 85 FF");
            MapSig2 = sigScanner.GetStaticAddressFromSig("44 0F 44 3D ?? ?? ?? ??");
            UpdateNavMapPointer();
            Maps = dataManager.GetExcelSheet<Map>();
            updateNaviMap();
            updateMap();
        }

        public static void UpdateNavMapPointer()
        {
            if(gameGui == null) { return; }
            naviMapPointer = gameGui.GetAddonByName("_NaviMap", 1);
        }

        public static bool updateNaviMap()
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
                visible = _naviMapPtr->IsVisible;
            }

            return true;
        }

        public static void updateOncePerZone(GameGui gameGui)
        {
            if(naviMapPointer == IntPtr.Zero)
            {
                UpdateNavMapPointer();
            }

            updateMap();
        }

        public unsafe static void CheckIfLoading()
        {
            var LocationTitle = (AtkUnitBase*)gameGui.GetAddonByName("_LocationTitle", 1);
            var FadeMiddle = (AtkUnitBase*)gameGui.GetAddonByName("FadeMiddle", 1);
            loading =
                (LocationTitle != null && LocationTitle->IsVisible) ||
                (FadeMiddle != null && FadeMiddle->IsVisible);
        }

        public static void updateMap()
        {
            if (Maps != null)
            {
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

        private unsafe static uint getMapId()
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
