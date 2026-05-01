using Texty.Configuration;

namespace Texty.Mode;

public static class TextyModeProvider
{
    public static ITextyMode Get(TextyMode mode) => mode switch
    {
        TextyMode.Edge => new EdgeMode(),
        TextyMode.Detail => new DetailMode(),
        TextyMode.Dither => new DitherMode(),
        _ => new DefaultTextyMode(),
    };
}
