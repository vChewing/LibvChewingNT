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
// MARK: - Things related to Language Models.

public partial struct MgrLangModel {
  private static LMInstantiator gLangModelCHS = new();
  private static LMInstantiator gLangModelCHT = new();
  private static LMUserOverride gUserOverrideModelCHS = new();
  private static LMUserOverride gUserOverrideModelCHT = new();

  public static LMInstantiator LmCHS => gLangModelCHS;
  public static LMInstantiator LmCHT => gLangModelCHT;
  public static LMUserOverride UomCHS => gUserOverrideModelCHS;
  public static LMUserOverride UomCHT => gUserOverrideModelCHT;
}

// MARK: - Things related to path management.
public partial struct MgrLangModel {
  private static string InputModeFileNameSuffix() {
    return (int)Prefs.CurrentInputMode switch { (int)InputMode.ImeModeCHT => "-cht",
                                                (int)InputMode.ImeModeCHS => "-chs",
                                                _ => "" };
  }
  private static string Slash() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"\" : @"/";

  /// <summary>
  /// 預設的使用者語彙資料目錄。<br />
  /// Windows 預設目錄：「C:\Users\USERNAME\Library\Application Support\vChewing\」
  /// macOS & Linux 預設目錄：「~/Library/Application Support/vChewing/」
  /// </summary>
  public static string DefaultUserDataPath() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                                Slash() + "Library" + Slash() + "Application Support" + Slash() +
                                                "vChewing" + Slash();
  /// <summary>
  /// 使用者語彙資料目錄。如果尚未設定，則傳回預設值。
  /// </summary>
  public static string UserDataPath {
    get => Uri.IsWellFormedUriString(UserDataPath, UriKind.RelativeOrAbsolute) ? UserDataPath : DefaultUserDataPath();
    set => UserDataPath = Uri.IsWellFormedUriString(value, UriKind.RelativeOrAbsolute) ? value : "";
  }

  public static string UserPhrasesDataURL() => UserDataPath + Slash() + "userdata" + InputModeFileNameSuffix() + ".txt";
  public static string UserSymbolDataURL() => UserDataPath + Slash() + "usersymbolphrases" + InputModeFileNameSuffix() +
                                              ".txt";
  public static string UserAssociatesDataURL() => UserDataPath + Slash() + "associatedPhrases" +
                                                  InputModeFileNameSuffix() + ".txt";
  public static string UserFilteredDataURL() => UserDataPath + Slash() + "exclude-phrases" + InputModeFileNameSuffix() +
                                                ".txt";
  public static string UserReplacementsDataURL() => UserDataPath + Slash() + "phrases-replacement" +
                                                    InputModeFileNameSuffix() + ".txt";
  public static string UserSymbolNodeDataURL() => UserDataPath + Slash() + "symbols" + InputModeFileNameSuffix() +
                                                  ".dat";
  public static string UserOverrideModelDataURL() => UserDataPath + Slash() + "override-model-data" +
                                                     InputModeFileNameSuffix() + ".dat";

  // MARK: LM Handling Functions.

  public static void SaveUserOverrideModelData() {}
}
}