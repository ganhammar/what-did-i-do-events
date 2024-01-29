﻿using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Constructs;

namespace AppStack.Constructs;

public class AppFunction : Function
{
  public AppFunction(Construct scope, string id, Props props)
    : base(scope, $"{id}Function", new FunctionProps
    {
      Runtime = Runtime.PROVIDED_AL2023,
      Architecture = Architecture.X86_64,
      Handler = props.Handler,
      Code = Code.FromAsset($"./.output/{id}.zip"),
      Timeout = Duration.Minutes(1),
      MemorySize = props.MemorySize,
      LogGroup = new LogGroup(scope, $"{id}LogGroup", new LogGroupProps
      {
        LogGroupName = $"/aws/lambda/{id}",
        Retention = RetentionDays.ONE_DAY,
        RemovalPolicy = RemovalPolicy.DESTROY,
      }),
      Tracing = Tracing.ACTIVE,
      Environment = new Dictionary<string, string>
      {
        { "TABLE_NAME", props.TableName ?? "" },
      },
    })
  {
  }

  public class Props
  {
    public Props(string handler, string? tableName = default, int memorySize = 1769)
    {
      Handler = handler;
      TableName = tableName;
      MemorySize = memorySize;
    }

    public string Handler { get; set; }
    public string? TableName { get; set; }
    public int MemorySize { get; set; }
  }
}
