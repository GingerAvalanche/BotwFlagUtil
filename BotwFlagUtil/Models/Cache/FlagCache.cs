using BotwFlagUtil.GameData;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;

namespace BotwFlagUtil.Models.Cache
{
    [ProtoContract]
    internal class FlagCache
    {
        [ProtoMember(1)]
        private static Dictionary<NintendoHash, FlagDiff> cache = [];
        private static bool initialized = false;
        private static string modName = string.Empty;
        private static readonly Dictionary<NintendoHash, Flag> orig = [];

        internal static void Init(string modName_, FlagMgr mgr)
        {
            if (initialized)
            {
                throw new InvalidOperationException("FlagCache already initialized!");
            }
            foreach (Flag flag in mgr.GetAllFlags())
            {
                orig[flag.HashValue] = flag;
            }
            modName = modName_;
            using (FileStream stream = File.OpenRead(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "botw_tools", $"{modName}.cache")))
            {
                cache = Serializer.Deserialize<Dictionary<NintendoHash, FlagDiff>>(stream);
            }
            initialized = true;
        }

        internal static void Apply(Flag new_)
        {
            if (!initialized)
            {
                throw new InvalidOperationException("FlagCache not initialized!");
            }
            cache[new_.HashValue] = new FlagDiff(new_, orig[new_.HashValue]);
            using FileStream stream = File.OpenWrite(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "botw_tools", $"{modName}.cache"));
            Serializer.Serialize(stream, cache);
        }

        internal static void Recall(ref Flag flag)
        {
            if (!initialized)
            {
                throw new InvalidOperationException("FlagCache not initialized!");
            }
            if (cache.TryGetValue(flag.HashValue, out FlagDiff? diff) &&
                orig.TryGetValue(flag.HashValue, out Flag origFlag))
            {
                flag = diff.Apply(origFlag);
            }
        }
    }
}
