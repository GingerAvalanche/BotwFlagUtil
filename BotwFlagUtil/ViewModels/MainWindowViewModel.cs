using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls.Selection;
using BotwFlagUtil.Enums;
using BotwFlagUtil.GameData;
using BotwFlagUtil.GameData.Util;
using ReactiveUI;

namespace BotwFlagUtil.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private string title = "BotwFlagUtil";
    private readonly Generator generator = new();

    // Flag list
    public readonly Dictionary<string, GeneratorConfidence> confidences = [];
    public readonly Dictionary<string, bool> confirmeds = [];
    private List<string> flagNames = [];
    private FlagStringType stringType = FlagStringType.None;
    
    public string Title
    {
        get => title;
        set => this.RaiseAndSetIfChanged(ref title, value);
    }
    public SelectionModel<string> FlagNameSelection { get; }
    public bool NeedsSave { get; private set; }

#region Current Flag Fields
        private string currentFlagName = string.Empty;
        private Flag flag = default;
        private FlagUnionType flagType = FlagUnionType.None;
        private string initValue = string.Empty;
        private string maxValue = string.Empty;
        private string minValue = string.Empty;
        private bool canConfirm = true;
        private bool canConfirmInit = true;
        private bool canConfirmMax = true;
        private bool canConfirmMin = true;
#endregion

#region Current Flag Properties

    public List<string> FlagNames
    {
        get => flagNames;
        set => this.RaiseAndSetIfChanged(ref flagNames, value);
    }
    public bool IsFlagLoaded
    {
        get => flag.HashValue != 0;
    }
    public bool UseCategory
    {
        get => flag.Category != null;
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
                CanConfirmMax = false;
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
        }
    }
    public bool IsOneTrigger
    {
        get => flag.IsOneTrigger;
        set
        {
            flag.IsOneTrigger = value;
            this.RaisePropertyChanged(nameof(IsOneTrigger));
        }
    }
    public bool IsProgramReadable
    {
        get => flag.IsProgramReadable;
        set
        {
            flag.IsProgramReadable = value;
            this.RaisePropertyChanged(nameof(IsProgramReadable));
        }
    }
    public bool IsProgramWritable
    {
        get => flag.IsProgramWritable;
        set
        {
            flag.IsProgramWritable = value;
            this.RaisePropertyChanged(nameof(IsProgramWritable));
        }
    }
    public bool IsSave
    {
        get => flag.IsSave;
        set
        {
            flag.IsSave = value;
            this.RaisePropertyChanged(nameof(IsSave));
        }
    }
    public string[] ResetTypes
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
    #endregion

    public MainWindowViewModel()
    {
        FlagNameSelection = new();
        FlagNameSelection.SelectionChanged += OnFlagNameSelected;
    }

    public void OnFlagNameSelected(object? sender, SelectionModelSelectionChangedEventArgs e)
    {
        if (!CanConfirm)
        {
            FlagNameSelection.SelectedItem = currentFlagName;
        }
        else if (e.SelectedItems.Single() is string nextFlagName &&
            nextFlagName != currentFlagName && 
            generator.mgr.TryRetrieve(
                nextFlagName, out Flag newFlag, out FlagUnionType fType, out FlagStringType sType
            ))
        {
            if (flag.HashValue != 0)
            {
                generator.mgr.Add(flag, stringType);
            }
            Flag = newFlag;
            flagType = fType;
            stringType = sType;
            currentFlagName = nextFlagName;
            initValue = newFlag.InitValue.ToString();
            maxValue = newFlag.MaxValue.ToString();
            minValue = newFlag.MinValue.ToString();

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
            Directory.Exists(Path.Combine(rootDir, "01007EF00011E800", "romfs")) ||
            Directory.Exists(Path.Combine(rootDir, "01007EF00011F001", "romfs"))))
        {
            return;
        }
        Title = $"BotwFlagUtil - {Path.GetFileName(rootDir)}";
        Helpers.RootDir = rootDir;

        generator.ReplaceManager(new());
        generator.GenerateActorFlags();
        generator.GenerateMapFlags();
        generator.GenerateEventFlags();
        generator.FinalizeGeneration();

        IEnumerable<Flag> flags = generator.mgr.GetAllFlags();
        List<string> flagNamesTemp = new(flags.Count());
        confidences.Clear();
        confidences.EnsureCapacity(flagNamesTemp.Capacity);
        confirmeds.Clear();
        confirmeds.EnsureCapacity(flagNamesTemp.Capacity);
        foreach (Flag flag in flags)
        {
            flagNamesTemp.Add(flag.DataName);
            confidences[flag.DataName] = generator.flagConfidence[flag.HashValue];
            confirmeds[flag.DataName] = false;
        }
        flagNamesTemp.Sort();
        FlagNames = flagNamesTemp;
        NeedsSave = true;
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
                    confidences.Clear();
                    confidences.EnsureCapacity(flagNamesTemp.Capacity);
                    confirmeds.Clear();
                    confirmeds.EnsureCapacity(flagNamesTemp.Capacity);
                    foreach (Flag flag in flags)
                    {
                        flagNamesTemp.Add(flag.DataName);
                        confidences[flag.DataName] = generator.flagConfidence[flag.HashValue];
                        confirmeds[flag.DataName] = false;
                    }
                    flagNamesTemp.Sort();
                    FlagNames = flagNamesTemp;
                }
            }
        }
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
            generator.mgr.Add(flag);
            generator.mgr.Write(bootupPath);
            generator.mgr.Remove(flag.DataName);
            NeedsSave = false;
        }
    }
}
