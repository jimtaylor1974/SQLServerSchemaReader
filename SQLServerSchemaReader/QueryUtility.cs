using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;

namespace SQLServerSchemaReader;

public static class QueryUtility
{
    public static async Task<IEnumerable<dynamic>> QueryAsync(
        string connectionString,
        string sql,
        object? parameters = null)
    {
        var dynamicParameters = GetParameters(parameters);

        await using var connection = new SqlConnection(connectionString);
        connection.Open();
        return await connection.QueryAsync(sql, dynamicParameters);
    }

    public static IEnumerable<dynamic> Query(
        string connectionString,
        string sql,
        object? parameters = null)
    {
        var dynamicParameters = GetParameters(parameters);

        using var connection = new SqlConnection(connectionString);
        connection.Open();
        return connection.Query(sql, dynamicParameters);
    }

    public static TResult ConvertTo<TResult>(dynamic result)
    {
        IDictionary<string, object> data = (IDictionary<string, object>)result;
        var properties = typeof(TResult).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (IsSimpleType(typeof(TResult)))
        {
            return (TResult)data[data.Keys.First()];
        }

        var instance = Activator.CreateInstance<TResult>();

        foreach (var key in data.Keys)
        {
            var property = properties.FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase));

            if (property != null)
            {
                var value = GetValue(data[key]);

                property.SetValue(instance, value);
            }
        }

        return instance;
    }

    private static DynamicParameters GetParameters(object? parameters)
    {
        var dynamicParameters = parameters as DynamicParameters;

        if (dynamicParameters != null)
        {
            return dynamicParameters;
        }

        dynamicParameters = new DynamicParameters();

        var parametersDictionary = parameters as IDictionary<string, object>;

        if (parametersDictionary == null && parameters != null)
        {
            parametersDictionary = ObjectToDictionary(parameters);
        }

        if (parametersDictionary != null)
        {
            foreach (var parameter in parametersDictionary)
            {
                dynamicParameters.Add(parameter.Key, parameter.Value);
            }
        }

        return dynamicParameters;
    }

    private static Dictionary<string, object>? ObjectToDictionary(object? data, StringComparer? comparer = null)
    {
        if (data == null)
        {
            return null;
        }

        const BindingFlags publicAttributes = BindingFlags.Public | BindingFlags.Instance;

        return data
            .GetType()
            .GetProperties(publicAttributes)
            .Where(property => property.CanRead)
            .ToDictionary(property => property.Name, property => property.GetValue(data, null),
                comparer ?? StringComparer.OrdinalIgnoreCase)!;
    }

    private static object? GetValue(object value)
    {
        if (value == DBNull.Value)
        {
            return null;
        }

        return value;
    }

    private static bool IsSimpleType(Type type)
    {
        if (IsNullable(type))
        {
            // nullable type, check if the nested type is simple.
            return IsSimpleType(type.GetGenericArguments()[0]);
        }

        return type.IsSubclassOf(typeof(ValueType)) || type == typeof(string) || type == typeof(DateTime);
    }

    private static bool IsNullable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}