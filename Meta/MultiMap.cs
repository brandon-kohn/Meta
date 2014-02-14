//
//! Copyright © 2008-2011
//! Brandon Kohn
//
//  Distributed under the Boost Software License, Version 1.0. (See
//  accompanying file LICENSE_1_0.txt or copy at
//  http://www.boost.org/LICENSE_1_0.txt)
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Meta
{    
    public class ListIterator<T> : IEnumerator<T>, IEquatable<ListIterator<T>>, IDisposable
    {        
        // Enumerators are positioned before the first element
        // until the first MoveNext() call.
        int position = int.MaxValue;
        IList<T> list = null;
                
        public ListIterator(IList<T> c, int p = -1)
        {
            list = c;
            position = p;
        }

        public void Dispose()
        {
            Dispose(true);

            // Use SupressFinalize in case a subclass 
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            
        }
        
        public bool MoveNext()
        {
            position = Math.Min(position + 1, list.Count);
            return (position < list.Count);
        }

        public bool MovePrev()
        {
            position = Math.Max(position - 1, -1);
            return (position >= 0);
        }

        public void Reset()
        {
            position = -1;
        }

        public bool IsEnd
        {
            get { return position >= list.Count; }
        }

        public int Position
        {
            get { return position; }
            set { position = value; }
        }

        object IEnumerator.Current
        {
            get
            {
                return list[position];
            }
        }

        public T Current
        {
            get
            {
                try
                {
                    return list[position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public bool Equals(ListIterator<T> other )
        {
            return list == other.list && (position == other.position || (IsEnd && other.IsEnd));
        }

        public int Distance( ListIterator<T> it )
        {
            Debug.Assert( list.Equals(it.list) );
            return it.position - position;
        }

        public ListIterator<T> Advance( int distance )
        {
            return new ListIterator<T>(list, position + distance);
        }

    }

    struct Range
    {
        public static KeyValuePair<K,V> MakeKeyValuePair<K,V>( K k, V v )
        {
            return new KeyValuePair<K, V>(k, v);
        }

        public static ListIterator<T> Begin<T>( IList<T> list )
        {
            return new ListIterator<T>(list, 0);
        }

        public static ListIterator<T> End<T>(IList<T> list)
        {
            return new ListIterator<T>(list, list.Count);
        }

        public static ListIterator<T> RBegin<T>(IList<T> list)
        {
            return new ListIterator<T>(list, list.Count);
        }

        public static ListIterator<T> REnd<T>(IList<T> list)
        {
            return new ListIterator<T>(list, -1);
        }

        public static ListIterator<T> LowerBound<T>(ListIterator<T> first, ListIterator<T> last, T value, IComparer<T> pred)
        {
            // find first element not before _Val, using _Pred
            int distance = first.Distance(last);
            Debug.Assert(distance >= 0);

            while(0 < distance)
            {
                // divide and conquer, find half that contains answer
                int d2 = distance / 2;
                ListIterator<T> mid = first.Advance(d2);//!Advance d2.

                if (pred.Compare(mid.Current, value) == -1)
                {
                    first.Position = mid.Position + 1;
                    distance -= d2 + 1;
                }
                else
                    distance = d2;
            }
            return first;
        }

        public static ListIterator<T> LowerBound<T>(ListIterator<T> first, ListIterator<T> last, T value) where T : IComparable<T>
        {
            // find first element not before _Val, using _Pred
            int distance = first.Distance(last);
            Debug.Assert(distance >= 0);

            while (0 < distance)
            {
                // divide and conquer, find half that contains answer
                int d2 = distance / 2;
                ListIterator<T> mid = first.Advance(d2);//!Advance d2.

                if (mid.Current.CompareTo(value) == -1)
                {
                    first.Position = mid.Position + 1;
                    distance -= d2 + 1;
                }
                else
                    distance = d2;
            }
            return first;
        }
    }

    public struct KeyValuePairComparer<K,V> : IComparer<KeyValuePair<K,V>> where K : IComparable<K>
    {
        public int Compare( KeyValuePair<K,V> lhs, KeyValuePair<K,V> rhs )
        {
            return lhs.Key.CompareTo(rhs.Key);
        }
    }
    
    /// <summary>
    /// Summary description for MultiMap.
    /// </summary>
    public class MultiMap<Key,Value> where Key : IComparable<Key> 
    {
        public MultiMap()
        {
        }
        
        public ListIterator<KeyValuePair<Key, Value>> Begin
        {
            get { return Range.Begin(list); }
        }

        public ListIterator<KeyValuePair<Key, Value>> RBegin
        {
            get { return Range.RBegin(list); }
        }

        public ListIterator<KeyValuePair<Key, Value>> End
        {
            get { return Range.End(list); }
        }

        #region member functions

        public int Find(Key k)
        {
            return list.BinarySearch(new KeyValuePair<Key, Value>(k, default(Value)));            
        }

        public Value this[int key]
        {
            get
            {
                return list[key].Value;
            }
            set
            {
                list[key] = new KeyValuePair<Key,Value>( list[key].Key, value );
            }
        }

        public void Insert( Key k, Value v )
        {
            KeyValuePair<Key, Value> kp = Range.MakeKeyValuePair(k, v);
            ListIterator<KeyValuePair<Key, Value>> it = Range.LowerBound<KeyValuePair<Key, Value>>
                (
                    Begin
                  , End
                  , kp
                  , new KeyValuePairComparer<Key, Value>()
                );

            if (it.IsEnd)
                list.Add(kp);
            else
                list.Insert(it.Position, kp);            
        }

        #endregion

        #region member variables

        private List< KeyValuePair<Key,Value> > list = new List< KeyValuePair<Key,Value> >();

        #endregion Member Data
    }
}
