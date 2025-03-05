using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BotwFlagUtil.GameData.Util;
using BotwFlagUtil.Models.GameData;
using BotwFlagUtil.Models.GameData.Util;
using BotwFlagUtil.Models.Structs;
using BymlLibrary;
using BymlLibrary.Nodes.Immutable.Containers;
using CsYaz0;
using Revrs;

namespace BotwFlagUtil.Models
{
    internal static class Helpers
    {
        public static string? rootDir;
        private static Endianness? _modEndianness;
        private static Dictionary<string, Vec3>? _modShrineLocs;
        private static Dictionary<string, Vec3>? _allShrineLocs;
        private static HashSet<string>? _vanillaLocSaveFlags;
        private static Dictionary<string, Vec3>? _vanillaShrineLocs; 
        public static readonly Dictionary<string, HashSet<string>> VanillaHasFlags =
            JsonSerializer.Deserialize<Dictionary<string, HashSet<string>>>(
                File.ReadAllText(
                    Path.Combine(AppContext.BaseDirectory, "data", "vanilla_actors.json")
                )
            )!;
        public static readonly HashSet<NintendoHash> VanillaFlagHashes =
            JsonSerializer.Deserialize<HashSet<NintendoHash>>(
                File.ReadAllText(
                    Path.Combine(AppContext.BaseDirectory, "data", "vanilla_hash.json")
                )
            )!;
        public static readonly Dictionary<string, FlagUnionType> KeyToFlagType = new()
        {
            { "bool_data", FlagUnionType.Bool },
            { "bool_array_data", FlagUnionType.BoolArray },
            { "f32_data", FlagUnionType.F32 },
            { "f32_array_data", FlagUnionType.F32Array },
            { "s32_data", FlagUnionType.S32 },
            { "s32_array_data", FlagUnionType.S32Array },
            { "string_data", FlagUnionType.String },
            { "string64_data", FlagUnionType.String },
            { "string256_data", FlagUnionType.String },
            { "string64_array_data", FlagUnionType.StringArray },
            { "string256_array_data", FlagUnionType.StringArray },
            { "vector2f_data", FlagUnionType.Vec2 },
            { "vector2f_array_data", FlagUnionType.Vec2Array },
            { "vector3f_data", FlagUnionType.Vec3 },
            { "vector3f_array_data", FlagUnionType.Vec3Array },
            { "vector4f_data", FlagUnionType.Vec4 },
        };
        public static readonly Dictionary<FlagUnionType, string> FlagTypeToKey = new()
        {
            { FlagUnionType.Bool, "bool_data" },
            { FlagUnionType.BoolArray, "bool_array_data" },
            { FlagUnionType.F32, "f32_data" },
            { FlagUnionType.F32Array, "f32_array_data" },
            { FlagUnionType.S32, "s32_data" },
            { FlagUnionType.S32Array, "s32_array_data" },
            { FlagUnionType.Vec2, "vector2f_data" },
            { FlagUnionType.Vec2Array, "vector2f_array_data" },
            { FlagUnionType.Vec3, "vector3f_data" },
            { FlagUnionType.Vec3Array, "vector3f_array_data" },
            { FlagUnionType.Vec4, "vector4f_data" },
        };
        public static readonly Dictionary<FlagUnionType, FlagUnionType> MainTypeToInitType = new()
        {
            { FlagUnionType.None, FlagUnionType.None },
            { FlagUnionType.Bool, FlagUnionType.S32 },
            { FlagUnionType.BoolArray, FlagUnionType.S32Array },
            { FlagUnionType.F32, FlagUnionType.F32 },
            { FlagUnionType.F32Array, FlagUnionType.F32Array },
            { FlagUnionType.S32, FlagUnionType.S32 },
            { FlagUnionType.S32Array, FlagUnionType.S32Array },
            { FlagUnionType.String, FlagUnionType.String },
            { FlagUnionType.StringArray, FlagUnionType.StringArray },
            { FlagUnionType.Vec2, FlagUnionType.Vec2 },
            { FlagUnionType.Vec2Array, FlagUnionType.Vec2Array },
            { FlagUnionType.Vec3, FlagUnionType.Vec3 },
            { FlagUnionType.Vec3Array, FlagUnionType.Vec3Array },
            { FlagUnionType.Vec4, FlagUnionType.Vec4 },
        };
        public static readonly Dictionary<FlagUnionType, FlagUnionType> MainTypeToMaxOrMinType = new()
        {
            { FlagUnionType.None, FlagUnionType.None },
            { FlagUnionType.Bool, FlagUnionType.Bool },
            { FlagUnionType.BoolArray, FlagUnionType.Bool },
            { FlagUnionType.F32, FlagUnionType.F32 },
            { FlagUnionType.F32Array, FlagUnionType.F32 },
            { FlagUnionType.S32, FlagUnionType.S32 },
            { FlagUnionType.S32Array, FlagUnionType.S32 },
            { FlagUnionType.String, FlagUnionType.String },
            { FlagUnionType.StringArray, FlagUnionType.String },
            { FlagUnionType.Vec2, FlagUnionType.Vec2 },
            { FlagUnionType.Vec2Array, FlagUnionType.Vec2 },
            { FlagUnionType.Vec3, FlagUnionType.Vec3 },
            { FlagUnionType.Vec3Array, FlagUnionType.Vec3 },
            { FlagUnionType.Vec4, FlagUnionType.Vec4 },
        };
        public static Dictionary<FlagUnionType, FlagUnionType> arrayTypeToSingleType = new()
        {
            { FlagUnionType.Bool, FlagUnionType.Bool },
            { FlagUnionType.BoolArray, FlagUnionType.Bool },
            { FlagUnionType.F32, FlagUnionType.F32 },
            { FlagUnionType.F32Array, FlagUnionType.F32 },
            { FlagUnionType.S32, FlagUnionType.S32 },
            { FlagUnionType.S32Array, FlagUnionType.S32 },
            { FlagUnionType.String, FlagUnionType.String },
            { FlagUnionType.StringArray, FlagUnionType.String },
            { FlagUnionType.Vec2, FlagUnionType.Vec2 },
            { FlagUnionType.Vec2Array, FlagUnionType.Vec2 },
            { FlagUnionType.Vec3, FlagUnionType.Vec3 },
            { FlagUnionType.Vec3Array, FlagUnionType.Vec3 },
            { FlagUnionType.Vec4, FlagUnionType.Vec4 },
        };
        public static readonly Dictionary<FlagUnionType, FlagUnionType> SingleTypeToArrayType = new()
        {
            { FlagUnionType.Bool, FlagUnionType.BoolArray },
            { FlagUnionType.F32, FlagUnionType.F32Array },
            { FlagUnionType.S32, FlagUnionType.S32Array },
            { FlagUnionType.String, FlagUnionType.StringArray },
            { FlagUnionType.Vec2, FlagUnionType.Vec2Array },
            { FlagUnionType.Vec3, FlagUnionType.Vec3Array },
        };
        public static readonly Dictionary<FlagUnionType, Dictionary<FlagStringType, string>> FlagAndStringTypeToKey =
            new()
        {
            {
                FlagUnionType.String,
                new()
                {
                    { FlagStringType.String32, "string_data" },
                    { FlagStringType.String64, "string64_data" },
                    { FlagStringType.String256, "string256_data" },
                }
            },
            {
                FlagUnionType.StringArray,
                new()
                {
                    { FlagStringType.String64, "string64_array_data" },
                    { FlagStringType.String256, "string256_array_data" },
                }
            },
        };
        public static readonly Dictionary<string, FlagStringType> KeyToStringType = new() {
            { "bool_data", FlagStringType.None },
            { "bool_array_data", FlagStringType.None },
            { "f32_data", FlagStringType.None },
            { "f32_array_data", FlagStringType.None },
            { "s32_data", FlagStringType.None },
            { "s32_array_data", FlagStringType.None },
            { "string_data", FlagStringType.String32 },
            { "string64_data", FlagStringType.String64 },
            { "string64_array_data", FlagStringType.String64 },
            { "string256_data", FlagStringType.String256 },
            { "string256_array_data", FlagStringType.String256 },
            { "vector2f_data", FlagStringType.None },
            { "vector2f_array_data", FlagStringType.None },
            { "vector3f_data", FlagStringType.None },
            { "vector3f_array_data", FlagStringType.None },
            { "vector4f_data", FlagStringType.None },
        };
        public static readonly Dictionary<string, HashSet<string>> ActionParams = new() {
            {
                "Demo_ActorInfoToGameDataVec3",
                [
                    "GameDataVec3fToName"
                ]
            },
            {
                "Demo_AddGameDataToRupee",
                [
                    "GameDataIntAddValueName"
                ]
            },
            {
                "Demo_CalcVecLengthToGameData",
                [
                    "GameDataFloatToName",
                    "GameDataVec3fSrcName"
                ]
            },
            {
                "Demo_FlagOFF",
                [
                    "FlagName"
                ]
            },
            {
                "Demo_FlagON",
                [
                    "FlagName"
                ]
            },
            {
                "Demo_GameDataAddInt",
                [
                    "GameDataIntSrcName",
                    "GameDataIntDstName",
                    "GameDataIntToName"
                ]
            },
            {
                "Demo_GameDataConvertIntToString",
                [
                    "GameDataIntInput",
                    "GameDataStringOutput"
                ]
            },
            {
                "Demo_GameDataCopyFloat",
                [
                    "GameDataFloatDstName",
                    "GameDataFloatSrcName"
                ]
            },
            {
                "Demo_GameDataCopyInt",
                [
                    "GameDataIntSrcName",
                    "GameDataIntDstName"
                ]
            },
            {
                "Demo_GameDataSubFloat",
                [
                    "GameDataFloatDstName",
                    "GameDataFloatSrcName",
                    "GameDataFloatToName"
                ]
            },
            {
                "Demo_GameDataSubInt",
                [
                    "GameDataIntSrcName",
                    "GameDataIntDstName",
                    "GameDataIntToName"
                ]
            },
            {
                "Demo_GameDataSubVec3",
                [
                    "GameDataVec3fDstName",
                    "GameDataVec3fSrcName",
                    "GameDataVec3fToName"
                ]
            },
            {
                "Demo_IncreaseGameDataInt",
                [
                    "GameDataIntName"
                ]
            },
            {
                "Demo_MiniGameTimerWrite",
                [
                    "GameDataIntNameSeconds",
                    "GameDataIntNameMintues",
                    "GameDataIntNameMiliseconds"
                ]
            },
            {
                "Demo_SetGameDataFloat",
                [
                    "GameDataFloatName"
                ]
            },
            {
                "Demo_SetGameDataInt",
                [
                    "GameDataIntName"
                ]
            },
            {
                "Demo_StorePlayerPosAndRotate",
                [
                    "GameDataVec3fPlayerPos",
                    "GameDataFloatPlayerDirectionY"
                ]
            },
            {
                "Demo_RestorePlayerPosAndRotate",
                [
                    "GameDataVec3fPlayerPos",
                    "GameDataFloatPlayerDirectionY"
                ]
            }
        };
        public static readonly Dictionary<string, HashSet<string>> QueryParams = new() {
            {
                "CheckFlag",
                [
                    "FlagName"
                ]
            },
            {
                "CheckGameDataFloat",
                [
                    "GameDataFloatName"
                ]
            },
            {
                "CheckGameDataInt",
                [
                    "GameDataIntName"
                ]
            },
            {
                "CompareGameDataFloat",
                [
                    "GameDataFloatName_A",
                    "GameDataFloatName_B"
                ]
            },
            {
                "CompareGameDataInt",
                [
                    "GameDataIntName_A",
                    "GameDataIntName_B"
                ]
            },
            {
                "CompareGameDataTime",
                [
                    "GameDataIntMilliA",
                    "GameDataIntMilliB",
                    "GameDataIntMinA",
                    "GameDataIntMinB",
                    "GameDataIntSecA",
                    "GameDataIntSecB"
                ]
            },
            {
                "CountFlag4",
                [
                    "GameDataFlagNo0",
                    "GameDataFlagNo1",
                    "GameDataFlagNo2",
                    "GameDataFlagNo3",
                    "GameDataFlagNo4"
                ]
            },
            {
                "RandomChoiceExceptOnFlag",
                [
                    "CheckFlag0",
                    "CheckFlag1",
                    "CheckFlag2",
                    "CheckFlag3",
                    "CheckFlag4",
                    "CheckFlag5",
                    "CheckFlag6",
                    "CheckFlag7",
                    "CheckFlag8",
                    "CheckFlag9"
                ]
            }
        };
        public static readonly HashSet<string> BoolFlags = [
            "FlagName"
        ];
        public static readonly HashSet<string> FloatFlags = [
            "GameDataFloatToName",
            "GameDataFloatDstName",
            "GameDataFloatSrcName",
            "GameDataFloatName",
            "GameDataFloatName_A",
            "GameDataFloatName_B",
            "GameDataFloatPlayerDirectionY"
        ];
        public static readonly HashSet<string> IntFlags = [
            "GameDataIntName",
            "GameDataIntAddValueName",
            "GameDataIntSrcName",
            "GameDataIntDstName",
            "GameDataIntToName",
            "GameDataIntNameSeconds",
            "GameDataIntNameMintues",
            "GameDataIntNameMiliseconds",
            "GameDataIntName_A",
            "GameDataIntName_B",
            "GameDataIntMilliA",
            "GameDataIntMilliB",
            "GameDataIntMinA",
            "GameDataIntMinB",
            "GameDataIntSecA",
            "GameDataIntSecB"
        ];
        public static readonly HashSet<string> StringFlags = [
            "GameDataStringOutput"
        ];
        public static readonly HashSet<string> Vec3Flags = [
            "GameDataVec3fSrcName",
            "GameDataVec3fDstName",
            "GameDataVec3fToName",
            "GameDataVec3fPlayerPos"
        ];
        public static readonly JsonSerializerOptions JsOpt = new() { WriteIndented = true };

        public static Endianness ModEndianness
        {
            get
            {
                if (!_modEndianness.HasValue)
                {
                    if (rootDir == null)
                    {
                        throw new InvalidOperationException(
                            "Attempted to get endianness before rootdir set"
                        );
                    }
                    _modEndianness = Directory.Exists(Path.Combine(rootDir, "content")) ? 
                        Endianness.Big : Endianness.Little;
                }
                return _modEndianness.Value;
            }
        }
        public static Dictionary<string, Vec3> ModShrineLocs =>
            _modShrineLocs ??= AllShrineLocs.Except(VanillaShrineLocs).ToDictionary();

        private static Dictionary<string, Vec3> AllShrineLocs
        {
            get
            {
                if (_allShrineLocs == null)
                {
                    _allShrineLocs = new(VanillaShrineLocs); // Shallow copy, will never edit, only add new
                    string staticPath = GetFullModPath("Map/MainField/Static.smubin");
                    if (staticPath != string.Empty)
                    {
                        Span<byte> bytes = Yaz0.Decompress(File.ReadAllBytes(staticPath));
                        RevrsReader reader = new(bytes, ModEndianness);
                        ImmutableByml byml = new(ref reader);
                        ImmutableBymlMap map = byml.GetMap();
                        ImmutableBymlStringTable keyTable = byml.KeyTable;
                        foreach (ImmutableByml marker in map.GetValue(keyTable, "LocationMarker").GetArray())
                        {
                            ImmutableBymlMap markerMap = marker.GetMap();
                            ImmutableBymlStringTable stringTable = byml.StringTable;
                            if (markerMap.TryGetValue(keyTable, "Icon", out ImmutableByml icon) &&
                                icon.GetString(stringTable) == "Dungeon" &&
                                markerMap.TryGetValue(keyTable, "MessageID", out ImmutableByml messageId) &&
                                !_allShrineLocs.TryGetValue(messageId.GetString(stringTable), out Vec3 value))
                            {
                                ImmutableBymlMap vec = markerMap.GetValue(keyTable, "Translate").GetMap();
                                value.X = vec.GetValue(keyTable, "X").GetFloat();
                                value.Y = vec.GetValue(keyTable, "Y").GetFloat();
                                value.Z = vec.GetValue(keyTable, "Z").GetFloat();
                                _allShrineLocs.Add(messageId.GetString(stringTable), value);
                            }
                        }
                    }
                }
                return _allShrineLocs;
            }
        }
        public static HashSet<string> VanillaLocSaveFlags
        {
            get
            {
                if (_vanillaLocSaveFlags == null)
                {
                    RevrsReader reader = new(
                        Yaz0.Decompress(
                            File.ReadAllBytes(GetFullStockPath("Map/MainField/Static.smubin"))
                        ),
                        ModEndianness
                    );
                    ImmutableByml stockStatic = new(ref reader);

                    _vanillaLocSaveFlags = [];
                    ImmutableBymlStringTable keyTable = stockStatic.KeyTable;
                    foreach (ImmutableByml marker in stockStatic
                        .GetMap()
                        .GetValue(keyTable, "LocationMarker")
                        .GetArray()
                    )
                    {
                        if (marker.GetMap().TryGetValue(keyTable, "SaveFlag", out ImmutableByml saveFlag))
                        {
                            _vanillaLocSaveFlags.Add(saveFlag.GetString(stockStatic.StringTable));
                        }
                    }
                }
                return _vanillaLocSaveFlags;
            }
        }

        private static Dictionary<string, Vec3> VanillaShrineLocs
        {
            get
            {
                if (_vanillaShrineLocs == null)
                {
                    string jsonPath =
                        Path.Combine(AppContext.BaseDirectory, "data", "vanilla_shrines.json");
                    _vanillaShrineLocs =
                        JsonSerializer.Deserialize<Dictionary<string, Vec3>>(File.ReadAllText(jsonPath))!;
                }
                return _vanillaShrineLocs;
            }
        }

        public static string GetFullModPath(string relativePath)
        {
            if (rootDir == null)
            {
                throw new InvalidOperationException("Attempted to read path without root directory");
            }
            string[] paths = ModEndianness == Endianness.Big ? 
            [
                Path.Combine(rootDir, "aoc", "0010", relativePath),
                Path.Combine(rootDir, "content", relativePath)
            ] : [
                Path.Combine(rootDir, "01007EF00011F001", "romfs", relativePath),
                Path.Combine(rootDir, "01007EF00011E800", "romfs", relativePath)
            ];

            foreach (string path in paths)
            {
                if (File.Exists(path) || Directory.Exists(path))
                {
                    return path;
                }
            }
            return string.Empty;
        }

        public static string GetFullStockPath(string relativePath)
        {
            Settings settings = Settings.Load();
            string root;
            if (ModEndianness == Endianness.Big)
            {
                if (File.Exists(Path.Combine(settings.DlcDir, relativePath)) ||
                    Directory.Exists(Path.Combine(settings.DlcDir, relativePath)))
                {
                    root = settings.DlcDir;
                }
                else if (File.Exists(Path.Combine(settings.UpdateDir, relativePath)) ||
                    Directory.Exists(Path.Combine(settings.UpdateDir, relativePath)))
                {
                    root = settings.UpdateDir;
                }
                else
                {
                    root = settings.GameDir;
                }
            }
            else
            {
                if (File.Exists(Path.Combine(settings.DlcDirNx, relativePath)) ||
                    Directory.Exists(Path.Combine(settings.DlcDirNx, relativePath)))
                {
                    root = settings.DlcDirNx;
                }
                else
                {
                    root = settings.GameDirNx;
                }
            }
            return Path.Combine(root, relativePath);
        }

        public static string GetNearestShrine(Vec3 loc)
        {
            double smallestDistance = 10000000.0;
            string nearestShrine = string.Empty;
            foreach ((string shrineName, Vec3 shrineLoc) in AllShrineLocs)
            {
                double shrineDistance = GetVectorDistance(loc, shrineLoc);
                if (shrineDistance < smallestDistance)
                {
                    smallestDistance = shrineDistance;
                    nearestShrine = shrineName;
                }
            }
            return nearestShrine;
        }

        public static bool GetStockMainFieldMapReader(string fileName, out RevrsReader reader)
        {
            string section = fileName.Split('_')[0];
            string path = GetFullStockPath($"Map/MainField/{section}/{fileName}");
            if (File.Exists(path))
            {
                reader = new(Yaz0.Decompress(File.ReadAllBytes(path)), ModEndianness);
                return true;
            }
            reader = default;
            return false;
        }

        public static bool GetStockPackReader(string fileName, out RevrsReader reader)
        {
            string path = GetFullStockPath($"Pack/{fileName}");
            if (File.Exists(path))
            {
                reader = new(File.ReadAllBytes(path), ModEndianness);
                return true;
            }
            reader = default;
            return false;
        }

        private static double GetVectorDistance(Vec3 a, Vec3 b)
            => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
    }

    public class FloatConverter : JsonConverter<float>
    {
        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            if (!float.IsFinite(value))
                JsonSerializer.Serialize(writer, value, options);
            else
                writer.WriteRawValue(value.ToString("0.0###", CultureInfo.InvariantCulture));
        }

        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            // TODO: Handle "NaN", "Infinity", "-Infinity"
            reader.GetSingle();
    }

    public class FloatListConverter : JsonConverter<List<float>>
    {
        public override void Write(Utf8JsonWriter writer, List<float> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            byte[] indentation = new byte[writer.CurrentDepth * 2 + 2].Fill((byte)' ');
            indentation[0] = (byte)',';
            indentation[1] = (byte)'\n';
            byte[] utf8Bytes = new byte[value.Count * (31 + (writer.Options.Indented ? indentation.Length : 0))];
            int arrayPos = 0;
            if (writer.Options.Indented)
            {
                indentation[1..].CopyTo(utf8Bytes, arrayPos);
                arrayPos += indentation.Length - 1;
            }
            for (int i = 0; i < value.Count; ++i)
            {
                float f = value[i];
                var toAdd = !float.IsFinite(f) ?
                    GetInfiniteValue(f).ToArray() :
                    Encoding.UTF8.GetBytes(f.ToString("0.0###", CultureInfo.InvariantCulture));
                toAdd.CopyTo(utf8Bytes, arrayPos);
                arrayPos += toAdd.Length;
                if (!writer.Options.Indented || i >= value.Count - 1) continue;
                indentation.CopyTo(utf8Bytes, arrayPos);
                arrayPos += indentation.Length;
            }
            writer.WriteRawValue(utf8Bytes.AsSpan(0, arrayPos), true);
            writer.WriteEndArray();
        }

        private static ReadOnlySpan<byte> GetInfiniteValue(float f)
        {
            if (float.IsNaN(f))
                return "NaN"u8;
            if (float.IsInfinity(f))
                return "Infinity"u8;
            if (float.IsNegativeInfinity(f))
                return "-Infinity"u8;
            throw new ArgumentException($"GetInfiniteValue called with finite value {f}", nameof(f));
        }

        public override List<float> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        ) => throw new NotImplementedException();
    }
}
