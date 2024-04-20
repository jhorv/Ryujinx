#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ryujinx.Common.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    public class RyujinxLinkedList<T> : ICollection<T>, ICollection, IReadOnlyCollection<T>
    {
        // This RyujinxLinkedList is a doubly-Linked circular list.
        internal RyujinxLinkedListNode<T>? _head;
        internal int _count;
        internal int _version;

        private const string ArgumentOutOfRangeBiggerThanCollection = "Must be less than or equal to the size of the collection.";
        private const string LinkedListEmpty = "The RyujinxLinkedList is empty.";
        private const string LinkedListNodeIsAttached = "The RyujinxLinkedList node already belongs to a RyujinxLinkedList.";
        private const string ExternalLinkedListNode = "The RyujinxLinkedList node does not belong to current RyujinxLinkedList.";
        private const string ArgRankMultiDimNotSupported = "Only single dimensional arrays are supported for the requested action.";
        private const string ArgNonZeroLowerBound = "The lower bound of target array must be zero.";
        private const string ArgInsufficientSpace = "Insufficient space in the target location to copy the information.";
        private const string ArgumentIncompatibleArrayType = "Target array type is not compatible with the type of items in the collection.";

        public RyujinxLinkedList()
        {
        }

        // public RyujinxLinkedList(IEnumerable<T> collection)
        // {
        //     ArgumentNullException.ThrowIfNull(collection);
        //
        //     foreach (T item in collection)
        //     {
        //         AddLast(item);
        //     }
        // }

        public int Count => _count;

        public RyujinxLinkedListNode<T>? First => _head;

        public RyujinxLinkedListNode<T>? Last => _head?._prev;

        bool ICollection<T>.IsReadOnly => false;

        void ICollection<T>.Add(T value) => AddLast(value);

        // public RyujinxLinkedListNode<T> AddAfter(RyujinxLinkedListNode<T> node, T value)
        // {
        //     ValidateNode(node);
        //     var result = RyujinxLinkedListNode<T>.Create(node._list!, value);
        //     InternalInsertNodeBefore(node._next!, result);
        //     return result;
        // }

        // public void AddAfter(RyujinxLinkedListNode<T> node, RyujinxLinkedListNode<T> newNode)
        // {
        //     ValidateNode(node);
        //     ValidateNewNode(newNode);
        //     InternalInsertNodeBefore(node._next!, newNode);
        //     newNode._list = this;
        // }

        public RyujinxLinkedListNode<T> AddBefore(RyujinxLinkedListNode<T> node, T value)
        {
            ValidateNode(node);
            var result = RyujinxLinkedListNode<T>.Create(node._list!, value);
            InternalInsertNodeBefore(node, result);
            if (node == _head)
            {
                _head = result;
            }
            return result;
        }

        // public void AddBefore(RyujinxLinkedListNode<T> node, RyujinxLinkedListNode<T> newNode)
        // {
        //     ValidateNode(node);
        //     ValidateNewNode(newNode);
        //     InternalInsertNodeBefore(node, newNode);
        //     newNode._list = this;
        //     if (node == _head)
        //     {
        //         _head = newNode;
        //     }
        // }

        public RyujinxLinkedListNode<T> AddFirst(T value)
        {
            var result = RyujinxLinkedListNode<T>.Create(this, value);
            if (_head == null)
            {
                InternalInsertNodeToEmptyList(result);
            }
            else
            {
                InternalInsertNodeBefore(_head, result);
                _head = result;
            }
            return result;
        }

        // public void AddFirst(RyujinxLinkedListNode<T> node)
        // {
        //     ValidateNewNode(node);
        //
        //     if (_head == null)
        //     {
        //         InternalInsertNodeToEmptyList(node);
        //     }
        //     else
        //     {
        //         InternalInsertNodeBefore(_head, node);
        //         _head = node;
        //     }
        //     node._list = this;
        // }

        public RyujinxLinkedListNode<T> AddLast(T value)
        {
            var result = RyujinxLinkedListNode<T>.Create(this, value);
            if (_head == null)
            {
                InternalInsertNodeToEmptyList(result);
            }
            else
            {
                InternalInsertNodeBefore(_head, result);
            }
            return result;
        }

        // public void AddLast(RyujinxLinkedListNode<T> node)
        // {
        //     ValidateNewNode(node);
        //
        //     if (_head == null)
        //     {
        //         InternalInsertNodeToEmptyList(node);
        //     }
        //     else
        //     {
        //         InternalInsertNodeBefore(_head, node);
        //     }
        //     node._list = this;
        // }

        public void Clear()
        {
            RyujinxLinkedListNode<T>? current = _head;
            while (current != null)
            {
                RyujinxLinkedListNode<T> temp = current;
                current = current.Next;
                temp.Dispose();
            }

            _head = null;
            _count = 0;
            _version++;
        }

        public bool Contains(T value)
        {
            return Find(value) != null;
        }

        public void CopyTo(T[] array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (index > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, ArgumentOutOfRangeBiggerThanCollection);
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException(ArgInsufficientSpace);
            }

            RyujinxLinkedListNode<T>? node = _head;
            if (node != null)
            {
                do
                {
                    array[index++] = node!._item;
                    node = node._next;
                } while (node != _head);
            }
        }

        public RyujinxLinkedListNode<T>? Find(T value)
        {
            RyujinxLinkedListNode<T>? node = _head;
            EqualityComparer<T> c = EqualityComparer<T>.Default;
            if (node != null)
            {
                if (value != null)
                {
                    do
                    {
                        if (c.Equals(node!._item, value))
                        {
                            return node;
                        }
                        node = node._next;
                    } while (node != _head);
                }
                else
                {
                    do
                    {
                        if (node!._item == null)
                        {
                            return node;
                        }
                        node = node._next;
                    } while (node != _head);
                }
            }
            return null;
        }

        // public RyujinxLinkedListNode<T>? FindLast(T value)
        // {
        //     if (_head == null) return null;
        //
        //     RyujinxLinkedListNode<T>? last = _head._prev;
        //     RyujinxLinkedListNode<T>? node = last;
        //     EqualityComparer<T> c = EqualityComparer<T>.Default;
        //     if (node != null)
        //     {
        //         if (value != null)
        //         {
        //             do
        //             {
        //                 if (c.Equals(node!._item, value))
        //                 {
        //                     return node;
        //                 }
        //
        //                 node = node._prev;
        //             } while (node != last);
        //         }
        //         else
        //         {
        //             do
        //             {
        //                 if (node!._item == null)
        //                 {
        //                     return node;
        //                 }
        //                 node = node._prev;
        //             } while (node != last);
        //         }
        //     }
        //     return null;
        // }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
            Count == 0 ? ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator() :
            GetEnumerator();

        public bool Remove(T value)
        {
            RyujinxLinkedListNode<T>? node = Find(value);
            if (node != null)
            {
                InternalRemoveNode(node);
                return true;
            }
            return false;
        }

        public void Remove(RyujinxLinkedListNode<T> node)
        {
            ValidateNode(node);
            InternalRemoveNode(node);
        }

        // public void RemoveFirst()
        // {
        //     if (_head == null) { throw new InvalidOperationException(LinkedListEmpty); }
        //     InternalRemoveNode(_head);
        // }
        //
        // public void RemoveLast()
        // {
        //     if (_head == null) { throw new InvalidOperationException(LinkedListEmpty); }
        //     InternalRemoveNode(_head._prev!);
        // }

        private void InternalInsertNodeBefore(RyujinxLinkedListNode<T> node, RyujinxLinkedListNode<T> newNode)
        {
            newNode._next = node;
            newNode._prev = node._prev;
            node._prev!._next = newNode;
            node._prev = newNode;
            _version++;
            _count++;
        }

        private void InternalInsertNodeToEmptyList(RyujinxLinkedListNode<T> newNode)
        {
            Debug.Assert(_head == null && _count == 0, "RyujinxLinkedList must be empty when this method is called!");
            newNode._next = newNode;
            newNode._prev = newNode;
            _head = newNode;
            _version++;
            _count++;
        }

        internal void InternalRemoveNode(RyujinxLinkedListNode<T> node)
        {
            Debug.Assert(node._list == this, "Deleting the node from another list!");
            Debug.Assert(_head != null, "This method shouldn't be called on empty list!");
            if (node._next == node)
            {
                Debug.Assert(_count == 1 && _head == node, "this should only be true for a list with only one node");
                _head = null;
            }
            else
            {
                node._next!._prev = node._prev;
                node._prev!._next = node._next;
                if (_head == node)
                {
                    _head = node._next;
                }
            }

            node.Dispose();
            _count--;
            _version++;
        }

        // internal static void ValidateNewNode(RyujinxLinkedListNode<T> node)
        // {
        //     ArgumentNullException.ThrowIfNull(node);
        //
        //     if (node._list != null)
        //     {
        //         throw new InvalidOperationException(LinkedListNodeIsAttached);
        //     }
        // }

        internal void ValidateNode(RyujinxLinkedListNode<T> node)
        {
            ArgumentNullException.ThrowIfNull(node);

            if (node._list != this)
            {
                throw new InvalidOperationException(ExternalLinkedListNode);
            }
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        void ICollection.CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
            {
                throw new ArgumentException(ArgRankMultiDimNotSupported, nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(ArgNonZeroLowerBound, nameof(array));
            }

            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (array.Length - index < Count)
            {
                throw new ArgumentException(ArgInsufficientSpace);
            }

            T[]? tArray = array as T[];
            if (tArray != null)
            {
                CopyTo(tArray, index);
            }
            else
            {
                // No need to use reflection to verify that the types are compatible because it isn't 100% correct and we can rely
                // on the runtime validation during the cast that happens below (i.e. we will get an ArrayTypeMismatchException).
                object?[]? objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException(ArgumentIncompatibleArrayType, nameof(array));
                }
                RyujinxLinkedListNode<T>? node = _head;
                try
                {
                    if (node != null)
                    {
                        do
                        {
                            objects[index++] = node!._item;
                            node = node._next;
                        } while (node != _head);
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(ArgumentIncompatibleArrayType, nameof(array));
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly RyujinxLinkedList<T> _list;
            private RyujinxLinkedListNode<T>? _node;
            private readonly int _version;
            private T? _current;
            private int _index;

            private const string InvalidOperationEnumOpCantHappen = "Enumeration has either not started or has already finished.";
            private const string InvalidOperationEnumFailedVersion = "Collection was modified after the enumerator was instantiated.";

            internal Enumerator(RyujinxLinkedList<T> list)
            {
                _list = list;
                _version = list._version;
                _node = list._head;
                _current = default;
                _index = 0;
            }

            public T Current => _current!;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _list.Count + 1))
                    {
                        throw new InvalidOperationException(InvalidOperationEnumOpCantHappen);
                    }

                    return Current;
                }
            }

            public bool MoveNext()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException(InvalidOperationEnumFailedVersion);
                }

                if (_node == null)
                {
                    _index = _list.Count + 1;
                    return false;
                }

                ++_index;
                _current = _node._item;
                _node = _node._next;
                if (_node == _list._head)
                {
                    _node = null;
                }
                return true;
            }

            void IEnumerator.Reset()
            {
                if (_version != _list._version)
                {
                    throw new InvalidOperationException(InvalidOperationEnumFailedVersion);
                }

                _current = default;
                _node = _list._head;
                _index = 0;
            }

            public void Dispose()
            {
            }
        }
    }
}
