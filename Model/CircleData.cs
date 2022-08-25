using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MiniMappingway.Model
{
    public class CircleData
    {
        public Vector2 Position;
        public CircleCategory Category;

        public CircleData(Vector2 position, CircleCategory category)
        {
            this.Position = position;
            this.Category = category;
        }
    }
}
