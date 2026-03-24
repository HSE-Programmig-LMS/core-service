using CoreService.Application.Abstractions.Audit;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Contracts.Users;
using CoreService.Application.UseCases.Users;
using CoreService.Domain.Audit;
using NSubstitute;
using Xunit;

namespace CoreService.Tests.Unit.Users;

public sealed class ChangeUserRoleUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRoleMissing_ShouldReturnValidationError()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();
        var roles = Substitute.For<IRoleRepository>();
        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new ChangeUserRoleUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(Guid.NewGuid(), new ChangeUserRoleRequest(""));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        roles.DidNotReceiveWithAnyArgs().NormalizeRoleCode(default);
        await roles.DidNotReceiveWithAnyArgs().ExistsAsync(default!, default);
        await users.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await users.DidNotReceiveWithAnyArgs().SetUserRoleAsync(default, default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleNormalizeReturnsNull_ShouldReturnRoleNotFound()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();
        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("???").Returns((string?)null);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new ChangeUserRoleUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(Guid.NewGuid(), new ChangeUserRoleRequest("???"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.RoleNotFound, result.Error!.Code);

        await roles.DidNotReceiveWithAnyArgs().ExistsAsync(default!, default);
        await users.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleDoesNotExist_ShouldReturnRoleNotFound()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();
        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("Teacher").Returns("teacher");
        roles.ExistsAsync("teacher", Arg.Any<CancellationToken>()).Returns(false);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new ChangeUserRoleUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(Guid.NewGuid(), new ChangeUserRoleRequest("Teacher"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.RoleNotFound, result.Error!.Code);

        await users.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await users.DidNotReceiveWithAnyArgs().SetUserRoleAsync(default, default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldReturnUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserDto?)null);

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("manager").Returns("manager");
        roles.ExistsAsync("manager", Arg.Any<CancellationToken>()).Returns(true);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new ChangeUserRoleUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new ChangeUserRoleRequest("manager"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.UserNotFound, result.Error!.Code);

        await users.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());
        await users.DidNotReceiveWithAnyArgs().SetUserRoleAsync(default, default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsSameAsCurrent_ShouldReturnOk_AndDoNothing()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var current = new UserDto(
            UserId: userId,
            Email: "u@x.com",
            FirstName: "A",
            LastName: "B",
            IsActive: true,
            Role: "teacher",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            LastLoginAtUtc: null);

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(current);

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("Teacher").Returns("teacher");
        roles.ExistsAsync("teacher", Arg.Any<CancellationToken>()).Returns(true);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new ChangeUserRoleUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new ChangeUserRoleRequest("Teacher"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(current, result.Value);

        await users.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());
        await users.DidNotReceiveWithAnyArgs().SetUserRoleAsync(default, default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSetUserRoleReturnsFalse_ShouldReturnUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var current = new UserDto(
            UserId: userId,
            Email: "u@x.com",
            FirstName: "A",
            LastName: "B",
            IsActive: true,
            Role: "assistant",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            LastLoginAtUtc: null);

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(current);
        users.SetUserRoleAsync(userId, "teacher", Arg.Any<CancellationToken>()).Returns(false);

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("teacher").Returns("teacher");
        roles.ExistsAsync("teacher", Arg.Any<CancellationToken>()).Returns(true);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new ChangeUserRoleUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new ChangeUserRoleRequest("teacher"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.UserNotFound, result.Error!.Code);

        await users.Received(1).SetUserRoleAsync(userId, "teacher", Arg.Any<CancellationToken>());
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccess_ShouldSetRole_ReloadUser_AndWriteAudit()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var before = new UserDto(
            UserId: userId,
            Email: "u@x.com",
            FirstName: "A",
            LastName: "B",
            IsActive: true,
            Role: "assistant",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            LastLoginAtUtc: null);

        var after = before with { Role = "teacher" };

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
             .Returns(before, after);

        users.SetUserRoleAsync(userId, "teacher", Arg.Any<CancellationToken>())
             .Returns(true);

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("Teacher").Returns("teacher");
        roles.ExistsAsync("teacher", Arg.Any<CancellationToken>()).Returns(true);

        var audit = Substitute.For<IAuditWriter>();
        AuditWriteEntry? captured = null;

        audit.WriteAsync(Arg.Any<AuditWriteEntry>(), Arg.Any<CancellationToken>())
             .Returns(Task.CompletedTask)
             .AndDoes(call => captured = call.ArgAt<AuditWriteEntry>(0));

        var ctx = Substitute.For<IUserContext>();
        ctx.UserId.Returns(actorId);

        var sut = new ChangeUserRoleUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new ChangeUserRoleRequest("Teacher"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("teacher", result.Value!.Role);

        await users.Received(1).SetUserRoleAsync(userId, "teacher", Arg.Any<CancellationToken>());
        await users.Received(2).GetByIdAsync(userId, Arg.Any<CancellationToken>());

        await audit.Received(1).WriteAsync(Arg.Any<AuditWriteEntry>(), Arg.Any<CancellationToken>());
        Assert.NotNull(captured);

        Assert.Equal(AuditEventTypes.CoreUserRoleChanged, captured!.EventType);
        Assert.Equal(actorId, captured.ActorUserId);
        Assert.Equal(AuditEntityTypes.User, captured.EntityType);
        Assert.Equal(userId, captured.EntityId);

        Assert.Contains("assistant", captured.DetailsJson);
        Assert.Contains("teacher", captured.DetailsJson);
    }
}
