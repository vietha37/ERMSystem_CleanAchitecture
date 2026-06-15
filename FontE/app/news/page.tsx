import Link from "next/link";
import { PublicPageShell } from "@/components/public/PublicPageShell";

type NewsArticle = {
  title: string;
  category: string;
  summary: string;
  readTime: string;
  date: string;
  imageUrl: string;
  accent: string;
};

const featuredArticle: NewsArticle = {
  category: "Tầm soát sớm",
  title: "Lập kế hoạch khám sức khỏe định kỳ: nên bắt đầu từ những chỉ số nào?",
  summary:
    "Một lộ trình tầm soát hợp lý giúp người bệnh hiểu rõ nguy cơ tim mạch, chuyển hóa, tiêu hóa và các dấu hiệu cần theo dõi sau mỗi lần khám.",
  readTime: "9 phút đọc",
  date: "Cập nhật hôm nay",
  imageUrl:
    "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?auto=format&fit=crop&w=1200&q=80",
  accent: "bg-cyan-600",
};

const articles: NewsArticle[] = [
  {
    category: "Tim mạch",
    title: "Khi nào tăng huyết áp cần đi khám chuyên khoa thay vì tự theo dõi tại nhà?",
    summary:
      "Các dấu hiệu cảnh báo, mốc đo huyết áp nên lưu lại và câu hỏi cần chuẩn bị trước khi gặp bác sĩ.",
    readTime: "6 phút đọc",
    date: "08/06/2026",
    imageUrl:
      "https://images.unsplash.com/photo-1588776814546-1ffcf47267a5?auto=format&fit=crop&w=900&q=80",
    accent: "bg-rose-600",
  },
  {
    category: "Tiêu hóa",
    title: "Nội soi tiêu hóa: ai nên tầm soát sớm và cần chuẩn bị những gì?",
    summary:
      "Gợi ý theo tuổi, tiền sử gia đình, triệu chứng kéo dài và cách chuẩn bị trước ngày nội soi.",
    readTime: "8 phút đọc",
    date: "07/06/2026",
    imageUrl:
      "https://images.unsplash.com/photo-1559757148-5c350d0d3c56?auto=format&fit=crop&w=900&q=80",
    accent: "bg-emerald-600",
  },
  {
    category: "Sản phụ khoa",
    title: "Ba mốc khám thai quan trọng không nên bỏ lỡ trong tam cá nguyệt đầu",
    summary:
      "Các mốc siêu âm, xét nghiệm và tư vấn giúp mẹ bầu theo dõi thai kỳ chủ động hơn.",
    readTime: "5 phút đọc",
    date: "06/06/2026",
    imageUrl:
      "https://images.unsplash.com/photo-1579684385127-1ef15d508118?auto=format&fit=crop&w=900&q=80",
    accent: "bg-pink-600",
  },
  {
    category: "Nhi khoa",
    title: "Trẻ ho kéo dài sau sốt: dấu hiệu nào cần đưa trẻ đi khám?",
    summary:
      "Cách phân biệt ho sau nhiễm siêu vi, ho do dị ứng và các biểu hiện cần bác sĩ đánh giá sớm.",
    readTime: "7 phút đọc",
    date: "05/06/2026",
    imageUrl:
      "https://images.unsplash.com/photo-1581254343469-d0c9b6c0b82b?auto=format&fit=crop&w=900&q=80",
    accent: "bg-amber-600",
  },
  {
    category: "Cơ xương khớp",
    title: "Đau lưng khi làm việc văn phòng: khi nào là dấu hiệu cần kiểm tra chuyên khoa?",
    summary:
      "Những tín hiệu từ cột sống, khớp và thói quen vận động cần điều chỉnh trước khi đau trở thành mạn tính.",
    readTime: "6 phút đọc",
    date: "04/06/2026",
    imageUrl:
      "https://images.unsplash.com/photo-1599045118108-bf9954418b76?auto=format&fit=crop&w=900&q=80",
    accent: "bg-violet-600",
  },
  {
    category: "Dinh dưỡng",
    title: "Ăn uống sau khi có kết quả xét nghiệm mỡ máu cao nên bắt đầu từ đâu?",
    summary:
      "Các nhóm thực phẩm nên ưu tiên, thói quen cần giảm và cách theo dõi lại chỉ số sau tư vấn.",
    readTime: "5 phút đọc",
    date: "03/06/2026",
    imageUrl:
      "https://images.unsplash.com/photo-1490645935967-10de6ba17061?auto=format&fit=crop&w=900&q=80",
    accent: "bg-lime-600",
  },
];

const topicTracks = [
  { label: "Tin chuyên khoa", count: "24 bài" },
  { label: "Tầm soát sớm", count: "18 bài" },
  { label: "Dinh dưỡng", count: "12 bài" },
  { label: "Sản phụ khoa", count: "10 bài" },
  { label: "Nhi khoa", count: "9 bài" },
  { label: "Hướng dẫn đi khám", count: "15 bài" },
];

const healthBriefs = [
  "Đo huyết áp 2 lần/ngày trong 7 ngày nếu chỉ số thường xuyên cao hơn bình thường.",
  "Mang theo kết quả xét nghiệm cũ để bác sĩ so sánh diễn tiến, nhất là bệnh mạn tính.",
  "Không tự ngưng thuốc đang dùng trước ngày khám nếu chưa có hướng dẫn từ bác sĩ.",
];

const editorialStats = [
  { value: "36", label: "bài viết chuyên môn" },
  { value: "6", label: "nhóm chủ đề sức khỏe" },
  { value: "15-30'", label: "thời gian phản hồi đặt lịch" },
];

export default function NewsPage() {
  return (
    <PublicPageShell>
      <section className="bg-white">
        <div className="mx-auto grid max-w-7xl gap-10 px-4 py-12 md:px-6 md:py-16 lg:grid-cols-[0.92fr_1.08fr] lg:items-center">
          <div>
            <p className="text-sm font-semibold uppercase tracking-[0.28em] text-cyan-700">Kiến thức sức khỏe</p>
            <h1 className="mt-4 text-4xl font-semibold tracking-tight text-slate-950 md:text-6xl">
              Đọc đúng thông tin trước khi chọn đúng lịch khám.
            </h1>
            <p className="mt-5 max-w-2xl text-base leading-8 text-slate-600 md:text-lg">
              Tin tức được biên tập theo nhu cầu thực tế của người bệnh: hiểu triệu chứng, chuẩn bị trước khi khám,
              đọc kết quả và theo dõi chăm sóc sau điều trị.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <Link
                href="/booking"
                className="inline-flex h-11 items-center justify-center rounded-full bg-slate-950 px-5 text-sm font-semibold text-white transition hover:bg-cyan-700"
              >
                Đặt lịch tư vấn
              </Link>
              <Link
                href="/specialties"
                className="inline-flex h-11 items-center justify-center rounded-full border border-slate-300 bg-white px-5 text-sm font-semibold text-slate-800 transition hover:border-cyan-300 hover:text-cyan-700"
              >
                Xem chuyên khoa
              </Link>
            </div>
          </div>

          <article className="overflow-hidden rounded-lg border border-slate-200 bg-slate-950 text-white shadow-sm">
            <div
              className="min-h-72 bg-cover bg-center"
              style={{ backgroundImage: `linear-gradient(180deg, rgba(15, 23, 42, 0.12), rgba(15, 23, 42, 0.64)), url(${featuredArticle.imageUrl})` }}
            />
            <div className="p-6 md:p-8">
              <div className="flex flex-wrap items-center gap-3 text-sm text-cyan-100">
                <span className={`h-2.5 w-2.5 rounded-full ${featuredArticle.accent}`} />
                <span>{featuredArticle.category}</span>
                <span>{featuredArticle.date}</span>
                <span>{featuredArticle.readTime}</span>
              </div>
              <h2 className="mt-4 text-2xl font-semibold tracking-tight md:text-3xl">{featuredArticle.title}</h2>
              <p className="mt-4 text-sm leading-7 text-slate-200">{featuredArticle.summary}</p>
            </div>
          </article>
        </div>
      </section>

      <section className="border-y border-slate-200 bg-slate-50">
        <div className="mx-auto grid max-w-7xl gap-4 px-4 py-6 md:grid-cols-3 md:px-6">
          {editorialStats.map((item) => (
            <div key={item.label} className="flex items-baseline gap-3">
              <span className="text-3xl font-semibold tracking-tight text-slate-950">{item.value}</span>
              <span className="text-sm font-medium text-slate-600">{item.label}</span>
            </div>
          ))}
        </div>
      </section>

      <section className="mx-auto grid max-w-7xl gap-8 px-4 py-12 md:px-6 md:py-16 lg:grid-cols-[1fr_320px]">
        <div>
          <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
            <div>
              <p className="text-sm font-semibold uppercase tracking-[0.22em] text-cyan-700">Bài mới</p>
              <h2 className="mt-3 text-3xl font-semibold tracking-tight text-slate-950">Theo dõi sức khỏe theo từng chủ đề</h2>
            </div>
            <p className="max-w-xl text-sm leading-7 text-slate-600">
              Mỗi bài viết tập trung vào một tình huống thường gặp để người bệnh chuẩn bị câu hỏi tốt hơn trước khi
              gặp bác sĩ.
            </p>
          </div>

          <div className="mt-8 grid gap-5 md:grid-cols-2">
            {articles.map((article, index) => (
              <article
                key={article.title}
                className={index === 0 ? "overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm md:col-span-2" : "overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm"}
              >
                <div className={index === 0 ? "grid md:grid-cols-[0.92fr_1.08fr]" : ""}>
                  <div
                    className={index === 0 ? "min-h-72 bg-cover bg-center" : "h-48 bg-cover bg-center"}
                    style={{ backgroundImage: `url(${article.imageUrl})` }}
                  />
                  <div className="p-5 md:p-6">
                    <div className="flex flex-wrap items-center gap-3 text-xs font-semibold uppercase tracking-[0.16em] text-slate-500">
                      <span className={`h-2.5 w-2.5 rounded-full ${article.accent}`} />
                      <span>{article.category}</span>
                    </div>
                    <h3 className="mt-4 text-xl font-semibold tracking-tight text-slate-950 md:text-2xl">{article.title}</h3>
                    <p className="mt-3 text-sm leading-7 text-slate-600">{article.summary}</p>
                    <div className="mt-5 flex items-center justify-between text-sm text-slate-500">
                      <span>{article.date}</span>
                      <span>{article.readTime}</span>
                    </div>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </div>

        <aside className="space-y-5 lg:sticky lg:top-24 lg:self-start">
          <div className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
            <p className="text-sm font-semibold uppercase tracking-[0.2em] text-cyan-700">Chủ đề</p>
            <div className="mt-5 grid gap-2">
              {topicTracks.map((topic) => (
                <div key={topic.label} className="flex items-center justify-between rounded-lg bg-slate-50 px-4 py-3">
                  <span className="text-sm font-semibold text-slate-800">{topic.label}</span>
                  <span className="text-xs font-medium text-slate-500">{topic.count}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="rounded-lg border border-cyan-100 bg-cyan-50 p-5 shadow-sm">
            <p className="text-sm font-semibold uppercase tracking-[0.2em] text-cyan-800">Ghi nhớ nhanh</p>
            <ul className="mt-5 grid gap-4">
              {healthBriefs.map((brief) => (
                <li key={brief} className="flex gap-3 text-sm leading-6 text-slate-700">
                  <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-cyan-700" />
                  <span>{brief}</span>
                </li>
              ))}
            </ul>
          </div>

          <div className="rounded-lg border border-slate-200 bg-slate-950 p-5 text-white shadow-sm">
            <p className="text-sm font-semibold uppercase tracking-[0.2em] text-cyan-200">Cần bác sĩ đọc cùng?</p>
            <p className="mt-3 text-sm leading-7 text-slate-200">
              Đặt lịch để bác sĩ đối chiếu triệu chứng, kết quả cũ và tư vấn hướng theo dõi phù hợp.
            </p>
            <Link
              href="/booking"
              className="mt-5 inline-flex h-10 items-center justify-center rounded-full bg-white px-4 text-sm font-semibold text-slate-950 transition hover:bg-cyan-100"
            >
              Đặt lịch khám
            </Link>
          </div>
        </aside>
      </section>
    </PublicPageShell>
  );
}
