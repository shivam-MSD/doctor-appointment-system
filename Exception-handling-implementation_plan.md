# Implementation Plan: Custom Exceptions & Global Exception Handling

We will add a structured hierarchy of custom exceptions in the Domain layer (`Domain/Exceptions`) and create a global exception handler (`Middleware/GlobalExceptionHandler.cs`) to catch them and return standardized RFC-7807 Problem Details.

## User Review Required

### Development Sequence: Logic vs. Authentication
You asked: **"do we need to implement authentication at the first and then implement the logic? or first implement the logic and then do authentication."**

> [!TIP]
> **Recommendation: Implement Business Logic First**
> 1. **Focus on Domain and Application Logic first:** Define your domain rules, service contracts, data persistence, validation, and custom exceptions. This allows you to build a fully functional core that is easy to test in isolation without being blocked or slowed down by authentication tokens, role checks, and JWT plumbing.
> 2. **Layer Authentication afterwards:** Once the core flow works, add ASP.NET Core Identity/JWT authentication middleware to secure the API. It is much easier to protect endpoints that are already written and tested, rather than trying to build business logic around a complex, partially functioning security layout.

---

## Proposed Changes

### Domain Layer (Exceptions)

We will create a structured exception hierarchy:
- `BaseException`: Abstract base class extending `Exception`, containing `HttpStatusCode StatusCode`, `string Title`, and custom detail properties.
- Base Categories:
  - `NotFoundException` (sets status to `404 Not Found`)
  - `BadRequestException` (sets status to `400 Bad Request`)
  - `ConflictException` (sets status to `409 Conflict`)
  - `UnauthorizedException` (sets status to `401 Unauthorized`)
  - `ForbiddenException` (sets status to `403 Forbidden`)

We will group entity-specific exceptions inside cohesive files to keep the directory clean:

#### [NEW] [BaseException.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Domain/Exceptions/BaseException.cs)
Defines `BaseException` and common category exceptions (`NotFoundException`, `BadRequestException`, `ConflictException`, `UnauthorizedException`, `ForbiddenException`).

#### [NEW] [UserExceptions.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Domain/Exceptions/UserExceptions.cs)
- `UserNotFoundException` (404)
- `EmailAlreadyExistsException` (409)
- `InvalidCredentialsException` (400)

#### [NEW] [DoctorExceptions.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Domain/Exceptions/DoctorExceptions.cs)
- `DoctorNotFoundException` (404)
- `DoctorNotVerifiedException` (403)

#### [NEW] [PatientExceptions.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Domain/Exceptions/PatientExceptions.cs)
- `PatientNotFoundException` (404)

#### [NEW] [AppointmentExceptions.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Domain/Exceptions/AppointmentExceptions.cs)
- `AppointmentNotFoundException` (404)
- `AppointmentSlotNotAvailableException` (409)
- `InvalidAppointmentStateException` (400) (e.g., trying to cancel an already completed appointment)

#### [NEW] [ScheduleExceptions.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Domain/Exceptions/ScheduleExceptions.cs)
- `ScheduleNotFoundException` (404)
- `ScheduleConflictException` (409)
- `InvalidTimeRangeException` (400)

#### [NEW] [SpecializationExceptions.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Domain/Exceptions/SpecializationExceptions.cs)
- `SpecializationNotFoundException` (404)

---

### Web API Layer (Middleware & Setup)

#### [NEW] [GlobalExceptionHandler.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Middleware/GlobalExceptionHandler.cs)
Implements standard .NET `IExceptionHandler` middleware. It captures thrown exceptions, logs them, maps `BaseException` subtypes to their corresponding HTTP status codes, and outputs RFC-7807 standard JSON (`application/problem+json`).

#### [MODIFY] [Program.cs](file:///d:/shivam/doctor-appointment-system/DoctorAppointmentSystem/DoctorAppointmentSystem/Program.cs)
- Add `builder.Services.AddExceptionHandler<GlobalExceptionHandler>();`
- Add `builder.Services.AddProblemDetails();`
- Add `app.UseExceptionHandler();`

---

## Verification Plan

### Automated Build Verification
We will build the project using dotnet CLI to verify there are no compilation errors:
- Command: `dotnet build` from `d:\shivam\doctor-appointment-system\DoctorAppointmentSystem`

### Manual Verification / Validation
We will create a temporary test controller or endpoint to verify that throwing a custom exception (e.g., `UserNotFoundException`) returns:
- Correct Status Code (e.g., `404`)
- Standardized Problem Details response schema:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "User Not Found",
  "status": 404,
  "detail": "User with ID '...' was not found.",
  "instance": "/api/users/..."
}
```
- Verify logging of internal errors correctly outputting stacks to standard logger output.
