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
      // 因為「.EmptyIgnoringPreviousState」會在處理之後被自動轉為「.Empty」，所以不需要單獨判斷。
      if (state is InputState.Empty or InputState.Deactivated) return false;
      Tools.PrintDebugIntel("550BCF7B: KeyHandler just refused an invalid input.");
      errorCallback(Error.OfNormal);
      stateCallback(state);
      return true;
    }

    // 如果當前組字器為空的話，就不再攔截某些修飾鍵，畢竟這些鍵可能會會用來觸發某些功能。
    bool isFunctionKey =
        input.IsControlHotKey() || input.IsAltHotKey() || input.IsCommandHold() || input.IsNumericPadKey();
    if (state is not InputState.NotEmpty && state is not InputState.AssociatedPhrases && isFunctionKey) return false;

    // MARK: Caps Lock processing.

    // 若 Caps Lock 被啟用的話，則暫停對注音輸入的處理。
    // 這裡的處理原先是給威注音曾經有過的 Shift 切換英數模式來用的，但因為採 Chromium 核
    // 心的瀏覽器會讓 IMK 無法徹底攔截對 Shift 鍵的單擊行為、導致這個模式的使用體驗非常糟
    // 糕，故僅保留以 Caps Lock 驅動的英數模式。
    // 要是拿來用於 Windows 輸入法開發的話，則該模式可以考慮用來製作成用 Shift 切換的英文模式。
    if (input.IsBackSpace() || input.IsEnter() || input.IsCursorClockLeft() || input.IsCursorClockRight() ||
        input.IsCursorForward() || input.IsCursorBackward()) {
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

    // TODO: 這裡有必要針對當前作業系統做差分處理。
    if (input.IsNumericPadKey()) {
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

    bool? compositionHandled = HandleComposition(input, state, stateCallback, errorCallback);
    if (compositionHandled is {} compositionReallyHandled) return compositionReallyHandled;

    // MARK: 用上下左右鍵呼叫選字窗 (Calling candidate window using Up / Down or PageUp / PageDn.)

    if (state is InputState.NotEmpty currentState && composer.IsEmpty && !input.IsAltHold() &&
        (input.IsCursorClockLeft() || input.IsCursorClockRight() || input.IsSpace() || input.IsPageDown() ||
         input.IsPageUp() || input.IsTab() && Prefs.SpecifyShiftTabKeyBehavior)) {
      if (input.IsSpace()) {
        // 倘若沒有在偏好設定內將 Space 空格鍵設為選字窗呼叫用鍵的話………
        if (!Prefs.ChooseCandidateUsingSpace) {
          if (compositor.Cursor >= compositor.Length) {
            string composingBuffer = currentState.ComposingBuffer;
            if (string.IsNullOrEmpty(composingBuffer)) {
              stateCallback(new InputState.Committing(composingBuffer));
            }
            Clear();
            stateCallback(new InputState.Committing(textToCommit: " "));
            stateCallback(new InputState.Empty());
          } else if (currentLM.HasUnigramsFor(" ")) {
            compositor.InsertReading(" ");
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

    // MARK: Clock-Left & Clock-Right

    if (input.IsCursorClockRight() || input.IsCursorClockLeft()) {
      if (input.IsAltHold() && state is InputState.Inputting) {
        if (input.IsCursorClockRight())
          return HandleInlineCandidateRotation(state, false, stateCallback, errorCallback);
        if (input.IsCursorClockLeft()) return HandleInlineCandidateRotation(state, true, stateCallback, errorCallback);
      }
      return HandleClockKey(state, stateCallback, errorCallback);
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
        if (currentLM.HasUnigramsFor("_punctuation_list")) {
          // 不要在注音沒敲完整的情況下叫出統合符號選單。
          if (!composer.IsEmpty) {
            Tools.PrintDebugIntel("17446655");
            errorCallback(Error.OfNormal);
          } else {
            compositor.InsertReading(reading: "_punctuation_list");
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

    // MARK: 全形/半形阿拉伯數字輸入 (FW / HW Arabic Numbers Input)

    if (state is InputState.Empty) {
      if (input.IsMainAreaNumKey() && input.IsShiftHold() && input.IsAltHold() && !input.IsControlHold() &&
          !input.IsCommandHold()) {
        string stringRAW = input.InputText;
        char[] c = stringRAW.ToCharArray();
        for (int i = 0; i < c.Length; i++) {
          if (c[i] == 32) {
            c[i] = (char)12288;
            continue;
          }
          if (c[i] < 127) c[i] = (char)(c[i] + 65248);
        }
        if (Prefs.HalfWidthPunctuationEnabled) stringRAW = new(c);
        stateCallback(new InputState.Committing(textToCommit: stringRAW));
        stateCallback(new InputState.Empty());
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

    // MARK: 全形/半形空白 (Full-Width / Half-Width Space)

    // 該功能僅可在當前組字區沒有任何內容的時候使用。
    if (state is InputState.Empty) {
      if (input.IsSpace() && !input.IsAltHold() && !input.IsControlHold() && !input.IsCommandHold()) {
        stateCallback(new InputState.Committing(textToCommit: input.IsShiftHold() ? "　" : " "));
      }
      stateCallback(new InputState.Empty());
      return true;
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