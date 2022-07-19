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
public partial class KeyHandler {
  /// <summary>
  /// 當且僅當選字窗出現時，對於經過初次篩選處理的輸入訊號的處理均藉由此函式來進行。
  /// </summary>
  /// <param name="input">輸入訊號。</param>
  /// <param name="state">給定狀態（通常為當前狀態）。</param>
  /// <param name="stateCallback">狀態回呼，交給對應的型別內的專有函式來處理。</param>
  /// <param name="errorCallback">錯誤回呼。</param>
  /// <returns>告知 IMK「該按鍵是否已經被輸入法攔截處理」。</returns>
  public bool HandleCandidate(InputStateProtocol state, InputSignalProtocol input,
                              Action<InputStateProtocol> stateCallback, Action<Error> errorCallback) {
    string inputText = input.InputText;
    ushort charCode = input.CharCode;
    if (theDelegate == null) {
      Tools.PrintDebugIntel("06661F6E");
      errorCallback(Error.OfNormal);
      return true;
    }
    CtlCandidate ctlCandidateCurrent = theDelegate.CtlCandidate();

    // MARK: 取消選字 (Cancel Candidate)

    bool CancelCandidateKey = input.IsBackSpace() || input.isEsc() || input.IsDelete() ||
                              (input.IsCursorBackward() || input.IsCursorForward()) && input.IsShiftHold();

    if (CancelCandidateKey) {
      if (state is InputState.AssociatedPhrases || Prefs.UseSCPCTypingMode || IsCompositorEmpty) {
        // 如果此時發現當前組字緩衝區為真空的情況的話，
        // 就將當前的組字緩衝區析構處理、強制重設輸入狀態。
        // 否則，一個本不該出現的真空組字緩衝區會使前後方向鍵與 BackSpace 鍵失靈。
        // 所以這裡需要對 isCompositorEmpty 做判定。
        Clear();
        stateCallback(new InputState.EmptyIgnorePreviousState());
      } else {
        stateCallback(BuildInputtingState());
      }
      return true;
    }

    // MARK: Enter

    if (input.IsEnter()) {
      if (state is InputState.AssociatedPhrases) {
        Clear();
        stateCallback(new InputState.EmptyIgnorePreviousState());
        return true;
      }
      theDelegate?.KeyHandler(this, ctlCandidateCurrent.SelectedCandidatedIndex, ctlCandidateCurrent);
      return true;
    }

    // MARK: Tab

    if (input.IsTab()) {
      bool updated = Prefs.SpecifyShiftTabKeyBehavior ? input.IsShiftHold() ? ctlCandidateCurrent.ShowPreviousPage()
                                                                            : ctlCandidateCurrent.ShowNextPage()
                     : input.IsShiftHold() ? ctlCandidateCurrent.HighlightPreviousCandidate()
                                                      : ctlCandidateCurrent.HighlightNextCandidate();
      if (updated) return true;
      Tools.PrintDebugIntel("9B691919");
      errorCallback(Error.OfNormal);
      return true;
    }

    // MARK: Space

    if (input.IsSpace()) {
      bool updated =
          Prefs.SpecifyShiftSpaceKeyBehavior
              ? input.IsShiftHold() ? ctlCandidateCurrent.HighlightNextCandidate() : ctlCandidateCurrent.ShowNextPage()
          : input.IsShiftHold() ? ctlCandidateCurrent.ShowNextPage()
                                : ctlCandidateCurrent.HighlightNextCandidate();
      if (updated) return true;
      Tools.PrintDebugIntel("A11C781F");
      errorCallback(Error.OfNormal);
      return true;
    }

    // MARK: PgDn

    if (input.IsPageDown()) {
      bool updated = ctlCandidateCurrent.ShowNextPage();
      if (updated) return true;
      Tools.PrintDebugIntel("9B691919");
      errorCallback(Error.OfNormal);
      return true;
    }

    // MARK: PgUp

    if (input.IsPageUp()) {
      bool updated = ctlCandidateCurrent.ShowPreviousPage();
      if (updated) return true;
      Tools.PrintDebugIntel("9569955D");
      errorCallback(Error.OfNormal);
      return true;
    }

    // MARK: Left Arrow

    if (input.IsLeft()) {
      switch (ctlCandidateCurrent.CurrentLayout) {
        case CtlCandidate.Layout.Horizontal:
          if (!ctlCandidateCurrent.HighlightPreviousCandidate()) {
            Tools.PrintDebugIntel("1145148D");
            errorCallback(Error.OfNormal);
          }
          break;
        case CtlCandidate.Layout.Vertical:
          if (!ctlCandidateCurrent.ShowPreviousPage()) {
            Tools.PrintDebugIntel("1919810D");
            errorCallback(Error.OfNormal);
          }
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      return true;
    }

    // MARK: Right Arrow

    if (input.IsRight()) {
      switch (ctlCandidateCurrent.CurrentLayout) {
        case CtlCandidate.Layout.Horizontal:
          if (!ctlCandidateCurrent.HighlightNextCandidate()) {
            Tools.PrintDebugIntel("9B65138D");
            errorCallback(Error.OfNormal);
          }
          break;
        case CtlCandidate.Layout.Vertical:
          if (!ctlCandidateCurrent.ShowNextPage()) {
            Tools.PrintDebugIntel("9244908D");
            errorCallback(Error.OfNormal);
          }
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      return true;
    }

    // MARK: Up Arrow

    if (input.IsUp()) {
      switch (ctlCandidateCurrent.CurrentLayout) {
        case CtlCandidate.Layout.Horizontal:
          if (!ctlCandidateCurrent.ShowPreviousPage()) {
            Tools.PrintDebugIntel("9B614524");
            errorCallback(Error.OfNormal);
          }
          break;
        case CtlCandidate.Layout.Vertical:
          if (!ctlCandidateCurrent.HighlightPreviousCandidate()) {
            Tools.PrintDebugIntel("ASD9908D");
            errorCallback(Error.OfNormal);
          }
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      return true;
    }

    // MARK: Down Arrow

    if (input.IsDown()) {
      switch (ctlCandidateCurrent.CurrentLayout) {
        case CtlCandidate.Layout.Horizontal:
          if (!ctlCandidateCurrent.ShowNextPage()) {
            Tools.PrintDebugIntel("92B990DD");
            errorCallback(Error.OfNormal);
          }
          break;
        case CtlCandidate.Layout.Vertical:
          if (!ctlCandidateCurrent.HighlightNextCandidate()) {
            Tools.PrintDebugIntel("6B99908D");
            errorCallback(Error.OfNormal);
          }
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      return true;
    }

    // MARK: Home Key

    if (input.IsHome()) {
      if (ctlCandidateCurrent.SelectedCandidatedIndex == 0) {
        Tools.PrintDebugIntel("9B6EDE8D");
        errorCallback(Error.OfNormal);
      } else {
        ctlCandidateCurrent.SelectedCandidatedIndex = 0;
      }
    }

    // MARK: End Key

    List<string> candidates = new();

    switch (state) {
      case InputState.ChoosingCandidate candidate:
        candidates = candidate.Candidates;
        break;
      case InputState.AssociatedPhrases phrases:
        candidates = phrases.Candidates;
        break;
    }

    if (candidates.Count == 0) return false;
    if (input.IsEnd()) {
      if (ctlCandidateCurrent.SelectedCandidatedIndex == candidates.Count - 1) {
        Tools.PrintDebugIntel("9B69AAAD");
        errorCallback(Error.OfNormal);
      } else {
        ctlCandidateCurrent.SelectedCandidatedIndex = candidates.Count - 1;
      }
    }

    // MARK: 聯想詞處理 (Associated Phrases)

    if (state is InputState.AssociatedPhrases) {
      if (!input.IsShiftHold()) return false;
    }

    int index = int.MaxValue;
    string match = state is InputState.AssociatedPhrases ? input.InputTextIgnoringModifiers() : inputText;

    int j = 0;
    while (j < ctlCandidateCurrent.KeyLabels.Count) {
      CandidateKeyLabel label = ctlCandidateCurrent.KeyLabels[j];
      if (string.Compare(match, label.Key, StringComparison.OrdinalIgnoreCase) == 0) {
        index = j;
        break;
      }
      j += 1;
    }

    if (index != int.MaxValue) {
      int candidateIndex = ctlCandidateCurrent.CandidateIndexAtKeyLabelIndex(index);
      if (candidateIndex != int.MaxValue && theDelegate != null) {
        theDelegate.KeyHandler(this, candidateIndex, ctlCandidateCurrent);
        return true;
      }
    }

    if (state is InputState.AssociatedPhrases) return false;

    // MARK: 逐字選字模式的處理 (SCPC Mode Processing)

    if (Prefs.UseSCPCTypingMode) {
      // 檢查：
      // - 是否是針對當前注音排列/拼音輸入種類專門提供的標點符號。
      // - 是否是需要摁修飾鍵才可以輸入的那種標點符號。

      string punctuationNamePrefix() {
        if (Prefs.HalfWidthPunctuationEnabled) {
          return "_half_punctuation_";
        }
        if (input.IsAltHold() && !input.IsControlHold()) {
          return "_alt_punctuation_";
        }
        if (input.IsControlHold() && !input.IsAltHold()) {
          return "_ctrl_punctuation_";
        }
        if (input.IsAltHold() && input.IsControlHold()) {
          return "_alt_ctrl_punctuation_";
        }
        return "_punctuation_";
      }

      string parser = CurrentMandarinParser();

      string customPunctuation = punctuationNamePrefix() + parser + Convert.ToChar(charCode);

      // 看看這個輸入是否是不需要修飾鍵的那種標點鍵輸入。

      string punctuation = punctuationNamePrefix() + Convert.ToChar(charCode);

      bool shouldAutoSelectCandidate = composer.InputValidityCheck(charCode) ||
                                       IfLangModelHasUnigramsFor(customPunctuation) ||
                                       IfLangModelHasUnigramsFor(punctuation);

      if (!shouldAutoSelectCandidate && input.IsUpperCaseASCIILetterKey()) {
        string letter = "_letter_" + (char)charCode;
        if (IfLangModelHasUnigramsFor(letter)) shouldAutoSelectCandidate = true;
      }

      if (shouldAutoSelectCandidate) {
        int candidateIndex = ctlCandidateCurrent.CandidateIndexAtKeyLabelIndex(0);
        if (candidateIndex == int.MaxValue || theDelegate == null) return true;
        theDelegate.KeyHandler(this, candidateIndex, ctlCandidateCurrent);
        Clear();
        InputState.EmptyIgnorePreviousState empty = new();
        stateCallback(empty);
        return Handle(input, empty, stateCallback, errorCallback);
      }
    }

    // MARK: 終末處理

    Tools.PrintDebugIntel("172A0F81");
    errorCallback(Error.OfNormal);
    return true;
  }
}
}