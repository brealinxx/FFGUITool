namespace FFGUITool.Models
{
    /// <summary>
    /// Compression preset option.
    /// </summary>
    public class CompressionPresetOption
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public double TargetSizeMB { get; set; }
        public string Description { get; set; } = "";
        public string Codec { get; set; } = "libx264";
        public bool UseCrf { get; set; }
        public int Crf { get; set; } = 23;
        public int MinVideoBitrateKbps { get; set; }
        public int MaxVideoBitrateKbps { get; set; }
        public int AudioBitrateKbps { get; set; } = 96;
        public int MaxHeight { get; set; }
        public int MaxFramerate { get; set; }

        public CompressionPresetOption() { }

        public CompressionPresetOption(string name, string value, double targetSizeMB, string description)
        {
            Name = name;
            Value = value;
            TargetSizeMB = targetSizeMB;
            Description = description;
        }

        public override string ToString() => $"{Name} - {Description}";
    }
}
