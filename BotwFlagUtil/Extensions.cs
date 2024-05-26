using BotwFlagUtil.GameData.Util;
using BymlLibrary;
using BymlLibrary.Extensions;
using BymlLibrary.Nodes.Immutable.Containers;

namespace BotwFlagUtil
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

        public static List<float> GetFloats(this ImmutableByml node)
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

        public static List<int> GetInts(this ImmutableByml node)
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

        public static List<string> GetStrings(this ImmutableByml node, ImmutableBymlStringTable stringTable)
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

        public static float[] GetVec(this ImmutableByml node)
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

        public static List<float[]> GetVecArray(this ImmutableByml node)
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

        public static FlagUnion GetFlagUnion(this ImmutableByml node, FlagUnionType type, ImmutableBymlStringTable stringTable)
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
        public static List<bool> GetBools(this Byml node) => node.GetArray()[0].GetMap()["Values"].GetArray().Select(n => n.GetBool()).ToList();

        // Array[1].Map["Values"].Array[many].Float
        public static List<float> GetFloats(this Byml node) => node.GetArray()[0].GetMap()["Values"].GetArray().Select(n => n.GetFloat()).ToList();

        // Array[1].Map["Values"].Array[many].Int
        public static List<int> GetInts(this Byml node) => node.GetArray()[0].GetMap()["Values"].GetArray().Select(n => n.GetInt()).ToList();

        // Array[1].Map["Values"].Array[many].String
        public static List<string> GetStrings(this Byml node) => node.GetArray()[0].GetMap()["Values"].GetArray().Select(n => n.GetString()).ToList();

        // Array[1].Vec
        public static float[] GetVec(this Byml node) => node.GetArray()[0].GetArray().Select(n => n.GetFloat()).ToArray();

        // Array[1].Map["Values"].Array[many].Array[1].Vec
        public static List<float[]> GetVecArray(this Byml node) => node.GetArray()[0].GetMap()["Values"].GetArray().Select(l => l.GetArray()[0].GetArray().Select(n => n.GetFloat()).ToArray()).ToList();

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

        internal static bool TryGetValue(this ImmutableBymlMap map, ImmutableBymlStringTable table, string key, out ImmutableByml value)
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

        internal static string GetString(this ImmutableByml byml, ImmutableBymlStringTable table) => table[byml.GetStringIndex()].ToManaged();
    }
}
