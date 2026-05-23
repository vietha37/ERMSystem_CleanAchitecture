"use client";

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { formatDateValue } from '@/lib/dateFormatting';
import { patientService } from '@/services/patientService';
import { appointmentService } from '@/services/appointmentService';
import { medicalRecordService } from '@/services/medicalRecordService';
import { getApiErrorMessage } from '@/services/error';
import { useAuth } from '@/hooks/useAuth';
import { Appointment, MedicalRecord, Patient } from '@/services/types';
import toast from 'react-hot-toast';

type PatientViewModel = Patient & {
  status?: string;
};

type DrawerData = {
  appointments: Appointment[];
  medicalRecords: MedicalRecord[];
  isLoading: boolean;
};

export default function PatientsPage() {
  const { role } = useAuth();
  const [patients, setPatients] = useState<PatientViewModel[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Search and Pagination
  const [searchQuery, setSearchQuery] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalItems, setTotalItems] = useState(0);

  // Form Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create');
  const [selectedPatientId, setSelectedPatientId] = useState<string | null>(null);
  const firstInputRef = useRef<HTMLInputElement>(null);

  // Detail Drawer State
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [selectedPatient, setSelectedPatient] = useState<PatientViewModel | null>(null);
  const [drawerData, setDrawerData] = useState<DrawerData>({
     appointments: [],
     medicalRecords: [],
     isLoading: false
  });

  const [fullName, setFullName] = useState('');
  const [formData, setFormData] = useState({
    dateOfBirth: '',
    gender: 'Male',
    phone: '',
    address: ''
  });

  // Debounce logic
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchQuery);
      setCurrentPage(1);
    }, 500);
    return () => clearTimeout(handler);
  }, [searchQuery]);

  // Autofocus modal input
  useEffect(() => {
    if (isModalOpen && firstInputRef.current) {
      setTimeout(() => firstInputRef.current?.focus(), 100);
    }
  }, [isModalOpen]);

  // Main Fetch
  const fetchPatients = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await patientService.getAll(currentPage, pageSize, debouncedSearch);
      const mapped: PatientViewModel[] = data.items.map((p) => ({
        ...p,
        status: "Active",
      }));

      setPatients(mapped);
      setTotalPages(Math.ceil((data?.totalCount || 0) / pageSize) || 1);
      setTotalItems(data?.totalCount || 0);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể tải danh sách bệnh nhân."));
      setPatients([]);
      setTotalPages(1);
      setTotalItems(0);
    } finally {
      setIsLoading(false);
    }
  }, [currentPage, pageSize, debouncedSearch]);

  useEffect(() => {
    fetchPatients();
  }, [fetchPatients]);

  const openCreateModal = () => {
    setModalMode('create');
    setSelectedPatientId(null);
    setFullName('');
    setFormData({ dateOfBirth: '', gender: 'Male', phone: '', address: '' });
    setIsModalOpen(true);
  };

  const openEditModal = (patient: PatientViewModel, e: React.MouseEvent) => {
    e.stopPropagation(); // prevent drawer open
    setModalMode('edit');
    setSelectedPatientId(patient.id);
    setFullName(patient.fullName || '');
    setFormData({
      dateOfBirth: patient.dateOfBirth ? patient.dateOfBirth.split('T')[0] : '',
      gender: patient.gender || 'Male',
      phone: patient.phone || '',
      address: patient.address || ''
    });
    setIsModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fullName.trim() || !formData.dateOfBirth) {
      toast.error("Họ tên và ngày sinh là bắt buộc.");
      return;
    }
    
    setIsSubmitting(true);
    const payload = {
      fullName: fullName.trim(),
      ...formData
    };

    try {
      if (modalMode === 'create') {
        await patientService.create(payload);
        toast.success("Đã tạo bệnh nhân.");
        setIsModalOpen(false);
        fetchPatients();
      } else {
        if (!selectedPatientId) return;
        await patientService.update(selectedPatientId, payload);
        toast.success("Đã cập nhật bệnh nhân.");
        setIsModalOpen(false);
        fetchPatients();
        if (selectedPatient?.id === selectedPatientId) {
          setSelectedPatient({ ...selectedPatient, ...payload });
        }
      }
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể xử lý yêu cầu."));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (role === "Doctor") {
      toast.error("Bác sĩ không có quyền xóa bệnh nhân.");
      return;
    }
    if (!window.confirm("Bạn có chắc muốn xóa bệnh nhân này không? Thao tác này không thể hoàn tác.")) return;
    
    try {
      await patientService.delete(id);
      toast.success("Đã xóa bệnh nhân.");
      if (patients.length === 1 && currentPage > 1) setCurrentPage(currentPage - 1);
      else fetchPatients();
      
      if (selectedPatient?.id === id) setIsDrawerOpen(false);
    } catch (error: unknown) {
      toast.error(getApiErrorMessage(error, "Không thể xóa bệnh nhân."));
    }
  };

  // Drawer Logic
  const openDrawer = async (patient: PatientViewModel) => {
    setSelectedPatient(patient);
    setIsDrawerOpen(true);
    setDrawerData({ appointments: [], medicalRecords: [], isLoading: true });
    
    try {
       const [apptsRes, recsRes] = await Promise.all([
          appointmentService.getAll(1, 200),
          medicalRecordService.getAll(1, 200)
       ]);
       
       const allAppointments = apptsRes.items;
       const allRecords = recsRes.items;
       const patientAppts = allAppointments.filter((a) => a.patientId === patient.id);
       const appointmentIds = new Set(patientAppts.map((a) => a.id));
       const patientRecs = allRecords.filter((r) => appointmentIds.has(r.appointmentId));
       
       setDrawerData({
         appointments: patientAppts.slice(0, 3), // top 3
         medicalRecords: patientRecs,
         isLoading: false
       });
    } catch {
       setDrawerData({ appointments: [], medicalRecords: [], isLoading: false });
    }
  };

  const getInitials = (fullNameStr?: string) => {
    if (!fullNameStr) return '?';
    const parts = fullNameStr.trim().split(' ');
    return parts.length >= 2
      ? `${parts[0].charAt(0)}${parts[parts.length - 1].charAt(0)}`.toUpperCase()
      : parts[0].charAt(0).toUpperCase();
  };

  const SkeletonRows = () => (
    <>
      {[1, 2, 3, 4, 5].map((i) => (
        <tr key={i} className="animate-pulse border-b border-gray-50">
          <td className="p-4"><div className="w-10 h-10 bg-gray-200 rounded-full"></div></td>
          <td className="p-4"><div className="h-4 bg-gray-200 rounded-md w-3/4"></div></td>
          <td className="p-4"><div className="h-4 bg-gray-200 rounded-md w-12"></div></td>
          <td className="p-4"><div className="h-4 bg-gray-200 rounded-md w-28"></div></td>
          <td className="p-4"><div className="h-4 bg-gray-200 rounded-md w-4/5"></div></td>
          <td className="p-4"><div className="h-6 w-16 bg-gray-200 rounded-full"></div></td>
          <td className="p-4 flex gap-2 justify-end">
            <div className="h-8 w-16 bg-gray-200 rounded-lg"></div>
            <div className="h-8 w-16 bg-gray-200 rounded-lg"></div>
          </td>
        </tr>
      ))}
    </>
  );

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Page Header */}
      <div className="flex justify-between items-center bg-white p-6 rounded-2xl shadow-sm border border-gray-100">
        <div>
          <h1 className="text-3xl font-bold text-gray-800 tracking-tight">Bệnh nhân</h1>
          <p className="text-gray-500 text-sm mt-1">Quản lý hồ sơ bệnh nhân và theo dõi lịch sử chăm sóc tập trung.</p>
        </div>
        <Button onClick={openCreateModal} className="shadow-md hover:shadow-lg transform transition-all hover:-translate-y-0.5">
           + Thêm bệnh nhân
        </Button>
      </div>

      <Card className="p-6 border-none shadow-sm rounded-2xl bg-white">
        {/* Controls Bar */}
        <div className="mb-6 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
          <div className="relative w-full md:w-[400px]">
            <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-gray-400">🔍</span>
            <input
              type="text"
              placeholder="Tìm theo tên bệnh nhân..."
              className="w-full pl-10 pr-4 py-2.5 border border-gray-200 rounded-xl focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all text-sm shadow-sm"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>
          
          <div className="flex items-center gap-3 bg-gray-50 px-4 py-2 rounded-xl border border-gray-100">
            <span className="text-sm font-medium text-gray-500">Hiển thị</span>
            <select 
              className="bg-transparent text-sm font-bold text-gray-700 outline-none cursor-pointer"
              value={pageSize}
              onChange={(e) => {
                 setPageSize(Number(e.target.value));
                 setCurrentPage(1);
              }}
            >
              <option value={5}>5 / trang</option>
              <option value={10}>10 / trang</option>
              <option value={20}>20 / trang</option>
            </select>
          </div>
        </div>

        {/* Enhanced Table */}
        <div className="rounded-2xl overflow-hidden border border-gray-100 shadow-sm relative h-[600px] bg-white">
            <div className="overflow-y-auto h-full scrollbar-thin scrollbar-thumb-gray-200">
              <table className="w-full text-left border-collapse min-w-max relative">
                <thead className="sticky top-0 bg-white/95 backdrop-blur-md z-10 border-b border-gray-200 shadow-sm">
                  <tr>
                    <th className="p-4 text-xs font-bold text-gray-500 uppercase tracking-wider">Mã nhận diện</th>
                    <th className="p-4 text-xs font-bold text-gray-500 uppercase tracking-wider w-1/4">Họ và tên</th>
                    <th className="p-4 text-xs font-bold text-gray-500 uppercase tracking-wider">Giới tính</th>
                    <th className="p-4 text-xs font-bold text-gray-500 uppercase tracking-wider">Điện thoại</th>
                    <th className="p-4 text-xs font-bold text-gray-500 uppercase tracking-wider w-1/4">Địa chỉ</th>
                    <th className="p-4 text-xs font-bold text-gray-500 uppercase tracking-wider">Trạng thái</th>
                    <th className="p-4 text-xs font-bold text-gray-500 uppercase tracking-wider text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {isLoading ? (
                    <SkeletonRows />
                  ) : patients.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="p-16 text-center text-gray-500 bg-gray-50/50">
                        <div className="text-4xl mb-3">📭</div>
                        <p className="font-medium">{debouncedSearch ? 'Không có bệnh nhân nào khớp từ khóa tìm kiếm.' : 'Chưa có hồ sơ bệnh nhân nào.'}</p>
                      </td>
                    </tr>
                  ) : (
                    patients.map((patient, index) => (
                      <tr 
                        key={patient.id} 
                        onClick={() => openDrawer(patient)}
                        style={{ animationDelay: `${index * 50}ms` }}
                        className="animate-fade-in cursor-pointer bg-white hover:bg-blue-50/40 hover:shadow-md transform hover:-translate-y-0.5 transition-all duration-200 relative z-0 hover:z-0"
                      >
                        <td className="p-4">
                          <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-100 to-blue-200 text-blue-700 flex items-center justify-center font-bold text-sm border border-blue-200 shadow-sm shadow-blue-100">
                            {getInitials(patient.fullName)}
                          </div>
                        </td>
                        <td className="p-4">
                           <div className="font-semibold text-gray-800">{patient.fullName}</div>
                           <div className="text-xs text-gray-500 mt-0.5">{patient.dateOfBirth ? formatDateValue(patient.dateOfBirth, 'Chưa có ngày sinh') : 'Chưa có ngày sinh'}</div>
                        </td>
                        <td className="p-4">
                           <span className={`px-2.5 py-1 rounded-lg text-xs font-bold ${
                             patient.gender === 'Male' ? 'bg-cyan-50 text-cyan-700 border border-cyan-100' : 
                             patient.gender === 'Female' ? 'bg-pink-50 text-pink-700 border border-pink-100' : 
                             'bg-gray-100 text-gray-700 border border-gray-200'
                           }`}>
                             {patient.gender === 'Male' ? 'Nam' : patient.gender === 'Female' ? 'Nữ' : patient.gender === 'Other' ? 'Khác' : 'Chưa rõ'}
                           </span>
                        </td>
                        <td className="p-4 text-sm font-medium text-gray-600">{patient.phone || 'Chưa có'}</td>
                        <td className="p-4 text-sm text-gray-500 truncate max-w-[200px]">{patient.address || 'Chưa có'}</td>
                        <td className="p-4">
                            <span className="flex items-center gap-1.5 px-2.5 py-1 bg-green-50 text-green-700 border border-green-100 rounded-full text-xs font-bold w-max">
                               <span className="w-2 h-2 rounded-full bg-green-500 animate-pulse"></span>
                               {patient.status === 'Active' || !patient.status ? 'Đang hoạt động' : patient.status}
                            </span>
                        </td>
                        <td className="p-4 flex justify-end gap-2">
                          <button
                            onClick={(e) => openEditModal(patient, e)}
                            className="bg-white border border-gray-200 text-gray-600 hover:text-blue-600 hover:border-blue-300 hover:bg-blue-50 px-3 py-1.5 rounded-lg transition-colors text-sm font-semibold shadow-sm"
                            title="Sửa bệnh nhân"
                          >
                            Sửa
                          </button>
                          {role !== "Doctor" && (
                            <button
                              onClick={(e) => handleDelete(patient.id, e)}
                              className="bg-white border border-red-100 text-red-500 hover:bg-red-500 hover:text-white hover:border-red-600 px-3 py-1.5 rounded-lg transition-colors text-sm font-semibold shadow-sm"
                              title="Xóa bệnh nhân"
                            >
                              Xóa
                            </button>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
        </div>

        {/* Pagination */}
        {!isLoading && totalPages > 0 && (
          <div className="mt-5 flex justify-between items-center text-sm">
            <div className="text-gray-500 font-medium">
               Hiển thị <span className="text-gray-900 font-bold">{(currentPage - 1) * pageSize + 1}</span> đến <span className="text-gray-900 font-bold">{Math.min(currentPage * pageSize, totalItems || patients.length)}</span> trong tổng số <span className="text-blue-600 font-bold">{totalItems || patients.length}</span> bệnh nhân
            </div>
            <div className="flex gap-1 items-center bg-gray-50 border border-gray-200 shadow-sm p-1 rounded-xl">
              <button 
                disabled={currentPage === 1}
                onClick={() => setCurrentPage(1)}
                className="px-3 py-1.5 rounded-lg font-bold disabled:opacity-40 hover:bg-white hover:shadow-sm text-gray-600 transition-all"
              >«</button>
              <button 
                disabled={currentPage === 1}
                onClick={() => setCurrentPage(p => p - 1)}
                className="px-3 py-1.5 rounded-lg font-bold disabled:opacity-40 hover:bg-white hover:shadow-sm text-gray-600 transition-all"
              >Trước</button>
              <span className="px-4 py-1.5 font-bold text-blue-700 bg-blue-100/50 rounded-lg shadow-inner">
                 {currentPage} / {totalPages}
              </span>
              <button 
                disabled={currentPage === totalPages}
                onClick={() => setCurrentPage(p => p + 1)}
                className="px-3 py-1.5 rounded-lg font-bold disabled:opacity-40 hover:bg-white hover:shadow-sm text-gray-600 transition-all"
              >Tiếp</button>
            </div>
          </div>
        )}
      </Card>

      {/* DETAIL DRAWER OVERLAY */}
      {isDrawerOpen && (
         <div 
           className="fixed inset-0 bg-gray-900/30 backdrop-blur-sm z-40 transition-opacity duration-300"
           onClick={() => setIsDrawerOpen(false)}
         />
      )}
      
      {/* DETAIL DRAWER COMPONENT */}
      <div 
        className={`fixed right-0 top-0 h-full w-full max-w-md bg-white shadow-2xl z-50 transform transition-transform duration-300 ease-in-out flex flex-col ${isDrawerOpen ? 'translate-x-0' : 'translate-x-full'}`}
      >
        {selectedPatient && (
          <>
            <div className="px-6 py-5 border-b border-gray-100 flex justify-between items-center bg-blue-50/30 backdrop-blur-sm sticky top-0 z-10">
              <h2 className="text-xl font-bold text-gray-800">Patient Details</h2>
              <button 
                onClick={() => setIsDrawerOpen(false)}
                className="w-8 h-8 flex items-center justify-center rounded-full bg-gray-100 text-gray-500 hover:bg-red-100 hover:text-red-500 transition-colors font-bold"
              >✕</button>
            </div>
            
            <div className="flex-1 overflow-y-auto p-6 space-y-8 scrollbar-thin scrollbar-thumb-gray-200">
               {/* Header Profile Section */}
               <div className="flex flex-col items-center text-center">
                  <div className="w-24 h-24 rounded-full bg-gradient-to-br from-blue-500 to-blue-600 text-white flex items-center justify-center font-bold text-3xl shadow-lg border-4 border-blue-50 mb-4">
                     {getInitials(selectedPatient.fullName)}
                  </div>
                  <h3 className="text-2xl font-bold text-gray-900">{selectedPatient.fullName}</h3>
                  <div className="flex items-center gap-2 mt-2">
                     <span className="px-3 py-1 bg-green-100 text-green-700 rounded-full text-xs font-bold uppercase tracking-wide">
                        {selectedPatient.status || 'Active'}
                     </span>
                     <span className="px-3 py-1 bg-gray-100 text-gray-600 rounded-full text-xs font-bold flex items-center gap-1">
                        Ngày sinh: {selectedPatient.dateOfBirth ? formatDateValue(selectedPatient.dateOfBirth, 'Chưa có') : 'Chưa có'}
                     </span>
                  </div>
               </div>

               {/* Contact Info */}
               <div className="bg-gray-50 rounded-2xl p-4 border border-gray-100 space-y-3 shadow-inner">
                  <div className="flex items-start gap-3">
                     <div className="mt-0.5 text-blue-500">📞</div>
                     <div>
                        <div className="text-xs font-bold text-gray-400 uppercase">Số điện thoại</div>
                        <div className="font-semibold text-gray-800">{selectedPatient.phone || 'Chưa có số điện thoại'}</div>
                     </div>
                  </div>
                  <div className="flex items-start gap-3">
                     <div className="mt-0.5 text-blue-500">🏠</div>
                     <div>
                        <div className="text-xs font-bold text-gray-400 uppercase">Địa chỉ</div>
                        <div className="font-semibold text-gray-800">{selectedPatient.address || 'Chưa có địa chỉ'}</div>
                     </div>
                  </div>
               </div>

               {/* Medical Summary Card */}
               <div className="bg-gradient-to-br from-blue-50 to-blue-100/50 rounded-2xl p-5 border border-blue-100 shadow-sm relative overflow-hidden">
                  <div className="absolute -right-4 -top-4 text-7xl opacity-5">📋</div>
                  <h4 className="text-sm font-bold text-blue-800 uppercase tracking-wide mb-4 flex items-center gap-2">
                     <span className="w-2 h-2 rounded-full bg-blue-500"></span>
                     Tóm tắt hồ sơ
                  </h4>
                  
                  {drawerData.isLoading ? (
                    <div className="animate-pulse space-y-3">
                       <div className="h-4 bg-blue-200/50 rounded w-full"></div>
                       <div className="h-4 bg-blue-200/50 rounded w-2/3"></div>
                       <div className="h-4 bg-blue-200/50 rounded w-3/4"></div>
                    </div>
                  ) : (
                    <div className="space-y-4">
                       <div className="flex justify-between items-center bg-white/60 p-3 rounded-xl border border-white">
                          <span className="text-gray-600 font-medium text-sm">Tổng lịch hẹn</span>
                          <span className="text-lg font-bold text-blue-700">{drawerData.appointments.length}</span>
                       </div>
                       <div className="flex justify-between items-center bg-white/60 p-3 rounded-xl border border-white">
                          <span className="text-gray-600 font-medium text-sm">Lần khám gần nhất</span>
                          <span className="font-bold text-gray-800 text-sm">
                            {drawerData.appointments.length > 0 ? formatDateValue(drawerData.appointments[0].appointmentDate, 'Chưa từng') : 'Chưa từng'}
                          </span>
                       </div>
                       <div className="flex justify-between items-center bg-white/60 p-3 rounded-xl border border-white">
                          <span className="text-gray-600 font-medium text-sm">Chẩn đoán gần nhất</span>
                          <span className="font-bold text-blue-600 text-sm">
                            {drawerData.medicalRecords.length > 0 ? drawerData.medicalRecords[0].diagnosis : 'Chưa có'}
                          </span>
                       </div>
                    </div>
                  )}
               </div>

               {/* Appointment History Preview Table */}
               <div>
                  <h4 className="text-sm font-bold text-gray-800 uppercase tracking-wide mb-3 px-1">Lịch hẹn gần đây</h4>
                  <div className="border border-gray-100 rounded-2xl overflow-hidden shadow-sm bg-white">
                    <table className="w-full text-left text-sm">
                      <thead className="bg-gray-50 border-b border-gray-100">
                         <tr>
                            <th className="px-4 py-3 font-semibold text-gray-500">Ngày</th>
                            <th className="px-4 py-3 font-semibold text-gray-500">Bác sĩ</th>
                            <th className="px-4 py-3 font-semibold text-gray-500 whitespace-nowrap">Trạng thái</th>
                         </tr>
                      </thead>
                      <tbody className="divide-y divide-gray-50">
                         {drawerData.isLoading ? (
                           <tr><td colSpan={3} className="px-4 py-6 text-center text-sm text-gray-400">Đang tải lịch sử...</td></tr>
                         ) : drawerData.appointments.length === 0 ? (
                           <tr><td colSpan={3} className="px-4 py-6 text-center text-sm text-gray-400">Chưa có lịch hẹn nào.</td></tr>
                         ) : (
                            drawerData.appointments.map((a) => (
                              <tr key={a.id} className="hover:bg-blue-50/30 transition-colors">
                                 <td className="px-4 py-3 text-gray-600 font-medium">{formatDateValue(a.appointmentDate)}</td>
                                 <td className="px-4 py-3 font-semibold text-gray-800">Bác sĩ phụ trách</td>
                                <td className="px-4 py-3">
                                  <span className={`px-2 py-0.5 rounded-md text-[10px] font-bold uppercase tracking-wider ${
                                    a.status === 'Completed' ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'
                                  }`}>
                                    {a.status}
                                  </span>
                                </td>
                             </tr>
                           ))
                         )}
                      </tbody>
                    </table>
                  </div>
               </div>

            </div>

            {/* Quick Actions Base */}
            <div className="p-6 border-t border-gray-100 bg-gray-50 flex flex-col gap-3 shrink-0">
               <button 
                 onClick={() => { toast("Chuyển sang màn tạo lịch hẹn..."); setIsDrawerOpen(false); }}
                 className="w-full bg-blue-600 hover:bg-blue-700 text-white font-bold py-3.5 rounded-xl shadow-md hover:shadow-lg transition-all"
               >
                 + Tạo lịch hẹn
               </button>
               <button 
                 onClick={() => { toast("Chuyển sang màn hồ sơ khám..."); setIsDrawerOpen(false); }}
                 className="w-full bg-white hover:bg-gray-50 text-blue-700 border border-blue-200 font-bold py-3.5 rounded-xl shadow-sm transition-all"
               >
                 Xem toàn bộ hồ sơ khám
               </button>
            </div>
          </>
        )}
      </div>

      {/* CREATE/EDIT MODAL FORM */}
      <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title={modalMode === 'create' ? 'Thêm bệnh nhân mới' : 'Cập nhật hồ sơ bệnh nhân'}>
        <form onSubmit={handleSubmit} className="space-y-4 mt-2 px-1 pb-2">
          
          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1.5">Họ và tên <span className="text-red-500">*</span></label>
            <input 
              ref={firstInputRef}
              type="text" 
              className="w-full px-4 py-2.5 border border-gray-300 rounded-xl focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all placeholder:text-gray-400 font-medium text-gray-800 shadow-sm"
              placeholder="e.g. John Doe"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              required
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-bold text-gray-700 mb-1.5">Ngày sinh <span className="text-red-500">*</span></label>
              <input 
                type="date" 
                className="w-full px-4 py-2.5 border border-gray-300 rounded-xl focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all font-medium text-gray-800 shadow-sm"
                value={formData.dateOfBirth}
                onChange={(e) => setFormData({...formData, dateOfBirth: e.target.value})}
                required
              />
            </div>
            
            <div>
              <label className="block text-sm font-bold text-gray-700 mb-1.5">Gender</label>
              <select 
                className="w-full px-4 py-2.5 bg-white border border-gray-300 rounded-xl focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all cursor-pointer font-medium text-gray-800 shadow-sm"
                value={formData.gender}
                onChange={(e) => setFormData({...formData, gender: e.target.value})}
              >
                <option value="Male">Male</option>
                <option value="Female">Female</option>
                <option value="Other">Other</option>
              </select>
            </div>
          </div>

          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1.5">Số điện thoại</label>
            <input 
              type="tel" 
              className="w-full px-4 py-2.5 border border-gray-300 rounded-xl focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all placeholder:text-gray-400 font-medium text-gray-800 shadow-sm"
              placeholder="+1 234 567 8900"
              value={formData.phone}
              onChange={(e) => setFormData({...formData, phone: e.target.value})}
            />
          </div>

          <div>
            <label className="block text-sm font-bold text-gray-700 mb-1.5">Địa chỉ thường trú</label>
            <textarea 
              rows={3}
              className="w-full px-4 py-2.5 border border-gray-300 rounded-xl focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 outline-none transition-all resize-y placeholder:text-gray-400 font-medium text-gray-800 shadow-sm"
              placeholder="Full address here..."
              value={formData.address}
              onChange={(e) => setFormData({...formData, address: e.target.value})}
            />
          </div>

          <div className="pt-6 border-t border-gray-100 flex justify-end gap-3">
            <button 
               type="button" 
               onClick={() => setIsModalOpen(false)}
               className="px-5 py-2.5 rounded-xl font-bold bg-gray-100 text-gray-600 hover:bg-gray-200 transition-colors"
            >
               Hủy
            </button>
            <button 
               type="submit" 
               disabled={isSubmitting}
               className="px-6 py-2.5 rounded-xl font-bold bg-blue-600 text-white hover:bg-blue-700 transition-colors shadow-md hover:shadow-lg disabled:opacity-50 disabled:shadow-none flex items-center gap-2"
            >
              {isSubmitting ? (
                 <>
                   <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                   {modalMode === 'create' ? 'Đang tạo...' : 'Đang cập nhật...'}
                 </>
              ) : (
                 modalMode === 'create' ? 'Lưu bệnh nhân' : 'Cập nhật thay đổi'
              )}
            </button>
          </div>
        </form>
      </Modal>


    </div>
  );
}
