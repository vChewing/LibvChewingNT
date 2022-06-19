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

using System.Text.RegularExpressions;

namespace LibvChewing {
public struct LMConsolidator {
  public readonly static string ConPragmaHeader = "# 𝙵𝙾𝚁𝙼𝙰𝚃 𝚘𝚛𝚐.𝚊𝚝𝚎𝚕𝚒𝚎𝚛𝙸𝚗𝚖𝚞.𝚟𝚌𝚑𝚎𝚠𝚒𝚗𝚐.𝚞𝚜𝚎𝚛𝙻𝚊𝚗𝚐𝚞𝚊𝚐𝚎𝙼𝚘𝚍𝚎𝚕𝙳𝚊𝚝𝚊.𝚏𝚘𝚛𝚖𝚊𝚝𝚝𝚎𝚍";
  public static bool ShowDebugOutput = true;

  /// <summary>
  /// 檢查給定檔案的標頭是否正常。
  /// </summary>
  /// <param name="path">給定檔案路徑。</param>
  /// <returns>結果正常則為真，其餘為假。</returns>
  public static bool CheckPragma(string path) {
    try {
      string headline = File.ReadLines(path).First();
      if (headline != ConPragmaHeader) {
        if (ShowDebugOutput) Console.WriteLine("Header Mismatch, Starting In-Place Consolidation.");
        return false;
      }
      if (ShowDebugOutput) Console.WriteLine("Header Verification Succeeded: {0}.", headline);
      return true;
    } catch (Exception e) {
      if (ShowDebugOutput) Console.WriteLine("Header Verification Failed: {0}.", e);
      return false;
    }
  }

  /// <summary>
  /// 檢查檔案是否以空行結尾，如果缺失則補充之。
  /// </summary>
  /// <param name="path"給定檔案路徑。</param>
  /// <returns>結果正常或修復順利則為真，其餘為假。</returns>
  public static bool FixEOF(string path) {
    try {
      using (FileStream stream = new(path, FileMode.Open, FileAccess.Read)) {
        stream.Seek(-2, SeekOrigin.End);
        if ((char)(byte)stream.ReadByte() == '\n') {
          stream.Seek(-1, SeekOrigin.End);
          if ((char)(byte)stream.ReadByte() == '\n') {
            using (FileStream fs = new(path, FileMode.Open)) {
              fs.Position = fs.Seek(-1, SeekOrigin.End);
              if (fs.ReadByte() == '\n') fs.SetLength(fs.Length - 1);
            }
            if (ShowDebugOutput) Console.WriteLine("EOF Successfully Ensured (removed duplicated EOFs).");
            return true;
          }
        }
        stream.Seek(-1, SeekOrigin.End);
        if ((char)(byte)stream.ReadByte() == '\n') {
          if (ShowDebugOutput) Console.WriteLine("EOF Successfully Ensured (no fix necessary).");
          return true;
        }
      }
      if (ShowDebugOutput) Console.WriteLine("EOF Fix Necessity Confirmed, Start Fixing.");
    } catch (Exception e) {
      if (ShowDebugOutput) Console.WriteLine("EOF Verification Failed: {0}.", e);
      return false;
    }
    try {
      using (StreamWriter streamForWriting = File.AppendText(path)) { streamForWriting.Write("\n"); }
      if (ShowDebugOutput) Console.WriteLine("EOF Successfully Ensured (a fix has been performed).");
      return true;
    } catch (Exception e) {
      if (ShowDebugOutput) Console.WriteLine("EOF Fix Failed: {0}.", e);
      return false;
    }
  }

  /// <summary>
  /// 統整給定的檔案的格式。
  /// </summary>
  /// <param name="path">給定檔案路徑。</param>
  /// <param name="shouldCheckPragma">是否在檔案標頭完好無損的情況下略過對格式的整理。</param>
  /// <returns>若整理順利或無須整理，則為真；反之為假。</returns>
  public static bool Consolidate(string path, bool shouldCheckPragma) {
    bool pragmaResult = CheckPragma(path);
    if (shouldCheckPragma) {
      if (pragmaResult) {
        if (ShowDebugOutput) Console.WriteLine("Pragma Intact, No Need to Consolidate.");
        return true;
      }
    };

    try {
      string strProcessed = File.ReadAllText(path);
      // Step 1: Consolidating formats per line.
      // -------
      // CJKWhiteSpace (\x{3000}) to ASCII Space
      // NonBreakWhiteSpace (\x{A0}) to ASCII Space
      // Tab to ASCII Space
      // 統整連續空格為一個 ASCII 空格
      strProcessed = Regex.Replace(strProcessed, "( +|　+| +|\t+)+", " ");
      // 去除行尾行首空格
      strProcessed = Regex.Replace(strProcessed, "(^ | $)", "");
      // CR & FF to LF, 且去除重複行
      strProcessed = Regex.Replace(strProcessed, "(\f+|\r+|\n+)+", "\n");
      // 去除行首尾空格
      strProcessed = Regex.Replace(strProcessed, "(\n | \n)", "\n");
      // 去除檔案開頭的空格（尾部的空格會被前一行修正）
      if (strProcessed.First() == ' ') strProcessed.Remove(0);
      // Step 2: Add Formatted Pragma, the Sorted Header:
      if (!pragmaResult) strProcessed = ConPragmaHeader + "\n" + strProcessed;
      // Step 3: Deduplication.
      // 下面兩次 .Reverse() 是首尾顛倒，免得破壞最新的 override 資訊。
      List<string> arrData = strProcessed.Split('\n').Reverse().Distinct().Reverse().ToList();
      strProcessed = string.Join("\n", arrData);
      // Step 4: Remove duplicated newlines at the end of the file.
      strProcessed = Regex.Replace(strProcessed, "\n+", "\n");
      // Step 5: Write consolidated file contents.
      FileInfo theFile = new(path);
      using (TextWriter outputStream = new StreamWriter(theFile.Open(FileMode.Truncate))) {
        outputStream.Write(strProcessed);
      }
      if (ShowDebugOutput) Console.WriteLine("Consolidation Successful.");
      return true;
    } catch (Exception e) {
      if (ShowDebugOutput) Console.WriteLine("Consolidation Failed: {0}.", e);
      return false;
    }
  }
}
}
