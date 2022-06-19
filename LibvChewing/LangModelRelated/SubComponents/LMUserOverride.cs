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
  private const double ConDecayThreshold = 1.0 / 1048576.0;
  public LMUserOverride(int capacity = 500, double decayConstant = 5400) {
    _capacity = Math.Max(1, capacity);
    _decayExponent = Math.Log(0.5) / decayConstant;
  }
  public void Observe(List<NodeAnchor> walkedAnchors, int cursorIndex, string candidate, double timestamp) {
    string key = ConvertKeyFrom(walkedAnchors, cursorIndex);
    if (_LRUMap.ContainsKey(key)) {
      _LRUMap[key].Observation.Update(candidate, timestamp);
      _LRUList.AddFirst(_LRUMap[key]);
      if (ShowDebugOutput) Console.WriteLine("UOM: Observation finished with existing observation: {0}", key);
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
      if (ShowDebugOutput) Console.WriteLine("UOM: Observation finished with new observation: {0}", key);
    }
  }

  public string Suggest(List<NodeAnchor> walkedAnchors, int cursorIndex, double timestamp) {
    string key = ConvertKeyFrom(walkedAnchors, cursorIndex);
    if (!_LRUMap.ContainsKey(key)) {
      if (ShowDebugOutput) Console.WriteLine("UOM: mutLRUMap[key] is nil, throwing blank suggestion for key: {0}", key);
      return "";
    }
    Observation observation = _LRUMap[key].Observation;

    string? candidate = null;
    double score = 0.0;
    foreach ((string? suggestedCandidate, Override? theOverride) in observation.Overrides) {
      double overrideScore = GetScore(eventCount: theOverride.Count, totalCount: observation.Count,
                                      eventTimestamp: theOverride.Timestamp, timestamp, lambda: _decayExponent);
      if (overrideScore == 0) {
        continue;
      }
      if (overrideScore <= score) continue;
      candidate = suggestedCandidate;
      score = overrideScore;
    }
    if (!string.IsNullOrEmpty(candidate)) return candidate;
    if (ShowDebugOutput) Console.WriteLine("UOM: No usable suggestions in the result for key: {0}", key);
    return "";
  }

  public static double GetScore(int eventCount, int totalCount, double eventTimestamp, double timestamp,
                                double lambda) {
    double decay = Math.Exp((timestamp - eventTimestamp) * lambda);
    if (decay < ConDecayThreshold) return 0;
    double prob = eventCount / (double)totalCount;
    return prob * decay;
  }

  private static string ConvertKeyFrom(List<NodeAnchor> walkedAnchors, int cursorIndex) {
    string[] arrEndingPunctuation = { "，", "。", "！", "？", "」", "』", "”", "’" };
    List<NodeAnchor> arrNodes = new();
    int intLength = 0;
    foreach (NodeAnchor theAnchor in walkedAnchors) {
      arrNodes.Add(theAnchor);
      intLength += theAnchor.SpanningLength;
      if (intLength >= cursorIndex) break;
    }

    if (arrNodes.Count == 0) return "";

    arrNodes.Reverse();

    Node? nodeCurrent = arrNodes[0].Node;
    if (nodeCurrent == null) return "";
    KeyValuePaired kvCurrent = nodeCurrent.CurrentKeyValue;
    if (arrEndingPunctuation.Contains(kvCurrent.Value)) return "";

    // 前置單元只記錄讀音，在其後的單元則同時記錄讀音與字詞
    string strCurrent = kvCurrent.Key;
    string strPrevious = "()";
    string strAnterior = "()";

    if (arrNodes.Count >= 2) {
      Node? nodePrevious = arrNodes[1].Node;
      if (nodePrevious != null) {
        KeyValuePaired kvPrevious = nodePrevious.CurrentKeyValue;
        if (!arrEndingPunctuation.Contains(kvPrevious.Value)) strPrevious = $"({kvPrevious.Key},{kvPrevious.Value})";
      }
    }

    if (arrNodes.Count >= 3) {
      Node? nodeAnterior = arrNodes[2].Node;
      if (nodeAnterior != null) {
        KeyValuePaired kvAnterior = nodeAnterior.CurrentKeyValue;
        if (!arrEndingPunctuation.Contains(kvAnterior.Value)) strAnterior = $"({kvAnterior.Key},{kvAnterior.Value})";
      }
    }

    return $"{strAnterior},{strPrevious},{strCurrent}";
  }

#region  // MARK: - Structs
  private class Override {
    public int Count { get; set; }
    public double Timestamp { get; set; }
    public Override(int count, double timestamp) {
      Count = count;
      Timestamp = timestamp;
    }
  }

  private class Observation {
    public int Count { get; private set; }
    public Dictionary<string, Override> Overrides = new();
    public Observation(int count = 0, Dictionary<string, Override>? overrides = null) {
      if (overrides != null) Overrides = overrides;
      Count = count;
    }
    public void Update(string candidate, double timestamp) {
      Count++;
      if (!Overrides.ContainsKey(candidate))
        Overrides[candidate] = new(count: 1, timestamp);
      else {
        Overrides[candidate].Timestamp = timestamp;
        Overrides[candidate].Count++;
      }
    }
  }

  private class KeyObservationPair {
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