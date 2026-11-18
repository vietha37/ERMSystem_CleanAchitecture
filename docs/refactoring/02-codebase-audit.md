# 02 - Codebase Audit Report

> **Document version:** 1.0 — 2026-11-18  
> **Author:** Senior Software Architect Audit  
> **Scope:** Backend & Frontend comprehensive code quality assessment

---

## PART A: BACKEND AUDIT

### A1. Domain Layer Analysis

**Location:** `BackE/ERMSystem.Domain/`

#### ⚠️ Anemic Domain Model (Critical Finding)

Tất cả entities trong Domain Layer đều là **Anemic Domain Models** — chỉ chứa properties với public getters/setters, không có behavior, validation, hay business rules.

**Bằng chứng cụ thể:**

**[Patient.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.Domain/Entities/Patient.cs)** (Lines 6-22):
```csharp
public class Patient
{
    public Guid Id { get; set; }        // No encapsulation
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;  // Should be enum or Value Object
    public string Phone { get; set; } = string.Empty;
    // ... all public setters, zero behavior
}
```

**[Appointment.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.Domain/Entities/Appointment.cs)** (Lines 5-16):
```csharp
public class Appointment
{
    public string Status { get; set; } = string.Empty;  // String instead of enum/VO
    // No status transition validation
    // No business rules (e.g., can't schedule in the past)
}
```

**[AppUser.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.Domain/Entities/AppUser.cs)** (Lines 5-18):
```csharp
public class AppUser
{
    public string PasswordHash { get; set; } = string.Empty;  // Public setter on password hash!
    public string Role { get; set; } = string.Empty;  // String role, should be enum
}
```

**Vấn đề chi tiết:**
1. **Không có encapsulation** — Mọi property đều có public setter, cho phép code bên ngoài modify trực tiếp mà không qua validation.
2. **Không có invariant protection** — Entity có thể ở trạng thái không hợp lệ (ví dụ: Patient không có FullName).
3. **Status dùng string** — Các trạng thái (Appointment.Status, AppUser.Role) dùng raw string thay vì strongly-typed enums hoặc Value Objects.
4. **Không có Domain Events** — Không có cơ chế phát sinh sự kiện khi trạng thái thay đổi.
5. **Không có factory methods** — Entity được tạo bằng object initializer, bypass mọi validation.

#### Domain Constants

**[AppointmentStatuses.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.Domain/Constants/)**, **[EncounterStatuses.cs]**, **[InvoiceStatuses.cs]**, **[PrescriptionStatuses.cs]**:
- Tồn tại nhưng chỉ là string constants, không phải enums.
- Không được sử dụng nhất quán trong entities (entities vẫn dùng raw string).

#### Domain Layer — Mức độ tuân thủ Coding Guidelines

| Quy chuẩn | Đánh giá | Ghi chú |
|:---|:---|:---|
| Entity naming (danh từ số ít) | ✅ Tuân thủ | Patient, Doctor, Appointment |
| Index annotations | ❌ Không áp dụng | .NET dùng Fluent API, nhưng không có index nào được define |
| equals/hashCode | ❌ Không có | Entities thiếu Equals/GetHashCode override |
| toString() | ❌ Không có | Không có ToString override cho debug |
| Entity kế thừa base audit | ❌ Không có | Không có BaseEntity hay AuditableEntity |

---

### A2. Application Layer Analysis

**Location:** `BackE/ERMSystem.Application/`

#### Service Layer — Business Logic Placement

**Nhận xét tích cực:**
- Services đều implement interfaces (IPatientService, IDoctorService...).
- Constructor injection cho repository dependencies.
- Proper use of CancellationToken throughout.
- Dashboard cache invalidation pattern nhất quán.

**Vấn đề phát hiện:**

**1. Business Logic trong Service thay vì Domain (Vi phạm Clean Architecture)**

**[PatientService.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.Application/Services/PatientService.cs)** (Lines 120-192):
```csharp
// MergePatients — Complex business logic nằm hoàn toàn trong Application Service
// Các validation rules, merge strategy đáng lẽ thuộc về Domain
if (sourcePatient.AppUserId.HasValue && targetPatient.AppUserId.HasValue 
    && sourcePatient.AppUserId != targetPatient.AppUserId)
{
    throw new InvalidOperationException("Cannot merge...");
}
```

**2. Manual Mapping thay vì AutoMapper/Mapster**

**[PatientService.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.Application/Services/PatientService.cs)** (Lines 204-216):
```csharp
private static PatientDto MapToDto(Patient p) => new PatientDto
{
    Id = p.Id,
    FullName = p.FullName,
    // ... 10 properties mapped manually
};
```

Pattern này lặp lại trong **MỖI service** — DoctorService, MedicineService, AppointmentService...

**3. Inconsistent Error Handling**

- Sử dụng `KeyNotFoundException` cho not-found cases (tốt hơn custom exception).
- Sử dụng `InvalidOperationException` cho business rule violations.
- **Không có Exception hierarchy** — Thiếu custom exception types (NotFoundException, BusinessRuleException, ConflictException).
- Hardcode error messages tiếng Anh — **Vi phạm quy chuẩn i18n** trong coding guidelines.

**4. DTO Design Issues**

**[DTOs folder](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.Application/DTOs/):**
- DTOs nằm trong 1 folder phẳng (~55 files), không tổ chức theo feature/domain.
- Không phân biệt rõ Request DTO vs Response DTO trong naming (CreatePatientDto, UpdatePatientDto vs PatientDto).
- **Thiếu validation annotations** trên Request DTOs — Vi phạm quy chuẩn.
- Không có DTOs cho Common/Pagination patterns ngoài PaginatedResult và PaginationRequest.

**5. Repository Interface Design**

**[IPatientRepository.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.Application/Interfaces/IPatientRepository.cs)** (Lines 8-33):
- Repository interfaces nằm trong Application Layer ✅ (đúng Clean Architecture).
- Nhưng **quá nhiều methods** — `GetAllAsync`, `GetPagedAsync`, `GetTotalCountAsync`, `GetCreatedCountByDayAsync`, `GetByIdAsync`, `GetByAppUserIdAsync`, `GetAppointmentCountAsync`, `GetPotentialDuplicateCandidatesAsync`, `ReassignAppointmentsAsync`, `AddAsync`, `UpdateAsync`, `MergeAsync`, `DeleteAsync` = 13 methods.
- **Vi phạm ISP** (Interface Segregation Principle) — Repository interface quá "fat".

**6. DependencyInjection.cs — Manual Registration**

**[DependencyInjection.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.Application/DependencyInjection.cs)** (Lines 13-38):
- 20 service registrations bằng tay.
- Không sử dụng assembly scanning hoặc convention-based registration.
- Dễ quên khi thêm service mới.

---

### A3. Infrastructure Layer Analysis

**Location:** `BackE/ERMSystem.Infrastructure/`

#### Repository Implementation

**[PatientRepository.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.Infrastructure/Repositories/PatientRepository.cs)** (Lines 9-342):

**Positive:**
- Complex LINQ queries with proper joins.
- `AsNoTracking()` for read queries ✅.
- Soft delete pattern (`DeletedAtUtc == null`) ✅.
- Helper method `BuildPatientQuery()` for DRY ✅.

**Issues:**

1. **Entity Mapping trong Repository** (Lines 265-288):
```csharp
// Repository đang map HospitalPatientEntity → Domain Patient
// Đây là responsibility của Mapper, không phải Repository
private static Patient MapPatient(PatientJoinRow row) => new Patient
{
    Id = row.Patient.Id,
    FullName = row.Patient.FullName,
    // ...
};
```
Repository đang kiêm luôn vai trò Mapper — vi phạm SRP.

2. **Domain Entity ≠ DB Entity nhưng mapping nằm sai chỗ:**
   - Domain `Patient` entity (7 properties) ≠ Hospital `HospitalPatientEntity` (20+ properties).
   - Mapping logic phức tạp (address concatenation, DateOnly → DateTime conversion) nằm trong Repository.

3. **SaveChangesAsync gọi quá nhiều lần trong 1 operation:**

**[PatientRepository.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.Application/Services/PatientService.cs)** — `AddAsync` gọi SaveChanges riêng, `AddOrUpdateEmergencyContact` modify context, rồi lại SaveChanges. Thiếu Unit of Work pattern.

4. **Potential N+1 Issues:**
   - `BuildPatientQuery()` sử dụng LEFT JOINs tốt.
   - Nhưng `ReassignAppointmentsAsync` load tất cả appointments rồi update từng cái — nên dùng `ExecuteUpdateAsync` cho bulk update.

#### Dual Database Context

**HospitalDbContext** chứa **40+ entity** types trong 1 DbContext — đây là warning sign cho **God Context**. Nên xem xét tách thành bounded context-specific DbContexts.

#### Infrastructure Services

- **AuthService** — Complex JWT token generation, refresh, MFA logic nằm trong Infrastructure ✅.
- **GeminiSymptomChatService** — External API integration nằm đúng layer ✅.
- **Background services** (OutboxPublisher, NotificationDispatch, RetentionCleanup, CRM campaigns) — Well-structured background worker pattern ✅.

---

### A4. API/Presentation Layer Analysis

**Location:** `BackE/ERMSystem.API/`

#### Controllers

**Positive findings:**
- Controllers đều thin, delegate to services ✅.
- Proper use of `[Authorize]` and permission-based policies ✅.
- CancellationToken propagation ✅.
- CreatedAtAction for POST responses ✅.

**Issues:**

1. **Inconsistent Error Handling in Controllers:**

**[PatientsController.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.API/Controllers/PatientsController.cs)** (Lines 51-62):
```csharp
// Some methods catch exceptions manually
catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
// While others let exceptions propagate to middleware
```
Có middleware `ApiExceptionHandlingMiddleware` nhưng controllers vẫn catch exception thủ công — không nhất quán.

2. **Duplicate Actor Resolution Logic:**

**[PatientsController.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.API/Controllers/PatientsController.cs)** (Lines 166-177):
```csharp
private Guid? ResolveActorUserId() { ... }
private string? ResolveActorUsername() { ... }
```
Logic này có thể xuất hiện trong nhiều controllers — nên extract thành extension method hoặc base controller.

3. **ModelState.IsValid check thủ công:**
Mặc dù đã cấu hình `ConfigureApiBehaviorOptions` trong Program.cs, một số controllers vẫn check `ModelState.IsValid` thủ công — redundant.

#### Program.cs

**[Program.cs](file:///c:/VietDang/ERMSystem_CleanAchitecture/BackE/ERMSystem.API/Program.cs)** (339 lines):

**Positive:**
- Security headers middleware ✅.
- JWT configuration with proper validation ✅.
- Rate limiting ✅.
- Configuration validation at startup (fail-fast) ✅.
- Structured Options pattern ✅.

**Issue:**
- **Program.cs quá dài** (339 lines) — Nên tách thành extension methods cho từng concern.

#### Security

- JWT authentication with HS256 ✅.
- Security headers (X-Content-Type-Options, X-Frame-Options, etc.) ✅.
- CORS properly configured ✅.
- **Potential issue:** CORS `SetIsOriginAllowed` cho phép private IP ranges trong development — cần đảm bảo bị disable trong production.

---

### A5. Testing

**Location:** `BackE/ERMSystem.Tests/`

| Test File | Coverage |
|:---|:---|
| AdminUserServiceTests | ✅ |
| AppointmentServiceTests | ✅ |
| DashboardServiceTests | ✅ |
| DoctorServiceTests | ✅ |
| MedicalRecordServiceTests | ✅ |
| MedicineServiceTests | ✅ |
| NotificationServiceTests | ✅ |
| PatientServiceTests | ✅ |
| PrescriptionItemServiceTests | ✅ |
| PrescriptionServiceTests | ✅ |
| ClinicDateTimeHelperTests | ✅ |
| CompactCodeGeneratorTests | ✅ |
| DoctorAccessScopeHelperTests | ✅ |
| PaginationTests | ✅ |

**Issues:**
1. **Chỉ test core services** — 10 service tests cho 20 services. Hospital-specific services (HospitalEncounter, HospitalBilling, etc.) **không có tests**.
2. **Không có Integration Tests** — Không có tests cho API endpoints, database, middleware.
3. **Không có tests cho Infrastructure layer** — Repositories, auth service, messaging chưa được test.

---

## PART B: FRONTEND AUDIT

### B1. Architecture & Folder Structure

#### Hiện trạng

```
FontE/
├── app/              → Monolithic page components (1000+ lines each)
├── components/       → Only 16 files total (4 UI, 6 layout, 6 public)
├── services/         → Flat service layer (22 files)
├── contexts/         → 2 context files
├── hooks/            → 2 hook files
├── lib/              → 2 utility files
├── content/          → 1 content file
└── i18n/             → 2 locale files
```

#### Vấn đề chính

**1. God Page Components (Critical)**

Mỗi page.tsx trong `app/(main)/` là một **monolithic component** chứa TẤT CẢ logic:

**[patients/page.tsx](file:///c:/VietDang/ERMSystem_CleanAchitecture/FontE/app/(main)/patients/page.tsx)** — **1084 lines** trong 1 file:
- State management (15+ useState hooks — lines 53-78)
- API data fetching (lines 176-235)
- Form handling (lines 274-304)
- Delete logic (lines 306-338)
- Drawer management (lines 340-391)
- Table rendering (lines 498-653)
- Pagination (lines 655-697)
- Modal rendering (lines 820-1000+)
- Drawer rendering (lines 700-820)

**Đây là pattern lặp lại trên MỌI page** — appointments, prescriptions, billing, clinical-orders, medical-records, dashboard... Mỗi file đều 500-1500+ lines.

**2. Không có Feature/Domain organization**

Tất cả pages nằm flat trong `app/(main)/` — không có sub-components, không có feature-specific hooks, không có local state management.

**3. Shared UI System quá minimal:**

Chỉ có **4 shared UI components**:
- `Button.tsx` (73 lines) — Well-designed với variants ✅
- `Card.tsx` — Basic wrapper
- `Modal.tsx` (103 lines) — Good accessibility (Escape key, aria) ✅
- `DataState.tsx` — Error/Empty state display

**Thiếu hoàn toàn:**
- Input/TextInput component → Raw `<input>` với Tailwind classes inline lặp lại
- Select/Dropdown component → Raw `<select>` trong mỗi page
- Table component → `<table>` markup copy-paste trong mỗi page
- Badge/Tag component → Inline spans lặp lại
- Pagination component → Copy-paste pagination markup
- Drawer component → Custom drawer logic trong mỗi page
- Tabs component
- Form components (FormField, FormGroup)
- Toast configuration component
- Loading skeleton component
- Confirm dialog (dùng `window.confirm` — rất anti-pattern)

### B2. Component Design Issues

**1. Inline HTML elements thay vì reusable components:**

**[patients/page.tsx](file:///c:/VietDang/ERMSystem_CleanAchitecture/FontE/app/(main)/patients/page.tsx)** (Lines 472-478):
```tsx
// Search input — copied in EVERY page
<input
  type="text"
  placeholder="Tìm theo tên, số điện thoại..."
  className="w-full rounded-xl border border-gray-200 py-2.5 pl-10 pr-4 text-sm..."
  value={searchQuery}
  onChange={(event) => setSearchQuery(event.target.value)}
/>
```

**2. window.confirm for destructive actions:**

**[patients/page.tsx](file:///c:/VietDang/ERMSystem_CleanAchitecture/FontE/app/(main)/patients/page.tsx)** (Lines 314-320):
```tsx
if (!window.confirm("Bạn có chắc muốn xóa...")) { return; }
```
Sử dụng `window.confirm` native — không phù hợp với design system chuyên nghiệp.

**3. Hardcoded Vietnamese strings:**

UI text được hardcode trực tiếp — mặc dù có hệ thống i18n (`useTranslation`), nó **không được sử dụng** trong majority of pages.

### B3. State & Data Management

**1. Excessive useState:**

Mỗi page có 15-20+ useState calls, khiến state management trở nên khó theo dõi. Nên sử dụng useReducer hoặc extract thành custom hooks.

**2. API Error Handling lặp lại:**

Pattern `try { ... } catch (error) { toast.error(getApiErrorMessage(error, "...")); }` xuất hiện hàng chục lần — nên abstract thành custom hook.

**3. No data caching:**

Mỗi lần navigate giữa các pages, data được fetch lại hoàn toàn — không có caching strategy.

### B4. Styling & UI/UX Analysis

**Positive:**
- Tailwind CSS usage nhất quán ✅.
- Responsive design considerations ✅.
- Loading skeletons ✅.
- Toast notifications ✅.

**Issues:**

1. **Inconsistent design tokens** — Màu sắc, border-radius, spacing không qua design token system mà hardcode trong mỗi component.

2. **Gradient usage:** Avatar initials sử dụng gradient (`bg-gradient-to-br from-blue-500 to-blue-600`) — một dấu hiệu "AI-generated" UI.

3. **Card-heavy layout** — Mọi thứ đều wrap trong card với rounded corners, borders, shadows — thiếu visual hierarchy đa dạng.

4. **No consistent icon system** — Dùng text abbreviations thay vì icons ("DT", "BS", "DC" cho phone/doctor/address).

5. **Pagination UI copy-paste** — Mỗi page tự implement pagination buttons, không có shared Pagination component.

### B5. API Service Layer

**[services/api.ts](file:///c:/VietDang/ERMSystem_CleanAchitecture/FontE/services/api.ts)** — Good implementation:
- Axios instance with interceptors ✅.
- Token refresh with deduplication (`refreshPromise`) ✅.
- Auth endpoint exclusion ✅.

**[services/types.ts](file:///c:/VietDang/ERMSystem_CleanAchitecture/FontE/services/types.ts)** — **1088 lines in one file!**
- Tất cả types trong 1 file — nên tách theo feature/domain.

**[services/patientService.ts](file:///c:/VietDang/ERMSystem_CleanAchitecture/FontE/services/patientService.ts)** — Notable issue (line 21):
```typescript
// Sends BOTH keys for compatibility — indicates API typo that was worked around
url += `&textSearch=${encoded}&textSeach=${encoded}`;
```
Cho thấy có **typo trong backend API** (`textSeach` thay vì `textSearch`) mà FE phải workaround.

---

## PART C: Cross-Cutting Compliance Assessment

### Mức độ tuân thủ Coding Guidelines

| Quy chuẩn (từ tài liệu tham chiếu) | Đánh giá | Ghi chú |
|:---|:---|:---|
| Kiến trúc BFF (tách Controller Web/Mobile) | ⚠️ N/A | Dự án dùng .NET không phải Spring Boot. Concept BFF có thể áp dụng qua separate API versioning |
| Entity naming convention | ✅ Tuân thủ | Danh từ số ít |
| Entity đánh Index | ❌ Không có | Không thấy Index annotations |
| Entity equals/hashCode | ❌ Không có | Thiếu hoàn toàn |
| Entity toString() | ❌ Không có | Thiếu hoàn toàn |
| DTO phân biệt Request/Response | ⚠️ Một phần | Có tách Create/Update DTO nhưng Response DTO không rõ naming |
| Validation annotations trên Request DTO | ❌ Rất ít | Thiếu DataAnnotations |
| Repository chỉ 4 hàm tối giản | ❌ Vi phạm | Repository có 13+ methods |
| Service DRY (helper methods chung) | ⚠️ Một phần | PatientService có helper methods nhưng không nhất quán |
| @Transactional pattern | ⚠️ N/A | .NET không dùng @Transactional; Transaction implicit qua SaveChanges |
| Controller phân quyền hằng số | ✅ Tuân thủ | AppPermissions constants |
| Exception ném bằng key hằng số i18n | ❌ Vi phạm | Hardcode error messages tiếng Anh |
| Message Bundle i18n | ❌ Vi phạm | Backend không có message bundle |

> **Lưu ý quan trọng:** Tài liệu quy chuẩn được viết cho **Spring Boot/Java** (JPA, MapStruct, `@Transactional`) nhưng dự án thực tế sử dụng **.NET/C#** (EF Core, manual mapping). Nhiều quy chuẩn cần được **điều chỉnh** cho phù hợp với .NET ecosystem thay vì áp dụng nguyên xi. Phần đề xuất chi tiết sẽ nằm trong tài liệu `04-target-architecture.md`.

---

## PART D: DEVOPS, INFRASTRUCTURE & DATABASE AUDIT

### D1. Containerization & Docker Security

#### 1. Lỗ hổng phơi bày cổng nội bộ trong Production (P0 Critical)
- **Vị trí:** `docker-compose.prod.yml` (Lines 9-11, 26-27).
- **Vấn đề:** Các dịch vụ nền tảng nội bộ bao gồm Redis (`6379:6379`) và RabbitMQ (`5672:5672`, `15672:15672`) đang được expose trực tiếp ra host interface `0.0.0.0`.
- **Rủi ro:** Khi deploy lên VPS công khai, bất kỳ ai trên internet cũng có thể kết nối trực tiếp vào message broker và cache memory nếu firewall không chặn chặt.
- **Giải pháp:** Xóa bỏ `ports:` binding trong `docker-compose.prod.yml` cho Redis và RabbitMQ. Chỉ cho phép các dịch vụ này giao tiếp nội bộ thông qua docker network `erm_network`.

#### 2. Chạy ứng dụng dưới quyền Root trong Containers (P0 Critical)
- **Vị trí:** `BackE/ERMSystem.API/Dockerfile` và `FontE/Dockerfile`.
- **Vấn đề:** Cả hai Dockerfiles đều không khai báo chỉ thị `USER`. Ứng dụng ASP.NET Core và Next.js chạy trực tiếp với quyền `root` bên trong container.
- **Rủi ro:** Nếu ứng dụng có lỗ hổng RCE (Remote Code Execution) hoặc upload file độc hại, kẻ tấn công có toàn quyền root bên trong container và dễ dàng tìm cách escape container ra host OS.
- **Giải pháp:** Bổ sung `USER app` vào runtime stage của .NET và `USER node` vào stage của Next.js.

#### 3. Tối ưu hóa Docker Build Context
- **Vấn đề:** Thiếu file `.dockerignore` tại thư mục root. Khi build container từ root context, toàn bộ thư mục `.git/`, `.next/`, `node_modules/`, `bin/`, `obj/` bị đẩy vào build daemon, làm chậm build pipeline đáng kể.
- **Vấn đề Caching .NET Layer:** Trong `BackE/ERMSystem.API/Dockerfile`, lệnh `COPY . .` được chạy trước `dotnet restore`, khiến Docker cache layer bị phá vỡ mỗi khi có bất kỳ dòng code nào thay đổi.
- **Giải pháp:** Copy file `.sln` và các file `.csproj` trước để chạy `dotnet restore` riêng biệt trước khi copy toàn bộ source code.

---

### D2. CI/CD Pipelines (.github/workflows)

1. **Token bảo mật trên VPS trong `deploy.yml`:**
   - Script deployment trên VPS sử dụng biến môi trường ephemeral `GITHUB_TOKEN` để `docker login ghcr.io`. Biến này có thể bị hết hạn hoặc không có quyền pull image nếu workflow kết thúc.
   - Cần cấu hình một GitHub Deploy Key hoặc Dedicated Read-Only PAT cho GHCR.
2. **Thiếu Concurrency Lock:**
   - Cả `ci.yml` và `deploy.yml` đều thiếu chỉ thị `concurrency: group: ... cancel-in-progress: true`. Nếu lập trình viên push commit liên tục, nhiều pipeline deploy sẽ chạy đè lên nhau gây race condition trên VPS.
3. **Chưa tận dụng Docker Buildx Cache:**
   - Build image trên GitHub Actions chưa bật `cache-from: type=gha`, khiến mỗi commit đều phải download dependencies và build image từ đầu (thời gian build kéo dài 6-10 phút thay vì 1-2 phút).

---

### D3. Database Schema & Clustered Index Fragmentation

- **Vị trí:** `Infrastructure/database/seed_full_database.sql`.
- **Vấn đề:** Hầu hết các bảng chính (`hospital_patients`, `hospital_encounters`, `hospital_prescriptions`, `hospital_invoices`) đều sử dụng `Id UNIQUEIDENTIFIER PRIMARY KEY`.
- **Phân tích:** Mặc định trong SQL Server, Primary Key sẽ là **Clustered Index**. Khi sử dụng `Guid.NewGuid()` ngẫu nhiên sinh từ ứng dụng hoặc `NEWID()` trong DB, các bản ghi mới sẽ được chèn ngẫu nhiên vào giữa các trang B-Tree của bảng thay vì chèn vào cuối.
- **Hậu quả:** Gây ra hiện tượng **B-Tree Page Split** nghiêm trọng và phân mảnh index (Fragmentation) lên đến >80% khi số lượng bản ghi đạt hàng trăm ngàn lượt khám, làm suy giảm tốc độ truy vấn đáng kể.
- **Giải pháp:**
  - Trong SQL Server DDL: Chuyển sang `DEFAULT NEWSEQUENTIALID()`.
  - Trong .NET 9/10: Sử dụng chuẩn `Guid.CreateVersion7()` (UUIDv7 - Time-ordered GUID) để đảm bảo ID luôn tăng dần theo thời gian, loại bỏ hoàn toàn hiện tượng phân mảnh Clustered Index.
