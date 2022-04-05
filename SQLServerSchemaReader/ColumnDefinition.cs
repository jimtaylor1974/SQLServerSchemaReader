namespace SQLServerSchemaReader;

public class ColumnDefinition
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string CollationName { get; set; }
    public int? Size { get; set; }
    public int? Precision { get; set; }
    public bool? Nullable { get; set; }
    public bool? NotNullable { get; set; }
    public string UniqueIndexName { get; set; }
    public bool? Unique { get; set; }
    public string PrimaryKeyName { get; set; }
    public bool? PrimaryKey { get; set; }
    public object DefaultValue { get; set; }
    public bool? Identity { get; set; }
    public string ColumnDescription { get; set; }
    public SystemMethods? WithDefault { get; set; }
    public string IndexName { get; set; }
}