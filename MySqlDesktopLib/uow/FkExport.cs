using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MySqlDesktop.data.datamodel;

namespace MySqlDesktop.uow
{
    public class FkExport : BaseExporter
    {
        private readonly string _database;
        private readonly IList<Entity> _entities;
        private static IList<EntityRecord> _entityRecords;

        public FkExport(string database, IList<Entity> entities)
        {
            _database = database;
            _entities = entities;
            CreateOutputFolder(_database);
        }

        static string GetOneText()
        {
            var parts = new List<string>
                        {
                            "if (id == null) return null;",
                            "var tt = typeof(T);"
                        };

            var ets = _entityRecords;

            for (var i = 0; i < _entityRecords.Count; i++)
            {
                var et = _entityRecords[i];

                var str = string.Join(Environment.NewLine,
                                      $"// {i + 1} of {_entityRecords.Count}",
                                      $"if(tt == typeof({et.ClassName}))",
                                      $"\treturn OneTask<{et.ClassName}>.Get(id)?.{et.Column};");
                parts.Add(str);
            }

            parts.Add("return null;");

            var sw1 = new StringWriter();
            sw1.WriteLine($"public static string GetOneText<T>(int? id) where T : DataObject");
            sw1.Write(CodeGroup(parts));

            return sw1.ToString();
        }

        static string GetCompositeText()
        {
            var parts = new List<string>
                        {
                            "if (id == null) return null;",
                            "var tt = typeof(T);"
                        };

            var ets = _entityRecords;

            for (var i = 0; i < ets.Count; i++)
            {
                var et = ets[i];

                var str = string.Join(Environment.NewLine,
                                      $"var it = OneTask<{et.ClassName}>.Get(id);",
                                      $"return it == null ? null : $\"{{it.{et.Column}}}\";");

                str = string.Join(Environment.NewLine,
                                  $"// {i + 1} of {ets.Count}",
                                  $"if(tt == typeof({et.ClassName}))",
                                  CodeGroup(str));

                parts.Add(str);
            }

            parts.Add("return null;");

            var sw2 = new StringWriter();
            sw2.WriteLine($"public static string GetCompositeText<T>(int? id) where T : DataObject");
            sw2.Write(CodeGroup(parts));

            return sw2.ToString();
        }

        static string GetText()
        {
            var sw3 = new StringWriter();
            sw3.WriteLine($"public static string GetText<T>(int? id, bool loadFk) where T : DataObject");
            sw3.Write(CodeGroup($"return loadFk == false ? string.Empty : GetOneText<T>(id) ?? GetCompositeText<T>(id) ?? $\"ID::{{id}}\";"));
            return sw3.ToString();
        }

        //static string GetText()
        //{
        //    var sw3 = new StringWriter();
        //    sw3.WriteLine($"public static string GetText<T>(int? id) where T : DataObject");
        //    sw3.Write(CodeGroup($"return GetOneText<T>(id) ?? GetCompositeText<T>(id) ?? $\"ID::{{id}}\";"));
        //    return sw3.ToString();
        //}

        public void Export()
        {
            var entities = _entities;
            var records = OneTask<Record>.List(e => e.Database == _database);

            _entityRecords = (from et in entities
                              let rc = records.FirstOrDefault(r => r.Table == et.Table && r.NetType == "string")
                              where rc != null
                              select new EntityRecord
                              {
                                  ClassName = et.ClassName,
                                  Column = rc.Column
                              }).ToList();

            //  FK file
            {
                var sw = new StringWriter();

                ////  OneTask
                //{
                //    sw.WriteLine($"public sealed class OneTask<TEntity> : ansyl.dao. BaseOneTask<UnitOfWork, TEntity> where TEntity : DataObject");
                //    sw.WriteLine(CodeGroup(new List<string>()));
                //    sw.WriteLine();
                //}

                //  FK
                {
                    var parts = new[] { GetText(), GetOneText(), GetCompositeText() };
                    sw.WriteLine($"public class Fk");
                    sw.Write(CodeGroup(parts));
                }

                var str = sw.ToString();

                sw = new StringWriter();
                sw.WriteLine("// ReSharper disable CheckNamespace");
                sw.WriteLine($"//Creation Time: {DateTime.Now:s}");
                sw.WriteLine("");
                sw.WriteLine("using ansyl;");
                sw.WriteLine("using ansyl.dao;");
                sw.WriteLine("using vm;");
                sw.WriteLine();

                sw.WriteLine($"namespace dm.{_database}");
                sw.WriteLine(CodeGroup(str));

                WriteAllText($"dao\\FK.cs", sw);
            }
        }
    }
}