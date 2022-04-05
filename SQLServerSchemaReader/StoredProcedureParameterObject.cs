namespace SQLServerSchemaReader;

public class StoredProcedureParameterObject
{
    public string Name { get; set; }
    public string Type { get; set; }
    public int Length { get; set; }
    public bool IsNullable { get; set; }
    public bool IsOutput { get; set; }
    public bool IsReadonly { get; set; }
    public bool IsXmlDocument { get; set; }
}