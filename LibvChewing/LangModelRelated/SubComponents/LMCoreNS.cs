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

using System.Text;
using Megrez;
using PListNet;
using PListNet.Nodes;

namespace LibvChewing {
/// <summary>
/// 與 LMCore 不同，LMCoreNS 直接讀取 plist。<br />
/// 這樣一來可以節省在舊 mac 機種內的資料讀入速度。<br />
/// 目前僅針對輸入法原廠語彙資料檔案使用 plist 格式。
/// </summary>
// 000
public struct LMCoreNS {
  /// <summary>
  /// 資料庫辭典。索引內容為經過加密的注音字串，資料內容則為 UTF8 資料陣列。
  /// </summary>
  private DictionaryNode _rangeMap = new();
  /// <summary>
  /// 啟用該選項的話，會強制施加預設權重、而無視原始權重資料。
  /// </summary>
  private readonly bool _shouldForceDefaultScore = false;
  /// <summary>
  /// 當某一筆資料內的權重資料毀損時，要施加的預設權重。
  /// </summary>
  private double _defaultScore = 0;
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
  /// <br />
  /// 某些參數在 LMCoreNS 內已作廢，但仍保留、以方便那些想用該專案源碼做實驗的人群。
  /// </summary>
  /// <param name="defaultScore">當某一筆資料內的權重資料毀損時，要施加的預設權重。</param>
  /// <param name="shouldForceDefaultScore">啟用該選項的話，會強制施加預設權重、而無視原始權重資料。</param>
  public LMCoreNS(double defaultScore = 0, bool shouldForceDefaultScore = false) {
    _rangeMap = new();
    _defaultScore = defaultScore;
    _shouldForceDefaultScore = shouldForceDefaultScore;
  }

  /// <summary>
  /// 將資料從檔案讀入至資料庫陣列內。
  /// </summary>
  /// <param name="path">給定路徑。</param>
  /// <returns>是否成功載入資料。</returns>
  public bool Open(string path) {
    if (IsLoaded) return false;
    try {
      Stream theStream = File.OpenRead(path);
      PNode theNode = PList.Load(theStream);
      _rangeMap = theNode as DictionaryNode ?? new();
    } catch (Exception e) {
      Console.WriteLine("↑ Exception happened when reading plist file at: {0}.", e);
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
  public List<Unigram> UnigramsFor(string strKey) {
    List<Unigram> grams = new();
    string strKeyEncrypted = Tools.CnvPhonabet2ASCII(strKey);
    if (!_rangeMap.ContainsKey(strKeyEncrypted)) return grams;
    ArrayNode arrRecords = _rangeMap[strKeyEncrypted] as ArrayNode ?? new();
    if (arrRecords.Count == 0) return grams;
    List<PNode> lstRecords = arrRecords.ToList();
    foreach (PNode? netaNode in lstRecords) {
      if (netaNode is not DataNode neta) continue;
      string strNeta = Encoding.UTF8.GetString(neta.Value);
      List<string> arrNeta = new(strNeta.Split(' ').Reverse());
      string strValue;
      double dblScore = _defaultScore;
      switch (arrNeta.Count) {
        case 0:
          continue;
        case 1:
          strValue = arrNeta[0];
          break;
        default:
          if (!_shouldForceDefaultScore) dblScore = CnvToDoubleScore(arrNeta[1]);
          strValue = arrNeta[0];
          break;
      }
      Unigram theGram = new(new(strKey, strValue), dblScore);
      grams.Add(theGram);
    }
    return grams;
  }

  /// <summary>
  /// 根據給定的讀音索引鍵來確認資料庫陣列內是否存在對應的資料。
  /// </summary>
  /// <param name="strKey">讀音索引鍵。</param>
  /// <returns>是否在庫。</returns>
  public bool HasUnigramsFor(string strKey) {
    if (_rangeMap.ContainsKey(Tools.CnvPhonabet2ASCII(strKey))) {
      return _rangeMap[Tools.CnvPhonabet2ASCII(strKey)] is ArrayNode;
    }
    return false;
  }

  /// <summary>
  /// 內部限定工具函式，分析給定的 string 然後取出裡面的 double 數值。否則返回 _defaultScore 的數值。
  /// </summary>
  /// <param name="value">給定的 string 字串。</param>
  /// <returns>取出的 double 數值。取值失敗的話，會返回 _defaultScore 的數值。</returns>
  private double CnvToDoubleScore(string value) {
    if (!double.TryParse(value, out double outVal)) return _defaultScore;
    // 前一行會生成 outVal 來供下一行讀取使用。
    return double.IsNaN(outVal) || double.IsInfinity(outVal) ? _defaultScore : outVal;
  }
}
}