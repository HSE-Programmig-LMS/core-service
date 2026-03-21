using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Common.Errors;
using CoreService.Application.Contracts.Auth;
using CoreService.Application.UseCases.Auth;
using NSubstitute;
using Xunit;

namespace CoreService.Tests.Unit.Auth;

public sealed class ResetPasswordUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEmailMissing_ShouldReturnValidationError()
    {
        // Arrange
        var svc = Substitute.For<IPasswordResetService>();
        var sut = new ResetPasswordUseCase(svc);

        // Act
        var result = await sut.ExecuteAsync(new ResetPasswordRequest(
            Email: "",
            Token: "token",
            NewPassword: "NewPassword1"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await svc.DidNotReceiveWithAnyArgs()
            .ResetPasswordAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenMissing_ShouldReturnValidationError()
    {
        // Arrange
        var svc = Substitute.For<IPasswordResetService>();
        var sut = new ResetPasswordUseCase(svc);

        // Act
        var result = await sut.ExecuteAsync(new ResetPasswordRequest(
            Email: "a@b.com",
            Token: "",
            NewPassword: "NewPassword1"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await svc.DidNotReceiveWithAnyArgs()
            .ResetPasswordAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNewPasswordMissing_ShouldReturnValidationError()
    {
        // Arrange
        var svc = Substitute.For<IPasswordResetService>();
        var sut = new ResetPasswordUseCase(svc);

        // Act
        var result = await sut.ExecuteAsync(new ResetPasswordRequest(
            Email: "a@b.com",
            Token: "token",
            NewPassword: ""));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await svc.DidNotReceiveWithAnyArgs()
            .ResetPasswordAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenResetFails_ShouldReturnInvalidResetToken()
    {
        // Arrange
        var svc = Substitute.For<IPasswordResetService>();
        svc.ResetPasswordAsync("a@b.com", "token", "NewPassword1", Arg.Any<CancellationToken>())
            .Returns(new PasswordResetResult(
                Succeeded: false,
                FailureCode: ErrorCodes.InvalidResetToken,
                FailureMessage: "Invalid token"));

        var sut = new ResetPasswordUseCase(svc);

        // Act
        var result = await sut.ExecuteAsync(new ResetPasswordRequest(
            Email: "a@b.com",
            Token: "token",
            NewPassword: "NewPassword1"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidResetToken, result.Error!.Code);

        await svc.Received(1).ResetPasswordAsync("a@b.com", "token", "NewPassword1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenResetSucceeds_ShouldReturnOkTrue()
    {
        // Arrange
        var svc = Substitute.For<IPasswordResetService>();
        svc.ResetPasswordAsync("a@b.com", "token", "NewPassword1", Arg.Any<CancellationToken>())
            .Returns(new PasswordResetResult(Succeeded: true));

        var sut = new ResetPasswordUseCase(svc);

        // Act
        var result = await sut.ExecuteAsync(new ResetPasswordRequest(
            Email: "a@b.com",
            Token: "token",
            NewPassword: "NewPassword1"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        await svc.Received(1).ResetPasswordAsync("a@b.com", "token", "NewPassword1", Arg.Any<CancellationToken>());
    }
}