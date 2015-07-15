//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <assert.h>
#include <stdint.h>
#include <unicode/uchar.h>
#include <unicode/ucol.h>
#include <unicode/usearch.h>
#include <unicode/utf16.h>

const int32_t CompareOptionsIgnoreCase = 1;
// const int32_t CompareOptionsIgnoreNonSpace = 2;
// const int32_t CompareOptionsIgnoreSymbols = 4;
// const int32_t CompareOptionsIgnoreKanaType = 8;
// const int32_t CompareOptionsIgnoreWidth = 0x10;
// const int32_t CompareOptionsStringSort = 0x20000000;

/*
 * To collator returned by this function is owned by the callee and must be closed when this method returns
 * with a U_SUCCESS UErrorCode.
 *
 * On error, the return value is undefined.
 */
UCollator* GetCollatorForLocaleAndOptions(const char* lpLocaleName, int32_t options, UErrorCode* pErr)
{
    UCollator* pColl = nullptr;

    pColl = ucol_open(lpLocaleName, pErr);

    if ((options & CompareOptionsIgnoreCase) == CompareOptionsIgnoreCase)
    {
        ucol_setAttribute(pColl, UCOL_STRENGTH, UCOL_SECONDARY, pErr);
    }

    return pColl;
}

/*
Function:
CompareString
*/
extern "C" int32_t CompareString(const char* lpLocaleName, const UChar* lpStr1, int32_t cwStr1Length, const UChar* lpStr2, int32_t cwStr2Length, int32_t options)
{
    UCollationResult result = UCOL_EQUAL;
    UErrorCode err = U_ZERO_ERROR;
    UCollator* pColl = GetCollatorForLocaleAndOptions(lpLocaleName, options, &err);

    if (U_SUCCESS(err))
    {
        result = ucol_strcoll(pColl, lpStr1, cwStr1Length, lpStr2, cwStr2Length);
        ucol_close(pColl);
    }

    return result;
}

/*
Function:
IndexOf
*/
extern "C" int32_t IndexOf(const char* lpLocaleName, const UChar* lpTarget, const UChar* lpSource, int32_t cwSourceLength, int32_t options)
{
    int32_t result = USEARCH_DONE;
    UErrorCode err = U_ZERO_ERROR;
    UCollator* pColl = GetCollatorForLocaleAndOptions(lpLocaleName, options, &err);

    if (U_SUCCESS(err))
    {
        UStringSearch* pSearch = usearch_openFromCollator(lpTarget, -1, lpSource, cwSourceLength, pColl, nullptr, &err);

        if (U_SUCCESS(err))
        {
            result = usearch_first(pSearch, &err);
            usearch_close(pSearch);
        }

        ucol_close(pColl);
    }

    return result;
}

/*
Function:
LastIndexOf
*/
extern "C" int32_t LastIndexOf(const char* lpLocaleName, const UChar* lpTarget, const UChar* lpSource, int32_t cwSourceLength, int32_t options)
{
    int32_t result = USEARCH_DONE;
    UErrorCode err = U_ZERO_ERROR;
    UCollator* pColl = GetCollatorForLocaleAndOptions(lpLocaleName, options, &err);

    if (U_SUCCESS(err))
    {
        UStringSearch* pSearch = usearch_openFromCollator(lpTarget, -1, lpSource, cwSourceLength, pColl, nullptr, &err);

        if (U_SUCCESS(err))
        {
            result = usearch_last(pSearch, &err);
            usearch_close(pSearch);
        }

        ucol_close(pColl);
    }

    return result;
}

/*
 Return value is a "BOOL" (1 = true, 0 = false)
 */
extern "C" int32_t EndsWith(const char* lpLocaleName, const UChar* lpTarget, const UChar* lpSource, int32_t cwSourceLength, int32_t options)
{
    int32_t result = FALSE;
    UErrorCode err = U_ZERO_ERROR;
    UCollator* pColl = GetCollatorForLocaleAndOptions(lpLocaleName, options, &err);

    if (U_SUCCESS(err))
    {
        UStringSearch* pSearch = usearch_openFromCollator(lpTarget, -1, lpSource, cwSourceLength, pColl, nullptr, &err);
        int32_t idx = USEARCH_DONE;

        if (U_SUCCESS(err))
        {
            idx = usearch_last(pSearch, &err);

            if (idx != USEARCH_DONE)
            {
                if ((idx + usearch_getMatchedLength(pSearch)) == cwSourceLength)
                {
                    result = TRUE;
                }
            }

            usearch_close(pSearch);
        }

        ucol_close(pColl);
    }

    return result;
}

extern "C" int32_t GetSortKey(const char* lpLocaleName, const UChar* lpStr, int32_t cwStrLength, uint8_t* sortKey, int32_t cbSortKeyLength, int32_t options)
{
    UErrorCode err = U_ZERO_ERROR;
    UCollator* pColl = GetCollatorForLocaleAndOptions(lpLocaleName, options, &err);
    int32_t result = 0;

    if (U_SUCCESS(err))
    {
        result = ucol_getSortKey(pColl, lpStr, cwStrLength, sortKey, cbSortKeyLength);
    }

    return result;
}

extern "C" int32_t CompareStringOrdinalIgnoreCase(const UChar* lpStr1, int32_t cwStr1Length, const UChar* lpStr2, int32_t cwStr2Length)
{
    int32_t str1Idx = 0;
    int32_t str2Idx = 0;

    while (str1Idx < cwStr1Length && str2Idx < cwStr2Length)
    {
        UChar32 str1Codepoint;
        UChar32 str2Codepoint;

        U16_NEXT(lpStr1, str1Idx, cwStr1Length, str1Codepoint);
        U16_NEXT(lpStr2, str2Idx, cwStr2Length, str2Codepoint);

        if (str1Codepoint != str2Codepoint && u_toupper(str1Codepoint) != u_toupper(str2Codepoint))
        {
            return str1Codepoint < str2Codepoint ? -1 : 1;
        }
    }

    if (cwStr1Length < cwStr2Length)
    {
        return -1;
    }

    if (cwStr2Length < cwStr1Length)
    {
        return 1;
    }

    return 0;
}
