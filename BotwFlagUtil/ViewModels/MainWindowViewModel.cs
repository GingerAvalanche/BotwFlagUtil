using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls.Selection;
using Avalonia.Media;
using BotwFlagUtil.Models;
using BotwFlagUtil.Models.Cache;
using BotwFlagUtil.Models.Enums;
using BotwFlagUtil.Models.GameData;
using BotwFlagUtil.Models.GameData.Util;
using ReactiveUI;
using Revrs;

namespace BotwFlagUtil.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private static readonly Dictionary<string, SolidColorBrush> Brushes = new()
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

    private int filterMask = 0b1111110;
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

    private Flag flag;
    private FlagUnionType flagType = FlagUnionType.None;
    private string initValue = string.Empty;
    private string maxValue = string.Empty;
    private string minValue = string.Empty;
    private bool canConfirm = true;
    private bool canConfirmInit = true;
    private bool canConfirmMax = true;
    private bool canConfirmMin = true;
    private bool confirmed;
    private string confirmText = "Confirmed \u2610";
    #endregion

    #region Current Flag Properties
    public bool IsFlagLoaded => flag.HashValue != 0;

    public string Category =>
        flag.Category == null
            ? FlagCategory.None.ToString()
            : ((FlagCategory)flag.Category).ToString();

    public string FlagType
    {
        get => flagType.ToString();
        set => this.RaiseAndSetIfChanged(
            ref flagType, (FlagUnionType)Enum.Parse(typeof(FlagUnionType), value)
        );
    }

    private FlagUnionType InitValueType => Helpers.MainTypeToInitType[flagType];

    public string InitValue
    {
        get => initValue;
        set
        {
            this.RaiseAndSetIfChanged(ref initValue, value);
            ConfirmHelper_UpdateUINoFilter(false);
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
        private set => this.RaiseAndSetIfChanged(ref flag, value);
    }
    private FlagUnionType BoundingValueType => Helpers.MainTypeToMaxOrMinType[flagType];

    public string MaxValue
    {
        get => maxValue;
        set
        {
            this.RaiseAndSetIfChanged(ref maxValue, value);
            ConfirmHelper_UpdateUINoFilter(false);
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
            ConfirmHelper_UpdateUINoFilter(false);
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
            this.RaisePropertyChanged();
            ConfirmHelper_UpdateUINoFilter(false);
        }
    }
    
    public bool IsOneTrigger
    {
        get => flag.IsOneTrigger;
        set
        {
            flag.IsOneTrigger = value;
            this.RaisePropertyChanged();
            ConfirmHelper_UpdateUINoFilter(false);
        }
    }
    
    public bool IsProgramReadable
    {
        get => flag.IsProgramReadable;
        set
        {
            flag.IsProgramReadable = value;
            this.RaisePropertyChanged();
            ConfirmHelper_UpdateUINoFilter(false);
        }
    }
    
    public bool IsProgramWritable
    {
        get => flag.IsProgramWritable;
        set
        {
            flag.IsProgramWritable = value;
            this.RaisePropertyChanged();
            ConfirmHelper_UpdateUINoFilter(false);
        }
    }
    
    public bool IsSave
    {
        get => flag.IsSave;
        set
        {
            flag.IsSave = value;
            this.RaisePropertyChanged();
            ConfirmHelper_UpdateUINoFilter(false);
        }
    }
    
    public static string[] ResetTypes =>
    [
        "Manual reset",
        "Reset on blood moon",
        "Reset on loading screen",
        "Reset at midnight",
        "Reset when Lord of the Mountain appears"
    ];

    public int ResetType
    {
        get => flag.ResetType;
        set
        {
            flag.ResetType = value;
            this.RaisePropertyChanged();
            ConfirmHelper_UpdateUINoFilter(false);
        }
    }

    private bool CanConfirm
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

    public string ConfirmText
    {
        // ReSharper disable once UnusedMember.Local
        get => confirmText;
        set => this.RaiseAndSetIfChanged(ref confirmText, value);
    }

    #endregion

    public MainWindowViewModel()
    {
        FlagNameSelection = new();
        FlagNameSelection.SelectionChanged += OnFlagNameSelected;
    }

    private void OnFlagNameSelected(object? sender, SelectionModelSelectionChangedEventArgs e)
    {
        if (e.SelectedItems.Count > 0 &&
            e.SelectedItems.Single() is string nextFlagName &&
            nextFlagName != flag.DataName && 
            generator.Mgr.TryGet(
                nextFlagName, out Flag newFlag, out FlagUnionType fType, out FlagStringType sType
            ))
        {
            // Reset the flag background color to the stored confirmation status
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
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
            this.RaisePropertyChanged(nameof(Category));
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
        Helpers.rootDir = rootDir;

        generator.ReplaceManager(new());
        generator.GenerateActorFlags();
        generator.GenerateLevelSensorFlags();
        generator.GenerateQuestFlags();
        generator.GenerateMapFlags();
        generator.GenerateEventFlags(); // Last because least certain
        generator.FinalizeGeneration();

        List<string> flagNamesTemp = new(generator.Mgr.GetFlagCount());
        FgColors.Clear();
        FgColors.EnsureCapacity(flagNamesTemp.Capacity);
        BgColors.Clear();
        BgColors.EnsureCapacity(flagNamesTemp.Capacity);
        confirmeds.Clear();
        confirmeds.EnsureCapacity(flagNamesTemp.Capacity);
        foreach (Flag f in generator.Mgr.GetAllFlags())
        {
            flagNamesTemp.Add(f.DataName);
            FgColors[f.DataName] = GetForegroundColor(generator.FlagConfidence[f.HashValue]);
            BgColors[f.DataName] = GetBackgroundColor(false);
            confirmeds[f.DataName] = false;
        }
        flagNamesTemp.Sort();
        allFlagNames = flagNamesTemp;
        FlagNames = flagNamesTemp.Where(FlagListFilter).ToList();

        FlagCache.Init(modName, generator.Mgr);
        foreach (Flag recalled in FlagCache.RecallAll())
        {
            generator.Mgr.Replace(recalled);
            BgColors[recalled.DataName] = GetBackgroundColor(true);
            confirmeds[recalled.DataName] = true;
        }
        NeedsSave = true;
        UpdateFilter();
    }

    public void Export()
    {
        if (Helpers.rootDir == null)
        {
            return;
        }

        using FileStream stream = File.Open(Path.Combine(Helpers.rootDir, "flags.json"), FileMode.Create);
        JsonSerializer.Serialize(stream, generator.Mgr, Helpers.JsOpt);
    }

    public void Import()
    {
        if (Helpers.rootDir != null)
        {
            string flagPath = Path.Combine(Helpers.rootDir, "flags.json");
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
                    List<string> flagNamesTemp = new(generator.Mgr.GetFlagCount());
                    FgColors.Clear();
                    FgColors.EnsureCapacity(flagNamesTemp.Capacity);
                    BgColors.Clear();
                    BgColors.EnsureCapacity(flagNamesTemp.Capacity);
                    confirmeds.Clear();
                    confirmeds.EnsureCapacity(flagNamesTemp.Capacity);
                    foreach (Flag f in generator.Mgr.GetAllFlags())
                    {
                        flagNamesTemp.Add(f.DataName);
                        FgColors[f.DataName] = GetForegroundColor(generator.FlagConfidence[f.HashValue]);
                        BgColors[f.DataName] = GetBackgroundColor(false);
                        confirmeds[f.DataName] = false;
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
        return Convert.ToBoolean((filterMask >> (int)generator.FlagConfidence[name]) & 1) &&
        // Mask, obtain confirmed bit by right-shifting by confirmed status (Plus 4 to bypass confidence bits) AND 1
            Convert.ToBoolean((filterMask >> (Convert.ToInt32(confirmeds[name]) + 5)) & 1);
    }

    public bool Confirm(bool? force = null)
    {
        if (CanConfirm)
        {
            // Can't simply toggle confirmed because the UI will desync with it
            // so check what the UI says
            confirmed = force ?? confirmText == "Confirmed \u2610";
            ConfirmHelper_UpdateUINoFilter(confirmed);
            ConfirmHelper_CommitChange();
            UpdateFilter();
            return true;
        }
        ConfirmHelper_UpdateUINoFilter(false);
        UpdateFilter();
        return false;
    }

    private void ConfirmHelper_UpdateUINoFilter(bool confirm)
    {
        BgColors[flag.DataName].Color = GetBackgroundColor(confirm).Color;
        SetConfirmText(confirm);
    }

    private void SetConfirmText(bool confirm)
    {
        ConfirmText = confirm ? "Confirmed \u2611" : "Confirmed \u2610";
    }

    private void ConfirmHelper_CommitChange()
    {
        confirmeds[flag.DataName] = confirmed;
        if (confirmed)
        {
            generator.Mgr.Replace(flag);
            FlagCache.Apply(flag);
        }
    }

    public void Save()
    {
        if (Helpers.rootDir != null)
        {
            string bootupPath = Helpers.GetFullModPath("Pack/Bootup.pack");
            if (bootupPath == string.Empty)
            {
                bootupPath = Path.Combine(
                    Helpers.rootDir,
                    Helpers.ModEndianness == Endianness.Big ? "content" :
                        "01007EF00011E800/romfs",
                    "Pack/Bootup.pack"
                );
                File.Copy(Helpers.GetFullStockPath("Pack/Bootup.pack"), bootupPath);
            }
            FlagMgr compiled = FlagMgr.Open(bootupPath);
            compiled.Merge(generator.Mgr);
            compiled.Write(bootupPath);
            NeedsSave = false;
        }
    }

    // Returns new brushes because each needs its own brush so the user confirmation can change
    private static SolidColorBrush GetBackgroundColor(bool confirmed)
    {
        return confirmed ? new SolidColorBrush(0xFF98FB98) : new SolidColorBrush(0x00FFFFFF);
    }

    // Returns references to existing brushes because confidence will never change
    private static SolidColorBrush GetForegroundColor(GeneratorConfidence confidence)
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
