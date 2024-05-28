using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using BymlLibrary.Nodes.Immutable.Containers;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace BotwFlagUtil.GameData.Util
{
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct Vec3 : IEquatable<Vec3>
    {
        [FieldOffset(0)]
        private float _x;
        [FieldOffset(4)]
        private float _y;
        [FieldOffset(8)]
        private float _z;
        public float X { readonly get => _x; set => _x = value; }
        public float Y { readonly get => _y; set => _y = value; }
        public float Z { readonly get => _z; set => _z = value; }

        public Vec3(string s)
        {
            float[] floats = s[1..^1].Split(",").Select(float.Parse).ToArray();
            if (floats.Length != 3) throw new FormatException($"Must be length 3, was length {s}");
            _x = floats[0];
            _y = floats[1];
            _z = floats[2];
        }

        public Vec3(ImmutableByml byml)
        {
            ImmutableBymlArray array = byml.GetArray()[0].GetArray();
            _x = array[0].GetFloat();
            _y = array[1].GetFloat();
            _z = array[2].GetFloat();
        }

        public Vec3(Byml byml)
        {
            BymlArray array = byml.GetArray()[0].GetArray();
            _x = array[0].GetFloat();
            _y = array[1].GetFloat();
            _z = array[2].GetFloat();
        }

        public Vec3(float x, float y, float z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public Vec3(float[] floats)
        {
            if (floats.Length != 3) throw new ArgumentException($"Must be length 3, was length {floats.Length}", nameof(floats));
            _x = floats[0];
            _y = floats[1];
            _z = floats[2];
        }

        public readonly Byml ToByml()
        {
            return new(new BymlArray([new Byml(new BymlArray([new Byml(_x), new Byml(_y), new Byml(_z)]))]));
        }

        public static bool operator ==(Vec3 first, Vec3 second) => first.Equals(second);
        public static bool operator !=(Vec3 first, Vec3 second) => !first.Equals(second);
        public static implicit operator Vec3(ImmutableByml byml) => new(byml);
        public static implicit operator Vec3(Byml byml) => new(byml);

        public readonly bool Equals(Vec3 other) => _x == other.X && _y == other.Y && _z == other.Z;

        public readonly override bool Equals(object? obj) => obj is Vec3 vec && Equals(vec);

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(_x, _y, _z);
        }

        public readonly override string ToString()
        {
            return $"[{_x:0.0###}, {_y:0.0###}, {_z:0.0###}]";
        }
    }
}
