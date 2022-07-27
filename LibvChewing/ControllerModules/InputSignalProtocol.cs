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

namespace LibvChewing {
public interface InputSignalProtocol {
  public bool IsTypingVertical { get; set; }
  public string InputText { get; set; }
  public string InputTextIgnoringModifiers();
  public ushort CharCode { get; set; }
  public ushort KeyCode { get; set; }
  public bool IsInvalidInput();
  public bool IsReservedKey();
  public bool IsUpperCaseASCIILetterKey();
  public bool IsShiftHold();
  public bool IsCommandHold();
  public bool IsControlHold();
  public bool IsControlHotKey();
  public bool IsAltHold();
  public bool IsAltHotKey();
  public bool IsCapsLockOn();
  public bool IsNumericPadKey();
  public bool IsNonLaptopFunctionKey();
  public bool IsTab();
  public bool IsEnter();
  public bool IsUp();
  public bool IsDown();
  public bool IsLeft();
  public bool IsRight();
  public bool IsPageUp();
  public bool IsPageDown();
  public bool IsSpace();
  public bool IsBackSpace();
  public bool isEsc();
  public bool IsHome();
  public bool IsEnd();
  public bool IsDelete();
  public bool IsCursorBackward();
  public bool IsCursorForward();
  public bool IsCursorClockRight();
  public bool IsCursorClockLeft();
  public bool IsSymbolMenuPhysicalKey();
  public bool IsMainAreaNumKey();
}
}