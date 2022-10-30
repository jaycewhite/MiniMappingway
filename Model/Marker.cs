using System.Numerics;

namespace MiniMappingway.Model
{
    public class Marker
    {
        public Vector2 Position;
        public readonly string SourceName;

        public Marker(Vector2 position, string sourceName)
        {
            Position = position;
            SourceName = sourceName;
        }
        
    }
}
