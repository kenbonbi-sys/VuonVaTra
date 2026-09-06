using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VuonNho.Core
{
    public enum JsonKind
    {
        Null,
        Bool,
        Number,
        String,
        Object,
        Array
    }

    public sealed class JsonParseException : Exception
    {
        public JsonParseException(string message) : base(message) { }
    }

    /// <summary>
    /// JSON toi thieu, du cho save snapshot. Nam trong Core de logic khong phu thuoc
    /// serializer cua engine, va de test doc lap voi Unity.
    /// Thu tu key duoc giu nguyen theo luc them, nen hai lan ghi cung state cho cung chuoi.
    /// </summary>
    public sealed class JsonValue
    {
        public JsonKind Kind { get; private set; }

        bool _bool;
        string _text;                                   // chuoi hoac token so nguyen ban
        List<string> _keys;
        Dictionary<string, JsonValue> _members;
        List<JsonValue> _items;

        JsonValue(JsonKind kind) { Kind = kind; }

        public static JsonValue Null() { return new JsonValue(JsonKind.Null); }

        public static JsonValue Of(bool value)
        {
            var v = new JsonValue(JsonKind.Bool);
            v._bool = value;
            return v;
        }

        public static JsonValue Of(long value)
        {
            var v = new JsonValue(JsonKind.Number);
            v._text = value.ToString(CultureInfo.InvariantCulture);
            return v;
        }

        public static JsonValue Of(int value) { return Of((long)value); }

        public static JsonValue Of(string value)
        {
            if (value == null) return Null();
            var v = new JsonValue(JsonKind.String);
            v._text = value;
            return v;
        }

        public static JsonValue NewObject()
        {
            var v = new JsonValue(JsonKind.Object);
            v._keys = new List<string>();
            v._members = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            return v;
        }

        public static JsonValue NewArray()
        {
            var v = new JsonValue(JsonKind.Array);
            v._items = new List<JsonValue>();
            return v;
        }

        public int Count
        {
            get
            {
                if (Kind == JsonKind.Array) return _items.Count;
                if (Kind == JsonKind.Object) return _keys.Count;
                return 0;
            }
        }

        public IList<JsonValue> Items
        {
            get
            {
                if (Kind != JsonKind.Array) throw new JsonParseException("Gia tri khong phai mang.");
                return _items;
            }
        }

        public JsonValue Set(string key, JsonValue value)
        {
            if (Kind != JsonKind.Object) throw new JsonParseException("Gia tri khong phai object.");
            if (!_members.ContainsKey(key)) _keys.Add(key);
            _members[key] = value ?? Null();
            return this;
        }

        public JsonValue Set(string key, long value) { return Set(key, Of(value)); }
        public JsonValue Set(string key, int value) { return Set(key, Of((long)value)); }
        public JsonValue Set(string key, bool value) { return Set(key, Of(value)); }
        public JsonValue Set(string key, string value) { return Set(key, Of(value)); }

        public JsonValue Add(JsonValue value)
        {
            if (Kind != JsonKind.Array) throw new JsonParseException("Gia tri khong phai mang.");
            _items.Add(value ?? Null());
            return this;
        }

        public bool Has(string key)
        {
            return Kind == JsonKind.Object && _members.ContainsKey(key);
        }

        public JsonValue Get(string key)
        {
            JsonValue value;
            if (Kind == JsonKind.Object && _members.TryGetValue(key, out value)) return value;
            return null;
        }

        public JsonValue Require(string key)
        {
            var value = Get(key);
            if (value == null) throw new JsonParseException("Thieu truong: " + key);
            return value;
        }

        public long AsLong()
        {
            if (Kind != JsonKind.Number) throw new JsonParseException("Gia tri khong phai so.");
            long parsed;
            if (long.TryParse(_text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)) return parsed;
            double asDouble;
            if (double.TryParse(_text, NumberStyles.Float, CultureInfo.InvariantCulture, out asDouble))
                return (long)asDouble;
            throw new JsonParseException("So khong doc duoc: " + _text);
        }

        public int AsInt() { return checked((int)AsLong()); }

        public bool AsBool()
        {
            if (Kind != JsonKind.Bool) throw new JsonParseException("Gia tri khong phai bool.");
            return _bool;
        }

        /// <summary>Tra ve null cho JSON null, de phan biet voi chuoi rong.</summary>
        public string AsStringOrNull()
        {
            if (Kind == JsonKind.Null) return null;
            if (Kind != JsonKind.String) throw new JsonParseException("Gia tri khong phai chuoi.");
            return _text;
        }

        public long GetLong(string key, long fallback)
        {
            var value = Get(key);
            return value == null || value.Kind != JsonKind.Number ? fallback : value.AsLong();
        }

        public int GetInt(string key, int fallback) { return checked((int)GetLong(key, fallback)); }

        public bool GetBool(string key, bool fallback)
        {
            var value = Get(key);
            return value == null || value.Kind != JsonKind.Bool ? fallback : value.AsBool();
        }

        public string GetStringOrNull(string key)
        {
            var value = Get(key);
            return value == null ? null : value.AsStringOrNull();
        }

        public string ToJson(bool pretty)
        {
            var builder = new StringBuilder(1024);
            Write(builder, pretty, 0);
            return builder.ToString();
        }

        void Write(StringBuilder builder, bool pretty, int depth)
        {
            switch (Kind)
            {
                case JsonKind.Null:
                    builder.Append("null");
                    break;
                case JsonKind.Bool:
                    builder.Append(_bool ? "true" : "false");
                    break;
                case JsonKind.Number:
                    builder.Append(_text);
                    break;
                case JsonKind.String:
                    WriteString(builder, _text);
                    break;
                case JsonKind.Object:
                    builder.Append('{');
                    for (int i = 0; i < _keys.Count; i++)
                    {
                        if (i > 0) builder.Append(',');
                        NewLine(builder, pretty, depth + 1);
                        WriteString(builder, _keys[i]);
                        builder.Append(':');
                        if (pretty) builder.Append(' ');
                        _members[_keys[i]].Write(builder, pretty, depth + 1);
                    }
                    if (_keys.Count > 0) NewLine(builder, pretty, depth);
                    builder.Append('}');
                    break;
                case JsonKind.Array:
                    builder.Append('[');
                    for (int i = 0; i < _items.Count; i++)
                    {
                        if (i > 0) builder.Append(',');
                        NewLine(builder, pretty, depth + 1);
                        _items[i].Write(builder, pretty, depth + 1);
                    }
                    if (_items.Count > 0) NewLine(builder, pretty, depth);
                    builder.Append(']');
                    break;
            }
        }

        static void NewLine(StringBuilder builder, bool pretty, int depth)
        {
            if (!pretty) return;
            builder.Append('\n');
            for (int i = 0; i < depth; i++) builder.Append("  ");
        }

        static void WriteString(StringBuilder builder, string text)
        {
            builder.Append('"');
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < ' ')
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            builder.Append(c);
                        break;
                }
            }
            builder.Append('"');
        }

        public static JsonValue Parse(string text)
        {
            if (text == null) throw new JsonParseException("Chuoi JSON rong.");
            int index = 0;
            var value = ParseValue(text, ref index);
            SkipWhitespace(text, ref index);
            if (index != text.Length) throw new JsonParseException("Con du lieu thua sau JSON.");
            return value;
        }

        static JsonValue ParseValue(string text, ref int index)
        {
            SkipWhitespace(text, ref index);
            if (index >= text.Length) throw new JsonParseException("Ket thuc JSON bat ngo.");

            char c = text[index];
            if (c == '{') return ParseObject(text, ref index);
            if (c == '[') return ParseArray(text, ref index);
            if (c == '"') return Of(ParseString(text, ref index));
            if (c == 't') { Expect(text, ref index, "true"); return Of(true); }
            if (c == 'f') { Expect(text, ref index, "false"); return Of(false); }
            if (c == 'n') { Expect(text, ref index, "null"); return Null(); }
            return ParseNumber(text, ref index);
        }

        static JsonValue ParseObject(string text, ref int index)
        {
            var result = NewObject();
            index++; // '{'
            SkipWhitespace(text, ref index);
            if (index < text.Length && text[index] == '}') { index++; return result; }

            while (true)
            {
                SkipWhitespace(text, ref index);
                if (index >= text.Length || text[index] != '"') throw new JsonParseException("Key phai la chuoi.");
                string key = ParseString(text, ref index);
                SkipWhitespace(text, ref index);
                if (index >= text.Length || text[index] != ':') throw new JsonParseException("Thieu dau hai cham.");
                index++;
                result.Set(key, ParseValue(text, ref index));
                SkipWhitespace(text, ref index);
                if (index >= text.Length) throw new JsonParseException("Object chua dong.");
                if (text[index] == ',') { index++; continue; }
                if (text[index] == '}') { index++; return result; }
                throw new JsonParseException("Ky tu la trong object: " + text[index]);
            }
        }

        static JsonValue ParseArray(string text, ref int index)
        {
            var result = NewArray();
            index++; // '['
            SkipWhitespace(text, ref index);
            if (index < text.Length && text[index] == ']') { index++; return result; }

            while (true)
            {
                result.Add(ParseValue(text, ref index));
                SkipWhitespace(text, ref index);
                if (index >= text.Length) throw new JsonParseException("Mang chua dong.");
                if (text[index] == ',') { index++; continue; }
                if (text[index] == ']') { index++; return result; }
                throw new JsonParseException("Ky tu la trong mang: " + text[index]);
            }
        }

        static JsonValue ParseNumber(string text, ref int index)
        {
            int start = index;
            if (index < text.Length && (text[index] == '-' || text[index] == '+')) index++;
            while (index < text.Length)
            {
                char c = text[index];
                if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '-' || c == '+') index++;
                else break;
            }
            if (start == index) throw new JsonParseException("So khong hop le.");
            var value = new JsonValue(JsonKind.Number);
            value._text = text.Substring(start, index - start);
            return value;
        }

        static string ParseString(string text, ref int index)
        {
            index++; // '"'
            var builder = new StringBuilder();
            while (true)
            {
                if (index >= text.Length) throw new JsonParseException("Chuoi chua dong.");
                char c = text[index++];
                if (c == '"') return builder.ToString();
                if (c != '\\') { builder.Append(c); continue; }

                if (index >= text.Length) throw new JsonParseException("Escape khong hoan chinh.");
                char escape = text[index++];
                switch (escape)
                {
                    case '"': builder.Append('"'); break;
                    case '\\': builder.Append('\\'); break;
                    case '/': builder.Append('/'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'n': builder.Append('\n'); break;
                    case 'r': builder.Append('\r'); break;
                    case 't': builder.Append('\t'); break;
                    case 'u':
                        if (index + 4 > text.Length) throw new JsonParseException("Escape unicode khong hoan chinh.");
                        builder.Append((char)ushort.Parse(text.Substring(index, 4), NumberStyles.HexNumber,
                                                          CultureInfo.InvariantCulture));
                        index += 4;
                        break;
                    default:
                        throw new JsonParseException("Escape la: \\" + escape);
                }
            }
        }

        static void Expect(string text, ref int index, string literal)
        {
            if (index + literal.Length > text.Length ||
                string.CompareOrdinal(text, index, literal, 0, literal.Length) != 0)
                throw new JsonParseException("Mong doi " + literal);
            index += literal.Length;
        }

        static void SkipWhitespace(string text, ref int index)
        {
            while (index < text.Length)
            {
                char c = text[index];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r') index++;
                else break;
            }
        }
    }
}
