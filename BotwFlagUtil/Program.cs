using BfevLibrary;
using BfevLibrary.Core;
using BotwFlagUtil;
using BotwFlagUtil.GameData;
using CsYaz0;
using Revrs;
using SarcLibrary;
using System.Text;

string dump = @"E:\Users\chodn\Documents\ISOs - WiiU\The Legend of Zelda Breath of the Wild (UPDATE DATA) (v208) (USA)";

Dictionary<string, HashSet<string>> actions = [];
Dictionary<string, HashSet<string>> queries = [];
HashSet<string> visitedActions = [];
HashSet<string> visitedQueries = [];
HashSet<string> boolFlagNames = [];
HashSet<string> floatFlagNames = [];
HashSet<string> intFlagNames = [];
HashSet<string> stringFlagNames = [];
HashSet<string> vec3FlagNames = [];

FlagMgr flagMgr = FlagMgr.Open(Path.Combine(dump, "content", "Pack", "Bootup.pack"));

foreach (string fileName in Directory.GetFiles(Path.Combine(dump, "content"), "*.sbeventpack", new EnumerationOptions() { RecurseSubdirectories = true }))
{
    RevrsReader reader = new(Yaz0.Decompress(File.ReadAllBytes(fileName)));
    ImmutableSarc eventPack = new(ref reader);

    ImmutableSarc.Enumerator enumerator = eventPack.GetEnumerator();
    while (enumerator.MoveNext())
    {
        var current = enumerator.Current;
        if (Encoding.UTF8.GetString(current.Data[..8]) == "BFEVFL\0\0")
        {
            BfevFile evfl = BfevFile.FromBinary(current.Data.ToArray());
            Dictionary<int, string> idxToActorMap = [];
            Dictionary<string, Dictionary<int, string>> actorToIdxToActionMap = [];
            Dictionary<string, Dictionary<int, string>> actorToIdxToQueryMap = [];
            if (evfl.Flowchart != null)
            {
                foreach (Event e in evfl.Flowchart.Events)
                {
                    if (e is ActionEvent action)
                    {
                        if (action.ActorAction != null && action.Parameters != null && Helpers.actionParams.TryGetValue(action.ActorAction, out HashSet<string>? actionFlags))
                        {
                            foreach (string flag in actionFlags)
                            {
                                if (Helpers.boolFlags.Contains(flag))
                                {
                                    boolFlagNames.Add(action.Parameters[flag].String!);
                                }
                                else if (Helpers.floatFlags.Contains(flag))
                                {
                                    floatFlagNames.Add(action.Parameters[flag].String!);
                                }
                                else if (Helpers.intFlags.Contains(flag))
                                {
                                    intFlagNames.Add(action.Parameters[flag].String!);
                                }
                                else if (Helpers.stringFlags.Contains(flag))
                                {
                                    stringFlagNames.Add(action.Parameters[flag].String!);
                                }
                                else if (Helpers.vec3Flags.Contains(flag))
                                {
                                    vec3FlagNames.Add(action.Parameters[flag].String!);
                                }
                            }
                        }
                    }
                    else if (e is SwitchEvent @switch)
                    {
                        if (@switch.ActorQuery != null && @switch.Parameters != null && Helpers.queryParams.TryGetValue(@switch.ActorQuery, out HashSet<string>? queryFlags))
                        {
                            foreach (string flag in queryFlags)
                            {
                                if (Helpers.boolFlags.Contains(flag))
                                {
                                    boolFlagNames.Add(@switch.Parameters[flag].String!);
                                }
                                else if (Helpers.floatFlags.Contains(flag))
                                {
                                    floatFlagNames.Add(@switch.Parameters[flag].String!);
                                }
                                else if (Helpers.intFlags.Contains(flag))
                                {
                                    intFlagNames.Add(@switch.Parameters[flag].String!);
                                }
                                else if (Helpers.stringFlags.Contains(flag))
                                {
                                    stringFlagNames.Add(@switch.Parameters[flag].String!);
                                }
                                else if (Helpers.vec3Flags.Contains(flag))
                                {
                                    vec3FlagNames.Add(@switch.Parameters[flag].String!);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

boolFlagNames.Remove(null);
floatFlagNames.Remove(null);
intFlagNames.Remove(null);
stringFlagNames.Remove(null);
vec3FlagNames.Remove(null);

foreach (string flagName in boolFlagNames)
{
    if (!flagMgr.Contains((int)Aamp.Security.Cryptography.Crc32.Compute(flagName)))
    {
        Console.WriteLine($"{flagName} not in vanilla game!");
    }
    //flagMgr.Add(new(flagName, FlagUnionType.Bool, isEventAssociated: true));
}
foreach (string flagName in floatFlagNames)
{
    if (!flagMgr.Contains((int)Aamp.Security.Cryptography.Crc32.Compute(flagName)))
    {
        Console.WriteLine($"{flagName} not in vanilla game!");
    }
    //flagMgr.Add(new(flagName, FlagUnionType.F32, isEventAssociated: true));
}
foreach (string flagName in intFlagNames)
{
    if (!flagMgr.Contains((int)Aamp.Security.Cryptography.Crc32.Compute(flagName)))
    {
        Console.WriteLine($"{flagName} not in vanilla game!");
    }
    //flagMgr.Add(new(flagName, FlagUnionType.S32, isEventAssociated: true));
}
foreach (string flagName in stringFlagNames)
{
    if (!flagMgr.Contains((int)Aamp.Security.Cryptography.Crc32.Compute(flagName)))
    {
        Console.WriteLine($"{flagName} not in vanilla game!");
    }
    //flagMgr.Add(new(flagName, FlagUnionType.String, isEventAssociated: true), FlagStringType.String32);
}
foreach (string flagName in vec3FlagNames)
{
    if (!flagMgr.Contains((int)Aamp.Security.Cryptography.Crc32.Compute(flagName)))
    {
        Console.WriteLine($"{flagName} not in vanilla game!");
    }
    //flagMgr.Add(new(flagName, FlagUnionType.Vec3, isEventAssociated: true));
}
