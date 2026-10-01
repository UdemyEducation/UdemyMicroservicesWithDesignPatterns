var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Stock_API>("stock-api");

builder.AddProject<Projects.SagaStateMachineWorkerService>("sagastatemachineworkerservice");

builder.AddProject<Projects.Payment_API>("payment-api");

builder.AddProject<Projects.Order_API>("order-api");

builder.Build().Run();
