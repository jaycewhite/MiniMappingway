using System.Numerics;

namespace MiniMappingway.Model;

public class MapData
{
    public int X;
    public int Y;
    public float NaviScale;
    public float ZoneScale;
    public float Rotation;
    public bool Visible;
    public float Zoom;
    public short OffsetX;
    public short OffsetY;
    public bool IsLocked;
    public readonly int NaviMapSize = 218;
    public float MinimapRadius => MapSize.X* 0.315f;
    public Vector2 MapSize => new Vector2(NaviMapSize* NaviScale, NaviMapSize* NaviScale);


    public MapData()
    {
    }
}