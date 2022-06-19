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

namespace LibvChewing.Tests {
public class LangModelTests {
  [Test]
  public void SubComponentTests() {
    LMConsolidator.ShowDebugOutput = false;
    Console.WriteLine("// 開始測試各項子語言模組。");
    LMCoreNSTest();
    LMCoreTestSansReverse();
    LMCoreTestWithReversedParameters();
    LMReplacementsTest();
    LMAssociatesTest();
    LMConsolidator.ShowDebugOutput = true;
  }

#region  // MARK: - Functions Used in SubComponentTests

  private void LMCoreNSTest() {
    LMCoreNS lmTest = new(defaultScore: -9.5);
    // Console.Write("// 測試 LMCoreNS");
    lmTest.Open("../../../files4test/data-test.plist");
    // Console.WriteLine("（在庫詞條數：{0}）", lmTest.Count);
    Assert.NotZero(lmTest.Count);
    const string keyA = "ㄅㄚ-ㄩㄝˋ-ㄓㄨㄥ-ㄑㄧㄡ-ㄕㄢ-ㄌㄧㄣˊ-ㄌㄧㄤˊ";
    const string keyB = "ㄈㄥ-ㄔㄨㄟ-ㄉㄚˋ-ㄉㄧˋ-ㄘㄠˇ-ㄓ-ㄅㄞˇ";
    Assert.True(lmTest.HasUnigramsFor(keyA));
    // Console.WriteLine(" - LMCoreNS: 在庫確認：{0}", keyA);
    Assert.True(lmTest.HasUnigramsFor(keyB));
    // Console.WriteLine(" - LMCoreNS: 在庫確認：{0}", keyB);
    List<Unigram> arrResult = lmTest.UnigramsFor(keyA);
    arrResult.AddRange(lmTest.UnigramsFor(keyB));  // AddRange 相當於將參數內的陣列的內容 Append 到自己身上。
    string strResult = arrResult.Aggregate("", (current, neta) => current + neta.KeyValue.Value + " ");
    string scoreResult = arrResult.Aggregate("", (current, neta) => current + neta.Score + " ");
    strResult = strResult.Remove(strResult.Length - 1);
    scoreResult = scoreResult.Remove(scoreResult.Length - 1);
    Assert.That(strResult, Is.EqualTo("八月中秋山林涼 風吹大地草枝擺"));
    Assert.That(scoreResult, Is.EqualTo("-8.085 -8.085"));
    Console.WriteLine(" - LMCoreNS 成功拼出詩句：{0} ({1})", strResult, scoreResult);
  }

  private void LMCoreTestSansReverse() {
    LMCore lmTest = new(defaultScore: -9.5);
    // Console.Write("// 測試 LMCore：不整理格式，不翻轉前兩欄，不統一權重");
    lmTest.Open("../../../files4test/data-test-plaintext.txt");
    // Console.WriteLine("（在庫詞條數：{0}）", lmTest.Count);
    Assert.NotZero(lmTest.Count);
    const string keyA = "gao1ke1ji4";
    const string keyB = "gong1si1";
    Assert.True(lmTest.HasUnigramsFor(keyA));
    // Console.WriteLine(" - LMCore-A: 在庫確認：{0}", keyA);
    Assert.True(lmTest.HasUnigramsFor(keyB));
    // Console.WriteLine(" - LMCore-A: 在庫確認：{0}", keyB);
    List<Unigram> arrResult = lmTest.UnigramsFor(keyA);
    arrResult.AddRange(lmTest.UnigramsFor(keyB));
    string strResult = arrResult.Aggregate("", (current, neta) => current + neta.KeyValue.Value + " ");
    string scoreResult = arrResult.Aggregate("", (current, neta) => current + neta.Score + " ");
    strResult = strResult.Remove(strResult.Length - 1);
    scoreResult = scoreResult.Remove(scoreResult.Length - 1);
    Assert.That(strResult, Is.EqualTo("高科技 公司"));
    Assert.That(scoreResult, Is.EqualTo("-9.842421 -6.299461"));
    Console.WriteLine(" - LMCore-A 成功拼出詞語：{0} ({1})", strResult, scoreResult);
  }

  private void LMCoreTestWithReversedParameters() {
    LMCore lmTest = new(defaultScore: 0, shouldForceDefaultScore: true, shouldReverse: true, shouldConsolidate: true);
    // Console.Write("// 測試 LMCore：整理格式，翻轉前兩欄，統一權重");
    lmTest.Open("../../../files4test/userdata-test.txt");
    // Console.WriteLine("（在庫詞條數：{0}）", lmTest.Count);
    Assert.NotZero(lmTest.Count);
    const string keyA = "ㄕㄣ-ㄆㄧ-ㄇㄚˊ-ㄉㄞˋ";
    const string keyB = "ㄕㄡˇ-ㄋㄚˊ-ㄒㄧㄢˊ-ㄘㄞˋ";
    Assert.True(lmTest.HasUnigramsFor(keyA));
    // Console.WriteLine(" - LMCore-B: 在庫確認：{0}", keyA);
    Assert.True(lmTest.HasUnigramsFor(keyB));
    // Console.WriteLine(" - LMCore-B: 在庫確認：{0}", keyB);
    List<Unigram> arrResult = lmTest.UnigramsFor(keyA);
    arrResult.AddRange(lmTest.UnigramsFor(keyB));
    string strResult = arrResult.Aggregate("", (current, neta) => current + neta.KeyValue.Value + " ");
    string scoreResult = arrResult.Aggregate("", (current, neta) => current + neta.Score + " ");
    strResult = strResult.Remove(strResult.Length - 1);
    scoreResult = scoreResult.Remove(scoreResult.Length - 1);
    Assert.That(strResult, Is.EqualTo("身披麻袋 手拿鹹菜"));
    Assert.That(scoreResult, Is.EqualTo("0 0"));
    Console.WriteLine(" - LMCore-B 成功拼出幹話：{0} ({1})", strResult, scoreResult);
  }

  private void LMReplacementsTest() {
    LMReplacements lmTest = new();
    // Console.Write("// 測試 LMReplacements");
    lmTest.Open("../../../files4test/phrases-replacement-test.txt");
    // Console.WriteLine("（在庫詞條數：{0}）", lmTest.Count);
    Assert.NotZero(lmTest.Count);
    const string key = "樹新風";
    Assert.True(lmTest.HasEntryFor(key));
    // Console.WriteLine(" - LMReplacements: 在庫確認：{0}", key);
    string strResult = lmTest.EntryFor(key);
    Assert.That(strResult, Is.EqualTo("🌳🆕🐝"));
    Console.WriteLine(" - LMReplacements 成功發現置換：{0} => {1}", key, strResult);
  }

  private void LMAssociatesTest() {
    LMAssociates lmTest = new();
    // Console.Write("// 測試 LMAssociates");
    lmTest.Open("../../../files4test/associatedPhrases-test.txt");
    // Console.WriteLine("（在庫詞條數：{0}）", lmTest.Count);
    Assert.NotZero(lmTest.Count);
    const string key = "天";
    Assert.True(lmTest.HasEntriesFor(key));
    // Console.WriteLine(" - LMAssociates: 在庫確認：{0}", key);
    IEnumerable<string> arrResult = lmTest.EntriesFor(key);
    string strResult = arrResult.Aggregate("", (current, neta) => current + key + neta + " ");
    strResult = strResult.Remove(strResult.Length - 1);
    Assert.That(strResult, Is.EqualTo("天涼好個秋 天天向上"));
    Console.WriteLine(" - LMAssociates 成功找出關聯：「{0}」開頭的詞 => {1}", key, strResult);
  }
#endregion
}
}
