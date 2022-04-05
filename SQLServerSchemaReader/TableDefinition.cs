namespace SQLServerSchemaReader;

public class TableDefinition
{
    public TableDefinition()
    {
        ObjectType = ObjectType.Table;
        Columns = new List<ColumnDefinition>();
        ForeignKeys = new List<ForeignKeyDefinition>();
    }

    public ObjectType ObjectType { get; set; }
    public string Schema { get; set; }
    public string Name { get; set; }
    public List<ColumnDefinition> Columns { get; set; }
    public List<ForeignKeyDefinition> ForeignKeys { get; set; }
}