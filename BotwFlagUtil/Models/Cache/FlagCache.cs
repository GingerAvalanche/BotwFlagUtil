using BotwFlagUtil.GameData;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BotwFlagUtil.Models.Cache
{
    internal class FlagCache
    {
        private static readonly string cache_folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "botw_tools", "cache");
        private static Dictionary<uint, FlagDiff> cache = [];
        private static bool initialized = false;
        private static string cacheName = string.Empty;
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
            cacheName = $"{modName_}_flag.cache";
            if (!Directory.Exists(cache_folder))
            {
                Directory.CreateDirectory(cache_folder);
            }
            else
            {
                string cache_path = Path.Combine(cache_folder, cacheName);
                if (File.Exists(cache_path))
                {
                    using FileStream stream = File.OpenRead(cache_path);
                    cache = Serializer.Deserialize<Dictionary<uint, FlagDiff>>(stream);
                }
            }
            initialized = true;
        }

        internal static void Apply(Flag new_)
        {
            if (!initialized)
            {
                throw new InvalidOperationException("FlagCache not initialized!");
            }
            FlagDiff diff = new(new_, orig[new_.HashValue]);
            if (!diff.IsEmpty())
            {
                cache[new_.HashValue.uvalue] = diff;
            }
            else
            {
                cache.Remove(new_.HashValue.uvalue);
            }
            using FileStream stream = File.OpenWrite(Path.Combine(cache_folder, cacheName));
            Serializer.Serialize(stream, cache);
        }

        internal static IEnumerable<Flag> RecallAll()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("FlagCache not initialized!");
            }
            return cache.Where(kvp => orig.ContainsKey(kvp.Key)).Select(kvp => kvp.Value.Apply(orig[kvp.Key]));
        }
    }
}
