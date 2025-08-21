using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FFGUITool.Models
{
    public class ProcessingTask : INotifyPropertyChanged
    {
        private TaskStatus _status = TaskStatus.Pending;
        private double _progress;
        private string _statusMessage = "";

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string InputFile { get; set; } = "";
        public string OutputFile { get; set; } = "";
        public ProcessingOptions Options { get; set; } = new();
        
        public TaskStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public double Progress
        {
            get => _progress;
            set
            {
                if (Math.Abs(_progress - value) > 0.01)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? ErrorMessage { get; set; }
        
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum TaskStatus
    {
        Pending,
        Queued,
        Processing,
        Completed,
        Failed,
        Cancelled
    }
}