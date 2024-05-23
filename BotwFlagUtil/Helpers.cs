using BotwFlagUtil.GameData;
using BotwFlagUtil.GameData.Util;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotwFlagUtil
{
    internal static class Helpers
    {
        public static Dictionary<string, FlagUnionType> keyToFlagType = new()
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
        public static Dictionary<FlagUnionType, string> flagTypeToKey = new()
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
        public static Dictionary<FlagUnionType, FlagUnionType> mainTypeToInitType = new()
        {
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
        public static Dictionary<FlagUnionType, FlagUnionType> mainTypeToMaxOrMinType = new()
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
        public static Dictionary<FlagUnionType, FlagUnionType> singleTypeToArrayType = new()
        {
            { FlagUnionType.Bool, FlagUnionType.BoolArray },
            { FlagUnionType.F32, FlagUnionType.F32Array },
            { FlagUnionType.S32, FlagUnionType.S32Array },
            { FlagUnionType.String, FlagUnionType.StringArray },
            { FlagUnionType.Vec2, FlagUnionType.Vec2Array },
            { FlagUnionType.Vec3, FlagUnionType.Vec3Array },
        };
        public static Dictionary<FlagUnionType, Dictionary<FlagStringType, string>> flagAndStringTypeToKey = new()
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
        public static Dictionary<string, HashSet<string>> actionParams = new() {
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
        public static Dictionary<string, HashSet<string>> queryParams = new() {
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
        public static HashSet<string> boolFlags = [
            "FlagName"
        ];
        public static HashSet<string> floatFlags = [
            "GameDataFloatToName",
            "GameDataFloatDstName",
            "GameDataFloatSrcName",
            "GameDataFloatName",
            "GameDataFloatName_A",
            "GameDataFloatName_B",
            "GameDataFloatPlayerDirectionY"
        ];
        public static HashSet<string> intFlags = [
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
        public static HashSet<string> stringFlags = [
            "GameDataStringOutput"
        ];
        public static HashSet<string> vec3Flags = [
            "GameDataVec3fSrcName",
            "GameDataVec3fDstName",
            "GameDataVec3fToName",
            "GameDataVec3fPlayerPos"
        ];
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
            byte[] utf8bytes = new byte[value.Count * (31 + (writer.Options.Indented ? indentation.Length : 0))];
            int arrayPos = 0;
            if (writer.Options.Indented)
            {
                indentation[1..].CopyTo(utf8bytes, arrayPos);
                arrayPos += indentation.Length - 1;
            }
            for (int i = 0; i < value.Count; ++i)
            {
                float f = value[i];
                byte[] toAdd;
                if (!float.IsFinite(f))
                {
                    toAdd = GetInfiniteValue(f).ToArray();
                }
                else
                {
                    toAdd = Encoding.UTF8.GetBytes(f.ToString("0.0###", CultureInfo.InvariantCulture));
                }
                toAdd.CopyTo(utf8bytes, arrayPos);
                arrayPos += toAdd.Length;
                if (writer.Options.Indented && i < value.Count - 1)
                {
                    indentation.CopyTo(utf8bytes, arrayPos);
                    arrayPos += indentation.Length;
                }
            }
            writer.WriteRawValue(utf8bytes.AsSpan(0, arrayPos), true);
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

        public override List<float> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();
    }
}
