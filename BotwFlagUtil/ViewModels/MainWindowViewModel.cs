using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls.Selection;
using Avalonia.Media;
using BotwFlagUtil.Enums;
using BotwFlagUtil.GameData;
using BotwFlagUtil.GameData.Util;
using BotwFlagUtil.Models.Cache;
using ReactiveUI;

namespace BotwFlagUtil.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public static readonly Dictionary<string, SolidColorBrush> Brushes = new()
    {
        { "FgRed", new SolidColorBrush(0xFFEB0050) },
        { "FgYellow", new SolidColorBrush(0xFF9B6E00) },
        { "FgGreen", new SolidColorBrush(0xFF008A00) },
        { "FgBlue", new SolidColorBrush(0xFF006AFF) },
        { "BgGreen", new SolidColorBrush(0xFF98FB98) },
        { "BgNone", new SolidColorBrush(0x00FFFFFF) },
    };

    private string title = "BotwFlagUtil";
    private readonly Generator generator = new();

    private FlagStringType stringType = FlagStringType.None;
    public string Title
    {
        get => title;
        set => this.RaiseAndSetIfChanged(ref title, value);
    }
    public SelectionModel<string> FlagNameSelection { get; }
    public bool NeedsSave { get; private set; }

    #region Flag List Members
    public readonly Dictionary<string, SolidColorBrush> FgColors = [];
    public readonly Dictionary<string, SolidColorBrush> BgColors = [];
    private readonly Dictionary<string, bool> confirmeds = [];
    private List<string> allFlagNames = [];
    private List<string> flagNames = [];

    public List<string> FlagNames
    {
        get => flagNames;
        set => this.RaiseAndSetIfChanged(ref flagNames, value);
    }
    #endregion

    #region Filter Members
    public int filterMask = 0b1111110;
    public bool FilterBlue
    {
        set => filterMask ^= (-Convert.ToInt32(value) ^ filterMask) & (1 << (int)GeneratorConfidence.Definite);
    }
    public bool FilterGreen
    {
        set => filterMask ^= (-Convert.ToInt32(value) ^ filterMask) & (1 << (int)GeneratorConfidence.Good);
    }
    public bool FilterYellow
    {
        set => filterMask ^= (-Convert.ToInt32(value) ^ filterMask) & (1 << (int)GeneratorConfidence.Mediocre);
    }
    public bool FilterRed
    {
        set => filterMask ^= (-Convert.ToInt32(value) ^ filterMask) & (1 << (int)GeneratorConfidence.Bad);
    }
    public bool FilterMan
    {
        set => filterMask ^= (-Convert.ToInt32(value) ^ filterMask) & (1 << 6);
    }
    public bool FilterAuto
    {
        set => filterMask ^= (-Convert.ToInt32(value) ^ filterMask) & (1 << 5);
    }
    #endregion

    #region Current Flag Fields
    private Flag flag = default;
    private FlagUnionType flagType = FlagUnionType.None;
    private string initValue = string.Empty;
    private string maxValue = string.Empty;
    private string minValue = string.Empty;
    private bool canConfirm = true;
    private bool canConfirmInit = true;
    private bool canConfirmMax = true;
    private bool canConfirmMin = true;
    private bool confirmed = false;
    private string confirmText = "Confirmed \u2610";
    #endregion

    #region Current Flag Properties
    public bool IsFlagLoaded
    {
        get => flag.HashValue != 0;
    }
    public bool UseCategory
    {
        get => flag.Category != null;
    }
    public static string[] Categories
    {
        get => [
            "Clear (Dungeon/Shrine Clear)",
            "Animal",
            "Enemy",
            "Sozai (Resources/Ingredients)",
            "Weapon (Swords/Spears/Shields/Bows)",
            "Other",
            "Boss",
            "Hinox/Stalnox",
            "Molduga",
            "Talus",
            "Korok",
            "DLC",
            "HardMode",
        ];
    }
    public string FlagType
    {
        get => flagType.ToString();
        set => this.RaiseAndSetIfChanged(
            ref flagType, (FlagUnionType)Enum.Parse(typeof(FlagUnionType), value)
        );
    }
    public FlagUnionType InitValueType
    {
        get => Helpers.mainTypeToInitType[flagType];
    }
    public string InitValue
    {
        get => initValue;
        set
        {
            this.RaiseAndSetIfChanged(ref initValue, value);
            Confirm(false);
            try {
                FlagUnion initVal = FlagUnion.FromString(InitValueType, value);
                flag.InitValue = initVal;
                CanConfirmInit = true;
            }
            catch (FormatException)
            {
                CanConfirmInit = false;
            }
            catch (ArgumentException)
            {
                CanConfirmMax = false;
            }
        }
    }
    public Flag Flag
    {
        get => flag;
        set => this.RaiseAndSetIfChanged(ref flag, value);
    }
    private FlagUnionType BoundingValueType
    {
        get => Helpers.mainTypeToMaxOrMinType[flagType];
    }
    public string MaxValue
    {
        get => maxValue;
        set
        {
            this.RaiseAndSetIfChanged(ref maxValue, value);
            Confirm(false);
            try {
                FlagUnion maxVal = FlagUnion.FromString(BoundingValueType, value);
                flag.MaxValue = maxVal;
                CanConfirmMax = true;
            }
            catch (FormatException)
            {
                CanConfirmMax = false;
            }
            catch (ArgumentException)
            {
                CanConfirmMax = false;
            }
        }
    }
    public string MinValue
    {
        get => minValue;
        set
        {
            this.RaiseAndSetIfChanged(ref minValue, value);
            Confirm(false);
            try {
                FlagUnion minVal = FlagUnion.FromString(BoundingValueType, value);
                flag.MinValue = minVal;
                CanConfirmMin = true;
            }
            catch (FormatException)
            {
                CanConfirmMin = false;
            }
            catch (ArgumentException)
            {
                CanConfirmMin = false;
            }
        }
    }
    public bool IsEventAssociated
    {
        get => flag.IsEventAssociated;
        set
        {
            flag.IsEventAssociated = value;
            this.RaisePropertyChanged(nameof(IsEventAssociated));
            Confirm(false);
        }
    }
    public bool IsOneTrigger
    {
        get => flag.IsOneTrigger;
        set
        {
            flag.IsOneTrigger = value;
            this.RaisePropertyChanged(nameof(IsOneTrigger));
            Confirm(false);
        }
    }
    public bool IsProgramReadable
    {
        get => flag.IsProgramReadable;
        set
        {
            flag.IsProgramReadable = value;
            this.RaisePropertyChanged(nameof(IsProgramReadable));
            Confirm(false);
        }
    }
    public bool IsProgramWritable
    {
        get => flag.IsProgramWritable;
        set
        {
            flag.IsProgramWritable = value;
            this.RaisePropertyChanged(nameof(IsProgramWritable));
            Confirm(false);
        }
    }
    public bool IsSave
    {
        get => flag.IsSave;
        set
        {
            flag.IsSave = value;
            this.RaisePropertyChanged(nameof(IsSave));
            Confirm(false);
        }
    }
    public static string[] ResetTypes
    {
        get => [
            "Manual reset",
            "Reset on blood moon",
            "Reset on loading screen",
            "Reset at midnight",
            "Reset when Lord of the Mountain appears"
        ];
    }
    public int ResetType
    {
        get => flag.ResetType;
        set
        {
            flag.ResetType = value;
            this.RaisePropertyChanged(nameof(ResetType));
            Confirm(false);
        }
    }
    public bool CanConfirm
    {
        get => canConfirm;
        set => this.RaiseAndSetIfChanged(ref canConfirm, value);
    }
    private bool CanConfirmInit
    {
        set
        {
            canConfirmInit = value;
            CanConfirm = canConfirmInit & canConfirmMax & canConfirmMin;
        }
    }
    private bool CanConfirmMax
    {
        set
        {
            canConfirmMax = value;
            CanConfirm = canConfirmInit & canConfirmMax & canConfirmMin;
        }
    }
    private bool CanConfirmMin
    {
        set
        {
            canConfirmMin = value;
            CanConfirm = canConfirmInit & canConfirmMax & canConfirmMin;
        }
    }

    private string ConfirmText
    {
        get => confirmText;
        set => this.RaiseAndSetIfChanged(ref confirmText, value);
    }
    #endregion

    public MainWindowViewModel()
    {
        FlagNameSelection = new();
        FlagNameSelection.SelectionChanged += OnFlagNameSelected;
    }

    public void OnFlagNameSelected(object? sender, SelectionModelSelectionChangedEventArgs e)
    {
        if (e.SelectedItems.Count > 0 &&
            e.SelectedItems.Single() is string nextFlagName &&
            nextFlagName != flag.DataName && 
            generator.mgr.TryGet(
                nextFlagName, out Flag newFlag, out FlagUnionType fType, out FlagStringType sType
            ))
        {
            // Reset the flag background color to the stored confirmation status
            if (flag.DataName != null && confirmeds.TryGetValue(flag.DataName, out bool value))
            {
                BgColors[flag.DataName].Color = GetBackgroundColor(value).Color;
            }
            Flag = newFlag;
            flagType = fType;
            stringType = sType;
            initValue = newFlag.InitValue.ToString();
            maxValue = newFlag.MaxValue.ToString();
            minValue = newFlag.MinValue.ToString();
            confirmed = confirmeds[nextFlagName];
            SetConfirmText(confirmed);

            // Bit of a workaround to keep the extra behavior of the properties from running
            this.RaisePropertyChanged(nameof(FlagType));
            this.RaisePropertyChanged(nameof(InitValue));
            this.RaisePropertyChanged(nameof(MaxValue));
            this.RaisePropertyChanged(nameof(MinValue));
            this.RaisePropertyChanged(nameof(IsEventAssociated));
            this.RaisePropertyChanged(nameof(IsOneTrigger));
            this.RaisePropertyChanged(nameof(IsProgramReadable));
            this.RaisePropertyChanged(nameof(IsProgramWritable));
            this.RaisePropertyChanged(nameof(IsSave));
            this.RaisePropertyChanged(nameof(ResetType));
            this.RaisePropertyChanged(nameof(UseCategory));
            this.RaisePropertyChanged(nameof(IsFlagLoaded));
        }
    }

    public void OpenMod(string rootDir)
    {
        if (!(Directory.Exists(Path.Combine(rootDir, "content")) ||
            Directory.Exists(Path.Combine(rootDir, "aoc")) ||
            Directory.Exists(Path.Combine(rootDir, "01007EF00011E000", "romfs")) ||
            Directory.Exists(Path.Combine(rootDir, "01007EF00011E800", "romfs")) ||
            Directory.Exists(Path.Combine(rootDir, "01007EF00011F001", "romfs"))))
        {
            return;
        }
        string modName = Path.GetFileName(rootDir);
        Title = $"BotwFlagUtil - {modName}";
        Helpers.RootDir = rootDir;

        generator.ReplaceManager(new());
        generator.GenerateActorFlags();
        generator.GenerateLevelSensorFlags();
        generator.GenerateQuestFlags();
        generator.GenerateMapFlags();
        generator.GenerateEventFlags(); // Last because least certain
        generator.FinalizeGeneration();

        IEnumerable<Flag> flags = generator.mgr.GetAllFlags();
        List<string> flagNamesTemp = new(flags.Count());
        FgColors.Clear();
        FgColors.EnsureCapacity(flagNamesTemp.Capacity);
        BgColors.Clear();
        BgColors.EnsureCapacity(flagNamesTemp.Capacity);
        confirmeds.Clear();
        confirmeds.EnsureCapacity(flagNamesTemp.Capacity);
        foreach (Flag flag in flags)
        {
            flagNamesTemp.Add(flag.DataName);
            FgColors[flag.DataName] = GetForegroundColor(generator.flagConfidence[flag.HashValue]);
            BgColors[flag.DataName] = GetBackgroundColor(false);
            confirmeds[flag.DataName] = false;
        }
        flagNamesTemp.Sort();
        allFlagNames = flagNamesTemp;
        FlagNames = flagNamesTemp.Where(FlagListFilter).ToList();

        FlagCache.Init(modName, generator.mgr);
        foreach (Flag recalled in FlagCache.RecallAll())
        {
            generator.mgr.Replace(recalled);
            BgColors[recalled.DataName] = GetBackgroundColor(true);
            confirmeds[recalled.DataName] = true;
        }
        NeedsSave = true;
        UpdateFilter();
    }

    public void Export()
    {
        if (Helpers.RootDir == null)
        {
            return;
        }

        using FileStream stream = File.Open(Path.Combine(Helpers.RootDir, "flags.json"), FileMode.Create);
        JsonSerializer.Serialize(stream, generator.mgr, Helpers.jsOpt);
    }

    public void Import()
    {
        if (Helpers.RootDir != null)
        {
            string flagPath = Path.Combine(Helpers.RootDir, "flags.json");
            if (File.Exists(flagPath))
            {
                FlagMgr? flagMgr;
                using (FileStream stream = File.OpenRead(flagPath))
                {
                    flagMgr = JsonSerializer.Deserialize<FlagMgr>(stream);
                }
                if (flagMgr != null)
                {
                    generator.ReplaceManager(flagMgr);
                    IEnumerable<Flag> flags = flagMgr.GetAllFlags();
                    List<string> flagNamesTemp = new(flags.Count());
                    FgColors.Clear();
                    FgColors.EnsureCapacity(flagNamesTemp.Capacity);
                    BgColors.Clear();
                    BgColors.EnsureCapacity(flagNamesTemp.Capacity);
                    confirmeds.Clear();
                    confirmeds.EnsureCapacity(flagNamesTemp.Capacity);
                    foreach (Flag flag in flags)
                    {
                        flagNamesTemp.Add(flag.DataName);
                        FgColors[flag.DataName] = GetForegroundColor(generator.flagConfidence[flag.HashValue]);
                        BgColors[flag.DataName] = GetBackgroundColor(false);
                        confirmeds[flag.DataName] = false;
                    }
                    flagNamesTemp.Sort();
                    allFlagNames = flagNamesTemp;
                    UpdateFilter();
                }
            }
        }
    }

    public void UpdateFilter()
    {
        FlagNames = allFlagNames.Where(FlagListFilter).ToList();
    }

    private bool FlagListFilter(string name)
    {
        // Mask, obtain confidence bit by right-shifting by confidence (ignoring possibility of Unknown) AND 1
        return Convert.ToBoolean((filterMask >> (int)generator.flagConfidence[name]) & 1) &&
        // Mask, obtain confirmed bit by right-shifting by confirmed status (Plus 4 to bypass confidence bits) AND 1
            Convert.ToBoolean((filterMask >> (Convert.ToInt32(confirmeds[name]) + 5)) & 1);
    }

    private void SetConfirmText(bool confirm)
    {
        ConfirmText = confirm ? "Confirmed \u2611" : "Confirmed \u2610";
    }

    public bool Confirm(bool? force = null)
    {
        if (CanConfirm)
        {
            confirmed = force ?? !confirmed;
            BgColors[flag.DataName].Color = GetBackgroundColor(confirmed).Color;
            confirmeds[flag.DataName] = confirmed;
            SetConfirmText(confirmed);
            if (confirmed)
            {
                generator.mgr.Replace(flag);
                FlagCache.Apply(flag);
            }
            UpdateFilter();
            return true;
        }
        BgColors[flag.DataName].Color = GetBackgroundColor(false).Color;
        SetConfirmText(false);
        UpdateFilter();
        return false;
    }

    public void Save()
    {
        if (Helpers.RootDir != null)
        {
            string bootupPath = Helpers.GetFullModPath("Pack/Bootup.pack");
            if (bootupPath == string.Empty)
            {
                bootupPath = Path.Combine(
                    Helpers.RootDir,
                    Helpers.ModEndianness == Revrs.Endianness.Big ? "content" :
                        "01007EF00011E800/romfs",
                    "Pack/Bootup.pack"
                );
                File.Copy(Helpers.GetFullStockPath("Pack/Bootup.pack"), bootupPath);
            }
            FlagMgr compiled = FlagMgr.Open(bootupPath);
            compiled.Merge(generator.mgr);
            compiled.Write(bootupPath);
            NeedsSave = false;
        }
    }

    // Returns new brushes because each needs its own brush so the user confirmation can change
    public static SolidColorBrush GetBackgroundColor(bool confirmed)
    {
        return confirmed ? new SolidColorBrush(0xFF98FB98) : new SolidColorBrush(0x00FFFFFF);
    }

    // Returns references to existing brushes because confidence will never change
    public static SolidColorBrush GetForegroundColor(GeneratorConfidence confidence)
    {
        return confidence switch
        {
            GeneratorConfidence.Bad => Brushes["FgRed"],
            GeneratorConfidence.Mediocre => Brushes["FgYellow"],
            GeneratorConfidence.Good => Brushes["FgGreen"],
            GeneratorConfidence.Definite => Brushes["FgBlue"],
            _ => Brushes["BgNone"],
        };
    }
}
