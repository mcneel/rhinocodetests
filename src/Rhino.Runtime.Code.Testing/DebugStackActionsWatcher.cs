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

        public void Add(StackAction action) => _queue.Enqueue(action);

        public int Count => _queue.Count;
        public StackAction Next() => _queue.Dequeue();

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
            _asserter(expected.Kind, StackActionKind.Pushed);
            _asserter(expected.FromEventKind, pushed.Event);
            _asserter(expected.FromEventLine, pushed.Reference.Position.LineNumber);
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
            _asserter(expected.Kind, StackActionKind.Swapped);
            _asserter(expected.FromEventKind, popped.Event);
            _asserter(expected.FromEventLine, popped.Reference.Position.LineNumber);
            _asserter(expected.ToEventKind, pushed.Event);
            _asserter(expected.ToEventLine, pushed.Reference.Position.LineNumber);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<StackAction> GetEnumerator() => _queue.GetEnumerator();
    }
}
