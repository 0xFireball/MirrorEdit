using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Styling;
using System;
using System.Linq;

namespace MirrorEdit
{
    public class MirrorEditor : TextBox, IStyleable
    {
        public static readonly AvaloniaProperty<int> TabSizeProperty =
            AvaloniaProperty.Register<MirrorEditor, int>(nameof(TabSize));

        public int TabSize
        {
            get { return GetValue(TabSizeProperty); }
            set { SetValue(TabSizeProperty, value); }
        }

        Type IStyleable.StyleKey => typeof(MirrorEditor);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                    e.Handled = true;
                    //Insert tab
                    Text = Text?.Insert(CaretIndex, new string(' ', TabSize));
                    if (Text == null) //In case the control was new
                    {
                        Text = new string(' ', TabSize);
                    }
                    CaretIndex += TabSize;
                    e.Handled = true;
                    break;

                case Key.Enter:
                    //Restore indentation
                    //Read previous indent
                    var lines = Text?.Split('\n').Select(s=>s+="\n").ToArray(); //Re-add the split newline
                    int previousIndent = 0;
                    if (lines == null)
                    {
                        previousIndent = 0;
                    }
                    else
                    {
                        //Calculate the indentation of the previous line
                        int lineChars = 0; //chars so far
                        int currentLineIndex = 0;
                        for (int i = 0; i < lines.Length; i++)
                        {
                            lineChars += lines[i].Length;
                            if (lineChars >= CaretIndex)
                            {
                                //Found the current line
                                currentLineIndex = i;
                                break;
                            }
                        }
                        string currentLine = lines[currentLineIndex];
                        for (int i = 0; i < currentLine.Length; i++)
                        {
                            if (currentLine[i] == ' ') //space
                            {
                                previousIndent++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    Text = Text?.Insert(CaretIndex, $"\n{new string(' ', previousIndent)}");
                    CaretIndex += previousIndent + 1; //Go to the end of the newly added indent
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            //

            base.OnTemplateApplied(e);
        }
    }
}