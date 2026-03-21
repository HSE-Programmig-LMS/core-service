using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Common.Errors;
using CoreService.Application.Contracts.Auth;
using CoreService.Application.UseCases.Auth;
using CoreService.Tests.Unit.TestDoubles;
using NSubstitute;
using Xunit;

namespace CoreService.Tests.Unit.Auth;

public sealed class LoginUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEmailMissing_ShouldReturnValidationError()
    {
        // Arrange
        var verifier = Substitute.For<IPasswordVerifier>();
        var jwt = Substitute.For<IJwtTokenService>();
        var store = Substitute.For<IRefreshTokenStore>();
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));

        var sut = new LoginUseCase(verifier, jwt, store, clock, TimeSpan.FromDays(30));

        // Act
        var result = await sut.ExecuteAsync(new LoginRequest("", "pass"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await verifier.DidNotReceiveWithAnyArgs().VerifyAsync(default!, default!, default);
        await jwt.DidNotReceiveWithAnyArgs().CreateAccessTokenAsync(default, default!, default!, default);
        await store.DidNotReceiveWithAnyArgs().StoreAsync(default, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordMissing_ShouldReturnValidationError()
    {
        // Arrange
        var verifier = Substitute.For<IPasswordVerifier>();
        var jwt = Substitute.For<IJwtTokenService>();
        var store = Substitute.For<IRefreshTokenStore>();
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));

        var sut = new LoginUseCase(verifier, jwt, store, clock, TimeSpan.FromDays(30));

        // Act
        var result = await sut.ExecuteAsync(new LoginRequest("a@b.com", ""));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await verifier.DidNotReceiveWithAnyArgs().VerifyAsync(default!, default!, default);
        await jwt.DidNotReceiveWithAnyArgs().CreateAccessTokenAsync(default, default!, default!, default);
        await store.DidNotReceiveWithAnyArgs().StoreAsync(default, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvalidCredentials_ShouldReturnInvalidCredentials()
    {
        // Arrange
        var verifier = Substitute.For<IPasswordVerifier>();
        verifier.VerifyAsync("a@b.com", "bad", Arg.Any<CancellationToken>())
            .Returns(new PasswordVerificationResult(
                IsValid: false,
                UserId: null,
                Email: null,
                RoleCode: null,
                FirstName: null,
                LastName: null,
                FailureCode: ErrorCodes.InvalidCredentials));

        var jwt = Substitute.For<IJwtTokenService>();
        var store = Substitute.For<IRefreshTokenStore>();
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));

        var sut = new LoginUseCase(verifier, jwt, store, clock, TimeSpan.FromDays(30));

        // Act
        var result = await sut.ExecuteAsync(new LoginRequest("a@b.com", "bad"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidCredentials, result.Error!.Code);

        await jwt.DidNotReceiveWithAnyArgs().CreateAccessTokenAsync(default, default!, default!, default);
        await store.DidNotReceiveWithAnyArgs().StoreAsync(default, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLockedOut_ShouldReturnLockedOut()
    {
        // Arrange
        var verifier = Substitute.For<IPasswordVerifier>();
        verifier.VerifyAsync("a@b.com", "pass", Arg.Any<CancellationToken>())
            .Returns(new PasswordVerificationResult(
                IsValid: false,
                UserId: Guid.NewGuid(),
                Email: "a@b.com",
                RoleCode: null,
                FirstName: null,
                LastName: null,
                FailureCode: ErrorCodes.LockedOut));

        var jwt = Substitute.For<IJwtTokenService>();
        var store = Substitute.For<IRefreshTokenStore>();
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));

        var sut = new LoginUseCase(verifier, jwt, store, clock, TimeSpan.FromDays(30));

        // Act
        var result = await sut.ExecuteAsync(new LoginRequest("a@b.com", "pass"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.LockedOut, result.Error!.Code);

        await jwt.DidNotReceiveWithAnyArgs().CreateAccessTokenAsync(default, default!, default!, default);
        await store.DidNotReceiveWithAnyArgs().StoreAsync(default, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserInactive_ShouldReturnUserInactive()
    {
        // Arrange
        var verifier = Substitute.For<IPasswordVerifier>();
        verifier.VerifyAsync("a@b.com", "pass", Arg.Any<CancellationToken>())
            .Returns(new PasswordVerificationResult(
                IsValid: false,
                UserId: Guid.NewGuid(),
                Email: "a@b.com",
                RoleCode: null,
                FirstName: null,
                LastName: null,
                FailureCode: ErrorCodes.UserInactive));

        var jwt = Substitute.For<IJwtTokenService>();
        var store = Substitute.For<IRefreshTokenStore>();
        var clock = new FakeClock(new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero));

        var sut = new LoginUseCase(verifier, jwt, store, clock, TimeSpan.FromDays(30));

        // Act
        var result = await sut.ExecuteAsync(new LoginRequest("a@b.com", "pass"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.UserInactive, result.Error!.Code);

        await jwt.DidNotReceiveWithAnyArgs().CreateAccessTokenAsync(default, default!, default!, default);
        await store.DidNotReceiveWithAnyArgs().StoreAsync(default, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccess_ShouldReturnTokens_AndStoreRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "me@example.com";
        var role = "manager";

        var verifier = Substitute.For<IPasswordVerifier>();
        verifier.VerifyAsync(email, "pass", Arg.Any<CancellationToken>())
            .Returns(new PasswordVerificationResult(
                IsValid: true,
                UserId: userId,
                Email: email,
                RoleCode: role,
                FirstName: "Alex",
                LastName: "Ivanov",
                FailureCode: null));

        var accessExpires = new DateTimeOffset(2026, 3, 22, 12, 15, 0, TimeSpan.Zero);
        var jwt = Substitute.For<IJwtTokenService>();
        jwt.CreateAccessTokenAsync(userId, role, email, Arg.Any<CancellationToken>())
            .Returns(new AccessTokenResult("ACCESS.JWT", accessExpires));

        var store = Substitute.For<IRefreshTokenStore>();

        // capture args passed to StoreAsync
        string? storedRawRefresh = null;
        DateTimeOffset storedRefreshExp = default;
        Guid storedUserId = Guid.Empty;

        store.StoreAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(call =>
            {
                storedUserId = call.ArgAt<Guid>(0);
                storedRawRefresh = call.ArgAt<string>(1);
                storedRefreshExp = call.ArgAt<DateTimeOffset>(2);
            });

        var clockNow = new DateTimeOffset(2026, 3, 22, 12, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(clockNow);
        var refreshLifetime = TimeSpan.FromDays(30);

        var sut = new LoginUseCase(verifier, jwt, store, clock, refreshLifetime);

        // Act
        var result = await sut.ExecuteAsync(new LoginRequest(email, "pass"));

        // Assert
        Assert.True(result.IsSuccess);
        var resp = result.Value!;
        Assert.Equal(userId, resp.UserId);
        Assert.Equal(email, resp.Email);
        Assert.Equal(role, resp.Role);

        Assert.Equal("ACCESS.JWT", resp.AccessToken);
        Assert.Equal(accessExpires, resp.AccessTokenExpiresAtUtc);

        Assert.False(string.IsNullOrWhiteSpace(resp.RefreshToken));
        Assert.Equal(clockNow.Add(refreshLifetime), resp.RefreshTokenExpiresAtUtc);

        // Verify JWT creation
        await jwt.Received(1).CreateAccessTokenAsync(userId, role, email, Arg.Any<CancellationToken>());

        // Verify refresh token storage called and matches returned token
        await store.Received(1).StoreAsync(userId, Arg.Any<string>(), clockNow.Add(refreshLifetime), Arg.Any<CancellationToken>());
        Assert.Equal(userId, storedUserId);
        Assert.Equal(resp.RefreshToken, storedRawRefresh);
        Assert.Equal(resp.RefreshTokenExpiresAtUtc, storedRefreshExp);
    }
}