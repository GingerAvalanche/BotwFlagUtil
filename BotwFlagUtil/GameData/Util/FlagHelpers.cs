using BymlLibrary;
using BymlLibrary.Nodes.Containers;
using Revrs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BotwFlagUtil.GameData.Util
{
    internal class FlagHelpers
    {
        public static JsonSerializerOptions floatOptions = new()
        {
            Converters = { new FloatConverter() },
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        public static JsonSerializerOptions floatListOptions = new()
        {
            Converters = { new FloatListConverter() },
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        public static readonly HashSet<string> Caption =
        [
            "AlbumPictureIndex",
            "IsGet_Obj_AmiiboItem",
            "CaptionPictSize",
            "SeakSensorPictureIndex",
            "AoC_HardMode_Enabled",
            "FamouseValue",
            "SaveDistrictName",
            "LastSaveTime_Lower",
            "GameClear",
            "IsChangedByDebug",
            "SaveLocationName",
            "IsSaveByAuto",
            "LastSaveTime_Upper",
            "IsLogicalDelete"
        ];
        public static readonly HashSet<string> Option =
        [
            "GyroOnOff",
            "PlayReport_CtrlMode_Ext",
            "PlayReport_CtrlMode_Free",
            "NexUniqueID_Upper",
            "MiniMapDirection",
            "CameraRLReverse",
            "JumpButtonChange",
            "TextRubyOnOff",
            "VoiceLanguage",
            "PlayReport_CtrlMode_Console_Free",
            "PlayReport_PlayTime_Handheld",
            "BalloonTextOnOff",
            "PlayReport_AudioChannel_Other",
            "PlayReport_AudioChannel_5_1ch",
            "NexIsPosTrackUploadAvailableCache",
            "NexsSaveDataUploadIntervalHoursCache",
            "NexUniqueID_Lower",
            "TrackBlockFileNumber",
            "Option_LatestAoCVerPlayed",
            "NexPosTrackUploadIntervalHoursCache",
            "NexLastUploadTrackBlockHardIndex",
            "MainScreenOnOff",
            "PlayReport_AudioChannel_Stereo",
            "NexIsSaveDataUploadAvailableCache",
            "NexLastUploadSaveDataTime",
            "PlayReport_AllPlayTime",
            "NexLastUploadTrackBlockIndex",
            "PlayReport_CtrlMode_Console_Ext",
            "AmiiboItemOnOff",
            "TrackBlockFileNumber_Hard",
            "StickSensitivity",
            "TextWindowChange",
            "IsLastPlayHardMode",
            "PlayReport_CtrlMode_Console_FullKey",
            "NexLastUploadTrackBlockTime",
            "PlayReport_CtrlMode_FullKey",
            "PlayReport_PlayTime_Console",
            "PlayReport_AudioChannel_Mono",
            "CameraUpDownReverse",
            "PlayReport_CtrlMode_Handheld"
        ];
        private static readonly Byml GameDataHeader = new(new Dictionary<string, Byml>()
        {
            { "IsCommon", false },
            { "IsCommonAtSameAccount", false },
            { "IsSaveSecureCode", true },
            { "file_name", "game_data.sav" }
        });
        private static readonly Byml CaptionHeader = new(new Dictionary<string, Byml>()
        {
            { "IsCommon", false },
            { "IsCommonAtSameAccount", false },
            { "IsSaveSecureCode", true },
            { "file_name", "caption.sav" }
        });
        private static readonly Byml OptionHeader = new(new Dictionary<string, Byml>()
        {
            { "IsCommon", false },
            { "IsCommonAtSameAccount", true },
            { "IsSaveSecureCode", true },
            { "file_name", "option.sav" }
        });

        public static ReadOnlyMemory<byte> MakeGameDataSaveFormatFile(BymlArray flags, int numFiles, Endianness endianness)
        {
            return new Byml(
                new Dictionary<string, Byml>()
                {
                    { "file_list", new BymlArray([GameDataHeader, flags]) },
                    { "save_info", MakeSaveInfoFooter(numFiles) }
                }
            ).ToBinary(endianness, 2);
        }

        public static ReadOnlyMemory<byte> MakeCaptionSaveFormatFile(BymlArray flags, int numFiles, Endianness endianness)
        {
            return new Byml(
                new Dictionary<string, Byml>()
                {
                    { "file_list", new BymlArray([CaptionHeader, flags]) },
                    { "save_info", MakeSaveInfoFooter(numFiles) }
                }
            ).ToBinary(endianness, 2);
        }

        public static ReadOnlyMemory<byte> MakeOptionSaveFormatFile(BymlArray flags, int numFiles, Endianness endianness)
        {
            return new Byml(
                new Dictionary<string, Byml>()
                {
                    { "file_list", new BymlArray([OptionHeader, flags]) },
                    { "save_info", MakeSaveInfoFooter(numFiles) }
                }
            ).ToBinary(endianness, 2);
        }

        private static Byml MakeSaveInfoFooter(int numFiles)
        {
            return new(
                new BymlArray(
                    [
                        new Dictionary<string, Byml>()
                        {
                            { "directory_num", numFiles },
                            { "is_build_machine", true },
                            { "revision", 18203 }
                        }
                    ]
                )
            );
        }
    }
}
