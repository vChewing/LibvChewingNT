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

using Megrez;
using Tekkon;

namespace LibvChewing {

public interface KeyHandlerDelegate {
  CtlCandidate CtlCandidate();
  void KeyHandler(KeyHandler Handler, int didSelectCandidateAtIndex, CtlCandidate controller);
  bool KeyHandler(KeyHandler Handler, InputStateProtocol didRequestWriteUserPhraseWithState);
}

/// <summary>
/// KeyHandler 按鍵調度模組。
/// </summary>
public partial class KeyHandler {
  private double kEpsilon = 0.000001;
  private int kMaxComposingBufferNeedsToWalkSize = Math.Max(12, Prefs.ComposingBufferSize / 2);
  private Composer composer;
  private Compositor compositor;
  public LMInstantiator currentLM = null!;
  public LMUserOverride currentUOM = null!;
  private List<NodeAnchor> walkedAnchors = new();
  private KeyHandlerDelegate? theDelegate;
  /// <summary>
  /// 必須使用這個獨立的內部變數，不然只交給對外 InputMode 變數的話會漏棧。<br />
  /// 要怨就只能怨 C# 沒有 didSet / willSet 機制。
  /// </summary>
  private InputMode inputMode;

  public InputMode InputMode {
    get => inputMode;
    set {
      // 這個標籤在下文會用到。
      bool isCHS = value == InputMode.ImeModeCHS;
      // 將新的簡繁輸入模式提報給 ctlInputMethod 與 IME 模組。
      Prefs.CurrentInputMode = value;
      // 重設所有語言模組。這裡不需要做按需重設，因為對運算量沒有影響。
      currentLM = isCHS ? MgrLangModel.LmCHS : MgrLangModel.LmCHT;
      currentUOM = isCHS ? MgrLangModel.UomCHS : MgrLangModel.UomCHT;
      // 將與主語言模組有關的選項同步至主語言模組內。
      SyncBaseLMPrefs();
      // 重建新的組字器，且清空注拼槽＋同步最新的注拼槽排列設定。
      // 組字器只能藉由重建才可以與當前新指派的語言模組對接。
      EnsureCompositor();
      EnsureParser();
      inputMode = value;
    }
  }

  public KeyHandler(KeyHandlerDelegate? givenDelegate = null) {
    if (givenDelegate != null) theDelegate = givenDelegate;
    compositor = new(MgrLangModel.LmCHS, length: 20, separator: "-");
    EnsureParser();
    InputMode = Prefs.CurrentInputMode;
  }

  public void Clear() {
    composer.Clear();
    compositor.Clear();
    walkedAnchors.Clear();
  }

  // MARK: - Functions dealing with Megrez.

  /// <summary>
  /// 實際上要拿給 Megrez 使用的的滑鼠游標位址，以方便在組字器最開頭或者最末尾的時候始終能抓取候選字節點陣列。<br />
  /// 威注音對游標前置與游標後置模式採取的候選字節點陣列抓取方法是分離的，且不使用 Node Crossing。
  /// </summary>
  /// <returns>實際上的游標位址。</returns>
  private int ActualCandidateCursorIndex =>
      Prefs.UseRearCursorMode ? Math.Min(compositor.Cursor, CompositorLength - 1) : Math.Max(compositor.Cursor, 1);

  /// <summary>
  /// 利用給定的讀音鏈來試圖爬取最接近的組字結果（最大相似度估算）。<br />
  /// 該過程讀取的權重資料是經過 Viterbi 演算法計算得到的結果。<br />
  /// 該函式的爬取順序是從頭到尾。
  /// </summary>
  private void Walk() {
    walkedAnchors = compositor.Walk();
    if (!Prefs.IsDebugModeEnabled) return;
    string path = Path.GetTempPath() + "vChewing-visualization.dot";
    try {
      FileInfo theFile = new(path);
      using TextWriter outputStream = new StreamWriter(theFile.Open(FileMode.Truncate));
      outputStream.Write(compositor.DumpDOT());
    } catch {
      Tools.PrintDebugIntel("Failed from writing GraphViz data.");
    }
  }

  /// <summary>
  /// 在爬取組字結果之前，先將即將從組字區溢出的內容遞交出去。<br />
  /// 在理想狀況之下，組字區多長都無所謂。但是，Viterbi 演算法使用 O(N^2)，
  /// 會使得運算壓力隨著節錨數量的增加而增大。於是，有必要限定組字區的長度。
  /// 超過該長度的內容會在爬軌之前先遞交出去，使其不再記入最大相似度估算的
  /// 估算對象範圍。用比較形象且生動卻有點噁心的解釋的話，蒼蠅一邊吃一邊屙。
  /// </summary>
  /// <returns></returns>
  private string CommitOverflownCompositionAndWalk() {
    string textToCommit = "";
    if (compositor.Width > Prefs.ComposingBufferSize && walkedAnchors.Count > 0) {
      NodeAnchor anchor = walkedAnchors[0];
      textToCommit = anchor.Node.CurrentPair.Value;
      compositor.RemoveHeadReadings(anchor.SpanLength);
    }
    Walk();
    return textToCommit;
  }

  /// <summary>
  /// 用以組建聯想詞陣列的函式。
  /// </summary>
  /// <param name="key">給定的聯想詞的開頭字。</param>
  /// <returns>抓取到的聯想詞陣列。不會是 nil，但那些負責接收結果的函式會對空白陣列結果做出正確的處理。</returns>
  private List<(string, string)> BuildAssociatePhraseArrayWith(string key) {
    List<string> arrResult = new();
    if (currentLM.HasAssociatedPhrasesForKey(key)) {
      arrResult.AddRange(currentLM.AssociatedPhrasesForKey(key));
    }
    return arrResult.Select(neta => ("", neta)).ToList();
  }

  /// <summary>
  /// 在組字器內，以給定之候選字字串、來試圖在給定游標位置所在之處指定選字處理過程。
  /// 然後再將對應的節錨內的節點標記為「已經手動選字過」。
  /// </summary>
  /// <param name="value">給定之候選字字串。</param>
  /// <param name="respectCursorPushing">若該選項為 true，則會在選字之後始終將游標推送至選字厚的節錨的前方。</param>
  private void FixNode((string, string)candidate, bool respectCursorPushing = true) {
    KeyValuePaired theCandidate = new(key: candidate.Item1, value: candidate.Item2);
    int adjustedIndex =
        Math.Max(0, Math.Min(ActualCandidateCursorIndex + (Prefs.UseRearCursorMode ? 1 : 0), CompositorLength));
    // 開始讓半衰模組觀察目前的狀況。
    NodeAnchor selectedNode = compositor.FixNodeWithCandidate(theCandidate, adjustedIndex);
    // 不要針對逐字選字模式啟用臨時半衰記憶模型。
    if (!Prefs.UseSCPCTypingMode) {
      bool addToUOM = true;
      // 所有讀音數與字符數不匹配的情況均不得塞入半衰記憶模組。
      if (selectedNode.SpanLength != theCandidate.Value.Length) {
        Tools.PrintDebugIntel("UOM: SpanLength != value.Count, dismissing.");
        addToUOM = false;
      }
      if (addToUOM) {
        // 威注音的 SymbolLM 的 Score 是 -12，符合該條件的內容不得塞入半衰記憶模組。
        if (selectedNode.Node.ScoreFor(theCandidate.Value) <= -12) {
          Tools.PrintDebugIntel("UOM: Score <= -12, dismissing.");
          addToUOM = false;
        }
      }
      if (addToUOM) {
        Tools.PrintDebugIntel("UOM: Start Observation.");
        // 令半衰記憶模組觀測給定的三元圖。
        // 這個過程會讓半衰引擎根據當前上下文生成三元圖索引鍵。
        currentUOM.Observe(walkedAnchors, adjustedIndex, theCandidate.Value, DateTime.Now.Ticks);
      }
    }
    // 開始爬軌。
    Walk();

    // 若偏好設定內啟用了相關選項，則會在選字之後始終將游標推送至選字厚的節錨的前方。
    if (!respectCursorPushing || !Prefs.MoveCursorAfterSelectingCandidate) return;
    compositor.JumpCursorBySpan(Compositor.TypingDirection.ToFront);
  }

  /// <summary>
  /// 組字器內超出最大動態爬軌範圍的節錨都會被自動標記為「已經手動選字過」，減少爬軌運算負擔。
  /// </summary>
  private void MarkNodesFixedIfNecessary() {
    int width = compositor.Width;
    if (width <= kMaxComposingBufferNeedsToWalkSize) return;
    int index = 0;
    foreach (NodeAnchor theAnchor in walkedAnchors) {
      if (index >= width - kMaxComposingBufferNeedsToWalkSize) break;
      Node theNode = theAnchor.Node;
      if (theNode.Score < Node.ConSelectedCandidateScore)
        compositor.FixNodeWithCandidateLiteral(theNode.CurrentPair.Value, index + theAnchor.SpanLength);
      index += theAnchor.SpanLength;
    }
  }

  /// <summary>
  /// 獲取候選字詞（包含讀音）陣列資料內容。
  /// </summary>
  /// <param name="fixOrder">是否鎖定候選字詞排序。</param>
  /// <returns>候選字詞資料讀音配對陣列。</returns>
  private List<(string, string)> GetCandidatesArray(bool fixOrder = true) {
    List<NodeAnchor> arrAnchors = RawAnchorsOfNodes;
    List<KeyValuePaired> arrCandidates = new();

    // 原理：nodes 這個回饋結果包含一堆子陣列，分別對應不同詞長的候選字。
    // 這裡先對陣列排序、讓最長候選字的子陣列的優先權最高。
    // 這個過程不會傷到子陣列內部的排序。
    if (arrAnchors.Count == 0) return new();

    // 讓更長的節錨排序靠前。
    arrAnchors = arrAnchors.OrderByDescending(a => a.SpanLength).ToList();

    // 將節錨內的候選字詞資料拓印到輸出陣列內。
    arrCandidates.AddRange(
        from theAnchor in arrAnchors from theCandidate in theAnchor.Node!.Candidates select theCandidate);
    // 決定是否根據半衰記憶模組的建議來調整候選字詞的順序。
    if (!fixOrder && !Prefs.UseSCPCTypingMode && Prefs.FetchSuggestionsFromUserOverrideModel) {
      List<Unigram> arrSuggestedUnigrams = FetchSuggestedCandidates().OrderByDescending(a => a.Score).ToList();
      List<KeyValuePaired> arrSuggestedCandidates = new();
      arrSuggestedCandidates.AddRange(arrSuggestedUnigrams.Select(gram => gram.KeyValue));
      foreach (KeyValuePaired candidate in arrSuggestedCandidates.Where(candidate =>
                                                                            arrCandidates.Contains(candidate))) {
        arrSuggestedCandidates.Remove(candidate);
      }
      arrCandidates = arrSuggestedCandidates.Concat(arrCandidates)
                          .Distinct()
                          .OrderByDescending(a => a.Key.Split('-').Length)
                          .ToList();
    }
    return arrCandidates.Select(neta => (neta.Key, neta.Value)).ToList();
  }

  /// <summary>
  /// 向半衰引擎詢問可能的選字建議。
  /// </summary>
  /// <returns>一個單元圖陣列。</returns>
  private List<Unigram> FetchSuggestedCandidates() => currentUOM.Suggest(walkedAnchors, compositor.Cursor,
                                                                         DateTime.Now.Ticks);

  /// <summary>
  /// 向半衰引擎詢問可能的選字建議、且套用給組字器內的當前游標位置。
  /// </summary>
  private void FetchAndApplySuggestionsFromUserOverrideModel() {
    // 如果逐字選字模式有啟用的話，直接放棄執行這個函式。
    if (Prefs.UseSCPCTypingMode) return;
    // 如果這個開關沒打開的話，直接放棄執行這個函式。
    if (!Prefs.FetchSuggestionsFromUserOverrideModel) return;
    // 先就當前上下文讓半衰引擎重新生成 trigram 索引鍵。
    string overrideValue = "";
    List<Unigram> fetchedSuggestions = FetchSuggestedCandidates();
    if (fetchedSuggestions.Count > 0) overrideValue = fetchedSuggestions[0].KeyValue.Value;

    // 再拿著索引鍵去問半衰模組有沒有選字建議。
    // 有的話就遵循之、讓天權星引擎對指定節錨下的節點複寫權重。
    if (!string.IsNullOrEmpty(overrideValue)) {
      Tools.PrintDebugIntel("UOM: Suggestion retrieved, overriding the node score of the selected candidate.");
      compositor.OverrideNodeScoreForSelectedCandidate(
          Math.Min(ActualCandidateCursorIndex + (Prefs.UseRearCursorMode ? 1 : 0), CompositorLength), overrideValue,
          FindHighestScore(RawAnchorsOfNodes, kEpsilon));
    } else {
      Tools.PrintDebugIntel("UOM: Blank suggestion retrieved, dismissing.");
    }
  }

  /// <summary>
  /// 就給定的節錨陣列，根據半衰模組的衰減指數，來找出最高權重數值。
  /// </summary>
  /// <param name="nodes">給定的節錨陣列。</param>
  /// <param name="epsilon">半衰模組的衰減指數。</param>
  /// <returns>尋獲的最高權重數值。</returns>
  private double FindHighestScore(List<NodeAnchor> nodes, double epsilon) {
    double highestScore = (from theAnchor in nodes select theAnchor.Node!.HighestUnigramScore).Prepend(0).Max();
    return highestScore + epsilon;
  }

  // MARK: - Extracted methods and functions (Tekkon).

  /// <summary>
  /// 獲取與當前注音排列或拼音輸入種類有關的標點索引鍵，以英數下畫線「_」結尾。
  /// </summary>
  /// <returns></returns>
  private string CurrentMandarinParser() {
    return Prefs.MandarinParser switch { (int)MandarinParser.OfDachen => "Standard_",
                                         (int)MandarinParser.OfDachen26 => "DachenCP26_",
                                         (int)MandarinParser.OfETen => "ETen_",
                                         (int)MandarinParser.OfETen26 => "ETen26_",
                                         (int)MandarinParser.OfHsu => "Hsu_",
                                         (int)MandarinParser.OfIBM => "IBM_",
                                         (int)MandarinParser.OfMiTAC => "MiTAC_",
                                         (int)MandarinParser.OfSeigyou => "Seigyou_",
                                         (int)MandarinParser.OfFakeSeigyou => "FakeSeigyou_",
                                         (int)MandarinParser.OfHanyuPinyin => "HanyuPinyin_",
                                         (int)MandarinParser.OfSecondaryPinyin => "SecondaryPinyin_",
                                         (int)MandarinParser.OfYalePinyin => "YalePinyin_",
                                         (int)MandarinParser.OfHualuoPinyin => "HualuoPinyin_",
                                         (int)MandarinParser.OfUniversalPinyin => "UniversalPinyin_",
                                         _ => "" };
  }

  /// <summary>
  /// 給注拼槽指定注音排列或拼音輸入種類之後，將注拼槽內容清空。
  /// </summary>
  private void EnsureParser() {
    switch (Prefs.MandarinParser) {
      case (int)MandarinParser.OfDachen:
      case (int)MandarinParser.OfDachen26:
      case (int)MandarinParser.OfETen:
      case (int)MandarinParser.OfETen26:
      case (int)MandarinParser.OfHsu:
      case (int)MandarinParser.OfIBM:
      case (int)MandarinParser.OfMiTAC:
      case (int)MandarinParser.OfSeigyou:
      case (int)MandarinParser.OfFakeSeigyou:
      case (int)MandarinParser.OfHanyuPinyin:
      case (int)MandarinParser.OfSecondaryPinyin:
      case (int)MandarinParser.OfYalePinyin:
      case (int)MandarinParser.OfHualuoPinyin:
      case (int)MandarinParser.OfUniversalPinyin:
        composer.EnsureParser((MandarinParser)Prefs.MandarinParser);
        break;
      default:
        Prefs.MandarinParser = 0;
        composer.EnsureParser();
        break;
    }
    composer.Clear();
  }

  /// <summary>
  /// 用於網頁 Ruby 的注音需要按照教科書印刷的方式來顯示輕聲。該函式負責這種轉換。
  /// </summary>
  /// <param name="target">要拿來做轉換處理的讀音鏈。</param>
  /// <param name="newSeparator">新的讀音分隔符。</param>
  /// <returns>經過轉換處理的讀音鏈。</returns>
  private string CnvZhuyinKeyToTextbookReading(string target, string newSeparator = "-") {
    List<string> arrReturn = new();
    foreach (string neta in target.Split("-")) {
      if (neta.Reverse().ToList()[0] != '˙') continue;
      string newNeta = neta.Remove(neta.Length - 1);
      newNeta.Insert(0, "˙");
      arrReturn.Add(newNeta);
    }
    return string.Join(newSeparator, arrReturn);
  }

  /// <summary>
  /// 用於網頁 Ruby 的拼音的陰平必須顯示，這裡處理一下。
  /// </summary>
  /// <param name="target">要拿來做轉換處理的讀音鏈。</param>
  /// <param name="newSeparator">新的讀音分隔符。</param>
  /// <returns>經過轉換處理的讀音鏈。</returns>
  private string RestoreToneOneInZhuyinKey(string target, string newSeparator = "-") {
    List<string> arrReturn = new();
    List<char> matchedTones = new() { 'ˊ', 'ˇ', 'ˋ', '˙' };
    foreach (string neta in target.Split("-")) {
      string newNeta = neta;
      if (matchedTones.Contains(neta.Reverse().ToList()[0])) newNeta += "1";
      arrReturn.Add(newNeta);
    }
    return string.Join(newSeparator, arrReturn);
  }

  // MARK: - Extracted methods and functions (Megrez).

  /// <summary>
  /// 組字器是否為空。
  /// </summary>
  private bool IsCompositorEmpty => compositor.IsEmpty;

  /// <summary>
  /// 獲取原始節錨資料陣列。
  /// 警告：不要對游標前置風格使用 nodesCrossing，否則會導致游標行為與 macOS 內建注音輸入法不一致。
  /// 微軟新注音輸入法的游標後置風格也是不允許 nodeCrossing 的。
  /// </summary>
  /// <value>原始節錨資料陣列</value>
  private List<NodeAnchor> RawAnchorsOfNodes => Prefs.UseRearCursorMode
                                                    ? compositor.NodesBeginningAt(ActualCandidateCursorIndex)
                                                    : compositor.NodesEndingAt(ActualCandidateCursorIndex);

  /// <summary>
  /// 將輸入法偏好設定同步至語言模組內。
  /// </summary>
  private void SyncBaseLMPrefs() {
    currentLM.isPhraseReplacementEnabled = Prefs.PhraseReplacementEnabled;
    currentLM.isCNSEnabled = Prefs.CNS11643Enabled;
    currentLM.isSymbolEnabled = Prefs.SymbolInputEnabled;
  }

  /// <summary>
  /// 令組字器重新初期化，使其與被重新指派過的主語言模組對接。
  /// </summary>
  private void EnsureCompositor() {
    // 每個漢字讀音都由一個西文半形減號分隔開。
    compositor = new(currentLM, length: 20, separator: "-");
  }

  /// <summary>
  /// 自組字器獲取目前的讀音陣列。
  /// </summary>
  private List<string> CurrentReadings => compositor.Readings;

  /// <summary>
  /// 以給定的（讀音）索引鍵，來檢測當前主語言模型內是否有對應的資料在庫。
  /// </summary>
  /// <param name="key">給定的（讀音）索引鍵。</param>
  /// <returns>若有在庫，則返回 true。</returns>
  private bool IfLangModelHasUnigramsFor(string key) => currentLM.HasUnigramsFor(key);

  /// <summary>
  /// 在組字器的給定游標位置內插入讀音。
  /// </summary>
  /// <param name="reading">讀音。</param>
  private void InsertToCompositorAtCursor(string reading) { compositor.InsertReading(reading); }

  /// <summary>
  /// 組字器的目前的長度。
  /// </summary>
  private int CompositorLength => compositor.Length;

  /// <summary>
  /// 在組字器內，朝著與文字輸入方向相反的方向、砍掉一個與游標相鄰的讀音。<br />
  /// 在威注音的術語體系當中，「與文字輸入方向相反的方向」為向後（Rear）。
  /// </summary>
  private void DeleteCompositorReadingAtTheRearOfCursor() => compositor.DropReading(Compositor.TypingDirection.ToRear);

  /// <summary>
  /// 在組字器內，朝著往文字輸入方向、砍掉一個與游標相鄰的讀音。<br />
  /// 在威注音的術語體系當中，「文字輸入方向」為向前（Front）。
  /// </summary>
  private void DeleteCompositorReadingToTheFrontOfCursor() =>
      compositor.DropReading(Compositor.TypingDirection.ToFront);

  /// <summary>
  /// 生成標點符號索引鍵。
  /// </summary>
  /// <param name="input">輸入的按鍵訊號。</param>
  /// <returns>生成的標點符號索引鍵。</returns>
  private string GeneratePunctuationNamePrefixWithInputSignal(InputSignalProtocol input) {
    if (Prefs.HalfWidthPunctuationEnabled) {
      return "_half_punctuation_";
    }
    return (input.IsAltHold(), input.IsControlHold()) switch {
      (true, true) => "_alt_ctrl_punctuation_",
      (false, true) => "_ctrl_punctuation_",
      (true, false) => "_alt_punctuation_",
      _ => "_punctuation_",
    };
  }
}
}