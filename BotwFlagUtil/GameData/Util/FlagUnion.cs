using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotwFlagUtil.GameData.Util
{
    public enum FlagUnionType : ushort
    {
        None = 0x0,
        Bool = 0x1,
        BoolArray = 0x2,
        F32 = 0x4,
        F32Array = 0x8,
        S32 = 0x10,
        S32Array = 0x20,
        String = 0x40,
        StringArray = 0x80,
        Vec2 = 0x100,
        Vec2Array = 0x200,
        Vec3 = 0x400,
        Vec3Array = 0x800,
        Vec4 = 0x1000,
        ContainerTypes = 0xAAA,
    }
    [JsonConverter(typeof(FlagConverter))]
    [StructLayout(LayoutKind.Explicit, Size = 32, CharSet = CharSet.Ansi)]
    public struct FlagUnion : IEquatable<FlagUnion>
    {
        [FieldOffset(0)]
        public readonly FlagUnionType Type;
        [FieldOffset(4)]
        public bool? boolValue;
        [FieldOffset(24)]
        public List<bool>? boolArrayValue;
        [FieldOffset(4)]
        public float? f32Value;
        [FieldOffset(24)]
        public List<float>? f32ArrayValue;
        [FieldOffset(4)]
        public int? s32Value;
        [FieldOffset(24)]
        public List<int>? s32ArrayValue;
        [FieldOffset(24)]
        public string? stringValue;
        [FieldOffset(24)]
        public List<string>? stringArrayValue;
        [FieldOffset(4)]
        public Vec2? vec2Value;
        [FieldOffset(24)]
        public List<Vec2>? vec2ArrayValue;
        [FieldOffset(4)]
        public Vec3? vec3Value;
        [FieldOffset(24)]
        public List<Vec3>? vec3ArrayValue;
        [FieldOffset(4)]
        public Vec4? vec4Value;

        public FlagUnion(FlagUnionType t)
        {
            if (t > FlagUnionType.Vec4)
            {
                throw new ArgumentException($"Cannot instantiate flag of type {t}");
            }
            Type = t;

            switch (t)
            {
                case FlagUnionType.Bool:
                    boolValue = false;
                    break;
                case FlagUnionType.BoolArray:
                    boolArrayValue = [false];
                    break;
                case FlagUnionType.F32:
                    f32Value = 0f;
                    break;
                case FlagUnionType.F32Array:
                    f32ArrayValue = [0f];
                    break;
                case FlagUnionType.S32:
                    s32Value = 0;
                    break;
                case FlagUnionType.S32Array:
                    s32ArrayValue = [0];
                    break;
                case FlagUnionType.String:
                    stringValue = "";
                    break;
                case FlagUnionType.StringArray:
                    stringArrayValue = [""];
                    break;
                case FlagUnionType.Vec2:
                    vec2Value = new();
                    break;
                case FlagUnionType.Vec2Array:
                    vec2ArrayValue = [new()];
                    break;
                case FlagUnionType.Vec3:
                    vec3Value = new();
                    break;
                case FlagUnionType.Vec3Array:
                    vec3ArrayValue = [new()];
                    break;
                case FlagUnionType.Vec4:
                    vec4Value = new();
                    break;
            }
        }
        public static implicit operator FlagUnion(FlagUnionType t) => new(t);

        public FlagUnion(bool v)
        {
            Type = FlagUnionType.Bool;
            boolValue = v;
        }
        public static implicit operator FlagUnion(bool v) => new(v);

        public FlagUnion(List<bool> v)
        {
            Type = FlagUnionType.BoolArray;
            boolArrayValue = v;
        }
        public static implicit operator FlagUnion(List<bool> v) => new(v);

        public FlagUnion(float v)
        {
            Type = FlagUnionType.F32;
            f32Value = v;
        }
        public static implicit operator FlagUnion(float v) => new(v);

        public FlagUnion(List<float> v)
        {
            Type = FlagUnionType.F32Array;
            f32ArrayValue = v;
        }
        public static implicit operator FlagUnion(List<float> v) => new(v);

        public FlagUnion(int v)
        {
            Type = FlagUnionType.S32;
            s32Value = v;
        }
        public static implicit operator FlagUnion(int v) => new(v);

        public FlagUnion(List<int> v)
        {
            Type = FlagUnionType.S32Array;
            s32ArrayValue = v;
        }
        public static implicit operator FlagUnion(List<int> v) => new(v);

        public FlagUnion(string v)
        {
            Type = FlagUnionType.String;
            stringValue = v;
        }
        public static implicit operator FlagUnion(string v) => new(v);

        public FlagUnion(List<string> v)
        {
            Type = FlagUnionType.StringArray;
            stringArrayValue = v;
        }
        public static implicit operator FlagUnion(List<string> v) => new(v);

        public FlagUnion(Vec2 v)
        {
            Type = FlagUnionType.Vec2;
            vec2Value = v;
        }
        public static implicit operator FlagUnion(Vec2 v) => new(v);

        public FlagUnion(List<Vec2> v)
        {
            Type = FlagUnionType.Vec2Array;
            vec2ArrayValue = v;
        }
        public static implicit operator FlagUnion(List<Vec2> v) => new(v);

        public FlagUnion(Vec3 v)
        {
            Type = FlagUnionType.Vec3;
            vec3Value = v;
        }
        public static implicit operator FlagUnion(Vec3 v) => new(v);

        public FlagUnion(List<Vec3> v)
        {
            Type = FlagUnionType.Vec3Array;
            vec3ArrayValue = v;
        }
        public static implicit operator FlagUnion(List<Vec3> v) => new(v);

        public FlagUnion(Vec4 v)
        {
            Type = FlagUnionType.Vec4;
            vec4Value = v;
        }
        public static implicit operator FlagUnion(Vec4 v) => new(v);

        public FlagUnion(List<float[]> v)
        {
            if (v.All(l => l.Length == 2))
            {
                Type = FlagUnionType.Vec2Array;
                vec2ArrayValue = v.Select(l => new Vec2(l)).ToList();
            }
            else if (v.All(l => l.Length == 3))
            {
                Type = FlagUnionType.Vec3Array;
                vec3ArrayValue = v.Select(l => new Vec3(l)).ToList();
            }
            else
            {
                throw new ArgumentException($"Vector array made with incorrect or inconsistent numbers of values in array, these must all be 2 or all be 3: [{string.Join(", ", v.Select(l => l.Length))}]", nameof(v));
            }
        }
        public static implicit operator FlagUnion(List<float[]> v) => new(v);

        public FlagUnion(List<FlagUnion> flags)
        {
            FlagUnionType firstType = flags[0].Type;
            if (firstType == FlagUnionType.Vec4)
            {
                throw new ArgumentException($"{firstType + 1} is not a Container type", nameof(flags));
            }
            if (!flags.All(f => f.Type == firstType))
            {
                throw new ArgumentException($"Not all flag types match {firstType}", nameof(flags));
            }
            Type = Helpers.singleTypeToArrayType[firstType];

            switch (Type)
            {
                case FlagUnionType.BoolArray:
                    boolArrayValue = flags.Select(f => f.boolValue!.Value).ToList();
                    break;
                case FlagUnionType.F32Array:
                    f32ArrayValue = flags.Select(f => f.f32Value!.Value).ToList();
                    break;
                case FlagUnionType.S32Array:
                    s32ArrayValue = flags.Select(f => f.s32Value!.Value).ToList();
                    break;
                case FlagUnionType.StringArray:
                    stringArrayValue = flags.Select(f => f.stringValue!).ToList();
                    break;
                case FlagUnionType.Vec2Array:
                    vec2ArrayValue = flags.Select(f => f.vec2Value!.Value).ToList();
                    break;
                case FlagUnionType.Vec3Array:
                    vec3ArrayValue = flags.Select(f => f.vec3Value!.Value).ToList();
                    break;
            }
        }
        public static implicit operator FlagUnion(List<FlagUnion> v) => new(v);

        public readonly bool Bool => boolValue!.Value;
        public readonly List<bool> BoolArray => boolArrayValue!;
        public readonly float F32 => f32Value!.Value;
        public readonly List<float> F32s => f32ArrayValue!;
        public readonly int S32 => s32Value!.Value;
        public readonly List<int> S32s => s32ArrayValue!;
        public readonly string String => stringValue!;
        public readonly List<string> Strings => stringArrayValue!;
        public readonly Vec2 Vec2 => vec2Value!.Value;
        public readonly List<Vec2> Vec2s => vec2ArrayValue!;
        public readonly Vec3 Vec3 => vec3Value!.Value;
        public readonly List<Vec3> Vec3s => vec3ArrayValue!;
        public readonly Vec4 Vec4 => vec4Value!.Value;

        public static bool operator ==(FlagUnion lhs, FlagUnion rhs) => lhs.Equals(rhs);
        public static bool operator !=(FlagUnion lhs, FlagUnion rhs) => !lhs.Equals(rhs);

        public override readonly bool Equals(object? obj)
        {
            if (obj is FlagUnion flag) return Equals(flag);
            return false;
        }

        public readonly bool Equals(FlagUnion other)
        {
            if (Type != other.Type) return false;
            if (IsContainerType())
            {
                return Type switch
                {
                    FlagUnionType.BoolArray => boolArrayValue!.SequenceEqual(other.boolArrayValue!),
                    FlagUnionType.F32Array => f32ArrayValue!.SequenceEqual(other.f32ArrayValue!),
                    FlagUnionType.S32Array => s32ArrayValue!.SequenceEqual(other.s32ArrayValue!),
                    FlagUnionType.StringArray => stringArrayValue!.SequenceEqual(other.stringArrayValue!),
                    FlagUnionType.Vec2Array => vec2ArrayValue!.SequenceEqual(other.vec2ArrayValue!),
                    FlagUnionType.Vec3Array => vec3ArrayValue!.SequenceEqual(other.vec3ArrayValue!),
                    _ => throw new InvalidOperationException($"Somehow {Type} got read as container type"),
                };
            }
            return vec4Value == other.vec4Value;
        }

        private readonly bool IsContainerType()
        {
            return Type switch
            {
                FlagUnionType.BoolArray or
                FlagUnionType.F32Array or
                FlagUnionType.S32Array or
                FlagUnionType.StringArray or
                FlagUnionType.Vec2Array or
                FlagUnionType.Vec3Array => true,
                _ => false,
            };
        }

        public override readonly string ToString()
        {
            return Type switch
            {
                FlagUnionType.Bool => $"{boolValue!.Value}",
                FlagUnionType.BoolArray => $"[{string.Join(", ", boolArrayValue!)}]",
                FlagUnionType.F32 => $"{f32Value!.Value}",
                FlagUnionType.F32Array => $"[{string.Join(", ", f32ArrayValue!)}]",
                FlagUnionType.S32 => $"{s32Value!.Value}",
                FlagUnionType.S32Array => $"[{string.Join(", ", s32ArrayValue!)}]",
                FlagUnionType.String => $"{stringValue!}",
                FlagUnionType.StringArray => $@"[""{string.Join(@""", """, stringArrayValue!)}""]",
                FlagUnionType.Vec2 => $"{vec2Value!.Value}",
                FlagUnionType.Vec2Array => $"[{string.Join(", ", vec2ArrayValue!)}]",
                FlagUnionType.Vec3 => $"{vec3Value!.Value}",
                FlagUnionType.Vec3Array => $"[{string.Join(", ", vec3ArrayValue!)}]",
                FlagUnionType.Vec4 => $"{vec4Value!.Value}",
                _ => throw new InvalidOperationException("FlagUnionType.None should never be instantiated"),
            };
        }

        public override readonly int GetHashCode()
        {
            return Type switch
            {
                FlagUnionType.Bool => HashCode.Combine(Type, boolValue),
                FlagUnionType.BoolArray => HashCode.Combine(Type, boolArrayValue!.Select(b => b.GetHashCode())),
                FlagUnionType.F32 => HashCode.Combine(Type, f32Value),
                FlagUnionType.F32Array => HashCode.Combine(Type, f32ArrayValue!.Select(f => f.GetHashCode())),
                FlagUnionType.S32 => HashCode.Combine(Type, s32Value),
                FlagUnionType.S32Array => HashCode.Combine(Type, s32ArrayValue!.Select(i => i.GetHashCode())),
                FlagUnionType.String => HashCode.Combine(Type, stringValue),
                FlagUnionType.StringArray => HashCode.Combine(Type, stringArrayValue!.Select(s => s.GetHashCode())),
                FlagUnionType.Vec2 => HashCode.Combine(Type, vec2Value),
                FlagUnionType.Vec2Array => HashCode.Combine(Type, vec2ArrayValue!.Select(v => v.GetHashCode())),
                FlagUnionType.Vec3 => HashCode.Combine(Type, vec3Value),
                FlagUnionType.Vec3Array => HashCode.Combine(Type, vec3ArrayValue!.Select(v => v.GetHashCode())),
                FlagUnionType.Vec4 => HashCode.Combine(Type, vec4Value),
                _ => throw new InvalidOperationException("FlagUnionType.None should never be instantiated"),
            };
        }

        public readonly Byml ToByml()
        {
            // God, the hoop-jumping is *ridiculous*
            // Vecs are the worst, but luckily their arrays are consistent with arrays of other types, so we can reuse the pattern
            return Type switch
            {
                // Bool
                FlagUnionType.Bool => boolValue!.Value,
                // Array[1].Map["Values"].Array[many].Bool
                FlagUnionType.BoolArray => new(new BymlArray([new Byml(new Dictionary<string, Byml>() { { "Values", new Byml(new BymlArray(boolArrayValue!.Select(b => new Byml(b)))) } })])),
                // Float
                FlagUnionType.F32 => f32Value!.Value,
                // Array[1].Map["Values"].Array[many].Float
                FlagUnionType.F32Array => new(new BymlArray([new Byml(new Dictionary<string, Byml>() { { "Values", new Byml(new BymlArray(f32ArrayValue!.Select(f => new Byml(f)))) } })])),
                // Int
                FlagUnionType.S32 => s32Value!.Value,
                // Array[1].Map["Values"].Array[many].Int
                FlagUnionType.S32Array => new(new BymlArray([new Byml(new Dictionary<string, Byml>() { { "Values", new Byml(new BymlArray(s32ArrayValue!.Select(s => new Byml(s)))) } })])),
                // String
                FlagUnionType.String => stringValue!,
                // Array[1].Map["Values"].Array[many].String
                FlagUnionType.StringArray => new(new BymlArray([new Byml(new Dictionary<string, Byml>() { { "Values", new Byml(new BymlArray(stringArrayValue!.Select(s => new Byml(s)))) } })])),
                // Array[1].Vec
                FlagUnionType.Vec2 => vec2Value!.Value.ToByml(),
                // Array[1].Map["Values"].Array[many].Array[1].Vec
                FlagUnionType.Vec2Array => new(new BymlArray([new Byml(new Dictionary<string, Byml>() { { "Values", new Byml(new BymlArray(vec2ArrayValue!.Select(v => v.ToByml()))) } })])),
                // Array[1].Vec
                FlagUnionType.Vec3 => vec3Value!.Value.ToByml(),
                // Array[1].Map["Values"].Array[many].Array[1].Vec
                FlagUnionType.Vec3Array => new(new BymlArray([new Byml(new Dictionary<string, Byml>() { { "Values", new Byml(new BymlArray(vec3ArrayValue!.Select(v => v.ToByml()))) } })])),
                // Array[1].Vec
                FlagUnionType.Vec4 => vec4Value!.Value.ToByml(),
                _ => throw new InvalidOperationException($"{Type} should never be instantiated"),
            };
        }

        private class FlagConverter : JsonConverter<FlagUnion>
        {
            public override FlagUnion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                JsonTokenType type = reader.TokenType;
                if (type == JsonTokenType.True || type == JsonTokenType.False)
                {
                    if (TryReadBool(ref reader, out FlagUnion? value))
                    {
                        return value.Value;
                    }
                }
                else if (type == JsonTokenType.Number)
                {
                    if (TryReadNumber(ref reader, out FlagUnion? value))
                    {
                        return value.Value;
                    }
                }
                else if (type == JsonTokenType.String)
                {
                    if (TryReadString(ref reader, out FlagUnion? value))
                    {
                        return value.Value;
                    }
                }
                else if (type == JsonTokenType.StartArray)
                {
                    if (TryReadArray(ref reader, out FlagUnion? value))
                    {
                        return value.Value;
                    }
                }
                else if (type == JsonTokenType.StartObject)
                {
                    if (TryReadVec(ref reader, out FlagUnion? value))
                    {
                        return value.Value;
                    }
                    Console.WriteLine($"Read {type}");
                }
                throw new NotImplementedException($"{type} not valid JSON token for Flag");
            }

            private static bool TryReadBool(ref Utf8JsonReader reader, [NotNullWhen(true)] out FlagUnion? value)
            {
                if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
                {
                    value = reader.GetBoolean();
                    return true;
                }
                value = null;
                return false;
            }

            private static bool TryReadNumber(ref Utf8JsonReader reader, [NotNullWhen(true)] out FlagUnion? value)
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetSingle(out float f))
                    {
                        value = f;
                        return true;
                    }
                    else if (reader.TryGetInt32(out int i))
                    {
                        value = i;
                        return true;
                    }
                }
                value = null;
                return false;
            }

            public static bool TryReadString(ref Utf8JsonReader reader, [NotNullWhen(true)] out FlagUnion? value)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    string? v = reader.GetString();
                    if (v != null)
                    {
                        value = v;
                        return true;
                    }
                }
                value = null;
                return false;
            }

            public static bool TryReadVec(ref Utf8JsonReader reader, [NotNullWhen(true)] out FlagUnion? value)
            {
                float[] result = new float[4];
                int highest = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        value = highest switch
                        {
                            1 => new Vec2(result[..2]),
                            2 => new Vec3(result[..3]),
                            3 => new Vec4(result),
                            _ => null,
                        };
                        return value.HasValue;
                    }
                    float v;
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string propertyName = reader.GetString()!;
                        reader.Read();
                        switch (propertyName)
                        {
                            case "X":
                                if (reader.TryGetSingle(out v))
                                {
                                    result[0] = v;
                                }
                                break;
                            case "Y":
                                if (reader.TryGetSingle(out v))
                                {
                                    result[1] = v;
                                    highest = highest > 1 ? highest : 1;
                                }
                                break;
                            case "Z":
                                if (reader.TryGetSingle(out v))
                                {
                                    result[2] = v;
                                    highest = highest > 2 ? highest : 2;
                                }
                                break;
                            case "W":
                                if (reader.TryGetSingle(out v))
                                {
                                    result[3] = v;
                                    highest = highest > 3 ? highest : 3;
                                }
                                break;
                        }
                    }
                }
                value = null;
                return false;
            }

            private static bool TryReadArray(ref Utf8JsonReader reader, [NotNullWhen(true)] out FlagUnion? value)
            {
                List<FlagUnion> result = [];
                reader.Read();
                JsonTokenType type = reader.TokenType;
                if (type == JsonTokenType.True || type == JsonTokenType.False)
                {
                    while (TryReadBool(ref reader, out FlagUnion? v))
                    {
                        result.Add(v.Value);
                        reader.Read();
                    }
                    value = result;
                    return true;
                }
                else if (type == JsonTokenType.Number)
                {
                    while (TryReadNumber(ref reader, out FlagUnion? v))
                    {
                        result.Add(v.Value);
                        reader.Read();
                    }
                    value = result;
                    return true;
                }
                else if (type == JsonTokenType.String)
                {
                    while (TryReadString(ref reader, out FlagUnion? v))
                    {
                        result.Add(v.Value);
                        reader.Read();
                    }
                    value = result;
                    return true;
                }
                else if (type == JsonTokenType.StartObject)
                {
                    while (TryReadVec(ref reader, out FlagUnion? v))
                    {
                        result.Add(v.Value);
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.EndArray)
                        {
                            break;
                        }
                    }
                    value = result;
                    return true;
                }
                value = null;
                return false;
            }

            public override void Write(Utf8JsonWriter writer, FlagUnion value, JsonSerializerOptions options)
            {
                switch (value.Type)
                {
                    case FlagUnionType.Bool:
                        JsonSerializer.Serialize(writer, value.boolValue!.Value);
                        break;
                    case FlagUnionType.BoolArray:
                        JsonSerializer.Serialize(writer, value.boolArrayValue!);
                        break;
                    case FlagUnionType.F32:
                        JsonSerializer.Serialize(writer, value.f32Value!.Value, FlagHelpers.floatOptions);
                        break;
                    case FlagUnionType.F32Array:
                        JsonSerializer.Serialize(writer, value.f32ArrayValue!, FlagHelpers.floatListOptions);
                        break;
                    case FlagUnionType.S32:
                        JsonSerializer.Serialize(writer, value.s32Value!.Value);
                        break;
                    case FlagUnionType.S32Array:
                        JsonSerializer.Serialize(writer, value.s32ArrayValue!);
                        break;
                    case FlagUnionType.String:
                        JsonSerializer.Serialize(writer, value.stringValue!);
                        break;
                    case FlagUnionType.StringArray:
                        JsonSerializer.Serialize(writer, value.stringArrayValue!);
                        break;
                    case FlagUnionType.Vec2:
                        JsonSerializer.Serialize(writer, value.vec2Value!.Value);
                        break;
                    case FlagUnionType.Vec2Array:
                        JsonSerializer.Serialize(writer, value.vec2ArrayValue!);
                        break;
                    case FlagUnionType.Vec3:
                        JsonSerializer.Serialize(writer, value.vec3Value!.Value);
                        break;
                    case FlagUnionType.Vec3Array:
                        JsonSerializer.Serialize(writer, value.vec3ArrayValue!);
                        break;
                    case FlagUnionType.Vec4:
                        JsonSerializer.Serialize(writer, value.vec4Value!.Value);
                        break;
                    default:
                        throw new InvalidOperationException($"FlagUnion must have flag type, had {value.Type}");
                }
            }
        }
    }
}
