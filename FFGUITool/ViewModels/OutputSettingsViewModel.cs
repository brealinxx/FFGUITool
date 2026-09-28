using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FFGUITool.ViewModels;

public partial class OutputSettingsViewModel(IRelayCommand selectFolderCommand) : ObservableObject
{
    [ObservableProperty] private string _outputPathText = "";
    [ObservableProperty] private string _outputNamePattern = "{name}_FFGUIToolOutPut_{label}";
    [ObservableProperty] private bool _preserveFolderStructure = true;
    [ObservableProperty] private bool _isBatchMode;
    [ObservableProperty] private bool _isProcessing;
    public IRelayCommand SelectFolderCommand { get; } = selectFolderCommand;
}
