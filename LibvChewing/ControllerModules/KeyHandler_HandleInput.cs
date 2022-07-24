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

// 該檔案乃按鍵調度模組當中「用來規定當 IMK 接受按鍵訊號時且首次交給按鍵調度模組處理時、
// 按鍵調度模組要率先處理」的部分。據此判斷是否需要將按鍵處理委派給其它成員函式。

using System.Runtime.InteropServices;

namespace LibvChewing {
public partial class KeyHandler {
  // MARK: - § 根據狀態調度按鍵輸入 (Handle Input with States)

  /// <summary>
  /// 對於輸入訊號的第一關處理均藉由此函式來進行。
  /// </summary>
  /// <param name="input">輸入訊號。</param>
  /// <param name="state">給定狀態（通常為當前狀態）。</param>
  /// <param name="stateCallback">狀態回呼，交給對應的型別內的專有函式來處理。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>告知 IMK「該按鍵是否已經被輸入法攔截處理」。</returns>
  public bool Handle(InputSignalProtocol input, InputStateProtocol state, Action<InputStateProtocol> stateCallback,
                     Action<Error> errorCallback) {
    // 如果按鍵訊號內的 inputTest 是空的話，則忽略該按鍵輸入，因為很可能是功能修飾鍵。
    if (string.IsNullOrEmpty(input.InputText)) return false;

    string inputText = input.InputText;
    ushort charCode = input.CharCode;

    // 提前過濾掉一些不合規的按鍵訊號輸入，免得相關按鍵訊號被送給 Megrez 引發輸入法崩潰。
    if (input.IsInvalidInput()) {
      // 在「.Empty(IgnoringPreviousState) 與 .Deactivated」狀態下的首次不合規按鍵輸入可以直接放行。
      // 因為「.EmptyIgnorePreviousState」會在處理之後被自動轉為「.Empty」，所以不需要單獨判斷。
      if (state is InputState.Empty or InputState.Deactivated) return false;
      Tools.PrintDebugIntel("550BCF7B: KeyHandler just refused an invalid input.");
      errorCallback(Error.OfNormal);
      stateCallback(state);
      return true;
    }

    // 如果當前組字器為空的話，就不再攔截某些修飾鍵，畢竟這些鍵可能會會用來觸發某些功能。
    bool isFunctionKey =
        input.IsControlHotKey() || input.IsAltHotKey() || input.IsCommandHold() || input.IsNumericPad();
    if (state is not InputState.NotEmpty && state is not InputState.AssociatedPhrases && isFunctionKey) return false;

    // MARK: Caps Lock processing.

    // 若 Caps Lock 被啟用的話，則暫停對注音輸入的處理。
    // 這裡的處理原先是給威注音曾經有過的 Shift 切換英數模式來用的，但因為採 Chromium 核
    // 心的瀏覽器會讓 IMK 無法徹底攔截對 Shift 鍵的單擊行為、導致這個模式的使用體驗非常糟
    // 糕，故僅保留以 Caps Lock 驅動的英數模式。
    // 要是拿來用於 Windows 輸入法開發的話，則該模式可以考慮用來製作成用 Shift 切換的英文模式。
    if (input.IsBackSpace() || input.IsEnter() || input.IsAbsorbedArrowKey() || input.IsExtraChooseCandidateKey() ||
        input.IsExtraChooseCandidateKeyReverse() || input.IsCursorForward() || input.IsCursorBackward()) {
      // 略過對 BackSpace 的處理。
    } else if (input.IsCapsLockOn()) {
      Clear();
      stateCallback(new InputState.Empty());

      // 摁 Shift 的話，無須額外處理，因為直接就會敲出大寫字母。
      if (input.IsShiftHold()) return false;

      // macOS InputMethodKit 限定處理:
      // 如果是 ASCII 當中的不可列印的字元的話，不使用「insertText:replacementRange:」。
      // 某些應用無法正常處理非 ASCII 字符的輸入。
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && char.IsAscii((char)charCode) &&
          char.IsControl((char)charCode))
        return false;

      // 將整個組字區的內容遞交給客體應用。
      stateCallback(new InputState.Committing(textToCommit: inputText.ToLower()));
      stateCallback(new InputState.Empty());

      return true;
    }

    // MARK: 處理數字小鍵盤 (Numeric Pad Processing)

    if (input.IsNumericPad()) {
      if (!input.IsLeft() && !input.IsRight() && !input.IsDown() && !input.IsUp() && !input.IsSpace() &&
          !char.IsControl((char)charCode)) {
        Clear();
        stateCallback(new InputState.Empty());
        stateCallback(new InputState.Committing(textToCommit: inputText.ToLower()));
        stateCallback(new InputState.Empty());
        return true;
      }
    }

    // MARK: 處理候選字詞 (Handle Candidates) & 聯想詞 (Handle Associated Phrases)

    switch (state) {
      // 處理候選字詞
      case InputState.ChoosingCandidate:
        return HandleCandidate(state, input, stateCallback, errorCallback);
      // 處理聯想詞
      case InputState.AssociatedPhrases when HandleCandidate(state, input, stateCallback, errorCallback):
        return true;
      case InputState.AssociatedPhrases:
        stateCallback(new InputState.Empty());
        break;
    }

    // MARK: 處理標記範圍、以便決定要把哪個範圍拿來新增使用者(濾除)語彙 (Handle Marking)

    if (state is InputState.Marking marking) {
      if (HandleMarkingState(marking, input, stateCallback, errorCallback)) return true;
      stateCallback(marking.ConvertedToInputting());
    }

    // MARK: 注音按鍵輸入處理 (Handle BPMF Keys)

    bool keyConsumedByReading = false;
    bool skipPhoneticHandling = input.IsReservedKey() || input.IsControlHold() || input.IsAltHold();

    // 這裡 inputValidityCheck() 是讓注拼槽檢查 charCode 這個 UniChar 是否是合法的注音輸入。
    // 如果是的話，就將這次傳入的這個按鍵訊號塞入注拼槽內且標記為「keyConsumedByReading」。
    // 函式 composer.receiveKey() 可以既接收 String 又接收 UniChar。
    if (skipPhoneticHandling && composer.InputValidityCheck(charCode)) {
      composer.ReceiveKey(charCode);
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
      if (!IfLangModelHasUnigramsFor(readingKey)) {
        Tools.PrintDebugIntel($"B49C0979：語彙庫內無「{readingKey}」的匹配記錄。");
        errorCallback(Error.OfNormal);
        composer.Clear();
        // 根據「組字器是否為空」來判定回呼哪一種狀態。
        stateCallback(compositor.IsEmpty ? new InputState.EmptyIgnorePreviousState() : BuildInputtingState());
        return true;  // 向 IMK 報告說這個按鍵訊號已經被輸入法攔截處理了。
      }

      // 將該讀音插入至組字器內的軌格當中。
      InsertToCompositorAtCursor(reading: readingKey);

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

    // 如果此時這個選項是 true 的話，可知當前注拼槽輸入了聲調、且上一次按鍵不是聲調按鍵。
    // 比方說大千傳統佈局敲「6j」會出現「ˊㄨ」但並不會被認為是「ㄨˊ」，因為先輸入的調號
    // 並非用來確認這個注音的調號。除非是：「ㄨˊ」「ˊㄨˊ」「ˊㄨˇ」「ˊㄨ 」等。
    if (keyConsumedByReading) {
      // 以回呼組字狀態的方式來執行 updateClientComposingBuffer()。
      stateCallback(BuildInputtingState());
      return true;
    }

    // MARK: 用上下左右鍵呼叫選字窗 (Calling candidate window using Up / Down or PageUp / PageDn.)

    if (state is InputState.NotEmpty currentState && composer.IsEmpty && !input.IsAltHold() &&
        (input.IsExtraChooseCandidateKey() || input.IsExtraChooseCandidateKeyReverse() || input.IsSpace() ||
         input.IsPageDown() || input.IsPageUp() || input.IsTab() && Prefs.SpecifyShiftTabKeyBehavior ||
         input.IsTypingVertical && input.IsVerticalTypingOnlyChooseCandidateKey())) {
      if (input.IsSpace()) {
        // 倘若沒有在偏好設定內將 Space 空格鍵設為選字窗呼叫用鍵的話………
        if (!Prefs.ChooseCandidateUsingSpace) {
          if (compositor.Cursor >= CompositorLength) {
            string composingBuffer = currentState.ComposingBuffer;
            if (string.IsNullOrEmpty(composingBuffer)) {
              stateCallback(new InputState.Committing(composingBuffer));
            }
            Clear();
            stateCallback(new InputState.Committing(textToCommit: " "));
            stateCallback(new InputState.Empty());
          } else if (IfLangModelHasUnigramsFor(" ")) {
            InsertToCompositorAtCursor(" ");
            string textToCommit = CommitOverflownCompositionAndWalk();
            InputState.Inputting inputting = BuildInputtingState();
            inputting.TextToCommit = textToCommit;
            stateCallback(inputting);
          }
          return true;
        }
        if (input.IsShiftHold())
          // 臉書等網站會攔截 Tab 鍵，所以用 Shift+CMD+Space 對候選字詞做正向/反向輪替。
          return HandleInlineCandidateRotation(state, reverseModifier: input.IsCommandHold(), stateCallback,
                                               errorCallback);
      }
      stateCallback(BuildCandidate(currentState, input.IsTypingVertical));
      return true;
    }

    // MARK: - 雜項鍵處理。

    // MARK: Esc

    if (input.isEsc()) return HandleEsc(state, stateCallback);

    // MARK: Tab

    if (input.IsTab())
      return HandleInlineCandidateRotation(state, reverseModifier: input.IsShiftHold(), stateCallback, errorCallback);

    // MARK: Cursor backward

    if (input.IsCursorBackward()) return HandleBackward(state, input, stateCallback, errorCallback);

    // MARK: Cursor forward

    if (input.IsCursorForward()) return HandleForward(state, input, stateCallback, errorCallback);

    // MARK: Home

    if (input.IsHome()) return HandleHome(state, stateCallback, errorCallback);

    // MARK: End

    if (input.IsEnd()) return HandleEnd(state, stateCallback, errorCallback);

    // MARK: Ctrl+PgLf or Shift+PgLf

    if ((input.IsControlHold() || input.IsShiftHold()) && input.IsAltHold() && input.IsLeft())
      return HandleHome(state, stateCallback, errorCallback);

    // MARK: Ctrl+PgRt or Shift+PgRt

    if ((input.IsControlHold() || input.IsShiftHold()) && input.IsAltHold() && input.IsRight())
      return HandleEnd(state, stateCallback, errorCallback);

    // MARK: AbsorbedArrowKey

    if (input.IsAbsorbedArrowKey() || input.IsExtraChooseCandidateKey() || input.IsExtraChooseCandidateKeyReverse()) {
      if (input.IsAltHold() && state is InputState.Inputting) {
        if (input.IsExtraChooseCandidateKey()) 
          return HandleInlineCandidateRotation(state, false, stateCallback, errorCallback);
        if (input.IsExtraChooseCandidateKeyReverse()) 
          return HandleInlineCandidateRotation(state, true, stateCallback, errorCallback);
      }
      return HandleAbsorbedArrowKey(state, stateCallback, errorCallback);
    }

    // MARK: BackSpace

    if (input.IsBackSpace()) return HandleBackSpace(state, stateCallback, errorCallback);

    // MARK: Delete

    if (input.IsDelete()) return HandleDelete(state, stateCallback, errorCallback);

    // MARK: Enter

    if (input.IsEnter())
      return input.IsCommandHold() && input.IsControlHold() ? input.IsAltHold()
                                                                  ? HandleCtrlAltCommandEnter(state, stateCallback)
                                                                  : HandleCtrlCommandEnter(state, stateCallback)
                                                            : HandleEnter(state, stateCallback);

    // MARK: -

    // MARK: Punctuation list

    if (input.IsSymbolMenuPhysicalKey() && !input.IsShiftHold()) {
      if (input.IsAltHold()) {
        if (IfLangModelHasUnigramsFor("_punctuation_list")) {
          // 不要在注音沒敲完整的情況下叫出統合符號選單。
          if (!composer.IsEmpty) {
            Tools.PrintDebugIntel("17446655");
            errorCallback(Error.OfNormal);
          } else {
            InsertToCompositorAtCursor(reading: "_punctuation_list");
            string textToCommit = CommitOverflownCompositionAndWalk();
            InputState.Inputting inputting = BuildInputtingState();
            inputting.TextToCommit = textToCommit;
            stateCallback(inputting);
            stateCallback(BuildCandidate(inputting, input.IsTypingVertical));
          }
          return true;
        }
      } else {
        // 得在這裡先 commit buffer，不然會導致「在摁 ESC 離開符號選單時會重複輸入上一次的組字區的內容」的不當行為。
        // 於是這裡用「模擬一次 Enter 鍵的操作」使其代為執行這個 commit buffer 的動作。
        // 這裡不需要該函式所傳回的 bool 結果，所以用「_ =」解消掉。
        _ = HandleEnter(state, stateCallback);
        stateCallback(new InputState.SymbolTable(SymbolNode.Root, input.IsTypingVertical));
        return true;
      }
    }

    // MARK: Punctuation

    // 如果仍無匹配結果的話，先看一下：
    // - 是否是針對當前注音排列/拼音輸入種類專門提供的標點符號。
    // - 是否是需要摁修飾鍵才可以輸入的那種標點符號。

    string punctuationNamePrefix() => GeneratePunctuationNamePrefixWithInputSignal(input);

    string parser = CurrentMandarinParser();
    string customPunctuation = punctuationNamePrefix() + parser + Convert.ToChar(charCode);
    if (HandlePunctuation(customPunctuation, state, input.IsTypingVertical, stateCallback, errorCallback)) return true;

    // 如果仍無匹配結果的話，看看這個輸入是否是不需要修飾鍵的那種標點鍵輸入。
    string punctuation = punctuationNamePrefix() + Convert.ToChar(charCode);
    if (HandlePunctuation(punctuation, state, input.IsTypingVertical, stateCallback, errorCallback)) return true;

    // 這裡不使用小麥注音 2.2 版的組字區處理方式，而是直接由詞庫負責。
    if (input.IsUpperCaseASCIILetterKey()) {
      string letter = "_letter_" + Convert.ToChar(charCode);
      if (HandlePunctuation(letter, state, input.IsTypingVertical, stateCallback, errorCallback)) return true;
    }

    // MARK: - 終末處理 (Still Nothing)

    // 對剩下的漏網之魚做攔截處理、直接將當前狀態繼續回呼給 ctlInputMethod。
    // 否則的話，可能會導致輸入法行為異常：部分應用會阻止輸入法完全攔截某些按鍵訊號。
    // 砍掉這一段會導致「F1-F12 按鍵干擾組字區」的問題。
    // 暫時只能先恢復這段，且補上偵錯彙報機制，方便今後排查故障。
    if (state is not InputState.NotEmpty && composer.IsEmpty) return false;
    Tools.PrintDebugIntel($"Blocked data: charCode: {charCode}, keyCode: {input.KeyCode}");
    Tools.PrintDebugIntel("A9BFF20E");
    errorCallback(Error.OfNormal);
    stateCallback(state);
    return true;
  }
}
}