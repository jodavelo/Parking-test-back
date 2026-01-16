using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Parking.Application.Commands;
using Parking.Domain.Entities;
using Parking.Domain.Exceptions;
using Parking.Domain.Interfaces;

namespace Parking.Tests;

public class ProcessAccessCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IVehicleRepository> _vehicleRepoMock;
    private readonly Mock<IAccessLogRepository> _accessLogRepoMock;
    private readonly Mock<ILogger<ProcessAccessCommandHandler>> _loggerMock;
    private readonly ProcessAccessCommandHandler _handler;

    public ProcessAccessCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _vehicleRepoMock = new Mock<IVehicleRepository>();
        _accessLogRepoMock = new Mock<IAccessLogRepository>();
        _loggerMock = new Mock<ILogger<ProcessAccessCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Vehicles).Returns(_vehicleRepoMock.Object);
        _unitOfWorkMock.Setup(x => x.AccessLogs).Returns(_accessLogRepoMock.Object);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _accessLogRepoMock
            .Setup(x => x.AddAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccessLog log, CancellationToken _) => log);

        _handler = new ProcessAccessCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_EntryNewVehicle_ShouldSucceed()
    {
        // Arrange
        var command = new ProcessAccessCommand("ABC123", Guid.NewGuid(), AccessType.Entry, DateTime.UtcNow);

        _vehicleRepoMock
            .Setup(x => x.GetByPlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        _vehicleRepoMock
            .Setup(x => x.GetActiveVehicleByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Entrada");
        _vehicleRepoMock.Verify(x => x.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EntryVehicleAlreadyInside_ShouldThrowException()
    {
        // Arrange
        var command = new ProcessAccessCommand("ABC123", Guid.NewGuid(), AccessType.Entry, DateTime.UtcNow);
        var existingVehicle = new Vehicle { Plate = "ABC123", IsInside = true };

        _vehicleRepoMock
            .Setup(x => x.GetByPlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVehicle);

        // Act & Assert
        await Assert.ThrowsAsync<VehicleAlreadyInsideException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExitVehicleNotInside_ShouldThrowException()
    {
        // Arrange
        var command = new ProcessAccessCommand("ABC123", Guid.NewGuid(), AccessType.Exit, DateTime.UtcNow);

        _vehicleRepoMock
            .Setup(x => x.GetByPlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        // Act & Assert
        await Assert.ThrowsAsync<VehicleNotInsideException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExitVehicleOutside_ShouldThrowException()
    {
        // Arrange
        var command = new ProcessAccessCommand("ABC123", Guid.NewGuid(), AccessType.Exit, DateTime.UtcNow);
        var vehicle = new Vehicle { Plate = "ABC123", IsInside = false };

        _vehicleRepoMock
            .Setup(x => x.GetByPlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        // Act & Assert
        await Assert.ThrowsAsync<VehicleNotInsideException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UserWithActiveVehicle_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ProcessAccessCommand("NEW123", userId, AccessType.Entry, DateTime.UtcNow);
        var activeVehicle = new Vehicle { Plate = "OLD123", IsInside = true, CurrentUserId = userId };

        _vehicleRepoMock
            .Setup(x => x.GetByPlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        _vehicleRepoMock
            .Setup(x => x.GetActiveVehicleByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeVehicle);

        // Act & Assert
        await Assert.ThrowsAsync<UserHasActiveVehicleException>(() =>
            _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExitSuccess_ShouldUpdateVehicleState()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ProcessAccessCommand("ABC123", userId, AccessType.Exit, DateTime.UtcNow);
        var vehicle = new Vehicle { Id = Guid.NewGuid(), Plate = "ABC123", IsInside = true, CurrentUserId = userId };

        _vehicleRepoMock
            .Setup(x => x.GetByPlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Salida");
        vehicle.IsInside.Should().BeFalse();
        _vehicleRepoMock.Verify(x => x.UpdateAsync(vehicle, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FailedAttempt_ShouldCreateAuditLog()
    {
        // Arrange
        var command = new ProcessAccessCommand("ABC123", Guid.NewGuid(), AccessType.Entry, DateTime.UtcNow);
        var existingVehicle = new Vehicle { Plate = "ABC123", IsInside = true };

        _vehicleRepoMock
            .Setup(x => x.GetByPlateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingVehicle);

        // Act & Assert
        await Assert.ThrowsAsync<VehicleAlreadyInsideException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _accessLogRepoMock.Verify(
            x => x.AddAsync(It.Is<AccessLog>(log => log.Success == false), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
