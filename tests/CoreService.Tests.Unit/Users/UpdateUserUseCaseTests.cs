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

public sealed class UpdateUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNoFieldsProvided_ShouldReturnValidationError()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();
        var roles = Substitute.For<IRoleRepository>();
        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new UpdateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(Guid.NewGuid(), new UpdateUserRequest());

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await users.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        await users.DidNotReceiveWithAnyArgs().UpdateAsync(default, default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCurrentUserNotFound_ShouldReturnUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
             .Returns((UserDto?)null);

        var roles = Substitute.For<IRoleRepository>();
        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new UpdateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new UpdateUserRequest(FirstName: "New"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.UserNotFound, result.Error!.Code);

        await users.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());
        await users.DidNotReceiveWithAnyArgs().UpdateAsync(default, default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailChangedAndAlreadyExists_ShouldReturnEmailAlreadyExists()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var current = new UserDto(
            UserId: userId,
            Email: "old@x.com",
            FirstName: "A",
            LastName: "B",
            IsActive: true,
            Role: "manager",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-5),
            LastLoginAtUtc: null);

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(current);
        users.EmailExistsAsync("new@x.com", Arg.Any<CancellationToken>()).Returns(true);

        var roles = Substitute.For<IRoleRepository>();
        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new UpdateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new UpdateUserRequest(Email: "new@x.com"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.EmailAlreadyExists, result.Error!.Code);

        await users.Received(1).EmailExistsAsync("new@x.com", Arg.Any<CancellationToken>());
        await users.DidNotReceiveWithAnyArgs().UpdateAsync(default, default!, default);
        await users.DidNotReceiveWithAnyArgs().SetUserRoleAsync(default, default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleProvidedButNormalizeReturnsNull_ShouldReturnRoleNotFound()
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
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-5),
            LastLoginAtUtc: null);

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(current);

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("???").Returns((string?)null);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new UpdateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new UpdateUserRequest(Role: "???"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.RoleNotFound, result.Error!.Code);

        await users.DidNotReceiveWithAnyArgs().UpdateAsync(default, default!, default);
        await users.DidNotReceiveWithAnyArgs().SetUserRoleAsync(default, default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleProvidedButDoesNotExist_ShouldReturnRoleNotFound()
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
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-5),
            LastLoginAtUtc: null);

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(current);

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("Teacher").Returns("teacher");
        roles.ExistsAsync("teacher", Arg.Any<CancellationToken>()).Returns(false);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new UpdateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new UpdateUserRequest(Role: "Teacher"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.RoleNotFound, result.Error!.Code);

        await users.DidNotReceiveWithAnyArgs().UpdateAsync(default, default!, default);
        await users.DidNotReceiveWithAnyArgs().SetUserRoleAsync(default, default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUpdateReturnsNull_ShouldReturnUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var current = new UserDto(
            UserId: userId,
            Email: "u@x.com",
            FirstName: "A",
            LastName: "B",
            IsActive: true,
            Role: "manager",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-5),
            LastLoginAtUtc: null);

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(current);
        users.UpdateAsync(userId, Arg.Any<UpdateUserData>(), Arg.Any<CancellationToken>())
             .Returns((UserDto?)null);

        var roles = Substitute.For<IRoleRepository>();
        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new UpdateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new UpdateUserRequest(FirstName: "New"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.UserNotFound, result.Error!.Code);

        await users.Received(1).UpdateAsync(userId, Arg.Any<UpdateUserData>(), Arg.Any<CancellationToken>());
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleChangeAndSetRoleFails_ShouldReturnUserNotFound_AndNoAudit()
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
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-5),
            LastLoginAtUtc: null);

        var updatedFromUpdate = current with { FirstName = "New" }; // роль ещё старая, как часто бывает

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(current);
        users.UpdateAsync(userId, Arg.Any<UpdateUserData>(), Arg.Any<CancellationToken>())
             .Returns(updatedFromUpdate);

        users.SetUserRoleAsync(userId, "teacher", Arg.Any<CancellationToken>())
             .Returns(false);

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("Teacher").Returns("teacher");
        roles.ExistsAsync("teacher", Arg.Any<CancellationToken>()).Returns(true);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new UpdateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new UpdateUserRequest(FirstName: "New", Role: "Teacher"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.UserNotFound, result.Error!.Code);

        await users.Received(1).SetUserRoleAsync(userId, "teacher", Arg.Any<CancellationToken>());
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccessWithoutRoleChange_ShouldWriteOnlyUserUpdatedAudit()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var current = new UserDto(
            UserId: userId,
            Email: "u@x.com",
            FirstName: "Old",
            LastName: "B",
            IsActive: true,
            Role: "manager",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-5),
            LastLoginAtUtc: null);

        var updatedFromUpdate = current with { FirstName = "New" };
        var finalReload = updatedFromUpdate; // без смены роли

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
             .Returns(current, finalReload);

        users.UpdateAsync(userId, Arg.Any<UpdateUserData>(), Arg.Any<CancellationToken>())
             .Returns(updatedFromUpdate);

        var roles = Substitute.For<IRoleRepository>();
        var audit = Substitute.For<IAuditWriter>();

        var entries = new List<AuditWriteEntry>();
        audit.When(x => x.WriteAsync(Arg.Any<AuditWriteEntry>(), Arg.Any<CancellationToken>()))
             .Do(ci => entries.Add(ci.ArgAt<AuditWriteEntry>(0)));

        var ctx = Substitute.For<IUserContext>();
        ctx.UserId.Returns(actorId);

        var sut = new UpdateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new UpdateUserRequest(FirstName: "New"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(finalReload, result.Value);

        await users.Received(1).UpdateAsync(userId, Arg.Any<UpdateUserData>(), Arg.Any<CancellationToken>());
        await users.DidNotReceiveWithAnyArgs().SetUserRoleAsync(default, default!, default);

        await audit.Received(1).WriteAsync(Arg.Any<AuditWriteEntry>(), Arg.Any<CancellationToken>());
        Assert.Single(entries);

        Assert.Equal(AuditEventTypes.CoreUserUpdated, entries[0].EventType);
        Assert.Equal(actorId, entries[0].ActorUserId);
        Assert.Equal(AuditEntityTypes.User, entries[0].EntityType);
        Assert.Equal(userId, entries[0].EntityId);
        Assert.Contains("Old", entries[0].DetailsJson);
        Assert.Contains("New", entries[0].DetailsJson);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccessWithRoleChange_ShouldWriteRoleChangedThenUserUpdatedAudit()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var current = new UserDto(
            UserId: userId,
            Email: "u@x.com",
            FirstName: "Old",
            LastName: "B",
            IsActive: true,
            Role: "assistant",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-5),
            LastLoginAtUtc: null);

        var updatedFromUpdate = current with { FirstName = "New" };
        var finalReload = updatedFromUpdate with { Role = "teacher" };

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
             .Returns(current, finalReload);

        users.UpdateAsync(userId, Arg.Any<UpdateUserData>(), Arg.Any<CancellationToken>())
             .Returns(updatedFromUpdate);

        users.SetUserRoleAsync(userId, "teacher", Arg.Any<CancellationToken>())
             .Returns(true);

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("Teacher").Returns("teacher");
        roles.ExistsAsync("teacher", Arg.Any<CancellationToken>()).Returns(true);

        var audit = Substitute.For<IAuditWriter>();
        var entries = new List<AuditWriteEntry>();
        audit.When(x => x.WriteAsync(Arg.Any<AuditWriteEntry>(), Arg.Any<CancellationToken>()))
             .Do(ci => entries.Add(ci.ArgAt<AuditWriteEntry>(0)));

        var ctx = Substitute.For<IUserContext>();
        ctx.UserId.Returns(actorId);

        var sut = new UpdateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new UpdateUserRequest(FirstName: "New", Role: "Teacher"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(finalReload, result.Value);

        await users.Received(1).SetUserRoleAsync(userId, "teacher", Arg.Any<CancellationToken>());
        await audit.Received(2).WriteAsync(Arg.Any<AuditWriteEntry>(), Arg.Any<CancellationToken>());
        Assert.Equal(2, entries.Count);

        Assert.Equal(AuditEventTypes.CoreUserRoleChanged, entries[0].EventType);
        Assert.Equal(AuditEventTypes.CoreUserUpdated, entries[1].EventType);

        Assert.Equal(actorId, entries[0].ActorUserId);
        Assert.Equal(actorId, entries[1].ActorUserId);

        Assert.Equal(AuditEntityTypes.User, entries[0].EntityType);
        Assert.Equal(AuditEntityTypes.User, entries[1].EntityType);

        Assert.Equal(userId, entries[0].EntityId);
        Assert.Equal(userId, entries[1].EntityId);

        Assert.Contains("assistant", entries[0].DetailsJson);
        Assert.Contains("teacher", entries[0].DetailsJson);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReloadAfterUpdateReturnsNull_ShouldReturnInternalError()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var current = new UserDto(
            UserId: userId,
            Email: "u@x.com",
            FirstName: "Old",
            LastName: "B",
            IsActive: true,
            Role: "manager",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-5),
            LastLoginAtUtc: null);

        var updatedFromUpdate = current with { FirstName = "New" };

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
             .Returns(current, (UserDto?)null);

        users.UpdateAsync(userId, Arg.Any<UpdateUserData>(), Arg.Any<CancellationToken>())
             .Returns(updatedFromUpdate);

        var roles = Substitute.For<IRoleRepository>();
        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();
        ctx.UserId.Returns(actorId);

        var sut = new UpdateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId, new UpdateUserRequest(FirstName: "New"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InternalError, result.Error!.Code);

        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }
}
