namespace FFGUITool.Models
{
    /// <summary>
    /// 编码器选项
    /// </summary>
    public class CodecOption
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Description { get; set; } = "";

        public CodecOption() { }

        public CodecOption(string name, string value, string description)
        {
            Name = name;
            Value = value;
            Description = description;
        }

        public override string ToString() => $"{Name} - {Description}";
    }
}