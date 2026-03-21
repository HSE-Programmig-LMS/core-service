using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Common.Errors;
using CoreService.Application.Contracts.Auth;
using CoreService.Application.UseCases.Auth;
using NSubstitute;
using Xunit;

namespace CoreService.Tests.Unit.Auth;

public sealed class LogoutUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenMissing_ShouldReturnValidationError()
    {
        // Arrange
        var store = Substitute.For<IRefreshTokenStore>();
        var sut = new LogoutUseCase(store);

        // Act
        var result = await sut.ExecuteAsync(new RefreshRequest(""));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await store.DidNotReceiveWithAnyArgs().RevokeAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenProvided_ShouldRevokeAndReturnOkTrue()
    {
        // Arrange
        var store = Substitute.For<IRefreshTokenStore>();
        var sut = new LogoutUseCase(store);

        // Act
        var result = await sut.ExecuteAsync(new RefreshRequest("some-refresh-token"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        await store.Received(1).RevokeAsync("some-refresh-token", Arg.Any<CancellationToken>());
    }
}