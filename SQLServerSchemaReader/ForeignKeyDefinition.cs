namespace SQLServerSchemaReader;

public class ForeignKeyDefinition
{
    public string PrimaryKeyColumn { get; set; }
    public string ForeignKeyColumn { get; set; }
    public string ForeignKeyTable { get; set; }
    public string ForeignKeyTableSchema { get; set; }
}