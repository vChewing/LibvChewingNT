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
  public readonly static string ConPragmaHeader = "# đľđžđđźđ°đ đđđ.đđđđđđđđ¸đđđ.đđđđđ đđđ.đđđđđťđđđđđđđđźđđđđđłđđđ.đđđđđđđđđ";
  public static bool ShowDebugOutput = true;

  /// <summary>
  /// ćŞ˘ćĽçľŚĺŽćŞćĄçć¨é ­ćŻĺŚć­Łĺ¸¸ă
  /// </summary>
  /// <param name="path">çľŚĺŽćŞćĄčˇŻĺžă</param>
  /// <returns>çľćć­Łĺ¸¸ĺçşçďźĺśé¤çşĺă</returns>
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
  /// ćŞ˘ćĽćŞćĄćŻĺŚäťĽçŠşčĄçľĺ°žďźĺŚćçźşĺ¤ąĺčŁĺäšă
  /// </summary>
  /// <param name="path"çľŚĺŽćŞćĄčˇŻĺžă</param>
  /// <returns>çľćć­Łĺ¸¸ćäżŽĺžŠé ĺŠĺçşçďźĺśé¤çşĺă</returns>
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
  /// çľąć´çľŚĺŽçćŞćĄçć źĺźă
  /// </summary>
  /// <param name="path">çľŚĺŽćŞćĄčˇŻĺžă</param>
  /// <param name="shouldCheckPragma">ćŻĺŚĺ¨ćŞćĄć¨é ­ĺŽĺĽ˝çĄćçććłä¸çĽéĺ°ć źĺźçć´çă</param>
  /// <returns>čĽć´çé ĺŠćçĄé ć´çďźĺçşçďźĺäšçşĺă</returns>
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
      // çľąć´éŁçşçŠşć źçşä¸ĺ ASCII çŠşć ź
      strProcessed = Regex.Replace(strProcessed, "(Â +|ă+| +|\t+)+", " ");
      // ĺťé¤čĄĺ°žčĄéŚçŠşć ź
      strProcessed = Regex.Replace(strProcessed, "(^ | $)", "");
      // CR & FF to LF, ä¸ĺťé¤éč¤čĄ
      strProcessed = Regex.Replace(strProcessed, "(\f+|\r+|\n+)+", "\n");
      // ĺťé¤čĄéŚĺ°žçŠşć ź
      strProcessed = Regex.Replace(strProcessed, "(\n | \n)", "\n");
      // ĺťé¤ćŞćĄéé ­ççŠşć źďźĺ°žé¨ççŠşć źćč˘Ťĺä¸čĄäżŽć­Łďź
      if (strProcessed.First() == ' ') strProcessed.Remove(0);
      // Step 2: Add Formatted Pragma, the Sorted Header:
      if (!pragmaResult) strProcessed = ConPragmaHeader + "\n" + strProcessed;
      // Step 3: Deduplication.
      // ä¸é˘ĺŠćŹĄ .Reverse() ćŻéŚĺ°žéĄĺďźĺĺžç ´ĺŁćć°ç override čłč¨ă
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
