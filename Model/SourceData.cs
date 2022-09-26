
using System;
using ImGuiNET;
using Newtonsoft.Json;

namespace MiniMappingway.Model
{
    [Serializable]
#pragma warning disable CS0660, CS0661
    public class SourceData : IEquatable<SourceData>
#pragma warning restore CS0660, CS0661
    {
        public bool Enabled { get; set; } = true;

        public uint Color { get; set; }

        [NonSerialized]
        private uint? _autoBorderColor;

        public float BorderDarkeningAmount { get; set; } = 0.7f;

        public int Priority { get; set; } = 1;

        public bool ShowBorder { get; set; } = true;

        public int CircleSize { get; set; } = 6;

        public bool BorderValid { get; set; } = true;

        public int BorderRadius { get; set; } = 2;

        public uint AutoBorderColour
        {
            get
            {
                if (_autoBorderColor != null && BorderValid)
                {
                    return (uint)_autoBorderColor;
                }
                var temp = ImGui.ColorConvertU32ToFloat4(Color);

                temp.Z *= BorderDarkeningAmount;
                temp.X *= BorderDarkeningAmount;
                temp.Y *= BorderDarkeningAmount;
                _autoBorderColor = ImGui.ColorConvertFloat4ToU32(temp);
                BorderValid = true;
                return (uint)_autoBorderColor;
            }
        }
        
        public SourceData(uint color)
        {
            Color = color; 
        }

        public bool Equals(SourceData? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Enabled == other.Enabled && Color == other.Color && BorderDarkeningAmount.Equals(other.BorderDarkeningAmount) && Priority == other.Priority && ShowBorder == other.ShowBorder && CircleSize == other.CircleSize && BorderRadius == other.BorderRadius;
        }

#pragma warning disable CS0659
        public override bool Equals(object? obj)
#pragma warning restore CS0659
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SourceData)obj);
        }
        

        public static bool operator ==(SourceData? left, SourceData? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SourceData? left, SourceData? right)
        {
            return !Equals(left, right);
        }

        public SourceData(SourceData data)
        {
            Color = data.Color;

            BorderDarkeningAmount = data.BorderDarkeningAmount;

            Priority = data.Priority;

            ShowBorder = data.ShowBorder;

            CircleSize = data.CircleSize;

            BorderValid = data.BorderValid;

            BorderRadius = data.BorderRadius;

            Enabled = data.Enabled;
        }

        [JsonConstructor]
        public SourceData(uint color, float borderDarkeningAmount, int priority, bool showBorder, int circleSize, bool borderValid, int borderRadius, bool enabled)
        {
            Color = color;
            BorderDarkeningAmount = borderDarkeningAmount;
            Priority = priority;
            ShowBorder = showBorder;
            CircleSize = circleSize;
            BorderValid = borderValid;
            BorderRadius = borderRadius;
            Enabled = enabled;
        }
    }



}
