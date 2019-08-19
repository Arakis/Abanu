﻿// Copyright (c) MOSA Project. Licensed under the New BSD License.
using System;
using System.Collections;
using System.Collections.Generic;

namespace lonos.kernel.core
{

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class KList<T> : IList<T>, IList, IReadOnlyList<T>
    {
        private const int _defaultCapacity = 4;

        private static readonly T[] _emptyArray = new T[0];

        private T[] _items;
        private int _size;
        private uint _elementSize;

        public KList(uint elementSize)
        {
            _items = _emptyArray;
            _size = 0;
            _elementSize = elementSize;
        }

        public unsafe KList(uint elementSize, int capacity)
        {
            _elementSize = elementSize;

            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            if (capacity == 0)
                _items = _emptyArray;
            else
                _items = CreateArray(capacity);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private T[] CreateArray(int capacity)
        {
            var type = typeof(T);
            var handle = type.TypeHandle;

            var ptr = Mosa.Runtime.Internal.AllocateArray(typeof(T).TypeHandle, _elementSize, (uint)capacity);
            return (T[])Mosa.Runtime.Intrinsic.GetObjectFromAddress(ptr);
        }

        private void DestryArray(T[] array)
        {
            var ptr = Mosa.Runtime.Intrinsic.GetObjectAddress(array);
            Memory.Free(ptr);
        }

        public void Destroy()
        {
            if (_items.Length > 0)
                DestryArray(_items);
            var ptr = Mosa.Runtime.Intrinsic.GetObjectAddress(this);
            Memory.Free(ptr);
        }

        public KList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            var c = collection as ICollection<T>;
            if (c != null)
            {
                var count = c.Count;
                if (count == 0)
                {
                    _items = _emptyArray;
                }
                else
                {
                    _items = CreateArray(count);
                    c.CopyTo(_items, 0);
                    _size = count;
                }
            }
            else
            {
                _size = 0;
                _items = _emptyArray;

                // This enumerable could be empty. Let Add handle resizing.
                // Note that the default capacity is 4 so Add will only begin resizing after 4 elements.

                using (var en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                        Add(en.Current);
                }
            }
        }

        public int Capacity
        {
            get
            {
                return _items.Length;
            }
            set
            {
                if (value < _size)
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = CreateArray(value);
                        if (_size > 0)
                            Copy(_items, 0, newItems, 0, _size);
                        if (_items != _emptyArray)
                            DestryArray(_items);
                        _items = newItems;
                    }
                    else
                    {
                        _items = _emptyArray;
                    }
                }
            }
        }

        private void Copy(T[] source, int sourceIndex, T[] destination, int destinationIndex, int size)
        {
            for (int i = 0; i < size; i++)
            {
                destination[i + destinationIndex] = source[i + sourceIndex];
            }
        }

        private void Copy(T[] source, int sourceIndex, Array destination, int destinationIndex, int size)
        {
            Copy(source, sourceIndex, (T[])destination, destinationIndex, size);
        }

        int ICollection.Count
        {
            get { return _size; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        bool IList.IsFixedSize
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets or sets the T at the specified index.
        /// </summary>
        /// <value></value>
        public T this[int index]
        {
            get { return _items[index]; }
            set { _items[index] = value; }
        }

        public int Count
        {
            get { return _size; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        object IList.this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        private void EnsureCapacity(int size)
        {
            if (_items.Length < size)
            {
                var newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;
                if (newCapacity < size) newCapacity = size;
                Capacity = newCapacity;
            }
        }

        private static bool IsCompatibleObject(object value)
        {
            // Non-null values are fine. Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            return ((value is T) || (value == null && default(T) == null));
        }

        public void Add(T item)
        {
            EnsureCapacity(_size + 1);

            _items[_size] = item;
            _size++;
        }

        int IList.Add(object value)
        {
            if (!IsCompatibleObject(value))
                throw new ArgumentException("item is of a type that is not assignable to the IList", nameof(value));
            Add((T)value);
            return Count - 1;
        }

        public void Clear()
        {
            _size = 0;
        }

        public bool Contains(T item)
        {
            for (int i = 0; i < _size; i++)
            {
                if (_items[i].Equals(item))
                    return true;
            }
            return false;
        }

        bool IList.Contains(object value)
        {
            if (IsCompatibleObject(value))
                return Contains((T)value);
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            Copy(_items, 0, array, arrayIndex, _size);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            Copy(_items, 0, array, arrayIndex, _size);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < _size; i++)
            {
                if (_items[i].Equals(item))
                    return i;
            }
            return -1;
        }

        int IList.IndexOf(object value)
        {
            if (IsCompatibleObject(value))
                return IndexOf((T)value);
            return -1;
        }

        public void Insert(int index, T item)
        {
            EnsureCapacity(_size + 1);

            _size++;
            for (int i = index; i < _size; i++)
            {
                _items[i] = _items[i + 1];
            }

            _items[index] = item;
        }

        void IList.Insert(int index, object value)
        {
            if (!IsCompatibleObject(value))
                throw new ArgumentException("item is of a type that is not assignable to the IList", nameof(value));
            Insert(index, (T)value);
        }

        public bool Remove(T item)
        {
            int at = IndexOf(item);

            if (at < 0)
                return false;

            RemoveAt(at);

            return true;
        }

        void IList.Remove(object value)
        {
            if (IsCompatibleObject(value))
                Remove((T)value);
        }

        /// <summary>
        /// Removes at.
        /// </summary>
        /// <param name="index">The index.</param>
        public void RemoveAt(int index)
        {
            _size--;

            for (int i = index; i < _size; i++)
            {
                _items[i] = _items[i + 1];
            }

            _items[_size] = default(T);
        }

        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private KList<T> list;
            private int index;
            private T current;

            internal Enumerator(KList<T> list)
            {
                this.list = list;
                index = 0;
                current = default(T);
            }

            public T Current
            {
                get { return current; }
            }

            object IEnumerator.Current
            {
                get { return current; }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                KList<T> localList = list;

                if (((uint)index < (uint)localList._size))
                {
                    current = localList._items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                index = list._size + 1;
                current = default(T);
                return false;
            }

            void IEnumerator.Reset()
            {
                index = 0;
                current = default(T);
            }
        }
    }
}
