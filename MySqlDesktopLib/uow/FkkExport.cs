using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ansyl;
using MySqlDesktop.data.datamodel;

namespace MySqlDesktop.uow
{
    public class FkkExport : BaseExporter
    {
        private readonly string _database;
        private readonly IList<Entity> _entities;
        private static IList<EntityRecord> _entityRecords;

        public FkkExport(string database, IList<Entity> entities)
        {
            _database = database;
            _entities = entities;
            CreateOutputFolder(_database);
        }

        private static IEnumerable<StringWriter> GetOneSections()
        {
            var ets = _entityRecords;

            for (var i = 0; i < ets.Count; i++)
            {
                var et = ets[i];

                var str = string.Join(Environment.NewLine,
                                      $"// {i + 1} of {ets.Count}",
                                      //$"if(tt == typeof({et.ClassName}))",
                                      $"private static string One({et.ClassName} it) => it == null ? null : $\"{{it.{et.Column}}}\";");

                var sw = new StringWriter();
                sw.WriteLine(str);
                yield return sw;
            }
        }

        private IEnumerable<StringWriter> GetTextSections()
        {
            {
                var sw = new StringWriter();
                sw.WriteLine($"private readonly OneTransaction _ot;");
                yield return sw;
            }

            {
                var sw = new StringWriter();
                sw.WriteLine($"public Fkk(OneTransaction ot)");
                sw.WriteLine(CodeGroup("_ot = ot;"));
                yield return sw;
            }

            {
                var sw = new StringWriter();
                sw.WriteLine($"private T Get<T>(int? id) where T : DataObject");
                sw.WriteLine(CodeGroup("return id == null || id <= 0 ? null : _ot.Get<T>(id);"));
                yield return sw;
            }

            {
                var parts = new List<string>();

                parts.Add($"if (isLazy || id == null || id == 0) return null;");
                parts.Add($"var tt = typeof(T);");

                var ets = _entityRecords;

                for (var i = 0; i < ets.Count; i++)
                {
                    var et = ets[i];

                    var str = string.Join(Environment.NewLine,
                                          $"// {i + 1} of {ets.Count}",
                                          $"if(tt == typeof({et.ClassName}))",
                                          $"\treturn One(Get<{et.ClassName}>(id));");

                    parts.Add(str);
                }

                parts.Add("return null;");

                var sw = new StringWriter();
                sw.WriteLine($"public string GetText<T>(int? id, bool isLazy = false) where T : DataObject");
                sw.WriteLine(CodeGroup(parts));
                yield return sw;
            }
        }

        public void Export()
        {
            var entities = _entities;
            var records  = OneTask<Record>.List(e => e.Database == _database);

            _entityRecords = (from et in entities
                              let rc = records.FirstOrDefault(r => r.Table == et.Table && r.NetType == "string" && r.Column.IsMatch("Guid") == false)
                              where rc != null
                              select new EntityRecord
                                     {
                                         ClassName = et.ClassName,
                                         Column    = rc.Column
                                     }).ToList();

            var sections1 = GetTextSections();
            var sections2 = GetOneSections();

            var sw = new StringWriter();

            //  FKK
            {
                var parts = from sections in new[] {sections1, sections2}
                            from section in sections
                            let str = section.ToString().TrimEnd('\r', '\n')
                            where str.IsNullOrWhite() == false
                            select str;

                sw.WriteLine($"public class Fkk");
                sw.Write(CodeGroup(parts));
            }

            {
                var str = sw.ToString();

                sw = new StringWriter();
                sw.WriteLine("// ReSharper disable CheckNamespace");
                sw.WriteLine($"//Creation Time: {DateTime.Now:s}");
                sw.WriteLine("");
                sw.WriteLine("using ansyl;");
                sw.WriteLine("using ansyl.dao;");
                sw.WriteLine("using dto;");
                sw.WriteLine();

                sw.WriteLine($"namespace dto.{_database}");
                sw.WriteLine(CodeGroup(str));

                WriteAllText($"dao\\Fkk.cs", sw);
            }
        }
    }
}
