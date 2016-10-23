using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using MirrorEdit.Controls;
using MirrorEdit.Util;
using System;
using System.Linq;

namespace MirrorEdit
{
    public class MirrorEditor : METextBox, IStyleable
    {
        private TimedRunner colorizerWorker;
        private ColorizerService colorizer;

        public MirrorEditor()
        {
            colorizer = new ColorizerService(this);
            //TODO: Use a property
            colorizerWorker = new TimedRunner(1000, colorizer.Run);
        }

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
                    e.Handled = true; //prevent losing focus
                    if (e.Modifiers == InputModifiers.Shift)
                    {
                        if (Text == null || Text.Length < TabSize)
                        {
                            break;
                        }
                        //Remove tab
                        CaretIndex -= TabSize; //Go back [tabsize] characters
                        //Eat the [tabsize] characters after
                        Text = Text.Remove(CaretIndex, TabSize);
                    }
                    else
                    {
                        //Insert tab
                        Text = Text?.Insert(CaretIndex, new string(' ', TabSize));
                        if (Text == null) //In case the control was new
                        {
                            Text = new string(' ', TabSize);
                        }
                        CaretIndex += TabSize;
                    }
                    break;

                case Key.Enter:
                    //Restore indentation
                    //Read previous indent
                    var lines = Text?.Split('\n').Select(s => s += "\n").ToArray(); //Re-add the split newline
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

        public override void Render(DrawingContext context)
        {
            base.Render(context);
        }
    }
}