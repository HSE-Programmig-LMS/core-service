using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Contracts.Users;
using CoreService.Application.UseCases.Users;
using NSubstitute;
using Xunit;

namespace CoreService.Tests.Unit.Users;

public sealed class GetUserByIdUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldReturnUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
             .Returns((UserDto?)null);

        var sut = new GetUserByIdUseCase(users);

        // Act
        var result = await sut.ExecuteAsync(userId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.UserNotFound, result.Error!.Code);

        await users.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserExists_ShouldReturnOkUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var dto = new UserDto(
            UserId: userId,
            Email: "u@x.com",
            FirstName: "A",
            LastName: "B",
            IsActive: true,
            Role: "manager",
            CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-5),
            LastLoginAtUtc: DateTimeOffset.UtcNow.AddMinutes(-10));

        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>())
             .Returns(dto);

        var sut = new GetUserByIdUseCase(users);

        // Act
        var result = await sut.ExecuteAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(dto, result.Value);

        await users.Received(1).GetByIdAsync(userId, Arg.Any<CancellationToken>());
    }
}