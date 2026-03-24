using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Contracts.Auth;
using CoreService.Application.Contracts.Users;
using CoreService.Application.UseCases.Auth;
using CoreService.Tests.Unit.TestDoubles;
using NSubstitute;
using Xunit;

namespace CoreService.Tests.Unit.Auth;

public sealed class RefreshUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenMissing_ShouldReturnValidationError()
    {
        // Arrange
        var store = Substitute.For<IRefreshTokenStore>();
        var users = Substitute.For<IUserRepository>();
        var jwt = Substitute.For<IJwtTokenService>();
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));

        var sut = new RefreshUseCase(store, users, jwt, clock, TimeSpan.FromDays(30));

        // Act
        var result = await sut.ExecuteAsync(new RefreshRequest(""));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await store.DidNotReceiveWithAnyArgs().GetActiveAsync(default!, default);
        await jwt.DidNotReceiveWithAnyArgs().CreateAccessTokenAsync(default, default!, default!, default);
        await store.DidNotReceiveWithAnyArgs().RotateAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTokenNotActive_ShouldReturnInvalidRefreshToken()
    {
        // Arrange
        var store = Substitute.For<IRefreshTokenStore>();
        store.GetActiveAsync("old", Arg.Any<CancellationToken>())
            .Returns((ActiveRefreshToken?)null);

        var users = Substitute.For<IUserRepository>();
        var jwt = Substitute.For<IJwtTokenService>();
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));

        var sut = new RefreshUseCase(store, users, jwt, clock, TimeSpan.FromDays(30));

        // Act
        var result = await sut.ExecuteAsync(new RefreshRequest("old"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidRefreshToken, result.Error!.Code);

        await users.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await jwt.DidNotReceiveWithAnyArgs().CreateAccessTokenAsync(default, default!, default!, default);
        await store.DidNotReceiveWithAnyArgs().RotateAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldReturnUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var store = Substitute.For<IRefreshTokenStore>();
        store.GetActiveAsync("old", Arg.Any<CancellationToken>())
            .Returns(new ActiveRefreshToken(userId, DateTimeOffset.UtcNow.AddDays(1)));

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((UserDto?)null);

        var jwt = Substitute.For<IJwtTokenService>();
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));

        var sut = new RefreshUseCase(store, users, jwt, clock, TimeSpan.FromDays(30));

        // Act
        var result = await sut.ExecuteAsync(new RefreshRequest("old"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Unauthorized, result.Error!.Code);

        await jwt.DidNotReceiveWithAnyArgs().CreateAccessTokenAsync(default, default!, default!, default);
        await store.DidNotReceiveWithAnyArgs().RotateAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserInactive_ShouldReturnUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var store = Substitute.For<IRefreshTokenStore>();
        store.GetActiveAsync("old", Arg.Any<CancellationToken>())
            .Returns(new ActiveRefreshToken(userId, DateTimeOffset.UtcNow.AddDays(1)));

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserDto(
                UserId: userId,
                Email: "x@y.com",
                FirstName: "A",
                LastName: "B",
                IsActive: false,
                Role: "student",
                CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
                LastLoginAtUtc: null));

        var jwt = Substitute.For<IJwtTokenService>();
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));

        var sut = new RefreshUseCase(store, users, jwt, clock, TimeSpan.FromDays(30));

        // Act
        var result = await sut.ExecuteAsync(new RefreshRequest("old"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Unauthorized, result.Error!.Code);

        await jwt.DidNotReceiveWithAnyArgs().CreateAccessTokenAsync(default, default!, default!, default);
        await store.DidNotReceiveWithAnyArgs().RotateAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRotationFails_ShouldReturnInvalidRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var store = Substitute.For<IRefreshTokenStore>();
        store.GetActiveAsync("old", Arg.Any<CancellationToken>())
            .Returns(new ActiveRefreshToken(userId, DateTimeOffset.UtcNow.AddDays(1)));

        store.RotateAsync("old", Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserDto(
                UserId: userId,
                Email: "x@y.com",
                FirstName: "A",
                LastName: "B",
                IsActive: true,
                Role: "manager",
                CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
                LastLoginAtUtc: null));

        var jwt = Substitute.For<IJwtTokenService>();
        jwt.CreateAccessTokenAsync(userId, "manager", "x@y.com", Arg.Any<CancellationToken>())
            .Returns(new AccessTokenResult("ACCESS.JWT", DateTimeOffset.UtcNow.AddMinutes(15)));

        var clock = new FakeClock(new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));
        var sut = new RefreshUseCase(store, users, jwt, clock, TimeSpan.FromDays(30));

        // Act
        var result = await sut.ExecuteAsync(new RefreshRequest("old"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidRefreshToken, result.Error!.Code);

        await store.Received(1).RotateAsync("old", Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccess_ShouldRotateAndReturnNewTokens()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(now);
        var refreshLifetime = TimeSpan.FromDays(30);

        var userId = Guid.NewGuid();
        var email = "me@example.com";
        var role = "manager";

        var store = Substitute.For<IRefreshTokenStore>();
        store.GetActiveAsync("old", Arg.Any<CancellationToken>())
            .Returns(new ActiveRefreshToken(userId, now.AddDays(1)));

        string? newRawCaptured = null;
        DateTimeOffset newExpCaptured = default;

        store.RotateAsync("old", Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(true)
            .AndDoes(call =>
            {
                newRawCaptured = call.ArgAt<string>(1);
                newExpCaptured = call.ArgAt<DateTimeOffset>(2);
            });

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new UserDto(
                UserId: userId,
                Email: email,
                FirstName: "Alex",
                LastName: "Ivanov",
                IsActive: true,
                Role: role,
                CreatedAtUtc: now.AddDays(-10),
                LastLoginAtUtc: now.AddMinutes(-5)));

        var accessExp = now.AddMinutes(15);
        var jwt = Substitute.For<IJwtTokenService>();
        jwt.CreateAccessTokenAsync(userId, role, email, Arg.Any<CancellationToken>())
            .Returns(new AccessTokenResult("ACCESS.JWT", accessExp));

        var sut = new RefreshUseCase(store, users, jwt, clock, refreshLifetime);

        // Act
        var result = await sut.ExecuteAsync(new RefreshRequest("old"));

        // Assert
        Assert.True(result.IsSuccess);
        var resp = result.Value!;
        Assert.Equal("ACCESS.JWT", resp.AccessToken);
        Assert.Equal(accessExp, resp.AccessTokenExpiresAtUtc);

        Assert.False(string.IsNullOrWhiteSpace(resp.RefreshToken));
        Assert.NotEqual("old", resp.RefreshToken);

        Assert.Equal(now.Add(refreshLifetime), resp.RefreshTokenExpiresAtUtc);

        await store.Received(1).RotateAsync("old", Arg.Any<string>(), now.Add(refreshLifetime), Arg.Any<CancellationToken>());
        Assert.Equal(resp.RefreshToken, newRawCaptured);
        Assert.Equal(resp.RefreshTokenExpiresAtUtc, newExpCaptured);

        await jwt.Received(1).CreateAccessTokenAsync(userId, role, email, Arg.Any<CancellationToken>());
    }
}
