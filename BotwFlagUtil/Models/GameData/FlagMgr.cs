using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BotwFlagUtil.Models.GameData.Util;
using BotwFlagUtil.Models.Structs;
using BymlLibrary;
using BymlLibrary.Extensions;
using BymlLibrary.Nodes.Containers;
using BymlLibrary.Nodes.Immutable.Containers;
using CsYaz0;
using Revrs;
using SarcLibrary;

namespace BotwFlagUtil.Models.GameData
{
    public enum FlagStringType
    {
        None,
        String32,
        String64,
        String256
    }
    public class FlagMgr
    {
        private readonly Dictionary<string, Dictionary<NintendoHash, Flag>> flags;

        public FlagMgr()
        {
            flags = new() {
                { "bool_data", new() },
                { "bool_array_data", new() },
                { "f32_data", new() },
                { "f32_array_data", new() },
                { "s32_data", new() },
                { "s32_array_data", new() },
                { "string_data", new() },
                { "string64_data", new() },
                { "string256_data", new() },
                { "string64_array_data", new() },
                { "string256_array_data", new() },
                { "vector2f_data", new() },
                { "vector2f_array_data", new() },
                { "vector3f_data", new() },
                { "vector3f_array_data", new() },
                { "vector4f_data", new() },
            };
        }

        private FlagMgr(Dictionary<string, Dictionary<NintendoHash, Flag>> flags)
        {
            this.flags = flags;
        }

        private void AddFromBymlImmutable(ImmutableByml byml, bool? revival = null)
        {
            ImmutableBymlStringTable keyTable = byml.KeyTable, stringTable = byml.StringTable;
            string type = keyTable[byml.GetMap()[0].KeyIndex].ToManaged();
            foreach (ImmutableByml node in byml.GetMap()[0].Node.GetArray())
            {
                FlagUnionType t = type switch
                {
                    "bool_data" => FlagUnionType.Bool,
                    "bool_array_data" => FlagUnionType.BoolArray,
                    "s32_data" => FlagUnionType.S32,
                    "s32_array_data" => FlagUnionType.S32Array,
                    "f32_data" => FlagUnionType.F32,
                    "f32_array_data" => FlagUnionType.F32Array,
                    "string_data" or "string64_data" or "string256_data" => FlagUnionType.String,
                    "string64_array_data" or "string256_array_data" => FlagUnionType.StringArray,
                    "vector2f_data" => FlagUnionType.Vec2,
                    "vector2f_array_data" => FlagUnionType.Vec2Array,
                    "vector3f_data" => FlagUnionType.Vec3,
                    "vector3f_array_data" => FlagUnionType.Vec3Array,
                    "vector4f_data" => FlagUnionType.Vec4,
                    _ => throw new ArgumentOutOfRangeException(nameof(byml), $"Unexpected flag type: {type}"),
                };
                Flag flag = new(node, t, keyTable, stringTable, revival);
                flags[type][flag.HashValue] = flag;
            }
        }

        public void AddFromByml(Byml byml, bool? revival = null)
        {
            foreach ((string type, Byml node) in byml.GetMap())
            {
                foreach (Byml flagNode in node.GetArray())
                {
                    FlagUnionType t = type switch
                    {
                        "bool_data" => FlagUnionType.Bool,
                        "bool_array_data" => FlagUnionType.BoolArray,
                        "s32_data" => FlagUnionType.BoolArray,
                        "s32_array_data" => FlagUnionType.S32Array,
                        "f32_data" => FlagUnionType.F32,
                        "f32_array_data" => FlagUnionType.F32Array,
                        "string_data" or "string64_data" or "string256_data" => FlagUnionType.String,
                        "string64_array_data" or "string256_array_data" => FlagUnionType.StringArray,
                        "vector2f_data" => FlagUnionType.Vec2,
                        "vector2f_array_data" => FlagUnionType.Vec2Array,
                        "vector3f_data" => FlagUnionType.Vec3,
                        "vector3f_array_data" => FlagUnionType.Vec3Array,
                        "vector4f_data" => FlagUnionType.Vec4,
                        _ => throw new ArgumentOutOfRangeException(nameof(byml), $"Unexpected flag type: {type}"),
                    };
                    Flag flag = new(flagNode, t, revival);
                    flags[type][flag.HashValue] = flag;
                }
            }
        }

        private void AddFromFile(Span<byte> data, Endianness endianness, bool revival)
        {
            RevrsReader reader = new(data, endianness);
            AddFromBymlImmutable(new ImmutableByml(ref reader), revival);
        }

        public static FlagMgr Open(string path)
        {
            Span<byte> data = File.ReadAllBytes(path);
            Endianness endianness = data[6] == 0xFE ? Endianness.Big : Endianness.Little;
            RevrsReader bootupReader = new(data, endianness);
            ImmutableSarc bootup = new(ref bootupReader);
            RevrsReader gameDataReader = new(Yaz0.Decompress(bootup["GameData/gamedata.ssarc"].Data), endianness);
            ImmutableSarc gameData = new(ref gameDataReader);
            FlagMgr mgr = new();
            foreach ((string fileName, Span<byte> fileData) in gameData)
            {
                bool revival = fileName.StartsWith("/r");
                mgr.AddFromFile(fileData, endianness, revival);
            }
            return mgr;
        }

        public void Merge(FlagMgr other)
        {
            foreach ((string type, Dictionary<NintendoHash, Flag> f) in other.flags)
            {
                foreach ((NintendoHash hash, Flag flag) in f)
                {
                    flags[type][hash] = flag;
                }
            }
        }

        /// <summary>
        /// Adds a flag to the manager
        /// </summary>
        /// <param name="flag">Flag to add</param>
        /// <param name="stringType">Type of string or string array. MUST be specified for flags of type String or StringArray</param>
        public void Add(Flag flag, FlagStringType stringType = FlagStringType.None)
        {
            FlagUnionType flagType = flag.InitValue.Type;
            if (flagType == FlagUnionType.None)
            {
                throw new ArgumentException("Cannot add temp flag to flag manager", nameof(flag));
            }
            if (Contains(flag.HashValue))
            {
                throw new ArgumentException(
                    $"{flag.DataName} already exists, cannot automatically determine which to use",
                    nameof(flag)
                );
            }
            if (flagType == FlagUnionType.S32)
            {
                if (flag.MaxValue.Type == FlagUnionType.Bool)
                    flagType = FlagUnionType.Bool;
            }
            else if (flagType == FlagUnionType.S32Array)
            {
                if (flag.MaxValue.Type == FlagUnionType.Bool)
                    flagType = FlagUnionType.BoolArray;
            }
            if (flagType == FlagUnionType.String || flagType == FlagUnionType.StringArray)
            {
                if (stringType == FlagStringType.None)
                    throw new ArgumentException("Strings must be assigned a type", nameof(stringType));
                if (flagType == FlagUnionType.StringArray && stringType == FlagStringType.String32)
                    throw new ArgumentException("String32 not allowed for StringArray", nameof(stringType));
                flags[Helpers.FlagAndStringTypeToKey[flagType][stringType]][flag.HashValue] = flag;
            }
            else
            {
                flags[Helpers.FlagTypeToKey[flagType]][flag.HashValue] = flag;
            }
        }

        private bool Contains(NintendoHash hash) => flags.Any(kvp => kvp.Value.ContainsKey(hash));

        public bool Replace(Flag flag)
        {
            foreach (Dictionary<NintendoHash, Flag> group in flags.Values)
            {
                if (group.ContainsKey(flag.HashValue))
                {
                    group[flag.HashValue] = flag;
                    return true;
                }
            }
            return false;
        }

        public bool Remove(string flagName)
        {
            Flag flag = Flag.GetTempFlag(flagName);
            return flags.Any(kvp => kvp.Value.Remove(flag.HashValue));
        }

        public bool TryGet(
            string flagName,
            out Flag flag,
            out FlagUnionType flagType,
            out FlagStringType stringType
        )
        {
            NintendoHash temp = Flag.GetTempFlag(flagName).HashValue;
            foreach ((string key, Dictionary<NintendoHash, Flag> f) in flags)
            {
                if (f.TryGetValue(temp, out flag))
                {
                    flagType = Helpers.KeyToFlagType[key];
                    stringType = Helpers.KeyToStringType[key];
                    return true;
                }
            }
            flag = default;
            flagType = FlagUnionType.None;
            stringType = FlagStringType.None;
            return false;
        }

        public int GetFlagCount()
        {
            return flags.Select(kvp => kvp.Value.Count).Sum();
        }

        public IEnumerable<Flag> GetAllFlags()
        {
            return flags.SelectMany(kvp => kvp.Value).Select(v => v.Value);
        }

        private Dictionary<string, ReadOnlyMemory<byte>> GetBgDataFiles(Endianness endianness)
        {
            Dictionary<string, IEnumerable<BymlArray>> gameFlags = [];

            foreach ((string type, Dictionary<NintendoHash, Flag> flagGroup) in flags)
            {
                switch (type)
                {
                    case "bool_data":
                    case "s32_data":
                        Dictionary<bool, HashSet<Byml>> revivalOrNot = new() {
                            { true, new() },
                            { false, new() },
                        };
                        foreach (Flag flag in flagGroup.Values) {
                            revivalOrNot[flag.IsRevival ?? false].Add(flag.ToByml());
                        }
                        gameFlags[$"/{type}_"] = revivalOrNot[false]
                            .Chunk(4096)
                            .Select(c => new BymlArray(c));
                        gameFlags[$"/revival_{type}_"] = revivalOrNot[true]
                            .Chunk(4096)
                            .Select(c => new BymlArray(c));
                        break;
                    case "string_data":
                        gameFlags["/string32_data_"] = flags[type]
                            .Select(f => f.Value.ToByml())
                            .Chunk(4096)
                            .Select(c => new BymlArray(c));
                        break;
                    default:
                        gameFlags[$"/{type}_"] = flags[type]
                            .Select(f => f.Value.ToByml())
                            .Chunk(4096)
                            .Select(c => new BymlArray(c));
                        break;
                }
            }

            Dictionary<string, ReadOnlyMemory<byte>> bgdataFiles = [];
            foreach ((string fileName, IEnumerable<BymlArray> flagSection) in gameFlags)
            {
                int i = 0;
                foreach (var flagGroup in flagSection)
                {
                    string fileKey;
                    if (fileName.Contains("revival")) fileKey = fileName[9..^1];
                    else if (fileName == "/string32_data_") fileKey = "string_data";
                    else fileKey = fileName[1..^1];
                    bgdataFiles[$"{fileName}{i}.bgdata"] =
                        new Byml(new Dictionary<string, Byml>() { { fileKey, flagGroup } }).ToBinary(endianness);
                    ++i;
                }
            }
            return bgdataFiles;
        }

        private Dictionary<string, ReadOnlyMemory<byte>> GetSvDataFiles(Endianness endianness)
        {
            BymlArray caption = new(FlagHelpers.Caption.Count);
            BymlArray option = new(FlagHelpers.Option.Count);
            List<BymlArray> general = new(flags.Count / 8192);
            general.Add(new(8192));
            int flagsInFile = 0;
            int fileNum = 1;
            foreach ((_, Flag flag) in flags.SelectMany(kvp => kvp.Value))
            {
                if (!flag.IsSave)
                {
                    continue;
                }
                if (FlagHelpers.Caption.Contains(flag.DataName))
                {
                    caption.Add(flag.ToSvByml());
                }
                else if (FlagHelpers.Option.Contains(flag.DataName))
                {
                    option.Add(flag.ToSvByml());
                }
                else
                {
                    if (flagsInFile == 8192)
                    {
                        flagsInFile = 0;
                        general.Add(new(8192));
                        ++fileNum;
                    }
                    general[fileNum-1].Add(flag.ToSvByml());
                    ++flagsInFile;
                }
            }

            Dictionary<string, ReadOnlyMemory<byte>> svdataFiles = [];
            int numFiles = general.Count + 2;
            fileNum = 0;
            foreach (BymlArray file in general)
            {
                svdataFiles[$"/saveformat_{fileNum++}.bgsvdata"] =
                    FlagHelpers.MakeGameDataSaveFormatFile(file, numFiles, endianness);
            }
            svdataFiles[$"/saveformat_{fileNum++}.bgsvdata"] =
                FlagHelpers.MakeCaptionSaveFormatFile(caption, numFiles, endianness);
            svdataFiles[$"/saveformat_{fileNum}.bgsvdata"] =
                FlagHelpers.MakeOptionSaveFormatFile(option, numFiles, endianness);
            return svdataFiles;
        }

        public void Write(string path)
        {
            Sarc bootup = Sarc.FromBinary(File.ReadAllBytes(path));

            Sarc gameDataSarc = [];
            foreach ((string fileName, ReadOnlyMemory<byte> data) in GetBgDataFiles(bootup.Endianness))
            {
                gameDataSarc.Add(fileName, data.ToArray());
            }
            using (MemoryStream ms = new())
            {
                gameDataSarc.Write(ms);
                bootup["GameData/gamedata.ssarc"] = Yaz0.Compress(ms.ToArray()).ToArray();
            }

            Sarc saveDataSarc = [];
            foreach ((string fileName, ReadOnlyMemory<byte> data) in GetSvDataFiles(bootup.Endianness))
            {
                saveDataSarc.Add(fileName, data.ToArray());

            }
            using (MemoryStream ms = new())
            {
                saveDataSarc.Write(ms);
                bootup["GameData/savedataformat.ssarc"] = Yaz0.Compress(ms.ToArray()).ToArray();
            }

            using (MemoryStream ms = new())
            {
                bootup.Write(ms);
                File.WriteAllBytes(path, ms.ToArray());
            }
        }

        private class FlagMgrConverter : JsonConverter<FlagMgr>
        {
            public override FlagMgr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Dictionary<string, HashSet<Flag>> read = JsonSerializer.Deserialize(ref reader,
                    JsonTypeInfo.CreateJsonTypeInfo<Dictionary<string, HashSet<Flag>>>(options))!;
                Dictionary<string, Dictionary<NintendoHash, Flag>> ret = read.Select(kvp =>
                        (
                            kvp.Key,
                            kvp.Value.Select(v => (v.HashValue, v)).ToDictionary()
                        )
                    ).ToDictionary();
                HashSet<NintendoHash> temp = [];
                foreach (Dictionary<NintendoHash, Flag> group in ret.Values)
                {
                    foreach (NintendoHash hash in group.Keys)
                    {
                        if (!temp.Add(hash))
                        {
                            throw new InvalidDataException($"No two flags can have the same hash: {hash}");
                        }
                    }
                }
                return new(ret);
            }

            public override void Write(Utf8JsonWriter writer, FlagMgr value, JsonSerializerOptions options)
            {
                Dictionary<string, HashSet<Flag>> temp = value.flags.Select(kvp =>
                        (
                            kvp.Key,
                            kvp.Value.Select(v => v.Value).ToHashSet()
                        )
                    ).ToDictionary();
                JsonSerializer.Serialize(writer, temp, options);
            }
        }
    }

    class HashValueComparer : IEqualityComparer<Flag>
    {
        public bool Equals(Flag a, Flag b) => a.HashValue == b.HashValue;
        public int GetHashCode(Flag f) => f.HashValue.ivalue;
    }
}
