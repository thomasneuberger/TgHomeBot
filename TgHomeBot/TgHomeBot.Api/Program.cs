using TgHomeBot.Api.Options;
using TghomeBot.SmartHome.HomeAssistant;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddHomeAssistant(builder.Configuration);

builder.Services.AddOptions<SmartHomeOptions>().Configure(options => builder.Configuration.GetSection("SmartHome").Bind(options));

builder.Services.AddHttpClient();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();