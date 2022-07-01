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

// 該檔案乃按鍵調度模組的用以承載「根據按鍵行為來調控模式」的各種成員函式的部分。

using Megrez;
using Microsoft.Extensions.Localization;
using Tekkon;

// MARK: - § 根據按鍵行為來調控模式的函式 (Functions Interact With States).

namespace LibvChewing {

public partial class KeyHandler {
  // MARK: - 構築狀態（State Building）

  /// <summary>
  /// 生成「正在輸入」狀態。
  /// </summary>
  /// <returns>生成了的「正在輸入」狀態。</returns>
  private InputState.Inputting BuildInputtingState() {
    // 「更新內文組字區 (Update the composing buffer)」是指要求客體軟體將組字緩衝區的內容
    // 換成由此處重新生成的組字字串（在 macOS 系統下得是 NSAttributeString，否則會不顯示）。
    List<string> tooltipParameterRef = new() { "", "" };
    string composingBuffer = "";
    int composedStringCursorIndex = 0;
    int readingCursorIndex = 0;
    // IMK 協定的內文組字區的游標長度與游標位置無法正確統計 UTF8 高萬字（比如 emoji）的長度，
    // 所以在這裡必須做糾偏處理。因為在用 Swift，所以可以用「.utf16」取代「NSString.length()」。
    // 這樣就可以免除不必要的類型轉換。
    // 我們推定 Windows 平台也得需要用 UTF16 來處理。如果實際情況有變的話，再做調整。
    foreach (NodeAnchor theAnchor in walkedAnchors) {
      if (theAnchor.Node == null) continue;
      Node theNode = theAnchor.Node;
      string strNodeValue = theNode.CurrentKeyValue.Value;
      composingBuffer += strNodeValue;
      List<string> arrSplit = U8Utils.GetU8Elements(strNodeValue);
      int codepointCount = arrSplit.Count;
      // 藉下述步驟重新將「可見游標位置」對齊至「組字器內的游標所在的讀音位置」。
      // 每個節錨（NodeAnchor）都有自身的幅位長度（spanningLength），可以用來
      // 累加、以此為依據，來校正「可見游標位置」。
      int spanningLength = theAnchor.SpanningLength;
      if (readingCursorIndex + spanningLength <= CompositorCursorIndex) {
        composedStringCursorIndex += strNodeValue.Length;
        readingCursorIndex += spanningLength;
      } else {
        if (codepointCount == spanningLength) {
          int i = 0;
          while (i < codepointCount && readingCursorIndex < CompositorCursorIndex) {
            composedStringCursorIndex += arrSplit[i].Length;
            readingCursorIndex += 1;
            i += 1;
          }
        } else {
          if (readingCursorIndex < CompositorCursorIndex) {
            composedStringCursorIndex += strNodeValue.Length;
            readingCursorIndex += spanningLength;
            readingCursorIndex = Math.Min(readingCursorIndex, CompositorCursorIndex);
            // 接下來再處理這麼一種情況：
            // 某些錨點內的當前候選字詞長度與讀音長度不相等。
            // 但此時游標還是按照每個讀音單位來移動的，
            // 所以需要上下文工具提示來顯示游標的相對位置。
            // 這裡先計算一下要用在工具提示當中的顯示參數的內容。
            switch (CompositorCursorIndex) {
              case var n when n >= compositor.Readings.Count:
                tooltipParameterRef[0] = compositor.Readings[^1];
                break;
              case 0:
                tooltipParameterRef[1] = compositor.Readings[CompositorCursorIndex];
                break;
              default:
                tooltipParameterRef[0] = compositor.Readings[CompositorCursorIndex - 1];
                tooltipParameterRef[1] = compositor.Readings[CompositorCursorIndex];
                break;
            }
          }
        }
      }
    }

    string head = composingBuffer.Substring(0, composedStringCursorIndex);
    string reading = composer.GetInlineCompositionForDisplay(isHanyuPinyin: Prefs.ShowHanyuPinyinInCompositionBuffer);
    string tail = composingBuffer.Substring(composedStringCursorIndex, composingBuffer.Length - head.Length);
    string composedText = head + reading + tail;
    int cursorIndex = composedStringCursorIndex + reading.Length;

    string cleanedComposition = "";
    // 防止組字區內出現不可列印的字元。
    foreach (char neta in composedText) {
      if (char.IsAscii(neta) && char.IsControl(neta)) break;
      cleanedComposition += neta;
    }
    // 這裡生成準備要拿來回呼的「正在輸入」狀態，但還不能立即使用，因為工具提示仍未完成。
    InputState.Inputting stateResult = new(cleanedComposition, cursorIndex);

    stateResult.Tooltip = (string.IsNullOrEmpty(tooltipParameterRef[0]),
                           string.IsNullOrEmpty(tooltipParameterRef[1])) switch {
      (true, true) => "",
      (true, false) => new LocalizedString("KeyHandler_BuildInputtingState_ToolTip_ToTheRearOf",
                                           $"Cursor is to the rear of {tooltipParameterRef[1]}.")
                           .ToString(),
      (false, true) => new LocalizedString("KeyHandler_BuildInputtingState_ToolTip_InFrontOf",
                                           $"Cursor is in front of {tooltipParameterRef[0]}.")
                           .ToString(),
      (false, false) => new LocalizedString("KeyHandler_BuildInputtingState_ToolTip_BetweenReadings",
                                            $"Cursor is between {tooltipParameterRef[0]} and {tooltipParameterRef[1]}.")
                            .ToString(),
    };

    // TODO: 可以在這裡加入控制工具提示配色的指令。

    return stateResult;
  }

  // MARK: - 用以生成候選詞陣列及狀態

  /// <summary>
  /// 拿著給定的候選字詞陣列資料內容，切換至選字狀態。
  /// </summary>
  /// <param name="currentState">當前狀態。</param>
  /// <param name="isTypingVertical">是否縱排輸入？</param>
  /// <returns>回呼一個新的選詞狀態，來就給定的候選字詞陣列資料內容顯示選字窗。</returns>
  private InputState.ChoosingCandidate BuildCandidate(InputState.NotEmpty currentState,
                                                      bool isTypingVertical = false) =>
      new(currentState.ComposingBuffer, currentState.CursorIndex,
          CandidatesArray(fixOrder: Prefs.UseFixecCandidateOrderOnSelection), isTypingVertical);

  // MARK: - 用以接收聯想詞陣列且生成狀態

  /// <summary>
  /// 拿著給定的聯想詞陣列資料內容，切換至聯想詞狀態。<br />
  /// 這次重寫時，針對「buildAssociatePhraseStateWithKey」這個（用以生成帶有
  /// 聯想詞候選清單的結果的狀態回呼的）函式進行了小幅度的重構處理，使其始終
  /// 可以從 Core 部分的「buildAssociatePhraseArray」函式獲取到一個內容類型
  /// 為「String」的標準 Swift 陣列 / C# 清單。這樣一來，該聯想詞狀態回呼函
  /// 式將始終能夠傳回正確的結果形態、永遠也無法傳回 nil。於是，所有在用到該
  /// 函式時以回傳結果類型判斷作為合法性判斷依據的函式，全都將依據改為檢查傳
  /// 回的陣列是否為空：如果陣列為空的話，直接回呼一個空狀態。
  /// </summary>
  /// <param name="key">給定的索引鍵（也就是給定的聯想詞的開頭字）。</param>
  /// <param name="isTypingVertical">是否縱排輸入？</param>
  /// <returns>回呼一個新的聯想詞狀態，來就給定的聯想詞陣列資料內容顯示選字窗。</returns>
  private InputState.AssociatedPhrases BuildAssociatePhraseStateWith(string key, bool isTypingVertical) =>
      new(BuildAssociatePhraseArrayWith(key), isTypingVertical);

  // MARK: - 用以處理就地新增自訂語彙時的行為

  /// <summary>
  /// 用以處理就地新增自訂語彙時的行為。
  /// </summary>
  /// <param name="state">給定狀態（通常為當前狀態）。</param>
  /// <param name="input">輸入訊號。</param>
  /// <param name="stateCallback">狀態回呼，交給對應的型別內的專有函式來處理。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>告知 IMK「該按鍵是否已經被輸入法攔截處理」。</returns>
  private bool HandleMarkingState(InputState.Marking state, InputSignalProtocol input,
                                  Action<InputStateProtocol> stateCallback, Action<Error> errorCallback) {
    if (input.isEsc()) {
      stateCallback(BuildInputtingState());
      return true;
    }

    // Enter
    if (input.IsEnter()) {
      if (theDelegate != null) {
        if (!theDelegate.KeyHandler(this, state)) {
          Tools.PrintDebugIntel("5B69CC8D");
          errorCallback(Error.OfNormal);
          return true;
        }
      }
      stateCallback(BuildInputtingState());
      return true;
    }

    // Shift + Left
    if (input.IsCursorBackward() && input.IsShiftHold()) {
      int index = state.MarkerIndex;
      if (index > 0) {
        index = U8Utils.GetU16PreviousPositionFor(state.ComposingBuffer, index);
        InputState.Marking marking = new(state.ComposingBuffer, state.CursorIndex, index,
                                         state.Readings) { TooltipForInputting = state.TooltipForInputting };
        stateCallback(Tools.GetRangeLength(marking.MarkedRange) == 0 ? marking.ConvertedToInputting() : marking);
      } else {
        Tools.PrintDebugIntel("1149908D");
        errorCallback(Error.OfNormal);
        stateCallback(state);
      }
      return true;
    }

    // Shift + Right
    if (input.IsCursorForward() && input.IsShiftHold()) {
      int index = state.MarkerIndex;
      if (index < state.ComposingBuffer.Length) {
        index = U8Utils.GetU16NextPositionFor(state.ComposingBuffer, index);
        InputState.Marking marking = new(state.ComposingBuffer, state.CursorIndex, index,
                                         state.Readings) { TooltipForInputting = state.TooltipForInputting };
        stateCallback(Tools.GetRangeLength(marking.MarkedRange) == 0 ? marking.ConvertedToInputting() : marking);
      } else {
        Tools.PrintDebugIntel("9B51408D");
        errorCallback(Error.OfNormal);
        stateCallback(state);
      }
      return true;
    }
    return false;
  }

  // MARK: - 標點輸入的處理

  /// <summary>
  /// 標點輸入的處理。
  /// </summary>
  /// <param name="customPunctuation">自訂標點。</param>
  /// <param name="state">給定狀態（通常為當前狀態）。</param>
  /// <param name="isTypingVertical">是否縱排輸入？</param>
  /// <param name="stateCallback">狀態回呼，交給對應的型別內的專有函式來處理。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>告知 IMK「該按鍵是否已經被輸入法攔截處理」。</returns>
  private bool HandlePunctuation(string customPunctuation, InputStateProtocol state, bool isTypingVertical,
                                 Action<InputStateProtocol> stateCallback, Action<Error> errorCallback) {
    if (IfLangModelHasUnigramsFor(customPunctuation)) return false;

    if (!composer.IsEmpty) {
      // 注音沒敲完的情況下，無視標點輸入。
      Tools.PrintDebugIntel("A9B69908D");
      errorCallback(Error.OfNormal);
      stateCallback(state);
      return true;
    }

    InsertToCompositorAtCursor(customPunctuation);
    string textToCommit = CommitOverflownCompositionAndWalk();
    InputState.Inputting inputting = BuildInputtingState();
    inputting.TextToCommit = textToCommit;
    stateCallback(inputting);

    // 從這一行之後開始，就是針對逐字選字模式的單獨處理。
    if (!Prefs.UseSCPCTypingMode || !composer.IsEmpty) return true;

    InputState.ChoosingCandidate candidateState = BuildCandidate(inputting, isTypingVertical);
    if (candidateState.Candidates.Count == 1) {
      Clear();
      if (string.IsNullOrEmpty(candidateState.Candidates[0]))
        stateCallback(candidateState);
      else {
        stateCallback(new InputState.Committing(textToCommit: candidateState.Candidates[0]));
        stateCallback(new InputState.Empty());
      }
    } else {
      stateCallback(candidateState);
    }
    return true;
  }

  // MARK: - Enter 鍵的處理

  /// <summary>
  /// Enter 鍵的處理。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleEnter(InputStateProtocol state, Action<InputStateProtocol> stateCallback) {
    if (state is not InputState.Inputting currentState) return false;
    Clear();
    stateCallback(new InputState.Committing(textToCommit: currentState.ComposingBuffer));
    stateCallback(new InputState.Empty());
    return true;
  }

  // MARK: - CMD+Enter 鍵的處理（注音文）

  /// <summary>
  /// CMD+Enter 鍵的處理（注音文）。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleCtrlCommandEnter(InputStateProtocol state, Action<InputStateProtocol> stateCallback) {
    if (state is not InputState.Inputting) return false;
    string composingBuffer = string.Join("-", CurrentReadings);
    if (Prefs.InlineDumpPinyinInLieuOfZhuyin) {
      composingBuffer = RestoreToneOneInZhuyinKey(composingBuffer);     // 恢復陰平標記
      composingBuffer = Shared.CnvPhonaToHanyuPinyin(composingBuffer);  // 注音轉拼音
    }

    composingBuffer = composingBuffer.Replace("-", " ");

    Clear();

    stateCallback(new InputState.Committing(textToCommit: composingBuffer));
    stateCallback(new InputState.Empty());
    return true;
  }

  // MARK: - CMD+Alt+Enter 鍵的處理（網頁 Ruby 注音文標記）

  /// <summary>
  /// CMD+Alt+Enter 鍵的處理（網頁 Ruby 注音文標記）。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleCtrlAltCommandEnter(InputStateProtocol state, Action<InputStateProtocol> stateCallback) {
    if (state is not InputState.Inputting) return false;
    string composed = "";
    foreach (NodeAnchor theAnchor in walkedAnchors) {
      if (theAnchor.Node == null) continue;
      Node theNode = theAnchor.Node;
      string key = theNode.Key;
      if (Prefs.InlineDumpPinyinInLieuOfZhuyin) {
        key = RestoreToneOneInZhuyinKey(key);
        key = Shared.CnvPhonaToHanyuPinyin(key);
        key = Shared.CnvHanyuPinyinToTextbookStyle(key);
        key = key.Replace("-", " ");
      } else {
        key = CnvZhuyinKeyToTextbookReading(key, " ");
      }

      string value = theNode.CurrentKeyValue.Value;
      // 不要給標點符號等特殊元素加注音
      composed += key.Contains("_") ? value : $"<ruby>{value}<rp>(</rp><rt>{key}</rt><rp>)</rp></ruby>";
    }

    Clear();

    stateCallback(new InputState.Committing(textToCommit: composed));
    stateCallback(new InputState.Empty());
    return true;
  }

  // MARK: - 處理 Backspace (macOS Delete) 按鍵行為

  /// <summary>
  /// 處理 Backspace (macOS Delete) 按鍵行為。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleBackSpace(InputStateProtocol state, Action<InputStateProtocol> stateCallback,
                               Action<Error> errorCallback) {
    if (state is not InputState.Inputting) return false;

    if (composer.HasToneMarker(withNothingElse: true))
      composer.Clear();
    else if (composer.IsEmpty) {
      if (CompositorCursorIndex > 0) {
        DeleteCompositorReadingAtTheRearOfCursor();
        Walk();
      } else {
        Tools.PrintDebugIntel("9D69908D");
        errorCallback(Error.OfNormal);
        stateCallback(state);
        return true;
      }
    } else {
      composer.DoBackSpace();
    }

    stateCallback(composer.IsEmpty && compositor.IsEmpty ? new InputState.EmptyIgnorePreviousState()
                                                         : BuildInputtingState());
    return true;
  }

  // MARK: - 處理 PC Delete (macOS Fn+BackSpace) 按鍵行為

  /// <summary>
  /// 處理 PC Delete (macOS Fn+BackSpace) 按鍵行為。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleDelete(InputStateProtocol state, Action<InputStateProtocol> stateCallback,
                            Action<Error> errorCallback) {
    if (state is not InputState.Inputting) return false;

    if (!composer.IsEmpty) {
      Tools.PrintDebugIntel("9C69908D");
      errorCallback(Error.OfNormal);
      stateCallback(state);
      return true;
    }

    if (CompositorCursorIndex == CompositorLength) {
      Tools.PrintDebugIntel("9B69938D");
      errorCallback(Error.OfNormal);
      stateCallback(state);
      return true;
    }

    DeleteCompositorReadingToTheFrontOfCursor();
    Walk();
    InputState.Inputting inputting = BuildInputtingState();
    stateCallback(string.IsNullOrEmpty(inputting.ComposingBuffer) ? new InputState.EmptyIgnorePreviousState()
                                                                  : inputting);
    return true;
  }

  // MARK: - 處理與當前文字輸入排版前後方向呈 90 度的那兩個方向鍵的按鍵行為

  /// <summary>
  /// 處理與當前文字輸入排版前後方向呈 90 度的那兩個方向鍵的按鍵行為。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleAbsorbedArrowKey(InputStateProtocol state, Action<InputStateProtocol> stateCallback,
                                      Action<Error> errorCallback) {
    if (state is not InputState.Inputting) return false;
    if (!composer.IsEmpty) {
      Tools.PrintDebugIntel("9B6F908D");
      errorCallback(Error.OfNormal);
    }
    stateCallback(state);
    return true;
  }

  // MARK: - 處理 Home 鍵的行為

  /// <summary>
  /// 處理 Home 鍵的行為。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleHome(InputStateProtocol state, Action<InputStateProtocol> stateCallback,
                          Action<Error> errorCallback) {
    if (state is not InputState.Inputting) return false;

    if (!composer.IsEmpty) {
      Tools.PrintDebugIntel("ABC44080");
      errorCallback(Error.OfNormal);
      stateCallback(state);
      return true;
    }

    if (CompositorCursorIndex != 0) {
      CompositorCursorIndex = 0;
      stateCallback(BuildInputtingState());
    } else {
      Tools.PrintDebugIntel("66D97F90");
      errorCallback(Error.OfNormal);
      stateCallback(state);
    }

    return true;
  }

  // MARK: - 處理 End 鍵的行為

  /// <summary>
  /// 處理 End 鍵的行為。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleEnd(InputStateProtocol state, Action<InputStateProtocol> stateCallback,
                         Action<Error> errorCallback) {
    if (state is not InputState.Inputting) return false;

    if (!composer.IsEmpty) {
      Tools.PrintDebugIntel("9B69908D");
      errorCallback(Error.OfNormal);
      stateCallback(state);
      return true;
    }

    if (CompositorCursorIndex != CompositorLength) {
      CompositorCursorIndex = CompositorLength;
      stateCallback(BuildInputtingState());
    } else {
      Tools.PrintDebugIntel("9B69908E");
      errorCallback(Error.OfNormal);
      stateCallback(state);
    }

    return true;
  }

  // MARK: - 處理 Esc 鍵的行為

  /// <summary>
  /// 處理 Esc 鍵的行為。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleEsc(InputStateProtocol state, Action<InputStateProtocol> stateCallback) {
    if (state is not InputState.Inputting) return false;

    if (Prefs.EscToCleanInputBuffer) {
      // 若啟用了該選項，則清空組字器的內容與注拼槽的內容。
      // 此乃 macOS 內建注音輸入法預設之行為，但不太受 Windows 使用者群體之待見。
      Clear();
      stateCallback(new InputState.EmptyIgnorePreviousState());
    } else {
      // 如果注拼槽不是空的話，則清空之。
      if (composer.IsEmpty) return true;
      composer.Clear();
      stateCallback(compositor.IsEmpty ? new InputState.EmptyIgnorePreviousState() : BuildInputtingState());
    }

    return true;
  }

  // MARK: - 處理向前方向鍵的行為

  /// <summary>
  /// 處理向前方向鍵的行為。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="input">輸入按鍵訊號。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleForward(InputStateProtocol state, InputSignalProtocol input,
                             Action<InputStateProtocol> stateCallback, Action<Error> errorCallback) {
    if (state is not InputState.Inputting currentState) return false;

    if (!composer.IsEmpty) {
      Tools.PrintDebugIntel("B3BA5257");
      errorCallback(Error.OfNormal);
      stateCallback(state);
      return true;
    }

    if (input.IsShiftHold()) {
      // Shift + Right
      if (currentState.CursorIndex < currentState.ComposingBuffer.Length) {
        int nextPosition = U8Utils.GetU16NextPositionFor(currentState.ComposingBuffer, currentState.CursorIndex);
        InputState.Marking marking = new(currentState.ComposingBuffer, currentState.CursorIndex, nextPosition,
                                         CurrentReadings) { TooltipForInputting = currentState.Tooltip };
        stateCallback(marking);
      } else {
        Tools.PrintDebugIntel("BB7F6DB9");
        errorCallback(Error.OfNormal);
        stateCallback(state);
      }
    } else {
      if (CompositorCursorIndex < CompositorLength) {
        CompositorCursorIndex += 1;
        stateCallback(BuildInputtingState());
      } else {
        Tools.PrintDebugIntel("A96AAD58");
        errorCallback(Error.OfNormal);
        stateCallback(state);
      }
    }

    return true;
  }

  // MARK: - 處理向後方向鍵的行為

  /// <summary>
  /// 處理向後方向鍵的行為。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="input">輸入按鍵訊號。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleBackward(InputStateProtocol state, InputSignalProtocol input,
                              Action<InputStateProtocol> stateCallback, Action<Error> errorCallback) {
    if (state is not InputState.Inputting currentState) return false;

    if (!composer.IsEmpty) {
      Tools.PrintDebugIntel("6ED95318");
      errorCallback(Error.OfNormal);
      stateCallback(state);
      return true;
    }

    if (input.IsShiftHold()) {
      // Shift + Right
      if (currentState.CursorIndex > 0) {
        int nextPosition = U8Utils.GetU16PreviousPositionFor(currentState.ComposingBuffer, currentState.CursorIndex);
        InputState.Marking marking = new(currentState.ComposingBuffer, currentState.CursorIndex, nextPosition,
                                         CurrentReadings) { TooltipForInputting = currentState.Tooltip };
        stateCallback(marking);
      } else {
        Tools.PrintDebugIntel("D326DEA3");
        errorCallback(Error.OfNormal);
        stateCallback(state);
      }
    } else {
      if (CompositorCursorIndex > 0) {
        CompositorCursorIndex -= 1;
        stateCallback(BuildInputtingState());
      } else {
        Tools.PrintDebugIntel("7045E6F");
        errorCallback(Error.OfNormal);
        stateCallback(state);
      }
    }

    return true;
  }

  // MARK: - 處理上下文候選字詞輪替（Tab 按鍵，或者 Shift+Space）

  /// <summary>
  /// 以給定之參數來處理上下文候選字詞之輪替。
  /// </summary>
  /// <param name="state">當前狀態。</param>
  /// <param name="reverseModifier">是否有控制輪替方向的修飾鍵輸入。</param>
  /// <param name="stateCallback">狀態回呼。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>將按鍵行為「是否有處理掉」藉由 ctlInputMethod 回報給 IMK。</returns>
  private bool HandleInlineCandidateRotation(InputStateProtocol state, bool reverseModifier,
                                             Action<InputStateProtocol> stateCallback, Action<Error> errorCallback) {
    if (composer.IsEmpty && (compositor.IsEmpty || walkedAnchors.Count == 0)) return false;
    if (state is not InputState.Inputting) {
      if (state is not InputState.Empty) {
        Tools.PrintDebugIntel("6044F081");
        errorCallback(Error.OfNormal);
        return true;
      }
      // 不妨礙使用者平時輸入 Tab 的需求。
      return false;
    }

    if (!composer.IsEmpty) {
      Tools.PrintDebugIntel("A2DAF7BC");
      errorCallback(Error.OfNormal);
      return true;
    }

    List<string> candidates = CandidatesArray(fixOrder: true);
    if (candidates.Count == 0) {
      Tools.PrintDebugIntel("3378A6DF");
      errorCallback(Error.OfNormal);
      return true;
    }

    int length = 0;
    NodeAnchor currentAnchor = new();
    int cursorIndex = Math.Min(ActualCandidateCursorIndex + (Prefs.UseRearCursorMode ? 1 : 0), CompositorLength);
    foreach (NodeAnchor anchor in walkedAnchors) {
      length += anchor.SpanningLength;
      if (length >= cursorIndex) {
        currentAnchor = anchor;
        break;
      }
    }

    if (currentAnchor.Node == null) {
      Tools.PrintDebugIntel("4F2DEC2F");
      errorCallback(Error.OfNormal);
      return true;
    }

    Node currentNode = currentAnchor.Node;
    string currentValue = currentNode.CurrentKeyValue.Value;

    int currentIndex = 0;
    if (currentNode.Score < Node.ConSelectedCandidateScore) {
      // 只要是沒有被使用者手動選字過的（節錨下的）節點，
      // 就從第一個候選字詞開始，這樣使用者在敲字時就會優先匹配
      // 那些字詞長度不小於 2 的單元圖。換言之，如果使用者敲了兩個
      // 注音讀音、卻發現這兩個注音讀音各自的單字權重遠高於由這兩個
      // 讀音組成的雙字詞的權重、導致這個雙字詞並未在爬軌時被自動
      // 選中的話，則使用者可以直接摁下本函式對應的按鍵來輪替候選字即可。
      // （預設情況下是 (Shift+)Tab 來做正 (反) 向切換，但也可以用
      // Shift(+CMD)+Space 來切換、以應對臉書綁架 Tab 鍵的情況。
      if (candidates[0] == currentValue)
        // 如果第一個候選字詞是當前節點的候選字詞的值的話，
        // 那就切到下一個（或上一個，也就是最後一個）候選字詞。
        currentIndex = reverseModifier ? candidates.Count - 1 : 1;
    } else {
      foreach (string candidate in candidates) {
        if (candidate == currentValue) {
          if (reverseModifier) {
            if (currentIndex == 0) {
              currentIndex = candidates.Count - 1;
            } else {
              currentIndex -= 1;
            }
          } else {
            currentIndex += 1;
          }
          break;
        }
        currentIndex += 1;
      }
    }

    if (currentIndex >= candidates.Count) {
      currentIndex = 0;
    }

    FixNode(candidates[currentIndex], respectCursorPushing: false);

    stateCallback(BuildInputtingState());
    return true;
  }
}
}