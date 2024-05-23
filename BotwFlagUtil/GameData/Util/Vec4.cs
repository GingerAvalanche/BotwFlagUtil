using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using BymlLibrary.Nodes.Immutable.Containers;
using System.Runtime.InteropServices;

namespace BotwFlagUtil.GameData.Util
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct Vec4 : IEquatable<Vec4>
    {
        [FieldOffset(0)]
        private float _x;
        [FieldOffset(4)]
        private float _y;
        [FieldOffset(8)]
        private float _z;
        [FieldOffset(12)]
        private float _w;
        public float X { readonly get => _x; set => _x = value; }
        public float Y { readonly get => _y; set => _y = value; }
        public float Z { readonly get => _z; set => _z = value; }
        public float W { readonly get => _w; set => _w = value; }

        public Vec4(string s)
        {
            float[] floats = s[1..^1].Split(",").Select(float.Parse).ToArray();
            if (floats.Length != 4) throw new ArgumentException($"Must be length 4, was length {s}", nameof(s));
            _x = floats[0];
            _y = floats[1];
            _z = floats[2];
            _w = floats[3];
        }

        public Vec4(ImmutableByml byml)
        {
            ImmutableBymlArray array = byml.GetArray()[0].GetArray();
            _x = array[0].GetFloat();
            _y = array[1].GetFloat();
            _z = array[2].GetFloat();
            _w = array[3].GetFloat();
        }

        public Vec4(Byml byml)
        {
            BymlArray array = byml.GetArray()[0].GetArray();
            _x = array[0].GetFloat();
            _y = array[1].GetFloat();
            _z = array[2].GetFloat();
            _w = array[3].GetFloat();
        }

        public Vec4(float x, float y, float z, float w)
        {
            _x = x;
            _y = y;
            _z = z;
            _w = w;
        }

        public Vec4(float[] floats)
        {
            if (floats.Length != 4) throw new ArgumentException($"Must be length 4, was length {floats.Length}", nameof(floats));
            _x = floats[0];
            _y = floats[1];
            _z = floats[2];
            _w = floats[3];
        }

        public readonly Byml ToByml()
        {
            return new(new BymlArray([new Byml(new BymlArray([new Byml(_x), new Byml(_y), new Byml(_z), new Byml(_w)]))]));
        }

        public static bool operator ==(Vec4 first, Vec4 second) => first.Equals(second);
        public static bool operator !=(Vec4 first, Vec4 second) => !first.Equals(second);
        public static implicit operator Vec4(ImmutableByml byml) => new(byml);
        public static implicit operator Vec4(Byml byml) => new(byml);

        public readonly bool Equals(Vec4 other) => _x == other.X && _y == other.Y && _z == other.Z && _w == other.W;

        public readonly override bool Equals(object? obj) => obj is Vec4 vec && Equals(vec);

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(_x, _y, _z, _w);
        }

        public readonly override string ToString()
        {
            return $"[{_x:0.0###}, {_y:0.0###}, {_z:0.0###}, {_w:0.0###}]";
        }
    }
}
