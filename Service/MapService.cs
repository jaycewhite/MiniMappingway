using System;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using MiniMappingway.Manager;
using MiniMappingway.Model;
using MiniMappingway.Service.Interface;

namespace MiniMappingway.Service
{
    public unsafe class MapService : IDisposable, IMapService
    {
        private AtkUnitBase* NaviMapPointer => (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("_NaviMap", 1);

        private readonly ExcelSheet<Map>? _maps;

        [Signature("44 8B 3D ?? ?? ?? ?? 45 85 FF", ScanType = ScanType.StaticAddress)]
        private readonly uint* _mapSig1 = null!;

        [Signature("44 0F 44 3D ?? ?? ?? ??", ScanType = ScanType.StaticAddress)]
        private readonly uint* _mapSig2 = null!;

        public MapData Data { get; }

        public bool IsLoading => CheckIfLoading();

        public MapService()
        {
            SignatureHelper.Initialise(this);

            _maps = ServiceManager.DataManager.GetExcelSheet<Map>();
            UpdateNaviMap();
            UpdateMap();
            Data = new MapData();
        }

        public bool UpdateNaviMap()
        {

            if (NaviMapPointer == null)
            {
                return false;
            }

            //There's probably a better way of doing this but I don't know it for now
            Data.IsLocked = ((AtkComponentCheckBox*)NaviMapPointer->GetNodeById(4)->GetComponent())->IsChecked;

            var rotationPtr = (float*)((nint)NaviMapPointer + 0x254);
            var naviScalePtr = (float*)((nint)NaviMapPointer + 0x24C);
            if (NaviMapPointer->UldManager.LoadedState != AtkLoadState.Loaded)
            {
                return false;
            }
            try
            {
                Data.Rotation = *rotationPtr;
                Data.Zoom = *naviScalePtr;
            }
            catch
            {
                // ignored
            }

            Data.X = NaviMapPointer->X;
            Data.Y = NaviMapPointer->Y;
            Data.NaviScale = NaviMapPointer->Scale;
            Data.Visible = (NaviMapPointer->VisibilityFlags & 0x03) == 0;
            return true;
        }

        private bool CheckIfLoading()
        {
            var locationTitle = (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("_LocationTitle", 1);
            var fadeMiddle = (AtkUnitBase*)ServiceManager.GameGui.GetAddonByName("FadeMiddle", 1);
            return locationTitle->IsVisible ||
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
                    Data.ZoneScale = (float)map.SizeFactor / 100;
                }
                else
                {
                    Data.ZoneScale = 1;
                }

                Data.OffsetX = map.OffsetX;
                Data.OffsetY = map.OffsetY;

            }
        }

        private uint GetMapId()
        {
            return *_mapSig1 == 0 ? *_mapSig2 : *_mapSig1;
        }



        public void Dispose()
        {
        }


    }
}
