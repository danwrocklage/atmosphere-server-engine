using ACore.Application;

var builder = await CellBuilder.Create();
await using var app = builder.Build();
await app.Run();