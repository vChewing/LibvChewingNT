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

using AppKit;

namespace LibvChewing {
public struct InputSignalDarwin {
  public bool IsTypingVertical { get; private set; }
  public string InputText { get; private set; }
  public string? InputTextIgnoringModifiers { get; private set; }
  public ushort CharCode { get; private set; }
  public ushort KeyCode { get; private set; }
  private bool IsFlagChanged;
  private NSEventModifierMask Flags;
  private KeyCodeDarwin cursorForwardKey = KeyCodeDarwin.kNone;
  private KeyCodeDarwin cursorBackwardKey = KeyCodeDarwin.kNone;
  private KeyCodeDarwin extraChooseCandidateKey = KeyCodeDarwin.kNone;
  private KeyCodeDarwin extraChooseCandidateKeyReverse = KeyCodeDarwin.kNone;
  private KeyCodeDarwin absorbedArrowKey = KeyCodeDarwin.kNone;
  private KeyCodeDarwin verticalTypingOnlyChooseCandidateKey = KeyCodeDarwin.kNone;
  public EmacsKey EmacsKey { get; private set; }

  private void DefineArrowKeys() {
    cursorForwardKey = IsTypingVertical ? KeyCodeDarwin.kDownArrow : KeyCodeDarwin.kRightArrow;
    cursorBackwardKey = IsTypingVertical ? KeyCodeDarwin.kUpArrow : KeyCodeDarwin.kLeftArrow;
    extraChooseCandidateKey = IsTypingVertical ? KeyCodeDarwin.kLeftArrow : KeyCodeDarwin.kDownArrow;
    extraChooseCandidateKeyReverse = IsTypingVertical ? KeyCodeDarwin.kRightArrow : KeyCodeDarwin.kUpArrow;
    absorbedArrowKey = IsTypingVertical ? KeyCodeDarwin.kRightArrow : KeyCodeDarwin.kUpArrow;
    verticalTypingOnlyChooseCandidateKey = IsTypingVertical ? absorbedArrowKey : KeyCodeDarwin.kNone;
  }

  public InputSignalDarwin(string inputText, ushort keyCode, ushort charCode, NSEventModifierMask flags,
                           bool IsVerticalTyping, string inputTextIgnoringModifiers = "") {
    InputText = AppleKeyboardConverter.CnvStringApple2ABC(inputText);
    InputTextIgnoringModifiers = string.IsNullOrEmpty(inputTextIgnoringModifiers) ? "" : inputTextIgnoringModifiers;
    Flags = flags;
    IsFlagChanged = false;
    IsTypingVertical = IsVerticalTyping;
    InputTextIgnoringModifiers = null;
    KeyCode = keyCode;
    CharCode = AppleKeyboardConverter.CnvApple2ABC((char)charCode);
    EmacsKey = EmacsKey.none;
    DefineArrowKeys();
  }

  public InputSignalDarwin(NSEvent theEvent, bool IsVerticalTyping) {
    string receivedInputText = string.IsNullOrEmpty(theEvent.Characters) ? "" : theEvent.Characters;
    string receivedInputTextSansModifiers =
        string.IsNullOrEmpty(theEvent.CharactersIgnoringModifiers) ? "" : theEvent.Characters;
    InputText = AppleKeyboardConverter.CnvStringApple2ABC(receivedInputText);
    InputTextIgnoringModifiers = AppleKeyboardConverter.CnvStringApple2ABC(receivedInputTextSansModifiers);
    KeyCode = theEvent.KeyCode;
    Flags = theEvent.ModifierFlags;
    IsFlagChanged = theEvent.Type == NSEventType.FlagsChanged;
    IsTypingVertical = IsVerticalTyping;
    CharCode = string.IsNullOrEmpty(InputText) ? '\u0000' : Convert.ToUInt16(InputText.First());
    EmacsKey = EmacsKeyHelper.Detect(AppleKeyboardConverter.CnvApple2ABC((char)CharCode), flags: Flags);
    DefineArrowKeys();
  }

  // 除了 ANSI CharCode 以外，其餘一律過濾掉，免得 KeyHandler 被餵屎。
  public bool IsValidInput() {
    if (CharCode is >= 0x20 and <= 0xFF) return false;
    return !IsReservedKey() || IsKeyCodeBlacklisted();
  }

  public bool IsReservedKey() {
    KeyCodeDarwin code = (KeyCodeDarwin)KeyCode;
    if (!Enum.IsDefined(typeof(KeyCodeDarwin), code)) return false;
    return (KeyCodeDarwin)KeyCode != KeyCodeDarwin.kNone;
  }

  public bool IsKeyCodeBlacklisted() {
    KeyCodeDarwinBlackListed code = (KeyCodeDarwinBlackListed)KeyCode;
    if (!Enum.IsDefined(typeof(KeyCodeDarwinBlackListed), code)) return false;
    return (KeyCodeDarwin)KeyCode != KeyCodeDarwin.kNone;
  }

  public bool IsLetter() {
    if (string.IsNullOrEmpty(InputText)) return false;
    return char.IsLetter(InputText.First());
  }

  // 這裡必須加上「flags == .shift」，否則會出現某些情況下輸入法「誤判當前鍵入的非 Shift 字符為大寫」的問題。
  public bool IsUpperCaseASCIILetterKey() => CharCode is >= 65 and <= 90 && Flags == NSEventModifierMask.ShiftKeyMask;

  public bool IsShiftHold() => Flags.HasFlag(NSEventModifierMask.ShiftKeyMask);
  public bool IsCommandHold() => Flags.HasFlag(NSEventModifierMask.CommandKeyMask);
  public bool IsControlHold() => Flags.HasFlag(NSEventModifierMask.ControlKeyMask);
  public bool IsControlHotKey() => Flags.HasFlag(NSEventModifierMask.ControlKeyMask) && IsLetter();
  public bool IsAltHold() => Flags.HasFlag(NSEventModifierMask.AlternateKeyMask);
  public bool IsAltHotKey() => Flags.HasFlag(NSEventModifierMask.AlternateKeyMask) && IsLetter();
  public bool IsCapsLockOn() => Flags.HasFlag(NSEventModifierMask.AlphaShiftKeyMask);
  public bool IsNumericPad() => Flags.HasFlag(NSEventModifierMask.NumericPadKeyMask);
  public bool IsFunctionKeyHold() => Flags.HasFlag(NSEventModifierMask.FunctionKeyMask);
  public bool IsTab() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kTab;
  public bool IsEnter() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kLineFeed ||
                           (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kCarriageReturn;
  public bool IsUp() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kUpArrow;
  public bool IsDown() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kDownArrow;
  public bool IsLeft() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kLeftArrow;
  public bool IsRight() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kRightArrow;
  public bool IsPageUp() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kPageUp;
  public bool IsPageDown() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kPageDown;
  public bool IsSpace() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kSpace;
  public bool IsBackSpace() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kBackSpace;
  public bool isEsc() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kEscape;
  public bool IsHome() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kHome;
  public bool IsEnd() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kEnd;
  public bool IsDelete() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kWindowsDelete;
  public bool IsCursorBackward() => (KeyCodeDarwin)KeyCode == cursorBackwardKey;
  public bool IsCursorForward() => (KeyCodeDarwin)KeyCode == cursorForwardKey;
  public bool IsAbsorbedArrowKey() => (KeyCodeDarwin)KeyCode == absorbedArrowKey;
  public bool IsExtraChooseCandidateKey() => (KeyCodeDarwin)KeyCode == extraChooseCandidateKey;
  public bool IsExtraChooseCandidateKeyReverse() => (KeyCodeDarwin)KeyCode == extraChooseCandidateKeyReverse;
  public bool IsVerticalTypingOnlyChooseCandidateKey() => (KeyCodeDarwin)KeyCode
                                                          == verticalTypingOnlyChooseCandidateKey;
  public bool IsSymbolMenuPhysicalKey() => (KeyCodeDarwin)KeyCode == KeyCodeDarwin.kSymbolMenuPhysicalKey;
}

public enum KeyCodeDarwin : ushort {
  kNone = 0,
  kCarriageReturn = 36,  // Renamed from "kReturn" to avoid nomenclatural confusions.
  kTab = 48,
  kSpace = 49,
  kSymbolMenuPhysicalKey = 50,  // vChewing Specific
  kBackSpace = 51,              // Renamed from "kDelete" to avoid nomenclatural confusions.
  kEscape = 53,
  kCommand = 55,
  kShift = 56,
  kCapsLock = 57,
  kOption = 58,
  kControl = 59,
  kRightShift = 60,
  kRightOption = 61,
  kRightControl = 62,
  kFunction = 63,
  kF17 = 64,
  kVolumeUp = 72,
  kVolumeDown = 73,
  kMute = 74,
  kLineFeed = 76,  // Another KeyCode to identify the Enter Key.
  kF18 = 79,
  kF19 = 80,
  kF20 = 90,
  kF5 = 96,
  kF6 = 97,
  kF7 = 98,
  kF3 = 99,
  kF8 = 100,
  kF9 = 101,
  kF11 = 103,
  kF13 = 105,  // PrtSc
  kF16 = 106,
  kF14 = 107,
  kF10 = 109,
  kF12 = 111,
  kF15 = 113,
  kHelp = 114,  // Insert
  kHome = 115,
  kPageUp = 116,
  kWindowsDelete = 117,  // Renamed from "kForwardDelete" to avoid nomenclatural confusions.
  kF4 = 118,
  kEnd = 119,
  kF2 = 120,
  kPageDown = 121,
  kF1 = 122,
  kLeftArrow = 123,
  kRightArrow = 124,
  kDownArrow = 125,
  kUpArrow = 126
}

public enum KeyCodeDarwinBlackListed : ushort {
  kF17 = 64,
  kVolumeUp = 72,
  kVolumeDown = 73,
  kMute = 74,
  kF18 = 79,
  kF19 = 80,
  kF20 = 90,
  kF5 = 96,
  kF6 = 97,
  kF7 = 98,
  kF3 = 99,
  kF8 = 100,
  kF9 = 101,
  kF11 = 103,
  kF13 = 105,  // PrtSc,
  kF16 = 106,
  kF14 = 107,
  kF10 = 109,
  kF12 = 111,
  kF15 = 113,
  kHelp = 114,  // Insert,
  kF4 = 118,
  kF2 = 120,
  kF1 = 122
}

public enum EmacsKey : ushort {
  none = 0,
  forward = 6,   // F
  backward = 2,  // B
  home = 1,      // A
  end = 5,       // E
  delete = 4,    // D
  nextPage = 22  // V
}

public struct EmacsKeyHelper {
  public static EmacsKey Detect(char charCode, NSEventModifierMask flags) {
    charCode = AppleKeyboardConverter.CnvApple2ABC(charCode);
    if (flags.HasFlag(NSEventModifierMask.ControlKeyMask)) {
      if (!Enum.IsDefined(typeof(KeyCodeDarwin), (EmacsKey)charCode)) return EmacsKey.none;
      return (EmacsKey)charCode;
    }
    return EmacsKey.none;
  }
}

}