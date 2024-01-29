using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using static Amazon.Lambda.DynamoDBEvents.DynamoDBEvent;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace App.Events.ScheduleEventNotifications;

public class Function
{
  public static async Task Handler(DynamoDBEvent dynamoEvent)
  {
    foreach (var record in dynamoEvent.Records)
    {
      if (record.EventName == OperationType.INSERT)
      {
        await HandleInsertEvent(record);
      }
    }
  }

  private static async Task HandleInsertEvent(DynamodbStreamRecord record)
  {
    var newImage = record.Dynamodb.NewImage;
    newImage.TryGetValue("PartitionKey", out var partitionKey);

    if (partitionKey?.S?.StartsWith("EVENT#") == false)
    {
      return;
    }

    newImage.TryGetValue("SortKey", out var sortKey);

    if (sortKey?.S != default && DateTime.TryParse(sortKey.S, out var date))
    {
      if (date <= DateTime.UtcNow)
      {
        return;
      }

      var client = new AmazonDynamoDBClient();
      var request = new PutItemRequest
      {
        TableName = Environment.GetEnvironmentVariable("TableName"),
        Item = new()
        {
          { "PartitionKey", new($"NOTIFICATION#{date.ToUniversalTime()}") },
          { "SortKey", new(partitionKey!.S) },
        },
      };
      await client.PutItemAsync(request);
    }
  }
}
