// a custom markup language for oxide.
// spec:
// Text 		::= {Element}
// Element 		::= Italic | Bold | Color | Size | String
// Italic		::= "[b]" Text "[/b]"
// Bold			::= "[i]" Text "[/i]"
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
// String		::= {? any character ?}

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
        public List<Element> Body;

        Element(ElementType type, object val, List<Element> body)
        {
            Type = type;
            Val = val;
            Body = body;
        }

        public static Element String(object s) => new Element(ElementType.String, s, new List<Element>());

        public static Element Tag(ElementType type, List<Element> body) => new Element(type, null, body);

        public static Element ParamTag(ElementType type, object val, List<Element> body) => new Element(type, val, body);
    }

    public enum ElementType { String, Bold, Italic, Color, Size }

    public class Formatter
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

        static readonly Dictionary<TokenType, TokenType> closeTags = new Dictionary<TokenType, TokenType>
        {
            [TokenType.Bold] = TokenType.CloseBold,
            [TokenType.Italic] = TokenType.CloseItalic,
            [TokenType.Color] = TokenType.CloseColor,
            [TokenType.Size] = TokenType.CloseSize
        };

        static TokenType? GetCloseTag(Token tag)
        {
            TokenType closeTag;
            if (closeTags.TryGetValue(tag.Type, out closeTag)) { return closeTag; }
            return null;
        }

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
                return () =>
                {
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

        class Parser
        {
            List<Token> tokens;
            int position = 0;
            int recursionLayer = 0;

            bool EOF() => position >= tokens.Count;

            Token Current() => tokens[position];

            void Next() => position++;

            void PushLevel() => recursionLayer++;

            void PopLevel() => recursionLayer--;

            bool MaxLevel() => recursionLayer >= 100;

            void AddElement(List<Element> body, Element e)
            {
                if (EOF())
                {
                    body.AddRange(e.Body);
                    return;
                }
                body.Add(e);
                Next();
            }

            void AddString(List<Element> body, object s) => body.Add(Element.String(s));

            void AddTag(List<Element> body, ElementType type, Token tag)
                => AddElement(body, Element.Tag(type, Tag(tag)));

            void AddParamTag(List<Element> body, ElementType type, Token tag)
                => AddElement(body, Element.ParamTag(type, tag.Val, Tag(tag)));

            List<Element> Tag(Token tag)
            {
                // protect against stack overflow attacks.
                // remove body if max recursion layer is
                // exceeded.
                if (MaxLevel())
                {
                    return new List<Element>();
                }
                PushLevel();
                // go through all children.
                // if eof is reached, prepend string of tag to body,
                // since no end tag could be found.
                // if close tag for this tag is found, return children.
                // open new tags for bold/italic/color/size, add strings
                // and invalid tokens as strings.
                // advance token position when writing any token and when
                // leaving a tag.
                // tag == null is only true for the the root tag.
                var body = new List<Element>();
                while (true)
                {
                    if (EOF())
                    {
                        if (tag != null)
                        {
                            body.Insert(0, Element.String(tag.Pattern));
                        }
                        PopLevel();
                        return body;
                    }
                    var t = Current();
                    if (tag != null && t.Type == GetCloseTag(tag))
                    {
                        PopLevel();
                        return body;
                    }
                    Next();
                    switch (t.Type)
                    {
                        case TokenType.String:
                            AddString(body, t.Val);
                            break;
                        case TokenType.Bold:
                            AddTag(body, ElementType.Bold, t);
                            break;
                        case TokenType.Italic:
                            AddTag(body, ElementType.Italic, t);
                            break;
                        case TokenType.Color:
                            AddParamTag(body, ElementType.Color, t);
                            break;
                        case TokenType.Size:
                            AddParamTag(body, ElementType.Size, t);
                            break;
                        default:
                            AddString(body, t.Pattern);
                            break;
                    }
                }
            }

            public static List<Element> Parse(List<Token> tokens)
            {
                var p = new Parser();
                p.tokens = tokens;
                return p.Tag(null);
            }
        }

        public static List<Element> Parse(string text) => Parser.Parse(Lexer.Lex(text));

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
