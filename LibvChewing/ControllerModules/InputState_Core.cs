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

// 註：所有 InputState 型別均不適合使用 Struct，因為 Struct 無法相互繼承派生。

/// <summary>
/// 所有 InputState 均遵守該協定：
/// </summary>
public interface InputStateProtocol {
  InputState.Type Type { get; }
}

/// <summary>
/// 此型別用以呈現輸入法控制器（ctlInputMethod）的各種狀態。<br />
/// 從實際角度來看，輸入法屬於有限態械（Finite State Machine）。其藉由滑鼠/鍵盤
/// 等輸入裝置接收輸入訊號，據此切換至對應的狀態，再根據狀態更新使用者介面內容，
/// 最終生成文字輸出、遞交給接收文字輸入行為的客體應用。此乃單向資訊流序，且使用
/// 者介面內容與文字輸出均無條件地遵循某一個指定的資料來源。<br />
/// InputState 型別用以呈現輸入法控制器正在做的事情，且分狀態儲存各種狀態限定的
/// 常數與變數。對輸入法而言，使用狀態模式（而非策略模式）來做這種常數變數隔離，
/// 可能會讓新手覺得會有些牛鼎烹雞，卻實際上變相減少了在程式維護方面的管理難度、
/// 不需要再在某個狀態下為了該狀態不需要的變數與常數的處置策略而煩惱。<br />
/// 對 InputState 型別下的諸多狀態的切換，應以生成新副本來取代舊有副本的形式來完
/// 成。唯一例外是 InputState.Marking、擁有可以將自身轉變為 InputState.Inputting
/// 的成員函式，但也只是生成副本、來交給輸入法控制器來處理而已。<br />
/// 輸入法控制器持下述狀態：<br />
/// - .Deactivated: 使用者沒在使用輸入法。<br />
/// - .AssociatedPhrases: 逐字選字模式內的聯想詞輸入狀態。因為逐字選字模式不需要在
///   組字區內存入任何東西，所以該狀態不受 .NotEmpty 的管轄。<br />
/// - .Empty: 使用者剛剛切換至該輸入法、卻還沒有任何輸入行為。抑或是剛剛敲字遞交給
///   客體應用、準備新的輸入行為。<br />
/// - .EmptyIgnorePreviousState: 與 Empty 類似，但會扔掉上一個狀態的內容、不將這些
///   內容遞交給客體應用。該狀態在處理完畢之後會被立刻切換至 .Empty()。<br />
/// - .Committing: 該狀態會承載要遞交出去的內容，讓輸入法控制器處理時代為遞交。<br />
/// - .NotEmpty: 非空狀態，是一種狀態大類、用以派生且代表下述諸狀態。<br />
/// - .Inputting: 使用者輸入了內容。此時會出現組字區（Compositor）。<br />
/// - .Marking: 使用者在組字區內標記某段範圍，可以決定是添入新詞、還是將這個範圍的
///   詞音組合放入語彙濾除清單。<br />
/// - .ChoosingCandidate: 叫出選字窗、允許使用者選字。<br />
/// - .SymbolTable: 波浪鍵符號選單專用的狀態，有自身的特殊處理。<br />
/// </summary>
public struct InputState {
  /// <summary>
  /// 用以讓每個狀態自描述的 enum。
  /// </summary>
  public enum Type {
    OfDeactivated,
    OfAssociatedPhrases,
    OfEmpty,
    OfEmptyIgnorePreviousState,
    OfCommitting,
    OfNotEmpty,
    OfInputting,
    OfMarking,
    OfChooseCandidate,
    OfSymbolTable
  }

  // MARK: - InputState.Deactivated

  /// <summary>
  /// .Empty: 使用者剛剛切換至該輸入法、卻還沒有任何輸入行為。<br />
  /// 抑或是剛剛敲字遞交給客體應用、準備新的輸入行為。
  /// </summary>
  public partial class Deactivated : InputStateProtocol {
    public Type Type => Type.OfDeactivated;
    public override string ToString() => "<InputState.Deactivated>";
  }

  // MARK: - InputState.Empty

  /// <summary>
  /// .Empty: 使用者剛剛切換至該輸入法、卻還沒有任何輸入行為。<br />
  /// 抑或是剛剛敲字遞交給客體應用、準備新的輸入行為。
  /// </summary>
  public partial class Empty : InputStateProtocol {
    virtual public Type Type => Type.OfEmpty;
    public string ComposingBuffer => "";
    public override string ToString() => "<InputState.Empty>";
  }

  // MARK: - InputState.EmptyIgnorePreviousState

  /// <summary>
  /// .EmptyIgnorePreviousState: 與 Empty 類似，<br />
  /// 但會扔掉上一個狀態的內容、不將這些內容遞交給客體應用。<br />
  /// 該狀態在處理完畢之後會被立刻切換至 .Empty()。
  /// </summary>
  public partial class EmptyIgnorePreviousState : Empty {
    override public Type Type => Type.OfEmptyIgnorePreviousState;
    public override string ToString() => "<InputState.EmptyIgnoringPreviousState>";
  }

  // MARK: - InputState.Committing

  /// <summary>
  /// .Committing: 該狀態會承載要遞交出去的內容，讓輸入法控制器處理時代為遞交。
  /// </summary>
  public partial class Committing : InputStateProtocol {
    public Type Type => Type.OfCommitting;
    public string TextToCommit { get; private set; } = "";
    public Committing(string textToCommit) { TextToCommit = textToCommit; }
    public override string ToString() => $"<InputState.Committing, TextToCommit:{TextToCommit}>";
  }

  // MARK: - InputState.AssociatedPhrases

  /// <summary>
  /// .AssociatedPhrases: 逐字選字模式內的聯想詞輸入狀態。<br />
  /// 因為逐字選字模式不需要在組字區內存入任何東西，所以該狀態不受 .NotEmpty 的管轄。
  /// </summary>
  public partial class AssociatedPhrases : InputStateProtocol {
    public Type Type => Type.OfAssociatedPhrases;
    public List<(string, string)> Candidates { get; private set; } = new();
    public bool IsTypingVertical { get; private set; } = false;
    public AssociatedPhrases(List<(string, string)> candidates, bool isTypingVertical) {
      Candidates = candidates;
      IsTypingVertical = isTypingVertical;
    }
    public override string ToString() =>
        $"<InputState.AssociatedPhrases, Candidates:{Candidates}, IsTypingVertical:{IsTypingVertical}>";
  }

  // MARK: - InputState.NotEmpty

  /// <summary>
  /// .NotEmpty: 非空狀態，是一種狀態大類、用以派生且代表下述諸狀態。<br />
  /// - .Inputting: 使用者輸入了內容。此時會出現組字區（Compositor）。<br />
  /// - .Marking: 使用者在組字區內標記某段範圍，可以決定是添入新詞、<br />
  ///   還是將這個範圍的詞音組合放入語彙濾除清單。<br />
  /// - .ChoosingCandidate: 叫出選字窗、允許使用者選字。<br />
  /// - .SymbolTable: 波浪鍵符號選單專用的狀態，有自身的特殊處理。<br />
  /// </summary>
  public partial class NotEmpty : InputStateProtocol {
    virtual public Type Type => Type.OfNotEmpty;
    public string ComposingBuffer { get; private set; }
    public int CursorIndex {
      get => CursorIndex;
    private
      set => Math.Max(value, 0);
    }
    public string ComposingBufferConverted() {
      string converted = Tools.KanjiConversionIfRequired(ComposingBuffer);
      if (U8Utils.GetU8Length(converted) != U8Utils.GetU8Length(ComposingBuffer) &&
          converted.Length != ComposingBuffer.Length)
        return ComposingBuffer;
      return converted;
    }
    public NotEmpty(string composingBuffer, int cursorIndex) {
      ComposingBuffer = composingBuffer;
      CursorIndex = cursorIndex;
    }
    public override string ToString() =>
        $"<InputState.NotEmpty, ComposingBuffer:{ComposingBuffer}, CursorIndex:{CursorIndex}>";
  }

  // MARK: - InputState.Inputting

  /// <summary>
  /// .Inputting: 使用者輸入了內容。此時會出現組字區（Compositor）。
  /// </summary>
  public partial class Inputting : NotEmpty {
    override public Type Type => Type.OfInputting;
    public string TextToCommit = "";
    public string Tooltip = "";
    public Inputting(string composingBuffer, int cursorIndex) : base(composingBuffer, cursorIndex) {}
    public override string ToString() =>
        $"<InputState.Inputting, ComposingBuffer:{ComposingBuffer}, CursorIndex:{CursorIndex}, TextToCommit:{TextToCommit}>";
  }

  // MARK: - InputState.Marking

  /// <summary>
  /// .Marking: 使用者在組字區內標記某段範圍，可以決定是添入新詞、
  /// 還是將這個範圍的詞音組合放入語彙濾除清單。
  /// </summary>
  public partial class Marking : NotEmpty {
    // TODO: Too many things lacked in this class.
    override public Type Type => Type.OfMarking;
    public Range allowedMarkRange =
        new(Prefs.MinCandidateLength, Math.Max(Prefs.MaxCandidateLength, Prefs.MinCandidateLength) + 1);
    public int MarkerIndex {
      get => MarkerIndex;
    private
      set => MarkerIndex = Math.Max(value, 0);
    }
    public Range MarkedRange { get; private set; }
    // TODO: Extend Literal Marked Range here.
    public List<string> Readings { get; private set; }
    public string TooltipForInputting = "";
    public string LiteralReadingThread() {
      // TODO: To be implemented.
      return "";
    }

    public Marking(string composingBuffer, int cursorIndex, int markerIndex, List<string> readings)
        : base(composingBuffer, cursorIndex) {
      int begin = Math.Min(cursorIndex, markerIndex);
      int end = Math.Max(cursorIndex, markerIndex);
      MarkedRange = new(begin, end);
      Readings = readings;
      MarkerIndex = markerIndex;
    }
    public override string ToString() =>
        $"<InputState.Marking, ComposingBuffer:{ComposingBuffer}, CursorIndex:{CursorIndex}, MarkedRange:{MarkedRange.ToString()}>";
    public Inputting ConvertedToInputting() {
      Inputting state = new(ComposingBuffer, CursorIndex);
      state.Tooltip = TooltipForInputting;
      return state;
    }
  }

  // MARK: - InputState.ChoosingCandidate

  /// <summary>
  /// .ChoosingCandidate: 叫出選字窗、允許使用者選字。
  /// </summary>
  public partial class ChoosingCandidate : NotEmpty {
    override public Type Type => Type.OfChooseCandidate;
    public List<(string, string)> Candidates { get; private set; }
    public bool IsTypingVertical { get; private set; }
    public ChoosingCandidate(string composingBuffer, int cursorIndex, List<(string, string)> candidates,
                             bool isTypingVertical)
        : base(composingBuffer, cursorIndex) {
      Candidates = candidates;
      IsTypingVertical = isTypingVertical;
    }
    public override string ToString() =>
        $"<InputState.ChoosingCandidate, Candidates:{Candidates}, IsTypingVertical:{IsTypingVertical}, ComposingBuffer:{ComposingBuffer}, CursorIndex:{CursorIndex}>";
  }

  // MARK: - InputState.SymbolTable

  /// <summary>
  /// .SymbolTable: 波浪鍵符號選單專用的狀態，有自身的特殊處理。
  /// </summary>
  public partial class SymbolTable : ChoosingCandidate {
    override public Type Type => Type.OfSymbolTable;
    public SymbolNode Node = new("/");
    static List<(string, string)> GenerateCandidates(SymbolNode node) {
      List<string> arrCandidateResults = new();
      if (node.Children != null) {
        foreach (SymbolNode neta in node.Children!) {
          arrCandidateResults.Add(neta.Title);
        }
      }
      return arrCandidateResults.Select(neta => ("", neta)).ToList();
    }
    public SymbolTable(SymbolNode node, bool isTypingVertical)
        : base("", 0, GenerateCandidates(node), isTypingVertical) {
      Node = node;
    }
    public override string ToString() =>
        $"<InputState.ChoosingCandidate, Candidates:{Candidates}, IsTypingVertical:{IsTypingVertical}>";
  }
}
}