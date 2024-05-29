using Aamp.Security.Cryptography;
using BotwFlagUtil.GameData.Util;
using BymlLibrary;
using BymlLibrary.Extensions;
using BymlLibrary.Nodes.Immutable.Containers;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BotwFlagUtil.GameData
{
    public struct Flag : IEquatable<Flag>
    {
        private string dataName = string.Empty;
        private NintendoHash hashValue = 0;
        private int? category = null;
        private readonly int deleteRev = -1;
        private FlagUnion initValue = default;
        private bool isEventAssociated = false;
        private bool isOneTrigger = false;
        private bool isProgramReadable = false;
        private bool isProgramWritable = false;
        private bool isSave = false;
        private FlagUnion maxValue = default;
        private FlagUnion minValue = default;
        private int resetType = 0;
        private bool? isRevival = null;

        [JsonConstructor]
        public Flag(
            string dataName,
            int hashValue,
            int? category,
            int deleteRev,
            FlagUnion initValue,
            bool isEventAssociated,
            bool isOneTrigger,
            bool isProgramReadable,
            bool isProgramWritable,
            bool isSave,
            FlagUnion maxValue,
            FlagUnion minValue,
            int resetType,
            bool? isRevival
        )
        {
            this.dataName = dataName;
            this.hashValue = hashValue;
            this.category = category;
            this.deleteRev = deleteRev;
            this.initValue = initValue;
            this.isEventAssociated = isEventAssociated;
            this.isOneTrigger = isOneTrigger;
            this.isProgramReadable = isProgramReadable;
            this.isProgramWritable = isProgramWritable;
            this.isSave = isSave;
            this.maxValue = maxValue;
            this.minValue = minValue;
            this.resetType = resetType;
            this.isRevival = isRevival;
        }

        public Flag(
            string dataName,
            FlagUnionType type,
            int deleteRev = -1,
            bool isEventAssociated = false,
            bool isOneTrigger = false,
            bool isProgramReadable = true,
            bool isProgramWritable = true,
            bool isSave = false,
            int resetType = 0,
            bool? isRevival = null
        )
        {
            this.dataName = string.Empty;
            DataName = dataName;
            if (type != FlagUnionType.None)
            {
                this.deleteRev = deleteRev;
                FlagUnionType initType = Helpers.mainTypeToInitType[type];
                initValue = initType;
                this.isEventAssociated = isEventAssociated;
                this.isOneTrigger = isOneTrigger;
                this.isProgramReadable = isProgramReadable;
                this.isProgramWritable = isProgramWritable;
                this.isSave = isSave;
                FlagUnionType boundingtype = Helpers.mainTypeToMaxOrMinType[type];
                maxValue = boundingtype;
                minValue = boundingtype;
                this.resetType = resetType;
                this.isRevival = isRevival;
            }
        }

        public Flag(ImmutableByml byml, FlagUnionType type, ImmutableBymlStringTable keyTable, ImmutableBymlStringTable stringTable, bool? isRevival = null)
        {
            dataName = string.Empty;
            foreach ((int keyIndex, ImmutableByml node) in byml.GetMap())
            {
                switch (keyTable[keyIndex].ToManaged())
                {
                    case "DataName":
                        dataName = stringTable[node.GetStringIndex()].ToManaged();
                        break;
                    case "HashValue":
                        hashValue = node.GetInt();
                        break;
                    case "InitValue":
                        initValue = node.GetFlagUnion(Helpers.mainTypeToInitType[type], stringTable);
                        break;
                    case "IsEventAssociated":
                        isEventAssociated = node.GetBool();
                        break;
                    case "IsOneTrigger":
                        isOneTrigger = node.GetBool();
                        break;
                    case "IsProgramReadable":
                        isProgramReadable = node.GetBool();
                        break;
                    case "IsProgramWritable":
                        isProgramWritable = node.GetBool();
                        break;
                    case "IsSave":
                        isSave = node.GetBool();
                        break;
                    case "MaxValue":
                        maxValue = node.GetFlagUnion(Helpers.mainTypeToMaxOrMinType[type], stringTable);
                        break;
                    case "MinValue":
                        minValue = node.GetFlagUnion(Helpers.mainTypeToMaxOrMinType[type], stringTable);
                        break;
                    case "ResetType":
                        resetType = node.GetInt();
                        break;
                    default:
                        break;
                }
            }
            this.isRevival = isRevival;
        }

        public Flag(Byml byml, FlagUnionType type, bool? isRevival = null)
        {
            dataName = string.Empty;
            foreach ((string key, Byml node) in byml.GetMap())
            {
                switch (key)
                {
                    case "DataName":
                        dataName = node.GetString();
                        break;
                    case "HashValue":
                        hashValue = node.GetInt();
                        break;
                    case "InitValue":
                        initValue = node.GetFlagUnion(Helpers.mainTypeToInitType[type]);
                        break;
                    case "IsEventAssociated":
                        isEventAssociated = node.GetBool();
                        break;
                    case "IsOneTrigger":
                        isOneTrigger = node.GetBool();
                        break;
                    case "IsProgramReadable":
                        isProgramReadable = node.GetBool();
                        break;
                    case "IsProgramWritable":
                        isProgramWritable = node.GetBool();
                        break;
                    case "IsSave":
                        isSave = node.GetBool();
                        break;
                    case "MaxValue":
                        maxValue = node.GetFlagUnion(Helpers.mainTypeToMaxOrMinType[type]);
                        break;
                    case "MinValue":
                        minValue = node.GetFlagUnion(Helpers.mainTypeToMaxOrMinType[type]);
                        break;
                    case "ResetType":
                        resetType = node.GetInt();
                        break;
                    default:
                        break;
                }
            }
            this.isRevival = isRevival;
        }

        public string DataName
        {
            readonly get => dataName;
            private set
            {
                dataName = value;
                hashValue = (int)Crc32.Compute(dataName);
            }
        }
        public readonly NintendoHash HashValue { get => hashValue; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Category { readonly get => category; set => category = value; }
        public readonly int DeleteRev { get => deleteRev; }
        public FlagUnion InitValue { readonly get => initValue; set => initValue = value; }
        public bool IsEventAssociated
        {
            readonly get => isEventAssociated; set => isEventAssociated = value;
        }
        public bool IsOneTrigger { readonly get => isOneTrigger; set => isOneTrigger = value; }
        public bool IsProgramReadable
        {
            readonly get => isProgramReadable; set => isProgramReadable = value;
        }
        public bool IsProgramWritable
        {
            readonly get => isProgramWritable; set => isProgramWritable = value;
        }
        public bool IsSave { readonly get => isSave; set => isSave = value; }
        public FlagUnion MaxValue { readonly get => maxValue; set => maxValue = value; }
        public FlagUnion MinValue { readonly get => minValue; set => minValue = value; }
        public int ResetType { readonly get => resetType; set => resetType = value; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsRevival { readonly get => isRevival; set => isRevival = value; }

        public static bool operator ==(Flag first, Flag second) => first.Equals(second);
        public static bool operator !=(Flag first, Flag second) => !first.Equals(second);

        /// <summary>
        /// Convenience function for getting something that will work with HashSet removals.
        /// i.e. HashValue, GetHashCode(), etc.
        /// Do not try to use a temp flag as a real flag.
        /// </summary>
        /// <param name="name">DataName to give the temp flag</param>
        /// <returns></returns>
        public static Flag GetTempFlag(string name) => new(name, FlagUnionType.None);

        public readonly Byml ToByml()
        {
            Dictionary<string, Byml> map = new()
            {
                { "DataName", dataName },
                { "DeleteRev", deleteRev },
                { "HashValue", hashValue.ivalue },
                { "InitValue", initValue.ToByml() },
                { "IsEventAssociated", isEventAssociated },
                { "IsOneTrigger", isOneTrigger },
                { "IsProgramReadable", isProgramReadable },
                { "IsProgramWritable", isProgramWritable },
                { "IsSave", isSave },
                { "MaxValue", maxValue.ToByml() },
                { "MinValue", minValue.ToByml() },
                { "ResetType", resetType },
            };
            return map;
        }

        public readonly Byml ToSvByml()
        {
            return new Dictionary<string, Byml>()
            {
                { "DataName", dataName },
                { "HashValue", hashValue.ivalue }
            };
        }

        public readonly bool Equals(Flag other)
        {
            return hashValue == other.hashValue &&
                isEventAssociated == other.isEventAssociated &&
                isOneTrigger == other.isOneTrigger &&
                isProgramReadable == other.isProgramReadable &&
                isProgramWritable == other.isProgramWritable &&
                isSave == other.isSave &&
                resetType == other.resetType;
        }

        public readonly override bool Equals(object? obj) => obj is Flag flag && Equals(flag);

        public readonly override int GetHashCode() => hashValue.ivalue;

        public readonly override string ToString()
        {
            return $"""
                - DataName: {dataName}
                  DeleteRev: {deleteRev}
                  HashValue: {hashValue}
                  InitValue: {initValue}
                  IsEventAssociated: {isEventAssociated}
                  IsOneTrigger: {isOneTrigger}
                  IsProgramReadable: {isProgramReadable}
                  IsProgramWritable: {isProgramWritable}
                  IsSave: {isSave}
                  MaxValue: {maxValue}
                  MinValue: {minValue}
                  ResetType: {resetType}
                """;
        }
    }
}
