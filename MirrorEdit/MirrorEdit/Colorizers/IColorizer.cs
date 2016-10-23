using Avalonia.Media;

namespace MirrorEdit.Colorizers
{
    public interface IColorizer
    {
        int StartIndex { get; }
        int EndIndex { get; }
        Color Color { get; }
    }
}