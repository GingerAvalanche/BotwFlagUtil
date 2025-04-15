using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Aamp.Security.Cryptography;
using BfevLibrary;
using BfevLibrary.Core;
using BotwFlagUtil.GameData.Util;
using BotwFlagUtil.Models.Enums;
using BotwFlagUtil.Models.GameData;
using BotwFlagUtil.Models.GameData.Util;
using BotwFlagUtil.Models.Structs;
using BymlLibrary;
using BymlLibrary.Nodes.Immutable.Containers;
using CsYaz0;
using Nintendo.Aamp;
using Revrs;
using SarcLibrary;

namespace BotwFlagUtil.Models
{
    public class Generator
    {
        public FlagMgr Mgr = new();
        public Dictionary<NintendoHash, GeneratorConfidence> FlagConfidence = [];
        private readonly HashSet<NintendoHash> orphanedFlagHashes = [];
        private readonly HashSet<Flag> flagsToAdd = new(new HashValueComparer());
        private readonly Dictionary<string, RevivalTag> actorRevivalTypes = [];
        private static readonly string[] LinkTagFlagNames =
        [
            "{0}",
            "Clear_{0}",
            "Open_{0}",
            "{0}_{1}_{2}"
        ];
        private static readonly HashSet<uint> RevivalTags =
        [
            0xb0c9e79a, // RevivalBloodyMoon
            0xdeb2c8bd, // RevivalNone
            0x21f1c37a, // RevivalNoneForDrop
            0x28a3a540, // RevivalNoneForUsed
            0x9e8d2a01, // RevivalRandom
            0x25cd2b4d, // RevivalRandomForDrop
            0x09e9e131, // RevivalUnderGodTime
        ];
        private static readonly HashSet<string> NoZukanFlagActors =
        [
            "DgnObj_DLC_Weapon_Sword_502",
            "Priest_Boss_Giant",
            "Priest_Boss_Normal",
            "Priest_Boss_ShadowClone_Real",
        ];
        private static readonly HashSet<string> NoShopFlagActors =
        [
            "Npc_DressFairy_00",
            "Npc_DressFairy_01",
            "Npc_DressFairy_02",
            "Npc_DressFairy_03",
        ];
        private static readonly Dictionary<NintendoHash, FlagCategory> ZukanCategoryMap = new()
        {
            { 0x24CD75FE, FlagCategory.Animal },
            { 0x2755F107, FlagCategory.Weapon },
            { 0x36565B66, FlagCategory.Boss },
            { 0x682E5129, FlagCategory.Sozai },
            { 0x994AEF4B, FlagCategory.Enemy },
            { 0xBB8D80C2, FlagCategory.Other },
        };
        private static readonly HashSet<string> ArmorProfiles =
        [
            "ArmorHead",
            "ArmorUpper",
            "ArmorLower",
            //"ArmorExtra0",
            //"ArmorExtra1",
            //"ArmorExtra2",
        ];

        public void GenerateActorFlags()
        {
            if (Helpers.RootDir == null)
            {
                throw new InvalidOperationException("Evaluating maps before setting root directory!");
            }
            if (!Helpers.TryGetFullModGamePath("Actor/ActorInfo.product.sbyml", out var actorInfoPath))
            {
                return;
            }

            HashSet<string> npcsToCheck = [];
            RevrsReader reader =
                new(Yaz0.Decompress(File.ReadAllBytes(actorInfoPath)), Helpers.ModEndianness);
            ImmutableByml actorInfo = new(ref reader);
            ImmutableBymlStringTable keyTable = actorInfo.KeyTable;
            ImmutableBymlStringTable stringTable = actorInfo.StringTable;
            actorInfo.GetMap().TryGetValue(keyTable, "Actors", out ImmutableByml actorList);
            foreach (ImmutableByml actor in actorList.GetArray())
            {   
                ImmutableBymlMap map = actor.GetMap();
                map.TryGetValue(keyTable, "name", out ImmutableByml nameNode);
                string actorName = nameNode.GetString(stringTable);

                if (map.TryGetValue(keyTable, "profile", out ImmutableByml profileNode))
                {
                    string profile = profileNode.GetString(stringTable);
                    if (ArmorProfiles.Contains(profile))
                    {
                        map.TryGetValue(keyTable, "armorStarNum", out ImmutableByml starNum);
                        if (starNum.GetInt() > 1)
                        {
                            // Skip flags for non-base armors, only the base armor's flags are used.
                            continue;
                        }
                    }
                    else if (profile == "NPC")
                    {
                        npcsToCheck.Add(actorName);
                    }
                }

                if (map.TryGetValue(keyTable, "tags", out ImmutableByml tags))
                {
                    ImmutableBymlMap tagsMap = tags.GetMap();
                    foreach (ImmutableBymlMapEntry entry in tagsMap)
                    {
                        string flagActorName = actorName;
                        if (map.TryGetValue(keyTable, "systemSameGroupActorName", out var other))
                        {
                            string temp = other.GetString(stringTable);
                            if (temp != string.Empty)
                            {
                                flagActorName = temp;
                            }
                        }
                        NintendoHash entryValue = entry.Node;
                        if (ZukanCategoryMap.TryGetValue(entryValue, out FlagCategory category) &&
                            !NoZukanFlagActors.Contains(actorName))
                        {
                            StageFlag(new(
                                $"IsNewPictureBook_{flagActorName}",
                                FlagUnionType.Bool,
                                isSave: true
                            ) {
                                MaxValue = true
                            }, GeneratorConfidence.Definite);

                            StageFlag(new(
                                $"IsRegisteredPictureBook_{flagActorName}",
                                FlagUnionType.Bool,
                                isSave: true
                            ) {
                                Category = (int)category,
                                MaxValue = true
                            }, GeneratorConfidence.Definite);

                            StageFlag(new(
                                $"PictureBookSize_{flagActorName}",
                                FlagUnionType.S32,
                                isSave: true
                            ) {
                                InitValue = -1,
                                MaxValue = 65536,
                                MinValue = -1
                            }, GeneratorConfidence.Definite);
                        }

                        if (entryValue.uvalue == 0xE0194F30) // CanGetPouch
                        {
                            StageFlag(new(
                                $"IsGet_{flagActorName}",
                                FlagUnionType.Bool,
                                isOneTrigger: true,
                                isSave: true
                            ) {
                                MaxValue = true
                            }, GeneratorConfidence.Definite);
                        }

                        if (entryValue.uvalue == 0x289F28B5) // CanEquip
                        {
                            StageFlag(new(
                                $"EquipTime_{flagActorName}",
                                FlagUnionType.S32,
                                isSave: true
                            ) {
                                MaxValue = 2147483647
                            }, GeneratorConfidence.Definite);

                            StageFlag(new(
                                $"PorchTime_{flagActorName}",
                                FlagUnionType.S32,
                                isSave: true
                            ) {
                                MaxValue = 2147483647
                            }, GeneratorConfidence.Definite);
                        }

                        if (RevivalTags.Contains(entryValue.uvalue))
                        {
                            if (entryValue.uvalue == 0xB0C9E79A) // RevivalBloodyMoon
                            {
                                actorRevivalTypes[flagActorName] = RevivalTag.RevivalBloodyMoon;
                            }
                            else if (entryValue.uvalue == 0x09E9E131) // RevivalUnderGodTime
                            {
                                actorRevivalTypes[flagActorName] = RevivalTag.RevivalUnderGodTime;
                            }
                            else if (entryValue.uvalue == 0x9e8d2a01 || entryValue.uvalue == 0x25cd2b4d) // RevivalRandom, RevivalRandomForDrop
                            {
                                actorRevivalTypes[flagActorName] = RevivalTag.RevivalRandom;
                            }
                            else // all RevivalNone.*
                            {
                                actorRevivalTypes[flagActorName] = RevivalTag.RevivalNone;
                            }
                        }
                    }
                }
            }

            foreach (string actorName in npcsToCheck)
            {
                if (NoShopFlagActors.Contains(actorName))
                {
                    continue; // Ignore DressFairies, their inventory is fake
                }
                if (!Helpers.TryGetFullModGamePath($"Actor/Pack/{actorName}.sbactorpack", out var path))
                {
                    continue; // Ignore NPCs that aren't modified by this mod
                }

                RevrsReader npcReader =
                    new(Yaz0.Decompress(File.ReadAllBytes(path)), Helpers.ModEndianness);
                ImmutableSarc pack = new(ref npcReader);
                AampFile bxml = new(pack[$"Actor/ActorLink/{actorName}.bxml"].Data.ToArray());
                string shopLink = bxml.RootNode
                    .Objects("LinkTarget")!
                    .Params("ShopDataUser")!
                    .Value.ToString()!;
                if (shopLink == "Dummy")
                {
                    continue;
                }

                StageFlag(new(
                    $"{actorName}_SoldOut",
                    FlagUnionType.Bool,
                    isSave: true
                ) {
                    MaxValue = true
                }, GeneratorConfidence.Definite);

                AampFile bshop = new(pack[$"Actor/ShopData/{shopLink}.bshop"].Data.ToArray());
                foreach (ParamEntry entry in bshop.RootNode.Objects("Header")!.ParamEntries)
                {
                    if (entry.HashString == "TableNum")
                    {
                        continue;
                    }
                    foreach (ParamEntry tableEntry in
                        bshop.RootNode.Objects(entry.Value.ToString()!)!.ParamEntries)
                    {
                        if (tableEntry.HashString.StartsWith("ItemName", StringComparison.Ordinal))
                        {
                            StageFlag(new(
                                $"{actorName}_{tableEntry.Value}",
                                FlagUnionType.S32,
                                isSave: true
                            ) {
                                MaxValue = 65535
                            }, GeneratorConfidence.Definite);
                        }
                    }
                }
            }
        }

        public void GenerateEventFlags()
        {
            // TODO: Handle event packs in titlebg (and bootup?)
            if (!Helpers.TryGetFullModGamePath("Event", out var path))
            {
                return;
            }
            foreach (string fileName in Directory.GetFiles(
                path, "*.sbeventpack"
            ))
            {
                RevrsReader reader = new(Yaz0.Decompress(File.ReadAllBytes(fileName)));
                ImmutableSarc eventPack = new(ref reader);

                foreach (ImmutableSarcEntry file in eventPack)
                {
                    if (Encoding.UTF8.GetString(file.Data[..8]) == "BFEVFL\0\0")
                    {
                        BfevFile evfl = BfevFile.FromBinary(file.Data.ToArray());
                        if (evfl.Flowchart != null)
                        {
                            foreach (Event e in evfl.Flowchart.Events)
                            {
                                string? flagName;
                                if (e is ActionEvent action)
                                {
                                    if (action is { ActorAction: not null, Parameters: not null } &&
                                        Helpers.ActionParams.TryGetValue(
                                            action.ActorAction, out HashSet<string>? actionFlags
                                        )
                                    )
                                    {
                                        foreach (string actionFlag in actionFlags)
                                        {
                                            if (Helpers.BoolFlags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    Flag flag = new(flagName, FlagUnionType.Bool) {
                                                        MaxValue = true,
                                                    };
                                                    GeneratorConfidence confidence =
                                                        GeneratorConfidence.Mediocre;
                                                    if (flagName.StartsWith("IsGet_",
                                                        StringComparison.Ordinal))
                                                    {
                                                        flag.IsOneTrigger = true;
                                                        flag.IsSave = true;
                                                        confidence = GeneratorConfidence.Definite;
                                                    }
                                                    StageFlag(flag, confidence);
                                                }
                                            }
                                            else if (Helpers.FloatFlags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.F32),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.IntFlags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.S32)
                                                        {
                                                            MaxValue = 2147483647,
                                                        },
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.StringFlags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.String),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.Vec3Flags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.Vec3),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (e is SwitchEvent @switch)
                                {
                                    if (@switch is { ActorQuery: not null, Parameters: not null } &&
                                        Helpers.QueryParams.TryGetValue(
                                            @switch.ActorQuery, out HashSet<string>? queryFlags
                                        )
                                    )
                                    {
                                        foreach (string queryFlag in queryFlags)
                                        {
                                            if (Helpers.BoolFlags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    Flag flag = new(flagName, FlagUnionType.Bool) {
                                                        MaxValue = true,
                                                    };
                                                    GeneratorConfidence confidence =
                                                        GeneratorConfidence.Mediocre;
                                                    if (flagName.StartsWith("IsGet_",
                                                        StringComparison.Ordinal))
                                                    {
                                                        flag.IsOneTrigger = true;
                                                        flag.IsSave = true;
                                                        confidence = GeneratorConfidence.Definite;
                                                    }
                                                    StageFlag(flag, confidence);
                                                }
                                            }
                                            else if (Helpers.FloatFlags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.F32),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.IntFlags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.S32)
                                                        {
                                                            MaxValue = 2147483647,
                                                        },
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.StringFlags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.String),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.Vec3Flags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.Vec3),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tries to generate a flag for a map actor.<br /><br />
        /// The flag should be added if true is returned, else the flag should be marked for deletion.
        /// </summary>
        /// <param name="byml">The byml of the actor instance to generate a flag for</param>
        /// <param name="keyTable">Key table of the root map byml</param>
        /// <param name="stringTable">String table of the root map byml</param>
        /// <param name="mapType">The GameDataMgr MapType of the map</param>
        /// <param name="mapName">The name of the mubin file, without extension, _Static,
        /// or _Dynamic</param>
        /// <param name="value">The Flag that is generated</param>
        /// <param name="confidence">The GeneratorConfidence of value</param>
        /// <returns>False if the flag should not be made, or true if it should</returns>
        /// <exception cref="InvalidDataException">Thrown under 2 conditions:<br />
        /// - byml is a LinkTag with MakeSaveFlag 1 and mapType is MapType.MainField<br />
        /// - byml is a LinkTag with MakeSaveFlag 2 and mapType is MapType.CDungeon</exception>
        /// <exception cref="NotImplementedException">Thrown when byml is a LinkTag with MakeSaveFlag
        /// that is not 0-3</exception>
        private bool GenerateFlagForMapActor(
            ImmutableByml byml,
            ImmutableBymlStringTable keyTable,
            ImmutableBymlStringTable stringTable,
            MapType mapType,
            string mapName,
            out Flag value,
            out GeneratorConfidence confidence)
        {
            if (mapType == MapType.AocField)
            {
                value = Flag.GetTempFlag("AocField_Flag");
                confidence = 0;
                return false;
            }
            ImmutableBymlMap map = byml.GetMap();
            string actorName = string.Empty;
            uint hashId = 0u;
            if (map.TryGetValue(keyTable, "UnitConfigName", out ImmutableByml configName))
            {
                actorName = configName.GetString(stringTable);
                ImmutableByml hashNode = map.GetValue(keyTable, "HashId");
                hashId = hashNode.Type == BymlNodeType.Int ?
                    (uint)hashNode.GetInt() : hashNode.GetUInt32();
                if (actorName.Contains("LinkTag"))
                {
                    if (map.TryGetValue(keyTable, "!Parameters", out ImmutableByml bParams))
                    {
                        ImmutableBymlMap paramsMap = bParams.GetMap();
                        string flagName;
                        int makeSaveFlag = 0;
                        if (paramsMap.TryGetValue(
                                keyTable, "MakeSaveFlag", out ImmutableByml linkMakeSaveFlag
                            ) &&
                            (makeSaveFlag = linkMakeSaveFlag.GetInt()) != 0)
                        {
                            if (makeSaveFlag == 1 && mapType == MapType.MainField)
                            {
                                throw new InvalidDataException(
                                    $"MainField LinkTags cannot have MakeSaveFlag 1. HashId: {hashId}"
                                );
                            }
                            if (makeSaveFlag == 2 && mapType == MapType.CDungeon)
                            {
                                throw new InvalidDataException(
                                    $"CDungeon LinkTags cannot have MakeSaveFlag 2. HashId: {hashId}"
                                );
                            }
                            string[] parts = makeSaveFlag switch
                            {
                                1 => [mapName],
                                2 => [GetNearestDungeonName(map.GetValue(keyTable, "Translate"))],
                                3 => [mapType.ToString(), actorName, hashId.ToString()],
                                _ => throw new NotImplementedException(),
                            };
                            flagName = string.Format(LinkTagFlagNames[makeSaveFlag], parts.ToArray<object>());
                        }
                        else if (paramsMap.TryGetValue(
                                     keyTable, "SaveFlag", out ImmutableByml linkSaveFlag
                                 ))
                        {
                            flagName = linkSaveFlag.GetString(stringTable);
                        }
                        else
                        {
                            goto shouldNotMakeFlag;
                        }

                        bool s32 = paramsMap.TryGetValue(keyTable, "IncrementSave", out ImmutableByml linkIncrement) &&
                                   linkIncrement.GetBool();

                        value = new(
                            flagName,
                            s32 ? FlagUnionType.S32 : FlagUnionType.Bool,
                            isOneTrigger: makeSaveFlag != 0 && makeSaveFlag != 3,
                            isSave: true,
                            resetType: 0,
                            isRevival: makeSaveFlag == 3
                        ) {
                            Category = makeSaveFlag == 1 ? (int)FlagCategory.Clear : null,
                            InitValue = 0,
                            MaxValue = s32 ? 1 : true,
                            MinValue = s32 ? 0 : false
                        };
                        confidence = GeneratorConfidence.Definite;
                        return true;
                    }
                }
                // If the mod has defined a new actor to place on a map, it presumably needs flags
                // so only count out vanilla actors that don't need flags
                //else if (!Helpers.vanillaHasFlags["no_flags"].Contains(actorName)) // Going to try using Revival tags instead
                else if (actorRevivalTypes.TryGetValue(actorName, out RevivalTag tag) && tag != RevivalTag.RevivalNone)
                {
                    string flagName = $"{mapType}_{actorName}_{hashId}";
                    int resetType = tag == RevivalTag.RevivalBloodyMoon && mapType == MapType.CDungeon ? 2 : (int)tag;
                    int initValue = tag == RevivalTag.RevivalRandom ? GetRandomInitValue(mapName) : 0;
                    bool revival = true;
                    confidence = GeneratorConfidence.Definite;
                    if (actorName.Contains("TBox"))
                    {
                        if (!map.TryGetValue(keyTable, "!Parameters", out ImmutableByml param))
                        {
                            goto shouldNotMakeFlag;
                        }
                        if (param.GetMap().TryGetValue(
                                keyTable, "EnableRevival", out ImmutableByml enableRevival
                            ) &&
                            enableRevival.GetBool()
                        )
                        {
                            resetType = 1;
                        }
                        else
                        {
                            resetType = 0;
                            revival = false;
                        }
                    }
                    else if (map.TryGetValue(keyTable, "LinksToObj", out ImmutableByml objLinks))
                    {
                        foreach (ImmutableByml link in objLinks.GetArray())
                        {
                            if (
                                link.GetMap()
                                    .TryGetValue(keyTable, "DefinitionName", out ImmutableByml defName) &&
                                defName.GetString(stringTable) == "ForSale"
                            )
                            {
                                resetType = 3;
                                revival = false;
                                break;
                            }
                        }
                    }

                    value = new(
                        flagName,
                        FlagUnionType.Bool,
                        isSave: true,
                        resetType: resetType,
                        isRevival: revival
                    ) {
                        InitValue = initValue,
                        MaxValue = true,
                        MinValue = false
                    };
                    return true;
                }
            }
            shouldNotMakeFlag:
            value = Flag.GetTempFlag($"{mapType}_{actorName}_{hashId}");
            confidence = 0;
            return false;
        }

        private void GenerateFlagsForMap(
            ImmutableByml mod,
            MapType mapType,
            string mapName = ""
        )
        {
            ImmutableBymlStringTable modKeyTable = mod.KeyTable;
            ImmutableBymlStringTable modStringTable = mod.StringTable;
            ImmutableBymlArray modObjs = mod.GetMap().GetValue(modKeyTable, "Objs").GetArray();
            NintendoHash[] modHashes = new NintendoHash[modObjs.Count];
            ImmutableBymlMap objMap;

            for (int i = 0; i < modObjs.Count; ++i)
            {
                objMap = modObjs[i].GetMap();
                modHashes[i] = objMap.GetValue(modKeyTable, "HashId");
                if (
                    GenerateFlagForMapActor(
                        modObjs[i],
                        modKeyTable,
                        modStringTable,
                        mapType,
                        mapName,
                        out Flag value,
                        out GeneratorConfidence confidence
                    )
                )
                {
                    StageFlag(value, confidence);
                }
                else
                {
                    orphanedFlagHashes.Add(value.HashValue);
                }
            }
        }

        private void GenerateFlagsForMapWithDiff(
            ImmutableByml mod,
            ImmutableByml stock,
            MapType mapType,
            string mapName = ""
        )
        {
            ImmutableBymlStringTable modKeyTable = mod.KeyTable;
            ImmutableBymlStringTable modStringTable = mod.StringTable;
            ImmutableBymlStringTable stockKeyTable = stock.KeyTable;
            ImmutableBymlStringTable stockStringTable = stock.StringTable;
            ImmutableBymlArray modObjs = mod.GetMap().GetValue(modKeyTable, "Objs").GetArray();
            ImmutableBymlArray stockObjs = stock.GetMap().GetValue(stockKeyTable, "Objs").GetArray();
            NintendoHash[] modHashes = new NintendoHash[modObjs.Count];
            NintendoHash[] stockHashes = new NintendoHash[stockObjs.Count];
            ImmutableBymlMap objMap;
            for (int i = 0; i < stockObjs.Count; ++i)
            {
                stockHashes[i] = stockObjs[i].GetMap().GetValue(stockKeyTable, "HashId");
            }

            for (int i = 0; i < modObjs.Count; ++i)
            {
                objMap = modObjs[i].GetMap();
                modHashes[i] = objMap.GetValue(modKeyTable, "HashId");
                if (stockHashes.Contains(modHashes[i]))
                {
                    continue;
                }
                if (
                    GenerateFlagForMapActor(
                        modObjs[i],
                        modKeyTable,
                        modStringTable,
                        mapType,
                        mapName,
                        out Flag value,
                        out GeneratorConfidence confidence
                    )
                )
                {
                    StageFlag(value, confidence);
                }
                else
                {
                    orphanedFlagHashes.Add(value.HashValue);
                }
            }

            // Sigh, I hate iterating over the same collection multiple times
            // It's bad enough that ImmutableByml.TryGetValue() has to do it
            for (int i = 0; i < stockObjs.Count; ++i)
            {
                if (!modHashes.Contains(stockHashes[i]))
                {
                    objMap = stockObjs[i].GetMap();
                    uint oldHash = Crc32.Compute(
                        $"{mapType}_{
                            objMap.GetValue(stockKeyTable, "UnitConfigName").GetString(stockStringTable)
                        }_{stockHashes[i]}"
                    );
                    orphanedFlagHashes.Add(oldHash);
                }
            }
        }

        public void GenerateLevelSensorFlags()
        {
            if (!Helpers.TryGetFullModGamePath("Pack/Bootup.pack", out var bootupPath))
            {
                return;
            }
            RevrsReader bootupReader = new(File.ReadAllBytes(bootupPath), Helpers.ModEndianness);
            ImmutableSarc bootup = new(ref bootupReader);
            RevrsReader levelReader = new(Yaz0.Decompress(bootup["Ecosystem/LevelSensor.sbyml"].Data), Helpers.ModEndianness);
            ImmutableByml levelSensor = new(ref levelReader);
            ImmutableBymlMap levelMap = levelSensor.GetMap();
            ImmutableBymlStringTable stringTable = levelSensor.StringTable;
            ImmutableBymlStringTable keyTable = levelSensor.KeyTable;
            foreach (ImmutableByml entry in levelMap.GetValue(keyTable, "flag").GetArray())
            {
                string flagName = entry.GetMap().GetValue(keyTable, "name").GetString(stringTable);
                StageFlag(new(
                    flagName,
                    FlagUnionType.S32,
                    isSave: true,
                    resetType: 0,
                    isRevival: false
                )
                {
                    InitValue = 0,
                    MaxValue = 65535,
                    MinValue = 0
                }, GeneratorConfidence.Good);
            }
        }

        private void GenerateMainStaticFlags(string path)
        {
            Span<byte> bytes = Yaz0.Decompress(File.ReadAllBytes(path));
            RevrsReader reader = new(bytes, Helpers.ModEndianness);
            ImmutableByml modStatic = new(ref reader);
            ImmutableBymlStringTable keyTable = modStatic.KeyTable;
            ImmutableBymlStringTable stringTable = modStatic.StringTable;
            foreach (ImmutableByml marker in modStatic
                .GetMap()
                .GetValue(keyTable, "LocationMarker")
                .GetArray()
            )
            {
                ImmutableBymlMap markerMap = marker.GetMap();
                string str;
                if (markerMap.TryGetValue(keyTable, "SaveFlag", out ImmutableByml flagByml) &&
                    (str = flagByml.GetString(stringTable)) != null &&
                    !Helpers.VanillaLocSaveFlags.Contains(str))
                {
                    StageFlag(new(str, FlagUnionType.S32, isSave: true, resetType: 0)
                    {
                        InitValue = 0,
                        MaxValue = 2147483647,
                        MinValue = -2147483648
                    }, GeneratorConfidence.Definite);
                }
                if (markerMap.TryGetValue(keyTable, "Icon", out ImmutableByml icon) &&
                    icon.GetString(stringTable) == "Dungeon" &&
                    markerMap.TryGetValue(keyTable, "MessageID", out ImmutableByml messageId) &&
                    (str = messageId.GetString(stringTable)) != string.Empty &&
                    Helpers.ModShrineLocs.ContainsKey(str))
                {
                    StageFlag(new(
                        $"Enter_{str}",
                        FlagUnionType.Bool,
                        isOneTrigger: true,
                        isSave: true,
                        resetType: 0
                    ), GeneratorConfidence.Definite);

                    StageFlag(new(
                        $"CompleteTreasure_{str}",
                        FlagUnionType.Bool,
                        isOneTrigger: true,
                        isSave: true,
                        resetType: 0
                    ), GeneratorConfidence.Definite);
                }
            }
        }

        public void GenerateMapFlags()
        {
            EnumerationOptions options = new() { RecurseSubdirectories = true };
            if (Helpers.TryGetFullModDlcPath("Map/MainField", out var mainfieldPath))
            {
                foreach (string path in Directory.EnumerateFiles(
                    mainfieldPath, "?-?_*.smubin", options
                ))
                {
                    Span<byte> bytes = Yaz0.Decompress(File.ReadAllBytes(path));
                    RevrsReader reader = new(bytes, Helpers.ModEndianness);
                    ImmutableByml modMap = new(ref reader);
                    if (!Helpers.TryGetStockMainFieldMapReader(
                        Path.GetFileName(path), out RevrsReader stockReader
                    ))
                    {
                        throw new InvalidDataException(
                            $"Vanilla counterpart not found for {Path.GetFileNameWithoutExtension(path)}"
                        );
                    }
                    GenerateFlagsForMapWithDiff(
                        modMap,
                        new(ref stockReader),
                        MapType.MainField,
                        Path.GetFileName(path).Split('_')[0]
                    );
                }
            }

            if (Helpers.TryGetFullModDlcPath("Map/MainField/Static.smubin", out var mainStaticPath))
            {
                GenerateMainStaticFlags(mainStaticPath);
            }

            // TODO: Do Dlc Pack path as well
            if (!Helpers.TryGetFullModGamePath("Pack", out var packPath))
            {
                return;
            }
            foreach (string path in Directory.EnumerateFiles(
                packPath,
                "Dungeon*.pack",
                options
            ))
            {
                string mapName = Path.GetFileNameWithoutExtension(path);
                RevrsReader reader = new(File.ReadAllBytes(path));
                ImmutableSarc modPack = new(ref reader);
                bool hasStock =
                    Helpers.TryGetStockPackReader(Path.GetFileName(path), out RevrsReader stockReader);

                if (hasStock)
                {
                    ImmutableSarc stockPack = new(ref stockReader);
                    foreach (string suffix in (string[])["_Static", "_Dynamic"])
                    {
                        string sarcPath = $"Map/CDungeon/{mapName}/{mapName}{suffix}.smubin";
                        RevrsReader modMapReader = new(
                            Yaz0.Decompress(modPack[sarcPath].Data), Helpers.ModEndianness
                        );
                        ImmutableByml modMap = new(ref modMapReader);

                        RevrsReader stockMapReader = new(
                            Yaz0.Decompress(stockPack[sarcPath].Data), Helpers.ModEndianness
                        );
                        ImmutableByml stockMap = new(ref stockMapReader);
                        GenerateFlagsForMapWithDiff(modMap, stockMap, MapType.CDungeon, mapName);
                    }
                }
                else
                {
                    foreach (string suffix in (string[])["_Static", "_Dynamic"])
                    {
                        string sarcPath = $"Map/CDungeon/{mapName}/{mapName}{suffix}.smubin";
                        RevrsReader modMapReader = new(
                            Yaz0.Decompress(modPack[sarcPath].Data), Helpers.ModEndianness
                        );
                        GenerateFlagsForMap(new(ref modMapReader), MapType.CDungeon, mapName);
                    }
                }
            }
        }

        public void GenerateQuestFlags()
        {
            if (!Helpers.TryGetFullModGamePath("Pack/TitleBG.pack", out var titleBgPath) ||
                !File.Exists(titleBgPath))
            {
                return;
            }
            RevrsReader titleBgReader = new(File.ReadAllBytes(titleBgPath), Helpers.ModEndianness);
            ImmutableSarc titleBg = new(ref titleBgReader);
            RevrsReader questReader = new(Yaz0.Decompress(titleBg["Quest/QuestProduct.sbquestpack"].Data), Helpers.ModEndianness);
            ImmutableByml questProduct = new(ref questReader);
            ImmutableBymlStringTable stringTable = questProduct.StringTable;
            ImmutableBymlStringTable keyTable = questProduct.KeyTable;
            foreach (ImmutableByml entry in questProduct.GetArray())
            {
                foreach (ImmutableByml stepDep in entry.GetMap().GetValue(keyTable, "StepDependencyFlags").GetArray())
                {
                    string flagName = stepDep.GetMap().GetValue(keyTable, "NextFlag").GetString(stringTable);
                    StageFlag(new(
                        flagName,
                        FlagUnionType.Bool,
                        isSave: true,
                        resetType: 0,
                        isRevival: false
                    )
                    {
                        InitValue = 0,
                        MaxValue = true,
                        MinValue = false
                    }, GeneratorConfidence.Definite);
                }
                foreach (ImmutableByml step in entry.GetMap().GetValue(keyTable, "Steps").GetArray())
                {
                    string flagName = step.GetMap().GetValue(keyTable, "NextFlag").GetString(stringTable);
                    StageFlag(new(
                        flagName,
                        FlagUnionType.Bool,
                        isSave: true,
                        resetType: 0,
                        isRevival: false
                    )
                    {
                        InitValue = 0,
                        MaxValue = true,
                        MinValue = false
                    }, GeneratorConfidence.Definite);
                }
            }
        }

        private static string GetNearestDungeonName(ImmutableByml byml)
        {
            // Can't use the implicit conversion because that's for bgdata
            // which has an extra nested array because thanks, Nintendo
            ImmutableBymlArray array = byml.GetArray();
            return Helpers.GetNearestShrine(
                new Vec3(array[0].GetFloat(), array[1].GetFloat(), array[2].GetFloat())
            );
        }

        private static int GetRandomInitValue(string mapName)
        {
            string field = mapName.Split('_')[0];
            if (field[1] != '-')
            {
                return 254;
            }
            string[] parts = field.Split('-');
            int horizontal = char.Parse(parts[0]) - 65;
            int vertical = int.Parse(parts[1]);
            return (horizontal * 8 + vertical) * 2;
        }

        public void ReplaceManager(FlagMgr mgr)
        {
            this.Mgr = mgr;
            FlagConfidence = mgr.GetAllFlags()
                .Select(f => (f.HashValue, GeneratorConfidence.Definite))
                .ToDictionary();
        }

        private void StageFlag(Flag flag, GeneratorConfidence confidence)
        {
            if (flagsToAdd.Add(flag))
            {
                FlagConfidence[flag.HashValue] = confidence;
            }
        }

        public void FinalizeGeneration()
        {
            foreach (Flag flag in flagsToAdd)
            {
                if (Helpers.VanillaFlagHashes.Contains(flag.HashValue))
                {
                    FlagConfidence.Remove(flag.HashValue);
                }
                else
                {
                    Mgr.Add(flag);
                }
            }
            flagsToAdd.Clear();
        }
    }
}
