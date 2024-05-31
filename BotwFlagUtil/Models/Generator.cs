using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Aamp.Security.Cryptography;
using BfevLibrary;
using BfevLibrary.Core;
using BotwFlagUtil.Enums;
using BotwFlagUtil.GameData;
using BotwFlagUtil.GameData.Util;
using BymlLibrary;
using BymlLibrary.Nodes.Immutable.Containers;
using CsYaz0;
using Nintendo.Aamp;
using Revrs;
using SarcLibrary;

namespace BotwFlagUtil
{
    public class Generator
    {
        public FlagMgr mgr;
        public Dictionary<NintendoHash, GeneratorConfidence> flagConfidence;
        private readonly HashSet<NintendoHash> orphanedFlagHashes;
        private readonly HashSet<Flag> flagsToAdd;
        private static readonly string[] linkTagFlagNames =
        [
            "{0}",
            "Clear_{0}",
            "Open_{0}",
            "{0}_{1}_{2}"
        ];
        private static readonly HashSet<string> noZukanFlagActors =
        [
            "DgnObj_DLC_Weapon_Sword_502",
            "Priest_Boss_Giant",
            "Priest_Boss_Normal",
            "Priest_Boss_ShadowClone_Real",
        ];
        private static readonly HashSet<string> noShopFlagActors =
        [
            "Npc_DressFairy_00",
            "Npc_DressFairy_01",
            "Npc_DressFairy_02",
            "Npc_DressFairy_03",
        ];
        private static readonly Dictionary<NintendoHash, FlagCategory> zukanCategoryMap = new()
        {
            { 0x24CD75FE, FlagCategory.Animal },
            { 0x2755F107, FlagCategory.Weapon },
            { 0x36565B66, FlagCategory.Boss },
            { 0x682E5129, FlagCategory.Sozai },
            { 0x994AEF4B, FlagCategory.Enemy },
            { 0xBB8D80C2, FlagCategory.Other },
        };
        private static readonly HashSet<string> armorProfiles =
        [
            "ArmorHead",
            "ArmorUpper",
            "ArmorLower",
            //"ArmorExtra0",
            //"ArmorExtra1",
            //"ArmorExtra2",
        ];

        public Generator()
        {
            mgr = new();
            flagConfidence = [];
            orphanedFlagHashes = [];
            flagsToAdd = new(new HashValueComparer());
        }

        public void GenerateActorFlags()
        {
            if (Helpers.RootDir == null)
            {
                throw new InvalidOperationException("Evaluating maps before setting root directory!");
            }
            string actorInfoPath = Helpers.GetFullModPath("Actor/ActorInfo.product.sbyml");
            if (actorInfoPath == string.Empty)
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
                string profile = string.Empty;

                if (map.TryGetValue(keyTable, "profile", out ImmutableByml profileNode) &&
                    (profile = profileNode.GetString(stringTable)) == "NPC")
                {
                    npcsToCheck.Add(actorName);
                }

                if (map.TryGetValue(keyTable, "tags", out ImmutableByml tags))
                {
                    ImmutableBymlMap tagsMap = tags.GetMap();
                    foreach (ImmutableBymlMapEntry entry in tagsMap)
                    {
                        NintendoHash entryValue = entry.Node;
                        if (entryValue == 0x19F6C13A && profile != "Bullet")
                        {
                            // Skip flags for arrows that are static items
                            // Will still generate for arrows that are actually arrows
                            break;
                        }
                        if (zukanCategoryMap.TryGetValue(entryValue, out FlagCategory category) &&
                            !noZukanFlagActors.Contains(actorName))
                        {
                            if (map.TryGetValue(keyTable, "drops", out ImmutableByml _))
                            {
                                // Skip actors that are zukan but this isn't the important actor
                                continue;
                            }
                            if (actorName.StartsWith("Armor_", StringComparison.Ordinal) &&
                                map.TryGetValue(keyTable, "normal0ItemName01", out var ingredient) &&
                                !ingredient.GetString(stringTable)
                                    .StartsWith("Armor_", StringComparison.Ordinal))
                            {
                                // Skip armors that can be crafted and whose first ingredient
                                // is another armor (aka actors that are upgraded armors)
                                continue;
                            }

                            StageFlag(new(
                                $"IsNewPictureBook_{actorName}",
                                FlagUnionType.Bool,
                                isSave: true
                            ) {
                                MaxValue = true
                            }, GeneratorConfidence.Definite);

                            StageFlag(new(
                                $"IsRegisteredPictureBook_{actorName}",
                                FlagUnionType.Bool,
                                isSave: true
                            ) {
                                Category = (int)category,
                                MaxValue = true
                            }, GeneratorConfidence.Definite);

                            StageFlag(new(
                                $"PictureBookSize_{actorName}",
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
                            if (map.TryGetValue(keyTable, "drops", out ImmutableByml drops) &&
                                drops.GetMap().Count > 0)
                            {
                                // Skip actors that can be got but give a different actor
                                continue;
                            }
                            if (!map.TryGetValue(keyTable, "itemSellingPrice", out ImmutableByml _))
                            {
                                // Skip actors that can be got but don't have a sale price node
                                continue;
                            }
                            if (armorProfiles.Contains(profile))
                            {
                                // Skip armors; game behavior means the IsGet Demo will always
                                // be played for them, regardless of the flag value
                                continue;
                            }

                            StageFlag(new(
                                $"IsGet_{actorName}",
                                FlagUnionType.Bool,
                                isOneTrigger: true,
                                isSave: true
                            ) {
                                MaxValue = true
                            }, GeneratorConfidence.Definite);
                        }

                        if (entryValue.uvalue == 0x289F28B5) // CanEquip
                        {
                            if (actorName.StartsWith("Armor_", StringComparison.Ordinal) &&
                                map.TryGetValue(keyTable, "normal0ItemName01", out var ingredient) &&
                                !ingredient.GetString(stringTable)
                                    .StartsWith("Armor_", StringComparison.Ordinal))
                            {
                                // Skip armors that can be crafted and whose first ingredient
                                // is another armor (aka actors that are upgraded armors)
                                continue;
                            }

                            StageFlag(new(
                                $"EquipTime_{actorName}",
                                FlagUnionType.S32,
                                isSave: true
                            ) {
                                MaxValue = 2147483647
                            }, GeneratorConfidence.Definite);

                            StageFlag(new(
                                $"PorchTime_{actorName}",
                                FlagUnionType.S32,
                                isSave: true
                            ) {
                                MaxValue = 2147483647
                            }, GeneratorConfidence.Definite);
                        }
                    }
                }
            }

            foreach (string actorName in npcsToCheck)
            {
                if (noShopFlagActors.Contains(actorName))
                {
                    continue; // Ignore DressFairies, their inventory is fake
                }
                string path = Helpers.GetFullModPath($"Actor/Pack/{actorName}.sbactorpack");
                if (path == string.Empty)
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
            string? flagName;
            // TODO: Handle event packs in titlebg (and bootup?)
            string path = Helpers.GetFullModPath("Event");
            if (path == string.Empty)
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
                        Dictionary<int, string> idxToActorMap = [];
                        Dictionary<string, Dictionary<int, string>> actorToIdxToActionMap = [];
                        Dictionary<string, Dictionary<int, string>> actorToIdxToQueryMap = [];
                        if (evfl.Flowchart != null)
                        {
                            foreach (Event e in evfl.Flowchart.Events)
                            {
                                if (e is ActionEvent action)
                                {
                                    if (action.ActorAction != null && action.Parameters != null &&
                                        Helpers.actionParams.TryGetValue(
                                            action.ActorAction, out HashSet<string>? actionFlags
                                        )
                                    )
                                    {
                                        foreach (string actionFlag in actionFlags)
                                        {
                                            if (Helpers.boolFlags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.Bool),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.floatFlags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.F32),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.intFlags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.S32),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.stringFlags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.String),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.vec3Flags.Contains(actionFlag))
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
                                    if (@switch.ActorQuery != null && @switch.Parameters != null &&
                                        Helpers.queryParams.TryGetValue(
                                            @switch.ActorQuery, out HashSet<string>? queryFlags
                                        )
                                    )
                                    {
                                        foreach (string queryFlag in queryFlags)
                                        {
                                            if (Helpers.boolFlags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.Bool),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.floatFlags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.F32),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.intFlags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.S32),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.stringFlags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    StageFlag(new(flagName, FlagUnionType.String),
                                                        GeneratorConfidence.Bad);
                                                }
                                            }
                                            else if (Helpers.vec3Flags.Contains(queryFlag))
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
        /// <returns>False if the flag should not be made, or true if it should</returns>
        /// <exception cref="InvalidDataException">Thrown under 2 conditions:<br />
        /// - byml is a LinkTag with MakeSaveFlag 1 and mapType is MapType.MainField<br />
        /// - byml is a LinkTag with MakeSaveFlag 2 and mapType is MapType.CDungeon</exception>
        /// <exception cref="NotImplementedException">Thrown when byml is a LinkTag with MakeSaveFlag
        /// that is not 0-3</exception>
        private static bool GenerateFlagForMapActor(
            ImmutableByml byml,
            ImmutableBymlStringTable keyTable,
            ImmutableBymlStringTable stringTable,
            MapType mapType,
            string mapName,
            out Flag value,
            out GeneratorConfidence confidence
        )
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
                            flagName = string.Format(linkTagFlagNames[makeSaveFlag], parts);
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

                        bool s32 = false;
                        if (paramsMap.TryGetValue(
                                keyTable, "IncrementSave", out ImmutableByml linkIncrement
                            ) &&
                            linkIncrement.GetBool())
                        {
                            s32 = true;
                        }

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
                else if (!Helpers.vanillaHasFlags["no_flags"].Contains(actorName))
                {
                    string flagName = $"{mapType}_{actorName}_{hashId}";
                    int resetType = mapType == MapType.MainField ? 1 : 2;
                    bool revival = true;
                    confidence = 0;
                    if (actorName.Contains("TBox"))
                    {
                        if (map.GetValue(keyTable, "!Parameters").GetMap().TryGetValue(
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
                        confidence = GeneratorConfidence.Definite;
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
                                confidence = GeneratorConfidence.Definite;
                                break;
                            }
                        }
                    }
                    else
                    {
                        confidence = GeneratorConfidence.Mediocre;
                    }

                    value = new(
                        flagName,
                        FlagUnionType.Bool,
                        isSave: true,
                        resetType: resetType,
                        isRevival: revival
                    ) {
                        InitValue = 0,
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
            string mainfieldPath = Helpers.GetFullModPath("Map/MainField");
            if (mainfieldPath != string.Empty)
            {
                foreach (string path in Directory.EnumerateFiles(
                    mainfieldPath, "?-?_*.smubin", options
                ))
                {
                    Span<byte> bytes = Yaz0.Decompress(File.ReadAllBytes(path));
                    RevrsReader reader = new(bytes, Helpers.ModEndianness);
                    ImmutableByml modMap = new(ref reader);
                    if (!Helpers.GetStockMainFieldMapReader(
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

            string mainStaticPath = Helpers.GetFullModPath("Map/MainField/Static.smubin");
            if (mainStaticPath != string.Empty)
            {
                GenerateMainStaticFlags(mainStaticPath);
            }

            string packPath = Helpers.GetFullModPath("Pack");
            if (packPath == string.Empty)
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
                RevrsReader reader = new(File.ReadAllBytes(path), Helpers.ModEndianness);
                ImmutableSarc modPack = new(ref reader);
                bool hasStock =
                    Helpers.GetStockPackReader(Path.GetFileName(path), out RevrsReader stockReader);
                // CAREFUL: Will ImmutableSarc..ctor() like a default RevrsReader?
                ImmutableSarc stockPack = new(ref stockReader);

                foreach (string suffix in (string[])["_Static", "_Dynamic"])
                {
                    string sarcPath = $"Map/CDungeon/{mapName}/{mapName}{suffix}.smubin";
                    RevrsReader modMapReader = new(
                        Yaz0.Decompress(modPack[sarcPath].Data), Helpers.ModEndianness
                    );
                    ImmutableByml modMap = new(ref modMapReader);

                    if (hasStock)
                    {
                        RevrsReader stockMapReader = new(
                            Yaz0.Decompress(stockPack[sarcPath].Data), Helpers.ModEndianness
                        );
                        ImmutableByml stockMap = new(ref stockMapReader);
                        GenerateFlagsForMapWithDiff(modMap, stockMap, MapType.CDungeon, mapName);
                    }
                    else
                    {
                        GenerateFlagsForMap(modMap, MapType.CDungeon, mapName);
                    }
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

        public void ReplaceManager(FlagMgr mgr)
        {
            this.mgr = mgr;
            flagConfidence = mgr.GetAllFlags()
                .Select(f => (f.HashValue, GeneratorConfidence.Definite))
                .ToDictionary();
        }

        private void StageFlag(Flag flag, GeneratorConfidence confidence)
        {
            if (flagsToAdd.Add(flag))
            {
                flagConfidence[flag.HashValue] = confidence;
            }
        }

        public void FinalizeGeneration()
        {
            foreach (Flag flag in flagsToAdd)
            {
                if (Helpers.vanillaFlagHashes.Contains(flag.HashValue))
                {
                    flagConfidence.Remove(flag.HashValue);
                }
                else
                {
                    mgr.Add(flag);
                }
            }
            flagsToAdd.Clear();
        }
    }
}
