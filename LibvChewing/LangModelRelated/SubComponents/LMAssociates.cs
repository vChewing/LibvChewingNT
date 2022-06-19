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

using Microsoft.VisualBasic.FileIO;

namespace LibvChewing {
/// <summary>
/// LMAssociates 在 LibvChewingNT 當中會利用 TextFieldParser 來讀取 txt 格式的辭典檔案。<br />
/// 目前僅針對輸入法使用者語彙資料檔案使用 txt 格式。
/// </summary>
// 000
public struct LMAssociates {
  /// <summary>
  /// 資料庫辭典。
  /// </summary>
  private Dictionary<string, List<string>> _rangeMap = new();
  /// <summary>
  /// 資料陣列內承載的資料筆數。
  /// </summary>
  public int Count => _rangeMap.Count;
  /// <summary>
  /// 檢測資料庫陣列內是否已經有載入的資料。
  /// </summary>
  public bool IsLoaded => _rangeMap.Count != 0;
  /// <summary>
  /// 初期化該語言模型。<br />
  /// </summary>
  public LMAssociates() {}

  /// <summary>
  /// 將資料從檔案讀入至資料庫陣列內。
  /// </summary>
  /// <param name="path">給定路徑。</param>
  /// <returns>是否成功載入資料。</returns>
  public bool Open(string path) {
    if (IsLoaded) return false;
    LMConsolidator.FixEOF(path);
    LMConsolidator.Consolidate(path, shouldCheckPragma: true);
    try {
      using (TextFieldParser parser = new(path)) {
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(" ");
        while (!parser.EndOfData) {
          string[]? fields = parser.ReadFields();
          if (fields == null) continue;
          if (fields.Length < 2) continue;
          if (fields[0].First() == '#') continue;
          if (fields[0].Length == 0 || fields[1].Length == 0) continue;
          string strKey = fields[0];
          string strValue = fields[1];
          if (!_rangeMap.ContainsKey(strKey)) _rangeMap[strKey] = new();  // 給缺失的記錄位置先插一個空白陣列。
          _rangeMap[strKey].Add(strValue);
        }
      }
      if (_rangeMap.Count == 0) {
        Console.WriteLine("↑ Nothing read from: {0}.", path);
        return false;
      }
    } catch (Exception e) {
      Console.WriteLine("↑ Exception happened when reading txt dictionary file at: {0}.", e);
      return false;
    }
    return true;
  }

  /// <summary>
  /// 將當前語言模組的資料庫陣列自記憶體內卸除。
  /// </summary>
  public void Close() {
    if (IsLoaded) _rangeMap = new();
  }

  // MARK: - Advanced features

  /// <summary>
  /// 根據給定的讀音索引鍵，來獲取資料庫陣列內的對應資料陣列的 UTF8 資料、就地分析、生成單元圖陣列。
  /// </summary>
  /// <param name="strKey">讀音索引鍵。</param>
  /// <returns>單元圖陣列。</returns>
  public IEnumerable<string> EntriesFor(string strKey) {
    return _rangeMap.ContainsKey(strKey) ? _rangeMap[strKey] : new();
  }

  /// <summary>
  /// 根據給定的讀音索引鍵來確認資料庫陣列內是否存在對應的資料。
  /// </summary>
  /// <param name="strKey">讀音索引鍵。</param>
  /// <returns>是否在庫。</returns>
  public bool HasEntriesFor(string strKey) => _rangeMap.ContainsKey(strKey);
}
}