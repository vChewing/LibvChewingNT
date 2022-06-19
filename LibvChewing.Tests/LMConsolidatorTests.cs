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
public class LMConsolidatorTests {
#region Consts
  private const string path = "./!LMConsolidatorTestFile.txt";
  private static string strRAW =
      @" nian2 黏 -11.336864
nian2 粘	-11.285740
jiang3 槳 -12.492933
gong1si1 公司 -6.299461
   ke1ji4 科技 -6.736613
nian2zhong1 年中 -11.373044
ji4gong1 濟公 -13.336653
jiang3jin1 獎金　-10.344678

nian2zhong1 年終 -11.668947
nian2zhong1 年中 -11.373044
 gao1ke1ji4 高科技 -9.842421
zhe4yang4 這樣 -6.000000 // Non-LibTaBE ";
  private static string strConsolidated =
      @"# 𝙵𝙾𝚁𝙼𝙰𝚃 𝚘𝚛𝚐.𝚊𝚝𝚎𝚕𝚒𝚎𝚛𝙸𝚗𝚖𝚞.𝚟𝚌𝚑𝚎𝚠𝚒𝚗𝚐.𝚞𝚜𝚎𝚛𝙻𝚊𝚗𝚐𝚞𝚊𝚐𝚎𝙼𝚘𝚍𝚎𝚕𝙳𝚊𝚝𝚊.𝚏𝚘𝚛𝚖𝚊𝚝𝚝𝚎𝚍
nian2 黏 -11.336864
nian2 粘 -11.285740
jiang3 槳 -12.492933
gong1si1 公司 -6.299461
ke1ji4 科技 -6.736613
ji4gong1 濟公 -13.336653
jiang3jin1 獎金 -10.344678
nian2zhong1 年終 -11.668947
nian2zhong1 年中 -11.373044
gao1ke1ji4 高科技 -9.842421
zhe4yang4 這樣 -6.000000 // Non-LibTaBE
";
#endregion

  [Test]
  public void TestAll() {
    LMConsolidator.ShowDebugOutput = false;
    Console.WriteLine("// 開始測試文本檔案統整工具。");
    PrepareFileForTests();
    Assert.That(File.ReadAllText(path), Is.EqualTo(strRAW));
    Assert.False(LMConsolidator.CheckPragma(path));
    Assert.True(LMConsolidator.FixEOF(path));
    TrimEOF();
    Assert.True(LMConsolidator.FixEOF(path));
    Assert.That(File.ReadAllText(path), Is.EqualTo(strRAW + "\n"));
    Assert.True(LMConsolidator.Consolidate(path, true));
    Assert.That(File.ReadAllText(path), Is.EqualTo(strConsolidated));
    Assert.True(LMConsolidator.CheckPragma(path));
    // Console.WriteLine(" - 已順利測試一般情況下的整理結果。");
    PrepareFileForTests(withPragma: true);
    Assert.True(LMConsolidator.Consolidate(path, true));
    Assert.That(File.ReadAllText(path), Is.Not.EqualTo(strConsolidated));
    Assert.True(LMConsolidator.FixEOF(path));
    Assert.True(LMConsolidator.Consolidate(path, false));
    Assert.That(File.ReadAllText(path), Is.EqualTo(strConsolidated));
    Assert.True(LMConsolidator.CheckPragma(path));
    File.Delete(path);
    LMConsolidator.ShowDebugOutput = true;
  }

#region  // MARK: - Functions Used in LMConsolidatorTests

  private void PrepareFileForTests(bool withPragma = false) {
    if (!File.Exists(path)) {
      using (StreamWriter sw = File.CreateText(path)) { sw.Write(strRAW); }
      Console.WriteLine(withPragma ? " - 尚未發現之前的測試資料；新的原始測試用資料部署完畢（帶標頭）。"
                                   : " - 尚未發現之前的測試資料；新的原始測試用資料部署完畢。");
    } else {
      FileInfo theFile = new(path);
      using (TextWriter outputStream = new StreamWriter(theFile.Open(FileMode.Truncate))) {
        if (withPragma) outputStream.Write(LMConsolidator.ConPragmaHeader + "\n");
        outputStream.Write(strRAW);
      }
      Console.WriteLine(withPragma ? " - 之前的測試資料已經清除；新的原始測試用資料部署完畢（帶標頭）。"
                                   : " - 之前的測試資料已經清除；新的原始測試用資料部署完畢。");
    }
  }

  private void TrimEOF() {
    using (FileStream fs = new(path, FileMode.Open)) {
      fs.Position = fs.Seek(-1, SeekOrigin.End);
      if (fs.ReadByte() == '\n') fs.SetLength(fs.Length - 1);
    }
    Console.WriteLine(" - 移除 EOF，以便測試 EOF 修復函式。");
  }
#endregion
}
}
