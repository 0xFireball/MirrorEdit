using MirrorEdit.Colorizers;
using System.Collections.Generic;

namespace MirrorEdit
{
    internal class ColorizerService
    {
        private MirrorEditor mirrorEditor;
        public List<IColorizer> Colorizers { get; } = new List<IColorizer>();

        public ColorizerService(MirrorEditor mirrorEditor)
        {
            this.mirrorEditor = mirrorEditor;
        }

        internal void Run()
        {
            //Run the colorizers
            foreach (var colorizer in Colorizers)
            {
                ApplyColorizer(colorizer);
            }
        }

        private void ApplyColorizer(IColorizer colorizer)
        {
            mirrorEditor.SelectionStart = colorizer.StartIndex;
            mirrorEditor.SelectionEnd = colorizer.EndIndex;
        }
    }
}