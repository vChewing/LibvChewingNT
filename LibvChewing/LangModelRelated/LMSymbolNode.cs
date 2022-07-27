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

using Microsoft.Extensions.Localization;
using Microsoft.VisualBasic.FileIO;

namespace LibvChewing {
public class SymbolNode {
  public string Title;
  public List<SymbolNode>? Children;
  public SymbolNode? Previous;

  public SymbolNode(string title, List<SymbolNode>? children = null) {
    Title = title;
    Children = children;
  }

  public SymbolNode(string title, string symbols) {
    Title = title;
    Children = new();
    foreach (string neta in U8Utils.GetU8Elements(symbols)) Children.Add(new(neta, children: null));
  }

  static void ParseUserSymbolNodeData() {
    string path = MgrLangModel.UserSymbolNodeDataURL();
    List<SymbolNode> arrChildren = new();
    try {
      using (TextFieldParser parser = new(path)) {
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters("=");
        while (!parser.EndOfData) {
          List<string> fieldSlice = parser.ReadFields()?.ToList() ?? new();
          if (fieldSlice.Count is 0 or > 2) continue;
          if (fieldSlice[0].First() == '#') continue;
          switch (fieldSlice.Count) {
            case 1:
              arrChildren.Add(new(fieldSlice[0], children: null));
              break;
            case 2:
              arrChildren.Add(new(fieldSlice[0], symbols: fieldSlice[1]));
              break;
          }
        }
      }
      Root = arrChildren.Count == 0 ? defaultSymbolRoot : new("/", children: arrChildren);
    } catch (Exception) {
      Root = defaultSymbolRoot;
    }
  }

  // MARK: - Static data.

  private readonly static string catCommonSymbols =
      new LocalizedString("catCommonSymbols", "Common_Symbols").ToString();
  private readonly static string catHoriBrackets =
      new LocalizedString("catHoriBrackets", "Horizontal_Brackets").ToString();
  private readonly static string catVertBrackets =
      new LocalizedString("catVertBrackets", "Vertical_Brackets").ToString();
  private readonly static string catGreekLetters = new LocalizedString("catGreekLetters", "Greek_Letters").ToString();
  private readonly static string catMathSymbols = new LocalizedString("catMathSymbols", "Math_Symbols").ToString();
  private readonly static string catCurrencyUnits =
      new LocalizedString("catCurrencyUnits", "Currency_Units").ToString();
  private readonly static string catSpecialSymbols =
      new LocalizedString("catSpecialSymbols", "Special_Symbols").ToString();
  private readonly static string catUnicodeSymbols =
      new LocalizedString("catUnicodeSymbols", "Unicode_Symbols").ToString();
  private readonly static string catCircledKanjis =
      new LocalizedString("catCircledKanjis", "Circled_Kanjis").ToString();
  private readonly static string catCircledKataKana =
      new LocalizedString("catCircledKataKana", "Circled_Katakana").ToString();
  private readonly static string catBracketKanjis =
      new LocalizedString("catBracketKanjis", "Bracket_Kanjis").ToString();
  private readonly static string catSingleTableLines =
      new LocalizedString("catSingleTableLines", "Single_Table_Lines").ToString();
  private readonly static string catDoubleTableLines =
      new LocalizedString("catDoubleTableLines", "Doube_Table_Lines").ToString();
  private readonly static string catFillingBlocks =
      new LocalizedString("catFillingBlocks", "Filling_Blocks").ToString();
  private readonly static string catLineSegments = new LocalizedString("catLineSegments", "Line_Segments").ToString();

  public static SymbolNode Root { get; private set; } = new("/", children: null);
  private readonly static SymbolNode defaultSymbolRoot = new("/", children: new() {
    new("/"),
    new(catCommonSymbols, symbols: "，、。．？！；：‧‥﹐﹒˙·‘’“”〝〞‵′〃～＄％＠＆＃＊"),
    new(catHoriBrackets, symbols: "（）「」〔〕｛｝〈〉『』《》【】﹙﹚﹝﹞﹛﹜"),
    new(catVertBrackets, symbols: "︵︶﹁﹂︹︺︷︸︿﹀﹃﹄︽︾︻︼"),
    new(catGreekLetters, symbols: "αβγδεζηθικλμνξοπρστυφχψωΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ"),
    new(catMathSymbols, symbols: "＋－×÷＝≠≒∞±√＜＞﹤﹥≦≧∩∪ˇ⊥∠∟⊿㏒㏑∫∮∵∴╳﹢"),
    new(catCurrencyUnits,
        symbols: "$€¥¢£₽₨₩฿₺₮₱₭₴₦৲৳૱௹﷼₹₲₪₡₫៛₵₢₸₤₳₥₠₣₰₧₯₶₷"),
    new(catSpecialSymbols, symbols: "↑↓←→↖↗↙↘↺⇧⇩⇦⇨⇄⇆⇅⇵↻◎○●⊕⊙※△▲☆★◇◆□■▽▼§￥〒￠￡♀♂↯"),
    new(catUnicodeSymbols, symbols: "♨☀☁☂☃♠♥♣♦♩♪♫♬☺☻"),
    new(catCircledKanjis,
        symbols: "㊟㊞㊚㊛㊊㊋㊌㊍㊎㊏㊐㊑㊒㊓㊔㊕㊖㊗︎㊘㊙︎㊜㊝㊠㊡㊢㊣㊤㊥㊦㊧㊨㊩㊪㊫㊬㊭㊮㊯㊰🈚︎🈯︎"),
    new(catCircledKataKana,
        symbols: "㋐㋑㋒㋓㋔㋕㋖㋗㋘㋙㋚㋛㋜㋝㋞㋟㋠㋡㋢㋣㋤㋥㋦㋧㋨㋩㋪㋫㋬㋭㋮㋯㋰㋱㋲㋳㋴㋵㋶㋷㋸㋹㋺㋻㋼㋾"),
    new(catBracketKanjis, symbols: "㈪㈫㈬㈭㈮㈯㈰㈱㈲㈳㈴㈵㈶㈷㈸㈹㈺㈻㈼㈽㈾㈿㉀㉁㉂㉃"),
    new(catSingleTableLines, symbols: "├─┼┴┬┤┌┐╞═╪╡│▕└┘╭╮╰╯"),
    new(catDoubleTableLines, symbols: "╔╦╗╠═╬╣╓╥╖╒╤╕║╚╩╝╟╫╢╙╨╜╞╪╡╘╧╛"),
    new(catFillingBlocks, symbols: "＿ˍ▁▂▃▄▅▆▇█▏▎▍▌▋▊▉◢◣◥◤"),
    new(catLineSegments, symbols: "﹣﹦≡｜∣∥–︱—︳╴¯￣﹉﹊﹍﹎﹋﹌﹏︴∕﹨╱╲／＼"),
  });
}
}