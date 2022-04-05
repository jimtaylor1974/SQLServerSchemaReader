namespace SQLServerSchemaReader;

public static class DatabaseSchemaUtility
{
    private const string userDefinedTableTypesQuery = @"SELECT s.name AS [Schema]
	,t.name AS [Name]
	,t.user_type_id AS [UserTypeId]
FROM SYS.TABLE_TYPES t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.IS_USER_DEFINED = 1
    AND (@SchemaName IS NULL OR s.name = @SchemaName)
ORDER BY t.name ASC";

    private const string userDefinedTableTypeColumnsQuery = @"SELECT 
	   table_type.name AS [ObjectName],
	   COL.name AS [FieldName],
       ST.name AS [DatabaseType],
       COL.[precision] AS [NumericPrecision],
       COL.scale AS [NumericScale],
       COL.Is_Nullable AS [IsNullable],
       COL.column_id AS [Position],
       CAST(COL.max_length AS INT) AS [MaxLength]
FROM sys.table_types table_type
JOIN sys.columns     COL
    ON table_type.type_table_object_id = COL.object_id
JOIN sys.systypes AS ST  
    ON ST.xtype = COL.system_type_id
where table_type.is_user_defined = 1 AND table_type.user_type_id = @UserTypeId
ORDER BY table_type.name,
         COL.column_id";

    private const string storedProceduresQuery = @"SELECT s.name AS [Schema]
	,o.name AS [Name]
	,o.object_id AS [ObjectId]
FROM sys.all_objects o
INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
WHERE o.type = 'P' -- procedures
	AND s.name <> 'sys'
    AND (@SchemaName IS NULL OR s.name = @SchemaName)
ORDER BY s.name ASC";

    private const string storedProcedureParametersQuery = @"
select 
parameters.name AS [Name]
,parameters.is_nullable AS [IsNullable]
,parameters.is_output AS [IsOutput]
,parameters.is_readonly AS [IsReadonly]
,parameters.is_xml_document AS [IsXmlDocument]
,types.name AS [Type]
,types.max_length AS [Length]
from sys.parameters
inner join sys.procedures on parameters.object_id = procedures.object_id 
inner join sys.types on parameters.system_type_id = types.system_type_id AND parameters.user_type_id = types.user_type_id
where procedures.object_id = @ObjectId
order by parameters.parameter_id";

    private const string objectsQuery = @"select 
    s.name as [Schema], 
    o.type_desc as [Type],
    o.name as [Name] 
from
    sys.all_objects o
    inner join sys.schemas s on s.schema_id = o.schema_id 
where
    o.type in ('U', 'V') -- tables and views
	and s.name <> 'sys'
	and s.name <> 'INFORMATION_SCHEMA'
	and s.name <> 'VersionInfo'
	and s.name <> 'sysdiagrams'
    and (@SchemaName IS NULL OR s.name = @SchemaName)
order by
    s.name";

    private const string tableColumnsQuery = @"SELECT DISTINCT @ObjectName AS [ObjectName]
	,sys.columns.NAME AS [FieldName]
	,sys.types.NAME AS [DatabaseType]
	,sys.columns.precision AS [NumericPrecision]
	,sys.columns.scale AS [NumericScale]
	,sys.columns.is_nullable AS [IsNullable]
	,(
		SELECT COUNT(column_name)
		FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE
		WHERE TABLE_NAME = sys.tables.NAME
			AND CONSTRAINT_NAME = (
				SELECT constraint_name
				FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
				WHERE TABLE_NAME = sys.tables.NAME
					AND constraint_type = 'PRIMARY KEY'
					AND COLUMN_NAME = sys.columns.NAME
				)
		) AS IsPrimaryKey
	,COLUMNPROPERTY(object_id(@ObjectName), sys.columns.NAME, 'IsIdentity') AS [IsIdentity]
	,(
		SELECT ORDINAL_POSITION
		FROM INFORMATION_SCHEMA.COLUMNS
		WHERE TABLE_NAME = @ObjectName
			AND TABLE_SCHEMA = @Schema
			AND COLUMN_NAME = sys.columns.NAME
		) AS [Position]
	,(
		SELECT COLUMN_DEFAULT
		FROM INFORMATION_SCHEMA.COLUMNS
		WHERE TABLE_NAME = @ObjectName
			AND TABLE_SCHEMA = @Schema
			AND COLUMN_NAME = sys.columns.NAME
		) AS [DefaultValue]
	,(
		SELECT CHARACTER_MAXIMUM_LENGTH
		FROM INFORMATION_SCHEMA.COLUMNS
		WHERE TABLE_NAME = @ObjectName
			AND TABLE_SCHEMA = @Schema
			AND COLUMN_NAME = sys.columns.NAME
		) AS [MaxLength]
	,(
		SELECT sik.keyno
		FROM sysobjects so
		INNER JOIN sysindexes si ON so.id = si.id
		INNER JOIN sysindexkeys sik ON so.id = sik.id
			AND si.indid = sik.indid
		INNER JOIN syscolumns sc ON so.id = sc.id
			AND sik.colid = sc.colid
		WHERE so.xtype = 'u'
			AND (si.STATUS & 32) = 0
			AND (si.STATUS & 2048) = 2048
			AND so.NAME = @ObjectName
			AND sc.NAME = sys.columns.NAME
		) AS [KeyNo]
FROM sys.columns
	,sys.types
	,sys.tables
WHERE sys.tables.object_id = sys.columns.object_id
	AND sys.types.system_type_id = sys.columns.system_type_id
	AND sys.types.user_type_id = sys.columns.user_type_id
	AND sys.tables.NAME = @ObjectName
	AND sys.tables.schema_id = SCHEMA_ID (@Schema)
ORDER BY [Position]";

    private const string viewColumnsQuery = @"SELECT 
 @ObjectName AS [ObjectName]
	,sys.columns.NAME AS [FieldName]
	,sys.types.NAME AS [DatabaseType]
	,sys.columns.precision AS [NumericPrecision]
	,sys.columns.scale AS [NumericScale]
	,sys.columns.is_nullable AS [IsNullable]
	,0 AS IsPrimaryKey
	,COLUMNPROPERTY(object_id(@ObjectName), sys.columns.NAME, 'IsIdentity') AS [IsIdentity]
	,(
		SELECT ORDINAL_POSITION
		FROM INFORMATION_SCHEMA.COLUMNS
		WHERE TABLE_NAME = @ObjectName
			AND TABLE_SCHEMA = @Schema
			AND COLUMN_NAME = sys.columns.NAME
		) AS [Position]
	,(
		SELECT COLUMN_DEFAULT
		FROM INFORMATION_SCHEMA.COLUMNS
		WHERE TABLE_NAME = @ObjectName
			AND TABLE_SCHEMA = @Schema
			AND COLUMN_NAME = sys.columns.NAME
		) AS [DefaultValue]
	,(
		SELECT CHARACTER_MAXIMUM_LENGTH
		FROM INFORMATION_SCHEMA.COLUMNS
		WHERE TABLE_NAME = @ObjectName
			AND TABLE_SCHEMA = @Schema
			AND COLUMN_NAME = sys.columns.NAME
		) AS [MaxLength]
	,(
		SELECT sik.keyno
		FROM sysobjects so
		INNER JOIN sysindexes si ON so.id = si.id
		INNER JOIN sysindexkeys sik ON so.id = sik.id
			AND si.indid = sik.indid
		INNER JOIN syscolumns sc ON so.id = sc.id
			AND sik.colid = sc.colid
		WHERE so.xtype = 'u'
			AND (si.STATUS & 32) = 0
			AND (si.STATUS & 2048) = 2048
			AND so.NAME = @ObjectName
			AND sc.NAME = sys.columns.NAME
		) AS [KeyNo]
FROM sys.columns
	,sys.types
,sys.views v
   WHERE sys.columns.object_id = v.object_id
   AND v.[name] = @ObjectName
	AND sys.types.user_type_id = sys.columns.user_type_id";

    private const string foreignKeyQuery = @"SELECT oParent.NAME [ParentTableName]
	,oParentColDtl.TABLE_SCHEMA AS [ParentTableSchema]
	,oParentCol.NAME [ParentColumnName]
	,oReference.NAME [ReferenceTableName]
	,refSchema.name as [ReferenceTableSchema]
	,oReferenceCol.NAME [ReferenceColumnName]
FROM sys.foreign_key_columns FKC
INNER JOIN sys.sysobjects oConstraint ON FKC.constraint_object_id = oConstraint.id
INNER JOIN sys.sysobjects oParent ON FKC.parent_object_id = oParent.id
INNER JOIN sys.all_columns oParentCol ON FKC.parent_object_id = oParentCol.object_id /* ID of the object to which this column belongs.*/
	AND FKC.parent_column_id = oParentCol.column_id /* ID of the column. Is unique within the object.Column IDs might not be sequential.*/
INNER JOIN sys.sysobjects oReference ON FKC.referenced_object_id = oReference.id
INNER JOIN INFORMATION_SCHEMA.COLUMNS oParentColDtl ON oParentColDtl.TABLE_NAME = oParent.NAME
	AND oParentColDtl.COLUMN_NAME = oParentCol.NAME
INNER JOIN sys.all_columns oReferenceCol ON FKC.referenced_object_id = oReferenceCol.object_id /* ID of the object to which this column belongs.*/
	AND FKC.referenced_column_id = oReferenceCol.column_id /* ID of the column. Is unique within the object.Column IDs might not be sequential.*/
INNER JOIN sys.all_objects o on o.object_id = oReference.id
    INNER JOIN sys.schemas refSchema on refSchema.schema_id = o.schema_id 
WHERE oParent.NAME = @ObjectName AND oParentColDtl.TABLE_SCHEMA = @Schema";

    private const string userDefinedTypesQuery = @"select distinct
	user_defined_type.name AS [UserDefinedDatabaseType]
	,t.name AS [DatabaseType]
	,t.precision AS [NumericPrecision]
	,t.scale AS [NumericScale]
from sys.types user_defined_type
inner join sys.types t on t.system_type_id = user_defined_type.system_type_id
where user_defined_type.is_user_defined = 1 and t.is_user_defined = 0 and t.name <> 'sysname'";

    public static DataSourceSchemaInfo ReadSchema(string connectionString, string? schemaName = null)
    {
        var databaseSchemaInfo = new DataSourceSchemaInfo();

        var userDefinedTypes = QueryUtility
            .Query(connectionString, userDefinedTypesQuery)
            .Select(QueryUtility.ConvertTo<UserDefinedType>)
            .ToDictionary(key => key.UserDefinedDatabaseType, value => value);

        databaseSchemaInfo.UserDefinedTypes.AddRange(userDefinedTypes.Values);

        var userDefinedTableTypes = QueryUtility.Query(connectionString, userDefinedTableTypesQuery, new
        {
            SchemaName = string.IsNullOrWhiteSpace(schemaName) ? null : schemaName
        })
            .Select(QueryUtility.ConvertTo<UserDefinedTableTypeObject>)
            .ToArray();

        foreach (var typeObject in userDefinedTableTypes)
        {
            var userDefinedTableType = new UserDefinedTableType
            {
                Schema = typeObject.Schema,
                Name = typeObject.Name
            };

            var userDefinedTableTypeColumns = QueryUtility.Query(connectionString, userDefinedTableTypeColumnsQuery, new
            {
                typeObject.UserTypeId
            })
                .Select(QueryUtility.ConvertTo<DbColumn>)
                .ToArray();

            var columnDefinitions = GetColumnDefinitions(userDefinedTableTypeColumns, userDefinedTypes);

            userDefinedTableType.Columns.AddRange(columnDefinitions);

            databaseSchemaInfo.UserDefinedTableTypes.Add(userDefinedTableType);
        }

        var storedProcedures = QueryUtility.Query(connectionString, storedProceduresQuery, new
        {
            SchemaName = string.IsNullOrWhiteSpace(schemaName) ? null : schemaName
        })
            .Select(QueryUtility.ConvertTo<StoredProcedureObject>)
            .ToArray();

        foreach (var sp in storedProcedures)
        {
            var diagramProcedures = new[]
            {
                    "sp_upgraddiagrams",
                    "sp_helpdiagrams",
                    "sp_helpdiagramdefinition",
                    "sp_creatediagram",
                    "sp_renamediagram",
                    "sp_alterdiagram",
                    "sp_dropdiagram"
                };

            if (sp.Schema == "dbo" && diagramProcedures.Contains(sp.Name))
            {
                continue;
            }

            var parameters = QueryUtility.Query(connectionString, storedProcedureParametersQuery, new
            {
                sp.ObjectId
            })
                .Select(QueryUtility.ConvertTo<StoredProcedureParameterObject>)
                .ToArray();

            var storedProcedure = new StoredProcedure
            {
                Schema = sp.Schema,
                Name = sp.Name
            };

            foreach (var p in parameters)
            {
                string databaseType;
                string typeSchema = null;

                if (userDefinedTypes.ContainsKey(p.Type))
                {
                    dynamic userDefinedType = userDefinedTypes[p.Type];
                    databaseType = userDefinedType.DatabaseType;
                }
                else if (TypeUtility.TryGetColumnType(p.Type, out ColumnType columnType))
                {
                    databaseType = columnType.ToString();
                }
                else
                {
                    databaseType = p.Type;

                    var userDefinedTableType = databaseSchemaInfo.UserDefinedTableTypes.FirstOrDefault(udtt =>
                        udtt.Name.Equals(p.Type, StringComparison.OrdinalIgnoreCase));

                    if (userDefinedTableType != null)
                    {
                        typeSchema = userDefinedTableType.Schema;
                    }
                }

                var parameter = new StoredProcedureParameter
                {
                    Name = p.Name.TrimStart('@'),
                    Length = p.Length,
                    Type = databaseType,
                    TypeSchema = typeSchema,
                    IsNullable = p.IsNullable,
                    IsOutput = p.IsOutput,
                    IsReadonly = p.IsReadonly,
                    IsXmlDocument = p.IsXmlDocument
                };

                storedProcedure.Parameters.Add(parameter);
            }

            databaseSchemaInfo.StoredProcedures.Add(storedProcedure);
        }

        var objects = QueryUtility.Query(connectionString, objectsQuery, new
        {
            SchemaName = string.IsNullOrWhiteSpace(schemaName) ? null : schemaName
        })
            .Select(QueryUtility.ConvertTo<DbObject>)
            .ToArray();

        foreach (var dbObject in objects)
        {
            string schema = dbObject.Schema;
            string type = dbObject.Type;
            string name = dbObject.Name;

            ObjectType objectType;

            switch (type)
            {
                case "USER_TABLE":
                    objectType = ObjectType.Table;
                    break;
                case "VIEW":
                    objectType = ObjectType.View;
                    break;
                default:
                    throw new NotSupportedException($"ObjectType of {type} is not supported.");
            }

            var tableDefinition = new TableDefinition
            {
                Schema = schema,
                ObjectType = objectType,
                Name = name
            };

            databaseSchemaInfo.TableDefinitions.Add(tableDefinition);

            var columnsQuery = objectType == ObjectType.Table
                ? tableColumnsQuery
                : viewColumnsQuery;

            var columns = QueryUtility.Query(connectionString, columnsQuery, new
            {
                ObjectName = name,
                Schema = schema
            }).Select(QueryUtility.ConvertTo<DbColumn>).ToArray();

            var columnDefinitions = GetColumnDefinitions(columns, userDefinedTypes);

            tableDefinition.Columns.AddRange(columnDefinitions);
        }

        foreach (var dbObject in objects)
        {
            var entityForeignKeys = QueryUtility.Query(
                    connectionString,
                    foreignKeyQuery,
                    new
                    {
                        ObjectName = dbObject.Name, // leftEntity
                        dbObject.Schema
                    })
                .Select(QueryUtility.ConvertTo<DbForeignKey>)
                .ToArray();

            foreach (var entityForeignKey in entityForeignKeys)
            {
                var foreignKey = new ForeignKeyDefinition
                {
                    PrimaryKeyColumn = entityForeignKey.ReferenceColumnName,
                    ForeignKeyColumn = entityForeignKey.ParentColumnName,
                    ForeignKeyTable = entityForeignKey.ParentTableName,
                    ForeignKeyTableSchema = entityForeignKey.ParentTableSchema
                };

                var foreignTableDefinition =
                    databaseSchemaInfo.TableDefinitions.Single(td =>
                        td.Schema == entityForeignKey.ReferenceTableSchema &&
                        td.Name == entityForeignKey.ReferenceTableName);

                foreignTableDefinition.ForeignKeys.Add(foreignKey);
            }
        }

        return databaseSchemaInfo;
    }

    private static List<ColumnDefinition> GetColumnDefinitions(DbColumn[] columns, Dictionary<string, UserDefinedType> userDefinedTypes)
    {
        var columnDefinitions = new List<ColumnDefinition>();
        foreach (var column in columns)
        {
            bool isPrimaryKey = column.IsPrimaryKey == 1;
            bool isIdentity = column.IsIdentity == 1;
            string databaseType = column.DatabaseType;
            int numericPrecision = column.NumericPrecision;
            int numericScale = column.NumericScale;

            if (userDefinedTypes.ContainsKey(column.DatabaseType))
            {
                dynamic userDefinedType = userDefinedTypes[column.DatabaseType];

                databaseType = userDefinedType.DatabaseType;
                numericPrecision = column.NumericPrecision;
                numericScale = column.NumericScale;
            }

            if (!TypeUtility.TryGetColumnType(databaseType, out ColumnType columnType))
            {
                continue;
            }

            var defaultValue = column.DefaultValue;
            var withDefault = ParseSystemMethodFromDefaultValue(defaultValue);
            if (withDefault != null)
            {
                defaultValue = null;
            }

            var columnDefinition = new ColumnDefinition
            {
                Identity = isIdentity,
                PrimaryKey = isPrimaryKey,
                Name = column.FieldName,
                Precision = numericPrecision,
                Size = column.MaxLength,
                Type = columnType.ToString(),
                Nullable = column.IsNullable,
                DefaultValue = defaultValue,
                WithDefault = withDefault
            };

            columnDefinitions.Add(columnDefinition);
        }

        return columnDefinitions;
    }

    private static SystemMethods? ParseSystemMethodFromDefaultValue(string defaultValue)
    {
        if (defaultValue == null)
        {
            return null;
        }

        var defaultValueToSystemMethod = new Dictionary<string, SystemMethods>(StringComparer.OrdinalIgnoreCase)
        {
            ["(newid())"] = SystemMethods.NewGuid,
            ["(newsequentialid())"] = SystemMethods.NewSequentialId,
            ["(getdate())"] = SystemMethods.CurrentDateTime,
            ["(sysdatetimeoffset())"] = SystemMethods.CurrentDateTimeOffset,
            ["(getutcdate())"] = SystemMethods.CurrentUTCDateTime,
            ["(user_name())"] = SystemMethods.CurrentUser
        };

        if (defaultValueToSystemMethod.ContainsKey(defaultValue))
        {
            return defaultValueToSystemMethod[defaultValue];
        }

        return null;
    }
}