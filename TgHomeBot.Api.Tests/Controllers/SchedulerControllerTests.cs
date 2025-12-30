using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TgHomeBot.Api.Controllers;
using TgHomeBot.Api.Models;
using TgHomeBot.Scheduling.Contract;
using TgHomeBot.Scheduling.Contract.Models;

namespace TgHomeBot.Api.Tests.Controllers;

[TestFixture]
public class SchedulerControllerTests
{
    private ILogger<SchedulerController> _logger = null!;
    private ISchedulerService _schedulerService = null!;
    private SchedulerController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<SchedulerController>>();
        _schedulerService = Substitute.For<ISchedulerService>();
        _controller = new SchedulerController(_logger, _schedulerService);
    }

    [Test]
    public void GetScheduledTasks_ShouldReturnTasks()
    {
        // Arrange
        var tasks = new List<ScheduledTaskInfo>
        {
            new() { TaskType = "Task1", TaskName = "Task One", CronExpression = "0 0 * * *", Enabled = true },
            new() { TaskType = "Task2", TaskName = "Task Two", CronExpression = "0 12 * * *", Enabled = false }
        };
        _schedulerService.GetScheduledTasks().Returns(tasks);

        // Act
        var result = _controller.GetScheduledTasks();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returnedTasks = okResult!.Value as List<ScheduledTaskInfo>;
        Assert.That(returnedTasks, Is.Not.Null);
        Assert.That(returnedTasks!.Count, Is.EqualTo(2));
    }

    [Test]
    public void GetScheduledTasks_ShouldReturnEmptyList_WhenNoTasks()
    {
        // Arrange
        _schedulerService.GetScheduledTasks().Returns(new List<ScheduledTaskInfo>());

        // Act
        var result = _controller.GetScheduledTasks();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var returnedTasks = okResult!.Value as List<ScheduledTaskInfo>;
        Assert.That(returnedTasks, Is.Empty);
    }

    [Test]
    public async Task RunTaskNow_ShouldReturnOk_WhenTaskRunsSuccessfully()
    {
        // Arrange
        var request = new RunTaskRequest { TaskType = "TestTask" };
        _schedulerService.RunTaskNowAsync("TestTask").Returns(Task.FromResult(true));

        // Act
        var result = await _controller.RunTaskNow(request);

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        await _schedulerService.Received(1).RunTaskNowAsync("TestTask");
    }

    [Test]
    public async Task RunTaskNow_ShouldReturnBadRequest_WhenTaskFails()
    {
        // Arrange
        var request = new RunTaskRequest { TaskType = "FailingTask" };
        _schedulerService.RunTaskNowAsync("FailingTask").Returns(Task.FromResult(false));

        // Act
        var result = await _controller.RunTaskNow(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        await _schedulerService.Received(1).RunTaskNowAsync("FailingTask");
    }

    [Test]
    public async Task RunTaskNow_ShouldReturnBadRequest_WhenTaskTypeIsNull()
    {
        // Arrange
        var request = new RunTaskRequest { TaskType = null! };

        // Act
        var result = await _controller.RunTaskNow(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        await _schedulerService.DidNotReceive().RunTaskNowAsync(Arg.Any<string>());
    }

    [Test]
    public async Task RunTaskNow_ShouldReturnBadRequest_WhenTaskTypeIsEmpty()
    {
        // Arrange
        var request = new RunTaskRequest { TaskType = "" };

        // Act
        var result = await _controller.RunTaskNow(request);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        await _schedulerService.DidNotReceive().RunTaskNowAsync(Arg.Any<string>());
    }

    [Test]
    public async Task RunTaskNow_ShouldReturnBadRequest_WhenRequestIsNull()
    {
        // Act
        var result = await _controller.RunTaskNow(null!);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        await _schedulerService.DidNotReceive().RunTaskNowAsync(Arg.Any<string>());
    }
}
