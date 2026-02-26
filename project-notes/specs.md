# Project Allocation Management – Product Specification

---

## 1. Document Control

| Field          | Value                                       |
| -------------- | ------------------------------------------- |
| Project        | Project Allocation Management System (PAMS) |
| Version        | 1.2.0                                       |
| Date           | 2026-02-26                                  |
| Author         | BusinessAnalyst (GitHub Copilot)            |
| Status         | Updated – Billable & Status Enum Revamp     |
| Pipeline State | Phase 1 – Specification Updated             |

---

## 2. Executive Summary

### Problem

Organizations lack a centralized, role-aware tool to track and manage project-level employee allocations, leading to overbooking, bench blindness, and reactive staffing decisions.

### Solution

A web-based Project Allocation Management System (PAMS) providing:

- Full CRUD for accounts, projects, employees, and allocations with strict role-based access.
- Real-time allocation status per employee (Bench / Partial / Full) within any date window.
- Skill-based employee discovery with bench-first surfacing.
- Dual-view dashboard: Project View and Employee View.

### Market

Internal workforce management tooling for mid-to-large technology services organizations.

### Differentiation

- Inline allocation capacity guard (blocks over-100% allocation at input).
- Configurable allocation increment (5% or 10%) and minimum threshold (25%) via system settings.
- Bench-first search result ordering on skill queries.
- Soft-delete allocation history for audit trail.

### MVP Scope

Delivery is split into two phases:

**MVP1 (Initial Release):** Allocation dashboard for Staff and Project Managers; Projects screen for PMs; full HR admin capabilities; role-based API responses; paginated single-endpoint filterable APIs (default page=1, limit=10); account and project summary statistics.

**MVP2 (Follow-on Release):** Employee self-service skill management (add, edit, remove own skills); Project Manager association with multiple accounts.

---

## 3. Business Context

### Business Objectives

| ID   | Objective                                         | KPI                                 | Formula                                         | Target          | Timeframe    |
| ---- | ------------------------------------------------- | ----------------------------------- | ----------------------------------------------- | --------------- | ------------ |
| BO-1 | Eliminate employee overbooking                    | Overbooking incidents per month     | Count of allocations exceeding 100% capacity    | 0               | From go-live |
| BO-2 | Reduce bench time                                 | Average bench duration per employee | Sum(bench days) / headcount                     | ≥ 20% reduction | 3 months     |
| BO-3 | Reduce PM time to allocate an employee            | Time-to-allocate                    | Time from search initiation to saved allocation | < 3 minutes     | From go-live |
| BO-4 | Provide accurate allocation reports to leadership | Report accuracy                     | Validated allocations / total allocations × 100 | ≥ 99%           | From go-live |

---

## 4. Stakeholders

### Internal

| Role            | Interest                                     |
| --------------- | -------------------------------------------- |
| HR              | Master data governance; full system control  |
| Project Manager | Staffing projects; viewing team availability |
| Staff           | Visibility into own allocation and schedule  |
| System Admin    | Configuration of system parameters           |

### External

| Role   | Interest                                       |
| ------ | ---------------------------------------------- |
| Client | Indirectly; billed against account allocations |

### Regulatory

- Data privacy requirements for employee PII (names, designations) must be assessed per applicable jurisdiction before deployment.

---

## 5. Target Users & Personas

### Persona 1 – HR Manager (Emma)

- **Role:** HR
- **Goals:** Maintain clean master data; onboard/offboard employees; configure accounts and projects.
- **Pain Points:** Manual spreadsheets; no audit trail; accidental over-allocations.
- **Technical Literacy:** High.
- **Workflows:** Daily – employee management; weekly – project/account review.

### Persona 2 – Project Manager (Ravi)

- **Role:** Project Manager
- **Goals:** Quickly find available talent with the right skills; allocate to project; monitor team capacity.
- **Pain Points:** No visibility into who is benched; unknown skill sets; overbooking risk.
- **Technical Literacy:** High.
- **Workflows:** Weekly – allocation planning; daily – team availability spot-check.

### Persona 3 – Staff Employee (Alex)

- **Role:** Staff (no reportees)
- **Goals:** See own project commitments and durations; know upcoming project transitions.
- **Pain Points:** Unaware of upcoming project transitions; no single view of all their engagements.
- **Technical Literacy:** Medium.
- **Workflows:** As needed – check own allocation dashboard.

### Persona 4 – Staff Team Lead (Sam)

- **Role:** Staff acting as Team Lead on one or more projects
- **Goals:** View own allocations; visibility into the allocations of team members project-by-project.
- **Pain Points:** Lack of project-scoped team visibility; reportees differ per project.
- **Technical Literacy:** Medium-High.
- **Security:** Can only see reportee allocations within projects they lead; cannot modify any allocation.
- **Workflows:** Weekly – review own dashboard; review team allocation per project tab.

---

## 6. Market & Competitive Analysis

| Feature                       | PAMS (MVP) | Float   | Resource Guru | Typical spreadsheet |
| ----------------------------- | ---------- | ------- | ------------- | ------------------- |
| Role-based access control     | Yes        | Partial | Yes           | No                  |
| Allocation % guardrail        | Yes        | Yes     | Yes           | No                  |
| Bench/availability view       | Yes        | Yes     | Yes           | No                  |
| Skill-based search            | Yes        | No      | Partial       | No                  |
| Soft-delete history           | Yes        | No      | No            | No                  |
| Configurable increment        | Yes        | No      | No            | N/A                 |
| Project + Employee dual views | Yes        | Partial | Yes           | N/A                 |

---

## 7. Scope Definition

### In Scope – MVP1

- Account management (CRUD) with summary stats (total projects, total active/inactive employees).
- Project management (CRUD, linked to accounts).
- Employee management (CRUD, hierarchy via reportsTo).
- Allocation management (CRUD with capacity validation).
- Role-based access: HR, Project Manager, Staff.
- Allocation status computation: Bench / Partial / Full.
- Skill tagging on employees by HR; skill-based search.
- Employee search, list, and filter via a single unified endpoint per resource (no separate search APIs).
- Staff Allocation Dashboard: own allocations + project-wise team view (if Team Lead).
- PM Allocation Dashboard: own allocations (same as Staff).
- PM Projects Screen: view and manage team allocations per project; configure project-scoped Team Leads; add/remove reportees per project.
- HR admin screens: all CRUD, all views.
- Configurable allocation minimum and increment.
- Soft-delete for allocations.
- Stop allocation (end from tomorrow or today if not yet started).
- Project-scoped Team Lead / reportee configuration (by PM or HR).
- Pagination on all list APIs: `page` (default 1) and `limit` (default 10).
- Role-based API responses: data filtered server-side based on token role.
- Identity and authentication via Keycloak OAuth2/OIDC (PAMS operates as a resource server).

### In Scope – MVP2

- Employee self-service skill management: Staff and PM can add, edit, and remove their own skills.
- Project Manager can be associated with multiple accounts.

### Out of Scope (Both MVP Phases)

- Mobile application.
- Client portal or external access.
- Automated notifications / email alerts.
- Leave / absence integration.
- Financial billing or cost tracking.
- Multi-language / localization.
- SAML / multi-IdP federation (Keycloak is the sole identity provider).
- Gantt chart or visual timeline view.
- Historical reporting / export.

---

## 8. Functional Requirements

### FR-001 – Create Account

| Field          | Value                                                                           |
| -------------- | ------------------------------------------------------------------------------- |
| Priority       | P0                                                                              |
| Persona        | HR                                                                              |
| Description    | HR can create a new account with a unique code, name, and type.                 |
| Trigger        | HR clicks "Add Account".                                                        |
| Preconditions  | User is authenticated as HR.                                                    |
| Postconditions | Account record is persisted; visible in account list.                           |
| User Story     | As an HR, I want to create an account so that projects can be grouped under it. |

**Acceptance Criteria:**

- AC-001-1: Account Code must be unique (case-insensitive); system rejects duplicates with error `ERR_ACCOUNT_CODE_EXISTS`.
- AC-001-2: Account Name is required; max 150 characters.
- AC-001-3: Account Type must be one of: `Client`, `Internal`, `Bench`.
- AC-001-4: On success, account appears in the account list immediately.
- AC-001-5: Account is set `isActive = true` by default.
- AC-001-6: Account list and detail responses include computed summary stats: `totalActiveProjects`, `totalInactiveProjects`, `totalActiveEmployees`, `totalInactiveEmployees` (counts of employees currently allocated to projects under this account).

**Negative / Edge Cases:**

- Submitting without Account Code → validation error "Account Code is required".
- Account Code with only whitespace → treated as empty; blocked.
- Duplicate Account Code (different casing) → blocked.

**Data Entities:** Account  
**Screens:** Account List, Add Account Modal/Form  
**Dependencies:** None

---

### FR-002 – Edit Account

| Field          | Value                                                                                             |
| -------------- | ------------------------------------------------------------------------------------------------- |
| Priority       | P0                                                                                                |
| Persona        | HR                                                                                                |
| Description    | HR can modify the name and type of an existing account. Account Code is immutable after creation. |
| Trigger        | HR clicks "Edit" on an account row.                                                               |
| Preconditions  | Account exists and is active.                                                                     |
| Postconditions | Account record reflects updated values.                                                           |
| User Story     | As an HR, I want to update account details to keep records accurate.                              |

**Acceptance Criteria:**

- AC-002-1: Account Code field is read-only in edit mode.
- AC-002-2: Account Name and Type can be changed.
- AC-002-3: Saving with an empty Account Name returns error "Account Name is required".
- AC-002-4: Changes are persisted and reflected immediately on the list.

**Negative / Edge Cases:**

- Navigating away without saving prompts unsaved-changes confirmation.

**Data Entities:** Account  
**Screens:** Account List, Edit Account Modal/Form  
**Dependencies:** None

---

### FR-003 – Deactivate Account

| Field          | Value                                                                                         |
| -------------- | --------------------------------------------------------------------------------------------- |
| Priority       | P1                                                                                            |
| Persona        | HR                                                                                            |
| Description    | HR can deactivate an account. Deactivation is a soft operation; historical data is preserved. |
| Trigger        | HR clicks "Deactivate" on an account row.                                                     |
| Preconditions  | Account exists; no active projects linked to it.                                              |
| Postconditions | Account `isActive = false`; no longer selectable for new projects.                            |
| User Story     | As an HR, I want to deactivate retired accounts without losing data.                          |

**Acceptance Criteria:**

- AC-003-1: If account has one or more active projects, deactivation is blocked. Error: `ERR_ACCOUNT_HAS_ACTIVE_PROJECTS`.
- AC-003-2: Confirmation dialog required before deactivation.
- AC-003-3: Deactivated accounts are hidden from project creation dropdowns.
- AC-003-4: Deactivated accounts remain visible in a filtered "Inactive" view.

**Negative / Edge Cases:**

- Attempting to deactivate an already inactive account → informational message "Account is already inactive".

**Data Entities:** Account  
**Screens:** Account List  
**Dependencies:** FR-004 (Project CRUD)

---

### FR-004 – Create Project

| Field          | Value                                                                                      |
| -------------- | ------------------------------------------------------------------------------------------ |
| Priority       | P0                                                                                         |
| Persona        | HR                                                                                         |
| Description    | HR can create a project linked to an existing active account and assign a Project Manager. |
| Trigger        | HR clicks "Add Project".                                                                   |
| Preconditions  | At least one active account exists.                                                        |
| Postconditions | Project record persisted; visible in project list grouped by account.                      |
| User Story     | As an HR, I want to create projects under accounts so teams can be staffed.                |

**Acceptance Criteria:**

- AC-004-1: Project Code must be unique (case-insensitive); error `ERR_PROJECT_CODE_EXISTS` on duplicate.
- AC-004-2: Account selection restricted to active accounts.
- AC-004-3: Project Manager field is optional at creation; must be an employee with role `ProjectManager`.
- AC-004-4: Start Date is required; End Date is optional.
- AC-004-5: End Date, if provided, must be ≥ Start Date; error "End Date must be on or after Start Date".
- AC-004-6: Project defaults to `status = Upcoming`, `isActive = true`.
- AC-004-7: `billable` boolean field — defaults to `true` when the linked account’s type is `Client`; defaults to `false` otherwise. HR can override explicitly during creation.

**Negative / Edge Cases:**

- Start Date in the past is allowed (historical projects).
- Assigning a non-PM employee as Project Manager → blocked.

**Data Entities:** Project, Account, Employee  
**Screens:** Project List, Add Project Modal/Form  
**Dependencies:** FR-001 (Account), FR-009 (Employee)

---

### FR-005 – Edit Project

| Field          | Value                                                                                                                                                        |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Priority       | P0                                                                                                                                                           |
| Persona        | HR, ProjectManager (PM scoped to own projects)                                                                                                               |
| Description    | HR or PM can update project name, manager, dates, status, and billable flag. Project Code is immutable post-creation. PM can only edit projects they manage. |
| Trigger        | HR or PM clicks "Edit" on a project row.                                                                                                                     |
| Preconditions  | Project exists. PM must be the assigned manager of the project.                                                                                              |
| Postconditions | Project record updated.                                                                                                                                      |
| User Story     | As an HR or PM, I want to keep project records (status, billable, dates) current.                                                                            |

**Acceptance Criteria:**

- AC-005-1: Project Code is read-only in edit mode.
- AC-005-2: All editable fields validated per FR-004 rules.
- AC-005-3: Changing status to `Completed` when active, future-dated allocations exist → warning displayed; user must confirm to proceed. Active allocations are not automatically ended.
- AC-005-4: Status lifecycle: `Upcoming → Active → Completed`. Direct transition from `Upcoming` to `Completed` is allowed.
- AC-005-5: `billable` flag is editable. Changing it does not affect existing allocations.

**Negative / Edge Cases:**

- Setting End Date earlier than an existing active allocation's To Date → warning (not block); allocation data is not modified.

**Data Entities:** Project, Account, Employee  
**Screens:** Project List, Edit Project Modal/Form  
**Dependencies:** FR-004

---

### FR-006 – Deactivate Project

| Field          | Value                                                                  |
| -------------- | ---------------------------------------------------------------------- |
| Priority       | P1                                                                     |
| Persona        | HR                                                                     |
| Description    | HR can deactivate a project. Historical allocations are preserved.     |
| Trigger        | HR clicks "Deactivate" on a project row.                               |
| Preconditions  | Project exists.                                                        |
| Postconditions | Project `isActive = false`; not selectable for future allocations.     |
| User Story     | As an HR, I want to retire projects without losing allocation history. |

**Acceptance Criteria:**

- AC-006-1: Confirmation required before deactivation.
- AC-006-2: Deactivated projects hidden from allocation creation dropdowns.
- AC-006-3: Deactivated projects visible in "Inactive" filter.

**Negative / Edge Cases:**

- Projects with active allocations can still be deactivated after confirmation; allocations are not terminated.

**Data Entities:** Project  
**Screens:** Project List  
**Dependencies:** FR-004

---

### FR-007 – Create Employee

| Field          | Value                                                                                           |
| -------------- | ----------------------------------------------------------------------------------------------- |
| Priority       | P0                                                                                              |
| Persona        | HR                                                                                              |
| Description    | HR can add a new employee with personal details, role, designation, skills, and reporting line. |
| Trigger        | HR clicks "Add Employee".                                                                       |
| Preconditions  | User is authenticated as HR.                                                                    |
| Postconditions | Employee record persisted; visible in employee list.                                            |
| User Story     | As an HR, I want to register employees so they can be allocated to projects.                    |

**Acceptance Criteria:**

- AC-007-1: `empCode` must be unique (case-insensitive); error `ERR_EMPCODE_EXISTS` on duplicate.
- AC-007-2: First Name and Last Name required; max 100 characters each.
- AC-007-3: Email required; must be valid email format; must be unique.
- AC-007-4: Role must be one of: `HR`, `ProjectManager`, `Staff`.
- AC-007-5: Designation required; max 150 characters.
- AC-007-6: `reportsTo` is optional; must reference an existing, active employee.
- AC-007-7: Skills are optional at creation; zero or more from a predefined list (searchable/taggable).
- AC-007-8: Employee defaults to `isActive = true`.

**Negative / Edge Cases:**

- Circular reporting chain (Employee A reports to B, B reports to A) → blocked; error `ERR_CIRCULAR_REPORTING`.
- Self-reference in `reportsTo` → blocked.

**Data Entities:** Employee, Skill, EmployeeSkill  
**Screens:** Employee List, Add Employee Modal/Form  
**Dependencies:** Skill master data

---

### FR-008 – Edit Employee

| Field          | Value                                                                                                                       |
| -------------- | --------------------------------------------------------------------------------------------------------------------------- |
| Priority       | P0                                                                                                                          |
| Persona        | HR                                                                                                                          |
| Description    | HR can modify employee details including role, designation, skills, and reporting line. empCode is immutable post-creation. |
| Trigger        | HR clicks "Edit" on an employee row.                                                                                        |
| Preconditions  | Employee exists and is active.                                                                                              |
| Postconditions | Employee record updated.                                                                                                    |
| User Story     | As an HR, I want to update employee information as their role or skills change.                                             |

**Acceptance Criteria:**

- AC-008-1: `empCode` is read-only in edit mode.
- AC-008-2: Role change from `ProjectManager` to `Staff` when employee manages active projects → show warning with list of impacted projects; user must confirm.
- AC-008-3: All other field validations same as FR-007.
- AC-008-4: Skills can be added or removed; removals do not affect existing allocations.

**Negative / Edge Cases:**

- Removing a skill that is currently used as a search filter by a PM does not break existing sessions.

**Data Entities:** Employee, Skill, EmployeeSkill  
**Screens:** Employee List, Edit Employee Modal/Form  
**Dependencies:** FR-007

---

### FR-009 – Deactivate Employee

| Field          | Value                                                                                               |
| -------------- | --------------------------------------------------------------------------------------------------- |
| Priority       | P1                                                                                                  |
| Persona        | HR                                                                                                  |
| Description    | HR can deactivate an employee. All active allocations for the employee must be reviewed beforehand. |
| Trigger        | HR clicks "Deactivate" on an employee row.                                                          |
| Preconditions  | Employee exists.                                                                                    |
| Postconditions | Employee `isActive = false`; not available for new allocations.                                     |
| User Story     | As HR, I want to offboard employees cleanly without data loss.                                      |

**Acceptance Criteria:**

- AC-009-1: If employee has active or future allocations, system displays a warning list; confirmation required.
- AC-009-2: Deactivation does NOT automatically end allocations; PM/HR must stop them separately.
- AC-009-3: Deactivated employees excluded from allocation search results.
- AC-009-4: Historical allocations for deactivated employee remain visible in reports/views.

**Negative / Edge Cases:**

- Deactivating an employee who is set as `reportsTo` for others → employees' `reportsTo` remains set but the manager is shown as inactive.

**Data Entities:** Employee  
**Screens:** Employee List  
**Dependencies:** FR-007, FR-013

---

### FR-010 – Create Allocation

| Field          | Value                                                                                                    |
| -------------- | -------------------------------------------------------------------------------------------------------- |
| Priority       | P0                                                                                                       |
| Persona        | HR, Project Manager                                                                                      |
| Description    | An authorized user can allocate an active employee to an active project for a time range and percentage. |
| Trigger        | User clicks "Allocate" or "Add Allocation" on the allocation screen.                                     |
| Preconditions  | Employee is active; project is active; user has allocation permissions.                                  |
| Postconditions | Allocation record persisted; employee capacity updated.                                                  |
| User Story     | As a Project Manager, I want to allocate an employee to my project with specific % and dates.            |

**Acceptance Criteria:**

- AC-010-1: `fromDate` is required.
- AC-010-2: `toDate` is optional (open-ended allocation); if provided, must be ≥ `fromDate`.
- AC-010-3: `percentage` must be ≥ configured minimum (default 25%); must be a multiple of the configured increment (default 5%); max 100%.
- AC-010-4: System computes the employee's total allocated % for any day within the new allocation's date range. The sum of all overlapping active allocations + new allocation must not exceed 100%. If exceeded: error `ERR_CAPACITY_EXCEEDED` with details (date of conflict, current total, requested %).
- AC-010-5: Project Manager can only allocate to projects they manage (where they are set as `projectManager`).
- AC-010-6: HR can allocate to any active project.
- AC-010-7: On success, allocation is immediately reflected in Employee View and Project View.

**Negative / Edge Cases:**

- Allocating 100% to an already-100%-allocated employee → blocked.
- `fromDate` = `toDate` (single day allocation) → allowed.
- Open-ended new allocation overlapping with open-ended existing allocation → system must assume indefinite overlap and block if sum > 100%.
- Allocation percentage = 0 → blocked; minimum enforced.
- Allocation to a Bench-type account/project → allowed; treated as a regular allocation for capacity purposes.

**Data Entities:** Allocation, Employee, Project  
**Screens:** Allocate Employee Modal/Form (from both Project View and Employee View)  
**Dependencies:** FR-007, FR-004, FR-011 (capacity engine)

---

### FR-011 – Allocation Capacity Engine

| Field          | Value                                                                                                                                                                                |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Priority       | P0                                                                                                                                                                                   |
| Persona        | System (internal)                                                                                                                                                                    |
| Description    | Given an employee and a date range, compute the maximum already-allocated percentage for any day in that range, accounting for all active (not soft-deleted, not ended) allocations. |
| Trigger        | Invoked by FR-010 (create), FR-012 (edit).                                                                                                                                           |
| Preconditions  | Allocations table is queryable.                                                                                                                                                      |
| Postconditions | Returns computed used capacity and available capacity for the range.                                                                                                                 |

**Acceptance Criteria:**

- AC-011-1: For each day d in [fromDate, toDate], sum all active allocations where `allocation.fromDate ≤ d ≤ allocation.toDate` (or `toDate IS NULL` for open-ended).
- AC-011-2: Return the maximum daily sum across all days in the range.
- AC-011-3: Capacity available = 100% − max daily sum.
- AC-011-4: Excludes the allocation being edited (for edit scenario).
- AC-011-5: Performance: Must respond within 300 ms for a single employee with up to 500 historical allocations.

**Negative / Edge Cases:**

- Employee with no allocations → available = 100%.
- Range of a single day → daily sum for that day.

**Data Entities:** Allocation  
**Dependencies:** None

---

### FR-012 – Edit Allocation

| Field          | Value                                                                                                       |
| -------------- | ----------------------------------------------------------------------------------------------------------- |
| Priority       | P0                                                                                                          |
| Persona        | HR, Project Manager                                                                                         |
| Description    | Authorized user can modify the percentage or date range of an existing active allocation.                   |
| Trigger        | User clicks "Edit" on an allocation row.                                                                    |
| Preconditions  | Allocation is active and not soft-deleted. Project Manager can only edit allocations on their own projects. |
| Postconditions | Allocation updated; capacity recalculated.                                                                  |
| User Story     | As a Project Manager, I want to adjust an employee's allocation % as project needs change.                  |

**Acceptance Criteria:**

- AC-012-1: All validation rules from FR-010 apply, excluding the current allocation being edited.
- AC-012-2: PM cannot edit allocations on projects they do not manage.
- AC-012-3: HR can edit any allocation.
- AC-012-4: Editing `fromDate` to a future date when the allocation has already started creates an audit note but is allowed (with confirmation).

**Negative / Edge Cases:**

- Editing a past-ended allocation → blocked; error "Cannot edit a completed allocation".

**Data Entities:** Allocation  
**Screens:** Allocation Edit Modal (from Project View and Employee View)  
**Dependencies:** FR-010, FR-011

---

### FR-013 – Stop Allocation

| Field          | Value                                                                                                                                                                    |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Priority       | P0                                                                                                                                                                       |
| Persona        | HR, Project Manager                                                                                                                                                      |
| Description    | Authorized user can stop an active allocation. The allocation's `toDate` is set to tomorrow if the allocation has already started, or to today if it hasn't started yet. |
| Trigger        | User clicks "Stop" on an allocation row.                                                                                                                                 |
| Preconditions  | Allocation is active. PM can only stop allocations on their own projects.                                                                                                |
| Postconditions | Allocation `toDate` updated; employee capacity freed from that date onward.                                                                                              |
| User Story     | As a Project Manager, I want to release an employee from a project from tomorrow.                                                                                        |

**Acceptance Criteria:**

- AC-013-1: If allocation `fromDate` > today → `toDate` is set to today (effectively cancels before it starts).
- AC-013-2: If allocation `fromDate` ≤ today and `toDate` is NULL or > tomorrow → `toDate` is set to tomorrow.
- AC-013-3: Confirmation dialog shown before stop: "Employee will be released from [Project Name] from [effective date]. Confirm?"
- AC-013-4: After stop, allocation remains visible in history with ended status.
- AC-013-5: PM cannot stop allocations on projects they do not manage.

**Negative / Edge Cases:**

- Allocation with `toDate` already = today or in the past → show informational "Allocation already ended".

**Data Entities:** Allocation  
**Screens:** Project View, Employee View  
**Dependencies:** FR-010

---

### FR-014 – Remove Past Allocation

| Field          | Value                                                                                                                    |
| -------------- | ------------------------------------------------------------------------------------------------------------------------ |
| Priority       | P1                                                                                                                       |
| Persona        | HR, Project Manager                                                                                                      |
| Description    | Authorized user can soft-delete a past (ended) allocation, removing it from the active list while preserving audit data. |
| Trigger        | User clicks "Remove" on a past allocation row.                                                                           |
| Preconditions  | Allocation `toDate` < today (it has ended). PM can only act on their own projects.                                       |
| Postconditions | Allocation `deletedAt` is set (soft-delete); hidden from default views.                                                  |
| User Story     | As a Project Manager, I want to clean up old allocations from the view.                                                  |

**Acceptance Criteria:**

- AC-014-1: Only allocations with `toDate < today` can be removed.
- AC-014-2: Soft-delete sets `deletedAt = now()`; record not physically deleted.
- AC-014-3: Soft-deleted allocations excluded from default list views but accessible via "Show Removed" toggle (HR only).
- AC-014-4: Removing an active or future allocation → blocked; error "Only ended allocations can be removed".

**Negative / Edge Cases:**

- PM attempts to remove an allocation from another PM's project → blocked with `ERR_UNAUTHORIZED`.

**Data Entities:** Allocation  
**Screens:** Project View, Employee View  
**Dependencies:** FR-013

---

### FR-015 – Employee Search (Name / empCode / Skill)

| Field          | Value                                                                                                                                                                                                         |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Priority       | P0                                                                                                                                                                                                            |
| Persona        | HR, Project Manager                                                                                                                                                                                           |
| Description    | User can search active employees by full name, partial name, empCode, or skill. Results display name, empCode, designation, availability %, and skills. Bench employees appear first when searching by skill. |
| Trigger        | User types in the employee search input field.                                                                                                                                                                |
| Preconditions  | User is authenticated as HR or Project Manager.                                                                                                                                                               |
| Postconditions | Filtered, sorted list of employees displayed.                                                                                                                                                                 |
| User Story     | As a Project Manager, I want to find available employees with specific skills quickly.                                                                                                                        |

**Acceptance Criteria:**

- AC-015-1: Search is triggered on ≥ 2 characters of input (debounced, 300 ms).
- AC-015-2: Name search: case-insensitive, partial match against `firstName + ' ' + lastName`.
- AC-015-3: empCode search: exact or prefix match (case-insensitive).
- AC-015-4: Skill search: returns employees who have the searched skill tagged. Results ordered: Bench employees first, then partially allocated (ascending %), then fully allocated last.
- AC-015-5: Search type is auto-detected: if input matches empCode format returns empCode result first; otherwise treats as name/skill search.
- AC-015-6: Each result card displays: Full Name, empCode, Designation, Availability % (computed for current date), Skills (comma-separated tags).
- AC-015-7: Deactivated employees excluded from results.
- AC-015-8: "Bench Only" toggle filters results to employees with 0% allocation for the selected date window (default: today).

**Negative / Edge Cases:**

- No results found → empty state message "No employees found matching your search".
- Search input cleared → show default list (all active employees, paginated).
- Date window for availability: defaults to today; PM can optionally set a date range to see projected availability.

**Data Entities:** Employee, Skill, Allocation  
**Screens:** Employee Search Panel (visible in Allocate flow and Employee View)  
**Dependencies:** FR-007, FR-011

---

### FR-016 – View Employee Allocation Status

| Field          | Value                                                                                                                                                                                          |
| -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Priority       | P0                                                                                                                                                                                             |
| Persona        | HR, Project Manager                                                                                                                                                                            |
| Description    | For a selected employee, the system displays their allocation status for a given date window: Bench, Partially Allocated, or Fully Allocated; plus a list of their current active allocations. |
| Trigger        | User clicks on an employee row / card.                                                                                                                                                         |
| Preconditions  | Employee exists and is active.                                                                                                                                                                 |
| Postconditions | Allocation status and detail panel rendered.                                                                                                                                                   |
| User Story     | As a Project Manager, I want to see an employee's current status and allocations before deciding to add them.                                                                                  |

**Acceptance Criteria:**

- AC-016-1: Status badge is computed for the selected date window (default: current date):
  - `Bench`: 0% allocated across the full window.
  - `Partial`: > 0% and < 100% allocated on any day in the window; badge shows "X% Available" where X = 100 − max_daily_allocated.
  - `Full`: 100% allocated on at least one day in the window; 0% available for that period.
- AC-016-2: Allocations list shows: Project Name, Account Code, Allocation %, From Date, To Date (or "Ongoing"), Role on project (if stored) — columns: Project, Account, %, From, To.
- AC-016-3: Only active (non-soft-deleted) allocations shown. PM sees only allocations on their own projects highlighted; all others greyed out (but still visible).
- AC-016-4: HR sees all allocations for the employee regardless of project ownership.

**Negative / Edge Cases:**

- Employee with no allocations → "Bench" status; empty allocations list with message "No active allocations".

**Data Entities:** Allocation, Project, Account, Employee  
**Screens:** Employee Detail Panel / Drawer  
**Dependencies:** FR-010, FR-011, FR-015

---

### FR-017 – Project View Dashboard

| Field          | Value                                                                                                                                                                                |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Priority       | P0                                                                                                                                                                                   |
| Persona        | HR, Project Manager                                                                                                                                                                  |
| Description    | A view listing all projects (PM sees only their managed projects; HR sees all). For each project, all current and future allocations are listed. Inline edit and stop are available. |
| Trigger        | User navigates to "Project View" tab.                                                                                                                                                |
| Preconditions  | User is authenticated.                                                                                                                                                               |
| Postconditions | Project-grouped allocation table rendered.                                                                                                                                           |
| User Story     | As a Project Manager, I want to see all my projects and who is allocated to each, and manage those allocations.                                                                      |

**Acceptance Criteria:**

- AC-017-1: Projects grouped by Account Code (collapsible sections).
- AC-017-2: For each project: Project Name, Project Code, Account, Status, and an allocations table.
- AC-017-3: Allocations table columns: Employee Name, empCode, Designation, %, From Date, To Date, Status (Active / Upcoming / Ended), Actions (Edit, Stop, Remove).
- AC-017-4: Default filter: show Active and Upcoming allocations. Toggle to show Ended allocations.
- AC-017-5: PM sees only projects where they are the assigned Project Manager.
- AC-017-6: HR sees all projects.
- AC-017-7: PM cannot edit, stop, or remove allocations on projects they do not manage (actions hidden/disabled).
- AC-017-8: "Add Allocation" button visible per project; opens allocation modal pre-filled with project.
- AC-017-9: Date range filter applies across all projects in the view.

**Negative / Edge Cases:**

- PM with no assigned projects → empty state "You have no assigned projects".
- Project with no allocations → shows project header with empty allocation table.

**Data Entities:** Project, Account, Allocation, Employee  
**Screens:** Project View  
**Dependencies:** FR-004, FR-010, FR-012, FR-013, FR-014

---

### FR-018 – Employee View Dashboard

| Field          | Value                                                                                                                                                              |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Priority       | P0                                                                                                                                                                 |
| Persona        | HR, Project Manager                                                                                                                                                |
| Description    | A view listing employees (PM sees all active employees; HR sees all). For each employee, all allocations are shown. Inline edit and stop available per allocation. |
| Trigger        | User navigates to "Employee View" tab.                                                                                                                             |
| Preconditions  | User is authenticated.                                                                                                                                             |
| Postconditions | Employee-grouped allocation table rendered.                                                                                                                        |
| User Story     | As a Project Manager, I want to see all employees and their allocations to manage capacity.                                                                        |

**Acceptance Criteria:**

- AC-018-1: Employees listed with: Name, empCode, Designation, Role, Availability %, Skills.
- AC-018-2: Expanding an employee row shows their allocation table: Project Name, Account Code, %, From, To, Status, Actions.
- AC-018-3: Default filter: active employees with at least one active/upcoming allocation + all bench employees. Toggle: show employees with ended-only allocations.
- AC-018-4: PM can edit/stop/remove only allocations on their own projects.
- AC-018-5: HR can edit/stop/remove any allocation.
- AC-018-6: "Allocate to Project" CTA available per employee row (PM sees only their projects in the project dropdown; HR sees all).
- AC-018-7: "Bench Only" toggle filters to employees with 0% allocation for the active date window.
- AC-018-8: Search bar (name, empCode, skill) filters the employee list in real time (same as FR-015).

**Negative / Edge Cases:**

- No employees → empty state message.

**Data Entities:** Employee, Allocation, Project, Account, Skill  
**Screens:** Employee View  
**Dependencies:** FR-007, FR-010, FR-015, FR-016

---

### FR-019 – Staff Allocation Dashboard

| Field          | Value                                                                                                                                                                                                                                                            |
| -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Priority       | P0                                                                                                                                                                                                                                                               |
| Persona        | Staff (Alex, Sam)                                                                                                                                                                                                                                                |
| MVP Phase      | MVP1                                                                                                                                                                                                                                                             |
| Description    | A staff employee logs in and sees their Allocation Dashboard: own current and upcoming allocations, and — if they are a Team Lead on any project — a project-wise read-only view of their reportees' allocations. No create, edit, stop, or remove capabilities. |
| Trigger        | Staff user authenticates and navigates to their dashboard.                                                                                                                                                                                                       |
| Preconditions  | User has role `Staff`.                                                                                                                                                                                                                                           |
| Postconditions | Staff Allocation Dashboard rendered.                                                                                                                                                                                                                             |
| User Story     | As a Staff employee, I want to see all my project allocations in one place; if I lead a team, I want to see each project's team members' allocations.                                                                                                            |

**Acceptance Criteria:**

- AC-019-1: Only the authenticated user's own allocations are shown in the "My Allocations" section.
- AC-019-2: Columns: Project Name, Account Code, Allocation %, From Date, To Date (or "Ongoing"), Status.
- AC-019-3: No Add, Edit, Stop, or Remove actions are available to Staff on any screen.
- AC-019-4: Staff cannot access Account, Project, or Employee management screens.
- AC-019-5: If the staff member is configured as a Team Lead for one or more projects (via FR-020), a **"My Team"** section is displayed below their own allocations.
- AC-019-6: My Team section shows one collapsible group per project on which the staff member has reportees. Each group lists: Employee Name, empCode, Designation, %, From Date, To Date, Status. All entries are read-only.
- AC-019-7: Staff members who are not a Team Lead on any project do not see the My Team section.
- AC-019-8: A staff member may be a Team Lead on Project A but not on Project B; only the relevant project group(s) appear.

**Negative / Edge Cases:**

- Staff with no allocations → "No current allocations" message.
- Staff who is a Team Lead but their reportees have no allocations → group shown with empty state "No allocations for this team yet".

**Data Entities:** Allocation, Employee, Project, Account, ProjectTeamMember (project-scoped reportee)  
**Screens:** Staff Allocation Dashboard  
**Dependencies:** FR-010, FR-020

---

### FR-020 – Configure Project-Scoped Team Lead / Reportees

| Field          | Value                                                                                                                                                                                                                                                                                                                                                  |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Priority       | P1                                                                                                                                                                                                                                                                                                                                                     |
| Persona        | HR, Project Manager                                                                                                                                                                                                                                                                                                                                    |
| MVP Phase      | MVP1                                                                                                                                                                                                                                                                                                                                                   |
| Description    | HR or PM can designate a Staff employee as a Team Lead within a specific project, and assign that project's team members as the Team Lead's reportees for that project only. Reportee relationships are project-scoped: a staff member may be a Team Lead on Project A with three reportees, and a regular contributor on Project B with no reportees. |
| Trigger        | PM or HR accesses the Team configuration panel on a project or from an employee's project membership detail.                                                                                                                                                                                                                                           |
| Preconditions  | The Team Lead candidate must be an active employee (any role). Reportees must be active employees allocated to the same project.                                                                                                                                                                                                                       |
| Postconditions | Project-scoped Team Lead and reportee mappings persisted; Team Lead's dashboard shows the new team group.                                                                                                                                                                                                                                              |
| User Story     | As a Project Manager, I want to assign a Team Lead for my project so they can track their team's allocations for that project.                                                                                                                                                                                                                         |

**Acceptance Criteria:**

- AC-020-1: PM can configure Team Lead and reportees only within their own projects.
- AC-020-2: HR can configure Team Lead and reportees for any project.
- AC-020-3: Team Lead assignment is scoped to a specific project; the logical relationship key is `(project_id, team_lead_id, reportee_id)`.
- AC-020-4: A staff member can be a Team Lead on multiple projects simultaneously.
- AC-020-5: A staff member can be a reportee under a Team Lead on one project while being a Team Lead themselves on another project.
- AC-020-6: Circular project-scoped reporting (A leads B, B leads A on the same project) → blocked.
- AC-020-7: Removing a reportee from a Team Lead's team does not affect any allocation records.
- AC-020-8: The `reportsTo` FK on the Employee entity remains as a global org-chart line; project-scoped Team Lead mappings are stored separately and do not override it.

**Negative / Edge Cases:**

- Configuring a reportee who is not yet allocated to that project → allowed; they may receive an allocation later.
- Deleting a project allocation for a reportee does not automatically remove the project-scoped team membership.

**Data Entities:** Employee, Project, ProjectTeamMember (new entity: `project_id`, `team_lead_id`, `reportee_id`)  
**Screens:** Project Detail → Team Configuration Panel; Employee Edit Modal  
**Dependencies:** FR-007, FR-008

---

### FR-021 – System Configuration

| Field          | Value                                                                                             |
| -------------- | ------------------------------------------------------------------------------------------------- |
| Priority       | P1                                                                                                |
| Persona        | HR (System Admin)                                                                                 |
| Description    | HR can configure system-level parameters: minimum allocation percentage and allocation increment. |
| Trigger        | HR navigates to Settings screen.                                                                  |
| Preconditions  | User is authenticated as HR.                                                                      |
| Postconditions | System config saved; all subsequent allocation validations use updated values.                    |
| User Story     | As HR, I want to set the minimum allocation % and increment to match our company policy.          |

**Acceptance Criteria:**

- AC-021-1: `minAllocationPercentage`: integer, 5–50, default 25.
- AC-021-2: `allocationIncrement`: integer, must be one of 5 or 10, default 5.
- AC-021-3: `minAllocationPercentage` must be a multiple of `allocationIncrement`.
- AC-021-4: Changes take effect for all new and edited allocations immediately.
- AC-021-5: Existing allocations are not retroactively validated against new config.

**Negative / Edge Cases:**

- Setting increment to 10 and min to 25 → blocked (25 is not a multiple of 10); error "Minimum must be a multiple of increment".

**Data Entities:** SystemConfig  
**Screens:** Settings Screen  
**Dependencies:** FR-010, FR-012

---

### FR-022 – PM Allocation Dashboard

| Field          | Value                                                                                                                                                                                                     |
| -------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Priority       | P0                                                                                                                                                                                                        |
| Persona        | Project Manager                                                                                                                                                                                           |
| MVP Phase      | MVP1                                                                                                                                                                                                      |
| Description    | A Project Manager has their own Allocation Dashboard showing their personal project allocations (as an allocatee). This is separate from the Projects Screen (FR-017) where they manage team allocations. |
| Trigger        | PM user authenticates and navigates to "My Allocations".                                                                                                                                                  |
| Preconditions  | User has role `ProjectManager`.                                                                                                                                                                           |
| Postconditions | PM's own allocation list rendered.                                                                                                                                                                        |
| User Story     | As a Project Manager, I want to see my own project allocations just like any staff member, separate from the projects I manage.                                                                           |

**Acceptance Criteria:**

- AC-022-1: Displays only the authenticated PM's own allocations (as an allocatee, not as a manager).
- AC-022-2: Columns: Project Name, Account Code, Allocation %, From Date, To Date (or "Ongoing"), Status.
- AC-022-3: No edit, stop, or remove actions on this screen.
- AC-022-4: PM can navigate from My Allocations to the Projects Screen (FR-017) and vice versa.
- AC-022-5: If the PM is also configured as a Team Lead on any project, a My Team section appears (project-wise) identical in behavior to FR-019 AC-019-6.

**Negative / Edge Cases:**

- PM with no personal allocations → "You have no current allocations" message.

**Data Entities:** Allocation, Employee, Project, Account  
**Screens:** PM Allocation Dashboard (My Allocations)  
**Dependencies:** FR-010, FR-019

---

### FR-023 – Employee Self-Manage Skills _(MVP2)_

| Field          | Value                                                                                                                                        |
| -------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| Priority       | P1                                                                                                                                           |
| Persona        | Staff, Project Manager                                                                                                                       |
| MVP Phase      | MVP2                                                                                                                                         |
| Description    | An authenticated employee (Staff or PM) can add skills to their own profile, edit existing skill tags, and remove skills from their profile. |
| Trigger        | User navigates to "My Profile" → "Skills" section.                                                                                           |
| Preconditions  | User is authenticated. Skills to be added must exist in the master skill list (HR-managed).                                                  |
| Postconditions | Employee's skill list updated; reflected in search results and employee detail views.                                                        |
| User Story     | As an employee, I want to keep my skills up to date so that PMs can discover me for relevant projects.                                       |

**Acceptance Criteria:**

- AC-023-1: Employee can add one or more skills from the master skill list via a searchable type-ahead input.
- AC-023-2: Employee cannot create new skill entries; they may only tag from the existing HR-managed master list.
- AC-023-3: Employee can remove a skill tag from their own profile.
- AC-023-4: Skill changes are immediately reflected in search and filter results (FR-015).
- AC-023-5: HR retains the ability to add/remove skills on any employee (FR-007, FR-008 unchanged).
- AC-023-6: Removing a skill does not affect any allocation records.

**Negative / Edge Cases:**

- Attempting to add an inactive skill → blocked; error "This skill is no longer active".
- Adding a skill already tagged on the profile → ignored silently or informational message "Skill already added".

**Data Entities:** Employee, Skill, EmployeeSkill  
**Screens:** My Profile → Skills Panel  
**Dependencies:** FR-007, FR-008, Skill master data

---

## 9. Non-Functional Requirements

### Performance

| ID     | Requirement                                                                                       |
| ------ | ------------------------------------------------------------------------------------------------- |
| NFR-01 | Employee search results returned within 500 ms for datasets up to 5,000 employees.                |
| NFR-02 | Allocation capacity check (FR-011) completes within 300 ms per employee.                          |
| NFR-03 | Project View and Employee View dashboards load within 2 seconds for up to 200 projects.           |
| NFR-04 | API endpoints respond with P95 latency ≤ 1 second under normal load (up to 100 concurrent users). |

### Security

| ID     | Requirement                                                                                                                                                           |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-05 | All API endpoints require authentication; unauthenticated requests return HTTP 401.                                                                                   |
| NFR-06 | Role-based authorization enforced server-side; client-side hiding is supplementary only.                                                                              |
| NFR-07 | PM-scoped operations (edit/stop/remove allocation) validate project ownership server-side.                                                                            |
| NFR-08 | All sensitive data transmitted over HTTPS.                                                                                                                            |
| NFR-09 | No passwords stored in PAMS. Authentication is fully delegated to Keycloak. PAMS validates Keycloak-issued OIDC Bearer tokens only; token tampering returns HTTP 401. |
| NFR-10 | Audit log entries created for all create/update/delete operations on core entities.                                                                                   |

### Reliability

| ID     | Requirement                                                                          |
| ------ | ------------------------------------------------------------------------------------ |
| NFR-11 | System availability target: 99.5% uptime during business hours.                      |
| NFR-12 | Database transactions used for allocation creation/update to prevent partial writes. |

### Usability

| ID     | Requirement                                                                                 |
| ------ | ------------------------------------------------------------------------------------------- |
| NFR-13 | All primary actions (allocate, stop, search) accessible within 3 clicks from the dashboard. |
| NFR-14 | Inline validation errors shown adjacent to fields; not solely in a toast notification.      |
| NFR-15 | Empty states must include actionable guidance text.                                         |

### Observability

| ID     | Requirement                                                                              |
| ------ | ---------------------------------------------------------------------------------------- |
| NFR-16 | Structured server-side logging for all API errors (4xx, 5xx) including request details.  |
| NFR-17 | Allocation capacity check failures logged with employee ID and computed capacity values. |

### API Design Standards

| ID     | Requirement                                                                                                                                                                                                                                                                                        |
| ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-18 | **Pagination (mandatory):** All list endpoints must support `page` (integer, min 1, default **1**) and `limit` (integer, min 1, max 100, default **10**) query parameters. Every paginated response must include a `pagination` object with fields: `page`, `limit`, `totalRecords`, `totalPages`. |
| NFR-19 | **Pagination defaults:** If `page` and/or `limit` are omitted the server applies defaults (page=1, limit=10). Unbounded list responses are prohibited.                                                                                                                                             |
| NFR-20 | **Single-endpoint strategy:** Each resource uses one endpoint for listing, searching, and filtering. Separate `/search` endpoints must not be created. Filtering is via query parameters e.g. `GET /employees?search=John&role=Staff&page=1&limit=10`.                                             |
| NFR-21 | **Role-based response shaping:** API responses must be filtered server-side based on the authenticated user's role extracted from the Keycloak token. Client-side role checking is supplementary only.                                                                                             |

**Standard paginated response envelope:**

```json
{
  "data": [],
  "pagination": {
    "page": 1,
    "limit": 10,
    "totalRecords": 125,
    "totalPages": 13
  }
}
```

---

## 10. Data Model Overview

### Account

| Attribute   | Type          | Required | Notes                            |
| ----------- | ------------- | -------- | -------------------------------- |
| accountId   | UUID / int PK | Yes      | System-generated                 |
| accountCode | varchar(50)   | Yes      | Unique, immutable after creation |
| accountName | varchar(150)  | Yes      |                                  |
| accountType | enum          | Yes      | Client, Internal, Bench          |
| isActive    | bool          | Yes      | Default true                     |
| createdAt   | datetime      | Yes      | System-set                       |
| updatedAt   | datetime      | Yes      | System-set                       |

**Computed Fields (read-only, not stored):**

| Field                  | Description                                                                                     |
| ---------------------- | ----------------------------------------------------------------------------------------------- |
| totalActiveProjects    | Count of projects linked to this account where `isActive = true`                                |
| totalInactiveProjects  | Count of projects linked to this account where `isActive = false`                               |
| totalActiveEmployees   | Count of distinct active employees currently allocated to any active project under this account |
| totalInactiveEmployees | Count of distinct inactive employees with past allocations under this account                   |

**Relationships:** One Account → Many Projects

---

### Project

| Attribute        | Type          | Required | Notes                            |
| ---------------- | ------------- | -------- | -------------------------------- |
| projectId        | UUID / int PK | Yes      | System-generated                 |
| projectCode      | varchar(50)   | Yes      | Unique, immutable after creation |
| projectName      | varchar(150)  | Yes      |                                  |
| accountId        | FK → Account  | Yes      |                                  |
| projectManagerId | FK → Employee | No       | Must be role = ProjectManager    |
| startDate        | date          | Yes      |                                  |
| endDate          | date          | No       | Must be ≥ startDate if provided  |
| status           | enum          | Yes      | Upcoming, Active, Completed      |
| billable         | bool          | Yes      | Default: true for Client acct    |
| isActive         | bool          | Yes      | Default true                     |
| createdAt        | datetime      | Yes      |                                  |
| updatedAt        | datetime      | Yes      |                                  |

**Relationships:** Many Projects → One Account; One Project → Many Allocations; One Project → One Employee (PM)

---

### Employee

| Attribute   | Type          | Required | Notes                            |
| ----------- | ------------- | -------- | -------------------------------- |
| employeeId  | UUID / int PK | Yes      | System-generated                 |
| empCode     | varchar(50)   | Yes      | Unique, immutable after creation |
| firstName   | varchar(100)  | Yes      |                                  |
| lastName    | varchar(100)  | Yes      |                                  |
| email       | varchar(254)  | Yes      | Unique, valid email format       |
| designation | varchar(150)  | Yes      |                                  |
| role        | enum          | Yes      | HR, ProjectManager, Staff        |
| reportsTo   | FK → Employee | No       | Nullable; no circular chains     |
| isActive    | bool          | Yes      | Default true                     |
| createdAt   | datetime      | Yes      |                                  |
| updatedAt   | datetime      | Yes      |                                  |

**Relationships:** Employee → Many EmployeeSkills; Employee → Many Allocations; Employee may have Many Reportees (inverse of reportsTo)

---

### Skill

| Attribute | Type          | Required | Notes                    |
| --------- | ------------- | -------- | ------------------------ |
| skillId   | UUID / int PK | Yes      | System-generated         |
| skillName | varchar(100)  | Yes      | Unique, case-insensitive |
| isActive  | bool          | Yes      | Default true             |

---

### EmployeeSkill (Join Table)

| Attribute  | Type          | Required | Notes |
| ---------- | ------------- | -------- | ----- |
| employeeId | FK → Employee | Yes      |       |
| skillId    | FK → Skill    | Yes      |       |

**Cardinality:** Many-to-Many (Employee ↔ Skill)

---

### Allocation

| Attribute     | Type          | Required | Notes                                         |
| ------------- | ------------- | -------- | --------------------------------------------- |
| allocationId  | UUID / int PK | Yes      | System-generated                              |
| employeeId    | FK → Employee | Yes      |                                               |
| projectId     | FK → Project  | Yes      |                                               |
| fromDate      | date          | Yes      |                                               |
| toDate        | date          | No       | Null = open-ended                             |
| percentage    | int           | Yes      | 1–100; constrained by system config           |
| allocatedById | FK → Employee | Yes      | Who created the allocation                    |
| isActive      | bool          | Yes      | Default true; false = soft-stopped via toDate |
| deletedAt     | datetime      | No       | Null = not removed; set = soft-deleted        |
| createdAt     | datetime      | Yes      |                                               |
| updatedAt     | datetime      | Yes      |                                               |

**Indexes:** (employeeId, fromDate, toDate) for capacity computation; (projectId) for project view.  
**Relationships:** Many Allocations → One Employee; Many Allocations → One Project

---

### SystemConfig

| Attribute   | Type         | Required | Notes                               |
| ----------- | ------------ | -------- | ----------------------------------- |
| configKey   | varchar(100) | Yes      | Unique key, e.g. `minAllocationPct` |
| configValue | varchar(255) | Yes      | Stored as string; parsed by domain  |

**Seed Data:**

- `minAllocationPct` = `25`
- `allocationIncrement` = `5`

---

## 11. User Journeys

### UJ-01 – Project Manager Allocates an Employee

| Step | Actor     | Action                                                                    |
| ---- | --------- | ------------------------------------------------------------------------- |
| 1    | Ravi (PM) | Navigates to Employee View                                                |
| 2    | Ravi      | Types "Android" in the search box                                         |
| 3    | System    | Returns employees with Android skill; bench employees listed first        |
| 4    | Ravi      | Clicks on "Alex Chen" (bench, 0% allocated)                               |
| 5    | System    | Shows Alex's allocation status: Bench; no current allocations             |
| 6    | Ravi      | Clicks "Allocate to Project"                                              |
| 7    | System    | Opens Allocate modal; Project dropdown pre-filtered to Ravi's projects    |
| 8    | Ravi      | Selects project, sets 50%, sets From: 2026-03-01, To: 2026-06-30          |
| 9    | System    | Validates: 50% + 0% existing = 50% ≤ 100%; validation passes              |
| 10   | Ravi      | Clicks Save                                                               |
| 11   | System    | Persists allocation; employee status updates to "Partial – 50% Available" |

**Alternate Flow:** At Step 9, if Alex already has a 75% allocation → system shows `ERR_CAPACITY_EXCEEDED`: "Alex is already 75% allocated on 2026-03-01. Maximum available: 25%."

**Postconditions:** Alex allocated; visible in Project View under Ravi's project.

---

### UJ-02 – HR Onboards a New Employee

| Step | Actor     | Action                                                                            |
| ---- | --------- | --------------------------------------------------------------------------------- |
| 1    | Emma (HR) | Navigates to Employees → Add Employee                                             |
| 2    | Emma      | Fills: empCode=EMP042, Name=Jordan Lee, email, designation=Senior Dev, role=Staff |
| 3    | Emma      | Adds skills: Java, Spring Boot                                                    |
| 4    | Emma      | Sets reportsTo = Sam (Staff Lead)                                                 |
| 5    | Emma      | Submits form                                                                      |
| 6    | System    | Validates uniqueness; persists record                                             |
| 7    | System    | Jordan appears in employee list; 0% allocated (Bench)                             |

---

### UJ-03 – Staff Employee Views Own Allocations and Team (Team Lead scenario)

| Step | Actor  | Action                                                                                            |
| ---- | ------ | ------------------------------------------------------------------------------------------------- |
| 1    | Alex   | Logs in                                                                                           |
| 2    | System | Identifies role = Staff; renders Staff Allocation Dashboard                                       |
| 3    | Alex   | Sees "My Allocations" section: Project Alpha / Internal / 50% / Mar 1 – Jun 30 / Active           |
| 4    | Alex   | (If configured as Team Lead on Project Alpha) Sees "My Team" section with group: "Project Alpha"  |
| 5    | Alex   | Expands "Project Alpha" group; sees read-only rows for each reportee's allocation on that project |
| 6    | Alex   | (If NOT a Team Lead on any project) My Team section is not shown                                  |

**Alternate Flow:** Alex is a Team Lead on Project Alpha but not on Project Beta. My Team section shows only a "Project Alpha" group; Project Beta does not appear.

**Postconditions:** Alex has visibility into their own allocations and, if applicable, their project-scoped team's allocations.

---

### UJ-04 – Project Manager Stops an Allocation

| Step | Actor  | Action                                                                                        |
| ---- | ------ | --------------------------------------------------------------------------------------------- |
| 1    | Ravi   | Opens Project View; locates allocation for Alex on Project Alpha                              |
| 2    | Ravi   | Clicks "Stop"                                                                                 |
| 3    | System | Shows confirmation: "Alex Chen will be released from Project Alpha from 2026-02-26. Confirm?" |
| 4    | Ravi   | Confirms                                                                                      |
| 5    | System | Sets `toDate = 2026-02-26`; Alex now Bench from 2026-02-27                                    |

---

## 12. UX & UI Requirements

### Screen: Account List

| Component     | Details                                                          |
| ------------- | ---------------------------------------------------------------- |
| Table columns | Account Code, Account Name, Type, Status, Actions                |
| Filters       | Status (Active / Inactive / All)                                 |
| Actions       | Add Account (button), Edit (row action), Deactivate (row action) |
| Empty state   | "No accounts found. Click 'Add Account' to create one."          |

---

### Screen: Project List

| Component     | Details                                                                          |
| ------------- | -------------------------------------------------------------------------------- |
| Grouping      | Grouped by Account Code (collapsible sections)                                   |
| Table columns | Project Code, Project Name, PM, Start Date, End Date, Status, Billable, Actions  |
| Filters       | Status (Upcoming / Active / Completed / All), Account, Billable (Yes / No / All) |
| Actions       | Add Project, Edit (row), Deactivate (row)                                        |
| Empty state   | "No projects. Add a project to get started."                                     |

---

### Screen: Employee List

| Component      | Details                                                                          |
| -------------- | -------------------------------------------------------------------------------- |
| Table columns  | empCode, Name, Designation, Role, Skills (tags), Availability %, Status, Actions |
| Filters        | Role, Status, Bench Only toggle                                                  |
| Search         | Name / empCode / Skill (debounced, ≥ 2 chars)                                    |
| Actions        | Add Employee, Edit (row), Deactivate (row)                                       |
| Availability % | Computed badge: green (Bench), amber (Partial), red (Full)                       |

---

### Screen: Project View (PM / HR Dashboard)

| Component           | Details                                                                      |
| ------------------- | ---------------------------------------------------------------------------- |
| Layout              | Collapsible project cards grouped by account                                 |
| Per project         | Project name, code, account, status, PM name                                 |
| Allocation table    | Employee Name, empCode, Designation, %, From, To, Allocation Status, Actions |
| Allocation statuses | Active (green), Upcoming (blue), Ended (grey)                                |
| Actions per row     | Edit, Stop, Remove (contextual per role/ownership)                           |
| Header actions      | Add Allocation (per project), Date Range filter, Status filter toggle        |

---

### Screen: Employee View (PM / HR Dashboard)

| Component           | Details                                                            |
| ------------------- | ------------------------------------------------------------------ |
| Layout              | Expandable employee rows                                           |
| Per employee header | Name, empCode, Designation, Availability % badge, Skills tags      |
| Expanded section    | Allocation table: Project, Account, %, From, To, Status, Actions   |
| Top bar             | Search (name/empCode/skill), Bench Only toggle, Date window picker |
| CTA per employee    | "Allocate to Project" button                                       |

---

### Screen: Allocate Employee Modal

| Component     | Details                                                                                                                                              |
| ------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| Employee info | Read-only: Name, empCode, Designation, Availability %, Skills                                                                                        |
| Fields        | Project (dropdown, filtered by permission), From Date (date picker), To Date (date picker, optional), Percentage (step input, constrained by config) |
| Validation    | Real-time capacity indicator: "X% available in selected window"                                                                                      |
| Capacity bar  | Visual bar showing current allocation + proposed allocation; goes red if > 100%                                                                      |
| CTA           | Save Allocation, Cancel                                                                                                                              |

---

### Screen: Staff Allocation Dashboard

| Component             | Details                                                                                                                   |
| --------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| My Allocations        | Table: Project Name, Account Code, %, From Date, To Date (or "Ongoing"), Status badge (Active / Upcoming / Ended)         |
| My Team (conditional) | Shown only if the staff member is a Team Lead on at least one project. Hidden entirely if no Team Lead assignments exist. |
| My Team – per project | One collapsible group per project where the staff member has reportees. Group header: Project Name, Account Code.         |
| My Team – columns     | Employee Name, empCode, Designation, %, From Date, To Date, Status. All rows read-only; no action buttons.                |
| No write actions      | All Add, Edit, Stop, Remove buttons hidden for Staff role.                                                                |

### Screen: PM Allocation Dashboard

| Component             | Details                                                                                    |
| --------------------- | ------------------------------------------------------------------------------------------ |
| My Allocations        | Same structure as Staff Allocation Dashboard — shows PM's own allocations as an allocatee. |
| My Team (conditional) | Same as Staff: shown only if PM is also a Team Lead on any project.                        |
| Navigation            | Clear link / tab to "My Projects" (Projects Screen FR-017) for managing team allocations.  |

---

### Notifications / Inline Feedback

| Event                           | Feedback                                                           |
| ------------------------------- | ------------------------------------------------------------------ |
| Successful save                 | Toast: "Saved successfully" (3s)                                   |
| Capacity exceeded               | Inline error in modal: "Cannot allocate. Capacity exceeded by X%." |
| Duplicate code                  | Inline field error below the input                                 |
| Stop allocation confirmation    | Modal dialog with summary                                          |
| Deactivation confirmation       | Modal dialog with consequences listed                              |
| Navigation with unsaved changes | Browser-level or app-level prompt                                  |

---

## 13. Integration Requirements

| Integration       | Direction       | Details                                                                                                                                                                                                                                                                                                                 | Failure Handling                                                                                                                                    |
| ----------------- | --------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| Authentication    | Keycloak → PAMS | OAuth2/OIDC. Keycloak is the Authorization Server and identity provider. PAMS is a resource server that validates Bearer tokens via Keycloak's JWKS endpoint. Roles (`HR`, `ProjectManager`, `Staff`) are mapped from Keycloak realm roles. Future migration path: swap Keycloak Authority URL for Azure AD / Entra ID. | Token validation failure returns HTTP 401; client redirects to Keycloak login page. Keycloak unavailability must not block read-only health checks. |
| Audit Log         | PAMS → Store    | Structured log entries for all mutations; written synchronously                                                                                                                                                                                                                                                         | Log failure must not abort primary operation but must be alerted                                                                                    |
| Skill Master Data | Internal CRUD   | Managed within PAMS; no external integration in MVP                                                                                                                                                                                                                                                                     | N/A                                                                                                                                                 |

---

## 14. Constraints & Assumptions

### Constraints

- C-01: Single tenant system for MVP.
- C-02: English language only in MVP.
- C-03: No mobile application in MVP.
- C-04: Technology stack and framework selection are delegated to the ProductArchitect.
- C-05: Allocation percentage is always a whole integer (no decimals).

### Assumptions

- A-01: Each employee has exactly one role (HR, ProjectManager, or Staff); roles do not overlap.
- A-02: An employee with role `ProjectManager` is not excluded from being allocated to projects.
- A-03: The `Bench` account type is a special reserved account used to represent employees who are on bench; projects under the Bench account behave like regular projects for allocation purposes.
- A-04: Keycloak is the designated identity provider. PAMS receives and validates OIDC Bearer tokens issued by Keycloak. PAMS stores no passwords. The Keycloak realm, client (`pams-api`), and role mappings (`HR`, `ProjectManager`, `Staff`) must be provisioned before PAMS can authenticate any user.
- A-05: A skill is a single string tag (e.g., "Android", "Java"); no skill hierarchy in MVP.
- A-06: Date range computation uses calendar days; weekends and holidays are not excluded.
- A-07: "Today" is computed from server time (UTC or configured timezone—architect decision).
- A-08: Multiple PMs can manage the same project if needed (future consideration); for MVP, a project has one PM field.
- A-09: An HR user can also be allocated to a project; their HR role does not prevent allocation.
- A-10: Project-scoped Team Lead / reportee mappings are stored separately from the global `reportsTo` FK on Employee. Both coexist: `reportsTo` represents the org-chart hierarchy; project-scoped mappings represent project team structure.
- A-11: A staff member can be a Team Lead on some projects and a regular contributor on others simultaneously.
- A-12: In MVP1, a PM is linked to a project via the single `projectManagerId` field. MVP2 will introduce PM-to-multiple-accounts association.

---

## 15. Risks & Mitigation

| ID   | Risk                                                             | Category  | Probability | Impact | Mitigation                                                                                      |
| ---- | ---------------------------------------------------------------- | --------- | ----------- | ------ | ----------------------------------------------------------------------------------------------- |
| R-01 | Capacity engine returns stale data under concurrent allocation   | Technical | Medium      | High   | Use database-level locking or serializable transactions during allocation writes                |
| R-02 | PM scope validation bypassed via API manipulation                | Security  | Low         | High   | Enforce ownership checks in service/domain layer, not just UI                                   |
| R-03 | User adoption low due to complex allocation workflow             | Adoption  | Medium      | Medium | Simplify UX via inline capacity indicators and pre-filled forms                                 |
| R-04 | Data migration from existing spreadsheets introduces bad data    | Technical | High        | Medium | Define import validation rules; HR reviews before go-live                                       |
| R-05 | Skill master data becomes stale / inconsistent                   | Business  | Medium      | Low    | HR manages skill list centrally; deactivation of unused skills                                  |
| R-06 | Soft-delete accumulation degrades query performance over time    | Technical | Low         | Medium | Index `deletedAt`; archive old soft-deleted records after configurable period                   |
| R-07 | Unclear timezone handling causes date-boundary allocation errors | Technical | Medium      | High   | Define server timezone explicitly; use date-only types (no time component) for allocation dates |

---

## 16. Future Enhancements (POST-MVP)

| ID    | Feature                                                                                                                 |
| ----- | ----------------------------------------------------------------------------------------------------------------------- |
| FE-01 | Email/Slack notifications when an employee is allocated, stopped, or approaching bench                                  |
| FE-02 | Visual Gantt-style timeline view per project or employee                                                                |
| FE-03 | Leave / holiday calendar integration for more accurate availability computation                                         |
| FE-04 | Export to CSV / Excel for allocation reports                                                                            |
| FE-05 | Multi-PM support on a single project (PM ↔ multiple accounts ships in MVP2; full multi-PM per project remains post-MVP) |
| FE-06 | Historical allocation analytics and utilization reports                                                                 |
| FE-07 | Client portal (read-only) showing headcount on their account                                                            |
| FE-08 | SSO / SAML / OAuth2 integration                                                                                         |
| FE-09 | Bulk allocation import via CSV                                                                                          |
| FE-10 | Mobile-responsive PWA                                                                                                   |

---

## 17. FR-ID Master Index

| FR-ID  | Title                                          | Priority | Persona  | Screens                                        | Status |
| ------ | ---------------------------------------------- | -------- | -------- | ---------------------------------------------- | ------ |
| FR-001 | Create Account                                 | P0       | HR       | Account List, Add Account Form                 | Draft  |
| FR-002 | Edit Account                                   | P0       | HR       | Account List, Edit Account Form                | Draft  |
| FR-003 | Deactivate Account                             | P1       | HR       | Account List                                   | Draft  |
| FR-004 | Create Project                                 | P0       | HR       | Project List, Add Project Form                 | Draft  |
| FR-005 | Edit Project                                   | P0       | HR, PM   | Project List, Edit Project Form                | Draft  |
| FR-006 | Deactivate Project                             | P1       | HR       | Project List                                   | Draft  |
| FR-007 | Create Employee                                | P0       | HR       | Employee List, Add Employee Form               | Draft  |
| FR-008 | Edit Employee                                  | P0       | HR       | Employee List, Edit Employee Form              | Draft  |
| FR-009 | Deactivate Employee                            | P1       | HR       | Employee List                                  | Draft  |
| FR-010 | Create Allocation                              | P0       | HR, PM   | Allocate Modal                                 | Draft  |
| FR-011 | Allocation Capacity Engine                     | P0       | System   | Internal                                       | Draft  |
| FR-012 | Edit Allocation                                | P0       | HR, PM   | Project View, Employee View                    | Draft  |
| FR-013 | Stop Allocation                                | P0       | HR, PM   | Project View, Employee View                    | Draft  |
| FR-014 | Remove Past Allocation                         | P1       | HR, PM   | Project View, Employee View                    | Draft  |
| FR-015 | Employee Search                                | P0       | HR, PM   | Employee View, Allocate Modal                  | Draft  |
| FR-016 | View Employee Allocation Status                | P0       | HR, PM   | Employee Detail Drawer                         | Draft  |
| FR-017 | Project View Dashboard                         | P0       | HR, PM   | Project View                                   | Draft  |
| FR-018 | Employee View Dashboard                        | P0       | HR, PM   | Employee View                                  | Draft  |
| FR-019 | Staff Allocation Dashboard                     | P0       | Staff    | Staff Allocation Dashboard                     | Draft  |
| FR-020 | Configure Project-Scoped Team Lead / Reportees | P1       | HR, PM   | Project Detail Team Panel, Employee Edit Modal | Draft  |
| FR-021 | System Configuration                           | P1       | HR       | Settings Screen                                | Draft  |
| FR-022 | PM Allocation Dashboard _(MVP1)_               | P0       | PM       | PM Allocation Dashboard                        | Draft  |
| FR-023 | Employee Self-Manage Skills _(MVP2)_           | P1       | Staff,PM | My Profile → Skills Panel                      | Draft  |
