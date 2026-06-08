export type QuickAction = {
  title: string;
  description: string;
  href: string;
  accent: string;
};

export type ServiceCategory = {
  title: string;
  summary: string;
  idealFor: string;
  duration: string;
  priceRange: string;
  items: string[];
};

export type Specialty = {
  title: string;
  lead: string;
  description: string;
  symptoms: string[];
};

export type DoctorProfile = {
  name: string;
  specialty: string;
  title: string;
  experience: string;
  focus: string[];
};

export type NewsArticle = {
  title: string;
  category: string;
  summary: string;
  readTime: string;
};

export const hospitalStats = [
  { value: "15-30'", label: "Thời gian phản hồi yêu cầu đặt lịch" },
  { value: "18", label: "Chuyên khoa và đơn vị cận lâm sàng" },
  { value: "24/7", label: "Hotline hỗ trợ đặt lịch và cấp cứu ban đầu" },
  { value: "1 hồ sơ", label: "Lưu toàn bộ lịch khám, kết quả và đơn thuốc" },
];

export const quickActions: QuickAction[] = [
  {
    title: "Đặt lịch khám",
    description: "Chọn chuyên khoa, bác sĩ, ngày giờ và nhận xác nhận từ bộ phận điều phối.",
    href: "/booking",
    accent: "bg-cyan-600",
  },
  {
    title: "Tìm chuyên khoa",
    description: "Xem nhóm triệu chứng thường gặp để chọn đúng cửa vào khi chưa biết nên khám ở đâu.",
    href: "/specialties",
    accent: "bg-emerald-600",
  },
  {
    title: "Xem gói dịch vụ",
    description: "So sánh khám tổng quát, xét nghiệm, tầm soát chuyên sâu và gói doanh nghiệp.",
    href: "/services",
    accent: "bg-blue-700",
  },
  {
    title: "Cổng bệnh nhân",
    description: "Theo dõi lịch hẹn, hồ sơ khám, đơn thuốc, hóa đơn và thanh toán trực tuyến.",
    href: "/portal",
    accent: "bg-slate-800",
  },
];

export const serviceCategories: ServiceCategory[] = [
  {
    title: "Khám tổng quát cá nhân",
    summary: "Gói khám nền tảng cho người cần kiểm tra sức khỏe định kỳ hoặc có triệu chứng chưa rõ nguyên nhân.",
    idealFor: "Người trưởng thành, gia đình, người có bệnh mạn tính cần theo dõi.",
    duration: "60-120 phút",
    priceRange: "Từ 650.000đ",
    items: [
      "Khám nội tổng quát và đo sinh hiệu",
      "Xét nghiệm máu, nước tiểu cơ bản",
      "Tư vấn kết quả và kế hoạch theo dõi sau khám",
    ],
  },
  {
    title: "Tầm soát chuyên sâu",
    summary: "Thiết kế theo nhóm nguy cơ để phát hiện sớm bất thường tim mạch, tiêu hóa, chuyển hóa và ung thư.",
    idealFor: "Người trên 35 tuổi, có tiền sử gia đình hoặc lối sống nguy cơ.",
    duration: "1 buổi hoặc 1 ngày",
    priceRange: "Từ 2.900.000đ",
    items: [
      "Tư vấn chọn gói theo tuổi, giới và tiền sử",
      "Chẩn đoán hình ảnh, xét nghiệm chuyên sâu",
      "Báo cáo tổng hợp kèm khuyến nghị tái khám",
    ],
  },
  {
    title: "Sản phụ khoa và nhi khoa",
    summary: "Theo dõi sức khỏe mẹ và bé trong cùng một hồ sơ, giảm lặp lại thông tin ở mỗi lần khám.",
    idealFor: "Phụ nữ mang thai, phụ nữ cần khám định kỳ, trẻ em cần theo dõi tăng trưởng.",
    duration: "45-90 phút",
    priceRange: "Từ 500.000đ",
    items: [
      "Khám thai, siêu âm, sàng lọc trước sinh",
      "Khám phụ khoa định kỳ và tư vấn sức khỏe sinh sản",
      "Khám nhi tổng quát, dinh dưỡng và hô hấp",
    ],
  },
  {
    title: "Khám sức khỏe doanh nghiệp",
    summary: "Tổ chức khám định kỳ theo phòng ban, ca làm việc và yêu cầu báo cáo riêng của doanh nghiệp.",
    idealFor: "Công ty cần khám định kỳ, onboarding nhân sự hoặc tầm soát theo rủi ro nghề nghiệp.",
    duration: "Theo quy mô đoàn",
    priceRange: "Báo giá theo số lượng",
    items: [
      "Thiết kế danh mục khám theo ngân sách",
      "Điều phối khám tại viện hoặc tại doanh nghiệp",
      "Báo cáo tổng hợp sức khỏe nhân sự cho HR",
    ],
  },
];

export const specialties: Specialty[] = [
  {
    title: "Tim mạch",
    lead: "Theo dõi huyết áp, đau ngực, khó thở, rối loạn mỡ máu và nguy cơ mạch vành.",
    description: "Phù hợp khi cần khám chuyên sâu, siêu âm tim, điện tim, Holter huyết áp hoặc quản lý bệnh tim mạn tính.",
    symptoms: ["Đau tức ngực", "Hồi hộp trống ngực", "Huyết áp cao", "Phù chân hoặc khó thở"],
  },
  {
    title: "Tiêu hóa - gan mật",
    lead: "Khám đau dạ dày, rối loạn tiêu hóa, gan nhiễm mỡ, viêm gan và tầm soát tiêu hóa.",
    description: "Tập trung nội soi, xét nghiệm chức năng gan, tư vấn dinh dưỡng và theo dõi sau điều trị.",
    symptoms: ["Đau thượng vị", "Đầy hơi kéo dài", "Rối loạn đi tiêu", "Vàng da hoặc men gan cao"],
  },
  {
    title: "Sản phụ khoa",
    lead: "Khám phụ khoa định kỳ, theo dõi thai kỳ, sàng lọc trước sinh và tư vấn sức khỏe sinh sản.",
    description: "Quy trình riêng tư, dễ đặt lịch, phù hợp cả khám định kỳ và các vấn đề cần xử lý sớm.",
    symptoms: ["Khám thai", "Rối loạn kinh nguyệt", "Đau vùng chậu", "Tư vấn trước mang thai"],
  },
  {
    title: "Nhi khoa",
    lead: "Khám hô hấp, tiêu hóa, dinh dưỡng, tăng trưởng và theo dõi bệnh thường gặp ở trẻ.",
    description: "Không gian thân thiện, bác sĩ giải thích rõ cho phụ huynh và có kế hoạch tái khám cụ thể.",
    symptoms: ["Sốt, ho, sổ mũi", "Biếng ăn", "Chậm tăng cân", "Đau bụng hoặc tiêu chảy"],
  },
  {
    title: "Cơ xương khớp",
    lead: "Khám đau lưng, đau khớp, gout, thoái hóa, loãng xương và chấn thương vận động.",
    description: "Kết hợp khám chuyên khoa, chẩn đoán hình ảnh và hướng dẫn phục hồi vận động.",
    symptoms: ["Đau khớp", "Cứng khớp buổi sáng", "Đau cột sống", "Sưng nóng khớp"],
  },
  {
    title: "Thần kinh",
    lead: "Khám đau đầu, mất ngủ, chóng mặt, tê yếu tay chân, rối loạn trí nhớ và nguy cơ đột quỵ.",
    description: "Định hướng chẩn đoán sớm, chỉ định cận lâm sàng hợp lý và theo dõi phục hồi sau điều trị.",
    symptoms: ["Đau đầu kéo dài", "Chóng mặt", "Tê bì tay chân", "Mất ngủ hoặc suy giảm trí nhớ"],
  },
];

export const doctors: DoctorProfile[] = [
  {
    name: "PGS.TS.BS Nguyễn Quốc Minh",
    specialty: "Tim mạch",
    title: "Giám đốc Trung tâm Tim mạch",
    experience: "20 năm kinh nghiệm",
    focus: ["Tăng huyết áp", "Suy tim", "Nguy cơ mạch vành"],
  },
  {
    name: "TS.BS Lê Thu Hà",
    specialty: "Sản phụ khoa",
    title: "Trưởng đơn vị Sản phụ khoa",
    experience: "17 năm kinh nghiệm",
    focus: ["Thai kỳ nguy cơ", "Sàng lọc trước sinh", "Nội tiết phụ khoa"],
  },
  {
    name: "ThS.BS Trần Hữu Nam",
    specialty: "Tiêu hóa",
    title: "Chuyên gia Nội soi và Tiêu hóa lâm sàng",
    experience: "15 năm kinh nghiệm",
    focus: ["Nội soi", "Gan mật", "Tầm soát ung thư sớm"],
  },
  {
    name: "BSCKII Phạm Khánh Linh",
    specialty: "Nhi khoa",
    title: "Bác sĩ điều phối chăm sóc trẻ em",
    experience: "14 năm kinh nghiệm",
    focus: ["Hô hấp nhi", "Dinh dưỡng", "Theo dõi tăng trưởng"],
  },
];

export const patientJourney = [
  "Gửi yêu cầu đặt lịch trên website hoặc hotline, bộ phận điều phối xác nhận thông tin trong 15-30 phút.",
  "Đến quầy ưu tiên, hoàn tất tiếp nhận và được hướng dẫn đúng phòng khám hoặc khu cận lâm sàng.",
  "Bác sĩ khám, chỉ định xét nghiệm/chẩn đoán hình ảnh nếu cần và cập nhật hồ sơ điện tử trong cùng phiên.",
  "Nhận kết luận, đơn thuốc, lịch tái khám và hóa đơn trên cổng bệnh nhân để theo dõi sau khi rời viện.",
];

export const newsArticles: NewsArticle[] = [
  {
    category: "Tim mạch",
    title: "Khi nào tăng huyết áp cần đi khám chuyên khoa thay vì tự theo dõi tại nhà?",
    summary: "Các dấu hiệu cảnh báo, mốc đo huyết áp nên lưu lại và câu hỏi cần chuẩn bị trước khi gặp bác sĩ.",
    readTime: "6 phút đọc",
  },
  {
    category: "Tiêu hóa",
    title: "Nội soi tiêu hóa: ai nên tầm soát sớm và cần chuẩn bị những gì?",
    summary: "Gợi ý theo tuổi, tiền sử gia đình, triệu chứng kéo dài và cách chuẩn bị trước ngày nội soi.",
    readTime: "8 phút đọc",
  },
  {
    category: "Sản phụ khoa",
    title: "Ba mốc khám thai quan trọng không nên bỏ lỡ trong tam cá nguyệt đầu",
    summary: "Các mốc siêu âm, xét nghiệm và tư vấn giúp mẹ bầu theo dõi thai kỳ chủ động hơn.",
    readTime: "5 phút đọc",
  },
];

export const trustPoints = [
  "Bảng giá và chi phí dự kiến được tư vấn trước khi thực hiện dịch vụ.",
  "Hồ sơ khám, kết quả cận lâm sàng và đơn thuốc được lưu trong một tài khoản bệnh nhân.",
  "Có nhắc lịch tái khám, hỗ trợ thanh toán online và kênh liên hệ sau khám.",
];

export const footerLinks = {
  services: ["Khám tổng quát", "Tầm soát chuyên sâu", "Sản phụ khoa", "Khám doanh nghiệp"],
  support: ["Đặt lịch", "Cổng bệnh nhân", "Hướng dẫn đi khám", "Bảng giá dịch vụ"],
  company: ["Giới thiệu", "Đội ngũ bác sĩ", "Tin sức khỏe", "Liên hệ"],
};
