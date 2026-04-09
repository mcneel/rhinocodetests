using System;
using System.Collections;
using System.Collections.Generic;

using Rhino.Runtime.Code.Execution;

namespace Rhino.Runtime.Code.Testing
{
    public enum StackActionKind { Pushed, Swapped, Popped };

    public sealed class StackAction
    {
        public StackActionKind Kind { get; }

        public ExecEvent FromEventKind { get; }
        public int FromEventLine { get; }

        public ExecEvent ToEventKind { get; }
        public int ToEventLine { get; }

        public StackAction(StackActionKind kind, ExecEvent fromKind, int fromLine, ExecEvent toKind, int toLine)
        {
            Kind = kind;
            FromEventKind = fromKind;
            FromEventLine = fromLine;
            ToEventKind = toKind;
            ToEventLine = toLine;
        }
    }

    public sealed class DebugStackActionsWatcher : DebugStackWatcherControls, IEnumerable<StackAction>
    {
        readonly Queue<StackAction> _queue = new();
        readonly Action<string> _reporter;
        readonly Action<object, object> _asserter;
        bool _errored = false;

        public void Add(StackAction action) => _queue.Enqueue(action);

        public StackAction Next() => _queue.Dequeue();

        public bool Pass => !_errored && _queue.Count == 0;
        public bool SkipAssert { get; set; } = false;

        public DebugStackActionsWatcher(Action<string> reporter, Action<object, object> asserter)
        {
            _reporter = reporter;
            _asserter = asserter;
        }

        protected override void OnStackFramePushed(ExecFrame pushed)
        {
            base.OnStackFramePushed(pushed);

            if (SkipAssert)
            {
                _reporter($"Pushed:  {pushed.Event} {pushed.Reference.Position}");
                return;
            }

            StackAction expected = Next();
            TryAssert(expected.Kind, StackActionKind.Pushed);
            TryAssert(expected.FromEventKind, pushed.Event);
            TryAssert(expected.FromEventLine, pushed.Reference.Position.LineNumber);
        }

        protected override void OnStackFrameSwapped(ExecFrame popped, ExecFrame pushed)
        {
            base.OnStackFrameSwapped(popped, pushed);

            if (SkipAssert)
            {
                _reporter($"Swapped: {popped.Event} {popped.Reference.Position} -> {pushed.Event} {pushed.Reference.Position}");
                return;
            }

            StackAction expected = Next();
            TryAssert(expected.Kind, StackActionKind.Swapped);
            TryAssert(expected.FromEventKind, popped.Event);
            TryAssert(expected.FromEventLine, popped.Reference.Position.LineNumber);
            TryAssert(expected.ToEventKind, pushed.Event);
            TryAssert(expected.ToEventLine, pushed.Reference.Position.LineNumber);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<StackAction> GetEnumerator() => _queue.GetEnumerator();

        void TryAssert(object expected, object actual)
        {
            if (_errored)
            {
                return;
            }

            try
            {
                _asserter(expected, actual);
            }
            catch (Exception ex)
            {
                _reporter($"{nameof(DebugStackActionsWatcher)}: {ex.Message}");
                _errored = true;
            }
        }
    }
}
