﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using Graphite.Linq;

namespace Graphite.Extensions
{
    public class ArrayItem<T>
    {
        public ArrayItem(T[] array, int index)
        {
            Array = array;
            Index = index;
        }

        public T[] Array { get; }
        public int Index { get; }

        public T Value
        {
            get => Array[Index];
            set => Array[Index] = value;
        }
    }

    public static class ArrayExtensions
    {
        public static byte[] ToBytes(this string source, Encoding encoding = null)
        {
            return source == null 
                ? null
                : (source == ""
                    ? new byte[] {}
                    : (encoding ?? Encoding.UTF8).GetBytes(source));
        }

        public static string ToString(this byte[] source, int count, Encoding encoding = null)
        {
            return source.ToString(0, count, encoding);
        }

        public static string ToString(this byte[] source, int offset, int count, Encoding encoding = null)
        {
            return source == null
                ? null
                : (encoding ?? Encoding.UTF8)
                .GetString(source, offset, count);
        }

        public static ArrayItem<T> GetItem<T>(this T[] source, int index)
        {
            return new ArrayItem<T>(source, index);
        }

        public static object EnsureValue<T>(this T[] source, 
            int index, Func<T> @default) where T : class
        {
            return source[index] ?? (source[index] = @default());
        }

        public static string ToHex(this byte[] bytes)
        {
            return new SoapHexBinary(bytes).ToString();
        }

        public static T[] AsArray<T>(this T source, params T[] tail)
        {
            return source.AsArray((IEnumerable<T>)tail);
        }

        public static T[] AsArray<T>(this T source, IEnumerable<T> tail)
        {
            return source.Join(tail).ToArray();
        }

        public static List<T> AsList<T>(this T source, params T[] tail)
        {
            return source.AsList((IEnumerable<T>)tail);
        }

        public static byte[] Copy(this byte[] source, int length)
        {
            var target = new byte[length];
            Array.Copy(source, target, length);
            return target;
        }

        public static bool ContainsAt(this byte[] source, byte[] compare, int offset, int? length = null)
        {
            length = length ?? compare.Length;
            if (offset < 0 || source == null || source.Length == 0 ||
                compare == null || length == 0 ||
                length > source.Length - offset) return false;

            for (var index = 0; index < length; index++)
            {
                if (source[index + offset] != compare[index]) return false;
            }

            return true;
        }

        public static bool Contains(this byte[] source, 
            byte[] find, int offset, int length)
        {
            return IndexOfSequence(source, find, offset, length) > -1;
        }

        public static int IndexOfSequence(this byte[] source,
            byte[] find, int offset, int length)
        {
            if (offset < 0 || length < 0 || source == null || 
                source.Length == 0) return -1;

            if (find == null || find.Length == 0 ||
                find.Length > source.Length - offset) return -1;

            for (var position = 0; position < length; position++)
            {
                if (source.ContainsAt(find, offset + position))
                    return position;
            }

            return -1;
        }
    }
}
