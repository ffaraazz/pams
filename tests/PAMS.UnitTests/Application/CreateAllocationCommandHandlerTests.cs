using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PAMS.UnitTests.Helpers;

namespace PAMS.UnitTests.Application;

/// <summary>
/// Unit tests for CreateAllocationCommandHandler (FR-010).
/// Handler orchestrates: auth check → project lookup → employee lookup →
/// overlapping allocations → capacity validation → persist → audit.
/// All repository dependencies are mocked via NSubstitute.
/// Tests are TDD fail-first: domain/application types don't exist yet.
/// </summary>
public sealed class CreateAllocationCommandHandlerTests
{
    // ─── Mocks ───────────────────────────────────────────────────────────────

    private readonly PAMS.Domain.Repositories.IProjectRepository _projectRepo = Substitute.For<PAMS.Domain.Repositories.IProjectRepository>();
    private readonly PAMS.Domain.Repositories.IEmployeeRepository _employeeRepo = Substitute.For<PAMS.Domain.Repositories.IEmployeeRepository>();
    private readonly PAMS.Domain.Repositories.IAllocationRepository _allocationRepo = Substitute.For<PAMS.Domain.Repositories.IAllocationRepository>();
    private readonly PAMS.Application.Interfaces.ICurrentUserService _currentUser = Substitute.For<PAMS.Application.Interfaces.ICurrentUserService>();
    private readonly PAMS.Domain.Common.IUnitOfWork _unitOfWork = Substitute.For<PAMS.Domain.Common.IUnitOfWork>();
    private readonly PAMS.Application.Interfaces.IAuditLogService _auditLog = Substitute.For<PAMS.Application.Interfaces.IAuditLogService>();

    private PAMS.Application.Commands.Allocations.CreateAllocationCommandHandler CreateSut()
        => new(_projectRepo, _employeeRepo, _allocationRepo, _currentUser, _unitOfWork, _auditLog);

    // ─── FR-010 / AC-010-7: Happy path – allocation created ─────────────────

    [Fact(DisplayName = "FR-010 | Handle_ValidCommand_ShouldPersistAndReturnCreated")]
    public async Task Handle_ValidCommand_ShouldPersistAndReturnCreated()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var pmId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, pmId);
        var employee = CreateActiveEmployee(employeeId, TestData.StaffEmpCode);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(pmId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);

        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);

        _allocationRepo.GetOverlappingAsync(employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PAMS.Domain.Entities.Allocation>());

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        await _allocationRepo.Received(1).AddAsync(Arg.Any<PAMS.Domain.Entities.Allocation>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── FR-010 / AC-010-5: PM scoped to own projects ───────────────────────

    [Fact(DisplayName = "FR-010 | Handle_PmNotProjectOwner_ShouldThrowForbiddenException")]
    public async Task Handle_PmNotProjectOwner_ShouldThrowForbiddenException()
    {
        // Arrange — PM tries to allocate on a project they don't own
        var projectId = Guid.NewGuid();
        var otherPmId = Guid.NewGuid();
        var callerPmId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, otherPmId);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.ProjectManager);
        _currentUser.EmployeeId.Returns(callerPmId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PAMS.Application.Exceptions.ForbiddenException>();
    }

    // ─── FR-010 / AC-010-6: HR can allocate to any project ──────────────────

    [Fact(DisplayName = "FR-010 | Handle_HrAllocatesToAnyProject_ShouldNotThrow")]
    public async Task Handle_HrAllocatesToAnyProject_ShouldNotThrow()
    {
        // Arrange — HR allocates to a project with a different PM
        var projectId = Guid.NewGuid();
        var otherPmId = Guid.NewGuid();
        var hrId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, otherPmId);
        var employee = CreateActiveEmployee(employeeId, TestData.StaffEmpCode);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);

        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);

        _allocationRepo.GetOverlappingAsync(employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PAMS.Domain.Entities.Allocation>());

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 25,
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert — no exception
        await act.Should().NotThrowAsync();
    }

    // ─── FR-010 / AC-010-4: Capacity exceeded → error ───────────────────────

    [Fact(DisplayName = "FR-010 | Handle_CapacityExceeded_ShouldThrowCapacityExceededException")]
    public async Task Handle_CapacityExceeded_ShouldThrowCapacityExceededException()
    {
        // Arrange — employee already at 80%, new request for 30%
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, hrId);
        var employee = CreateActiveEmployee(employeeId, TestData.StaffEmpCode);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);

        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);

        // Existing allocation at 80%
        var existingAllocation = CreateAllocation(employeeId, projectId, 80, TestData.Today, null);
        _allocationRepo.GetOverlappingAsync(employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PAMS.Domain.Entities.Allocation> { existingAllocation });

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 30, // 80 + 30 = 110 > 100
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PAMS.Domain.Exceptions.CapacityExceededException>();
    }

    // ─── FR-010 / Edge: Inactive project → not found ─────────────────────────

    [Fact(DisplayName = "FR-010 | Handle_InactiveProject_ShouldThrowNotFoundException")]
    public async Task Handle_InactiveProject_ShouldThrowNotFoundException()
    {
        // Arrange — project is deactivated (not found by repo)
        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns((PAMS.Domain.Entities.Project?)null);

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PAMS.Application.Exceptions.NotFoundException>();
    }

    // ─── FR-010 / Edge: Inactive employee → not found ────────────────────────

    [Fact(DisplayName = "FR-010 | Handle_InactiveEmployee_ShouldThrowNotFoundException")]
    public async Task Handle_InactiveEmployee_ShouldThrowNotFoundException()
    {
        // Arrange — employee is deactivated
        var projectId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, hrId);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);

        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns((PAMS.Domain.Entities.Employee?)null);

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PAMS.Application.Exceptions.NotFoundException>();
    }

    // ─── FR-010 / Edge: Audit log is written ─────────────────────────────────

    [Fact(DisplayName = "FR-010 | Handle_SuccessfulCreation_ShouldWriteAuditLog")]
    public async Task Handle_SuccessfulCreation_ShouldWriteAuditLog()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, hrId);
        var employee = CreateActiveEmployee(employeeId, TestData.StaffEmpCode);

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);

        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);

        _allocationRepo.GetOverlappingAsync(employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PAMS.Domain.Entities.Allocation>());

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert — audit log called
        await _auditLog.Received(1).LogAsync(
            Arg.Is<string>(s => s.Contains("allocation.created")),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    // ─── Enriched AllocationDetailResponse Tests (TDD Red Phase) ────────────
    // These tests reference properties that do not yet exist on AllocationDetailResponse.
    // They will fail to compile until the DTO is enriched — expected TDD behavior.

    [Fact(DisplayName = "FR-010 | Handle_WhenAllocationCreated_ResponseShouldIncludeProjectRole")]
    public async Task Handle_WhenAllocationCreated_ResponseShouldIncludeProjectRole()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, hrId);
        project.ProjectName.Returns("Alpha Project");

        var employee = CreateActiveEmployee(employeeId, TestData.StaffEmpCode);
        employee.FirstName.Returns("Jane");
        employee.LastName.Returns("Doe");

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);
        _allocationRepo.GetOverlappingAsync(employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PAMS.Domain.Entities.Allocation>());

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null,
            ProjectRole = "Architect"
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — ProjectRole should come from command.ProjectRole
        result.ProjectRole.Should().Be("Architect");
    }

    [Fact(DisplayName = "FR-010 | Handle_WhenProjectRoleNotProvided_ResponseShouldHaveNullProjectRole")]
    public async Task Handle_WhenProjectRoleNotProvided_ResponseShouldHaveNullProjectRole()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, hrId);
        project.ProjectName.Returns("Alpha Project");

        var employee = CreateActiveEmployee(employeeId, TestData.StaffEmpCode);
        employee.FirstName.Returns("Jane");
        employee.LastName.Returns("Doe");

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);
        _allocationRepo.GetOverlappingAsync(employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PAMS.Domain.Entities.Allocation>());

        // Note: ProjectRole is NOT set on the command
        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — ProjectRole should be null when not provided
        result.ProjectRole.Should().BeNull();
    }

    [Fact(DisplayName = "FR-010 | Handle_WhenAllocationCreated_ResponseShouldIncludeBillable")]
    public async Task Handle_WhenAllocationCreated_ResponseShouldIncludeBillable()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, hrId);
        project.ProjectName.Returns("Alpha Project");
        project.Billable.Returns(true);

        var employee = CreateActiveEmployee(employeeId, TestData.StaffEmpCode);
        employee.FirstName.Returns("Jane");
        employee.LastName.Returns("Doe");

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);
        _allocationRepo.GetOverlappingAsync(employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PAMS.Domain.Entities.Allocation>());

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — Billable should come from Project.Billable
        result.Billable.Should().BeTrue();
    }

    [Fact(DisplayName = "FR-010 | Handle_WhenAllocationCreated_ResponseShouldIncludeAccountInfo")]
    public async Task Handle_WhenAllocationCreated_ResponseShouldIncludeAccountInfo()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, hrId);
        project.ProjectName.Returns("Alpha Project");
        var account = Substitute.For<PAMS.Domain.Entities.Account>();
        account.AccountCode.Returns("ACC001");
        account.AccountName.Returns("Acme Corp");
        project.Account.Returns(account);

        var employee = CreateActiveEmployee(employeeId, TestData.StaffEmpCode);
        employee.FirstName.Returns("Jane");
        employee.LastName.Returns("Doe");

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);
        _allocationRepo.GetOverlappingAsync(employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PAMS.Domain.Entities.Allocation>());

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — AccountCode and AccountName should come from Project.Account
        result.AccountCode.Should().Be("ACC001");
        result.AccountName.Should().Be("Acme Corp");
    }

    [Fact(DisplayName = "FR-010 | Handle_WhenAllocationCreated_ResponseShouldIncludeStatus")]
    public async Task Handle_WhenAllocationCreated_ResponseShouldIncludeStatus()
    {
        // Arrange — FromDate = today, ToDate = null → computed Status = "Active"
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, hrId);
        project.ProjectName.Returns("Alpha Project");

        var employee = CreateActiveEmployee(employeeId, TestData.StaffEmpCode);
        employee.FirstName.Returns("Jane");
        employee.LastName.Returns("Doe");

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);
        _allocationRepo.GetOverlappingAsync(employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PAMS.Domain.Entities.Allocation>());

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — Status is computed: "Active" when FromDate <= today && (ToDate == null || ToDate >= today)
        result.Status.Should().Be("Active");
    }

    [Fact(DisplayName = "FR-010 | Handle_WhenAllocationCreated_ResponseShouldIncludeUpdatedAt")]
    public async Task Handle_WhenAllocationCreated_ResponseShouldIncludeUpdatedAt()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var hrId = Guid.NewGuid();

        var project = CreateActiveProject(projectId, TestData.ProjectCode, hrId);
        project.ProjectName.Returns("Alpha Project");

        var employee = CreateActiveEmployee(employeeId, TestData.StaffEmpCode);
        employee.FirstName.Returns("Jane");
        employee.LastName.Returns("Doe");

        _currentUser.Role.Returns(PAMS.Domain.Enums.EmployeeRole.HR);
        _currentUser.EmployeeId.Returns(hrId);

        _projectRepo.GetByCodeAsync(TestData.ProjectCode, Arg.Any<CancellationToken>())
            .Returns(project);
        _employeeRepo.GetByEmpCodeAsync(TestData.StaffEmpCode, Arg.Any<CancellationToken>())
            .Returns(employee);
        _allocationRepo.GetOverlappingAsync(employeeId, Arg.Any<DateOnly>(), Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(new List<PAMS.Domain.Entities.Allocation>());

        var command = new PAMS.Application.Commands.Allocations.CreateAllocationCommand
        {
            ProjectCode = TestData.ProjectCode,
            EmpCode = TestData.StaffEmpCode,
            Percentage = 50,
            FromDate = TestData.Today,
            ToDate = null
        };

        var sut = CreateSut();
        var before = DateTime.UtcNow;

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert — UpdatedAt should be set to approximately now
        result.UpdatedAt.Should().BeOnOrAfter(before);
    }

    // ─── Allocation Billable Flag Tests (TDD Red Phase) ────────────────────
    // These reflection-based tests verify that the Allocation entity and
    // CreateAllocationCommand gain a Billable property, and that MapFrom
    // maps billable from the allocation level, not just the project level.
    // All three will FAIL until the implementation is added.

    [Fact(DisplayName = "FR-010 | Allocation_ShouldHaveBillablePropertyOfTypeBool")]
    public void Allocation_ShouldHaveBillablePropertyOfTypeBool()
    {
        // Arrange
        var allocationType = typeof(PAMS.Domain.Entities.Allocation);

        // Act
        var billableProperty = allocationType.GetProperty("Billable");

        // Assert — will FAIL: Allocation entity has no 'Billable' property yet
        billableProperty.Should().NotBeNull("Allocation entity should have a 'Billable' property");
        billableProperty!.PropertyType.Should().Be(typeof(bool),
            "Billable should be a bool");
    }

    [Fact(DisplayName = "FR-010 | CreateAllocationCommand_ShouldHaveBillableProperty")]
    public void CreateAllocationCommand_ShouldHaveBillableProperty()
    {
        // Arrange
        var commandType = typeof(PAMS.Application.Commands.Allocations.CreateAllocationCommand);

        // Act
        var billableProperty = commandType.GetProperty("Billable");

        // Assert — will FAIL: CreateAllocationCommand has no 'Billable' property yet
        billableProperty.Should().NotBeNull("CreateAllocationCommand should have a 'Billable' property");
        billableProperty!.PropertyType.Should().Be(typeof(bool),
            "Billable should be a bool on the command");
    }

    [Fact(DisplayName = "FR-010 | AllocationDetailResponse_MapFrom_ShouldMapBillableFromAllocationEntity")]
    public void AllocationDetailResponse_MapFrom_ShouldMapBillableFromAllocationEntity()
    {
        // Arrange — project is NOT billable, but allocation-level billable should be true
        var allocation = Substitute.For<PAMS.Domain.Entities.Allocation>();
        allocation.Id.Returns(Guid.NewGuid());
        allocation.EmployeeId.Returns(Guid.NewGuid());
        allocation.ProjectId.Returns(Guid.NewGuid());
        allocation.FromDate.Returns(TestData.Today);
        allocation.Percentage.Returns(50);

        var project = Substitute.For<PAMS.Domain.Entities.Project>();
        project.Billable.Returns(false); // project-level billable is false

        var employee = Substitute.For<PAMS.Domain.Entities.Employee>();
        employee.EmpCode.Returns(TestData.StaffEmpCode);

        // Act — attempt to set allocation-level Billable via reflection
        var billableProp = typeof(PAMS.Domain.Entities.Allocation).GetProperty("Billable");

        // Assert — will FAIL: Allocation entity has no 'Billable' property yet,
        // so per-allocation billable override cannot be mapped
        billableProp.Should().NotBeNull(
            "Allocation entity needs a 'Billable' property so MapFrom can map " +
            "resource-level billable independently of project-level billable");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static PAMS.Domain.Entities.Project CreateActiveProject(Guid id, string code, Guid pmId)
    {
        // TDD placeholder — actual Project entity will have a factory/constructor
        // The test is designed against the expected entity shape
        var project = Substitute.For<PAMS.Domain.Entities.Project>();
        project.Id.Returns(id);
        project.ProjectCode.Returns(code);
        project.ProjectManagerId.Returns(pmId);
        project.IsActive.Returns(true);
        return project;
    }

    private static PAMS.Domain.Entities.Employee CreateActiveEmployee(Guid id, string empCode)
    {
        var employee = Substitute.For<PAMS.Domain.Entities.Employee>();
        employee.Id.Returns(id);
        employee.EmpCode.Returns(empCode);
        employee.IsActive.Returns(true);
        return employee;
    }

    private static PAMS.Domain.Entities.Allocation CreateAllocation(Guid employeeId, Guid projectId, int pct, DateOnly from, DateOnly? to)
    {
        var alloc = Substitute.For<PAMS.Domain.Entities.Allocation>();
        alloc.EmployeeId.Returns(employeeId);
        alloc.ProjectId.Returns(projectId);
        alloc.Percentage.Returns(pct);
        alloc.FromDate.Returns(from);
        alloc.ToDate.Returns(to);
        return alloc;
    }
}
