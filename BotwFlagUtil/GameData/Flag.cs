using Aamp.Security.Cryptography;
using BotwFlagUtil.GameData.Util;
using BymlLibrary;
using BymlLibrary.Extensions;
using BymlLibrary.Nodes.Immutable.Containers;
using System.Text.Json.Serialization;

namespace BotwFlagUtil.GameData
{
    public struct Flag : IEquatable<Flag>
    {
        private string dataName;
        private NintendoHash hashValue;
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Category;
        [JsonInclude]
        private readonly int DeleteRev;
        [JsonInclude]
        public FlagUnion InitValue;
        [JsonInclude]
        public bool IsEventAssociated;
        [JsonInclude]
        public bool IsOneTrigger;
        [JsonInclude]
        public bool IsProgramReadable;
        [JsonInclude]
        public bool IsProgramWritable;
        [JsonInclude]
        public bool IsSave;
        [JsonInclude]
        public FlagUnion MaxValue;
        [JsonInclude]
        public FlagUnion MinValue;
        [JsonInclude]
        public int ResetType;
        [JsonInclude]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsRevival;

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
            Category = category;
            DeleteRev = deleteRev;
            InitValue = initValue;
            IsEventAssociated = isEventAssociated;
            IsOneTrigger = isOneTrigger;
            IsProgramReadable = isProgramReadable;
            IsProgramWritable = isProgramWritable;
            IsSave = isSave;
            MaxValue = maxValue;
            MinValue = minValue;
            ResetType = resetType;
            IsRevival = isRevival;
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
                DeleteRev = deleteRev;
                FlagUnionType initType = Helpers.mainTypeToInitType[type];
                InitValue = initType;
                IsEventAssociated = isEventAssociated;
                IsOneTrigger = isOneTrigger;
                IsProgramReadable = isProgramReadable;
                IsProgramWritable = isProgramWritable;
                IsSave = isSave;
                FlagUnionType boundingtype = Helpers.mainTypeToMaxOrMinType[type];
                MaxValue = boundingtype;
                MinValue = boundingtype;
                ResetType = resetType;
                IsRevival = isRevival;
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
                        InitValue = node.GetFlagUnion(Helpers.mainTypeToInitType[type], stringTable);
                        break;
                    case "IsEventAssociated":
                        IsEventAssociated = node.GetBool();
                        break;
                    case "IsOneTrigger":
                        IsOneTrigger = node.GetBool();
                        break;
                    case "IsProgramReadable":
                        IsProgramReadable = node.GetBool();
                        break;
                    case "IsProgramWritable":
                        IsProgramWritable = node.GetBool();
                        break;
                    case "IsSave":
                        IsSave = node.GetBool();
                        break;
                    case "MaxValue":
                        MaxValue = node.GetFlagUnion(Helpers.mainTypeToMaxOrMinType[type], stringTable);
                        break;
                    case "MinValue":
                        MinValue = node.GetFlagUnion(Helpers.mainTypeToMaxOrMinType[type], stringTable);
                        break;
                    case "ResetType":
                        ResetType = node.GetInt();
                        break;
                    default:
                        break;
                }
            }
            IsRevival = isRevival;
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
                        InitValue = node.GetFlagUnion(Helpers.mainTypeToInitType[type]);
                        break;
                    case "IsEventAssociated":
                        IsEventAssociated = node.GetBool();
                        break;
                    case "IsOneTrigger":
                        IsOneTrigger = node.GetBool();
                        break;
                    case "IsProgramReadable":
                        IsProgramReadable = node.GetBool();
                        break;
                    case "IsProgramWritable":
                        IsProgramWritable = node.GetBool();
                        break;
                    case "IsSave":
                        IsSave = node.GetBool();
                        break;
                    case "MaxValue":
                        MaxValue = node.GetFlagUnion(Helpers.mainTypeToMaxOrMinType[type]);
                        break;
                    case "MinValue":
                        MinValue = node.GetFlagUnion(Helpers.mainTypeToMaxOrMinType[type]);
                        break;
                    case "ResetType":
                        ResetType = node.GetInt();
                        break;
                    default:
                        break;
                }
            }
            IsRevival = isRevival;
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
                { "DeleteRev", DeleteRev },
                { "HashValue", hashValue.ivalue },
                { "InitValue", InitValue.ToByml() },
                { "IsEventAssociated", IsEventAssociated },
                { "IsOneTrigger", IsOneTrigger },
                { "IsProgramReadable", IsProgramReadable },
                { "IsProgramWritable", IsProgramWritable },
                { "IsSave", IsSave },
                { "MaxValue", MaxValue.ToByml() },
                { "MinValue", MinValue.ToByml() },
                { "ResetType", ResetType },
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
                IsEventAssociated == other.IsEventAssociated &&
                IsOneTrigger == other.IsOneTrigger &&
                IsProgramReadable == other.IsProgramReadable &&
                IsProgramWritable == other.IsProgramWritable &&
                IsSave == other.IsSave &&
                ResetType == other.ResetType;
        }

        public readonly override bool Equals(object? obj) => obj is Flag flag && Equals(flag);

        public readonly override int GetHashCode() => hashValue.GetHashCode();

        public readonly override string ToString()
        {
            return $"""
                - DataName: {dataName}
                  DeleteRev: {DeleteRev}
                  HashValue: {hashValue}
                  InitValue: {InitValue}
                  IsEventAssociated: {IsEventAssociated}
                  IsOneTrigger: {IsOneTrigger}
                  IsProgramReadable: {IsProgramReadable}
                  IsProgramWritable: {IsProgramWritable}
                  IsSave: {IsSave}
                  MaxValue: {MaxValue}
                  MinValue: {MinValue}
                  ResetType: {ResetType}
                """;
        }
    }
}
