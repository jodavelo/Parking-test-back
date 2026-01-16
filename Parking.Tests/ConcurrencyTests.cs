using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Parking.Application.Commands;
using Parking.Domain.Entities;
using Parking.Domain.Exceptions;
using Parking.Domain.Interfaces;

namespace Parking.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task Handle_ConcurrentEntries_ShouldHandleConcurrencyConflict()
    {
        // Arrange
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var vehicleRepoMock = new Mock<IVehicleRepository>();
        var accessLogRepoMock = new Mock<IAccessLogRepository>();
        var loggerMock = new Mock<ILogger<ProcessAccessCommandHandler>>();

        unitOfWorkMock.Setup(x => x.Vehicles).Returns(vehicleRepoMock.Object);
        unitOfWorkMock.Setup(x => x.AccessLogs).Returns(accessLogRepoMock.Object);
        unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        vehicleRepoMock
            .Setup(x => x.GetByPlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        vehicleRepoMock
            .Setup(x => x.GetActiveVehicleByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        accessLogRepoMock
            .Setup(x => x.AddAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccessLog log, CancellationToken _) => log);

        unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException());

        var handler = new ProcessAccessCommandHandler(unitOfWorkMock.Object, loggerMock.Object);
        var command = new ProcessAccessCommand("ABC123", Guid.NewGuid(), AccessType.Entry, DateTime.UtcNow);

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SimultaneousRequests_OnlyOneSucceeds()
    {
        // Arrange
        var plate = "CONCURRENT123";
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var vehicleRepoMock = new Mock<IVehicleRepository>();
        var accessLogRepoMock = new Mock<IAccessLogRepository>();
        var loggerMock = new Mock<ILogger<ProcessAccessCommandHandler>>();

        var callCount = 0;
        var unitOfWorkMock1 = CreateUnitOfWorkMock(vehicleRepoMock, accessLogRepoMock);
        var unitOfWorkMock2 = CreateUnitOfWorkMock(vehicleRepoMock, accessLogRepoMock);

        vehicleRepoMock
            .Setup(x => x.GetByPlateAsync(plate.ToUpperInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1) return null;
                return new Vehicle { Plate = plate, IsInside = true };
            });

        vehicleRepoMock
            .Setup(x => x.GetActiveVehicleByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        accessLogRepoMock
            .Setup(x => x.AddAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccessLog log, CancellationToken _) => log);

        var handler1 = new ProcessAccessCommandHandler(unitOfWorkMock1.Object, loggerMock.Object);
        var handler2 = new ProcessAccessCommandHandler(unitOfWorkMock2.Object, loggerMock.Object);

        var command1 = new ProcessAccessCommand(plate, userId1, AccessType.Entry, DateTime.UtcNow);
        var command2 = new ProcessAccessCommand(plate, userId2, AccessType.Entry, DateTime.UtcNow);

        // Act
        var result1 = await handler1.Handle(command1, CancellationToken.None);

        // Assert
        result1.Success.Should().BeTrue();

        await Assert.ThrowsAsync<VehicleAlreadyInsideException>(() =>
            handler2.Handle(command2, CancellationToken.None));
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock(
        Mock<IVehicleRepository> vehicleRepoMock,
        Mock<IAccessLogRepository> accessLogRepoMock)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(x => x.Vehicles).Returns(vehicleRepoMock.Object);
        unitOfWorkMock.Setup(x => x.AccessLogs).Returns(accessLogRepoMock.Object);
        unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWorkMock;
    }
}
