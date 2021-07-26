using System;
using ansyl.dao;
using MySqlDesktop.ui;

namespace MySqlDesktop.data.datamodel
{
    /// <summary>
    /// Entity Data Model
    /// </summary>
    public class Entity : DataObject
    {
        public int    EntityId  { get; set; }
        public string Database  { get; set; }
        public string Table     { get; set; }
        public string ClassName { get; set; }
    }

    /// <summary>
    /// Record Data Model 
    /// </summary>
    public class Record : DataObject
    {
        public int          RecordId        { get; set; }
        public string       Database        { get; set; }
        public string       Table           { get; set; }
        public string       Column          { get; set; }
        public string       DataType        { get; set; }
        public string       ColumnType      { get; set; }
        public byte         Position        { get; set; }
        public bool         IsNullable      { get; set; }
        public bool         IsUnsigned      { get; set; }
        public int?         MaxLength       { get; set; }
        public string       NetType         { get; set; }
        public string       DataModelCode   { get; set; }
        public string       ViewModelCode   { get; set; }
        public string       ViewModelFKCode { get; set; }
        public string       DataToView      { get; set; }
        public string       DataToViewFK    { get; set; }
        public string       ViewToData      { get; set; }
        public bool         IsPrimaryKey    { get; set; }
        public bool         IsUniqueKey     { get; set; }
        public bool         IsLocalKey      { get; set; }
        public bool         IsForeignKey    { get; set; }
        public EnumDataType EnumDataType    { get; set; }
        public DateTime     InsertTime      { get; set; }

        public string FkColumn => IsForeignKey == false ? null : $"{Column}Text";
    }
}
