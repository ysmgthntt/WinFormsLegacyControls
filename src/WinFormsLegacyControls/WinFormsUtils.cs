﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace System.Windows.Forms
{
    internal static class WindowsFormsUtils
    {
        /// <summary>
        ///  If you want to know if a piece of text contains one and only one &amp;
        ///  this is your function. If you have a character "t" and want match it to &amp;Text
        ///  Control.IsMnemonic is a better bet.
        /// </summary>
        public static bool ContainsMnemonic([NotNullWhen(true)] string? text)
        {
            if (text is not null)
            {
                int textLength = text.Length;
                int firstAmpersand = text.IndexOf('&', 0);
                if (firstAmpersand >= 0 && firstAmpersand <= /*second to last char=*/textLength - 2)
                {
                    // we found one ampersand and it's either the first character
                    // or the second to last character
                    // or a character in between

                    // We're so close!  make sure we don't have a double ampersand now.
                    int secondAmpersand = text.IndexOf('&', firstAmpersand + 1);
                    if (secondAmpersand == -1)
                    {
                        // didn't find a second one in the string.
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///  Retrieves the mnemonic from a given string, or zero if no mnemonic.
        ///  As used by the Control.Mnemonic to get mnemonic from Control.Text.
        /// <summary>
        public static char GetMnemonic(string text, bool convertToUpperCase)
        {
            char mnemonic = '\0';
            if (text is not null)
            {
                int len = text.Length;
                for (int i = 0; i < len - 1; i++)
                {
                    if (text[i] == '&')
                    {
                        if (text[i + 1] == '&')
                        {
                            // we have an escaped &, so we need to skip it.
                            i++;
                            continue;
                        }

                        if (convertToUpperCase)
                        {
                            mnemonic = char.ToUpper(text[i + 1], CultureInfo.CurrentCulture);
                        }
                        else
                        {
                            mnemonic = char.ToLower(text[i + 1], CultureInfo.CurrentCulture);
                        }
                        break;
                    }
                }
            }
            return mnemonic;
        }

        /// <summary>
        ///  Compares the strings using invariant culture for Turkish-I support. Returns true if they match.
        ///
        ///  If your strings are symbolic (returned from APIs, not from user) the following calls
        ///  are faster than this method:
        ///
        ///  String.Equals(s1, s2, StringComparison.Ordinal)
        ///  String.Equals(s1, s2, StringComparison.OrdinalIgnoreCase)
        /// </summary>
        public static bool SafeCompareStrings(string? string1, string? string2, bool ignoreCase)
        {
            if ((string1 is null) || (string2 is null))
            {
                // if either key is null, we should return false
                return false;
            }

            // Because String.Compare returns an ordering, it can not terminate early if lengths are not the same.
            // Also, equivalent characters can be encoded in different byte sequences, so it can not necessarily
            // terminate on the first byte which doesn't match. Hence this optimization.
            if (string1.Length != string2.Length)
            {
                return false;
            }

            return string.Compare(string1, string2, ignoreCase, CultureInfo.InvariantCulture) == 0;
        }

        public static string GetComponentName(IComponent component, string? defaultNameValue)
        {
            Debug.Assert(component is not null, "component passed here cannot be null");
            if (string.IsNullOrEmpty(defaultNameValue))
            {
                return component.Site?.Name ?? string.Empty;
            }
            else
            {
                return defaultNameValue;
            }
        }

        public sealed class ArraySubsetEnumerator : IEnumerator
        {
            private readonly object[]? _array;
            private readonly int _total;
            private int _current;

            public ArraySubsetEnumerator(object[] array, int count)
            {
                Debug.Assert(count == 0 || array is not null, "if array is null, count should be 0");
                Debug.Assert(array is null || count <= array.Length, "Trying to enumerate more than the array contains");
                _array = array;
                _total = count;
                _current = -1;
            }

            public bool MoveNext()
            {
                if (_current < _total - 1)
                {
                    _current++;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                _current = -1;
            }

            public object? Current => _current == -1 ? null : _array![_current];
        }
    }
}
