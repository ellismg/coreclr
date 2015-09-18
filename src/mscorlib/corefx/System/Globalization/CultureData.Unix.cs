//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    internal partial class CultureData
    {
        const string LOCALE_NAME_SYSTEM_DEFAULT = @"!x-sys-default-locale";

        //ICU constants
        const int ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY = 100; // max size of keyword or value
        const int ICU_ULOC_FULLNAME_CAPACITY = 157;           // max size of locale name

        /// <summary>
        /// This method uses the sRealName field (which is initialized by the constructor before this is called) to
        /// initialize the rest of the state of CultureData based on the underlying OS globalization library.
        /// </summary>
        private unsafe bool InitCultureData()
        {
            Contract.Assert(this.sRealName != null);

            string realNameBuffer;
            if (this.sRealName == LOCALE_NAME_SYSTEM_DEFAULT)
            {
                realNameBuffer = null; //ICU uses null to obtain the default (system) locale
            }
            else
            {
                realNameBuffer = this.sRealName;
            }

            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_FULLNAME_CAPACITY);
            if (!Interop.GlobalizationInterop.GetLocaleName(realNameBuffer, sb, sb.Capacity))
            {
                StringBuilderCache.Release(sb);
                return false; // Fail
            }

            // Success, so use the locale name returned
            this.sRealName = StringBuilderCache.GetStringAndRelease(sb);
            realNameBuffer = this.sRealName;

            this.sWindowsName = realNameBuffer;
            this.sName = this.sWindowsName;

            this.sSpecificCulture = this.sWindowsName; // we don't attempt to find a non-neutral locale if a neutral is passed in (unlike win32)

            this.iLanguage = this.ILANGUAGE;
            this.bNeutral = this.SISO3166CTRYNAME.Length == 0;

            return true;
        }
 
        private string GetLocaleInfo(LocaleStringData type)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.GetLocaleInfo] Expected this.sWindowsName to be populated already");
            return GetLocaleInfo(this.sWindowsName, type);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        private string GetLocaleInfo(string localeName, LocaleStringData type)
        {
            Contract.Assert(localeName != null, "[CultureData.GetLocaleInfo] Expected localeName to be not be null");

            switch (type)
            {
                case LocaleStringData.NegativeInfinitySymbol:
                    // not an equivalent in ICU; should we remove support for this property?
                    return string.Format("{0}{1}",
                        GetLocaleInfo(localeName, LocaleStringData.NegativeSign),
                        GetLocaleInfo(localeName, LocaleStringData.PositiveInfinitySymbol));
            }

            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY);

            bool result = Interop.GlobalizationInterop.GetLocaleInfoString(localeName, (uint)type, sb, sb.Capacity);
            if (!result)
            {
                // Failed, just use empty string
                StringBuilderCache.Release(sb);
                Contract.Assert(false, "[CultureData.GetLocaleInfo(LocaleStringData)] Failed");
                return String.Empty;
            }
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        private int GetLocaleInfo(LocaleNumberData type)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.GetLocaleInfo(LocaleNumberData)] Expected this.sWindowsName to be populated already");

            return GetLocaleIntInfo((uint)type);
        }

        private int GetLocaleIntInfo(uint type)
        {
            int value = 0;
            bool result = Interop.GlobalizationInterop.GetLocaleInfoInt(this.sWindowsName, type, ref value);
            if (!result)
            {
                // Failed, just use 0
                Contract.Assert(false, "[CultureData.GetLocaleInfo(LocaleNumberData)] failed");
            }
            return value;
        }

        private int[] GetLocaleInfo(LocaleGroupingData type)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.GetLocaleInfo(LocaleGroupingData)] Expected this.sWindowsName to be populated already");

            int primaryGroupingSize = 0;
            int secondaryGroupingSize = 0;
            bool result = Interop.GlobalizationInterop.GetLocaleInfoGroupingSizes(this.sWindowsName, (uint)type, ref primaryGroupingSize, ref secondaryGroupingSize);
            if (!result)
            {
                Contract.Assert(false, "[CultureData.GetLocaleInfo(LocaleGroupingData type)] failed");
            }

            if (secondaryGroupingSize == 0)
            {
                return new int[] { primaryGroupingSize };
            }

            return new int[] { primaryGroupingSize, secondaryGroupingSize };
        }

        private string GetTimeFormatString()
        {
            return GetTimeFormatString(false);
        }

        private string GetTimeFormatString(bool shortFormat)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.GetTimeFormatString(bool shortFormat)] Expected this.sWindowsName to be populated already");

            StringBuilder sb = StringBuilderCache.Acquire(ICU_ULOC_KEYWORD_AND_VALUES_CAPACITY);

            bool result = Interop.GlobalizationInterop.GetLocaleTimeFormat(this.sWindowsName, shortFormat, sb, sb.Capacity);
            if (!result)
            {
                // Failed, just use empty string
                StringBuilderCache.Release(sb);
                Contract.Assert(false, "[CultureData.GetTimeFormatString(bool shortFormat)] Failed");
                return String.Empty;
            }
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        private int GetFirstDayOfWeek()
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.GetFirstDayOfWeek()] Expected this.sWindowsName to be populated already");

            const uint LOCALE_IFIRSTDAYOFWEEK = 0x0000100C;
            int value = this.GetLocaleIntInfo(LOCALE_IFIRSTDAYOFWEEK);
            return value;
        }

        private String[] GetTimeFormats()
        {
            string format = GetTimeFormatString(false);
            return new string[] { format };
        }

        private String[] GetShortTimeFormats()
        {
            string format = GetTimeFormatString(true);
            return new string[] { format };
        }

        private static CultureData GetCultureDataFromRegionName(String regionName)
        {
            // no support to lookup by region name, other than the hard-coded list in CultureData
            return null;
        }

        private static string GetLanguageDisplayName(string cultureName)
        {
            return new CultureInfo(cultureName).m_cultureData.GetLocaleInfo(cultureName, LocaleStringData.LocalizedDisplayName);
        }

        private static string GetRegionDisplayName(string isoCountryCode)
        {
            // use the fallback which is to return NativeName
            return null;
        }

        private static CultureInfo GetUserDefaultCulture()
        {
            return new CultureInfo(LOCALE_NAME_SYSTEM_DEFAULT);
        }

        private static bool IsCustomCultureId(int cultureId)
        {
            const int LOCALE_CUSTOM_DEFAULT = 0x0c00;
            const int LOCALE_CUSTOM_UNSPECIFIED = 0x1000;

            return (cultureId == LOCALE_CUSTOM_DEFAULT || cultureId == LOCALE_CUSTOM_UNSPECIFIED);
        }
    }
}
