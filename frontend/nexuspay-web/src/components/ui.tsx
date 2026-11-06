import type { ReactNode } from 'react'

export function PageHeader({ eyebrow, title, description, action }: { eyebrow?: string; title: string; description?: string; action?: ReactNode }) {
  return <div className="mb-7 flex flex-wrap items-end justify-between gap-4"><div>{eyebrow && <p className="mb-2 text-xs font-bold uppercase tracking-[.16em] text-cyan-700">{eyebrow}</p>}<h1 className="text-2xl font-bold tracking-tight text-slate-900 sm:text-3xl">{title}</h1>{description && <p className="mt-2 text-sm text-slate-500">{description}</p>}</div>{action}</div>
}
export function Panel({ children, className = '' }: { children: ReactNode; className?: string }) { return <section className={`rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-6 ${className}`}>{children}</section> }
export function Button({ children, variant = 'primary', ...props }: React.ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'secondary' | 'danger' }) {
  const palette = variant === 'primary' ? 'bg-[#132643] text-white hover:bg-[#1d385e]' : variant === 'danger' ? 'border border-rose-200 bg-rose-50 text-rose-700 hover:bg-rose-100' : 'border border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
  return <button {...props} className={`inline-flex min-h-10 items-center justify-center rounded-lg px-4 text-sm font-semibold transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-cyan-700 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 ${palette} ${props.className ?? ''}`}>{children}</button>
}
export function StatusPill({ value }: { value: string }) {
  const key = value.toLowerCase()
  const color = ['completed', 'succeeded', 'enabled', 'true'].includes(key) ? 'bg-emerald-50 text-emerald-700' : ['failed', 'rejected', 'timedout', 'cancelled', 'inactive'].includes(key) ? 'bg-rose-50 text-rose-700' : ['pending', 'processing'].includes(key) ? 'bg-amber-50 text-amber-700' : 'bg-slate-100 text-slate-600'
  return <span className={`inline-flex rounded-full px-2.5 py-1 text-xs font-semibold ${color}`}>{value}</span>
}
export function Loading({ label = 'Cargando información…' }: { label?: string }) { return <div className="py-12 text-center text-sm text-slate-400">{label}</div> }
export function Empty({ children = 'No hay datos para mostrar.' }: { children?: string }) { return <div className="py-12 text-center text-sm text-slate-400">{children}</div> }
export function Field({ label, error, children }: { label: string; error?: string; children: ReactNode }) {
  return <label className="block text-sm font-medium text-slate-700"><span>{label}</span><div className="mt-1.5">{children}</div>{error && <span className="mt-1 block text-xs text-rose-600">{error}</span>}</label>
}
export const inputClass = 'min-h-10 w-full rounded-lg border border-slate-200 bg-white px-3 text-sm outline-none transition placeholder:text-slate-400 focus-visible:border-cyan-700 focus-visible:ring-2 focus-visible:ring-cyan-200'
export function Pager({ page, pageSize, totalCount, onChange }: { page: number; pageSize: number; totalCount: number; onChange: (page: number) => void }) {
  const pages = Math.max(1, Math.ceil(totalCount / pageSize))
  return <div className="flex items-center justify-between border-t border-slate-100 px-4 py-3 text-xs text-slate-500"><span>{totalCount.toLocaleString()} registros · página {page} de {pages}</span><div className="flex gap-2"><Button variant="secondary" disabled={page <= 1} onClick={() => onChange(page - 1)}>Anterior</Button><Button variant="secondary" disabled={page >= pages} onClick={() => onChange(page + 1)}>Siguiente</Button></div></div>
}
