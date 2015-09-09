//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <unicode/dtfmtsym.h>

/*
* These values should be kept in sync with System.Globalization.CalendarId
*/
enum CalendarId : int16_t
{
	UNINITIALIZED_VALUE = 0,
	GREGORIAN = 1,     // Gregorian (localized) calendar
	GREGORIAN_US = 2,     // Gregorian (U.S.) calendar
	JAPAN = 3,     // Japanese Emperor Era calendar
	/* SSS_WARNINGS_OFF */
	TAIWAN = 4,     // Taiwan Era calendar /* SSS_WARNINGS_ON */ 
	KOREA = 5,     // Korean Tangun Era calendar
	HIJRI = 6,     // Hijri (Arabic Lunar) calendar
	THAI = 7,     // Thai calendar
	HEBREW = 8,     // Hebrew (Lunar) calendar
	GREGORIAN_ME_FRENCH = 9,     // Gregorian Middle East French calendar
	GREGORIAN_ARABIC = 10,     // Gregorian Arabic calendar
	GREGORIAN_XLIT_ENGLISH = 11,     // Gregorian Transliterated English calendar
	GREGORIAN_XLIT_FRENCH = 12,
	// Note that all calendars after this point are MANAGED ONLY for now.
	JULIAN = 13,
	JAPANESELUNISOLAR = 14,
	CHINESELUNISOLAR = 15,
	SAKA = 16,     // reserved to match Office but not implemented in our code
	LUNAR_ETO_CHN = 17,     // reserved to match Office but not implemented in our code
	LUNAR_ETO_KOR = 18,     // reserved to match Office but not implemented in our code
	LUNAR_ETO_ROKUYOU = 19,     // reserved to match Office but not implemented in our code
	KOREANLUNISOLAR = 20,
	TAIWANLUNISOLAR = 21,
	PERSIAN = 22,
	UMALQURA = 23,
	LAST_CALENDAR = 23      // Last calendar ID
};

/*
Function:
GetCalendarId

Gets the associated CalendarId for the ICU calendar name.
*/
CalendarId GetCalendarId(const char* calendarName);

