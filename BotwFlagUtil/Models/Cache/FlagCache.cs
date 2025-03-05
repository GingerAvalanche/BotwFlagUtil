using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BotwFlagUtil.Models.GameData;
using BotwFlagUtil.Models.Structs;

namespace BotwFlagUtil.Models.Cache
{
    internal static class FlagCache
    {
        private static readonly string CacheFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "botw_tools",
            "cache");
        private static Dictionary<uint, FlagDiff> _cache = [];
        private static bool _initialized;
        private static string _cacheName = string.Empty;
        private static readonly Dictionary<NintendoHash, Flag> Orig = [];

        internal static void Init(string modName, FlagMgr mgr)
        {
            if (_initialized)
            {
                throw new InvalidOperationException("FlagCache already initialized!");
            }
            foreach (Flag flag in mgr.GetAllFlags())
            {
                Orig[flag.HashValue] = flag;
            }
            _cacheName = $"{modName}_flag.cache";
            if (!Directory.Exists(CacheFolder))
            {
                Directory.CreateDirectory(CacheFolder);
            }
            else
            {
                string cachePath = Path.Combine(CacheFolder, _cacheName);
                if (File.Exists(cachePath))
                {
                    using FileStream stream = File.OpenRead(cachePath);
                    _cache = Serializer.Deserialize<Dictionary<uint, FlagDiff>>(stream);
                }
            }
            _initialized = true;
        }

        internal static void Apply(Flag newFlag)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("FlagCache not initialized!");
            }
            FlagDiff diff = new(newFlag, Orig[newFlag.HashValue]);
            if (!diff.IsEmpty())
            {
                _cache[newFlag.HashValue.uvalue] = diff;
            }
            else
            {
                _cache.Remove(newFlag.HashValue.uvalue);
            }
            using FileStream stream = File.OpenWrite(Path.Combine(CacheFolder, _cacheName));
            Serializer.Serialize(stream, _cache);
        }

        internal static IEnumerable<Flag> RecallAll()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("FlagCache not initialized!");
            }
            return _cache.Where(kvp => Orig.ContainsKey(kvp.Key)).Select(kvp => kvp.Value.Apply(Orig[kvp.Key]));
        }
    }
}
