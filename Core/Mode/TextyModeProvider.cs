using Texty.Core.Configuration;

namespace Texty.Core.Mode;

public static class TextyModeProvider
{
    public static ITextyMode Get(TextyMode mode) => mode switch
    {
        TextyMode.Edge => new EdgeMode(),
        TextyMode.Detail => new DetailMode(),
        TextyMode.Dither => new DitherMode(),
        TextyMode.Block => new BlockMode(),
        _ => new DefaultTextyMode(),
    };
}
