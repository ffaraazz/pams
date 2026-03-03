# PAMS API Consumption Flow Guide

---

| Field   | Value                             |
| ------- | --------------------------------- |
| Version | 1.0.0                             |
| Date    | 2026-03-03                        |
| Author  | ProductArchitect (GitHub Copilot) |

---

## 1. API Endpoint Master Table

All 27 endpoints in a single reference table.

| #   | Verb   | Route                                                  | Auth        | Purpose                                         | Used By       |
| --- | ------ | ------------------------------------------------------ | ----------- | ----------------------------------------------- | ------------- |
| 1   | GET    | /api/v1/accounts                                       | CanAllocate | List accounts (paginated, filterable)           | HR            |
| 2   | GET    | /api/v1/accounts/{accountCode}                         | CanAllocate | Get single account                              | HR            |
| 3   | POST   | /api/v1/accounts                                       | HROnly      | Create account                                  | HR            |
| 4   | PUT    | /api/v1/accounts/{accountCode}                         | HROnly      | Update account                                  | HR            |
| 5   | GET    | /api/v1/projects                                       | Authorize   | List projects (paginated, filterable)           | HR, PM        |
| 6   | GET    | /api/v1/projects/{projectCode}                         | Authorize   | Get project detail (with allocations + team)    | HR, PM, Staff |
| 7   | POST   | /api/v1/projects                                       | HROnly      | Create project                                  | HR            |
| 8   | PUT    | /api/v1/projects/{projectCode}                         | CanAllocate | Update project (PM scoped)                      | HR, PM        |
| 9   | GET    | /api/v1/employees                                      | CanAllocate | List/search employees (paginated)               | HR, PM        |
| 10  | GET    | /api/v1/employees/{empCode}                            | Authorize   | Get employee detail with allocations            | HR, PM, Staff |
| 11  | GET    | /api/v1/employees/me                                   | Authorize   | Get MY profile + allocations + managed projects | Everyone      |
| 12  | POST   | /api/v1/employees                                      | HROnly      | Create employee                                 | HR            |
| 13  | PUT    | /api/v1/employees/{empCode}                            | HROnly      | Update employee                                 | HR            |
| 14  | POST   | /api/v1/allocations                                    | CanAllocate | Create allocation                               | HR, PM        |
| 15  | GET    | /api/v1/allocations/{id}                               | Authorize   | Get single allocation                           | Anyone        |
| 16  | PUT    | /api/v1/allocations/{id}                               | CanAllocate | Update allocation % and dates                   | HR, PM        |
| 17  | PATCH  | /api/v1/allocations/{id}                               | CanAllocate | Stop allocation (set ToDate=today)              | HR, PM        |
| 18  | DELETE | /api/v1/allocations/{id}                               | CanAllocate | Soft-delete past allocation                     | HR, PM        |
| 19  | GET    | /api/v1/allocations/capacity-check                     | CanAllocate | Check employee availability in date window      | HR, PM        |
| 20  | GET    | /api/v1/projects/{code}/team-members                   | CanAllocate | List team-lead/reportee assignments             | HR, PM        |
| 21  | POST   | /api/v1/projects/{code}/team-members                   | CanAllocate | Add team assignment                             | HR, PM        |
| 22  | DELETE | /api/v1/projects/{code}/team-members/{lead}/{reportee} | CanAllocate | Remove team assignment                          | HR, PM        |
| 23  | GET    | /api/v1/skills                                         | Authorize   | List skills                                     | Everyone      |
| 24  | POST   | /api/v1/skills                                         | HROnly      | Create skill                                    | HR            |
| 25  | PUT    | /api/v1/skills/{skillId}                               | HROnly      | Update skill                                    | HR            |
| 26  | GET    | /api/v1/system-config                                  | HROnly      | Get allocation rules                            | HR            |
| 27  | PUT    | /api/v1/system-config                                  | HROnly      | Update allocation rules                         | HR            |

---

## 2. Staff Flow

Staff members have read-only access to their own profile, their allocations, and projects they lead.

### Home Screen

```
GET /api/v1/employees/me
```

Returns:

- Employee profile (name, designation, role, skills)
- `currentAllocations[]` — projects the staff member is allocated to, with %, dates, and status
- `managedProjects[]` — projects where this staff member is a Team Lead (via ProjectTeamMembers)

### View a Managed Project

When staff clicks on a project from `managedProjects[]`:

```
GET /api/v1/projects/{projectCode}
```

Returns the enriched `ProjectDetailResponse`:

- Project metadata (name, account, PM, status, billable)
- `allocations[]` — all active allocations on this project (read-only for staff)
- `teamMembers[]` — team lead/reportee assignments

> Staff cannot create, edit, stop, or remove allocations. They can only view.

### View a Colleague's Profile

```
GET /api/v1/employees/{empCode}
```

Returns employee detail with their current allocations (if the staff member is authorized to view).

### Staff Flow Diagram

```
+-----------------------------+
|       Staff Home Screen     |
|  GET /employees/me          |
+---------+-------------------+
          |
          v
+---------+-------------------+
| Profile + allocations[]     |
| + managedProjects[]         |
+---------+-------------------+
          |
    (click managed project)
          |
          v
+---------+-------------------+
| GET /projects/{code}        |
| -> Project detail           |
| -> allocations[] (read-only)|
| -> teamMembers[] (read-only)|
+-----------------------------+
```

---

## 3. PM Flow

Project Managers can manage allocations and team assignments on projects they own.

### Home Screen

```
GET /api/v1/employees/me
```

Returns:

- Profile + skills
- `currentAllocations[]` — projects the PM is allocated TO as a resource
- `managedProjects[]` — projects where the PM is ProjectManager OR TeamLead

### Project Detail Screen

```
GET /api/v1/projects/{projectCode}
```

Returns enriched response with:

- `allocations[]` — all active allocations for this project
- `teamMembers[]` — team lead/reportee assignments

### Edit a Resource's Allocation

```
PUT /api/v1/allocations/{id}
```

Update percentage or date range. Capacity rules enforced server-side. PM can only edit allocations on their own projects.

### Stop a Resource's Allocation

```
PATCH /api/v1/allocations/{id}
Body: { "action": "stop" }
```

Sets `toDate` to today (or `fromDate` if allocation hasn't started). PM scoped to own projects.

### Remove a Past Allocation

```
DELETE /api/v1/allocations/{id}
```

Soft-deletes an ended allocation (toDate < today). PM scoped to own projects.

### Add a New Resource (Full Flow)

```
Step 1:  GET  /api/v1/employees?search=Android
         -> Find the employee by name, empCode, or skill

Step 2:  GET  /api/v1/allocations/capacity-check?empCode=EMP042&fromDate=2026-04-01&toDate=2026-09-30
         -> Verify the employee has available capacity

Step 3:  POST /api/v1/allocations
         Body: { empCode: "EMP042", projectCode: "PROJ-ALPHA", fromDate: "2026-04-01", toDate: "2026-09-30", percentage: 50 }
         -> Create the allocation
```

### Assign a Reportee to a Team Lead

```
POST /api/v1/projects/{code}/team-members
Body: { teamLeadEmpCode: "EMP010", reporteeEmpCode: "EMP042" }
```

### Remove a Reportee Assignment

```
DELETE /api/v1/projects/{code}/team-members/{teamLeadEmpCode}/{reporteeEmpCode}
```

### PM Flow Diagram

```
+-----------------------------+
|        PM Home Screen       |
|  GET /employees/me          |
+---------+-------------------+
          |
          v
+---------+-------------------+
| Profile + allocations[]     |
| + managedProjects[]         |
+---------+-------------------+
          |
    (click managed project)
          |
          v
+---------+---------------------------+
| GET /projects/{code}                |
| -> allocations[] + teamMembers[]    |
+---------+---------------------------+
          |
          +-------+-------+-------+--------+
          |       |       |       |        |
          v       v       v       v        v
        EDIT    STOP   REMOVE   ADD     TEAM
        PUT     PATCH  DELETE  (flow)   POST/DELETE
  /alloc/{id} /alloc/{id} /alloc/{id}  /team-members
          |
   (Add new resource flow)
          |
          v
  GET /employees?search=...
          |
          v
  GET /allocations/capacity-check?...
          |
          v
  POST /allocations
```

---

## 4. HR Flow

HR has full access to all CRUD operations across the system.

### Home Screen

```
GET /api/v1/employees/me
```

Returns own profile with allocations and managed projects.

### Manage People

```
GET  /api/v1/employees                -> List all employees (paginated, filterable)
POST /api/v1/employees                -> Create a new employee
PUT  /api/v1/employees/{empCode}      -> Update employee (designation, role, isActive, skills, reportsTo)
```

### Manage Projects

```
GET  /api/v1/projects                 -> List all projects (paginated, filterable)
POST /api/v1/projects                 -> Create a new project (assign PM, set billable, dates)
PUT  /api/v1/projects/{projectCode}   -> Update project (name, PM, status, billable, isActive)
```

### Manage Accounts

```
GET  /api/v1/accounts                 -> List all accounts
GET  /api/v1/accounts/{accountCode}   -> Get single account detail
POST /api/v1/accounts                 -> Create a new account
PUT  /api/v1/accounts/{accountCode}   -> Update account (name, type, isActive)
```

### Allocation Management

Same as PM flow — HR can manage allocations on ANY project (not scoped):

```
POST   /api/v1/allocations            -> Create allocation
PUT    /api/v1/allocations/{id}       -> Update allocation % or dates
PATCH  /api/v1/allocations/{id}       -> Stop allocation
DELETE /api/v1/allocations/{id}       -> Soft-delete ended allocation
GET    /api/v1/allocations/capacity-check?empCode=...&fromDate=...  -> Check availability
```

### Team Member Management

```
GET    /api/v1/projects/{code}/team-members                        -> List assignments
POST   /api/v1/projects/{code}/team-members                        -> Add assignment
DELETE /api/v1/projects/{code}/team-members/{lead}/{reportee}       -> Remove assignment
```

### System Configuration

```
GET /api/v1/system-config             -> Get current allocation rules (minAllocationPct, allocationIncrement)
PUT /api/v1/system-config             -> Update allocation rules
```

### Manage Skills

```
GET  /api/v1/skills                   -> List all skills
POST /api/v1/skills                   -> Create a new skill
PUT  /api/v1/skills/{skillId}         -> Update skill name or isActive
```

### HR Flow Diagram

```
+------------------------------+
|         HR Home Screen       |
|   GET /employees/me          |
+---------+--------------------+
          |
    +-----+-----+------+------+------+------+
    |           |      |      |      |      |
    v           v      v      v      v      v
  PEOPLE    PROJECTS  ACCTS  ALLOC  TEAM  CONFIG
  GET/POST  GET/POST  GET    POST   GET   GET
  PUT       PUT       POST   PUT    POST  PUT
  /employees /projects PUT    PATCH  DEL
                      /accts  DEL
                              capacity-check
```

---

## 5. Key Concepts

### What is an Allocation?

An **Allocation** is an explicit resource assignment that records:

- **Who** (employee) is working on **what** (project)
- **When** (fromDate → toDate, or open-ended)
- **How much** (percentage of capacity: 1–100%)

Allocations are created by HR or PM. They are subject to capacity rules:

- Total overlapping allocations for one employee cannot exceed 100%
- Minimum percentage and increment are configurable via SystemConfig

### Allocation vs Project Management

**Being a PM on a project ≠ being allocated to it.**

| Concept            | Meaning                                                                      | How it's set                                                    |
| ------------------ | ---------------------------------------------------------------------------- | --------------------------------------------------------------- |
| Project Manager    | The person who manages the project (can edit allocations, assign team leads) | `project.projectManagerId` — set at project creation or via PUT |
| Allocated Resource | The person doing the work on the project                                     | `allocations` table — explicit POST /allocations                |

A PM can be both the manager AND an allocated resource, but these are separate records. A PM who only manages (not works on) a project will have no allocation row — they appear in `managedProjects[]` but not in `currentAllocations[]`.

### ProjectRole vs Designation

ProjectRole vs Designation: Designation is the org-level title on the Employee entity (e.g., 'Engineering Manager'). ProjectRole is the allocation-level role on a specific project (e.g., 'Architect', 'Tech Lead'). An employee can have different ProjectRoles across different allocations.

### Managing vs Working Distinction

```
+-----------------------+----------------------------+
| "I manage this project" | "I work on this project" |
+-----------------------+----------------------------+
| project.projectManagerId = me | allocations row exists |
| OR TeamLead on project        | with my employeeId     |
+-----------------------+----------------------------+
| Shows in: managedProjects[]   | Shows in: currentAllocations[] |
+-----------------------+----------------------------+
```

### ManagedProjects Computation

The `managedProjects[]` array in `EmployeeDetailResponse` is computed from TWO sources:

1. **ProjectManager**: Projects where `project.projectManagerId = currentUser.employeeId`
2. **TeamLead**: Projects where the user appears in `project_team_members` as `team_lead_id`

Each entry includes:

- Project metadata (code, name, account, status)
- `managementRole`: either `ProjectManager` or `TeamLead`
- `activeResourceCount`: count of active allocations on that project

---

## 6. Removed Endpoints

The following endpoints have been **removed** as of v1.8.0. They are no longer available in the API.

| Removed Endpoint               | Replacement                                                                                                              |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------ |
| `GET /dashboard/project-view`  | **REMOVED.** Use `GET /projects/{code}` which now includes `Allocations[]` and `TeamMembers[]` in the enriched response. |
| `GET /dashboard/employee-view` | **REMOVED.** Use `GET /employees` for search + `GET /employees/me` for own profile with `ManagedProjects[]`.             |

The `CanViewDashboard` authorization policy has also been **REMOVED**.

---

## 7. Enriched Response DTOs

In v1.7.0, several response DTOs were enriched to embed related data, reducing the number of API calls needed by consumers.

### AllocationDetailResponse — New Fields

| Field         | Type                             | Description                                |
| ------------- | -------------------------------- | ------------------------------------------ |
| `projectRole` | string (nullable)                | Role the employee performs on this project |
| `billable`    | boolean                          | Whether the project is billable            |
| `accountCode` | string                           | Account business code                      |
| `accountName` | string                           | Account display name                       |
| `status`      | string (Active, Upcoming, Ended) | Computed allocation status based on dates  |
| `updatedAt`   | date-time                        | Last modification timestamp                |

These fields are denormalized from the related Employee, Project, and Account entities so that consumers don't need to make additional calls to resolve display data.

### ProjectDetailResponse (ProjectResponse) — New Fields

| Field         | Type                        | Description                             |
| ------------- | --------------------------- | --------------------------------------- |
| `allocations` | AllocationDetailResponse[]  | All active allocations for this project |
| `teamMembers` | ProjectTeamMemberResponse[] | Team lead/reportee assignments          |

Previously, getting allocations for a project required calling a separate endpoint. Now `GET /projects/{code}` returns everything in one call.

### EmployeeDetailResponse — New Fields

| Field             | Type                 | Description                           |
| ----------------- | -------------------- | ------------------------------------- |
| `managedProjects` | ManagedProjectItem[] | Projects where user is PM or TeamLead |

### ManagedProjectItem Schema

| Field                 | Type                                 | Description                                |
| --------------------- | ------------------------------------ | ------------------------------------------ |
| `projectId`           | uuid                                 | Internal project ID                        |
| `projectCode`         | string                               | Business code                              |
| `projectName`         | string                               | Display name                               |
| `accountCode`         | string                               | Parent account code                        |
| `accountName`         | string                               | Parent account name                        |
| `managementRole`      | string (ProjectManager, TeamLead)    | How the user manages this project          |
| `status`              | string (Upcoming, Active, Completed) | Project lifecycle status                   |
| `activeResourceCount` | integer                              | Count of active allocations on the project |

### Before vs After — API Call Comparison

**Before (v1.6.0) — endpoints now removed:**

```
Staff home screen:
  1. GET /employees/me           -> profile + allocations
  2. GET /dashboard/project-view -> find managed projects (heavy call) [REMOVED]
  Total: 2 calls, 1 heavy

PM project detail:
  1. GET /projects/{code}        -> project metadata only
  2. GET /dashboard/project-view -> allocations for the project [REMOVED]
  3. GET /projects/{code}/team-members -> team assignments
  Total: 3 calls
```

**After (v1.7.0):**

```
Staff home screen:
  1. GET /employees/me           -> profile + allocations + managedProjects
  Total: 1 call

PM project detail:
  1. GET /projects/{code}        -> project metadata + allocations + teamMembers
  Total: 1 call
```
