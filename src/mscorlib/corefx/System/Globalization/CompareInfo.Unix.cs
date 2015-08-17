// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        // ICU uses a char* (UTF-8) to represent a locale name.
        private readonly byte[] m_sortNameAsUtf8;

        internal unsafe CompareInfo(CultureInfo culture)
        {
            m_name = culture.m_name;
            m_sortName = culture.SortName;

            m_sortNameAsUtf8 = System.Text.Encoding.UTF8.GetBytes(this.m_sortName);
        }

        internal static int IndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Contract.Assert(source != null);
            Contract.Assert(value != null);

            // TODO: Implement This Fully.

            if (value.Length == 0)
            {
                return startIndex;
            }

            if (ignoreCase)
            {
                source = source.ToUpper(CultureInfo.InvariantCulture);
                value = value.ToUpper(CultureInfo.InvariantCulture);
            }

            source = source.Substring(startIndex, count);

            for (int i = 0; i + value.Length <= source.Length; i++)
            {
                for (int j = 0; j < value.Length; j++) {
                   if (source[i + j] != value[j]) {
                       break;
                   }

                   if (j == value.Length - 1) {
                       return i + startIndex;
                   }
                }
            }

            return -1;
        }

        internal static int LastIndexOfOrdinal(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Contract.Assert(source != null);
            Contract.Assert(value != null);

            // TODO: Implement This Fully.

            if (value.Length == 0)
            {
                return startIndex;
            }

            if (ignoreCase)
            {
                source = source.ToUpper(CultureInfo.InvariantCulture);
                value = value.ToUpper(CultureInfo.InvariantCulture);
            }

            source = source.Substring(startIndex - count + 1, count);

            int last = -1;

            int cur = 0;
            while ((cur = IndexOfOrdinal(source, value, last + 1, source.Length - last - 1, false)) != -1)
            {
                last = cur;
            }

            return last >= 0 ?
                last + startIndex - count + 1 :
                -1;
        }

        private unsafe int GetHashCodeOfStringCore(string source, CompareOptions options)
        {
            Contract.Assert(source != null);
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            int sortKeyLength = Interop.GlobalizationInterop.GetSortKey(m_sortNameAsUtf8, source, source.Length, null, 0, options);

            // Have the sort key be a multiple of 4 so we can treat it as a sequence of Int32's when we hash it.
            sortKeyLength = ((sortKeyLength + 3) & ~1);

            byte[] sortKey = new byte[sortKeyLength];

            Interop.GlobalizationInterop.GetSortKey(m_sortNameAsUtf8, source, source.Length, sortKey, sortKey.Length, options);

            // TODO: We should be using randomized hashing (via Marvin32) here to collapse the sort key
            // into a hash value.
            int hash = 5381;

            unchecked
            {
                for (int i = 0; i < sortKey.Length - 4; i += 4)
                {
                    int cur = BitConverter.ToInt32(sortKey, i);
                    hash = ((hash << 5) + hash) + i;
                }
            }

            return hash;
        }

        [System.Security.SecuritySafeCritical]
        private static unsafe int CompareStringOrdinalIgnoreCase(char* string1, int count1, char* string2, int count2)
        {
            return Interop.GlobalizationInterop.CompareStringOrdinalIgnoreCase(string1, count1, string2, count2);
        }

        [System.Security.SecuritySafeCritical]
        private unsafe int CompareString(string string1, int offset1, int length1, string string2, int offset2, int length2, CompareOptions options)
        {
            Contract.Assert(string1 != null);
            Contract.Assert(string2 != null);
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            fixed (char* pString1 = string1)
            {
                fixed (char* pString2 = string2)
                {
                    return Interop.GlobalizationInterop.CompareString(m_sortNameAsUtf8, pString1 + offset1, length1, pString2 + offset2, length2, options);
                }
            }
        }

        [System.Security.SecuritySafeCritical]
        private unsafe int IndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(target != null);
            Contract.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            if (options == CompareOptions.Ordinal)
            {
                return IndexOfOrdinal(source, target, startIndex, count, ignoreCase: false);
            }

            fixed (char* pSource = source)
            {
                return Interop.GlobalizationInterop.IndexOf(m_sortNameAsUtf8, target, pSource + startIndex, count, options);
            }
        }

        private unsafe int LastIndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(target != null);
            Contract.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            if (options == CompareOptions.Ordinal)
            {
                return LastIndexOfOrdinal(source, target, startIndex, count, ignoreCase: false);
            }

            fixed (char* pSource = source)
            {
                return Interop.GlobalizationInterop.LastIndexOf(m_sortNameAsUtf8, target, pSource + startIndex, count, options);
            }
        }

        private bool StartsWith(string source, string prefix, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(!string.IsNullOrEmpty(prefix));
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            // ICU doesn't appear to have a better way of testing if a string starts with another short of doing a regular
            // index of check.
            return IndexOfCore(source, prefix, 0, source.Length, options) == 0;
        }

        private bool EndsWith(string source, string suffix, CompareOptions options)
        {
            Contract.Assert(!string.IsNullOrEmpty(source));
            Contract.Assert(!string.IsNullOrEmpty(suffix));
            Contract.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            // Unlike StartsWith, we do EndsWith inside the native ICU wrapper, because we need to understand the length of the match
            // as well as the index in order to figure out if suffix is actually at the end of the string.
            return Interop.GlobalizationInterop.EndsWith(m_sortNameAsUtf8, suffix, source, source.Length, options);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
