﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace System.Windows.Forms
{
    internal static class ClientUtils
    {
        // Sequential version
        // assumes sequential enum members 0,1,2,3,4 -etc.
        //
        public static bool IsEnumValid(Enum enumValue, int value, int minValue, int maxValue)
        {
            bool valid = (value >= minValue) && (value <= maxValue);
#if DEBUG
            Debug_SequentialEnumIsDefinedCheck(enumValue, minValue, maxValue);
#endif
            return valid;
        }

#if DEBUG
        [ThreadStatic]
#pragma warning disable IDE1006 // Naming styles
        private static Hashtable? t_enumValueInfo;
#pragma warning restore IDE1006 // Naming styles
        public const int MAXCACHE = 300;  // we think we're going to get O(100) of these, put in a tripwire if it gets larger.

        private sealed class SequentialEnumInfo
        {
            public SequentialEnumInfo(Type t)
            {
                int actualMinimum = int.MaxValue;
                int actualMaximum = int.MinValue;
                int countEnumVals = 0;

                foreach (int iVal in Enum.GetValues(t))
                {
                    actualMinimum = Math.Min(actualMinimum, iVal);
                    actualMaximum = Math.Max(actualMaximum, iVal);
                    countEnumVals++;
                }

                if (countEnumVals - 1 != (actualMaximum - actualMinimum))
                {
                    Debug.Fail("this enum cannot be sequential.");
                }
                MinValue = actualMinimum;
                MaxValue = actualMaximum;
            }
            public int MinValue;
            public int MaxValue;
        }

        private static void Debug_SequentialEnumIsDefinedCheck(Enum value, int minVal, int maxVal)
        {
            Type t = value.GetType();

            t_enumValueInfo ??= new Hashtable();

            SequentialEnumInfo? sequentialEnumInfo = null;

            if (t_enumValueInfo.ContainsKey(t))
            {
                sequentialEnumInfo = t_enumValueInfo[t] as SequentialEnumInfo;
            }
            if (sequentialEnumInfo is null)
            {
                sequentialEnumInfo = new SequentialEnumInfo(t);

                if (t_enumValueInfo.Count > MAXCACHE)
                {
                    // see comment next to MAXCACHE declaration.
                    Debug.Fail("cache is too bloated, clearing out, we need to revisit this.");
                    t_enumValueInfo.Clear();
                }
                t_enumValueInfo[t] = sequentialEnumInfo;
            }
            if (minVal != sequentialEnumInfo.MinValue)
            {
                // put string allocation in the IF block so the common case doesnt build up the string.
                Debug.Fail("Minimum passed in is not the actual minimum for the enum.  Consider changing the parameters or using a different function.");
            }
            if (maxVal != sequentialEnumInfo.MaxValue)
            {
                // put string allocation in the IF block so the common case doesnt build up the string.
                Debug.Fail("Maximum passed in is not the actual maximum for the enum.  Consider changing the parameters or using a different function.");
            }
        }
#endif
    }
}
