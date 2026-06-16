using CommunityToolkit.Mvvm.ComponentModel;

namespace FFGUITool.Models
{
    public partial class BatchTaskItem : ObservableObject
    {
        public BatchTaskItem(string inputPath)
        {
            InputPath = inputPath;
            FileName = System.IO.Path.GetFileName(inputPath);
        }

        public string InputPath { get; }
        public string FileName { get; }

        [ObservableProperty]
        private string _status = "";

        [ObservableProperty]
        private string _outputPath = "";

        [ObservableProperty]
        private string _message = "";
    }
}
