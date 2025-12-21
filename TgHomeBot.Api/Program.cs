using System.Text.Json.Serialization;
using Serilog;
using Serilog.Templates;
using Serilog.Templates.Themes;
using TgHomeBot.Api;
using TgHomeBot.Common.Contract;
using TgHomeBot.Notifications.Telegram;
using TgHomeBot.Scheduling;
using TgHomeBot.SmartHome.HomeAssistant;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSerilog((services, options) =>
{
    options
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console(new ExpressionTemplate(
            // Include trace and span ids when present.
            "[{@t:HH:mm:ss} {@l:u3}{#if @tr is not null} {SourceContext} ({substring(@tr,0,4)}:{substring(@sp,0,4)}){#end}] {@m}\n{@x}",
            theme: TemplateTheme.Code));
});

builder.Services.AddOptions<FileStorageOptions>().Configure(options => builder.Configuration.GetSection("FileStorage").Bind(options));
builder.Services.AddOptions<SerilogLogFileProvider.SerilogOptions>().Configure(options => builder.Configuration.GetSection("Serilog").Bind(options));

builder.Services.AddHomeAssistant(builder.Configuration);

builder.Services.AddOptions<SmartHomeOptions>().Configure(options => builder.Configuration.GetSection("SmartHome").Bind(options));
builder.Services.AddSingleton<IHostedService, MonitoringService>();
//builder.Services.AddSingleton<IHostedService, PollingService>();

builder.Services.AddSingleton<ILogFileProvider, SerilogLogFileProvider>();

builder.Services.AddTelegram(builder.Configuration);

builder.Services.AddSingleton<IHostedService, NotificationService>();

builder.Services.AddScheduling();

builder.Services.AddHttpClient();

builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();