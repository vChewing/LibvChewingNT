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

namespace LibvChewing {
public class LMUserOverride {
  public static bool ShowDebugOutput = true;
  private int _capacity;
  private double _decayExponent;
  private LinkedList<KeyObservationPair> _LRUList = new();
  private Dictionary<string, KeyObservationPair> _LRUMap = new();
  private const double ConDecayThreshold = 1.0 / 1048576.0;       // 衰減二十次之後差不多就失效了。
  private const double ConObservedOverrideHalfLife = 3600.0 * 6;  // 6 小時半衰一次，能持續不到六天的記憶。
  public LMUserOverride(int capacity = 500, double decayConstant = ConObservedOverrideHalfLife) {
    _capacity = Math.Max(1, capacity);
    _decayExponent = Math.Log(0.5) / decayConstant;
  }
  public void Observe(List<NodeAnchor> walkedAnchors, int cursorIndex, string candidate, double timestamp,
                      Action<bool> saveCallback) {
    string key = ConvertKeyFrom(walkedAnchors, cursorIndex);
    if (_LRUMap.ContainsKey(key)) {
      Suggest(walkedAnchors, cursorIndex, timestamp,
              _ => {
                _LRUMap[key].Observation.Update(candidate, timestamp);
                _LRUList.AddFirst(_LRUMap[key]);
                if (ShowDebugOutput) Console.WriteLine($"UOM: Observation finished with existing observation: {key}");
                saveCallback(true);
              });
    } else {
      Observation observation = new();
      observation.Update(candidate, timestamp);
      KeyObservationPair koPair = new(key, observation);
      _LRUMap[key] = koPair;
      _LRUList.AddFirst(koPair);
      if (_LRUList.Count > _capacity) {
        _LRUMap.Remove(_LRUList.Last().Key);
        _LRUList.RemoveLast();
      }
      if (ShowDebugOutput) Console.WriteLine($"UOM: Observation finished with new observation: {key}");
      saveCallback(true);
    }
  }

  public List<Unigram> Suggest(List<NodeAnchor> walkedAnchors, int cursorIndex, double timestamp,
                               Action<bool> decayCallback) {
    string key = ConvertKeyFrom(walkedAnchors, cursorIndex);
    string currentReadingKey = ConvertKeyFrom(walkedAnchors, cursorIndex, readingOnly: true);
    if (!_LRUMap.ContainsKey(key)) {
      if (ShowDebugOutput) Console.WriteLine("UOM: mutLRUMap[key] is nil, throwing blank suggestion for key: {0}", key);
      return new();
    }
    Observation observation = _LRUMap[key].Observation;

    List<Unigram> arrResults = new();
    double currentHighScore = 0.0;
    foreach ((string suggestedCandidate, Override theOverride) in observation.Overrides) {
      double overrideScore = GetScore(eventCount: theOverride.Count, totalCount: observation.Count,
                                      eventTimestamp: theOverride.Timestamp, timestamp, lambda: _decayExponent);
      if (overrideScore <= currentHighScore || string.IsNullOrEmpty(suggestedCandidate)) continue;

      double overrideDetectionScore =
          GetScore(eventCount: theOverride.Count, totalCount: observation.Count, eventTimestamp: theOverride.Timestamp,
                   timestamp, lambda: _decayExponent * 2);
      if (overrideDetectionScore <= currentHighScore) decayCallback(true);

      Unigram suggestedUnigram = new(new(currentReadingKey, suggestedCandidate), overrideScore);
      arrResults.Insert(0, suggestedUnigram);
      currentHighScore = overrideScore;
    }
    if (arrResults.Count > 0) return arrResults;
    if (ShowDebugOutput) Console.WriteLine("UOM: No usable suggestions in the result for key: {0}", key);
    return new();
  }

  private static double GetScore(int eventCount, int totalCount, double eventTimestamp, double timestamp,
                                 double lambda) {
    double decay = Math.Exp((timestamp - eventTimestamp) * lambda);
    if (decay < ConDecayThreshold) return 0;
    double prob = eventCount / (double)totalCount;
    return prob * decay;
  }

  private static string ConvertKeyFrom(List<NodeAnchor> walkedAnchors, int cursorIndex, bool readingOnly = false) {
    string[] whiteList = { "你", "他", "妳", "她", "祢", "衪", "它", "牠", "再", "在" };
    List<NodeAnchor> arrNodes = new();
    int intLength = 0;
    foreach (NodeAnchor theAnchor in walkedAnchors) {
      arrNodes.Add(theAnchor);
      intLength += theAnchor.SpanLength;
      if (intLength >= cursorIndex) break;
    }

    if (arrNodes.Count == 0) return "";

    arrNodes.Reverse();

    Node nodeCurrent = arrNodes[0].Node;
    KeyValuePaired kvCurrent = nodeCurrent.CurrentPair;
    if (kvCurrent.Key.Contains('_')) return "";

    // 字音數與字數不一致的內容會被拋棄。
    if (kvCurrent.Key.Split('-').Length != U8Utils.GetU8Length(kvCurrent.Value)) return "";

    // 前置單元只記錄讀音，在其後的單元則同時記錄讀音與字詞
    string strCurrent = kvCurrent.Key;
    string readingStack = strCurrent;
    KeyValuePaired kvPrevious = new();
    KeyValuePaired kvAnterior = new();
    string trigramKey() => $"{kvAnterior.ToNGramKey()},{kvPrevious.ToNGramKey()},{strCurrent}";

    string result() => readingStack.Contains('_') || !kvPrevious.IsValid() &&
                                                         U8Utils.GetU8Length(kvCurrent.Value) == 1 &&
                                                         !whiteList.Contains(kvCurrent.Value)
                           ? ""
                       : readingOnly ? strCurrent
                                     : trigramKey();

    if (arrNodes.Count >= 2) {
      Node nodePrevious = arrNodes[1].Node;
      kvPrevious = nodePrevious.CurrentPair;
      if (!kvPrevious.Key.Contains('_') && kvPrevious.Key.Split('-').Length == U8Utils.GetU8Length(kvPrevious.Value)) {
        readingStack = kvPrevious.Key + "-" + readingStack;
      }
    }

    if (arrNodes.Count < 3) return result();
    Node nodeAnterior = arrNodes[2].Node;
    kvAnterior = nodeAnterior.CurrentPair;
    if (kvAnterior.Key.Contains('_')) return result();
    if (kvAnterior.Key.Split('-').Length != U8Utils.GetU8Length(kvAnterior.Value)) return result();
    readingStack = kvAnterior.Key + "-" + readingStack;

    return result();
  }

  // TODO: Add JSON serialization read / dump support for this module. -> using System.Text.Json;

#region  // MARK: - Structs
  private struct Override {
    public int Count { get; set; }
    public double Timestamp { get; set; }
    public Override(int count, double timestamp) {
      Count = count;
      Timestamp = timestamp;
    }
  }

  private struct Observation {
    public int Count { get; private set; }
    public Dictionary<string, Override> Overrides = new();
    public Observation() { Count = 0; }
    public void Update(string candidate, double timestamp) {
      Count++;
      if (!Overrides.ContainsKey(candidate))
        Overrides[candidate] = new(count: 1, timestamp);
      else {
        int newCount = Overrides[candidate].Count + 1;
        Overrides[candidate] = new(count: newCount, timestamp);
      }
    }
  }

  private struct KeyObservationPair {
    public string Key { get; }
    public Observation Observation { get; }
    public KeyObservationPair(string key, Observation observation) {
      Key = key;
      Observation = observation;
    }
  }
#endregion
}
}