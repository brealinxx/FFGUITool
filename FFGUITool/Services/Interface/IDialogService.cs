using System.Threading.Tasks;
using System.Collections.Generic;
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
        Task<string?> ShowScrollableMessage(string title, string message);
        Task<string?> ShowActionMessage(string title, string message, IReadOnlyList<(string Id, string Text)> actions);
        Task<bool> ShowConfirmation(string title, string message);
        Task<IStorageFile?> OpenFileDialog(string title, FilePickerFileType[]? filters = null);
        Task<IReadOnlyList<IStorageFile>> OpenFilesDialog(string title, FilePickerFileType[]? filters = null);
        Task<IStorageFolder?> OpenFolderDialog(string title);
        Window? GetMainWindow();
    }
}
