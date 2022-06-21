// (c) 2022 and onwards The vChewing Project (MIT-NTL License).
/*
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

1. The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

2. No trademark license is granted to use the trade names, trademarks, service
marks, or product names of Contributor, except as required to fulfill notice
requirements above.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Globalization;

namespace LibvChewing {
public class U8Utils {
  /// <summary>
  /// This function is equivalent to string.components(separatedBy: "") in Swift,
  /// returning a List of all characters used in the string with surrogate-pairs kept intact.
  /// </summary>
  /// <param name="text">input string.</param>
  /// <returns>A List of results.</returns>
  public static List<string> GetU8Elements(string text) {
    List<string> result = new();
    TextElementEnumerator charEnum = StringInfo.GetTextElementEnumerator(text);
    while (charEnum.MoveNext()) {
      result.Add(charEnum.GetTextElement());
    }
    return result;
  }

  /// <summary>
  /// This function is equivalent to string.count in Swift,
  /// returning the count of all characters used in the string with surrogate-pairs counted correctly.
  /// </summary>
  /// <param name="text">input string.</param>
  /// <returns>Literal count of characters. A surrogate pair won't be counted as separated UTF16 chars.</returns>
  public static int GetU8Length(string text) => new StringInfo(text).LengthInTextElements;

  // MARK: - Functions migrated from StringUtils shipped by vChewing for macOS.

  /// Shiki's Notes: The cursor index in the IMK inline composition buffer
  /// still uses UTF16 index measurements. This means that any attempt of
  /// using Swift native UTF8 handlings to replace Zonble's NSString (or
  /// .utf16) handlings below will still result in unavoidable necessities
  /// of solving the UTF16->UTF8 conversions in another approach. Therefore,
  /// I strongly advise against any attempt of such until the day that IMK is
  /// capable of handling the cursor index in its inline composition buffer using
  /// UTF8 measurements.

  /// <summary>
  /// Converts the index in a C# string to the literal index in a UTF8 string.<br />
  /// An Emoji might be compose by more than one UTF-16 code points. However,
  /// the length of a C# string is only the sum of the UTF-16 code points. The
  /// method helps to find the index in a UTF8 string by passing the index
  /// in a C# string.
  /// </summary>
  /// <param name="text">Incoming string to deal with.</param>
  /// <param name="utf16Index">UTF16 index value passed in.</param>
  /// <returns>Literal index calculated by this function.</returns>
  public static int GetCharIndexLiteral(string text, int utf16Index) {
    int length = 0;
    int i = 0;
    foreach (string theChar in GetU8Elements(text)) {
      length += theChar.Length;
      if (length > utf16Index) {
        return i;
      }
      i++;
    }
    return GetU8Length(text);
  }

  public static int GetU16NextPositionFor(string text, int index) {
    int fixedIndex = Math.Min(GetCharIndexLiteral(text, index) + 1, GetU8Length(text));
    return GetU8Elements(text).GetRange(0, fixedIndex).Sum(x => x.Length);
  }

  public static int GetU16PreviousPositionFor(string text, int index) {
    int fixedIndex = Math.Max(GetCharIndexLiteral(text, index) - 1, 0);
    return GetU8Elements(text).GetRange(0, fixedIndex).Sum(x => x.Length);
  }

  public static string GetU16Substring(string text, int index1, int index2) {
    int startIndex = Math.Min(index1, index2);
    int endIndex = Math.Max(index1, index2);
    return text.Substring(startIndex, endIndex - startIndex);
  }

  public static string GetU8Substring(string text, int index1, int index2) {
    int startIndex = Math.Min(index1, index2);
    int endIndex = Math.Max(index1, index2);
    List<string> elements = GetU8Elements(text).GetRange(startIndex, endIndex - startIndex);
    return string.Join("", elements);
  }
}
}
