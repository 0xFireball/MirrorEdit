﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using MirrorEdit.Controls.Utils;
using MirrorEdit.Presenters;
using MirrorEdit.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace MirrorEdit.Controls
{
    public class METextBox : TemplatedControl, UndoRedoHelper<METextBox.UndoRedoState>.IUndoRedoHost
    {
        public static readonly StyledProperty<bool> AcceptsReturnProperty =
            AvaloniaProperty.Register<METextBox, bool>("AcceptsReturn");

        public static readonly StyledProperty<bool> AcceptsTabProperty =
            AvaloniaProperty.Register<METextBox, bool>("AcceptsTab");

        public static readonly DirectProperty<METextBox, bool> CanScrollHorizontallyProperty =
            AvaloniaProperty.RegisterDirect<METextBox, bool>("CanScrollHorizontally", o => o.CanScrollHorizontally);

        public static readonly DirectProperty<METextBox, int> CaretIndexProperty =
            AvaloniaProperty.RegisterDirect<METextBox, int>(
                nameof(CaretIndex),
                o => o.CaretIndex,
                (o, v) => o.CaretIndex = v);

        public static readonly DirectProperty<METextBox, IEnumerable<Exception>> DataValidationErrorsProperty =
            AvaloniaProperty.RegisterDirect<METextBox, IEnumerable<Exception>>(
                nameof(DataValidationErrors),
                o => o.DataValidationErrors);

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<METextBox, bool>(nameof(IsReadOnly));

        public static readonly DirectProperty<METextBox, int> SelectionStartProperty =
            AvaloniaProperty.RegisterDirect<METextBox, int>(
                nameof(SelectionStart),
                o => o.SelectionStart,
                (o, v) => o.SelectionStart = v);

        public static readonly DirectProperty<METextBox, int> SelectionEndProperty =
            AvaloniaProperty.RegisterDirect<METextBox, int>(
                nameof(SelectionEnd),
                o => o.SelectionEnd,
                (o, v) => o.SelectionEnd = v);

        public static readonly DirectProperty<METextBox, string> TextProperty =
            METextBlock.TextProperty.AddOwner<METextBox>(
                o => o.Text,
                (o, v) => o.Text = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            METextBlock.TextAlignmentProperty.AddOwner<METextBox>();

        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            METextBlock.TextWrappingProperty.AddOwner<METextBox>();

        public static readonly StyledProperty<string> WatermarkProperty =
            AvaloniaProperty.Register<METextBox, string>("Watermark");

        public static readonly StyledProperty<bool> UseFloatingWatermarkProperty =
            AvaloniaProperty.Register<METextBox, bool>("UseFloatingWatermark");

        private struct UndoRedoState : IEquatable<UndoRedoState>
        {
            public string Text { get; }
            public int CaretPosition { get; }

            public UndoRedoState(string text, int caretPosition)
            {
                Text = text;
                CaretPosition = caretPosition;
            }

            public bool Equals(UndoRedoState other) => ReferenceEquals(Text, other.Text) || Equals(Text, other.Text);
        }

        private string _text;
        private int _caretIndex;
        private int _selectionStart;
        private int _selectionEnd;
        private bool _canScrollHorizontally;
        private METextPresenter _presenter;
        private UndoRedoHelper<UndoRedoState> _undoRedoHelper;
        private bool _ignoreTextChanges;
        private IEnumerable<Exception> _dataValidationErrors;

        static METextBox()
        {
            FocusableProperty.OverrideDefaultValue(typeof(METextBox), true);
        }

        public METextBox()
        {
            this.GetObservable(TextWrappingProperty)
                .Select(x => x == TextWrapping.NoWrap)
                .Subscribe(x => CanScrollHorizontally = x);

            var horizontalScrollBarVisibility = this.GetObservable(AcceptsReturnProperty)
                .Select(x => x ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden);

            Bind(
                ScrollViewer.HorizontalScrollBarVisibilityProperty,
                horizontalScrollBarVisibility,
                BindingPriority.Style);
            _undoRedoHelper = new UndoRedoHelper<UndoRedoState>(this);
        }

        public METextPresenter Presenter => _presenter;

        public bool AcceptsReturn
        {
            get { return GetValue(AcceptsReturnProperty); }
            set { SetValue(AcceptsReturnProperty, value); }
        }

        public bool AcceptsTab
        {
            get { return GetValue(AcceptsTabProperty); }
            set { SetValue(AcceptsTabProperty, value); }
        }

        public bool CanScrollHorizontally
        {
            get { return _canScrollHorizontally; }
            private set { SetAndRaise(CanScrollHorizontallyProperty, ref _canScrollHorizontally, value); }
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
                UndoRedoState state;
                if (_undoRedoHelper.TryGetLastState(out state) && state.Text == Text)
                    _undoRedoHelper.UpdateLastState();
            }
        }

        public IEnumerable<Exception> DataValidationErrors
        {
            get { return _dataValidationErrors; }
            private set { SetAndRaise(DataValidationErrorsProperty, ref _dataValidationErrors, value); }
        }

        public bool IsReadOnly
        {
            get { return GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
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

        [Content]
        public string Text
        {
            get { return _text; }
            set
            {
                if (!_ignoreTextChanges)
                {
                    SetAndRaise(TextProperty, ref _text, value);
                }
            }
        }

        public TextAlignment TextAlignment
        {
            get { return GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        public string Watermark
        {
            get { return GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        public bool UseFloatingWatermark
        {
            get { return GetValue(UseFloatingWatermarkProperty); }
            set { SetValue(UseFloatingWatermarkProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            _presenter = e.NameScope.Get<METextPresenter>("PART_METextPresenter");
            _presenter.Cursor = new Cursor(StandardCursorType.Ibeam);
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            // when navigating to a textbox via the tab key, select all text if
            //   1) this textbox is *not* a multiline textbox
            //   2) this textbox has any text to select
            if (e.NavigationMethod == NavigationMethod.Tab &&
                !AcceptsReturn &&
                Text?.Length > 0)
            {
                SelectionStart = 0;
                SelectionEnd = Text.Length;
            }
            else
            {
                _presenter.ShowCaret();
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            SelectionStart = 0;
            SelectionEnd = 0;
            _presenter.HideCaret();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            HandleTextInput(e.Text);
        }

        private void HandleTextInput(string input)
        {
            if (!IsReadOnly)
            {
                string text = Text ?? string.Empty;
                int caretIndex = CaretIndex;
                if (!string.IsNullOrEmpty(input))
                {
                    DeleteSelection();
                    caretIndex = CaretIndex;
                    text = Text ?? string.Empty;
                    SetTextInternal(text.Substring(0, caretIndex) + input + text.Substring(caretIndex));
                    CaretIndex += input.Length;
                    SelectionStart = SelectionEnd = CaretIndex;
                    _undoRedoHelper.DiscardRedo();
                }
            }
        }

        private async void Copy()
        {
            await ((IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard)))
                .SetTextAsync(GetSelection());
        }

        private async void Paste()
        {
            var text = await ((IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard))).GetTextAsync();
            if (text == null)
            {
                return;
            }
            _undoRedoHelper.Snapshot();
            HandleTextInput(text);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            string text = Text ?? string.Empty;
            int caretIndex = CaretIndex;
            bool movement = false;
            bool handled = true;
            var modifiers = e.Modifiers;

            switch (e.Key)
            {
                case Key.A:
                    if (modifiers == InputModifiers.Control)
                    {
                        SelectAll();
                    }

                    break;

                case Key.C:
                    if (modifiers == InputModifiers.Control)
                    {
                        Copy();
                    }
                    break;

                case Key.X:
                    if (modifiers == InputModifiers.Control)
                    {
                        Copy();
                        DeleteSelection();
                    }
                    break;

                case Key.V:
                    if (modifiers == InputModifiers.Control)
                    {
                        Paste();
                    }

                    break;

                case Key.Z:
                    if (modifiers == InputModifiers.Control)
                        _undoRedoHelper.Undo();

                    break;

                case Key.Y:
                    if (modifiers == InputModifiers.Control)
                        _undoRedoHelper.Redo();

                    break;

                case Key.Left:
                    MoveHorizontal(-1, modifiers);
                    movement = true;
                    break;

                case Key.Right:
                    MoveHorizontal(1, modifiers);
                    movement = true;
                    break;

                case Key.Up:
                    MoveVertical(-1, modifiers);
                    movement = true;
                    break;

                case Key.Down:
                    MoveVertical(1, modifiers);
                    movement = true;
                    break;

                case Key.Home:
                    MoveHome(modifiers);
                    movement = true;
                    break;

                case Key.End:
                    MoveEnd(modifiers);
                    movement = true;
                    break;

                case Key.Back:
                    if (modifiers == InputModifiers.Control && SelectionStart == SelectionEnd)
                    {
                        SetSelectionForControlBackspace(modifiers);
                    }

                    if (!DeleteSelection() && CaretIndex > 0)
                    {
                        var removedCharacters = 1;
                        // handle deleting /r/n
                        // you don't ever want to leave a dangling /r around. So, if deleting /n, check to see if
                        // a /r should also be deleted.
                        if (CaretIndex > 1 &&
                            text[CaretIndex - 1] == '\n' &&
                            text[CaretIndex - 2] == '\r')
                        {
                            removedCharacters = 2;
                        }

                        SetTextInternal(text.Substring(0, caretIndex - removedCharacters) + text.Substring(caretIndex));
                        CaretIndex -= removedCharacters;
                        SelectionStart = SelectionEnd = CaretIndex;
                    }

                    break;

                case Key.Delete:
                    if (modifiers == InputModifiers.Control && SelectionStart == SelectionEnd)
                    {
                        SetSelectionForControlDelete(modifiers);
                    }

                    if (!DeleteSelection() && caretIndex < text.Length)
                    {
                        var removedCharacters = 1;
                        // handle deleting /r/n
                        // you don't ever want to leave a dangling /r around. So, if deleting /n, check to see if
                        // a /r should also be deleted.
                        if (CaretIndex < text.Length - 1 &&
                            text[caretIndex + 1] == '\n' &&
                            text[caretIndex] == '\r')
                        {
                            removedCharacters = 2;
                        }

                        SetTextInternal(text.Substring(0, caretIndex) + text.Substring(caretIndex + removedCharacters));
                    }

                    break;

                case Key.Enter:
                    if (AcceptsReturn)
                    {
                        HandleTextInput("\r\n");
                    }

                    break;

                case Key.Tab:
                    if (AcceptsTab)
                    {
                        HandleTextInput("\t");
                    }
                    else
                    {
                        base.OnKeyDown(e);
                        handled = false;
                    }

                    break;

                default:
                    handled = false;
                    break;
            }

            if (movement && ((modifiers & InputModifiers.Shift) != 0))
            {
                SelectionEnd = CaretIndex;
            }
            else if (movement)
            {
                SelectionStart = SelectionEnd = CaretIndex;
            }

            if (handled)
            {
                e.Handled = true;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (e.Source == _presenter)
            {
                var point = e.GetPosition(_presenter);
                var index = CaretIndex = _presenter.GetCaretIndex(point);
                var text = Text;

                if (text != null)
                {
                    switch (e.ClickCount)
                    {
                        case 1:
                            SelectionStart = SelectionEnd = index;
                            break;

                        case 2:
                            if (!StringUtils.IsStartOfWord(text, index))
                            {
                                SelectionStart = StringUtils.PreviousWord(text, index);
                            }

                            SelectionEnd = StringUtils.NextWord(text, index);
                            break;

                        case 3:
                            SelectionStart = 0;
                            SelectionEnd = text.Length;
                            break;
                    }
                }

                e.Device.Capture(_presenter);
                e.Handled = true;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_presenter != null && e.Device.Captured == _presenter)
            {
                var point = e.GetPosition(_presenter);
                CaretIndex = SelectionEnd = _presenter.GetCaretIndex(point);
            }
        }

        protected override void OnPointerReleased(PointerEventArgs e)
        {
            if (_presenter != null && e.Device.Captured == _presenter)
            {
                e.Device.Capture(null);
            }
        }

        protected override void UpdateDataValidation(AvaloniaProperty property, BindingNotification status)
        {
            if (property == TextProperty)
            {
                var classes = (IPseudoClasses)Classes;
                DataValidationErrors = UnpackException(status.Error);
                classes.Set(":error", DataValidationErrors != null);
            }
        }

        private static IEnumerable<Exception> UnpackException(Exception exception)
        {
            if (exception != null)
            {
                var aggregate = exception as AggregateException;
                var exceptions = aggregate == null ?
                    (IEnumerable<Exception>)new[] { exception } :
                    aggregate.InnerExceptions;
                var filtered = exceptions.Where(x => !(x is BindingChainException)).ToList();

                if (filtered.Count > 0)
                {
                    return filtered;
                }
            }

            return null;
        }

        private int CoerceCaretIndex(int value)
        {
            var text = Text;
            var length = text?.Length ?? 0;

            if (value < 0)
            {
                return 0;
            }
            else if (value > length)
            {
                return length;
            }
            else if (value > 0 && text[value - 1] == '\r' && text[value] == '\n')
            {
                return value + 1;
            }
            else
            {
                return value;
            }
        }

        private int DeleteCharacter(int index)
        {
            var start = index + 1;
            var text = Text;
            var c = text[index];
            var result = 1;

            if (c == '\n' && index > 0 && text[index - 1] == '\r')
            {
                --index;
                ++result;
            }
            else if (c == '\r' && index < text.Length - 1 && text[index + 1] == '\n')
            {
                ++start;
                ++result;
            }

            Text = text.Substring(0, index) + text.Substring(start);

            return result;
        }

        private void MoveHorizontal(int direction, InputModifiers modifiers)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if ((modifiers & InputModifiers.Control) == 0)
            {
                var index = caretIndex + direction;

                if (index < 0 || index > text.Length)
                {
                    return;
                }
                else if (index == text.Length)
                {
                    CaretIndex = index;
                    return;
                }

                var c = text[index];

                if (direction > 0)
                {
                    CaretIndex += (c == '\r' && index < text.Length - 1 && text[index + 1] == '\n') ? 2 : 1;
                }
                else
                {
                    CaretIndex -= (c == '\n' && index > 0 && text[index - 1] == '\r') ? 2 : 1;
                }
            }
            else
            {
                if (direction > 0)
                {
                    CaretIndex += StringUtils.NextWord(text, caretIndex) - caretIndex;
                }
                else
                {
                    CaretIndex += StringUtils.PreviousWord(text, caretIndex) - caretIndex;
                }
            }
        }

        private void MoveVertical(int count, InputModifiers modifiers)
        {
            var formattedText = _presenter.FormattedText;
            var lines = formattedText.GetLines().ToList();
            var caretIndex = CaretIndex;
            var lineIndex = GetLine(caretIndex, lines) + count;

            if (lineIndex >= 0 && lineIndex < lines.Count)
            {
                var line = lines[lineIndex];
                var rect = formattedText.HitTestTextPosition(caretIndex);
                var y = count < 0 ? rect.Y : rect.Bottom;
                var point = new Point(rect.X, y + (count * (line.Height / 2)));
                var hit = formattedText.HitTestPoint(point);
                CaretIndex = hit.TextPosition + (hit.IsTrailing ? 1 : 0);
            }
        }

        private void MoveHome(InputModifiers modifiers)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if ((modifiers & InputModifiers.Control) != 0)
            {
                caretIndex = 0;
            }
            else
            {
                var lines = _presenter.FormattedText.GetLines();
                var pos = 0;

                foreach (var line in lines)
                {
                    if (pos + line.Length > caretIndex || pos + line.Length == text.Length)
                    {
                        break;
                    }

                    pos += line.Length;
                }

                caretIndex = pos;
            }

            CaretIndex = caretIndex;
        }

        private void MoveEnd(InputModifiers modifiers)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if ((modifiers & InputModifiers.Control) != 0)
            {
                caretIndex = text.Length;
            }
            else
            {
                var lines = _presenter.FormattedText.GetLines();
                var pos = 0;

                foreach (var line in lines)
                {
                    pos += line.Length;

                    if (pos > caretIndex)
                    {
                        if (pos < text.Length)
                        {
                            --pos;
                            if (pos > 0 && Text[pos - 1] == '\r' && Text[pos] == '\n')
                            {
                                --pos;
                            }
                        }

                        break;
                    }
                }

                caretIndex = pos;
            }

            CaretIndex = caretIndex;
        }

        private void SelectAll()
        {
            SelectionStart = 0;
            SelectionEnd = Text?.Length ?? 0;
        }

        private bool DeleteSelection()
        {
            if (!IsReadOnly)
            {
                var selectionStart = SelectionStart;
                var selectionEnd = SelectionEnd;

                if (selectionStart != selectionEnd)
                {
                    var start = Math.Min(selectionStart, selectionEnd);
                    var end = Math.Max(selectionStart, selectionEnd);
                    var text = Text;
                    SetTextInternal(text.Substring(0, start) + text.Substring(end));
                    SelectionStart = SelectionEnd = CaretIndex = start;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private string GetSelection()
        {
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var end = Math.Max(selectionStart, selectionEnd);
            if (start == end || (Text?.Length ?? 0) < end)
            {
                return "";
            }
            return Text.Substring(start, end - start);
        }

        private int GetLine(int caretIndex, IList<FormattedTextLine> lines)
        {
            int pos = 0;
            int i;

            for (i = 0; i < lines.Count; ++i)
            {
                var line = lines[i];
                pos += line.Length;

                if (pos > caretIndex)
                {
                    break;
                }
            }

            return i;
        }

        private void SetTextInternal(string value)
        {
            try
            {
                _ignoreTextChanges = true;
                SetAndRaise(TextProperty, ref _text, value);
            }
            finally
            {
                _ignoreTextChanges = false;
            }
        }

        private void SetSelectionForControlBackspace(InputModifiers modifiers)
        {
            SelectionStart = CaretIndex;
            MoveHorizontal(-1, modifiers);
            SelectionEnd = CaretIndex;
        }

        private void SetSelectionForControlDelete(InputModifiers modifiers)
        {
            SelectionStart = CaretIndex;
            MoveHorizontal(1, modifiers);
            SelectionEnd = CaretIndex;
        }

        UndoRedoState UndoRedoHelper<UndoRedoState>.IUndoRedoHost.UndoRedoState
        {
            get { return new UndoRedoState(Text, CaretIndex); }
            set
            {
                Text = value.Text;
                SelectionStart = SelectionEnd = CaretIndex = value.CaretPosition;
            }
        }
    }
}