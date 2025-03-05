using System;
using System.Linq;
using System.Runtime.InteropServices;
using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using BymlLibrary.Nodes.Immutable.Containers;

namespace BotwFlagUtil.Models.GameData.Util
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Vec2 : IEquatable<Vec2>
    {
        [FieldOffset(0)]
        private float _x;
        [FieldOffset(4)]
        private float _y;
        public float X { readonly get => _x; set => _x = value; }
        public float Y { readonly get => _y; set => _y = value; }

        public Vec2(string s)
        {
            float[] floats = s[1..^1].Split(",").Select(float.Parse).ToArray();
            if (floats.Length != 2) throw new FormatException($"Must be length 2, was {s}");
            _x = floats[0];
            _y = floats[1];
        }

        private Vec2(ImmutableByml byml)
        {
            ImmutableBymlArray array = byml.GetArray()[0].GetArray();
            _x = array[0].GetFloat();
            _y = array[1].GetFloat();
        }

        private Vec2(Byml byml)
        {
            BymlArray array = byml.GetArray()[0].GetArray();
            _x = array[0].GetFloat();
            _y = array[1].GetFloat();
        }

        public Vec2(float x, float y)
        {
            _x = x;
            _y = y;
        }

        public Vec2(float[] floats)
        {
            if (floats.Length != 2) throw new ArgumentException($"Must be length 2, was length {floats.Length}", nameof(floats));
            _x = floats[0];
            _y = floats[1];
        }

        public readonly Byml ToByml()
        {
            return new(new BymlArray([new Byml(new BymlArray([new Byml(_x), new Byml(_y)]))]));
        }

        public static bool operator ==(Vec2 first, Vec2 second) => first.Equals(second);
        public static bool operator !=(Vec2 first, Vec2 second) => !first.Equals(second);
        public static implicit operator Vec2(ImmutableByml byml) => new(byml);
        public static implicit operator Vec2(Byml byml) => new(byml);

        public readonly bool Equals(Vec2 other) => Math.Abs(_x - other.X) < Tolerance &&
                                                   Math.Abs(_y - other.Y) < Tolerance;
        private const float Tolerance = 10e-5f;

        public readonly override bool Equals(object? obj) => obj is Vec2 vec && Equals(vec);

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(_x, _y);
        }

        public readonly override string ToString()
        {
            return $"[{_x:0.0###}, {_y:0.0###}]";
        }
    }
}
