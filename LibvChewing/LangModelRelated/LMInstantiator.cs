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

namespace LibvChewing {
/// <summary>
/// <para>語言模組副本化模組（LMInstantiator，下稱「LMI」）自身為符合天權星組字引擎內
/// 的 LanguageModel 協定的模組、統籌且整理來自其它子模組的資料（包括使用者語彙、
/// 繪文字模組、語彙濾除表、原廠語言模組等）。</para>
/// <para>LMI 型別為與輸入法按鍵調度模組直接溝通之唯一語言模組。當組字器開始根據給定的
/// 讀音鏈構築語句時，LMI 會接收來自組字器的讀音、輪流檢查自身是否有可以匹配到的
/// 單元圖結果，然後將結果整理為陣列、再回饋給組字器。</para>
/// <para>LMI 還會在將單元圖結果整理成陣列時做出下述處理轉換步驟：</para>
/// <list type="number">
/// <item>獲取原始結果陣列。</item>
/// <item>如果有原始結果也出現在濾除表當中的話，則自結果陣列丟棄這類結果。</item>
/// <item>如果啟用了語彙置換的話，則對目前經過處理的結果陣列套用語彙置換。</item>
/// <item>擁有相同讀音與詞語資料值的單元圖只會留下權重最大的那一筆，其餘重複值會被丟棄。</item>
/// </list>
/// <para>LMI 會根據需要分別載入原廠語言模組和其他個別的子語言模組。LMI 本身不會記錄這些
/// 語言模組的相關資料的存放位置，僅藉由參數來讀取相關訊息。</para>
/// </summary>
public class LMInstantiator : LanguageModel {
  public static bool ShowDebugOutput = true;
  // 在函式內部用以記錄狀態的開關。
  public bool isPhraseReplacementEnabled = false;
  public bool isCNSEnabled = false;
  public bool isSymbolEnabled = false;

  // 介紹一下幾個通用的語言模組型別：
  // ----------------------
  // LMCoreEX 是全功能通用型的模組，每一筆辭典記錄以 key 為注音、以 [Unigram] 陣列作為記錄內容。
  // 比較適合那種每筆記錄都有不同的權重數值的語言模組，雖然也可以強制施加權重數值就是了。
  // LMCoreEX 的辭典陣列不承載 Unigram 本體、而是承載索引範圍，這樣可以節約記憶體。
  // 一個 LMCoreEX 就可以滿足威注音幾乎所有語言模組副本的需求，當然也有這兩個例外：
  // ulmReplacements 與 ulmAssociates 分別擔當語彙置換表資料與使用者聯想詞的資料承載工作。
  // 但是，LMCoreEX 對 2010-2013 年等舊 mac 機種而言，讀取速度異常緩慢。
  // 於是 LMCoreNS 就出場了，專門用來讀取原廠的 plist 格式的辭典。
  // 後來又寫了 C# 版本的時候，最開始的 LMCore 可以用 .NET 6 內建的東西來迅速完成。

  // 接下來聲明原廠語言模組：
  public LMCoreNS lmCore = new(defaultScore: -9.9);  // 核心模組。
  public LMCoreNS lmMisc = new(defaultScore: -1);    // 注音文模組。

  // 簡體中文模式與繁體中文模式共用全字庫 (CNS) 擴展模組，故靜態處理。Symbols 同理。
  // 不然，每個模式都會讀入一份全字庫，會多佔用 100MB 記憶體。
  public static LMCoreNS lmCNS = new(defaultScore: -11);
  public static LMCoreNS lmSymbols = new(defaultScore: -13);

  // 聲明使用者語言模組。
  // Reverse 的話，第一欄是注音，第二欄是對應的漢字，第三欄是可能的權重。
  // 不 Reverse 的話，第一欄是漢字，第二欄是對應的注音，第三欄是可能的權重。
  public LMCore ulmPhrases =
      new(shouldReverse: true, shouldConsolidate: true, defaultScore: 0, shouldForceDefaultScore: true);
  public LMCore ulmFiltered =
      new(shouldReverse: true, shouldConsolidate: true, defaultScore: 0, shouldForceDefaultScore: true);
  public LMCore ulmSymbols =
      new(shouldReverse: true, shouldConsolidate: true, defaultScore: -12, shouldForceDefaultScore: true);
  public LMReplacements ulmReplacements = new();
  public LMAssociates ulmAssociates = new();

  /// <summary>
  /// 測試函式，用來統計該副本內各個子模組的資料載入狀況。<br />
  /// 該函式僅用於單元測試，若要修改的話、請同步修改對應的單元測試內容。
  /// </summary>
  public StringBuilder DataLoadingStatistics() {
    StringBuilder strOutput = new();
    strOutput.Append(
        $" - 原廠資料：字詞 {lmCore.Count}, 注音文 {lmMisc.Count}, 全字庫 {lmCNS.Count}, 符號 {lmSymbols.Count}\n");
    strOutput.Append(
        $" - 自訂資料：字詞 {ulmPhrases.Count}, 濾除表 {ulmFiltered.Count}, 置換項 {ulmReplacements.Count}, 符號 {ulmSymbols.Count}, 聯想 {ulmAssociates.Count}");
    return strOutput;
  }

  // MARK: - 工具函式

  // 我們在這裡簡化處理，因為所有與檔案讀取有關的錯誤都被子語言模組自己處理了。

  public bool IsLanguageModelLoaded => lmCore.IsLoaded;
  public void LoadLanguageModel(string path) => lmCore.Open(path);

  public bool IsCNSDataLoaded => lmCNS.IsLoaded;
  public void LoadCNSData(string path) => lmCNS.Open(path);

  public bool IsMiscDataLoaded => lmMisc.IsLoaded;
  public void LoadMiscData(string path) => lmMisc.Open(path);

  public bool IsSymbolDataLoaded => lmSymbols.IsLoaded;
  public void LoadSymbolData(string path) => lmSymbols.Open(path);

  public void LoadUserPhrasesData(string path, string? filterPath) {
    ulmPhrases.Open(path, reopen: true);
    if (filterPath == null) return;
    ulmFiltered.Open(filterPath, reopen: true);
  }

  public void LoadUserSymbolData(string path) => ulmSymbols.Open(path, reopen: true);

  public void LoadUserAssociatesData(string path) => ulmAssociates.Open(path, reopen: true);

  public void LoadUserReplacementsData(string path) => ulmReplacements.Open(path, reopen: true);

  // MARK: - 核心函式（對外）

  // 威注音輸入法目前尚未具備對雙元圖的處理能力，故停用該函式。
  /// <summary>
  /// 【已作廢】根據給定的讀音索引鍵與前述讀音索引鍵，生成雙單元圖陣列。
  /// </summary>
  /// <param name="key">讀音索引鍵。</param>
  /// <param name="precedingKey">前述讀音索引鍵。</param>
  /// <returns>雙元圖陣列。</returns>
  public List<Bigram> BigramsForKeys(string precedingKey, string key) { return new(); }

  /// <summary>
  /// 給定讀音字串，讓 LMI 給出對應的經過處理的單元圖陣列。
  /// </summary>
  /// <param name="key">給定的讀音字串。</param>
  /// <returns>對應的經過處理的單元圖陣列。</returns>
  public List<Unigram> UnigramsFor(string key) {
    if (key == " ") return new() { new(new(" ", " "), 0) };

    // 準備不同的語言模組容器，開始逐漸往容器陣列內塞入資料。
    List<Unigram> rawAllUnigrams = new();

    // 用 reversed 指令讓使用者語彙檔案內的詞條優先順序隨著行數增加而逐漸增高。
    // 這樣一來就可以在就地新增語彙時徹底複寫優先權。
    // 將兩句差分也是為了讓 rawUserUnigrams 的類型不受可能的影響。
    // 與 Swift 不同，.Reverse() 在 C# 內是自函式，所以得單獨執行。
    rawAllUnigrams.AddRange(ulmPhrases.UnigramsFor(key));
    rawAllUnigrams.Reverse();

    // LMMisc 與 LMCore 的 score 在 (-10.0, 0.0) 這個區間內。
    rawAllUnigrams.AddRange(lmMisc.UnigramsFor(key));
    rawAllUnigrams.AddRange(lmCore.UnigramsFor(key));

    if (isCNSEnabled) rawAllUnigrams.AddRange(lmCNS.UnigramsFor(key));

    if (isSymbolEnabled) {
      rawAllUnigrams.AddRange(ulmSymbols.UnigramsFor(key));
      rawAllUnigrams.AddRange(lmSymbols.UnigramsFor(key));
    }

    // 準備過濾清單。因為我們在 Swift 使用 NSOrderedSet，所以就不需要統計清單了。
    HashSet<KeyValuePaired> filteredPairs = new();

    // 載入要過濾的 KeyValuePair 清單。
    foreach (Unigram unigram in ulmFiltered.UnigramsFor(key)) {
      filteredPairs.Add(unigram.KeyValue);
    }

    return FilterAndTransform(rawAllUnigrams, filteredPairs);
  }

  /// <summary>
  /// 根據給定的讀音索引鍵來確認資料庫陣列內是否存在對應的資料。
  /// </summary>
  /// <param name="key">讀音索引鍵。</param>
  /// <returns>是否在庫。</returns>
  public bool HasUnigramsFor(string key) {
    if (key == " ") return true;
    if (!ulmFiltered.HasUnigramsFor(key)) return ulmPhrases.HasUnigramsFor(key) || lmCore.HasUnigramsFor(key);
    return UnigramsFor(key).Count != 0;
  }

  public List<string> AssociatedPhrasesForKey(string key) {
    List<string> result = ulmAssociates.EntriesFor(key: key);
    return result.Count == 0 ? new() : result;
  }

  public bool HasAssociatedPhrasesForKey(string key) => ulmAssociates.HasEntriesFor(key: key);

  // MARK: - 核心函式（對內）

  /// <summary>
  /// 給定單元圖原始結果陣列，經過語彙過濾處理＋置換處理＋去重複處理之後，給出單元圖結果陣列。
  /// </summary>
  /// <param name="unigrams">傳入的單元圖原始結果陣列。</param>
  /// <param name="filteredPairs">傳入的要過濾掉的鍵值配對陣列。</param>
  /// <returns>經過語彙過濾處理＋置換處理＋去重複處理的單元圖結果陣列。</returns>
  private List<Unigram> FilterAndTransform(List<Unigram> unigrams, HashSet<KeyValuePaired> filteredPairs) {
    List<Unigram> results = new();
    HashSet<KeyValuePaired> insertedPairs = new();
    foreach (Unigram unigram in unigrams) {
      KeyValuePaired pair = unigram.KeyValue;
      if (filteredPairs.Contains(pair)) continue;
      if (isPhraseReplacementEnabled) {
        string replacement = ulmReplacements.EntryFor(pair.Value);
        if (!string.IsNullOrEmpty(replacement)) pair = new(pair.Key, replacement);
      }
      if (insertedPairs.Contains(pair)) continue;
      results.Add(item: new(pair, unigram.Score));
      insertedPairs.Add(pair);
    }
    return results;
  }
}
}