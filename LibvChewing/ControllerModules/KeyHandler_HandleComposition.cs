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

// 該檔案用來處理 KeyHandler.HandleInput() 當中的與組字有關的行為。

namespace LibvChewing {
public partial class KeyHandler {
  /// <summary>
  /// 用來處理 KeyHandler.HandleInput() 當中的與組字有關的行為。
  /// </summary>
  /// <param name="input">輸入訊號。</param>
  /// <param name="state">給定狀態（通常為當前狀態）。</param>
  /// <param name="stateCallback">狀態回呼，交給對應的型別內的專有函式來處理。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>告知 IMK「該按鍵是否已經被輸入法攔截處理」。</returns>
  public bool? HandleComposition(InputSignalProtocol input, InputStateProtocol state,
                                 Action<InputStateProtocol> stateCallback, Action<Error> errorCallback) {
    // MARK: 注音按鍵輸入處理 (Handle BPMF Keys)

    bool keyConsumedByReading = false;
    bool skipPhoneticHandling = input.IsReservedKey() || input.IsNumericPadKey() || input.IsNonLaptopFunctionKey() ||
                                input.IsControlHold() || input.IsAltHold() || input.IsShiftHold() ||
                                input.IsCommandHold();

    // 這裡 inputValidityCheck() 是讓注拼槽檢查 charCode 這個 UniChar 是否是合法的注音輸入。
    // 如果是的話，就將這次傳入的這個按鍵訊號塞入注拼槽內且標記為「keyConsumedByReading」。
    // 函式 composer.receiveKey() 可以既接收 String 又接收 UniChar。
    if (skipPhoneticHandling && composer.InputValidityCheck(input.CharCode)) {
      composer.ReceiveKey(input.CharCode);
      keyConsumedByReading = true;

      // 沒有調號的話，只需要 updateClientComposingBuffer() 且終止處理（return true）即可。
      // 有調號的話，則不需要這樣，而是轉而繼續在此之後的處理。
      if (!composer.HasToneMarker()) {
        stateCallback(BuildInputtingState());
        return true;
      }
    }

    bool composeReading = composer.HasToneMarker();  // 這裡不需要做排他性判斷。

    // 如果當前的按鍵是 Enter 或 Space 的話，這時就可以取出 composer 內的注音來做檢查了。
    // 來看看詞庫內到底有沒有對應的讀音索引。這裡用了「|=」判斷處理方式。
    composeReading |= !composer.IsEmpty && (input.IsSpace() || input.IsEnter());
    if (composeReading) {
      // 補上空格，否則倚天忘形與許氏排列某些音無法響應不了陰平聲調。
      // 小麥注音因為使用 OVMandarin，所以不需要這樣補。但鐵恨引擎對所有聲調一視同仁。
      if (input.IsSpace() && !composer.HasToneMarker()) composer.ReceiveKey(input: " ");
      string readingKey = composer.GetComposition();  // 拿取用來進行索引檢索用的注音。
      // 如果輸入法的辭典索引是漢語拼音的話，要注意上一行拿到的內容得是漢語拼音。

      // 向語言模型詢問是否有對應的記錄。
      if (!currentLM.HasUnigramsFor(readingKey)) {
        Tools.PrintDebugIntel($"B49C0979：語彙庫內無「{readingKey}」的匹配記錄。");
        errorCallback(Error.OfNormal);
        composer.Clear();
        // 根據「組字器是否為空」來判定回呼哪一種狀態。
        stateCallback(compositor.IsEmpty ? new InputState.EmptyIgnoringPreviousState() : BuildInputtingState());
        return true;  // 向 IMK 報告說這個按鍵訊號已經被輸入法攔截處理了。
      }

      // 將該讀音插入至組字器內的軌格當中。
      compositor.InsertReading(reading: readingKey);

      // 讓組字器反爬軌格。
      string textToCommit = CommitOverflownCompositionAndWalk();

      // 看看半衰記憶模組是否會對目前的狀態給出自動選字建議。
      FetchAndApplySuggestionsFromUserOverrideModel();

      // 將組字器內超出最大動態爬軌範圍的節錨都標記為「已經手動選字過」，減少之後的爬軌運算負擔。
      MarkNodesFixedIfNecessary();

      // 之後就是更新組字區了。先清空注拼槽的內容。
      composer.Clear();

      // 再以回呼組字狀態的方式來執行 updateClientComposingBuffer()。
      InputState.Inputting inputting = BuildInputtingState();
      inputting.TextToCommit = textToCommit;
      stateCallback(inputting);

      // 逐字選字模式的處理。
      if (Prefs.UseSCPCTypingMode) {
        InputState.ChoosingCandidate choosingCandidates = BuildCandidate(inputting, input.IsTypingVertical);
        if (choosingCandidates.Candidates.Count == 1) {
          Clear();
          string text = choosingCandidates.Candidates[0].Item2;
          string reading = choosingCandidates.Candidates[0].Item1;
          stateCallback(new InputState.Committing(textToCommit: text));

          if (!Prefs.AssociatedPhrasesEnabled)
            stateCallback(new InputState.Empty());
          else {
            InputState.AssociatedPhrases associatedPhrases =
                BuildAssociatePhraseStateWith(new(reading, text), input.IsTypingVertical);
            stateCallback(associatedPhrases.Candidates.Count > 0 ? associatedPhrases : new InputState.Empty());
          }
        } else {
          stateCallback(choosingCandidates);
        }
      }
      // 將「這個按鍵訊號已經被輸入法攔截處理了」的結果藉由 ctlInputMethod 回報給 IMK。
      return true;
    }

    if (!keyConsumedByReading) return null;
    // 如果上述選項是 true 的話，可知當前注拼槽輸入了聲調、且上一次按鍵不是聲調按鍵。
    // 比方說大千傳統佈局敲「6j」會出現「ˊㄨ」但並不會被認為是「ㄨˊ」，因為先輸入的調號
    // 並非用來確認這個注音的調號。除非是：「ㄨˊ」「ˊㄨˊ」「ˊㄨˇ」「ˊㄨ 」等。
    // 以回呼組字狀態的方式來執行 updateClientComposingBuffer()。
    stateCallback(BuildInputtingState());
    return true;
  }
}
}