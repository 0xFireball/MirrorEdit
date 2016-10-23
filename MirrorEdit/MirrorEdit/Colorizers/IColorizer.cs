using Avalonia.Media;

namespace MirrorEdit.Colorizers
{
    public interface IColorizer
    {
        int StartIndex { get; }
        int StopIndex { get; }
        Color Color { get; }
    }
}