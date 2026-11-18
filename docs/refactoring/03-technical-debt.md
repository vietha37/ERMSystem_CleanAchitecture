# 03 - Technical Debt Inventory

> **Document version:** 1.0 — 2026-11-18  
> **Author:** Senior Software Architect Audit  
> **Priority Legend:** P0=Critical, P1=High, P2=Medium, P3=Low

---

## Backend Issues

### BE-001: Anemic Domain Model — Zero Business Logic in Domain Layer
- **Priority:** P1 — High
- **Location:** `BackE/ERMSystem.Domain/Entities/` (all 8 entity files)
- **Mô tả:** Tất cả domain entities chỉ chứa properties với public getters/setters, không có behavior, validation, hay business rules.
- **Nguyên nhân gốc rễ:** Domain entities được thiết kế như POCOs/DTOs thay vì Rich Domain Models. Business logic nằm trong Application Services.
- **Vi phạm:** DDD Tactical Patterns, Encapsulation principle, Tell-Don't-Ask principle.
- **Ảnh hưởng:** Business rules bị phân tán trong Services, khó protect domain invariants, khó test business logic isolation.
- **Rủi ro:** Business rules có thể bị bypass khi code mới trực tiếp modify entity properties. Duplicated validation across services.
- **Giải pháp:** Refactor entities thành Rich Domain Models với private setters, factory methods, validation methods. Sử dụng Value Objects cho Gender, Status, Phone.
- **Độ phức tạp:** Medium — Cần refactor entities + update tất cả services sử dụng chúng.

---

### BE-002: Status Fields Use Raw Strings Instead of Strongly-Typed Enums
- **Priority:** P1 — High
- **Location:** `Appointment.Status`, `AppUser.Role`, HospitalData entities
- **Mô tả:** Status/Role dùng `string` type mặc dù có Constants files. Không có type safety.
- **Nguyên nhân gốc rễ:** Initial design lựa chọn flexibility trên type safety.
- **Vi phạm:** Type Safety, Domain Model integrity.
- **Ảnh hưởng:** Runtime errors thay vì compile-time errors. Typos trong status strings không bị phát hiện.
- **Rủi ro:** Invalid status values được persist vào database. Difficult to track valid transitions.
- **Giải pháp:** Convert status/role fields thành enums hoặc Value Objects. Database migration required.
- **Độ phức tạp:** Medium — Requires DB migration + entity changes + service updates.

---

### BE-003: Hardcoded Error Messages — No i18n Support
- **Priority:** P2 — Medium
- **Location:** All Application Services (PatientService, AppointmentService, etc.)
- **Mô tả:** Error messages hardcoded bằng tiếng Anh (`"Patient with ID {id} not found."`, `"Cannot merge patients..."`).
- **Nguyên nhân gốc rễ:** Chưa implement i18n infrastructure cho backend.
- **Vi phạm:** Coding Guidelines Section 8 (Exception ném bằng key hằng số i18n).
- **Ảnh hưởng:** Không hỗ trợ đa ngôn ngữ trên error responses. Inconsistent language (BE English, FE Vietnamese).
- **Giải pháp:** Implement exception hierarchy + resource files (.resx) cho i18n messages.
- **Độ phức tạp:** Medium.

---

### BE-004: Missing Validation on Request DTOs
- **Priority:** P1 — High
- **Location:** `BackE/ERMSystem.Application/DTOs/` — CreatePatientDto, UpdatePatientDto, CreateDoctorDto, etc.
- **Mô tả:** Request DTOs thiếu DataAnnotation validation (`[Required]`, `[StringLength]`, `[EmailAddress]`, etc.).
- **Vi phạm:** Coding Guidelines Section 4.2, Defense-in-depth principle.
- **Ảnh hưởng:** Invalid data có thể bypass frontend validation và reach database.
- **Rủi ro:** Data integrity issues, potential SQL injection via unsanitized inputs, XSS.
- **Giải pháp:** Thêm DataAnnotations hoặc FluentValidation cho tất cả Request DTOs.
- **Độ phức tạp:** Low-Medium.

---

### BE-005: Fat Repository Interfaces — ISP Violation
- **Priority:** P2 — Medium
- **Location:** `IPatientRepository` (13 methods), similarly other repositories.
- **Mô tả:** Repository interfaces chứa quá nhiều methods, bao gồm cả domain-specific queries.
- **Vi phạm:** Interface Segregation Principle (ISP), Coding Guidelines Section 5 (4 methods tối giản).
- **Giải pháp:** Tách thành base IRepository<T> + domain-specific query interfaces.
- **Độ phức tạp:** Medium.

---

### BE-006: Manual Object Mapping in Every Service
- **Priority:** P2 — Medium  
- **Location:** All Application Services — `MapToDto()` private methods.
- **Mô tả:** Mỗi service có private `MapToDto()` method viết tay. Cùng 1 entity có thể được map ở nhiều nơi.
- **Vi phạm:** DRY principle, SRP (Service kiêm Mapper).
- **Ảnh hưởng:** Code duplication, error-prone khi thêm field mới (phải update ở nhiều chỗ).
- **Giải pháp:** Áp dụng AutoMapper hoặc Mapster, hoặc tạo dedicated Mapper classes.
- **Độ phức tạp:** Low.

---

### BE-007: Entity Mapping in Repository Layer
- **Priority:** P2 — Medium
- **Location:** `PatientRepository.cs` (Lines 265-288), similarly other Hospital repositories.
- **Mô tả:** Repository chịu trách nhiệm map HospitalEntity → Domain Entity. Đây là anti-corruption layer logic nhưng nằm sai vị trí.
- **Vi phạm:** SRP — Repository kiêm Mapper.
- **Giải pháp:** Tạo dedicated entity mapper/translator classes.
- **Độ phức tạp:** Low.

---

### BE-008: HospitalDbContext God Context — 40+ Entity Types
- **Priority:** P2 — Medium
- **Location:** `BackE/ERMSystem.Infrastructure/HospitalData/HospitalDbContext.cs`
- **Mô tả:** Một DbContext quản lý 40+ entity types thuộc nhiều bounded contexts khác nhau.
- **Vi phạm:** Bounded Context principle, SRP.
- **Ảnh hưởng:** Slow startup, complex migrations, tight coupling giữa các domains.
- **Giải pháp:** Tách thành bounded context-specific DbContexts (Phase 2 — khi hệ thống đủ lớn).
- **Độ phức tạp:** High — Requires careful migration planning.

---

### BE-009: Inconsistent Error Handling — Dual Pattern
- **Priority:** P2 — Medium
- **Location:** Controllers catch exceptions thủ công mặc dù có `ApiExceptionHandlingMiddleware`.
- **Mô tả:** Một số controllers catch `KeyNotFoundException` thủ công, một số để middleware xử lý.
- **Vi phạm:** Consistency principle.
- **Giải pháp:** Thống nhất dùng middleware + custom exception types. Remove try-catch từ controllers.
- **Độ phức tạp:** Low.

---

### BE-010: Hospital Services Lack Unit Tests
- **Priority:** P1 — High
- **Location:** `BackE/ERMSystem.Tests/` — Thiếu tests cho 10 Hospital services.
- **Mô tả:** HospitalEncounterService, HospitalBillingService, HospitalPrescriptionService, etc. không có unit tests.
- **Ảnh hưởng:** Critical business logic (billing, prescriptions, clinical orders) không được test.
- **Giải pháp:** Viết unit tests cho tất cả hospital services.
- **Độ phức tạp:** Medium.

---

### BE-011: No Integration Tests
- **Priority:** P2 — Medium
- **Location:** `BackE/ERMSystem.Tests/` — Chỉ có unit tests.
- **Mô tả:** Không có API integration tests, database integration tests, hoặc end-to-end tests.
- **Giải pháp:** Thêm integration test project với WebApplicationFactory.
- **Độ phức tạp:** Medium.

---

### BE-012: Missing Base Entity / Audit Trail
- **Priority:** P2 — Medium
- **Location:** Domain entities (Patient, Doctor, Appointment, etc.)
- **Mô tả:** Domain entities thiếu audit fields (CreatedBy, UpdatedBy, UpdatedAt). Chỉ Patient có CreatedAt.
- **Vi phạm:** Coding Guidelines Section 3.1 (Entity kế thừa AbstractAuditingEntity).
- **Giải pháp:** Tạo BaseEntity với audit fields, hospital entities đã có audit fields riêng.
- **Độ phức tạp:** Medium.

---

### BE-013: PasswordHash Has Public Setter
- **Priority:** P0 — Critical (Security)
- **Location:** `AppUser.cs` Line 10: `public string PasswordHash { get; set; }`
- **Mô tả:** PasswordHash property có public setter, cho phép code bất kỳ modify trực tiếp.
- **Rủi ro:** Security vulnerability — password hash có thể bị overwrite accidentally.
- **Giải pháp:** Private setter + method `SetPasswordHash(string hash)` với validation.
- **Độ phức tạp:** Low.

---

## Frontend Issues

### FE-001: God Page Components — 500-1500+ Lines Per Page
- **Priority:** P0 — Critical
- **Location:** ALL pages in `FontE/app/(main)/` — patients, appointments, billing, prescriptions, etc.
- **Mô tả:** Mỗi page.tsx chứa 500-1500+ lines bao gồm state management, data fetching, event handling, và rendering trong 1 component duy nhất.
- **Bằng chứng:** `patients/page.tsx` = 1084 lines, 15+ useState hooks.
- **Vi phạm:** SRP, Component-Based Architecture, Maintainability.
- **Ảnh hưởng:** Extremely difficult to maintain, test, or reuse logic. High merge conflict risk.
- **Giải pháp:** Decompose into feature-specific hooks, sub-components, and container/presenter pattern.
- **Độ phức tạp:** High.

---

### FE-002: Missing Shared UI Component System
- **Priority:** P1 — High
- **Location:** `FontE/components/ui/` — Only 4 components (Button, Card, Modal, DataState).
- **Mô tả:** Thiếu hoàn toàn: Input, Select, Table, Pagination, Badge, Drawer, Tabs, FormField, ConfirmDialog, SearchInput, Skeleton.
- **Ảnh hưởng:** Code duplication trên mọi page (search input, table, pagination đều copy-paste).
- **Giải pháp:** Xây dựng shared UI component library.
- **Độ phức tạp:** Medium-High.

---

### FE-003: window.confirm for Destructive Actions
- **Priority:** P1 — High
- **Location:** Multiple pages — `patients/page.tsx` Line 315, similarly in other pages.
- **Mô tả:** Sử dụng browser native `window.confirm()` cho delete confirmations.
- **Vi phạm:** UI/UX quality standards, Design System consistency.
- **Ảnh hưởng:** Breaks UI consistency, not styleable, not accessible properly.
- **Giải pháp:** Create ConfirmDialog component using Modal.
- **Độ phức tạp:** Low.

---

### FE-004: Types File Monolith — 1088 Lines
- **Priority:** P2 — Medium
- **Location:** `FontE/services/types.ts` — 1088 lines, 70+ type definitions.
- **Mô tả:** Tất cả API types trong 1 file duy nhất.
- **Vi phạm:** Modularity, Feature-based organization.
- **Giải pháp:** Tách types theo feature/domain module.
- **Độ phức tạp:** Low.

---

### FE-005: i18n Not Used in Page Components
- **Priority:** P2 — Medium
- **Location:** ALL page components — Vietnamese strings hardcoded.
- **Mô tả:** Có hệ thống i18n (useTranslation hook, en.json, vi.json) nhưng pages hardcode Vietnamese text.
- **Vi phạm:** i18n principle.
- **Giải pháp:** Migrate all UI strings to i18n keys.
- **Độ phức tạp:** Medium (nhiều strings cần migrate).

---

### FE-006: No Frontend Tests
- **Priority:** P2 — Medium
- **Location:** `FontE/` — Zero test files.
- **Mô tả:** Không có unit tests, component tests, hay E2E tests.
- **Giải pháp:** Add Jest + React Testing Library cho components, Playwright/Cypress cho E2E.
- **Độ phức tạp:** Medium-High.

---

### FE-007: API URL Typo Workaround
- **Priority:** P3 — Low
- **Location:** `FontE/services/patientService.ts` Line 21.
- **Mô tả:** FE sends cả `textSearch` và `textSeach` (typo) cho compatibility.
- **Giải pháp:** Fix backend API parameter name, remove workaround from FE.
- **Độ phức tạp:** Low.

---

### FE-008: No Data Caching Strategy
- **Priority:** P2 — Medium
- **Location:** All service calls in page components.
- **Mô tả:** Mỗi page navigation refetch toàn bộ data. Không có client-side cache.
- **Giải pháp:** Consider React Query / SWR cho data fetching + caching.
- **Độ phức tạp:** Medium.

---

### FE-009: Missing Design Tokens System
- **Priority:** P2 — Medium
- **Location:** All component styling.
- **Mô tả:** Colors, spacing, border-radius, shadows hardcode bằng Tailwind utility classes. Không có centralized design tokens.
- **Giải pháp:** Define CSS variables or Tailwind theme extensions cho design tokens.
- **Độ phức tạp:** Medium.

---

### FE-010: Inconsistent Icon Usage
- **Priority:** P3 — Low
- **Location:** UI throughout the application.
- **Mô tả:** Sử dụng text abbreviations ("DT", "BS", "DC", "Tim") thay vì proper icons. Không có icon library.
- **Giải pháp:** Integrate Lucide Icons hoặc Heroicons.
- **Độ phức tạp:** Low.

---

## DevOps & Infrastructure Issues

### OPS-001: Production Host Port Exposure for Redis & RabbitMQ
- **Priority:** P0 — Critical
- **Location:** `docker-compose.prod.yml` Lines 9-11, 26-27.
- **Mô tả:** Cổng Redis (`6379`) và RabbitMQ (`5672`, `15672`) được expose công khai ra host interface `0.0.0.0`.
- **Rủi ro:** Kẻ tấn công có thể truy cập trái phép message broker và memory cache nếu tường lửa máy chủ có sơ hở.
- **Giải pháp:** Xóa bỏ mục `ports:` trong file production compose; chỉ duy trì kết nối mạng nội bộ giữa các container (`erm_network`).
- **Độ phức tạp:** Low.

---

### OPS-002: Containers Executing with Root Privileges
- **Priority:** P0 — Critical
- **Location:** `BackE/ERMSystem.API/Dockerfile` Line 15, `FontE/Dockerfile` Line 14.
- **Mô tả:** Không chỉ định chỉ thị `USER` trong runtime image; container chạy dưới quyền `root`.
- **Rủi ro:** Container breakout nếu có lỗ hổng thực thi mã từ xa.
- **Giải pháp:** Thêm `USER app` vào .NET runtime và `USER node` vào Next.js runtime.
- **Độ phức tạp:** Low.

---

### OPS-003: Clustered Index B-Tree Fragmentation via Random GUIDs
- **Priority:** P1 — High
- **Location:** `Infrastructure/database/seed_full_database.sql` Line 28, all Aggregate Primary Keys.
- **Mô tả:** Khóa chính kiểu `UNIQUEIDENTIFIER` là clustered index được gán bằng `Guid.NewGuid()` ngẫu nhiên.
- **Rủi ro:** B-Tree Page Splits nghiêm trọng làm giảm hiệu năng I/O và truy vấn khi số lượng bệnh nhân và đơn thuốc tăng cao.
- **Giải pháp:** Cấu hình `NEWSEQUENTIALID()` trên SQL Server hoặc chuyển sang `Guid.CreateVersion7()` (UUIDv7) trong .NET.
- **Độ phức tạp:** Medium.

---

### OPS-004: Inefficient Docker Layer Caching & Missing Root .dockerignore
- **Priority:** P2 — Medium
- **Location:** Root directory & `BackE/ERMSystem.API/Dockerfile`.
- **Mô tả:** Thiếu root `.dockerignore`; `COPY . .` được gọi trước `dotnet restore`.
- **Ảnh hưởng:** Thời gian build image kéo dài không cần thiết, làm chậm CI/CD.
- **Giải pháp:** Tạo `.dockerignore` chuẩn; tách bước restore project dependencies trước khi copy mã nguồn.
- **Độ phức tạp:** Low.

---

## Priority Summary Matrix

| Priority | Count | Issues |
|:---|:---:|:---|
| **P0 — Critical** | 4 | BE-013 (PasswordHash public setter), FE-001 (God page components), OPS-001 (Exposed Redis/RabbitMQ ports), OPS-002 (Root containers) |
| **P1 — High** | 7 | BE-001 (Anemic Domain), BE-002 (String Status), BE-004 (Missing DTO Validation), BE-010 (Missing Hospital Tests), FE-002 (Missing Shared UI), FE-003 (window.confirm), OPS-003 (GUID Index Fragmentation) |
| **P2 — Medium** | 12 | BE-003, BE-005, BE-006, BE-007, BE-008, BE-009, BE-011, BE-012, FE-004, FE-005, FE-006, FE-008, FE-009, OPS-004 |
| **P3 — Low** | 2 | FE-007 (API Typo), FE-010 (Text Icons) |
| **Total** | 25 | — |
