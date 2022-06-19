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

namespace LibvChewing {
public static class Tools {
  /// <summary>
  /// 內部函式，用以將注音讀音索引鍵進行加密，也可藉由 IsReverse 參數解密。<br />
  /// <br />
  /// 使用這種加密字串作為索引鍵，可以增加對 plist 資料庫的存取速度。<br />
  /// <br />
  /// 如果傳入的字串當中包含 ASCII 下畫線符號的話，則表明該字串並非注音讀音字串，會被忽略處理。
  /// </summary>
  /// <param name="Incoming">傳入的未加密（或已加密）注音讀音字串。</param>
  /// <param name="IsReverse">如果要解密的話，請將此設為 true </param>
  /// <returns>藉由轉換過程得到的已加密（或未加密）的注音讀音字串。</returns>
  public static string CnvPhonabet2ASCII(string Incoming, bool IsReverse = false) {
    if (Incoming.Contains('_')) return new("");
    return DicPhonabet2ASCII.Aggregate(
        Incoming,
        (current, neta) => current.Replace(IsReverse ? neta.Item2 : neta.Item1, IsReverse ? neta.Item1 : neta.Item2));
  }

  private readonly static (string, string)[] DicPhonabet2ASCII = {
    ("ㄅ", "b"), ("ㄆ", "p"), ("ㄇ", "m"), ("ㄈ", "f"), ("ㄉ", "d"), ("ㄊ", "t"), ("ㄋ", "n"), ("ㄌ", "l"), ("ㄍ", "g"),
    ("ㄎ", "k"), ("ㄏ", "h"), ("ㄐ", "j"), ("ㄑ", "q"), ("ㄒ", "x"), ("ㄓ", "Z"), ("ㄔ", "C"), ("ㄕ", "S"), ("ㄖ", "r"),
    ("ㄗ", "z"), ("ㄘ", "c"), ("ㄙ", "s"), ("ㄧ", "i"), ("ㄨ", "u"), ("ㄩ", "v"), ("ㄚ", "a"), ("ㄛ", "o"), ("ㄜ", "e"),
    ("ㄝ", "E"), ("ㄞ", "B"), ("ㄟ", "P"), ("ㄠ", "M"), ("ㄡ", "F"), ("ㄢ", "D"), ("ㄣ", "T"), ("ㄤ", "N"), ("ㄥ", "L"),
    ("ㄦ", "R"), ("ˊ", "2"),  ("ˇ", "3"),  ("ˋ", "4"),  ("˙", "5")
  };

  /// <summary>
  /// 往終端列印偵錯報告。如果偵錯模式沒有開啟的話，則不列印。
  /// </summary>
  /// <param name="content">要列印的內容。</param>
  public static void PrintDebugIntel(string content) {
    if (!Prefs.IsDebugModeEnabled) return;
    content = "vChewingDebug: " + content;
    Console.WriteLine(content);
  }

  /// <summary>
  /// 按需轉換漢字。
  /// </summary>
  /// <param name="text">要轉換的漢字字串。</param>
  /// <returns>轉換結果。</returns>
  public static string KanjiConversionIfRequired(string text) {
    if (Prefs.CurrentInputMode != InputMode.ImeModeCHT) return text;
    // TODO: 回頭在這裡擴充「康熙字/JIS漢字轉換」的功能。
    return text;
  }

  public static int GetRangeLength(Range range) => range.End.Value - range.Start.Value;
}
}