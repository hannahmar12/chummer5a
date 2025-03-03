/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Chummer
{
    /// <summary>
    /// Structured array built for working properly as a key to dictionaries. Read-only to make sure keys remain immutable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct KeyArray<T> : IReadOnlyList<T>
    {
        private readonly T[] _aobjItems;
        private readonly int _intHashCode;

        public KeyArray(IEnumerable<T> lstItems)
        {
            _aobjItems = lstItems.ToArray();
            _intHashCode = _aobjItems.GetEnsembleHashCode();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)_aobjItems.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T item)
        {
            return _aobjItems.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex + Length > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            for (int i = 0; i < Length; ++i)
            {
                array[arrayIndex] = this[i];
                arrayIndex += 1;
            }
        }

        int IReadOnlyCollection<T>.Count => Length;

        public int Length => _aobjItems.Length;

        public T this[int index] => _aobjItems[index];

        public override int GetHashCode()
        {
            return _intHashCode;
        }

        public static bool operator ==(KeyArray<T> lhs, KeyArray<T> rhs) => lhs.Equals(rhs);

        public static bool operator !=(KeyArray<T> lhs, KeyArray<T> rhs) => !(lhs == rhs);

        public bool Equals(KeyArray<T> rhs)
        {
            if (GetHashCode() != rhs.GetHashCode())
                return false;
            if (Length != rhs.Length)
                return false;
            for (int i = 0; i < Length; ++i)
            {
                if (!Equals(this[i], rhs[i]))
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals((KeyArray<T>)obj);
        }
    }
}
