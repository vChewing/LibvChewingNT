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

using System.Runtime.InteropServices;

namespace LibvChewing {
public struct AppleKeyboardConverter {
  public readonly static List<string> ArrDynamicBasicKeyLayout =
      new() { "com.apple.keylayout.ZhuyinBopomofo",
              "com.apple.keylayout.ZhuyinEten",
              "org.atelierInmu.vChewing.keyLayouts.vchewingdachen",
              "org.atelierInmu.vChewing.keyLayouts.vchewingmitac",
              "org.atelierInmu.vChewing.keyLayouts.vchewingibm",
              "org.atelierInmu.vChewing.keyLayouts.vchewingseigyou",
              "org.atelierInmu.vChewing.keyLayouts.vchewingeten",
              "org.unknown.keylayout.vChewingDachen",
              "org.unknown.keylayout.vChewingFakeSeigyou",
              "org.unknown.keylayout.vChewingETen",
              "org.unknown.keylayout.vChewingIBM",
              "org.unknown.keylayout.vChewingMiTAC" };

  public static bool IsDynamicBasicKeyboardLayoutEnabled =>
      RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && ArrDynamicBasicKeyLayout.Contains(Prefs.BasicKeyboardLayout);

  /// <summary>
  /// ?????? Apple ???????????????????????????
  /// </summary>
  /// <param name="charCode">????????? char ushort???</param>
  /// <returns>???????????????????????? char ushort???</returns>
  public static ushort CnvApple2ABC(ushort charCode) {
    // ??????????????????????????????????????????????????????????????????????????????????????????????????????
    if (!IsDynamicBasicKeyboardLayoutEnabled) return charCode;
    // ??????????????? Apple ?????????????????????????????????????????????
    charCode = Prefs.BasicKeyboardLayout switch {
      "com.apple.keylayout.ZhuyinBopomofo" => charCode switch {
        97 => 65,
        98 => 66,
        99 => 67,
        100 => 68,
        101 => 69,
        102 => 70,
        103 => 71,
        104 => 72,
        105 => 73,
        106 => 74,
        107 => 75,
        108 => 76,
        109 => 77,
        110 => 78,
        111 => 79,
        112 => 80,
        113 => 81,
        114 => 82,
        115 => 83,
        116 => 84,
        117 => 85,
        118 => 86,
        119 => 87,
        120 => 88,
        121 => 89,
        122 => 90,
        _ => charCode
      },
      "com.apple.keylayout.ZhuyinEten" => charCode switch {
        65345 => 65,
        65346 => 66,
        65347 => 67,
        65348 => 68,
        65349 => 69,
        65350 => 70,
        65351 => 71,
        65352 => 72,
        65353 => 73,
        65354 => 74,
        65355 => 75,
        65356 => 76,
        65357 => 77,
        65358 => 78,
        65359 => 79,
        65360 => 80,
        65361 => 81,
        65362 => 82,
        65363 => 83,
        65364 => 84,
        65365 => 85,
        65366 => 86,
        65367 => 87,
        65368 => 88,
        65369 => 89,
        65370 => 90,
        _ => charCode
      },
      _ => charCode
    };
    charCode = charCode switch {
      // ?????? SHIFT ??????????????????????????????
      65281 => 33,
      65312 => 64,
      65283 => 35,
      65284 => 36,
      65285 => 37,
      65087 => 94,
      65286 => 38,
      65290 => 42,
      65288 => 40,
      65289 => 41,
      _ => charCode switch {
        // ??????????????????????????????????????????
        12289 => 92,
        12300 => 91,
        12301 => 93,
        12302 => 123,
        12303 => 125,
        65292 => 60,
        12290 => 62,
        _ => charCode switch {
          // ???????????????
          12573 => 44,
          12582 => 45,
          12577 => 46,
          12581 => 47,
          12578 => 48,
          12549 => 49,
          12553 => 50,
          711 => 51,
          715 => 52,
          12563 => 53,
          714 => 54,
          729 => 55,
          12570 => 56,
          12574 => 57,
          12580 => 59,
          12551 => 97,
          12566 => 98,
          12559 => 99,
          12558 => 100,
          12557 => 101,
          12561 => 102,
          12565 => 103,
          12568 => 104,
          12571 => 105,
          12584 => 106,
          12572 => 107,
          12576 => 108,
          12585 => 109,
          12569 => 110,
          12575 => 111,
          12579 => 112,
          12550 => 113,
          12560 => 114,
          12555 => 115,
          12564 => 116,
          12583 => 117,
          12562 => 118,
          12554 => 119,
          12556 => 120,
          12567 => 121,
          12552 => 122,
          _ => charCode
        }
      }
    };
    // ?????? Alt ????????????
    if (charCode == 8212) charCode = 45;
    // Apple ?????????????????????????????????????????????
    if (Prefs.BasicKeyboardLayout == "com.apple.keylayout.ZhuyinEten")
      charCode = charCode switch {
        65343 => 95,
        65306 => 58,
        65311 => 63,
        65291 => 43,
        65372 => 124,
        _ => charCode
      };
    return charCode;
  }

  public static string CnvStringApple2ABC(string strOut) {
    if (!IsDynamicBasicKeyboardLayoutEnabled) return strOut;
    // ??????????????? Apple ?????????????????????????????????????????????
    strOut = Prefs.BasicKeyboardLayout switch {
      "com.apple.keylayout.ZhuyinBopomofo" =>
          strOut switch { "a" => "A", "b" => "B", "c" => "C", "d" => "D", "e" => "E", "f" => "F", "g" => "G",
                          "h" => "H", "i" => "I", "j" => "J", "k" => "K", "l" => "L", "m" => "M", "n" => "N",
                          "o" => "O", "p" => "P", "q" => "Q", "r" => "R", "s" => "S", "t" => "T", "u" => "U",
                          "v" => "V", "w" => "W", "x" => "X", "y" => "Y", "z" => "Z",
                          _ => strOut },
      "com.apple.keylayout.ZhuyinEten" =>
          strOut switch { "???" => "A", "???" => "B", "???" => "C", "???" => "D", "???" => "E", "???" => "F", "???" => "G",
                          "???" => "H", "???" => "I", "???" => "J", "???" => "K", "???" => "L", "???" => "M", "???" => "N",
                          "???" => "O", "???" => "P", "???" => "Q", "???" => "R", "???" => "S", "???" => "T", "???" => "U",
                          "???" => "V", "???" => "W", "???" => "X", "???" => "Y", "???" => "Z",
                          _ => strOut },
      _ => strOut
    };
    strOut = strOut switch {
      // ?????? SHIFT ??????????????????????????????
      "???" => "!", "???" => "@", "???" => "#", "???" => "$", "???" => "%", "???" => "^", "???" => "&", "???" => "*",
      "???" => "(", "???" => ")",
      _ => strOut switch {
        // ??????????????????????????????????????????
        "???" => "\\", "???" => "[", "???" => "]", "???" => "{", "???" => "}", "???" => "<", "???" => ">",
        _ => strOut switch {
          // ???????????????
          "???" => ",", "???" => "-", "???" => ".", "???" => "/", "???" => "0", "???" => "1", "???" => "2", "??" => "3",
          "??" => "4", "???" => "5", "??" => "6", "??" => "7", "???" => "8", "???" => "9", "???" => ";", "???" => "a",
          "???" => "b", "???" => "c", "???" => "d", "???" => "e", "???" => "f", "???" => "g", "???" => "h", "???" => "i",
          "???" => "j", "???" => "k", "???" => "l", "???" => "m", "???" => "n", "???" => "o", "???" => "p", "???" => "q",
          "???" => "r", "???" => "s", "???" => "t", "???" => "u", "???" => "v", "???" => "w", "???" => "x", "???" => "y",
          "???" => "z",
          _ => strOut
        }
      }
    };
    // ?????? Alt ????????????
    if (strOut == "???") strOut = "-";
    // Apple ?????????????????????????????????????????????
    if (Prefs.BasicKeyboardLayout == "com.apple.keylayout.ZhuyinEten") {
      strOut = strOut switch { "???" => "_", "???" => ":", "???" => "?", "???" => "+", "???" => "|",
                               _ => strOut };
    }
    return strOut;
  }
}
}
