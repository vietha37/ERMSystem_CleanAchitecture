export function toDateInputValue(date: Date): string {
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}

export function getStatusClass(stage: string): string {
  switch (stage) {
    case "Cho tiep don":
      return "border border-slate-200 bg-slate-100 text-slate-700";
    case "Cho mo ho so":
      return "border border-amber-200 bg-amber-50 text-amber-700";
    case "Dang kham":
      return "border border-sky-200 bg-sky-50 text-sky-700";
    case "Cho ke don":
      return "border border-violet-200 bg-violet-50 text-violet-700";
    case "Da ke don":
      return "border border-emerald-200 bg-emerald-50 text-emerald-700";
    case "Da hoan thanh":
      return "border border-blue-200 bg-blue-50 text-blue-700";
    default:
      return "border border-slate-200 bg-slate-100 text-slate-700";
  }
}
