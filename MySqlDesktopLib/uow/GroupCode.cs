using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MySqlDesktop.uow
{
    public class GroupCode
    {
        public static string Get(IEnumerable<string> leaders, IEnumerable<string> children)
        {
            var str = string.Join("\r\n", children);
            str = BaseExporter.CodeGroup(str);

            var lines = leaders.ToList();
            lines.Add(str);

            return string.Join(Environment.NewLine, lines)
                         .TrimEnd('\r', '\n', '\t', ' ');
        }

        public static string Get(string leader, IEnumerable<string> children)
        {
            return Get(new[] { leader }, children);
        }

        public static string Get(string leader, params string[] children)
        {
            return Get(new[] { leader }, children);
        }
    }

    public class SectionCode
    {
        public static string Get(IEnumerable<string> leaders, IEnumerable<string> children)
        {
            var str = string.Join("\r\n\r\n", children);
            str = BaseExporter.CodeGroup(str);

            var lines = leaders.ToList();
            lines.Add(str);

            return string.Join(Environment.NewLine, lines)
                         .TrimEnd('\r', '\n', '\t', ' ');
        }

        public static string Get(string leader, IEnumerable<string> children)
        {
            return Get(new[] { leader }, children);
        }

        public static string Get(string leader, params string[] children)
        {
            return Get(new[] { leader }, children);
        }
    }

    public enum SeparatorType : byte
    {
        One = 1,
        Two = 2
    }

    public class Section
    {
        private readonly SeparatorType _separatorType;

        public static Section One() => new Section(SeparatorType.One);

        public static Section Two() => new Section(SeparatorType.Two);

        Section(SeparatorType separatorType)
        {
            _separatorType = separatorType;
        }

        private readonly List<string> _leaders = new List<string>();
        private readonly List<string> _children = new List<string>();

        public Section AddLeader(string line, bool trim = false)
        {
            if (trim)
                line = line.Trim('\r', '\n', ' ');
            _leaders.Add(line);
            return this;
        }

        public Section AddLeader(IEnumerable<string> lines, bool trim = false)
        {
            _leaders.ForEach(c => AddLeader(c, trim));

            return this;
        }

        public Section AddChild(string line, bool trim = false)
        {
            if (trim)
                line = line.Trim('\r', '\n', ' ');

            _children.Add(line);
            return this;
        }

        public Section AddChildren(IEnumerable<string> lines, bool trim = false)
        {
            foreach (var line in lines)
            {
                AddChild(line, trim);
            }
            return this;
        }

        public Section AddChild(Section section, bool trim = false) => AddChild(section.ToString(), trim);
        //public Section AddLeader(Section section) => AddChild(section.ToString());

        public override string ToString()
        {
            var sep = "";

            if (_separatorType == SeparatorType.One)
                sep = Environment.NewLine;
            else if (_separatorType == SeparatorType.Two)
                sep = Environment.NewLine + Environment.NewLine;

            var str = string.Join(sep, _children);
            str = BaseExporter.CodeGroup(str);

            var lines = _leaders.ToList();
            lines.Add(str);

            return string.Join(Environment.NewLine, lines)
                         .TrimEnd('\r', '\n', '\t', ' ');
        }
    }
}
