using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ansyl;
using MySqlDesktop.data.datamodel;
using MySqlDesktop.uow;

namespace MySqlDesktop.ui
{
    /// <summary>
    /// Database Field HTML Interface
    /// </summary>
    public interface IFieldUi
    {
        string GetLabel(bool isViewer);
        string GetViewer();
        string GetEditor();
    }

    /// <summary>
    /// Database Table HTML Interface
    /// </summary>
    public interface ITableUi
    {
        string GetEditOneDL();
        string GetEditOneDV();
        string GetViewOneDL();
        string GetViewOneDV();
    }

    /// <summary>
    /// UI Interface Mode
    /// </summary>
    public enum UiMode
    {
        ViewOne = 1,
        EditOne = 2,
        ViewList = 3
    }

    /// <summary>
    /// Data HTML Container - either DL (data list) or DV (div)
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum UiContainer
    {
        DV = 1,
        DL = 2
    }

    public class TableUi : ITableUi
    {
        public Entity Entity   { get; }
        public string Database { get; }
        public string DmClass  => $"{Entity.ClassName}";
        public string VmClass  => $"{Entity.ClassName}ViewModel";

        public IList<Record> Records { get; }

        public TableUi(string database, Entity entity, IList<Record> records)
        {
            Database = database;
            Entity   = entity;
            Records  = records;
        }

        private const string NBSP = "&nbsp";

        private IEnumerable<KeyValuePair<string, string>> GetKeyValuePairs(bool isView)
        {
            var fieldUis = (from rc in Records
                            select new
                                   {
                                       Record = rc,
                                       Ui     = new FieldUi(rc)
                                   }).ToList();

            var lines = (from field in fieldUis
                         let ui = field.Ui
                         let label = ui.GetLabel(isView)
                         let value = isView ? ui.GetViewer() : ui.GetEditor()
                         select new KeyValuePair<string, string>(label, value)).ToList();

            if (isView)
            {
                //from ui in fieldUis
                //where ui.Record.EnumDataType == EnumDataType.Hidden
                //select 

                var column = Records.SingleOrDefault(i => i.IsPrimaryKey == true)?.Column;
                //const string value = //"<input type=\"submit\" value=\"Submit Changes\" >";
                var value = $"<a href=\"@Url.Action(\"Edit{DmClass}\", new {{id = Model.{column}}})\">Edit</a>";
                lines.Add(new KeyValuePair<string, string>(NBSP, value));
            }
            else
            {
                const string value = "<input type=\"submit\" value=\"Submit Changes\" >";
                lines.Add(new KeyValuePair<string, string>(NBSP, value));
            }

            return lines;
        }

        private string _editOneDl, _editOneDv, _viewOneDl, _viewOneDv;

        public string GetEditOneDL()
        {
            if (_editOneDl.IsNullOrWhite())
            {
                var lines    = GetKeyValuePairs(false);
                var elements = new List<string>();

                //var dl = new Element(EnumTag.Dl);

                foreach (var (label, value) in lines)
                {
                    var element1 = Element.Create(EnumTag.Dt, null, label);
                    var element2 = Element.Create(EnumTag.Dd, null, value);
                    elements.AddRange(new[] {element1, element2});
                }

                var dl = new Element(EnumTag.Dl, "dl-horizontal");
                dl.AddChildren(elements);

                var dv12 = new Element(EnumTag.Div, "col-xs-12");
                dv12.AddChild(dl);

                var form = NewForm();
                form.AddChild(dv12);

                _editOneDl = GetFullPage(form.GetContents());
            }

            return _editOneDl;
        }

        Element NewForm()
        {
            var action  = $"Post{DmClass}";
            var element = new Element(EnumTag.Form);
            element.AddAttribute("action",  $"@Url.Action(\"{action}\")");
            element.AddAttribute("method",  "post");
            element.AddAttribute("enctype", "multipart/form-data");
            return element;
        }

        public string GetEditOneDV()
        {
            if (_editOneDv.IsNullOrWhite())
            {
                var lines    = GetKeyValuePairs(false);
                var elements = new List<string>();

                const string cls1 = "col-xs-3";
                const string cls2 = "col-xs-9";
                const string cls3 = "clearfix";

                foreach (var (label, value) in lines)
                {
                    elements.AddRange(new[]
                                      {
                                          Element.Create(EnumTag.Div, cls1, label),
                                          Element.Create(EnumTag.Div, cls2, value),
                                          Element.Create(EnumTag.Div, cls3, "")
                                      });
                }

                var form = NewForm();
                form.AddChildren(elements);

                _editOneDv = GetFullPage(form.GetContents());
            }

            return _editOneDv;
        }

        public string GetViewOneDL()
        {
            if (_viewOneDl.IsNullOrWhite())
            {
                var lines = GetKeyValuePairs(true);

                var elements = new List<string>();

                foreach (var (label, value) in lines)
                {
                    var element1 = Element.Create(EnumTag.Dt, null, label);
                    var element2 = Element.Create(EnumTag.Dd, null, value);
                    //var elements = JoinElements(element1, element2);
                    elements.AddRange(new[] {element1, element2});
                }

                var dl = new Element(EnumTag.Dl, "dl-horizontal");
                dl.AddChildren(elements);
                //dl.AddChild(elements);

                var dv12 = new Element(EnumTag.Div, "col-xs-12");
                dv12.AddChild(dl);

                _viewOneDl = GetFullPage(dv12.GetContents());
            }

            return _viewOneDl;
        }

        public string GetViewOneDV()
        {
            if (_viewOneDv.IsNullOrWhite())
            {
                var lines    = GetKeyValuePairs(true);
                var elements = new List<string>();

                const string cls1 = "col-xs-3";
                const string cls2 = "col-xs-9";
                const string cls3 = "clearfix";

                foreach (var (label, value) in lines)
                {
                    elements.AddRange(new[]
                                      {
                                          Element.Create(EnumTag.Div, cls1, label),
                                          Element.Create(EnumTag.Div, cls2, value),
                                          Element.Create(EnumTag.Div, cls3, "")
                                      });

                    //var element1 = Element.Create(EnumTag.Div, cls1, label);
                    //var element2 = Element.Create(EnumTag.Div, cls2, value);
                    //elements.AddRange(new[] { element1, element2 });
                }

                _viewOneDv = GetFullPage(elements.ToArray());
            }

            return _viewOneDv;
        }

        private static string JoinElements(params string[] elements)
        {
            return string.Join(Environment.NewLine, elements);
        }

        private string GetFullPage(params string[] contents)
        {
            string row1, row2;

            {
                //  1: title
                var h3       = Element.Create(EnumTag.H3,  null,        DmClass);
                var divTitle = Element.Create(EnumTag.Div, "col-xs-12", h3);

                //  2: blank
                var divBlank = Element.Create(EnumTag.Div, "col-xs-12", string.Empty);

                //  3: row 1
                var str1 = JoinElements(divTitle, divBlank);
                row1 = Element.Create(EnumTag.Div, "row iModel", str1);
            }

            {
                //  4: row 2
                var str2 = string.Join(Environment.NewLine, contents);
                row2 = Element.Create(EnumTag.Div, "row iModel", str2);
            }

            var swPage = new StringWriter();
            swPage.WriteLine($"@model view.{Database}.{VmClass}");

            foreach (var row in new[] {row1, row2})
            {
                swPage.WriteLine();
                swPage.WriteLine(row);
            }

            return swPage.ToString();
        }
    }

    public class FieldUi : IFieldUi
    {
        private readonly Record _record;
        private string _editor;
        private string _viewer;

        public FieldUi(Record @record)
        {
            this._record = @record;
        }

        public string GetLabel(bool isViewer)
        {
            var name  = _record.Column;
            var title = Regex.Replace(name, "[a-z][A-Z]", m => m.Value.Insert(1, " "));

            var rr = _record;

            if (rr.IsPrimaryKey == false || rr.IsForeignKey)
                return isViewer
                           ? Element.Create(EnumTag.Span, null, title)
                           : $"<label for=\"{name}\">{title}:</label>";

            return string.Empty;
            //if (_record.IsPrimaryKey == true)
            //    return string.Empty;

            //return isViewer
            //           ? Element.Create(EnumTag.Span, null, title)
            //           : $"<label for=\"{name}\">{title}:</label>";
        }

        private static string GetAttributes(IDictionary<string, object> dic)
        {
            dic["class"] = "iEdit";

            return string.Join(" ", from p in dic
                                    let val = Convert.ToString(p.Value)
                                    where val.IsNullOrWhite() == false
                                    let q = val.Contains("\"") ? '\'' : '\"'
                                    select $"{p.Key}={q}{p.Value}{q}");
        }

        public bool HasModel = true;

        private string Input(string type, int? maxlength = null, string placeholder = null)
        {
            var name = _record.Column;

            maxlength ??= _record.MaxLength ?? 0;

            var dic = new Dictionary<string, object>
                      {
                          ["type"] = type,
                          ["id"]   = name,
                          ["name"] = name
                      };

            if (maxlength > 0)
                dic["maxlength"] = $"{maxlength}";

            if (placeholder != null)
                dic["placeholder"] = placeholder;

            if (HasModel)
                dic["value"] = $"@Model.{name}";

            var attributes = GetAttributes(dic);
            return _editor = $"<input {attributes} >";
        }

        private string InputEnumSet()
        {
            const char separator  = 'µ';
            var        rc         = _record;
            var        columnType = rc.ColumnType;
            var        isEnum     = columnType.IsMatch("^enum");
            var        name       = rc.Column;
            var        inputType  = isEnum ? "radio" : "checkbox";

            var flds = columnType.ReplaceX("','", separator.ToString())
                                 .ReplaceX("^(enum|set)", "")
                                 .Trim('(', '\'', ')')
                                 .Split(separator);

            var map = new Dictionary<int, string>();

            for (int i = 0; i < flds.Length; i++)
            {
                var id    = $"{name}{i  + 1}";
                var value = isEnum ? (i + 1) : (1 << i);
                var title = Regex.Replace(flds[i], "([A-Z]+)|([0-9]+)", m => " " + m.Value).Trim();

                var dic = new Dictionary<string, object>
                          {
                              ["type"]  = inputType,
                              ["id"]    = id,
                              ["name"]  = name,
                              ["value"] = value
                          };

                var attributes = GetAttributes(dic);

                map[value] = $"<input {attributes} @vm.Html.IsChecked(@Model.{name}, {value})> " +
                             $"<label for=\"{id}\">{title}</label>";
            }

            //if (isEnum && record.IsNullable)
            //    map[0] = "(None)";

            return string.Join("<br>\r\n", map.Select(p => p.Value));
        }

        private string InputCheckBox()
        {
            var name    = _record.Column;
            var options = new[] {"No", "Yes"};

            var map = new Dictionary<int, string>();

            for (var i = 0; i < options.Length; i++)
            {
                var id    = $"{name}{i + 1}";
                var value = i;
                var title = options[i];

                var dic = new Dictionary<string, object>
                          {
                              ["type"]  = "radio",
                              ["id"]    = id,
                              ["name"]  = name,
                              ["value"] = value
                          };

                var attributes = GetAttributes(dic);

                map[value] = $"<input {attributes} @vm.Html.IsChecked(@Model.{name}, {value})> " +
                             $"<label for=\"{id}\">{title}</label>";
            }

            return string.Join("<br>\r\n", map.Select(p => p.Value));
        }

        private string TextArea()
        {
            var name      = _record.Column;
            var maxlength = _record.MaxLength ?? 0;

            const int rows = 3;
            const int cols = 50;

            var dic = new Dictionary<string, object>
                      {
                          ["id"]        = name,
                          ["name"]      = name,
                          ["rows"]      = rows,
                          ["cols"]      = cols,
                          ["maxlength"] = maxlength,
                      };

            var value = HasModel == false ? string.Empty : $"@Model.{name}";

            return $"<textarea {GetAttributes(dic)}>{value}</textarea>";
        }

        public string GetEditor()
        {
            if (_editor != null)
                return _editor;

            var elements = new List<string> {GetEditorElements()};

            if (HasModel && _record.IsForeignKey)
            {
                var value = $"@Model.{_record.FkColumn}";

                var element = $"<span id=\"{_record.FkColumn}\" class=\"iView\">{value}</span>";
                elements.Add(element);
            }

            return _editor = string.Join(Environment.NewLine, from e in elements
                                                              where e.IsNullOrWhite() == false
                                                              select e);
        }

        public string GetEditorElements()
        {
            var enumDataType = _record.EnumDataType;

            //  key-hidden fields
            if (enumDataType == EnumDataType.Hidden)
                return Input("hidden");

            //  number fields
            if (enumDataType.In(EnumDataType.Integer, EnumDataType.Real))
                return Input("number");

            //  radiobox and checkbox
            if (enumDataType == EnumDataType.Enum || enumDataType == EnumDataType.Set)
                return InputEnumSet();

            //  radiobox and checkbox
            if (enumDataType == EnumDataType.CheckBox)
                return InputCheckBox();

            //  long text fields
            if (enumDataType == EnumDataType.TextArea)
                return TextArea();

            //  date-time fields
            switch (enumDataType)
            {
                case EnumDataType.Date:
                    return Input("date", placeholder: "yyyy-MM-dd");
                case EnumDataType.Time:
                    return Input("time", maxlength: 8, placeholder: "HH:mm:ss");
                case EnumDataType.DateTime:
                    return Input("datetime-local", maxlength: 19, placeholder: "yyyy-MM-ddTHH:mm");
                case EnumDataType.Month:
                    return Input("month", maxlength: 7, placeholder: "yyyy-MM");
            }

            //  short text fields
            if (enumDataType == EnumDataType.Text)
            {
                var name = _record.Column;

                switch (name)
                {
                    case "Email":
                        return Input("email");
                    case "Password":
                        return Input("password");
                    default:
                        return Input("text");
                }
            }

            return null;
        }

        static string GetDataFields(params string[] fields)
        {
            return fields.Length == 0 ? null : string.Join(" ", fields).Trim();
        }

        public string GetViewer()
        {
            if (_viewer != null)
                return _viewer;

            const string classSpan = "iView";

            var value = string.Empty;

            if (HasModel)
                value = _record.IsForeignKey ? $"@Model.{_record.FkColumn}" : $"@Model.{_record.Column}";

            return _viewer ??= $"<span id=\"{_record.Column}\" class=\"{classSpan}\">{value}</span>";
        }
    }

    /// <summary>
    /// HTML input element type
    /// </summary>
    public enum EnumDataType : byte
    {
        None = 0,
        Hidden = 1,
        Text,
        TextArea,
        Date,
        Time,
        DateTime,
        Integer,
        Real,

        Email,
        Phone,
        Month,
        Color,

        //Byte,

        Enum,
        Set,
        Blob,
        CheckBox
    }

    /// <summary>
    /// HTML element
    /// </summary>
    public enum EnumTag : byte
    {
        Html = 1,
        Head,
        Title,
        Div,
        Img,
        H1,
        H2,
        H3,
        H4,
        H5,
        H6,
        P,
        Br,
        Span,
        Dl,
        Dt,
        Dd,
        Form
    }
}
