using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MirrorEdit.Controls;
using System;
using System.Reactive.Linq;

namespace MirrorEdit.Presenters
{
    public class METextPresenter : METextBlock
    {
        public static readonly DirectProperty<METextPresenter, int> CaretIndexProperty =
            TextBox.CaretIndexProperty.AddOwner<METextPresenter>(
                o => o.CaretIndex,
                (o, v) => o.CaretIndex = v);

        public static readonly DirectProperty<METextPresenter, int> SelectionStartProperty =
            TextBox.SelectionStartProperty.AddOwner<METextPresenter>(
                o => o.SelectionStart,
                (o, v) => o.SelectionStart = v);

        public static readonly DirectProperty<METextPresenter, int> SelectionEndProperty =
            TextBox.SelectionEndProperty.AddOwner<METextPresenter>(
                o => o.SelectionEnd,
                (o, v) => o.SelectionEnd = v);

        private readonly DispatcherTimer _caretTimer;
        private int _caretIndex;
        private int _selectionStart;
        private int _selectionEnd;
        private bool _caretBlink;
        private IBrush _highlightBrush;

        public METextPresenter()
        {
            _caretTimer = new DispatcherTimer();
            _caretTimer.Interval = TimeSpan.FromMilliseconds(500);
            _caretTimer.Tick += CaretTimerTick;

            Observable.Merge(
                this.GetObservable(SelectionStartProperty),
                this.GetObservable(SelectionEndProperty))
                .Subscribe(_ => InvalidateFormattedText());

            this.GetObservable(CaretIndexProperty)
                .Subscribe(CaretIndexChanged);
        }

        public int CaretIndex
        {
            get
            {
                return _caretIndex;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(CaretIndexProperty, ref _caretIndex, value);
            }
        }

        public int SelectionStart
        {
            get
            {
                return _selectionStart;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(SelectionStartProperty, ref _selectionStart, value);
            }
        }

        public int SelectionEnd
        {
            get
            {
                return _selectionEnd;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(SelectionEndProperty, ref _selectionEnd, value);
            }
        }

        public int GetCaretIndex(Point point)
        {
            var hit = FormattedText.HitTestPoint(point);
            return hit.TextPosition + (hit.IsTrailing ? 1 : 0);
        }

        public override void Render(DrawingContext context)
        {
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;

            if (selectionStart != selectionEnd)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;

                // issue #600: set constaint before any FormattedText manipulation
                //             see base.Render(...) implementation
                FormattedText.Constraint = Bounds.Size;

                var rects = FormattedText.HitTestTextRange(start, length);

                if (_highlightBrush == null)
                {
                    _highlightBrush = (IBrush)this.FindStyleResource("HighlightBrush");
                }

                foreach (var rect in rects)
                {
                    context.FillRectangle(_highlightBrush, rect);
                }
            }

            base.Render(context);

            if (selectionStart == selectionEnd)
            {
                var backgroundColor = (((Control)TemplatedParent).GetValue(BackgroundProperty) as SolidColorBrush)?.Color;
                var caretBrush = Brushes.Black;

                if (backgroundColor.HasValue)
                {
                    byte red = (byte)~(backgroundColor.Value.R);
                    byte green = (byte)~(backgroundColor.Value.G);
                    byte blue = (byte)~(backgroundColor.Value.B);

                    caretBrush = new SolidColorBrush(Color.FromRgb(red, green, blue));
                }

                if (_caretBlink)
                {
                    var charPos = FormattedText.HitTestTextPosition(CaretIndex);
                    var x = Math.Floor(charPos.X) + 0.5;
                    var y = Math.Floor(charPos.Y) + 0.5;
                    var b = Math.Ceiling(charPos.Bottom) - 0.5;

                    context.DrawLine(
                        new Pen(caretBrush, 1),
                        new Point(x, y),
                        new Point(x, b));
                }
            }
        }

        public void ShowCaret()
        {
            _caretBlink = true;
            _caretTimer.Start();
            InvalidateVisual();
        }

        public void HideCaret()
        {
            _caretBlink = false;
            _caretTimer.Stop();
            InvalidateVisual();
        }

        internal void CaretIndexChanged(int caretIndex)
        {
            if (this.GetVisualParent() != null)
            {
                _caretBlink = true;
                _caretTimer.Stop();
                _caretTimer.Start();
                InvalidateVisual();

                if (IsMeasureValid)
                {
                    var rect = FormattedText.HitTestTextPosition(caretIndex);
                    this.BringIntoView(rect);
                }
                else
                {
                    // The measure is currently invalid so there's no point trying to bring the
                    // current char into view until a measure has been carried out as the scroll
                    // viewer extents may not be up-to-date.
                    Dispatcher.UIThread.InvokeAsync(
                        () =>
                        {
                            var rect = FormattedText.HitTestTextPosition(caretIndex);
                            this.BringIntoView(rect);
                        },
                        DispatcherPriority.Normal);
                }
            }
        }

        protected override FormattedText CreateFormattedText(Size constraint)
        {
            var result = base.CreateFormattedText(constraint);
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var length = Math.Max(selectionStart, selectionEnd) - start;

            if (length > 0)
            {
                result.SetForegroundBrush(Brushes.White, start, length);
            }

            return result;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var text = Text;

            if (!string.IsNullOrEmpty(text))
            {
                return base.MeasureOverride(availableSize);
            }
            else
            {
                // TODO: Pretty sure that measuring "X" isn't the right way to do this...
                using (var formattedText = new FormattedText(
                    "X",
                    FontFamily,
                    FontSize,
                    FontStyle,
                    TextAlignment,
                    FontWeight))
                {
                    return formattedText.Measure();
                }
            }
        }

        private int CoerceCaretIndex(int value)
        {
            var text = Text;
            var length = text?.Length ?? 0;
            return Math.Max(0, Math.Min(length, value));
        }

        private void CaretTimerTick(object sender, EventArgs e)
        {
            _caretBlink = !_caretBlink;
            InvalidateVisual();
        }
    }
}