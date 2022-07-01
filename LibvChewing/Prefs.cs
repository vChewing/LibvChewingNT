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
public enum InputMode { ImeModeNIL = 0409, ImeModeCHS = 0804, ImeModeCHT = 0404 }

public partial struct Prefs {
  // MARK: - Input Modes

  public static int CurrentInputLocale = 0409;
  public static InputMode CurrentInputMode {
    get {
      switch (CurrentInputLocale) {
        case 0409:
        case 0804:
          return (InputMode)CurrentInputLocale;
        default:
          return InputMode.ImeModeNIL;
      }
    }
    set => CurrentInputLocale = (int)value;
  }

  // MARK: - Other Stored properties

  public static string BasicKeyboardLayout = "com.apple.keylayout.ZhuyinBopomofo";

  public static int MaxCandidateLength = 10;
  public static int MinCandidateLength => AllowBoostingSingleKanjiAsUserPhrase ? 1 : 2;

  public static bool AllowBoostingSingleKanjiAsUserPhrase = false;

  public static bool IsDebugModeEnabled = false;
  public static int ComposingBufferSize = 20;
  public static bool PhraseReplacementEnabled = false;
  public static bool CNS11643Enabled = false;
  public static bool SymbolInputEnabled = true;
  public static int MandarinParser = 0;
  public static bool UseRearCursorMode = false;
  public static bool UseSCPCTypingMode = false;
  public static bool MoveCursorAfterSelectingCandidate = true;
  public static bool FetchSuggestionsFromUserOverrideModel = true;
  public static bool SpecifyShiftTabKeyBehavior = false;
  public static bool SpecifyShiftSpaceKeyBehavior = true;
  public static bool HalfWidthPunctuationEnabled = false;
  public static bool ShowHanyuPinyinInCompositionBuffer = false;
  public static bool UseFixecCandidateOrderOnSelection = false;
  public static bool InlineDumpPinyinInLieuOfZhuyin = false;
  public static bool EscToCleanInputBuffer = true;
  public static bool AssociatedPhrasesEnabled = false;
  public static bool ChooseCandidateUsingSpace = true;
}
}
