using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Contracts.Users;
using CoreService.Application.UseCases.Auth;
using NSubstitute;
using Xunit;

namespace CoreService.Tests.Unit.Auth;

public sealed class GetMeUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(false);
        userContext.UserId.Returns((Guid?)null);

        var users = Substitute.For<IUserRepository>();

        var sut = new GetMeUseCase(userContext, users);

        // Act
        var result = await sut.ExecuteAsync();

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorCodes.Unauthorized, result.Error!.Code);

        await users.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAuthenticatedButUserIdMissing_ShouldReturnUnauthorized()
    {
        // Arrange
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns((Guid?)null);

        var users = Substitute.For<IUserRepository>();

        var sut = new GetMeUseCase(userContext, users);

        // Act
        var result = await sut.ExecuteAsync();

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorCodes.Unauthorized, result.Error!.Code);

        await users.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldReturnUserNotFound()
    {
        // Arrange
        var meId = Guid.NewGuid();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(meId);

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(meId, Arg.Any<CancellationToken>())
            .Returns((UserDto?)null);

        var sut = new GetMeUseCase(userContext, users);

        // Act
        var result = await sut.ExecuteAsync();

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorCodes.UserNotFound, result.Error!.Code);

        await users.Received(1).GetByIdAsync(meId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldReturnMeResponse()
    {
        // Arrange
        var meId = Guid.NewGuid();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(meId);

        var dto = new UserDto(
            UserId: meId,
            Email: "me@example.com",
            FirstName: "Alex",
            LastName: "Ivanov",
            IsActive: true,
            Role: "manager",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-10),
            LastLoginAtUtc: DateTimeOffset.UtcNow.AddMinutes(-5)
        );

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(meId, Arg.Any<CancellationToken>())
            .Returns(dto);

        var sut = new GetMeUseCase(userContext, users);

        // Act
        var result = await sut.ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var me = result.Value!;
        Assert.Equal(dto.UserId, me.UserId);
        Assert.Equal(dto.Email, me.Email);
        Assert.Equal(dto.Role, me.Role);
        Assert.Equal(dto.FirstName, me.FirstName);
        Assert.Equal(dto.LastName, me.LastName);

        await users.Received(1).GetByIdAsync(meId, Arg.Any<CancellationToken>());
    }
}