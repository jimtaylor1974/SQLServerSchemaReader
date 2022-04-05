namespace SQLServerSchemaReader;

public class UserDefinedTableType
{
    public string Schema { get; set; }
    public string Name { get; set; }
    public string QualifiedName => $"{Schema}.{Name}";
    public List<ColumnDefinition> Columns { get; set; } = new List<ColumnDefinition>();
}