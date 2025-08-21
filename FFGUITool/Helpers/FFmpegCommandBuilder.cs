using System.Collections.Generic;
using System.Text;

namespace FFGUITool.Helpers
{
    public class FFmpegCommandBuilder
    {
        private readonly List<string> _inputOptions = new();
        private readonly List<string> _outputOptions = new();
        private string _input = "";
        private string _output = "";

        public FFmpegCommandBuilder AddInput(string input)
        {
            _input = $"-i \"{input}\"";
            return this;
        }

        public FFmpegCommandBuilder AddInputOption(string option)
        {
            _inputOptions.Add(option);
            return this;
        }

        public FFmpegCommandBuilder SetVideoCodec(string codec)
        {
            _outputOptions.Add($"-c:v {codec}");
            return this;
        }

        public FFmpegCommandBuilder SetAudioCodec(string codec)
        {
            _outputOptions.Add($"-c:a {codec}");
            return this;
        }

        public FFmpegCommandBuilder SetVideoBitrate(int bitrate)
        {
            _outputOptions.Add($"-b:v {bitrate}k");
            return this;
        }

        public FFmpegCommandBuilder SetAudioBitrate(int bitrate)
        {
            _outputOptions.Add($"-b:a {bitrate}k");
            return this;
        }

        public FFmpegCommandBuilder SetScale(string resolution)
        {
            _outputOptions.Add($"-vf scale={resolution}");
            return this;
        }

        public FFmpegCommandBuilder SetFrameRate(double fps)
        {
            _outputOptions.Add($"-r {fps}");
            return this;
        }

        public FFmpegCommandBuilder SetSampleRate(int sampleRate)
        {
            _outputOptions.Add($"-ar {sampleRate}");
            return this;
        }

        public FFmpegCommandBuilder SetChannels(int channels)
        {
            _outputOptions.Add($"-ac {channels}");
            return this;
        }

        public FFmpegCommandBuilder AddParameter(string key, string value)
        {
            _outputOptions.Add($"{key} {value}");
            return this;
        }

        public FFmpegCommandBuilder SetOutput(string output)
        {
            _output = $"\"{output}\"";
            return this;
        }

        public string Build()
        {
            var command = new StringBuilder();
            
            // Add input options
            if (_inputOptions.Count > 0)
            {
                command.Append(string.Join(" ", _inputOptions));
                command.Append(" ");
            }

            // Add input
            command.Append(_input);
            command.Append(" ");

            // Add output options
            if (_outputOptions.Count > 0)
            {
                command.Append(string.Join(" ", _outputOptions));
                command.Append(" ");
            }

            // Add output
            command.Append(_output);

            return command.ToString().Trim();
        }
    }
}
