using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using AppStack.Constructs;
using Constructs;

namespace AppStack;

public class AppStack : Stack
{
  private const string TableName = "what-did-i-do";

  internal AppStack(Construct scope, string id, IStackProps props)
    : base(scope, id, props)
  {
    // DynamoDB
    var applicationTable = Table.FromTableAttributes(this, "ApplicationTable", new TableAttributes
    {
      TableArn = $"arn:aws:dynamodb:{Region}:{Account}:table/{TableName}",
      GrantIndexPermissions = true,
    });

    // Event Functions
    HandleEventFunctions(applicationTable);
  }

  private void HandleEventFunctions(
    ITable applicationTable)
  {
    var scheduleEventNotificationsFunction = new AppFunction(this, "ScheduleEventNotifications", new AppFunction.Props(
      "ScheduleEventNotifications::App.Events.ScheduleEventNotifications.Function::Handler",
      TableName
    ));

    scheduleEventNotificationsFunction.AddEventSource(new DynamoEventSource(applicationTable, new DynamoEventSourceProps
    {
      StartingPosition = StartingPosition.TRIM_HORIZON,
      Enabled = true,
      BatchSize = 10,
      MaxBatchingWindow = Duration.Minutes(5),
      RetryAttempts = 3,
    }));
    scheduleEventNotificationsFunction.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
    {
      Effect = Effect.ALLOW,
      Actions = new[] {
        "dynamodb:DescribeStream",
        "dynamodb:GetRecords",
        "dynamodb:GetShardIterator",
        "dynamodb:ListStreams",
      },
      Resources = new[] { "arn:aws:dynamodb:eu-north-1:519157272275:table/what-did-i-do/stream/2024-01-28T19:57:27.460" },
    }));
  }
}
