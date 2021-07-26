using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ansyl;

namespace MySqlDesktop.ui
{
    /// <summary>
    /// Base UI HTML Element
    /// </summary>
    public abstract class BaseHtmlElement
    {
        public abstract string GetContents();

        public override string ToString()
        {
            return GetContents();
        }
    }

    /// <summary>
    /// UI HTML String Element
    /// </summary>
    public class StringElement : BaseHtmlElement
    {
        private readonly string _str;

        public StringElement(string str)
        {
            _str = str?.TrimEnd('\r', '\n', '\t');
        }

        public override string GetContents()
        {
            return _str;
        }
    }

    /// <summary>
    /// General UI HTML Element
    /// </summary>
    public class Element : BaseHtmlElement
    {
        private readonly EnumTag _tag;
        private readonly IList<BaseHtmlElement> _children = new List<BaseHtmlElement>();
        private readonly IList<string> _classItems = new List<string>();
        private readonly IDictionary<string, object> _attributes = new Dictionary<string, object>();

        public Element(EnumTag tag, string @class = null)
        {
            _tag = tag;

            AddClass(@class);
        }

        public void AddClass(string newClass)
        {
            if (string.IsNullOrWhiteSpace(newClass))
                return;

            const string key = "class";

            if (_attributes.ContainsKey(key) == false)
                _attributes[key] = "";

            foreach (var nc in newClass?.Trim().Split(' '))
            {
                if (_classItems.Contains(nc) != false) continue;

                _classItems.Add(nc);
                _attributes[key] = (_attributes[key] + " " + nc).Trim();
            }
        }

        public void AddAttribute(string key, string val)
        {
            if (val.IsNullOrWhite())
                return;

            if (key.Equals("class"))
            {
                AddClass(val);
            }
            else
            {
                _attributes[key] = val;
            }
        }

        public void AddChild(BaseHtmlElement child) => _children.Add(child);
        public void AddChild(string str) => AddChild(new StringElement(str));

        private string _contents;

        public override string ToString()
        {
            return _contents ??= GetContents();
        }

        public static Element Create(EnumTag tag, string @class = null, BaseHtmlElement childElement = null)
        {
            return Create(tag, @class, new[] { childElement });
        }

        public static Element Create(EnumTag tag, string @class, params BaseHtmlElement[] childElements)
        {
            var element = new Element(tag, @class);
            childElements.ForEach(element.AddChild);
            return element;
        }

        public static string Create(EnumTag enumTag, string @class, string content)
        {
            return Create(enumTag, @class, new StringElement(content)).GetContents();
        }

        public override string GetContents()
        {
            var tag = _tag.ToString().ToLower();
            string body = null;
            bool isSingleLine;

            //  1: body
            {
                var strBody = string.Join("", _children.Select(c => c.GetContents()))
                                    .Trim('\r', '\n');

                isSingleLine = tag.IsMatch("dl|dt|dd|div") == false && 
                               strBody.IsMatch("div") == false && 
                               (tag.In("span") || strBody.Length <= 100); // 50;

                if (isSingleLine == false)
                    strBody = "\t" + Regex.Replace(strBody, @"[\r\n]+", m => m.Value + "\t");

                body = strBody.TrimEnd('\r', '\n', '\t', ' ');
            }

            var strAttributes = string.Join(" ", from a in _attributes
                                                 select $"{a.Key}=\"{a.Value}\"");

            var startTag = $"<{tag} {strAttributes}>".Replace(" >", ">");
            var closeTag = $"</{tag}>";

            var separator = isSingleLine ? "" : Environment.NewLine;

            return string.Join(separator, from it in new[] {startTag, body, closeTag}
                                          where it.IsNullOrWhite() == false
                                          select it);
        }

        public void AddChildren(IEnumerable<string> elements)
        {
            var str = string.Join(Environment.NewLine, elements);

            AddChild(new StringElement(str));
        }
    }
}
