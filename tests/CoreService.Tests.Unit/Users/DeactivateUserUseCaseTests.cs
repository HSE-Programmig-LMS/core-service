using CoreService.Application.Abstractions.Audit;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.UseCases.Users;
using CoreService.Domain.Audit;
using NSubstitute;
using Xunit;

namespace CoreService.Tests.Unit.Users;

public sealed class DeactivateUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldReturnUserNotFound()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();
        users.DeactivateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns(false);

        var audit = Substitute.For<IAuditWriter>();
        var ctx = Substitute.For<IUserContext>();

        var sut = new DeactivateUserUseCase(users, audit, ctx);

        var userId = Guid.NewGuid();

        // Act
        var result = await sut.ExecuteAsync(userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.UserNotFound, result.Error!.Code);

        await users.Received(1).DeactivateAsync(userId, Arg.Any<CancellationToken>());
        await audit.DidNotReceiveWithAnyArgs().WriteAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSuccess_ShouldReturnOkTrue_AndWriteAudit()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var users = Substitute.For<IUserRepository>();
        users.DeactivateAsync(userId, Arg.Any<CancellationToken>())
             .Returns(true);

        var audit = Substitute.For<IAuditWriter>();
        AuditWriteEntry? captured = null;

        audit.When(x => x.WriteAsync(Arg.Any<AuditWriteEntry>(), Arg.Any<CancellationToken>()))
             .Do(ci => captured = ci.ArgAt<AuditWriteEntry>(0));

        var ctx = Substitute.For<IUserContext>();
        ctx.UserId.Returns(actorId);

        var sut = new DeactivateUserUseCase(users, audit, ctx);

        // Act
        var result = await sut.ExecuteAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        await users.Received(1).DeactivateAsync(userId, Arg.Any<CancellationToken>());
        await audit.Received(1).WriteAsync(Arg.Any<AuditWriteEntry>(), Arg.Any<CancellationToken>());

        Assert.NotNull(captured);
        Assert.Equal(AuditEventTypes.CoreUserDeactivated, captured!.EventType);
        Assert.Equal(actorId, captured.ActorUserId);
        Assert.Equal(AuditEntityTypes.User, captured.EntityType);
        Assert.Equal(userId, captured.EntityId);
        Assert.Contains("false", captured.DetailsJson); // is_active=false
    }
}