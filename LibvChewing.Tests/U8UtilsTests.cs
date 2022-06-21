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

namespace LibvChewing.Tests {
public class U8UtilsTests {
  public void testNextNormal_0() {
    string s = "中文";
    Assert.That(U8Utils.GetU16NextPositionFor(s, 0), Is.EqualTo(1));
  }
  [Test]
  public void testNextNormal_1() {
    string s = "中文";
    Assert.That(U8Utils.GetU16NextPositionFor(s, 1), Is.EqualTo(2));
  }
  [Test]
  public void testNextWithTree_0() {
    string s = "🌳🌳";
    Assert.That(U8Utils.GetU16NextPositionFor(s, 0), Is.EqualTo(2));
  }
  [Test]
  public void testNextWithTree_1() {
    string s = "🌳🌳";
    Assert.That(U8Utils.GetU16NextPositionFor(s, 1), Is.EqualTo(2));
  }
  [Test]
  public void testNextWithTree_2() {
    string s = "🌳🌳";
    Assert.That(U8Utils.GetU16NextPositionFor(s, 2), Is.EqualTo(4));
  }
  [Test]
  public void testNextWithTree_3() {
    string s = "🌳🌳";
    Assert.That(U8Utils.GetU16NextPositionFor(s, 3), Is.EqualTo(4));
  }
  [Test]
  public void testNextWithTree_4() {
    string s = "🌳🌳";
    Assert.That(U8Utils.GetU16NextPositionFor(s, 4), Is.EqualTo(4));
  }
  [Test]
  public void testNextWithTree_5() {
    string s = "🌳🌳🌳";
    Assert.That(U8Utils.GetU16NextPositionFor(s, 4), Is.EqualTo(6));
  }
  [Test]
  public void testPrevNormal_0() {
    string s = "中文";
    Assert.That(U8Utils.GetU16PreviousPositionFor(s, 1), Is.EqualTo(0));
  }
  [Test]
  public void testPrevNormal_1() {
    string s = "中文";
    Assert.That(U8Utils.GetU16PreviousPositionFor(s, 2), Is.EqualTo(1));
  }
  [Test]
  public void testPrevWithTree_0() {
    string s = "🌳🌳";
    Assert.That(U8Utils.GetU16PreviousPositionFor(s, 0), Is.EqualTo(0));
  }
  [Test]
  public void testPrevWithTree_1() {
    string s = "🌳🌳";
    Assert.That(U8Utils.GetU16PreviousPositionFor(s, 1), Is.EqualTo(0));
  }
  [Test]
  public void testPrevWithTree_2() {
    string s = "🌳🌳";
    Assert.That(U8Utils.GetU16PreviousPositionFor(s, 2), Is.EqualTo(0));
  }
  [Test]
  public void testPrevWithTree_3() {
    string s = "🌳🌳";
    Assert.That(U8Utils.GetU16PreviousPositionFor(s, 3), Is.EqualTo(0));
  }
  [Test]
  public void testPrevWithTree_4() {
    string s = "🌳🌳";
    Assert.That(U8Utils.GetU16PreviousPositionFor(s, 4), Is.EqualTo(2));
  }
}
}