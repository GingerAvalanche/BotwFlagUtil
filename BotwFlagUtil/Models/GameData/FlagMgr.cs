using BotwFlagUtil.GameData.Util;
using BymlLibrary;
using BymlLibrary.Extensions;
using BymlLibrary.Nodes.Containers;
using BymlLibrary.Nodes.Immutable.Containers;
using CsYaz0;
using Revrs;
using SarcLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace BotwFlagUtil.GameData
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
        [JsonInclude]
        private readonly Dictionary<string, HashSet<Flag>> _flags;
        private readonly HashSet<NintendoHash> _hashes;

        public FlagMgr()
        {
            _flags = new() {
                { "bool_data", [] },
                { "bool_array_data", [] },
                { "f32_data", [] },
                { "f32_array_data", [] },
                { "s32_data", [] },
                { "s32_array_data", [] },
                { "string_data", [] },
                { "string64_data", [] },
                { "string256_data", [] },
                { "string64_array_data", [] },
                { "string256_array_data", [] },
                { "vector2f_data", [] },
                { "vector2f_array_data", [] },
                { "vector3f_data", [] },
                { "vector3f_array_data", [] },
                { "vector4f_data", [] },
            };
            _hashes = [];
        }

        [JsonConstructor]
        public FlagMgr(Dictionary<string, HashSet<Flag>> _flags)
        {
            this._flags = _flags;
            _hashes = _flags.SelectMany(kvp => kvp.Value.Select(f => f.HashValue)).ToHashSet();
        }

        public void AddFromBymlImmutable(ImmutableByml byml, bool? revival = null)
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
                _flags[type].Add(new(node, t, keyTable, stringTable, revival));
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
                    _flags[type].Add(new(node, t, revival));
                }
            }
        }

        public void AddFromFile(Span<byte> data, Endianness endianness, bool revival)
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
            mgr._hashes.UnionWith(mgr._flags.SelectMany(kvp => kvp.Value.Select(f => f.HashValue)).ToHashSet());
            return mgr;
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
                throw new ArgumentException($"Cannot add {flag.DataName}, already exists", nameof(flag));
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
                if (stringType == FlagStringType.None) throw new ArgumentException("Strings must be assigned a type", nameof(stringType));
                if (flagType == FlagUnionType.StringArray && stringType == FlagStringType.String32)
                    throw new ArgumentException("String32 not allowed for StringArray", nameof(stringType));
                _flags[Helpers.flagAndStringTypeToKey[flagType][stringType]].Add(flag);
            }
            else
            {
                _flags[Helpers.flagTypeToKey[flagType]].Add(flag);
            }
            _hashes.Add(flag.HashValue);
        }

        public bool Contains(NintendoHash hash) => _hashes.Contains(hash);

        public bool Remove(string flagName)
        {
            Flag flag = Flag.GetTempFlag(flagName);
            return _flags.Any(kvp => kvp.Value.Remove(flag)) && _hashes.Remove(flag.HashValue);
        }

        public bool TryRetrieve(string flagName, out Flag flag, out FlagStringType stringType)
        {
            foreach ((string key, HashSet<Flag> flags) in _flags)
            {
                if (flags.TryGetValue(Flag.GetTempFlag(flagName), out flag))
                {
                    flags.Remove(flag);
                    _hashes.Remove(flag.HashValue);
                    stringType = Helpers.keyToStringType[key];
                    return true;
                }
            }
            flag = default;
            stringType = FlagStringType.None;
            return false;
        }

        public int CountFlags()
        {
            return _flags.Select(kvp => kvp.Value.Count).Sum();
        }

        public int CountFlags(Func<Flag, bool> condition)
        {
            return _flags.Select(kvp => kvp.Value.Where(condition).Count()).Sum();
        }

        public List<string> GetFlagNames(Func<Flag, bool> condition)
        {
            return _flags.SelectMany(kvp => kvp.Value, (t, f) => f).Where(f => condition(f)).Select(f => f.DataName).ToList();
        }

        public List<string> ConvertFlagsByCondition(Func<Flag, bool> condition, Func<Flag, string> conversion)
        {
            return _flags.SelectMany(kvp => kvp.Value, (t, f) => f).Where(f => condition(f)).Select(f => conversion(f)).ToList();
        }

        public IEnumerable<Flag> GetAllFlags()
        {
            return _flags.SelectMany(kvp => kvp.Value);
        }

        private Dictionary<string, ReadOnlyMemory<byte>> GetBgDataFiles(Endianness endianness)
        {
            Dictionary<string, IEnumerable<BymlArray>> gameFlags = [];

            // I'm sorry.
            foreach ((string type, HashSet<Flag> flags) in _flags)
            {
                switch (type)
                {
                    case "bool_data":
                    case "s32_data":
                        gameFlags[$"/{type}_"] = _flags[type].Where(f => !(f.IsRevival ?? false)).Select(f => f.ToByml()).Chunk(4096).Select(c => new BymlArray(c));
                        gameFlags[$"/revival_{type}_"] = _flags[type].Where(f => f.IsRevival ?? false).Select(f => f.ToByml()).Chunk(4096).Select(c => new BymlArray(c));
                        break;
                    case "string_data":
                        gameFlags["/string32_data_"] = _flags[type].Select(f => f.ToByml()).Chunk(4096).Select(c => new BymlArray(c));
                        break;
                    default:
                        gameFlags[$"/{type}_"] = _flags[type].Select(f => f.ToByml()).Chunk(4096).Select(c => new BymlArray(c));
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
                    bgdataFiles[$"{fileName}{i}.bgdata"] = new Byml(new Dictionary<string, Byml>() { { fileKey, flagGroup } }).ToBinary(endianness, 2);
                    ++i;
                }
            }
            return bgdataFiles;
        }

        private Dictionary<string, ReadOnlyMemory<byte>> GetSvDataFiles(Endianness endianness)
        {
            HashSet<Flag> allFlags = _flags.SelectMany(kvp => kvp.Value).Where(f => f.IsSave).ToHashSet();
            IEnumerable<BymlArray> saveFlags = allFlags.Where(f => !(FlagHelpers.Caption.Contains(f.DataName) || FlagHelpers.Option.Contains(f.DataName)))
                .Select(f => f.ToSvByml())
                .Chunk(8192)
                .Select(c => new BymlArray(c));
            BymlArray captionFlags = new(allFlags.Where(f => FlagHelpers.Caption.Contains(f.DataName)).Select(f => f.ToSvByml()));
            BymlArray optionFlags = new(allFlags.Where(f => FlagHelpers.Option.Contains(f.DataName)).Select(f => f.ToSvByml()));

            Dictionary<string, ReadOnlyMemory<byte>> svdataFiles = [];
            int numFiles = saveFlags.Count() + 2;
            int fileNum = 0;
            foreach (BymlArray file in saveFlags)
            {
                svdataFiles[$"/saveformat_{fileNum++}.bgsvdata"] = FlagHelpers.MakeGameDataSaveFormatFile(file, numFiles, endianness);
            }
            svdataFiles[$"/saveformat_{fileNum++}.bgsvdata"] = FlagHelpers.MakeCaptionSaveFormatFile(captionFlags, numFiles, endianness);
            svdataFiles[$"/saveformat_{fileNum}.bgsvdata"] = FlagHelpers.MakeOptionSaveFormatFile(optionFlags, numFiles, endianness);
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
                gameDataSarc.Write(ms);
                bootup["GameData/savedataformat.ssarc"] = Yaz0.Compress(ms.ToArray()).ToArray();
            }

            using (MemoryStream ms = new())
            {
                bootup.Write(ms);
                File.WriteAllBytes(path, ms.ToArray());
            }
        }
    }
}
