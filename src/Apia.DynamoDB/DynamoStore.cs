using System.Linq.Expressions;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using OneOf;

namespace Apia.DynamoDB;

/// <summary>
/// DynamoDB-backed entity store. Each item is stored with PK, SK, a _type discriminator,
/// a _data JSON blob for deserialization, and every entity field as an individual top-level
/// attribute so that DynamoDB FilterExpressions can reference them server-side.
/// </summary>
public sealed class DynamoStore<T>(
    IAmazonDynamoDB client,
    string tableName,
    Func<T, string> pk,
    Func<T, string> sk,
    string pkAttribute = "PK",
    string skAttribute = "SK")
    : IEntityStore<T>
{
    private const string TypeAttr = "_type";
    private const string DataAttr = "_data";
    private static readonly string TypeName = typeof(T).Name;

    public async Task<OneOf<T, NotFound>> Get(string id)
    {
        var (pkVal, skVal) = Split(id);
        var response = await client.GetItemAsync(new GetItemRequest
        {
            TableName = tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [pkAttribute] = new AttributeValue { S = pkVal },
                [skAttribute] = new AttributeValue { S = skVal }
            }
        });

        return response.Item.Count == 0
            ? new NotFound()
            : JsonSerializer.Deserialize<T>(response.Item[DataAttr].S)!;
    }

    public async IAsyncEnumerable<T> All()
    {
        Dictionary<string, AttributeValue>? lastKey = null;
        do
        {
            var request = new ScanRequest
            {
                TableName = tableName,
                FilterExpression = "#sysType = :sysType",
                ExpressionAttributeNames  = new Dictionary<string, string>   { ["#sysType"] = TypeAttr },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":sysType"] = new AttributeValue { S = TypeName } }
            };
            if (lastKey is not null)
                request.ExclusiveStartKey = lastKey;

            var response = await client.ScanAsync(request);
            foreach (var item in response.Items)
                yield return JsonSerializer.Deserialize<T>(item[DataAttr].S)!;

            lastKey = response.LastEvaluatedKey.Count > 0 ? response.LastEvaluatedKey : null;
        }
        while (lastKey is not null);
    }

    public async Task Set(T entity)
    {
        var json  = JsonSerializer.Serialize(entity);
        var item  = new Dictionary<string, AttributeValue>
        {
            [pkAttribute] = new AttributeValue { S = pk(entity) },
            [skAttribute] = new AttributeValue { S = sk(entity) },
            [TypeAttr]    = new AttributeValue { S = TypeName },
            [DataAttr]    = new AttributeValue { S = json }
        };

        using var doc = JsonDocument.Parse(json);
        foreach (var prop in doc.RootElement.EnumerateObject())
            item[prop.Name] = JsonElementToAttribute(prop.Value);

        await client.PutItemAsync(new PutItemRequest { TableName = tableName, Item = item });
    }

    public async Task Remove(string id)
    {
        var (pkVal, skVal) = Split(id);
        await client.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [pkAttribute] = new AttributeValue { S = pkVal },
                [skAttribute] = new AttributeValue { S = skVal }
            }
        });
    }

    internal IAsyncEnumerable<T> AllFiltered(Expression<Func<T, bool>> predicate)
    {
        var (userFilter, userNames, userValues) = new DynamoFilterTranslator().Translate(predicate.Body);
        var names  = new Dictionary<string, string>(userNames)   { ["#sysType"] = TypeAttr };
        var values = new Dictionary<string, AttributeValue>(userValues) { [":sysType"] = new AttributeValue { S = TypeName } };
        return AllFiltered($"#sysType = :sysType AND ({userFilter})", names, values);
    }

    private async IAsyncEnumerable<T> AllFiltered(
        string filterExpression,
        Dictionary<string, string> attrNames,
        Dictionary<string, AttributeValue> attrValues)
    {
        Dictionary<string, AttributeValue>? lastKey = null;
        do
        {
            var request = new ScanRequest
            {
                TableName = tableName,
                FilterExpression          = filterExpression,
                ExpressionAttributeNames  = attrNames,
                ExpressionAttributeValues = attrValues
            };
            if (lastKey is not null)
                request.ExclusiveStartKey = lastKey;

            var response = await client.ScanAsync(request);
            foreach (var item in response.Items)
                yield return JsonSerializer.Deserialize<T>(item[DataAttr].S)!;

            lastKey = response.LastEvaluatedKey.Count > 0 ? response.LastEvaluatedKey : null;
        }
        while (lastKey is not null);
    }

    private static (string pk, string sk) Split(string id)
    {
        var sep = id.IndexOf(DynamoIdentity<T>.Separator);
        return sep < 0
            ? throw new FormatException(
                $"Invalid DynamoDB composite id '{id}': missing separator. Use DynamoIdentity<T> to produce ids.")
            : (id[..sep], id[(sep + 1)..]);
    }

    private static AttributeValue JsonElementToAttribute(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String                  => new AttributeValue { S    = el.GetString() ?? "" },
        JsonValueKind.Number                  => new AttributeValue { N    = el.GetRawText() },
        JsonValueKind.True or JsonValueKind.False => new AttributeValue { BOOL = el.GetBoolean() },
        JsonValueKind.Null                    => new AttributeValue { NULL = true },
        JsonValueKind.Array                   => new AttributeValue { L    = el.EnumerateArray().Select(JsonElementToAttribute).ToList() },
        JsonValueKind.Object                  => new AttributeValue { M    = el.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToAttribute(p.Value)) },
        _                                     => new AttributeValue { S    = el.GetRawText() }
    };
}
