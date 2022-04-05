namespace SQLServerSchemaReader;

public class StoredProcedure
{
    public string Schema { get; set; }
    public string Name { get; set; }
    public string QualifiedName => $"{Schema}.{Name}";
    public List<StoredProcedureParameter> Parameters { get; set; } = new List<StoredProcedureParameter>();
}