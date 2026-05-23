import { PublicPageShell } from "@/components/public/PublicPageShell";
import { SectionHeading } from "@/components/public/SectionHeading";
import { newsArticles } from "@/content/hospitalContent";

const categories = ["Tin chuyên khoa", "Dinh dưỡng", "Tầm soát sớm", "Sản phụ khoa", "Nhi khoa", "Khuyến mãi"];

export default function NewsPage() {
  return (
    <PublicPageShell>
      <section className="mx-auto max-w-7xl px-4 py-16 md:px-6 md:py-22">
        <SectionHeading
          eyebrow="Kiến thức sức khỏe"
          title="Website bệnh viện tư cần vận hành như một phòng tin chuyên môn."
          description="Không chỉ có trang giới thiệu dịch vụ, lớp nội dung cần đủ mạnh để giữ nhịp tương tác với khách hàng trước và sau khi sử dụng dịch vụ y tế."
        />

        <div className="mt-8 flex flex-wrap gap-3">
          {categories.map((category) => (
            <span key={category} className="rounded-full border border-slate-200 bg-white px-4 py-2 text-sm text-slate-700">
              {category}
            </span>
          ))}
        </div>

        <div className="mt-12 grid gap-5 lg:grid-cols-3">
          {newsArticles.map((article) => (
            <article key={article.title} className="rounded-[2rem] border border-slate-200 bg-white p-6 shadow-[0_20px_55px_rgba(15,23,42,0.05)]">
              <p className="text-sm uppercase tracking-[0.22em] text-cyan-700">{article.category}</p>
              <h2 className="mt-4 text-2xl font-semibold tracking-tight text-slate-950">{article.title}</h2>
              <p className="mt-4 text-sm leading-7 text-slate-600">{article.summary}</p>
              <p className="mt-6 text-sm font-medium text-slate-500">{article.readTime}</p>
            </article>
          ))}
        </div>
      </section>
    </PublicPageShell>
  );
}
