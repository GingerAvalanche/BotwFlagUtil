using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AampLibrary;
using AampLibrary.IO.Hashing;
using AampLibrary.Primitives;
using AampLibrary.Structures;
using BotwFlagUtil.GameData.Util;
using BotwFlagUtil.Models.GameData.Util;
using BymlLibrary;
using BymlLibrary.Extensions;
using BymlLibrary.Nodes.Immutable.Containers;
using Revrs;

namespace BotwFlagUtil.Models
{
    internal static class Extensions
    {
        public static T[] Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = value;
            }
            return array;
        }

        public static void PrintAll(this Dictionary<string, HashSet<string>> dict)
        {
            foreach ((string key, HashSet<string> values) in dict)
            {
                if (values.Count == 0) continue;
                Console.WriteLine(key);
                foreach (string value in values)
                {
                    Console.WriteLine($"- {value}");
                }
            }
        }

        public static List<bool> GetBools(this ImmutableByml node)
        {
            // Array[1].Map["Values"].Array[many].Bool
            ImmutableBymlArray array = node.GetArray()[0].GetMap()[0].Node.GetArray();
            List<bool> result = new(array.Count);
            foreach (ImmutableByml boolean in array)
            {
                result.Add(boolean.GetBool());
            }
            return result;
        }

        private static List<float> GetFloats(this ImmutableByml node)
        {
            // Array[1].Map["Values"].Array[many].Float
            ImmutableBymlArray array = node.GetArray()[0].GetMap()[0].Node.GetArray();
            List<float> result = new(array.Count);
            foreach (ImmutableByml f32 in array)
            {
                result.Add(f32.GetFloat());
            }
            return result;
        }

        private static List<int> GetInts(this ImmutableByml node)
        {
            // Array[1].Map["Values"].Array[many].Int
            ImmutableBymlArray array = node.GetArray()[0].GetMap()[0].Node.GetArray();
            List<int> result = new(array.Count);
            foreach (ImmutableByml s32 in array)
            {
                result.Add(s32.GetInt());
            }
            return result;
        }

        private static List<string> GetStrings(this ImmutableByml node, ImmutableBymlStringTable stringTable)
        {
            // Array[1].Map["Values"].Array[many].String
            ImmutableBymlArray array = node.GetArray()[0].GetMap()[0].Node.GetArray();
            List<string> result = new(array.Count);
            foreach (ImmutableByml str in array)
            {
                result.Add(stringTable[str.GetStringIndex()].ToManaged());
            }
            return result;
        }

        private static float[] GetVec(this ImmutableByml node)
        {
            // Array[1].Vec
            ImmutableBymlArray array = node.GetArray()[0].GetArray();
            float[] result = new float[array.Count];
            int i = 0;
            foreach (ImmutableByml f32 in array)
            {
                result[i++] = f32.GetFloat();
            }
            return result;
        }

        private static List<float[]> GetVecArray(this ImmutableByml node)
        {
            // Array[1].Map["Values"].Array[many].Array[1].Vec
            ImmutableBymlArray array = node.GetArray()[0].GetMap()[0].Node.GetArray();
            List<float[]> result = new(array.Count);
            foreach (ImmutableByml obj in array)
            {
                ImmutableBymlArray array2 = obj.GetArray()[0].GetArray();
                float[] result2 = new float[array2.Count];
                int i = 0;
                foreach (ImmutableByml f32 in array2)
                {
                    result2[i++] = f32.GetFloat();
                }
                result.Add(result2);
            }
            return result;
        }

        public static FlagUnion GetFlagUnion(this ImmutableByml node,
            FlagUnionType type,
            ImmutableBymlStringTable stringTable)
        {
            return type switch
            {
                FlagUnionType.Bool => node.GetBool(),
                FlagUnionType.BoolArray => node.GetInts(),
                FlagUnionType.F32 => node.GetFloat(),
                FlagUnionType.F32Array => node.GetFloats(),
                FlagUnionType.S32 => node.GetInt(),
                FlagUnionType.S32Array => node.GetInts(),
                FlagUnionType.String => stringTable[node.GetStringIndex()].ToManaged(),
                FlagUnionType.StringArray => node.GetStrings(stringTable),
                FlagUnionType.Vec2 => new Vec2(node.GetVec()),
                FlagUnionType.Vec2Array => node.GetVecArray(),
                FlagUnionType.Vec3 => new Vec3(node.GetVec()),
                FlagUnionType.Vec3Array => node.GetVecArray(),
                FlagUnionType.Vec4 => new Vec4(node.GetVec()),
                _ => throw new ArgumentException($"Invalid flag type: {type}", nameof(type)),
            };
        }

        // Array[1].Map["Values"].Array[many].Bool
        public static List<bool> GetBools(this Byml node) =>
            node.GetArray()[0]
                .GetMap()["Values"]
                .GetArray()
                .Select(n => n.GetBool())
                .ToList();

        // Array[1].Map["Values"].Array[many].Float
        private static List<float> GetFloats(this Byml node) =>
            node.GetArray()[0]
                .GetMap()["Values"]
                .GetArray()
                .Select(n => n.GetFloat())
                .ToList();

        // Array[1].Map["Values"].Array[many].Int
        private static List<int> GetInts(this Byml node) =>
            node.GetArray()[0]
                .GetMap()["Values"]
                .GetArray()
                .Select(n => n.GetInt())
                .ToList();

        // Array[1].Map["Values"].Array[many].String
        private static List<string> GetStrings(this Byml node) =>
            node.GetArray()[0]
                .GetMap()["Values"]
                .GetArray()
                .Select(n => n.GetString())
                .ToList();

        // Array[1].Vec
        private static float[] GetVec(this Byml node) =>
            node.GetArray()[0]
                .GetArray()
                .Select(n => n.GetFloat())
                .ToArray();

        // Array[1].Map["Values"].Array[many].Array[1].Vec
        private static List<float[]> GetVecArray(this Byml node) =>
            node.GetArray()[0]
                .GetMap()["Values"]
                .GetArray()
                .Select(l => l.GetArray()[0]
                    .GetArray()
                    .Select(n => n.GetFloat())
                    .ToArray())
                .ToList();

        public static FlagUnion GetFlagUnion(this Byml node, FlagUnionType type)
        {
            return type switch
            {
                FlagUnionType.Bool => node.GetBool(),
                FlagUnionType.BoolArray => node.GetInts(),
                FlagUnionType.F32 => node.GetFloat(),
                FlagUnionType.F32Array => node.GetFloats(),
                FlagUnionType.S32 => node.GetInt(),
                FlagUnionType.S32Array => node.GetInts(),
                FlagUnionType.String => node.GetString(),
                FlagUnionType.StringArray => node.GetStrings(),
                FlagUnionType.Vec2 => new Vec2(node.GetVec()),
                FlagUnionType.Vec2Array => node.GetVecArray(),
                FlagUnionType.Vec3 => new Vec3(node.GetVec()),
                FlagUnionType.Vec3Array => node.GetVecArray(),
                FlagUnionType.Vec4 => new Vec4(node.GetVec()),
                _ => throw new ArgumentException($"Invalid flag type: {type}", nameof(type)),
            };
        }

        internal static bool TryGetValue(this ImmutableBymlMap map,
            ImmutableBymlStringTable table,
            string key,
            out ImmutableByml value)
        {
            foreach (var (mapKeyIdx, mapValue) in map)
            {
                Span<byte> mapKeySpan = table[mapKeyIdx];
                if (mapKeySpan.Length == key.Length + 1 && mapKeySpan.ToManaged() == key)
                {
                    value = mapValue;
                    return true;
                }
            }
            value = default;
            return false;
        }

        internal static ImmutableByml GetValue(this ImmutableBymlMap map, ImmutableBymlStringTable table, string key)
        {
            foreach (var (mapKeyIdx, mapValue) in map)
            {
                Span<byte> mapKeySpan = table[mapKeyIdx];
                if (mapKeySpan.Length == key.Length + 1 && mapKeySpan.ToManaged() == key)
                {
                    return mapValue;
                }
            }
            throw new KeyNotFoundException(key);
        }

        internal static string GetString(this ImmutableByml byml, ImmutableBymlStringTable table) =>
            table[byml.GetStringIndex()]
                .ToManaged();

        internal static ParameterObject GetParameterObject(this ImmutableAamp aamp, string[] lists, string obj)
        {
            uint hash;
            AampParameterList subList = aamp.IO;
            
            int count = subList.ListCount;
            int group = subList.ListsOffset;
            int baseOffset = 0;

            foreach (var name in lists)
            {
                hash = Crc32.ComputeHash(name);
                for (int i = 0; i < count; ++i)
                {
                    subList =
                        aamp.GetList(i, baseOffset + group * 4, out int listOffset);

                    if (subList.Name != hash)
                    {
                        if (i == lists.Length - 1)
                        {
                            throw new KeyNotFoundException(string.Join("->",lists));
                        }
                        continue;
                    }
                    count = subList.ListCount;
                    group = subList.ListsOffset;
                    baseOffset = listOffset;
                    break;
                }
            }
            
            hash = Crc32.ComputeHash(obj);

            for (int j = 0; j < subList.ObjectCount; ++j)
            {
                ref AampParameterObject subObject =
                    ref aamp.GetObject(j, baseOffset + subList.ObjectsOffset * 4, out int objOffset);

                if (subObject.Name != hash) continue;
                ParameterObject pObj = new(subObject.ParameterCount);

                for (int k = 0; k < subObject.ParameterCount; ++k) {
                    ref AampParameter parameter = ref aamp.GetParameter(
                        k,
                        objOffset + subObject.ParametersOffset * 4,
                        out int parameterOffset
                    );

                    int dataOffset = parameterOffset + parameter.DataOffset * 4;
                    RevrsReader reader = new(aamp.ParameterData[dataOffset..], Endianness.Little);

                    pObj[parameter.Name] = parameter.Type switch {
                        AampParameterType.Bool => reader.Read<uint>() != 0,
                        AampParameterType.Float => reader.Read<float>(),
                        AampParameterType.Int => reader.Read<int>(),
                        AampParameterType.Vec2 => reader.Read<Vector2>(),
                        AampParameterType.Vec3 => reader.Read<Vector3>(),
                        AampParameterType.Vec4 => reader.Read<Vector4>(),
                        AampParameterType.Color => reader.Read<Color4>(),
                        AampParameterType.String32 or
                        AampParameterType.String64 or
                        AampParameterType.String256 or
                        AampParameterType.StringRef => new(aamp.ReadString(dataOffset), parameter.Type),
                        AampParameterType.Curve1 => reader.Read<Curve1>(),
                        AampParameterType.Curve2 => reader.Read<Curve2>(),
                        AampParameterType.Curve3 => reader.Read<Curve3>(),
                        AampParameterType.Curve4 => reader.Read<Curve4>(),
                        AampParameterType.IntArray => ReadArray<int>(ref reader),
                        AampParameterType.FloatArray => ReadArray<float>(ref reader),
                        AampParameterType.Quat => reader.Read<Quaternion>(),
                        AampParameterType.UInt32 => reader.Read<uint>(),
                        AampParameterType.UInt32Array => ReadArray<uint>(ref reader),
                        AampParameterType.ByteArray => ReadArray<byte>(ref reader),
                        _ => throw new InvalidOperationException($"""
                            Invalid or unsupported parameter type: '{parameter.Type}'
                            """)
                    };
                }

                return pObj;
            }
            throw new KeyNotFoundException(string.Join("->",[..lists, obj]));
        }
        
        internal static Parameter GetParam(this ImmutableAamp aamp, string[] lists, string obj, string param)
        {
            uint hash;
            AampParameterList subList = aamp.IO;
            
            int count = subList.ListCount;
            int group = subList.ListsOffset;
            int baseOffset = 0;

            foreach (var name in lists)
            {
                hash = Crc32.ComputeHash(name);
                for (int i = 0; i < count; ++i)
                {
                    subList =
                        aamp.GetList(i, baseOffset + group * 4, out int listOffset);

                    if (subList.Name != hash)
                    {
                        if (i == lists.Length - 1)
                        {
                            throw new KeyNotFoundException(string.Join("->",lists));
                        }
                        continue;
                    }
                    count = subList.ListCount;
                    group = subList.ListsOffset;
                    baseOffset = listOffset;
                    break;
                }
            }
            
            hash = Crc32.ComputeHash(obj);
            
            for (int j = 0; j < subList.ObjectCount; ++j)
            {
                ref AampParameterObject subObject =
                    ref aamp.GetObject(j, baseOffset + subList.ObjectsOffset * 4, out int objOffset);
                
                if (subObject.Name != hash) continue;
                count = subObject.ParameterCount;
                group = subObject.ParametersOffset;
                baseOffset = objOffset;

                hash = Crc32.ComputeHash(param);

                for (int k = 0; k < count; ++k)
                {
                    ref AampParameter parameter =
                        ref aamp.GetParameter(k, baseOffset + group * 4, out int paramOffset);

                    if (parameter.Name != hash) continue;
                    int dataOffset = paramOffset + parameter.DataOffset * 4;
                    RevrsReader reader = new(aamp.ParameterData[dataOffset..], Endianness.Little);

                    return parameter.Type switch
                    {
                        AampParameterType.Bool => reader.Read<uint>() != 0,
                        AampParameterType.Float => reader.Read<float>(),
                        AampParameterType.Int => reader.Read<int>(),
                        AampParameterType.Vec2 => reader.Read<Vector2>(),
                        AampParameterType.Vec3 => reader.Read<Vector3>(),
                        AampParameterType.Vec4 => reader.Read<Vector4>(),
                        AampParameterType.Color => reader.Read<Color4>(),
                        AampParameterType.String32 or
                        AampParameterType.String64 or
                        AampParameterType.String256 or
                        AampParameterType.StringRef => new(aamp.ReadString(dataOffset), parameter.Type),
                        AampParameterType.Curve1 => reader.Read<Curve1>(),
                        AampParameterType.Curve2 => reader.Read<Curve2>(),
                        AampParameterType.Curve3 => reader.Read<Curve3>(),
                        AampParameterType.Curve4 => reader.Read<Curve4>(),
                        AampParameterType.IntArray => ReadArray<int>(ref reader),
                        AampParameterType.FloatArray => ReadArray<float>(ref reader),
                        AampParameterType.Quat => reader.Read<Quaternion>(),
                        AampParameterType.UInt32 => reader.Read<uint>(),
                        AampParameterType.UInt32Array => ReadArray<uint>(ref reader),
                        AampParameterType.ByteArray => ReadArray<byte>(ref reader),
                        _ => throw new InvalidOperationException($"""
                            Invalid or unsupported parameter type: '{parameter.Type}'
                            """)
                    };
                }
            }
            throw new KeyNotFoundException(string.Join("->",[..lists, obj, param]));
        }

        private static T[] ReadArray<T>(ref RevrsReader reader) where T : unmanaged
        {
            reader.Move(-4);
            int count = reader.Read<int>();
            return reader.ReadSpan<T>(count).ToArray();
        }
    }
}
