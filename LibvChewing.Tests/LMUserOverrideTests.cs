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
public class LMUserOverrideTests {
  [Test]
  public void TestInput() {
    // 這次不是要測試 Megrez，而是要測試 LMUserOverride，所以先弄個簡化的情形。
    Console.Write("// 開始測試半衰記憶模組，正在準備……");
    SimpleLM lmTestInput = new(Shared.StrSampleData);
    Compositor theCompositor = new(lmTestInput, separator: "-");

    // 模擬輸入法的行為，每次敲字或選字都重新 walk。;
    theCompositor.InsertReading("gao1");
    theCompositor.InsertReading("ke1");
    theCompositor.InsertReading("ji4");
    theCompositor.InsertReading("gong1");
    theCompositor.InsertReading("si1");
    theCompositor.InsertReading("de5");
    theCompositor.InsertReading("nian2");
    theCompositor.InsertReading("zhong1");
    theCompositor.FixNodeWithCandidateLiteral("年終", 7);
    theCompositor.InsertReading("jiang3");
    theCompositor.InsertReading("jin1");
    theCompositor.Cursor = theCompositor.Length - 1;

    List<NodeAnchor> walked = theCompositor.Walk();
    // Assert.That(walked, Is.Not.Empty);

    List<string> composed = (from phrase in walked let node = phrase.Node where node != null select node.CurrentPair.Value).ToList();
    List<string> correctResult = new() { "高科技", "公司", "的", "年終", "獎金" };
    Assert.That(string.Join("_", composed), Is.EqualTo(string.Join("_", correctResult)));
    Console.WriteLine("完畢！");

    // MARK: - 開始測試半衰模組。
    LMUserOverride.ShowDebugOutput = false;
    LMUserOverride uom = new();
    string strResult = " - 拿到不同的建議結果：";

    // 有些詞需要觀測兩遍才會被建議。
    uom.Observe(walked, theCompositor.Cursor, "獎金", DateTime.Now.Ticks);
    uom.Observe(walked, theCompositor.Cursor, "獎金", DateTime.Now.Ticks);
    List<Unigram> arrResult =
        uom.Suggest(walkedAnchors: walked, cursorIndex: theCompositor.Cursor, timestamp: DateTime.Now.Ticks);
    strResult += arrResult.Aggregate("", (current, neta) => current + " " + neta.KeyValue.Value);
    foreach (Unigram unigram in arrResult) Console.WriteLine(unigram);

    theCompositor.Cursor = 2;
    uom.Observe(walked, theCompositor.Cursor, "高科技", DateTime.Now.Ticks);
    arrResult = uom.Suggest(walkedAnchors: walked, cursorIndex: theCompositor.Cursor, timestamp: DateTime.Now.Ticks);
    strResult += arrResult.Aggregate("", (current, neta) => current + " " + neta.KeyValue.Value);
    foreach (Unigram unigram in arrResult) Console.WriteLine(unigram);

    theCompositor.Cursor = 4;
    uom.Observe(walked, theCompositor.Cursor, "公司", DateTime.Now.Ticks);
    arrResult = uom.Suggest(walkedAnchors: walked, cursorIndex: theCompositor.Cursor, timestamp: DateTime.Now.Ticks);
    strResult += arrResult.Aggregate("", (current, neta) => current + " " + neta.KeyValue.Value);
    foreach (Unigram unigram in arrResult) Console.WriteLine(unigram);

    theCompositor.Cursor = 7;
    uom.Observe(walked, theCompositor.Cursor, "年終", DateTime.Now.Ticks);
    arrResult = uom.Suggest(walkedAnchors: walked, cursorIndex: theCompositor.Cursor, timestamp: DateTime.Now.Ticks);
    strResult += arrResult.Aggregate("", (current, neta) => current + " " + neta.KeyValue.Value);
    foreach (Unigram unigram in arrResult) Console.WriteLine(unigram);

    Console.WriteLine(strResult);
    Assert.That(strResult, Is.EqualTo(" - 拿到不同的建議結果： 獎金 高科技 公司 年終"));

    LMUserOverride.ShowDebugOutput = true;
  }
}
}