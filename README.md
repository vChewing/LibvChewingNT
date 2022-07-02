LibvChewing 是計畫寫給 Windows 平台的 DotNet 使用的威注音輸入法通用核心套件。

該專案衍生自基於純 Swift 語言研發的 macOS 版威注音輸入法（[GitHub](https://github.com/vChewing/vChewing-macOS/) | [Gitee](https://gitee.com/vChewing/vChewing-macOS)），目前已經完成下述內容：

- 注拼引擎「鐵恨 Tekkon」：全部移植完成，由單獨的倉庫維護（[GitHub](https://github.com/vChewing/TekkonNT) | [Gitee](https://gitee.com/vChewing/TekkonNT)）。
- 組字引擎「天權星 Megrez」：全部移植完成，由單獨的倉庫維護（[GitHub](https://github.com/vChewing/MegrezNT) | [Gitee](https://gitee.com/vChewing/MegrezNT)）。
- 語言模組：全部移植完成（除 LMUserOverride 缺失存檔讀檔功能）、在本倉庫內維護。完成的條目有：
  - LMCore：讀取純文本格式的一般辭典檔案，可以決定是否在讀取前主動整理格式。多用於使用者辭典檔案的整理。
  - LMCoreNS：讀取 plist 格式的辭典檔案（原廠辭典格式），載入速度快。
  - LMAssociates：聯想詞辭典語言模組。
  - LMReplacements：使用者語彙替換表辭典專屬語言模組。
  - LMUserOverride：使用者半衰記憶模組。與 Swift 版不同的是，該模組目前尚未引入將記憶內容以 JSON 格式存檔讀檔的功能。
  - LMSymbolNode：波浪符號選單模組（尚未製作單元測試）。
  - LMInstantiator：語言模組副本化模組。
  - LMConsolidator：針對純文本辭典檔案專門進行格式整理的模組。
- 控制模組：在本倉庫維護。移植完成的內容如下：
  - KeyHandler：按鍵輸入控制模組（尚未製作有效的單元測試）。
  - KeyCodeTranslator：針對 Windows / Darwin (macOS) / Linux & BSD 平台的 KeyCode 翻譯器，目前處於剛好夠用的概念階段。
  - AppleKeyboardConverter：將 Apple Zhuyin Bopomofo 鍵盤佈局翻譯成能被注拼引擎正確處理的形態。
- 工具：在本倉庫維護。移植完成的內容如下：
  - U8Utils：專門用來處理高萬字（Unicode Surrogate-Pairs），比如某些全字庫冷僻字、以及繪文字等。

未完成的內容：
  - MgrLangModel 辭典檔案存取管理專用模組：只是完成了一個需要被其它已完成或正在研發的模組所需要的抽象內容。
  - Prefs（對應 Swift 版的 MgrPrefs）：只是完成了一個需要被其它已完成或正在研發的模組所需要的抽象內容。
  - InputState_Core：只是完成了跨平台部分的內容，但可以使用 partial 型別來擴充平台限定功能。
  - InputSignal：僅完成了 InputSignalProtocol 以方便 KeyHandler 的研發。InputSignal_CLI 則是為了單元測試而實作的型別，但只能用於 .NET 6 的命令行工具的研發。
  - CtlCandidate：只是完成了一個需要被其它已完成或正在研發的模組所需要的抽象內容。
  - Tools：其它小工具模組，功能急需擴充。
  - macOS 版威注音輸入法所持有的其餘尚未移植的功能。

作者對 Windows TSF 的理解幾乎趨近於零，所以無法獨自完成 Windows 版的威注音輸入法研發。但為了向世間拿出誠意，所以才完成了上述部分。

該倉庫可搭配威注音語料庫生成的辭典檔案來使用（[GitHub](https://github.com/vChewing/libvchewing-data) | [Gitee](https://gitee.com/vChewing/libvchewing-data)）。

這個倉庫就曬出來給有志者們提供一些柴禾。該倉庫的內容以 MIT-NTL 授權協議發行：在 MIT 基礎上新增了對「威注音」的產品名稱的使用限制。該授權協議的中文版本請洽 macOS 版威注音輸入法的倉庫。

Contributing 相關注意須知：請在推 PR 之前使用至少 clang-format v14 來整理您修改過的檔案的內容格式（Google 行文風格：雙空格縮進、且左花括弧不另起新行）。在 C# 常用命名習慣的基礎上，請務必將名稱內的大寫縮略詞全部以大寫縮寫。舉例：「CreateIBMComputer()」不寫成「CreateIbmComputer()」。IntelliJ Rider 的智能 Refactor 雖強大，但就喜歡在命名習慣的事情上靠北。無視就好（除非真的有將英文拼寫敲錯）。

---

澄清：目前 macOS 版威注音輸入法仍舊繼續使用 Swift 來開發維護，因為：

1. Xamarin 目前不支援 InputMethodKit，而作者沒那個錢去買 RemObjects 的 C# Mac 開發工具套裝。
  - 該套裝可以用 InputMethodKit，可以編譯出不需要 Runtime 的 C# Cocoa / Swift .NET 程式）。
2. 目前已經證實 .NET 6 只能用於 macOS 10.15 Catalina 開始的 macOS 系統，而目前已用 C# 寫完的內容大量依賴 C# v10 的特性（C# v10 是 .NET 6 的一部分）。此時將 macOS 版威注音輸入法全面改用 .NET 6 的話，會不得不放棄對 macOS 10.11-10.14 系統的支援。

$ EOF.
