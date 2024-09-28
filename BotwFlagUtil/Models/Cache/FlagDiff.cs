﻿using BotwFlagUtil.GameData;
using BotwFlagUtil.GameData.Util;
using ProtoBuf;

namespace BotwFlagUtil.Models.Cache
{
    [ProtoContract]
    internal class FlagDiff
    {
        [ProtoMember(1)]
        public string? InitValue { get; set; }
        [ProtoMember(2)]
        public bool? IsEventAssociated { get; set; }
        [ProtoMember(3)]
        public bool? IsOneTrigger { get; set; }
        [ProtoMember(4)]
        public bool? IsProgramReadable { get; set; }
        [ProtoMember(5)]
        public bool? IsProgramWritable { get; set; }
        [ProtoMember(6)]
        public bool? IsSave { get; set; }
        [ProtoMember(7)]
        public string? MaxValue { get; set; }
        [ProtoMember(8)]
        public string? MinValue { get; set; }
        [ProtoMember(9)]
        public int? ResetType { get; set; }

        public FlagDiff(Flag mod, Flag orig)
        {
            if (mod.InitValue != orig.InitValue)
            {
                InitValue = mod.InitValue.ToString();
            }
            if (mod.IsEventAssociated != orig.IsEventAssociated)
            {
                IsEventAssociated = mod.IsEventAssociated;
            }
            if (mod.IsOneTrigger != orig.IsOneTrigger)
            {
                IsOneTrigger = mod.IsOneTrigger;
            }
            if (mod.IsProgramReadable != orig.IsProgramReadable)
            {
                IsProgramReadable = mod.IsProgramReadable;
            }
            if (mod.IsProgramWritable != orig.IsProgramWritable)
            {
                IsProgramWritable = mod.IsProgramWritable;
            }
            if (mod.IsSave != orig.IsSave)
            {
                IsSave = mod.IsSave;
            }
            if (mod.MaxValue != orig.MaxValue)
            {
                MaxValue = mod.MaxValue.ToString();
            }
            if (mod.MinValue != orig.MinValue)
            {
                MinValue = mod.MinValue.ToString();
            }
            if (mod.ResetType != orig.ResetType)
            {
                ResetType = mod.ResetType;
            }
        }

        public Flag Apply(Flag orig)
        {
            Flag newFlag = orig;
            if (InitValue != null)
            {
                newFlag.InitValue = FlagUnion.FromString(newFlag.InitValue.Type, InitValue);
            }
            if (IsEventAssociated.HasValue)
            {
                newFlag.IsEventAssociated = IsEventAssociated.Value;
            }
            if (IsOneTrigger.HasValue)
            {
                newFlag.IsOneTrigger = IsOneTrigger.Value;
            }
            if (IsProgramReadable.HasValue)
            {
                newFlag.IsProgramReadable = IsProgramReadable.Value;
            }
            if (IsProgramWritable.HasValue)
            {
                newFlag.IsProgramWritable = IsProgramWritable.Value;
            }
            if (IsSave.HasValue)
            {
                newFlag.IsSave = IsSave.Value;
            }
            if (MaxValue != null)
            {
                newFlag.MaxValue = FlagUnion.FromString(newFlag.MaxValue.Type, MaxValue);
            }
            if (MinValue != null)
            {
                newFlag.MinValue = FlagUnion.FromString(newFlag.MinValue.Type, MinValue);
            }
            if (ResetType.HasValue)
            {
                newFlag.ResetType = ResetType.Value;
            }
            return newFlag;
        }
    }
}
