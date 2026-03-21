using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Contracts.Users;
using CoreService.Application.UseCases.Users;
using NSubstitute;
using Xunit;

namespace CoreService.Tests.Unit.Users;

public sealed class GetUsersUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPageLessThan1_ShouldReturnValidationError()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();
        var sut = new GetUsersUseCase(users);

        var query = new UsersQuery(Page: 0, PageSize: 20);

        // Act
        var result = await sut.ExecuteAsync(query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await users.DidNotReceiveWithAnyArgs().GetListAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPageSizeLessThan1_ShouldReturnValidationError()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();
        var sut = new GetUsersUseCase(users);

        var query = new UsersQuery(Page: 1, PageSize: 0);

        // Act
        var result = await sut.ExecuteAsync(query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await users.DidNotReceiveWithAnyArgs().GetListAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPageSizeGreaterThan200_ShouldReturnValidationError()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();
        var sut = new GetUsersUseCase(users);

        var query = new UsersQuery(Page: 1, PageSize: 201);

        // Act
        var result = await sut.ExecuteAsync(query);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ValidationError, result.Error!.Code);

        await users.DidNotReceiveWithAnyArgs().GetListAsync(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidQuery_ShouldReturnPagedResult()
    {
        // Arrange
        var users = Substitute.For<IUserRepository>();
        var sut = new GetUsersUseCase(users);

        var query = new UsersQuery(
            EmailContains: "hse",
            Role: "teacher",
            IsActive: true,
            Page: 2,
            PageSize: 20);

        var expected = new PagedResult<UserDto>(
            Items: new[]
            {
                new UserDto(
                    UserId: Guid.NewGuid(),
                    Email: "t1@hse.ru",
                    FirstName: "A",
                    LastName: "B",
                    IsActive: true,
                    Role: "teacher",
                    CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-3),
                    LastLoginAtUtc: null)
            },
            Page: 2,
            PageSize: 20,
            TotalCount: 21);

        users.GetListAsync(query, Arg.Any<CancellationToken>())
             .Returns(expected);

        // Act
        var result = await sut.ExecuteAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);

        await users.Received(1).GetListAsync(query, Arg.Any<CancellationToken>());
    }
}