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
public enum FunctionKey {
  Null,
  Tab,
  Enter,
  Up,
  Down,
  Left,
  Right,
  PageUp,
  PageDown,
  Space,
  BackSpace,
  Esc,
  Home,
  End,
  Delete
}

public struct KeyUtils {
  static FunctionKey ParseKeyCode(ushort keyCode) {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
      return keyCode switch {
        36 => FunctionKey.Enter,
        48 => FunctionKey.Tab,
        49 => FunctionKey.Space,
        51 => FunctionKey.Delete,
        53 => FunctionKey.Esc,
        76 => FunctionKey.Enter,
        115 => FunctionKey.Home,
        116 => FunctionKey.PageUp,
        117 => FunctionKey.BackSpace,
        119 => FunctionKey.End,
        121 => FunctionKey.PageDown,
        123 => FunctionKey.Left,
        124 => FunctionKey.Right,
        125 => FunctionKey.Down,
        126 => FunctionKey.Up,
        _ => FunctionKey.Null
      };
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
      return keyCode switch {
        8 => FunctionKey.BackSpace,
        9 => FunctionKey.Tab,
        13 => FunctionKey.Enter,
        20 => FunctionKey.Space,
        25 => FunctionKey.Left,
        26 => FunctionKey.Up,
        27 => FunctionKey.Esc,
        28 => FunctionKey.Down,
        33 => FunctionKey.PageUp,
        34 => FunctionKey.PageDown,
        35 => FunctionKey.End,
        36 => FunctionKey.Home,
        39 => FunctionKey.Right,
        46 => FunctionKey.Delete,
        _ => FunctionKey.Null
      };
    }
    // The rest parts are for Linux and BSD:
    return keyCode switch {
      1 => FunctionKey.Esc,
      14 => FunctionKey.BackSpace,
      15 => FunctionKey.Tab,
      28 => FunctionKey.Enter,
      57 => FunctionKey.Space,
      102 => FunctionKey.Home,
      103 => FunctionKey.Up,
      104 => FunctionKey.PageUp,
      105 => FunctionKey.Left,
      106 => FunctionKey.Right,
      107 => FunctionKey.End,
      108 => FunctionKey.Down,
      109 => FunctionKey.PageDown,
      111 => FunctionKey.Delete,
      _ => FunctionKey.Null
    };
  }
}

}
