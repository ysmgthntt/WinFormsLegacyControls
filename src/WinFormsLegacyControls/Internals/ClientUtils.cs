// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace System.Windows.Forms
{
    static internal class ClientUtils
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
        private static Hashtable? enumValueInfo;
        public const int MAXCACHE = 300;  // we think we're going to get O(100) of these, put in a tripwire if it gets larger.

        private class SequentialEnumInfo
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

            if (enumValueInfo is null)
            {
                enumValueInfo = new Hashtable();
            }

            SequentialEnumInfo? sequentialEnumInfo = null;

            if (enumValueInfo.ContainsKey(t))
            {
                sequentialEnumInfo = enumValueInfo[t] as SequentialEnumInfo;
            }
            if (sequentialEnumInfo is null)
            {
                sequentialEnumInfo = new SequentialEnumInfo(t);

                if (enumValueInfo.Count > MAXCACHE)
                {
                    // see comment next to MAXCACHE declaration.
                    Debug.Fail("cache is too bloated, clearing out, we need to revisit this.");
                    enumValueInfo.Clear();
                }
                enumValueInfo[t] = sequentialEnumInfo;
            }
            if (minVal != sequentialEnumInfo.MinValue)
            {
                // put string allocation in the IF block so the common case doesnt build up the string.
                System.Diagnostics.Debug.Fail("Minimum passed in is not the actual minimum for the enum.  Consider changing the parameters or using a different function.");
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
