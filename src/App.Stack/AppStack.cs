using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using AppStack.Constructs;
using Constructs;

namespace AppStack;

public class AppStack : Stack
{
  private const string TableName = "what-did-i-do";

  internal AppStack(Construct scope, string id, IStackProps props)
    : base(scope, id, props)
  {
    // Event Functions
    HandleEventFunctions();
  }

  private void HandleEventFunctions()
  {
    var scheduleEventNotificationsFunction = new AppFunction(this, "ScheduleEventNotifications", new AppFunction.Props(
      "ScheduleEventNotifications::App.Events.ScheduleEventNotifications.Function::Handler",
      TableName
    ));

    var streamArn = Fn.ImportValue("what-did-i-do-domain-stack-ApplicationTableStreamArn");

    scheduleEventNotificationsFunction.AddEventSourceMapping("DynamoEventStream", new EventSourceMappingOptions
    {
      EventSourceArn = streamArn,
      StartingPosition = StartingPosition.TRIM_HORIZON,
      Enabled = true,
      BatchSize = 10,
      MaxBatchingWindow = Duration.Minutes(5),
      RetryAttempts = 3,
    });
    scheduleEventNotificationsFunction.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
    {
      Effect = Effect.ALLOW,
      Actions = new[] {
        "dynamodb:DescribeStream",
        "dynamodb:GetRecords",
        "dynamodb:GetShardIterator",
        "dynamodb:ListStreams",
      },
      Resources = new[] { streamArn },
    }));
    scheduleEventNotificationsFunction.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
    {
      Effect = Effect.ALLOW,
      Actions = new string[] { "dynamodb:PutItem" },
      Resources = new string[] { $"arn:aws:dynamodb:{Region}:{Account}:table/{TableName}" },
    }));
  }
}
