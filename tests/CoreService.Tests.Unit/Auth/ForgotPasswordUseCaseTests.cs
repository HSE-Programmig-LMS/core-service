using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Common.Errors;
using CoreService.Application.Contracts.Auth;
using CoreService.Application.UseCases.Auth;
using NSubstitute;
using Xunit;

namespace CoreService.Tests.Unit.Auth;

public sealed class ForgotPasswordUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEmailMissing_ShouldReturnValidationError()
    {
        // Arrange
        var svc = Substitute.For<IPasswordResetService>();
        var sut = new ForgotPasswordUseCase(svc);

        // Act
        var result = await sut.ExecuteAsync(new ForgotPasswordRequest(""));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await svc.DidNotReceiveWithAnyArgs().GenerateResetTokenAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldStillReturnOkTrue()
    {
        // Arrange
        var svc = Substitute.For<IPasswordResetService>();
        svc.GenerateResetTokenAsync("x@y.com", Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var sut = new ForgotPasswordUseCase(svc);

        // Act
        var result = await sut.ExecuteAsync(new ForgotPasswordRequest("x@y.com"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        await svc.Received(1).GenerateResetTokenAsync("x@y.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenGenerated_ShouldReturnOkTrue()
    {
        // Arrange
        var svc = Substitute.For<IPasswordResetService>();
        svc.GenerateResetTokenAsync("x@y.com", Arg.Any<CancellationToken>())
            .Returns("RESET_TOKEN");

        var sut = new ForgotPasswordUseCase(svc);

        // Act
        var result = await sut.ExecuteAsync(new ForgotPasswordRequest("x@y.com"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        await svc.Received(1).GenerateResetTokenAsync("x@y.com", Arg.Any<CancellationToken>());
    }
}