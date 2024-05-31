using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Aamp.Security.Cryptography;
using BfevLibrary;
using BfevLibrary.Core;
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
    public enum MapType
    {
        MainField,
        CDungeon,
        MainFieldDungeon,
        AocField
    }
    public enum GeneratorConfidence
    {
        Unknown,
        Bad,
        Mediocre,
        Good,
        Definite
    }

    public class Generator
    {
        public FlagMgr mgr;
        public Dictionary<NintendoHash, GeneratorConfidence> flagConfidence;
        private readonly HashSet<NintendoHash> orphanedFlagHashes;
        private static readonly string[] linkTagFlagNames =
        [
            "{0}",
            "Clear_{0}",
            "Open_{0}",
            "{0}_{1}_{2}"
        ];

        public Generator()
        {
            mgr = new();
            flagConfidence = [];
            orphanedFlagHashes = [];
        }

        public void GenerateEventFlags()
        {
            string? flagName;
            // TODO: Handle event packs in titlebg (and bootup?)
            string path = Helpers.GetFullModPath("Event");
            if (!Directory.Exists(path))
            {
                return;
            }
            HashSet<Flag> flagsToAdd = [];
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
                                                    flagsToAdd.Add(new(flagName, FlagUnionType.Bool));
                                                }
                                            }
                                            else if (Helpers.floatFlags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    flagsToAdd.Add(new(flagName, FlagUnionType.F32));
                                                }
                                            }
                                            else if (Helpers.intFlags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    flagsToAdd.Add(new(flagName, FlagUnionType.S32));
                                                }
                                            }
                                            else if (Helpers.stringFlags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    flagsToAdd.Add(new(flagName, FlagUnionType.String));
                                                }
                                            }
                                            else if (Helpers.vec3Flags.Contains(actionFlag))
                                            {
                                                flagName = action.Parameters[actionFlag].String;
                                                if (flagName != null)
                                                {
                                                    flagsToAdd.Add(new(flagName, FlagUnionType.Vec3));
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
                                                    flagsToAdd.Add(new(flagName, FlagUnionType.Bool));
                                                }
                                            }
                                            else if (Helpers.floatFlags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    flagsToAdd.Add(new(flagName, FlagUnionType.F32));
                                                }
                                            }
                                            else if (Helpers.intFlags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    flagsToAdd.Add(new(flagName, FlagUnionType.S32));
                                                }
                                            }
                                            else if (Helpers.stringFlags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    flagsToAdd.Add(new(flagName, FlagUnionType.String));
                                                }
                                            }
                                            else if (Helpers.vec3Flags.Contains(queryFlag))
                                            {
                                                flagName = @switch.Parameters[queryFlag].String;
                                                if (flagName != null)
                                                {
                                                    flagsToAdd.Add(new(flagName, FlagUnionType.Vec3));
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
            foreach (Flag flag in flagsToAdd)
            {
                mgr.Add(flag, FlagStringType.String32);
                // NO idea what event flags should look like, aside from name, hash, type
                flagConfidence[flag.HashValue] = GeneratorConfidence.Bad;
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
                            Category = makeSaveFlag == 1 ? 1 : null,
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
                    mgr.Add(value);
                    flagConfidence[value.HashValue] = confidence;
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
                    if (!flagConfidence.ContainsKey(value.HashValue))
                    {
                        mgr.Add(value);
                        flagConfidence[value.HashValue] = confidence;
                    }
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

        public void GenerateItemFlags()
        {
            if (Helpers.RootDir == null)
            {
                throw new InvalidOperationException("Evaluating maps before setting root directory!");
            }
            string actorPath = Helpers.GetFullModPath("Actor/Pack");
            if (!Directory.Exists(actorPath))
            {
                return;
            }
            foreach (string path in Directory.EnumerateFiles(
                actorPath, "*.sbactorpack"
            ))
            {
                string actorName = Path.GetFileNameWithoutExtension(path);
                Flag flag;
                if (actorName.StartsWith("Animal_", StringComparison.Ordinal))
                {
                    flag = new(
                        $"IsNewPictureBook_{actorName}",
                        FlagUnionType.Bool,
                        isSave: true
                    ) {
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"IsRegisteredPictureBook_{actorName}",
                        FlagUnionType.Bool,
                        isSave: true
                    ) {
                        Category = 2,
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"PictureBookSize_{actorName}",
                        FlagUnionType.S32,
                        isSave: true
                    ) {
                        Category = 2,
                        InitValue = -1,
                        MaxValue = 65536,
                        MinValue = -1
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    RevrsReader reader =
                        new(Yaz0.Decompress(File.ReadAllBytes(path)), Helpers.ModEndianness);
                    ImmutableSarc pack = new(ref reader);
                    AampFile bxml = new(pack[$"Actor/ActorLink/{actorName}.bxml"].Data.ToArray());
                    ParamObject? tags = bxml.RootNode.Objects("Tags");
                    if (tags != null)
                    {
                        foreach (ParamEntry entry in tags.ParamEntries)
                        {
                            if (entry.Value.ToString() == "CanGetPouch")
                            {
                                flag = new(
                                    $"IsGet_{actorName}",
                                    FlagUnionType.Bool,
                                    isOneTrigger: true,
                                    isSave: true
                                ) {
                                    MaxValue = true
                                };
                                mgr.Add(flag);
                                flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;
                                break;
                            }
                        }
                    }
                }
                else if (actorName.StartsWith("Armor_", StringComparison.Ordinal))
                {
                    flag = new(
                        $"IsGet_{actorName}",
                        FlagUnionType.Bool,
                        isOneTrigger: true,
                        isSave: true
                    ) {
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"EquipTime_{actorName}",
                        FlagUnionType.S32,
                        isSave: true
                    ) {
                        MaxValue = 2147483647
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"PorchTime_{actorName}",
                        FlagUnionType.S32,
                        isSave: true
                    ) {
                        MaxValue = 2147483647
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;
                }
                else if (actorName.StartsWith("Enemy_", StringComparison.Ordinal))
                {
                    flag = new(
                        $"IsNewPictureBook_{actorName}",
                        FlagUnionType.Bool,
                        isSave: true
                    ) {
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"IsRegisteredPictureBook_{actorName}",
                        FlagUnionType.Bool,
                        isSave: true
                    ) {
                        Category = 3,
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"PictureBookSize_{actorName}",
                        FlagUnionType.S32,
                        isSave: true
                    ) {
                        Category = 2,
                        InitValue = -1,
                        MaxValue = 65536,
                        MinValue = -1
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;
                }
                else if (actorName.StartsWith("Item_", StringComparison.Ordinal))
                {
                    flag = new(
                        $"IsGet_{actorName}",
                        FlagUnionType.Bool,
                        isOneTrigger: true,
                        isSave: true
                    ) {
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"IsNewPictureBook_{actorName}",
                        FlagUnionType.Bool,
                        isSave: true
                    ) {
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"IsRegisteredPictureBook_{actorName}",
                        FlagUnionType.Bool,
                        isSave: true
                    ) {
                        Category = 4,
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Good;

                    flag = new(
                        $"PictureBookSize_{actorName}",
                        FlagUnionType.S32,
                        isSave: true
                    ) {
                        Category = 2,
                        InitValue = -1,
                        MaxValue = 65536,
                        MinValue = -1
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;
                }
                else if (actorName.StartsWith("Npc_", StringComparison.OrdinalIgnoreCase))
                {
                    RevrsReader reader =
                        new(Yaz0.Decompress(File.ReadAllBytes(path)), Helpers.ModEndianness);
                    ImmutableSarc pack = new(ref reader);
                    AampFile bxml = new(pack[$"Actor/ActorLink/{actorName}.bxml"].Data.ToArray());
                    string shopLink = bxml.RootNode
                        .Objects("LinkTarget")!
                        .Params("ShopDataUser")!
                        .Value.ToString()!;
                    if (shopLink == "Dummy")
                    {
                        continue;
                    }

                    flag = new(
                        $"{actorName}_SoldOut",
                        FlagUnionType.Bool,
                        isSave: true
                    ) {
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

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
                                flag = new(
                                    $"{actorName}_{tableEntry.Value}",
                                    FlagUnionType.S32,
                                    isSave: true
                                ) {
                                    MaxValue = 65535
                                };
                                mgr.Add(flag);
                                flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;
                            }
                        }
                    }
                }
                else if (actorName.StartsWith("Weapon_", StringComparison.Ordinal))
                {
                    flag = new(
                        $"IsGet_{actorName}",
                        FlagUnionType.Bool,
                        isOneTrigger: true,
                        isSave: true
                    ) {
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"IsNewPictureBook_{actorName}",
                        FlagUnionType.Bool,
                        isSave: true
                    ) {
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"IsRegisteredPictureBook_{actorName}",
                        FlagUnionType.Bool,
                        isSave: true
                    ) {
                        Category = 5,
                        MaxValue = true
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"PictureBookSize_{actorName}",
                        FlagUnionType.S32,
                        isSave: true
                    ) {
                        Category = 2,
                        InitValue = -1,
                        MaxValue = 65536,
                        MinValue = -1
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"EquipTime_{actorName}",
                        FlagUnionType.S32,
                        isSave: true
                    ) {
                        MaxValue = 2147483647
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;

                    flag = new(
                        $"PorchTime_{actorName}",
                        FlagUnionType.S32,
                        isSave: true
                    ) {
                        MaxValue = 2147483647
                    };
                    mgr.Add(flag);
                    flagConfidence[flag.HashValue] = GeneratorConfidence.Definite;
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
                    mgr.Add(new(str, FlagUnionType.S32, isSave: true, resetType: 0)
                    {
                        InitValue = 0,
                        MaxValue = 2147483647,
                        MinValue = -2147483648
                    });
                    flagConfidence[Crc32.Compute(str)] = GeneratorConfidence.Definite;
                }
                if (markerMap.TryGetValue(keyTable, "Icon", out ImmutableByml icon) &&
                    icon.GetString(stringTable) == "Dungeon" &&
                    markerMap.TryGetValue(keyTable, "MessageID", out ImmutableByml messageId) &&
                    (str = messageId.GetString(stringTable)) != string.Empty &&
                    Helpers.ModShrineLocs.ContainsKey(str))
                {
                    mgr.Add(new(
                        $"Enter_{str}",
                        FlagUnionType.Bool,
                        isOneTrigger: true,
                        isSave: true,
                        resetType: 0
                    ));
                    flagConfidence[Crc32.Compute($"Enter_{str}")] =
                        GeneratorConfidence.Definite;

                    mgr.Add(new(
                        $"CompleteTreasure_{str}",
                        FlagUnionType.Bool,
                        isOneTrigger: true,
                        isSave: true,
                        resetType: 0
                    ));
                    flagConfidence[Crc32.Compute($"CompleteTreasure_{str}")] =
                        GeneratorConfidence.Definite;
                }
            }
        }

        public void GenerateMapFlags()
        {
            EnumerationOptions options = new() { RecurseSubdirectories = true };
            string mainfieldPath = Helpers.GetFullModPath("Map/MainField");
            if (Directory.Exists(mainfieldPath))
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
            if (File.Exists(mainStaticPath))
            {
                GenerateMainStaticFlags(mainStaticPath);
            }

            string packPath = Helpers.GetFullModPath("Pack");
            if (!Directory.Exists(packPath))
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
    }
}
