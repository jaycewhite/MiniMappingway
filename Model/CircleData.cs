using System.Numerics;

namespace MiniMappingway.Model
{
    public class CircleData
    {
        public Vector2 Position;
        public readonly string SourceName;

        public CircleData(Vector2 position, string sourceName)
        {
            Position = position;
            SourceName = sourceName;
        }
        
    }
}
