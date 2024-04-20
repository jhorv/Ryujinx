#nullable enable
using System;

namespace Ryujinx.Common.Collections
{
    public sealed class RyujinxLinkedListNode<T> : IDisposable
    {
        private static readonly ObjectPool<RyujinxLinkedListNode<T>>
            _objectPool = new ObjectPool<RyujinxLinkedListNode<T>>(() => new(), 1000);

        internal RyujinxLinkedList<T>? _list;
        internal RyujinxLinkedListNode<T>? _next;
        internal RyujinxLinkedListNode<T>? _prev;
        internal T _item;
        private bool _isDisposed;

        public static RyujinxLinkedListNode<T> Create(T value)
        {
            var node = _objectPool.Allocate();
            node._isDisposed = false;
            node._item = value;
            return node;
        }

        public static RyujinxLinkedListNode<T> Create(RyujinxLinkedList<T> list, T value)
        {
            var node = _objectPool.Allocate();
            node._isDisposed = false;
            node._list = list;
            node._item = value;
            return node;
        }

        private RyujinxLinkedListNode()
        {
            _item = default!;
        }

        // public RyujinxLinkedListNode(T value)
        // {
        //     _item = value;
        // }
        //
        // internal RyujinxLinkedListNode(RyujinxLinkedList<T> list, T value)
        // {
        //     _list = list;
        //     _item = value;
        // }
        //
        public RyujinxLinkedList<T>? List => _list;

        public RyujinxLinkedListNode<T>? Next
            => _next == null || _next == _list!._head ? null : _next;

        public RyujinxLinkedListNode<T>? Previous
            => _prev == null || this == _list!._head ? null : _prev;

        public T Value
        {
            get => _item;
            set => _item = value;
        }

        /// <summary>Gets a reference to the value held by the node.</summary>
        public ref T ValueRef => ref _item;

        internal void Invalidate()
        {
            _list = null;
            _next = null;
            _prev = null;
        }

        public void Dispose()
        {
            if (_isDisposed == false)
            {
                _isDisposed = true;
                _objectPool.Release(this);
            }
        }
    }
}
