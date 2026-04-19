using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using OneOf;

namespace Apia.DynamoDB;

/// <summary>
/// DynamoDB-backed entity store. Each item is stored with configurable PK and SK attributes,
/// a _type discriminator for single-table design, and _data containing the JSON-serialized entity.
/// The string id used by IEntityStore is the composite PK + unit-separator + SK,
/// produced by DynamoIdentity.
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
                FilterExpression = "#t = :type",
                ExpressionAttributeNames  = new Dictionary<string, string> { ["#t"] = TypeAttr },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":type"] = new AttributeValue { S = TypeName }
                }
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
        await client.PutItemAsync(new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                [pkAttribute] = new AttributeValue { S = pk(entity) },
                [skAttribute] = new AttributeValue { S = sk(entity) },
                [TypeAttr]    = new AttributeValue { S = TypeName },
                [DataAttr]    = new AttributeValue { S = JsonSerializer.Serialize(entity) }
            }
        });
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

    private static (string pk, string sk) Split(string id)
    {
        var sep = id.IndexOf(DynamoIdentity<T>.Separator);
        return sep < 0
            ? throw new FormatException(
                $"Invalid DynamoDB composite id '{id}': expected a '{(int)DynamoIdentity<T>.Separator}' separator. Use DynamoIdentity<T> to produce ids.")
            : (id[..sep], id[(sep + 1)..]);
    }
}
