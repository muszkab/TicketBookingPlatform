using Application.Common.Exceptions;
using Domain.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Infrastructure;

namespace WebApi.Tests.Unit.Infrastructure;

public class GlobalExceptionHandlerTests
{
    private readonly IProblemDetailsService _problemDetailsService = Substitute.For<IProblemDetailsService>();
    private ProblemDetailsContext? _capturedContext;

    private GlobalExceptionHandler CreateSut(string? environmentName = null, bool writeSucceeds = true)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName ?? Environments.Production);

        _problemDetailsService
            .TryWriteAsync(Arg.Any<ProblemDetailsContext>())
            .Returns(call =>
            {
                _capturedContext = call.Arg<ProblemDetailsContext>();
                return ValueTask.FromResult(writeSucceeds);
            });

        return new GlobalExceptionHandler(
            _problemDetailsService,
            NullLogger<GlobalExceptionHandler>.Instance,
            environment);
    }

    private static DefaultHttpContext CreateHttpContext()
        => new() { Response = { Body = new MemoryStream() } };

    private ProblemDetailsContext CapturedContext()
    {
        _capturedContext.Should().NotBeNull("the handler must delegate to IProblemDetailsService");
        return _capturedContext!;
    }

    private ProblemDetails CapturedProblem() => CapturedContext().ProblemDetails;

    [Theory]
    [InlineData(typeof(ArgumentException), StatusCodes.Status400BadRequest, "Invalid request")]
    [InlineData(typeof(NotFoundException), StatusCodes.Status404NotFound, "Resource not found")]
    [InlineData(typeof(ConflictException), StatusCodes.Status409Conflict, "Conflict")]
    [InlineData(typeof(BusinessRuleException), StatusCodes.Status409Conflict, "Business rule violation")]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status500InternalServerError, "An unexpected error occurred")]
    public async Task TryHandleAsync_Should_Map_Exception_To_ExpectedProblem(
        Type exceptionType, int expectedStatus, string expectedTitle)
    {
        GlobalExceptionHandler sut = CreateSut();
        DefaultHttpContext context = CreateHttpContext();
        Exception exception = CreateException(exceptionType);

        bool handled = await sut.TryHandleAsync(context, exception, CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(expectedStatus);

        ProblemDetails problem = CapturedProblem();
        problem.Status.Should().Be(expectedStatus);
        problem.Title.Should().Be(expectedTitle);
    }

    [Fact]
    public async Task TryHandleAsync_Should_Map_ArgumentNullException_As_BadRequest()
    {
        GlobalExceptionHandler sut = CreateSut();
        DefaultHttpContext context = CreateHttpContext();

        await sut.TryHandleAsync(context, new ArgumentNullException("param"), CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest,
            "ArgumentNullException derives from ArgumentException");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Expose_ExceptionMessage_For_HandledExceptions()
    {
        GlobalExceptionHandler sut = CreateSut();
        var exception = new ConflictException("Order already paid.");

        await sut.TryHandleAsync(CreateHttpContext(), exception, CancellationToken.None);

        CapturedProblem().Detail.Should().Be("Order already paid.");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Hide_Details_For_UnexpectedExceptions_InProduction()
    {
        GlobalExceptionHandler sut = CreateSut(Environments.Production);

        await sut.TryHandleAsync(
            CreateHttpContext(),
            new InvalidOperationException("secret internal detail"),
            CancellationToken.None);

        ProblemDetails problem = CapturedProblem();
        problem.Detail.Should().BeNull("internal details must not leak outside development");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Include_Details_For_UnexpectedExceptions_InDevelopment()
    {
        GlobalExceptionHandler sut = CreateSut(Environments.Development);

        await sut.TryHandleAsync(
            CreateHttpContext(),
            new InvalidOperationException("diagnostic detail"),
            CancellationToken.None);

        ProblemDetails problem = CapturedProblem();
        problem.Detail.Should().NotBeNull();
        problem.Detail.Should().Contain("diagnostic detail");
        problem.Detail.Should().Contain(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task TryHandleAsync_Should_Pass_HttpContextAndException_To_ProblemDetailsService()
    {
        GlobalExceptionHandler sut = CreateSut();
        DefaultHttpContext context = CreateHttpContext();
        var exception = new ConflictException("boom");

        await sut.TryHandleAsync(context, exception, CancellationToken.None);

        ProblemDetailsContext captured = CapturedContext();
        captured.HttpContext.Should().BeSameAs(context);
        captured.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task TryHandleAsync_Should_Return_False_When_RequestWasCancelled()
    {
        GlobalExceptionHandler sut = CreateSut();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        bool handled = await sut.TryHandleAsync(
            CreateHttpContext(), new OperationCanceledException(), cts.Token);

        handled.Should().BeFalse("a client abort must not be rewritten into a 500");
        await _problemDetailsService.DidNotReceive().TryWriteAsync(Arg.Any<ProblemDetailsContext>());
    }

    [Fact]
    public async Task TryHandleAsync_Should_Handle_OperationCanceledException_When_TokenNotCancelled()
    {
        GlobalExceptionHandler sut = CreateSut();
        DefaultHttpContext context = CreateHttpContext();

        bool handled = await sut.TryHandleAsync(
            context, new OperationCanceledException(), CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task TryHandleAsync_Should_Return_False_When_ResponseHasStarted()
    {
        GlobalExceptionHandler sut = CreateSut();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        bool handled = await sut.TryHandleAsync(
            context, new ConflictException("too late"), CancellationToken.None);

        handled.Should().BeFalse();
        await _problemDetailsService.DidNotReceive().TryWriteAsync(Arg.Any<ProblemDetailsContext>());
    }

    [Fact]
    public async Task TryHandleAsync_Should_Propagate_ProblemDetailsService_Result()
    {
        GlobalExceptionHandler sut = CreateSut(writeSucceeds: false);

        bool handled = await sut.TryHandleAsync(
            CreateHttpContext(), new ConflictException("boom"), CancellationToken.None);

        handled.Should().BeFalse("the handler must report failure if the response could not be written");
    }

    private static Exception CreateException(Type exceptionType) => exceptionType switch
    {
        _ when exceptionType == typeof(ArgumentException) => new ArgumentException("invalid argument"),
        _ when exceptionType == typeof(NotFoundException) => new NotFoundException("Event", Guid.NewGuid()),
        _ when exceptionType == typeof(ConflictException) => new ConflictException("conflicting state"),
        _ when exceptionType == typeof(BusinessRuleException) => new BusinessRuleException("rule violated"),
        _ => new InvalidOperationException("unexpected"),
    };

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => true;
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public string? ReasonPhrase { get; set; }
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public void OnCompleted(Func<object, Task> callback, object state) { }
        public void OnStarting(Func<object, Task> callback, object state) { }
    }
}
