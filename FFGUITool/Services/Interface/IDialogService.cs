using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace FFGUITool.Services
{
    /// <summary>
    /// 对话框服务接口
    /// </summary>
    public interface IDialogService
    {
        Task<string?> ShowMessage(string title, string message);
        Task<bool> ShowConfirmation(string title, string message);
        Task<IStorageFile?> OpenFileDialog(string title, FilePickerFileType[]? filters = null);
        Task<IStorageFolder?> OpenFolderDialog(string title);
        Window? GetMainWindow();
    }
}