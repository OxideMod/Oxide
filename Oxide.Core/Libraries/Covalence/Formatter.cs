﻿// A custom markup language for Oxide
// Spec:
// Text 		::= {Element}
// Element 		::= String | Bold | Italic | Color | Size
// String		::= {? any character ?}
// Bold			::= "[b]" Text "[/b]"
// Italic		::= "[i]" Text "[/i]"
// Color		::= "[#" ColorValue "]" Text "[/#]"
// ColorValue	::=	RGB | RGBA | Name
// RGB			::= 6 * HexDigit
// RGBA			::= 8 * HexDigit
// HexDigit		::= Digit | "a" | "A" | "b" | "B" | "c" | "C" | "d" | "D" | "e" | "E" | "f" | "F"
// Name			::= "aqua" | "black" | "blue" | "brown" | "cyan" | "darkblue"
//					| "fuchsia" | "green" | "grey" | "lightblue" | "lime"
//					| "magenta" | "maroon" | "navy" | "olive" | "orange"
//					| "purple" | "red" | "silver" | "teal" | "white" | "yellow"
//					? any casing allowed ?
// Size			::= "[+" Integer "]" Text "[/#]"
// Integer		::= Digit {Digit}
// Digit 		::= "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9"

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Oxide.Core.Libraries.Covalence
{
    public class Element
    {
        public ElementType Type;
        public object Val;
        public List<Element> Body = new List<Element>();

        Element(ElementType type, object val)
        {
            Type = type;
            Val = val;
        }

        public static Element String(object s) => new Element(ElementType.String, s);

        public static Element Tag(ElementType type) => new Element(type, null);

        public static Element ParamTag(ElementType type, object val) => new Element(type, val);
    }

    public enum ElementType { String, Bold, Italic, Color, Size }

    class Formatter
    {
        static readonly Dictionary<string, string> colorNames = new Dictionary<string, string>
        {
            ["aqua"] = "00ffff",
            ["black"] = "000000",
            ["blue"] = "0000ff",
            ["brown"] = "a52a2a",
            ["cyan"] = "00ffff",
            ["darkblue"] = "0000a0",
            ["fuchsia"] = "ff00ff",
            ["green"] = "008000",
            ["grey"] = "808080",
            ["lightblue"] = "add8e6",
            ["lime"] = "00ff00",
            ["magenta"] = "ff00ff",
            ["maroon"] = "800000",
            ["navy"] = "000080",
            ["olive"] = "808000",
            ["orange"] = "ffa500",
            ["purple"] = "800080",
            ["red"] = "ff0000",
            ["silver"] = "c0c0c0",
            ["teal"] = "008080",
            ["white"] = "ffffff",
            ["yellow"] = "ffff00"
        };

        class Token { public TokenType Type; public object Val; public string Pattern; }

        enum TokenType { String, Bold, Italic, Color, Size, CloseBold, CloseItalic, CloseColor, CloseSize }

        static readonly Dictionary<ElementType, TokenType?> closeTags = new Dictionary<ElementType, TokenType?>
        {
            [ElementType.String] = null,
            [ElementType.Bold] = TokenType.CloseBold,
            [ElementType.Italic] = TokenType.CloseItalic,
            [ElementType.Color] = TokenType.CloseColor,
            [ElementType.Size] = TokenType.CloseSize
        };

        class Lexer
        {
            delegate State State();

            string text;
            int patternStart = 0;
            int tokenStart = 0;
            int position = 0;
            List<Token> tokens = new List<Token>();

            char Current() => text[position];

            void Next() => position++;

            void StartNewToken() => tokenStart = position;

            void StartNewPattern()
            {
                patternStart = position;
                StartNewToken();
            }

            void Reset() => tokenStart = patternStart;

            string Token() => text.Substring(tokenStart, position - tokenStart);

            void Add(TokenType type, object val = null)
            {
                var t = new Token();
                t.Type = type;
                t.Val = val;
                t.Pattern = text.Substring(patternStart, position - patternStart);
                tokens.Add(t);
            }

            void WritePatternString()
            {
                if (patternStart >= position)
                {
                    return;
                }
                int ts = tokenStart;
                tokenStart = patternStart;
                Add(TokenType.String, Token());
                tokenStart = ts;
            }

            static bool IsValidColorCode(string val)
                => (val.Length == 6 || val.Length == 8)
                    && val.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F');

            static object ParseColor(string val)
            {
                string color;
                if (!colorNames.TryGetValue(val.ToLower(), out color) && !IsValidColorCode(val))
                {
                    return null;
                }
                color = color ?? val;
                if (color.Length == 6)
                {
                    color += "ff";
                }
                return color;
            }

            static object ParseSize(string val)
            {
                int size;
                if (Int32.TryParse(val, out size)) { return size; }
                return null;
            }

            // end of tag (]), transition back to Str
            State EndTag(TokenType t)
            {
                Next();
                return () => {
                    var ch = Current();
                    if (ch == ']')
                    {
                        Next();
                        Add(t);
                        StartNewPattern();
                        return Str;
                    }
                    Reset();
                    return Str;
                };
            }

            // start of param tag ([# or [+), read and parse param.
            State ParamTag(TokenType t, Func<string, object> parse)
            {
                Next();
                StartNewToken();
                State s = null;
                s = () =>
                {
                    if (Current() != ']')
                    {
                        Next();
                        return s;
                    }
                    var parsed = parse(Token());
                    if (parsed == null)
                    {
                        Reset();
                        return Str;
                    }
                    Next();
                    Add(t, parsed);
                    StartNewPattern();
                    return Str;
                };
                return s;
            }

            // start of close tag ([/), trying to identify close tag.
            State CloseTag()
            {
                switch (Current())
                {
                    case 'b':
                        return EndTag(TokenType.CloseBold);
                    case 'i':
                        return EndTag(TokenType.CloseItalic);
                    case '#':
                        return EndTag(TokenType.CloseColor);
                    case '+':
                        return EndTag(TokenType.CloseSize);
                    default:
                        Reset();
                        return Str;
                }
            }

            // start of tag ([), trying to identify tag.
            State Tag()
            {
                switch (Current())
                {
                    case 'b':
                        return EndTag(TokenType.Bold);
                    case 'i':
                        return EndTag(TokenType.Italic);
                    case '#':
                        return ParamTag(TokenType.Color, ParseColor);
                    case '+':
                        return ParamTag(TokenType.Size, ParseSize);
                    case '/':
                        Next();
                        return CloseTag;
                    default:
                        Reset();
                        return Str;
                }
            }

            // any string, trying to find a tag with ([).
            State Str()
            {
                if (Current() == '[')
                {
                    WritePatternString();
                    StartNewPattern();
                    Next();
                    return Tag;
                }
                Next();
                return Str;
            }

            public static List<Token> Lex(string text)
            {
                // a pattern is the full pattern of a token, 
                // e.g. [#foo] instead of just foo as token.
                // when we reach eof or an error we want to
                // default to using a string as token.
                // to accomplish this, we need the full pattern
                // instead of just the token.
                var l = new Lexer();
                l.text = text;
                // run the state machine until eof
                State state = l.Str;
                while (l.position < l.text.Length)
                {
                    // each function represents a state.
                    // each state returns a new state.
                    state = state();
                }
                // flush leftover pattern
                l.WritePatternString();
                return l.tokens;
            }
        }

        class Entry
        {
            public string Pattern;
            public Element Element;

            public Entry(string pattern, Element e)
            {
                Pattern = pattern;
                Element = e;
            }
        }

        static List<Element> Parse(List<Token> tokens)
        {
            int i = 0;
            var s = new Stack<Entry>();
            s.Push(new Entry(null, Element.Tag(ElementType.String)));
            while (i < tokens.Count)
            {
                var t = tokens[i++];
                Action<Element> push = el => s.Push(new Entry(t.Pattern, el));
                var e = s.Peek().Element;
                if (t.Type == closeTags[e.Type])
                {
                    // last tag was closed, pop tag and add to parent
                    s.Pop();
                    s.Peek().Element.Body.Add(e);
                    continue;
                }
                // open new tags on bold, italic, color & size.
                // add strings and invalid tags as strings to body of current tag.
                switch (t.Type)
                {
                    case TokenType.String:
                        e.Body.Add(Element.String(t.Val));
                        break;
                    case TokenType.Bold:
                        push(Element.Tag(ElementType.Bold));
                        break;
                    case TokenType.Italic:
                        push(Element.Tag(ElementType.Italic));
                        break;
                    case TokenType.Color:
                        push(Element.ParamTag(ElementType.Color, t.Val));
                        break;
                    case TokenType.Size:
                        push(Element.ParamTag(ElementType.Size, t.Val));
                        break;
                    default:
                        e.Body.Add(Element.String(t.Pattern));
                        break;
                }
            }
            // stringify all tags that weren't closed at eof
            while (s.Count > 1)
            {
                var e = s.Pop();
                var body = s.Peek().Element.Body;
                body.Add(Element.String(e.Pattern));
                body.AddRange(e.Element.Body);
            }
            return s.Pop().Element.Body;
        }

        public static List<Element> Parse(string text) => Parse(Lexer.Lex(text));

        class Tag
        {
            public string Open;
            public string Close;

            public Tag(string open, string close)
            {
                Open = open;
                Close = close;
            }
        }

        static Tag Translation(Element e, Dictionary<ElementType, Func<object, Tag>> translations)
        {
            Func<object, Tag> parse;
            if (translations.TryGetValue(e.Type, out parse))
            {
                return parse(e.Val);
            }
            return new Tag("", "");
        }

        static string ToTreeFormat(List<Element> tree, Dictionary<ElementType, Func<object, Tag>> translations)
        {
            // translation(string) 	= string_value
            // translation(tree) 	= open_tag_translation 
            //                      + translation(child_1) + translation(child_2) + ... + translation(child_n) 
            //                      + close_tag_translation
            var sb = new StringBuilder();
            foreach (var e in tree)
            {
                if (e.Type == ElementType.String)
                {
                    sb.Append(e.Val);
                    continue;
                }
                var tag = Translation(e, translations);
                sb.Append(tag.Open);
                sb.Append(ToTreeFormat(e.Body, translations));
                sb.Append(tag.Close);
            }
            return sb.ToString();
        }

        static string ToTreeFormat(string text, Dictionary<ElementType, Func<object, Tag>> translations)
            => ToTreeFormat(Parse(text), translations);

        static string RGBAtoRGB(object rgba) => rgba.ToString().Substring(0, 6);

        public static string ToPlaintext(string text) => ToTreeFormat(text, new Dictionary<ElementType, Func<object, Tag>>());

        public static string ToUnity(string text) => ToTreeFormat(text, new Dictionary<ElementType, Func<object, Tag>>
        {
            [ElementType.Bold] = _ => new Tag("<b>", "</b>"),
            [ElementType.Italic] = _ => new Tag("<i>", "</i>"),
            [ElementType.Color] = c => new Tag($"<color={c}>", "</color>"),
            [ElementType.Size] = s => new Tag($"<size={s}>", "</size>")
        });

        public static string ToRustLegacy(string text) => ToTreeFormat(text, new Dictionary<ElementType, Func<object, Tag>>
        {
            [ElementType.Color] = c => new Tag($"[color #{RGBAtoRGB(c)}]", "")
        });

        public static string ToRoKAnd7DTD(string text) => ToTreeFormat(text, new Dictionary<ElementType, Func<object, Tag>>
        {
            [ElementType.Color] = c => new Tag($"[{RGBAtoRGB(c)}]", "")
        });

        public static string ToTerraria(string text) => ToTreeFormat(text, new Dictionary<ElementType, Func<object, Tag>>
        {
            [ElementType.Color] = c => new Tag($"[c/{RGBAtoRGB(c)}:", "]")
        });
    }
}
