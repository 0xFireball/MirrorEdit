using Avalonia.Controls;
using Avalonia.Styling;
using System;

namespace MirrorEdit
{
    public class MirrorEditor : TextBox, IStyleable
    {
        Type IStyleable.StyleKey => typeof(MirrorEditor);
    }
}