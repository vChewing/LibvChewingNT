
namespace LibvChewing {
public struct CandidateKeyLabel {
  public string Key { get; private set; }
  public string DisplayedText { get; private set; }
  public CandidateKeyLabel(string key, string displayedText) {
    Key = key;
    DisplayedText = displayedText;
  }
}

public interface CtlCandidateDelegate {
  int CandidateCountForController(CtlCandidate controller);
  (string, string) CtlCandidate(CtlCandidate controller, int CandidateAtIndex);
  void CtlCandidateDidSelect(CtlCandidate controller, int didSelectCandidateAtIndex);
}

public abstract class CtlCandidate {
  public enum Layout { Horizontal, Vertical }
  public Layout CurrentLayout = Layout.Horizontal;
  public CtlCandidateDelegate? theDelegate {
    get => theDelegate;
    set {
      theDelegate = value;
      ReloadData();
    }
  }
  public int SelectedCandidatedIndex = int.MaxValue;
  public bool IsVisible = false;  // Need to implement "didSet".

  public List<CandidateKeyLabel> KeyLabels =
      new() { new("1", "1"), new("2", "2"), new("3", "3"), new("4", "4"), new("5", "5"),
              new("6", "6"), new("7", "7"), new("8", "8"), new("9", "9") };
  public string tooltip = "";
  public bool ShowNextPage() => false;
  public bool ShowPreviousPage() => false;
  public bool HighlightNextCandidate() => false;
  public bool HighlightPreviousCandidate() => false;
  public int CandidateIndexAtKeyLabelIndex(int index) => int.MaxValue;
  public void ReloadData() {}
}
}