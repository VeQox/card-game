using server.ExtensionMethods;
using server.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCorsPolicy();
builder.Services.AddSingleton(new RoomRepository());

var app = builder.Build();

app.UseCors("default");
app.UseWebSockets();
app.MapControllers();

app.Run();