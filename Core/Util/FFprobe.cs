using System.Diagnostics;
using Texty.Core.Configuration;

namespace Texty.Core.Util;

public static class FFprobe
{
    public static Process Create(TextyConfig config)
    {
        var args = BuildArgs(config);

        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
    }

    private static string BuildArgs(TextyConfig config)
    {
        return
            "-v error -select_streams v:0 " +
            "-show_entries stream=width,height:format=duration " +
            "-of default=noprint_wrappers=1 " +
            $"\"{config.Input}\"";
    }
}
