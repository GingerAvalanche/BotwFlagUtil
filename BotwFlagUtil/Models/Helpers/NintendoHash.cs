using System;
using System.Runtime.InteropServices;
using BymlLibrary;

namespace BotwFlagUtil
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct NintendoHash : IEquatable<NintendoHash>
    {
        [FieldOffset(0)]
        public int ivalue;
        [FieldOffset(0)]
        public uint uvalue;

        public NintendoHash(Byml byml)
        {
            if (byml.Type == BymlNodeType.Int)
            {
                ivalue = byml.GetInt();
            }
            else
            {
                uvalue = byml.GetUInt32();
            }
        }
        public static implicit operator NintendoHash(Byml byml) => new(byml);

        public NintendoHash(ImmutableByml ibyml)
        {
            if (ibyml.Type == BymlNodeType.Int)
            {
                ivalue = ibyml.GetInt();
            }
            else
            {
                uvalue = ibyml.GetUInt32();
            }
        }
        public static implicit operator NintendoHash(ImmutableByml byml) => new(byml);

        public NintendoHash(int val)
        {
            ivalue = val;
        }
        public static implicit operator NintendoHash(int val) => new(val);

        public NintendoHash(uint val)
        {
            uvalue = val;
        }
        public static implicit operator NintendoHash(uint val) => new(val);

        public readonly Byml ToHash()
        {
            if (ivalue < 0)
            {
                return new Byml(uvalue);
            }
            return new Byml(ivalue);
        }

        public override readonly string ToString()
        {
            if (ivalue < 0)
            {
                return $"!u 0x{uvalue:x8}";
            }
            return ivalue.ToString();
        }

        public static bool operator ==(NintendoHash a, NintendoHash b) => a.uvalue == b.uvalue;
        public static bool operator !=(NintendoHash a, NintendoHash b) => a.uvalue != b.uvalue;

        public readonly bool Equals(NintendoHash other) => uvalue == other.uvalue;
        public readonly override bool Equals(object? other)
        {
            if (other is null) return false;
            if (other is NintendoHash hash) return uvalue == hash.uvalue;
            return false;
        }
        public readonly override int GetHashCode() => uvalue.GetHashCode();
    }
}