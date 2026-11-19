# TÀI LIỆU HƯỚNG DẪN LẬP TRÌNH VÀ QUY CHUẨN KIẾN TRÚC (DEVELOPER GUIDE)
## Hệ thống ERM Hospital Management System

---

## 1. TỔNG QUAN KIẾN TRÚC HỆ THỐNG

Dự án áp dụng mô hình kiến trúc **Clean Architecture** kết hợp **Domain-Driven Design (DDD)** ở Backend (.NET 10) và **Feature-Sliced Component Architecture** ở Frontend (Next.js 16 + React 19).

```
                      ┌────────────────────────────────────────┐
                      │             PRESENTATION               │
                      │  Next.js 16 (FE)  /  Web API (.NET 10) │
                      └──────────────────┬─────────────────────┘
                                         │
                                         ▼
                      ┌────────────────────────────────────────┐
                      │              APPLICATION               │
                      │  Use Cases, App Services, DTOs, Caching│
                      └──────────────────┬─────────────────────┘
                                         │
                                         ▼
                      ┌────────────────────────────────────────┐
                      │                DOMAIN                  │
                      │ Aggregates, Entities, Value Objects,   │
                      │ Domain Events, Domain Exceptions       │
                      └──────────────────▲─────────────────────┘
                                         │
                      ┌──────────────────┴─────────────────────┐
                      │             INFRASTRUCTURE             │
                      │ EF Core DbContext, Repositories, Redis,│
                      │ Outbox Publisher, Background Services  │
                      └────────────────────────────────────────┘
```

---

## 2. QUY CHUẨN BACKEND (.NET 10 - CLEAN ARCHITECTURE)

### 2.1. Tầng Domain (`ERMSystem.Domain`)
Là trung tâm của ứng dụng, chứa toàn bộ quy tắc và bất biến nghiệp vụ y tế. **Tuyệt đối không phụ thuộc vào bất kỳ tầng nào khác.**

1. **Entities & Aggregate Roots:**
   - Kế thừa từ `BaseEntity<TId>` hoặc `AggregateRoot<TId>`.
   - **Encapsulation:** Toàn bộ properties phải có `private set` hoặc `protected set`. Mọi thay đổi trạng thái phải đi qua các phương thức nghiệp vụ nội tại (vd: `CheckIn()`, `Cancel()`, `MergeWith()`).
   - Không được tạo anemic model (chỉ có getter/setter công khai).

2. **Value Objects:**
   - Kế thừa từ `ValueObject` (vd: `MedicalRecordNumber`, `EmergencyContact`, `DosageInstruction`).
   - Là bất biến (immutable), so sánh dựa trên giá trị các thuộc tính thay vì identity.
   - Tự bảo vệ tính hợp lệ trong constructor/factory method (fail-fast validation).

3. **Domain Events:**
   - Kế thừa từ `IDomainEvent` (vd: `PatientCreatedEvent`, `AppointmentStatusChangedEvent`).
   - Được đăng ký thông qua `AddDomainEvent()` bên trong Aggregate Root và phát ra sau khi thực hiện nghiệp vụ.

4. **Kiểm tra tự động (Architecture Gate):**
   - Tầng `Domain` được kiểm soát tự động bởi `NetArchTest.Rules` trong unit test suite để ngăn chặn việc import chéo các tầng ngoài.

### 2.2. Tầng Application (`ERMSystem.Application`)
1. **Application Services:**
   - Đóng vai trò điều phối luồng (orchestration): gọi Repository lấy Aggregate -> thực thi business method của Aggregate -> lưu lại qua `IUnitOfWork`.
   - Không đặt business rules tính toán vào Application Service nếu rule đó thuộc về Aggregate nội tại.
2. **DTOs & Mapping:**
   - Toàn bộ dữ liệu giao tiếp với Presentation phải qua DTOs.
   - Giữ nguyên naming convention JSON theo chuẩn `camelCase`.
3. **Caching:**
   - Dùng `IDistributedCache` qua các service chuyên biệt (`DashboardQueryCache`, `CatalogQueryCache`).
   - Áp dụng cache versioning để invalidate tức thì khi có cập nhật dữ liệu liên quan.

### 2.3. Tầng Infrastructure (`ERMSystem.Infrastructure`)
1. **Tối ưu hóa EF Core:**
   - 100% câu truy vấn đọc danh sách/chi tiết bắt buộc sử dụng `.AsNoTracking()`.
   - Thao tác xóa mềm hoặc cập nhật trạng thái hàng loạt phải dùng `.ExecuteUpdateAsync()` và `.ExecuteDeleteAsync()` trực tiếp trên Database, không nạp toàn bộ thực thể vào bộ nhớ.
2. **Transaction & Unit of Work:**
   - Sử dụng `IUnitOfWork` để cam kết toàn vẹn dữ liệu giữa nhiều repository trong cùng một use case.

---

## 3. QUY CHUẨN FRONTEND (NEXT.JS 16 - FEATURE SLICES)

### 3.1. Cấu trúc thư mục theo Feature Slice
Mọi chức năng nghiệp vụ được đóng gói độc lập trong thư mục `FontE/features/<feature-name>/`:

```
features/<feature-name>/
├── types.ts                  # Kiểu dữ liệu, enum, interfaces
├── hooks/                    # Custom hooks quản lý state, mutation, filters
│   └── use<Feature>List.ts
├── components/               # UI components chuyên biệt của feature
│   ├── <Feature>Header.tsx
│   ├── <Feature>Table.tsx
│   └── <Feature>Modal.tsx
├── views/                    # View coordinator ghép nối hooks & components
│   └── <Feature>ManagementView.tsx
└── index.ts                  # Public API barrel export
```

Trang trong `FontE/app/(main)/<route>/page.tsx` chỉ đóng vai trò wrapper tối giản (< 10 dòng) để render View tương ứng:
```tsx
import { PatientsView } from "@/features/patients";

export default function PatientsPage() {
  return <PatientsView />;
}
```

### 3.2. Giới hạn độ dài file & Trách nhiệm đơn lẻ
- **QUY TẮC BẮT BUỘC:** Không có bất kỳ file component nào vượt quá **200 dòng code**.
- Khi một component vượt quá 150-180 dòng, phải tách nhỏ thành subcomponent hoặc chuyển logic state sang custom hook.

### 3.3. Bộ Shared UI Components chuẩn
1. **Phân trang Data Table:**
   - 100% bảng dữ liệu phải sử dụng component dùng chung:
     ```tsx
     import { TablePagination } from "@/shared/ui/Table/TablePagination";
     ```
   - Hỗ trợ đầy đủ: nhảy trang đầu/trước/tiếp/cuối, hiển thị khoảng bản ghi (`startItem - endItem / totalItems`), tùy chọn cỡ trang (`pageSizeOptions`).
2. **Hộp thoại xác nhận (Confirm Dialog):**
   - **CẤM TUYỆT ĐỐI** gọi `window.confirm()`.
   - Bắt buộc sử dụng `ConfirmDialog`:
     ```tsx
     import { ConfirmDialog } from "@/shared/ui/Modal/ConfirmDialog";
     ```
   - Hỗ trợ xử lý bất đồng bộ (`isLoading`), tự động khóa phím Enter để tránh xác nhận nhầm.
3. **Clinical UI Design Tokens:**
   - Tránh các background gradient sặc sỡ và bo góc quá mức kiểu AI-generated (`rounded-[2rem] bg-gradient-to-br...`).
   - Sử dụng chuẩn y tế chuyên nghiệp: nền trắng/xám nhẹ, viền `border-slate-200`, bo góc `rounded-xl`, bóng đổ tinh tế `shadow-xs`.

### 3.4. Ranh giới kiến trúc Frontend (ESLint Boundary Gate)
- Dự án đã kích hoạt rule `no-restricted-imports` trong `eslint.config.mjs`.
- **Cấm nhập trực tiếp nội bộ:** `from "@/features/other-feature/components/..."`.
- **Chỉ được phép nhập từ Public Barrel:** `from "@/features/other-feature"` hoặc di chuyển component tái sử dụng vào `@/shared/ui/`.

---

## 4. HƯỚNG DẪN THÊM MỚI MỘT CHỨC NĂNG (STEP-BY-STEP WORKFLOW)

### Bước 1: Khởi tạo Domain Model (Backend)
1. Tạo Entity/Aggregate Root trong `BackE/ERMSystem.Domain/Entities/`.
2. Kế thừa `AggregateRoot<Guid>` hoặc `BaseEntity<Guid>`.
3. Định nghĩa private setters và các business methods.
4. Thêm Value Objects (nếu có) trong `BackE/ERMSystem.Domain/ValueObjects/`.

### Bước 2: Thiết lập Repository & Application Service
1. Thêm interface repository trong `BackE/ERMSystem.Application/Interfaces/`.
2. Hiện thực repository trong `BackE/ERMSystem.Infrastructure/Repositories/` (nhớ thêm `.AsNoTracking()` cho read queries).
3. Đăng ký DI trong `Infrastructure/DependencyInjection.cs`.
4. Tạo DTOs và Application Service trong `ERMSystem.Application/`.

### Bước 3: Phơi bày API Endpoint
1. Tạo Controller trong `BackE/ERMSystem.API/Controllers/`.
2. Sử dụng HTTP status codes chuẩn (`200 OK`, `201 Created`, `204 NoContent`, `400 BadRequest`, `404 NotFound`).

### Bước 4: Xây dựng Feature Slice (Frontend)
1. Tạo thư mục `FontE/features/<feature-name>/`.
2. Khai báo `types.ts`.
3. Viết custom hook trong `hooks/use<Feature>.ts`.
4. Thiết kế các components nhỏ (< 200 dòng) với `TablePagination` và `ConfirmDialog`.
5. Tạo View trong `views/<Feature>View.tsx`.
6. Export qua `index.ts`.
7. Tạo page route gọn gàng trong `FontE/app/(main)/<route>/page.tsx`.

---

## 5. HỆ THỐNG KIỂM THỬ TỰ ĐỘNG & CI/CD GATES

Mọi Pull Request trước khi merge vào nhánh `main` phải vượt qua các cổng kiểm thử tự động tại GitHub Actions (`.github/workflows/ci.yml`):

```bash
# 1. Kiểm tra Backend
dotnet build BackE/ERMSystem.sln --configuration Release
dotnet test BackE/ERMSystem.Tests/ERMSystem.Tests.csproj

# 2. Kiểm tra Frontend
cd FontE
npm run lint    # Kiểm tra Clean Code & Boundary rules
npm run test    # Chạy toàn bộ Vitest Unit Tests
npm run build   # Build production Next.js prerender
```

> [!IMPORTANT]
> **Tuyệt đối không commit mã nguồn có chứa dữ liệu mock hoặc hardcode.** Toàn bộ dữ liệu hiển thị phải thông qua API và dịch vụ backend thực tế.
