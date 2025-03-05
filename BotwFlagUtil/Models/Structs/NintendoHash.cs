using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aamp.Security.Cryptography;
using BymlLibrary;

namespace BotwFlagUtil.Models.Structs
{
    [JsonConverter(typeof(NintendoHashConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct NintendoHash : IEquatable<NintendoHash>
    {
        [FieldOffset(0)]
        public int ivalue;
        [FieldOffset(0)]
        public uint uvalue;

        private NintendoHash(Byml byml)
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

        private NintendoHash(ImmutableByml ibyml)
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

        private NintendoHash(int val)
        {
            ivalue = val;
        }
        public static implicit operator NintendoHash(int val) => new(val);

        private NintendoHash(uint val)
        {
            uvalue = val;
        }
        public static implicit operator NintendoHash(uint val) => new(val);

        private NintendoHash(string val)
        {
            uvalue = Crc32.Compute(val);
        }
        public static implicit operator NintendoHash(string val) => new(val);

        public readonly Byml ToHash()
        {
            if (ivalue < 0)
            {
                return new Byml(uvalue);
            }
            return new Byml(ivalue);
        }

        public readonly override string ToString()
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

        private class NintendoHashConverter : JsonConverter<NintendoHash>
        {
            public override NintendoHash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                JsonTokenType type = reader.TokenType;
                if (type == JsonTokenType.Number)
                {
                    if (reader.TryGetInt32(out int intVal))
                    {
                        return intVal;
                    }
                    else if (reader.TryGetUInt32(out uint uintVal))
                    {
                        return uintVal;
                    }
                    else
                    {
                        string? val = reader.GetString();
                        if (val != null && val.StartsWith("!u 0", StringComparison.Ordinal))
                        {
                            return Convert.ToUInt32(val[5..], 16);
                        }
                        throw new JsonException($"NintendoHash must be int or uint, was {val ?? "null"}");
                    }
                }
                throw new JsonException($"NintendoHash must be Number, was given {type}");
            }

            public override void Write(Utf8JsonWriter writer, NintendoHash value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value.ivalue, options);
            }
        }
    }
}