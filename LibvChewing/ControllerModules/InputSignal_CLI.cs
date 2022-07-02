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
public class InputSignalCLI : InputSignalProtocol {
  public bool IsTypingVertical { get; set; }
  public string InputText { get; set; }
  public string InputTextIgnoringModifiers() {
    return Key switch { ConsoleKey.D0 => "0", ConsoleKey.D1 => "1", ConsoleKey.D2 => "2", ConsoleKey.D3 => "3",
                        ConsoleKey.D4 => "4", ConsoleKey.D5 => "5", ConsoleKey.D6 => "6", ConsoleKey.D7 => "7",
                        ConsoleKey.D8 => "8", ConsoleKey.D9 => "9",
                        _ => InputText };
  }
  public ushort CharCode { get; set; }
  public ushort KeyCode { get; set; }
  public ConsoleKey Key { get; }

  public char TheChar { get; }
  public ConsoleModifiers Flags { get; private set; }
  private bool IsFlagChanged() => Flags.HasFlag(ConsoleModifiers.Alt) || Flags.HasFlag(ConsoleModifiers.Shift) ||
                                  Flags.HasFlag(ConsoleModifiers.Control);
  private ConsoleKey cursorForwardKey = ConsoleKey.NoName;
  private ConsoleKey cursorBackwardKey = ConsoleKey.NoName;
  private ConsoleKey extraChooseCandidateKey = ConsoleKey.NoName;
  private ConsoleKey extraChooseCandidateKeyReverse = ConsoleKey.NoName;
  private ConsoleKey absorbedArrowKey = ConsoleKey.NoName;
  private ConsoleKey verticalTypingOnlyChooseCandidateKey = ConsoleKey.NoName;

  private void DefineArrowKeys() {
    cursorForwardKey = IsTypingVertical ? ConsoleKey.DownArrow : ConsoleKey.RightArrow;
    cursorBackwardKey = IsTypingVertical ? ConsoleKey.UpArrow : ConsoleKey.LeftArrow;
    extraChooseCandidateKey = IsTypingVertical ? ConsoleKey.LeftArrow : ConsoleKey.DownArrow;
    extraChooseCandidateKeyReverse = IsTypingVertical ? ConsoleKey.RightArrow : ConsoleKey.UpArrow;
    absorbedArrowKey = IsTypingVertical ? ConsoleKey.RightArrow : ConsoleKey.UpArrow;
    verticalTypingOnlyChooseCandidateKey = IsTypingVertical ? absorbedArrowKey : ConsoleKey.NoName;
  }

  public InputSignalCLI(string inputText, ConsoleKey keyCode, ushort charCode, ConsoleModifiers flags,
                        bool IsVerticalTyping = false) {
    InputText = AppleKeyboardConverter.CnvStringApple2ABC(inputText);
    Flags = flags;
    IsTypingVertical = IsVerticalTyping;
    Key = keyCode;
    CharCode = AppleKeyboardConverter.CnvApple2ABC(charCode);
    TheChar = (char)CharCode;
    DefineArrowKeys();
  }

  public InputSignalCLI(ConsoleKeyInfo theEvent, bool IsVerticalTyping = false) {
    InputText = theEvent.KeyChar.ToString();
    Key = theEvent.Key;
    Flags = theEvent.Modifiers;
    IsTypingVertical = IsVerticalTyping;
    CharCode = theEvent.KeyChar;
    TheChar = theEvent.KeyChar;
    DefineArrowKeys();
  }

  // 除了 ANSI CharCode 以外，其餘一律過濾掉，免得 KeyHandler 被餵屎。
  public bool IsInvalidInput() { return char.IsControl(TheChar); }

  public bool IsReservedKey() {
    ConsoleKey[] reservedKeys = { ConsoleKey.Enter,      ConsoleKey.Tab,       ConsoleKey.Spacebar,
                                  ConsoleKey.Delete,     ConsoleKey.Escape,    ConsoleKey.Enter,
                                  ConsoleKey.Home,       ConsoleKey.PageUp,    ConsoleKey.Backspace,
                                  ConsoleKey.End,        ConsoleKey.PageDown,  ConsoleKey.LeftArrow,
                                  ConsoleKey.RightArrow, ConsoleKey.DownArrow, ConsoleKey.UpArrow };
    return reservedKeys.Contains(Key);
  }

  public bool IsLetter() { return !string.IsNullOrEmpty(InputText) && char.IsLetter(InputText.First()); }

  // 這裡必須加上「flags == .shift」，否則會出現某些情況下輸入法「誤判當前鍵入的非 Shift 字符為大寫」的問題。
  public bool IsUpperCaseASCIILetterKey() => CharCode is >= 65 and <= 90 && Flags == ConsoleModifiers.Shift;

  public bool IsShiftHold() => Flags.HasFlag(ConsoleModifiers.Shift);
  public bool IsCommandHold() => Key is ConsoleKey.LeftWindows or ConsoleKey.RightWindows;
  public bool IsControlHold() => Flags.HasFlag(ConsoleModifiers.Control);
  public bool IsControlHotKey() => Flags.HasFlag(ConsoleModifiers.Control) && IsLetter();
  public bool IsAltHold() => Flags.HasFlag(ConsoleModifiers.Alt);
  public bool IsAltHotKey() => Flags.HasFlag(ConsoleModifiers.Alt) && IsLetter();
  public bool IsCapsLockOn() => char.IsUpper(TheChar);
  public bool IsNumericPad() {
    ConsoleKey[] numPadKeys = { ConsoleKey.NumPad0, ConsoleKey.NumPad1, ConsoleKey.NumPad2,  ConsoleKey.NumPad3,
                                ConsoleKey.NumPad4, ConsoleKey.NumPad5, ConsoleKey.NumPad6,  ConsoleKey.NumPad7,
                                ConsoleKey.NumPad8, ConsoleKey.NumPad9, ConsoleKey.Multiply, ConsoleKey.Subtract,
                                ConsoleKey.Decimal, ConsoleKey.Divide,  ConsoleKey.Add };
    return numPadKeys.Contains(Key);
  }
  public bool IsTab() => Key is ConsoleKey.Tab;
  public bool IsEnter() => Key is ConsoleKey.Enter;
  public bool IsUp() => Key is ConsoleKey.UpArrow;
  public bool IsDown() => Key is ConsoleKey.DownArrow;
  public bool IsLeft() => Key is ConsoleKey.LeftArrow;
  public bool IsRight() => Key is ConsoleKey.RightArrow;
  public bool IsPageUp() => Key is ConsoleKey.PageUp;
  public bool IsPageDown() => Key is ConsoleKey.PageDown;
  public bool IsSpace() => Key is ConsoleKey.Spacebar;
  public bool IsBackSpace() => Key is ConsoleKey.Backspace;
  public bool isEsc() => Key is ConsoleKey.Escape;
  public bool IsHome() => Key is ConsoleKey.Home;
  public bool IsEnd() => Key is ConsoleKey.End;
  public bool IsDelete() => Key is ConsoleKey.Delete;
  public bool IsCursorBackward() => Key == cursorBackwardKey;
  public bool IsCursorForward() => Key == cursorForwardKey;
  public bool IsAbsorbedArrowKey() => Key == absorbedArrowKey;
  public bool IsExtraChooseCandidateKey() => Key == extraChooseCandidateKey;
  public bool IsExtraChooseCandidateKeyReverse() => Key == extraChooseCandidateKeyReverse;
  public bool IsVerticalTypingOnlyChooseCandidateKey() => Key == verticalTypingOnlyChooseCandidateKey;
  public bool IsSymbolMenuPhysicalKey() => TheChar is '`';
}

}