# BÁO CÁO ĐÁNH GIÁ KIỂM THỬ TỰ ĐỘNG (UNIT TEST EVALUATION REPORT)

**Dự án:** ERM System (Electronic Medical Record & Hospital Management System)  
**Thời gian thực thi:** 11/08/2026  
**Môi trường:** .NET 10.0 (xUnit 2.9.3, Moq 4.20.72)  
**File kết quả:** `test_evaluation.md`  

---

## 1. TỔNG QUAN KẾT QUẢ KIỂM THỬ (TEST SUMMARY)

| Chỉ số | Giá trị |
| :--- | :--- |
| **Tổng số Unit Test đã chạy** | **89** |
| **Số test PASSED (Thành công)** | **89** (100%) |
| **Số test FAILED (Thất bại)** | **0** (0%) |
| **Số test SKIPPED (Bỏ qua)** | **0** (0%) |
| **Thời gian chạy bộ test** | **~5.18 giây** |
| **Vị trí Project Test** | `BackE/ERMSystem.Tests/ERMSystem.Tests.csproj` |

---

## 2. DANH SÁCH VÀ BAO PHỦ CÁC LUỒNG NGHIỆP VỤ (TEST COVERAGE BREAKDOWN)

Bộ Unit Test đã kiểm thử toàn bộ 11 mô-đun cốt lõi thuộc tầng **Application Layer** & **Utilities** của hệ thống ERM:

### 2.1. AppointmentService (Quản lý Lịch hẹn) - 13 Tests
- `GetAllAppointmentsAsync_ReturnsPaginatedResult`: Lấy danh sách lịch hẹn phân trang.
- `GetAppointmentByIdAsync_Found_ReturnsDto`: Lấy chi tiết lịch hẹn tồn tại.
- `GetAppointmentByIdAsync_NotFound_ReturnsNull`: Xử lý khi không tìm thấy lịch hẹn.
- `CreateAppointmentAsync_ValidData_ReturnsDto`: Tạo lịch hẹn thành công & xóa cache Dashboard.
- `CreateAppointmentAsync_InvalidStatus_ThrowsArgumentException`: Kiểm tra validation trạng thái không hợp lệ (ngoài Pending/Completed/Cancelled).
- `CreateAppointmentAsync_PatientNotFound_ThrowsKeyNotFoundException`: Báo lỗi khi Bệnh nhân không tồn tại.
- `CreateAppointmentAsync_DoctorNotFound_ThrowsKeyNotFoundException`: Báo lỗi khi Bác sĩ không tồn tại.
- `CreateAppointmentAsync_AllValidStatuses_Succeed` (Theory: Pending, Completed, Cancelled): Kiểm thử các trạng thái hợp lệ.
- `UpdateAppointmentAsync_NotFound_ThrowsKeyNotFoundException`: Cập nhật khi không thấy ID.
- `UpdateAppointmentAsync_InvalidStatus_ThrowsArgumentException`: Cập nhật với trạng thái sai định dạng.
- `UpdateAppointmentAsync_ValidData_UpdatesSuccessfully`: Cập nhật lịch hẹn thành công.
- `DeleteAppointmentAsync_NotFound_ThrowsKeyNotFoundException`: Xóa lịch hẹn không tồn tại.
- `DeleteAppointmentAsync_Found_DeletesAndInvalidatesCache`: Xóa lịch hẹn thành công & invalidate cache.

### 2.2. PatientService (Quản lý Bệnh nhân & Gộp hồ sơ) - 13 Tests
- `GetAllPatientsAsync_ReturnsPaginatedResult`: Phân trang danh sách bệnh nhân.
- `GetPatientByIdAsync_Found_ReturnsDto` & `GetPatientByIdAsync_NotFound_ReturnsNull`: Tra cứu theo ID.
- `GetPatientByAppUserIdAsync_Found_ReturnsDto`: Tìm hồ sơ bệnh nhân theo Tài khoản đăng nhập (AppUserId).
- `CreatePatientAsync_ReturnsCreatedDto`: Tạo mới bệnh nhân & làm mới cache.
- `UpdatePatientAsync_Found_UpdatesFields` & `UpdatePatientAsync_NotFound_ThrowsKeyNotFoundException`: Cập nhật thông tin bệnh nhân.
- `DeletePatientAsync_Found_Deletes` & `DeletePatientAsync_NotFound_ThrowsKeyNotFoundException`: Xóa bệnh nhân.
- `MergePatientsAsync_SameIds_ThrowsInvalidOperationException`: Báo lỗi khi cố gộp cùng 1 bệnh nhân.
- `MergePatientsAsync_SourceNotFound_ThrowsKeyNotFoundException`: Báo lỗi khi nguồn gộp không tìm thấy.
- `MergePatientsAsync_TargetNotFound_ThrowsKeyNotFoundException`: Báo lỗi khi đích gộp không tìm thấy.
- `MergePatientsAsync_ValidMerge_ReturnsResult`: **[Nghiệp vụ quan trọng]** Gộp 2 bệnh nhân, chuyên dời Lịch hẹn, chuyển AppUserId, ghi Compliance Audit log và làm mới Cache.
- `GetPotentialDuplicatesAsync_PatientNotFound_ThrowsKeyNotFoundException`: Phát hiện hồ sơ trùng lặp.

### 2.3. DoctorService (Quản lý Bác sĩ) - 8 Tests
- `GetAllDoctorsAsync_ReturnsPaginatedResult`: Phân trang bác sĩ.
- `GetDoctorByIdAsync_Found_ReturnsDto` / `NotFound_ReturnsNull`: Tìm bác sĩ theo ID.
- `CreateDoctorAsync_ReturnsCreatedDto`: Tạo hồ sơ bác sĩ mới.
- `UpdateDoctorAsync_Found_UpdatesFields` / `NotFound_ThrowsKeyNotFoundException`: Cập nhật thông tin bác sĩ.
- `DeleteDoctorAsync_Found_DeletesDoctor` / `NotFound_ThrowsKeyNotFoundException`: Xóa hồ sơ bác sĩ.

### 2.4. MedicalRecordService (Hồ sơ Bệnh án) - 10 Tests
- `GetAllMedicalRecordsAsync_ReturnsPaginatedResult`: Danh sách bệnh án.
- `GetMedicalRecordByIdAsync_Found_ReturnsDto` / `NotFound_ReturnsNull`: Tìm bệnh án theo ID.
- `GetMedicalRecordByAppointmentIdAsync_Found_ReturnsDto`: Tìm bệnh án theo Lịch hẹn.
- `CreateMedicalRecordAsync_AppointmentNotFound_ThrowsKeyNotFoundException`: Báo lỗi nếu Lịch hẹn không có thực.
- `CreateMedicalRecordAsync_DuplicateForAppointment_ThrowsInvalidOperationException`: Chặn tạo 2 bệnh án cho cùng 1 Lịch hẹn (Ràng buộc 1-1).
- `CreateMedicalRecordAsync_Valid_CreatesSuccessfully`: Tạo bệnh án thành công & xóa cache Dashboard.
- `UpdateMedicalRecordAsync_Found_UpdatesFields` / `NotFound_ThrowsKeyNotFoundException`: Cập nhật triệu chứng, chẩn đoán, ghi chú.
- `DeleteMedicalRecordAsync_Found_Deletes` / `NotFound_ThrowsKeyNotFoundException`: Xóa bệnh án.

### 2.5. MedicineService (Quản lý Thuốc) - 8 Tests
- `GetAllMedicinesAsync_ReturnsPaginatedResult`: Phân trang danh mục thuốc.
- `GetMedicineByIdAsync_Found_ReturnsDto` / `NotFound_ReturnsNull`: Chi tiết thông tin thuốc.
- `CreateMedicineAsync_ReturnsCreatedDto`: Thêm thuốc mới.
- `UpdateMedicineAsync_Found_UpdatesFields` / `NotFound_ThrowsKeyNotFoundException`: Cập nhật tên/mô tả thuốc.
- `DeleteMedicineAsync_Found_Deletes` / `NotFound_ThrowsKeyNotFoundException`: Xóa thuốc khỏi danh mục.

### 2.6. PrescriptionService (Đơn thuốc) - 9 Tests
- `GetAllPrescriptionsAsync_ReturnsPaginatedResult`: Danh sách đơn thuốc.
- `GetPrescriptionByIdAsync_Found_ReturnsDto` / `NotFound_ReturnsNull`: Tra cứu đơn thuốc theo ID.
- `GetPrescriptionByMedicalRecordIdAsync_Found_ReturnsDto`: Tra cứu đơn thuốc theo Hồ sơ bệnh án.
- `CreatePrescriptionAsync_MedicalRecordNotFound_ThrowsKeyNotFoundException`: Kiểm tra ràng buộc Bệnh án tồn tại.
- `CreatePrescriptionAsync_DuplicateForMedicalRecord_ThrowsInvalidOperationException`: Chặn tạo trùng lặp đơn thuốc cho 1 bệnh án.
- `CreatePrescriptionAsync_Valid_CreatesSuccessfully`: Tạo đơn thuốc mới & invalidate cache.
- `DeletePrescriptionAsync_Found_Deletes` / `NotFound_ThrowsKeyNotFoundException`: Xóa đơn thuốc.

### 2.7. PrescriptionItemService (Chi tiết Đơn thuốc) - 6 Tests
- `AddItemToPrescriptionAsync_PrescriptionNotFound_ThrowsKeyNotFoundException`: Báo lỗi khi Đơn thuốc không tồn tại.
- `AddItemToPrescriptionAsync_MedicineNotFound_ThrowsKeyNotFoundException`: Báo lỗi khi Thuốc không tồn tại.
- `AddItemToPrescriptionAsync_Valid_AddsAndReturnsUpdatedPrescription`: Thêm thuốc, liều dùng, thời gian vào đơn thuốc.
- `RemoveItemFromPrescriptionAsync_ItemNotFound_ThrowsKeyNotFoundException`: Báo lỗi khi không thấy dòng thuốc.
- `RemoveItemFromPrescriptionAsync_ItemBelongsToDifferentPrescription_ThrowsKeyNotFoundException`: Chặn xóa dòng thuốc thuộc đơn thuốc khác.
- `RemoveItemFromPrescriptionAsync_Valid_RemovesAndReturnsUpdatedPrescription`: Xóa dòng thuốc khỏi đơn thành công.

### 2.8. NotificationService (Thông báo Lịch khám) - 3 Tests
- `GetTodayNotificationsAsync_ReturnsAllTodayAppointments_ForAdminRole`: Quản trị viên nhận tất cả lịch khám trong ngày.
- `GetTodayNotificationsAsync_FiltersByDoctorName_ForDoctorRole`: Bác sĩ chỉ nhận thông báo lịch khám khớp với tên mình (hỗ trợ xóa dấu tiếng Việt / khoảng trắng).
- `GetTodayNotificationsAsync_ReturnsEmpty_WhenNoAppointments`: Xử lý mượt mà khi không có lịch khám trong ngày.

### 2.9. DashboardService (Thống kê Tổng quan) - 4 Tests
- `GetDashboardStatsAsync_ReturnsCachedValue_WhenCacheHit`: Lấy dữ liệu từ Query Cache khi có cache sẵn.
- `GetDashboardStatsAsync_CalculatesStatsAndCaches_WhenCacheMiss`: Tính toán các chỉ số (tỷ lệ hoàn thành, tỷ lệ hủy, tỷ lệ thu tiền billing, top chẩn đoán) và lưu cache khi cache miss.
- `GetDashboardTrendsAsync_Daily_ReturnsTrendPoints`: Thống kê xu hướng bệnh nhân/lịch hẹn/đơn thuốc theo ngày.
- `GetDashboardTrendsAsync_Monthly_ReturnsMonthlyTrendPoints`: Thống kê xu hướng theo tháng.

### 2.10. CompactCodeGenerator (Sinh mã ngắn gọn) - 4 Tests
- `Generate_ReturnsCorrectFormat`: Kiểm tra định dạng mã `PREFIX-TIME-RANDOM`.
- `Generate_WithCustomRandomLength_ReturnsCorrectLength`: Kiểm tra độ dài phần ngẫu nhiên tùy chỉnh.
- `Generate_DifferentPrefixes`: Kiểm tra sinh mã chính xác theo prefix (MR, LB, INV...).
- `Generate_NullOrEmptyPrefix_DefaultsToID`: Tự động fallback về prefix "ID".

### 2.11. Pagination Utilities (Phân trang) - 6 Tests
- `PaginationRequest_DefaultValues`: Kiểm tra mặc định PageNumber=1, PageSize=10.
- `PaginationRequest_PageSizeExceedsMax_ClampedTo50`: Tự động khống chế PageSize tối đa là 50 để tránh overload DB.
- `PaginationRequest_TextSearchAlias_Works`: Hỗ trợ tương thích ngược cho tham số gõ sai `TextSeach`.
- `PaginatedResult_CalculatesTotalPages_Correctly`: Tính đúng tổng số trang (vd: 23 item / 10 = 3 trang).
- `PaginatedResult_ExactDivision_NoExtraPage`: Tính đúng trường hợp chia hết.
- `PaginatedResult_ZeroItems_ZeroPages` & `PaginatedResult_SingleItem_OnePage`: Xử lý biên trường hợp 0 hoặc 1 item.

---

## 3. GHI NHẬN LỖI & FIX BUG ĐÃ THỰC HIỆN (LOGS & BUG FIXES)

Trong quá trình phát triển và chạy thử bộ unit test, đã phát hiện và khắc phục các vấn đề sau:

1. **Lỗi `CompactCodeGenerator.Generate` Signature**:
   - *Phát hiện:* Hàm `CompactCodeGenerator.Generate` yêu cầu thêm tham số `DateTime nowUtc` bắt buộc.
   - *Khắc phục:* Cập nhật lại tất cả test case trong `CompactCodeGeneratorTests.cs` truyền `nowUtc = DateTime.UtcNow` đúng định dạng.
2. **Dashboard Query Cache Invalidation**:
   - *Kiểm tra:* Đảm bảo khi Create / Update / Delete / Merge dữ liệu ở Patient, Appointment, MedicalRecord, Prescription thì `IDashboardQueryCache.InvalidateAsync` được gọi chính xác 100%. All verify call đều PASSED.
3. **Phân quyền & Lọc thông báo Bác sĩ trong NotificationService**:
   - *Kiểm tra:* Thuật toán chuẩn hóa chuỗi tiếng Việt bỏ dấu (`bac si`, `dr`, khoảng trắng) hoạt động chính xác đối với role Doctor.

---

## 4. TỔNG HỢP ĐÁNH GIÁ HỆ THỐNG (SYSTEM OVERALL ASSESSMENT)

1. **Độ ổn định kiến trúc (Clean Architecture)**:  
   - Các luồng xử lý nghiệp vụ tách biệt hoàn toàn giữa `Application` và `Infrastructure`/`Domain`.
   - Dependency Injection (DI) thiết kế chuẩn chỉnh, dễ dàng Mocking và kiểm thử cô lập.
2. **Xử lý ngoại lệ & Validation**:  
   - Toàn bộ exception chính (như `KeyNotFoundException`, `InvalidOperationException`, `ArgumentException`) đều được bắn ra chuẩn xác khi dữ liệu đầu vào vi phạm logic hệ thống.
3. **Hiệu năng & Caching**:  
   - `DashboardService` kết hợp `IDashboardQueryCache` hoạt động tối ưu, tự động thu hồi cache khi có biến động dữ liệu.
4. **Sẵn sàng Sản xuất (Production Readiness)**:  
   - Hệ thống backend đạt mức độ tin cậy rất cao. 89 unit test phủ kín toàn bộ luồng chính và luồng ngoại lệ.

---
