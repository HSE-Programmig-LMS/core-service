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

public sealed class CreateUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenValidationFails_ShouldReturnValidationError()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();
        var roles = Substitute.For<IRoleRepository>();
        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new CreateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(new CreateUserRequest(
            Email: "",
            Password: "",
            FirstName: "",
            LastName: "",
            Role: ""));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        roles.DidNotReceiveWithAnyArgs().NormalizeRoleCode(default);
        await roles.DidNotReceiveWithAnyArgs().ExistsAsync(default!, default);
        await users.DidNotReceiveWithAnyArgs().EmailExistsAsync(default!, default);
        await users.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
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

        var sut = new CreateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(new CreateUserRequest(
            Email: "a@b.com",
            Password: "Password1",
            FirstName: "A",
            LastName: "B",
            Role: "???"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.RoleNotFound, result.Error!.Code);

        await roles.DidNotReceiveWithAnyArgs().ExistsAsync(default!, default);
        await users.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleDoesNotExist_ShouldReturnRoleNotFound()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("Manager").Returns("manager");
        roles.ExistsAsync("manager", Arg.Any<CancellationToken>()).Returns(false);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new CreateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(new CreateUserRequest(
            Email: "a@b.com",
            Password: "Password1",
            FirstName: "A",
            LastName: "B",
            Role: "Manager"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.RoleNotFound, result.Error!.Code);

        await users.DidNotReceiveWithAnyArgs().EmailExistsAsync(default!, default);
        await users.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailAlreadyExists_ShouldReturnEmailAlreadyExists()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();
        users.EmailExistsAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(true);

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("manager").Returns("manager");
        roles.ExistsAsync("manager", Arg.Any<CancellationToken>()).Returns(true);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new CreateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(new CreateUserRequest(
            Email: "a@b.com",
            Password: "Password1",
            FirstName: "A",
            LastName: "B",
            Role: "manager"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.EmailAlreadyExists, result.Error!.Code);

        await users.Received(1).EmailExistsAsync("a@b.com", Arg.Any<CancellationToken>());
        await users.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await users.DidNotReceiveWithAnyArgs().SetUserRoleAsync(default, default!, default);
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSetUserRoleReturnsFalse_ShouldReturnInternalError()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("manager").Returns("manager");
        roles.ExistsAsync("manager", Arg.Any<CancellationToken>()).Returns(true);

        var users = Substitute.For<IUserRepository>();
        users.EmailExistsAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(false);

        users.CreateAsync(Arg.Any<CreateUserData>(), Arg.Any<CancellationToken>())
            .Returns(new UserDto(
                UserId: userId,
                Email: "a@b.com",
                FirstName: "A",
                LastName: "B",
                IsActive: true,
                Role: "",
                CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
                LastLoginAtUtc: null));

        users.SetUserRoleAsync(userId, "manager", Arg.Any<CancellationToken>())
            .Returns(false);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();
        ctx.UserId.Returns(actorId);

        var sut = new CreateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(new CreateUserRequest(
            Email: "a@b.com",
            Password: "Password1",
            FirstName: "A",
            LastName: "B",
            Role: "manager"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InternalError, result.Error!.Code);

        await users.Received(1).CreateAsync(Arg.Any<CreateUserData>(), Arg.Any<CancellationToken>());
        await users.Received(1).SetUserRoleAsync(userId, "manager", Arg.Any<CancellationToken>());

        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenReloadReturnsNull_ShouldReturnInternalError()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("manager").Returns("manager");
        roles.ExistsAsync("manager", Arg.Any<CancellationToken>()).Returns(true);

        var users = Substitute.For<IUserRepository>();
        users.EmailExistsAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(false);

        users.CreateAsync(Arg.Any<CreateUserData>(), Arg.Any<CancellationToken>())
            .Returns(new UserDto(
                UserId: userId,
                Email: "a@b.com",
                FirstName: "A",
                LastName: "B",
                IsActive: true,
                Role: "",
                CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
                LastLoginAtUtc: null));

        users.SetUserRoleAsync(userId, "manager", Arg.Any<CancellationToken>())
            .Returns(true);

        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((UserDto?)null);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();
        ctx.UserId.Returns(actorId);

        var sut = new CreateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(new CreateUserRequest(
            Email: "a@b.com",
            Password: "Password1",
            FirstName: "A",
            LastName: "B",
            Role: "manager"));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InternalError, result.Error!.Code);

        await users.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccess_ShouldCreateAssignRoleReload_AndWriteTwoAuditEvents()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var roles = Substitute.For<IRoleRepository>();
        roles.NormalizeRoleCode("Manager").Returns("manager");
        roles.ExistsAsync("manager", Arg.Any<CancellationToken>()).Returns(true);

        var users = Substitute.For<IUserRepository>();
        users.EmailExistsAsync("a@b.com", Arg.Any<CancellationToken>()).Returns(false);

        users.CreateAsync(Arg.Any<CreateUserData>(), Arg.Any<CancellationToken>())
            .Returns(new UserDto(
                UserId: userId,
                Email: "a@b.com",
                FirstName: "A",
                LastName: "B",
                IsActive: true,
                Role: "",
                CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
                LastLoginAtUtc: null));

        users.SetUserRoleAsync(userId, "manager", Arg.Any<CancellationToken>())
            .Returns(true);

        var finalUser = new UserDto(
            UserId: userId,
            Email: "a@b.com",
            FirstName: "A",
            LastName: "B",
            IsActive: true,
            Role: "manager",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
            LastLoginAtUtc: null);

        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(finalUser);

        var ctx = Substitute.For<IUserContext>();
        ctx.UserId.Returns(actorId);

        var audit = Substitute.For<IAuditWriter>();
        var auditEntries = new List<AuditWriteEntry>();

        audit.When(x => x.WriteAsync(Arg.Any<AuditWriteEntry>(), Arg.Any<CancellationToken>()))
             .Do(ci => auditEntries.Add(ci.ArgAt<AuditWriteEntry>(0)));

        var sut = new CreateUserUseCase(users, roles, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(new CreateUserRequest(
            Email: "a@b.com",
            Password: "Password1",
            FirstName: "A",
            LastName: "B",
            Role: "Manager"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(finalUser, result.Value);

        await users.Received(1).CreateAsync(
            Arg.Is<CreateUserData>(d =>
                d.Email == "a@b.com" &&
                d.Password == "Password1" &&
                d.FirstName == "A" &&
                d.LastName == "B" &&
                d.IsActive == true),
            Arg.Any<CancellationToken>());

        await users.Received(1).SetUserRoleAsync(userId, "manager", Arg.Any<CancellationToken>());
        await users.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());

        await audit.Received(2).WriteAsync(Arg.Any<AuditWriteEntry>(), Arg.Any<CancellationToken>());
        Assert.Equal(2, auditEntries.Count);

        // core.user.created
        Assert.Equal(AuditEventTypes.CoreUserCreated, auditEntries[0].EventType);
        Assert.Equal(actorId, auditEntries[0].ActorUserId);
        Assert.Equal(AuditEntityTypes.User, auditEntries[0].EntityType);
        Assert.Equal(userId, auditEntries[0].EntityId);
        Assert.Contains("a@b.com", auditEntries[0].DetailsJson);

        // core.user.role.changed
        Assert.Equal(AuditEventTypes.CoreUserRoleChanged, auditEntries[1].EventType);
        Assert.Equal(actorId, auditEntries[1].ActorUserId);
        Assert.Equal(AuditEntityTypes.User, auditEntries[1].EntityType);
        Assert.Equal(userId, auditEntries[1].EntityId);
        Assert.Contains("manager", auditEntries[1].DetailsJson);
    }
}
