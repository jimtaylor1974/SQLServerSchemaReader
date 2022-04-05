namespace SQLServerSchemaReader;

public class DataSourceSchemaInfo
{
    public DataSourceSchemaInfo()
    {
        TableDefinitions = new List<TableDefinition>();
        UserDefinedTypes = new List<UserDefinedType>();
        UserDefinedTableTypes = new List<UserDefinedTableType>();
        StoredProcedures = new List<StoredProcedure>();
    }

    public List<TableDefinition> TableDefinitions { get; set; }
    public List<UserDefinedType> UserDefinedTypes { get; set; }
    public List<UserDefinedTableType> UserDefinedTableTypes { get; set; }
    public List<StoredProcedure> StoredProcedures { get; set; }
}