using Newtonsoft.Json;
using SQLServerSchemaReader;

Console.WriteLine("Enter connection string:");
var connectionString = Console.ReadLine();
var schema = DatabaseSchemaUtility.ReadSchema(connectionString!);

var serialized = JsonConvert.SerializeObject(schema);

var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Schema.txt");

File.WriteAllText(path, serialized);

Console.WriteLine("DONE");

Console.ReadLine();