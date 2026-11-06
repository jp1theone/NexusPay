import { useCallback, useEffect, useState } from 'react'
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { api, money, type Operation } from '../services/api'
import { Button, Empty, Loading, PageHeader, Panel, StatusPill } from '../components/ui'
import { ErrorNotice } from '../App'

type Summary = { totalOperations: number; completedOperations: number; pendingOperations: number; failedOperations: number; successRatePercent: number; simulatedVolumeByCurrency: { currency: string; amount: number }[]; availableProviders: number; recentIntegrationErrors: number; recentOperations: Operation[] }
export function DashboardPage() {
  const [range, setRange] = useState(30)
  const [summary, setSummary] = useState<Summary | null>(null)
  const [error, setError] = useState<unknown>()
  const [loading, setLoading] = useState(true)
  const requestSummary = useCallback(async () => {
    const to = new Date(), from = new Date(to); from.setDate(from.getDate() - range)
    return api<Summary>(`/api/dashboard/summary?from=${encodeURIComponent(from.toISOString())}&to=${encodeURIComponent(to.toISOString())}`)
  }, [range])
  useEffect(() => {
    let active = true
    void requestSummary().then(result => { if (active) { setSummary(result); setError(undefined) } })
      .catch(reason => { if (active) setError(reason) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [requestSummary])
  const retry = () => {
    setLoading(true); setError(undefined)
    void requestSummary().then(setSummary).catch(setError).finally(() => setLoading(false))
  }
  const metrics = summary ? [
    { title: 'Operaciones', value: summary.totalOperations.toLocaleString(), note: 'En el periodo' },
    { title: 'Completadas', value: summary.completedOperations.toLocaleString(), note: `${summary.successRatePercent}% tasa de éxito` },
    { title: 'En curso', value: summary.pendingOperations.toLocaleString(), note: 'Pendientes o procesando' },
    { title: 'Volumen aprobado', value: summary.simulatedVolumeByCurrency.length ? summary.simulatedVolumeByCurrency.map(item => money(item.amount, item.currency)).join(' · ') : '—', note: 'Separado por moneda' },
  ] : []
  const chart = summary ? [
    { label: 'Completadas', value: summary.completedOperations }, { label: 'Pendientes', value: summary.pendingOperations }, { label: 'Fallidas', value: summary.failedOperations },
  ] : []
  return <>
    <PageHeader eyebrow="Centro de control" title="Resumen operativo" description="Indicadores calculados desde las operaciones persistidas." action={<select aria-label="Periodo del dashboard" className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm" value={range} onChange={event => { setLoading(true); setError(undefined); setRange(Number(event.target.value)) }}><option value={7}>Últimos 7 días</option><option value={30}>Últimos 30 días</option><option value={90}>Últimos 90 días</option></select>} />
    {error && <div className="mb-4"><ErrorNotice error={error}/><Button className="mt-2" variant="secondary" onClick={retry}>Reintentar</Button></div>}
    {loading ? <Loading /> : summary && <>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">{metrics.map((metric, index) => <Panel key={metric.title}><div className="flex items-start justify-between"><span className="text-sm text-slate-500">{metric.title}</span><span className={`grid size-9 place-items-center rounded-xl text-sm ${index === 0 ? 'bg-blue-50 text-blue-700' : index === 1 ? 'bg-emerald-50 text-emerald-700' : index === 2 ? 'bg-amber-50 text-amber-700' : 'bg-violet-50 text-violet-700'}`}>{['↗', '✓', '◷', '$'][index]}</span></div><p className={`mt-4 font-bold tracking-tight ${index === 3 ? 'text-lg' : 'text-2xl'}`}>{metric.value}</p><p className="mt-1 text-xs text-slate-400">{metric.note}</p></Panel>)}</div>
      <div className="mt-5 grid gap-5 xl:grid-cols-[1.5fr_1fr]">
        <Panel><div className="mb-5 flex items-center justify-between"><div><h2 className="font-semibold">Estado de operaciones</h2><p className="mt-1 text-xs text-slate-400">Distribución para el periodo seleccionado</p></div><span className="text-xs text-slate-400">{range} días</span></div>
          <div className="h-64">{summary.totalOperations === 0 ? <Empty>Aún no hay operaciones en este periodo.</Empty> : <ResponsiveContainer width="100%" height="100%"><AreaChart data={chart} margin={{ left: -18, right: 10, top: 8 }}><defs><linearGradient id="operationFill" x1="0" x2="0" y1="0" y2="1"><stop offset="0%" stopColor="#0e7490" stopOpacity={0.3}/><stop offset="95%" stopColor="#0e7490" stopOpacity={0.02}/></linearGradient></defs><CartesianGrid stroke="#e9eef5" vertical={false}/><XAxis dataKey="label" axisLine={false} tickLine={false} tick={{ fill: '#64748b', fontSize: 12 }}/><YAxis allowDecimals={false} axisLine={false} tickLine={false} tick={{ fill: '#94a3b8', fontSize: 11 }}/><Tooltip/><Area type="monotone" dataKey="value" name="Operaciones" stroke="#0e7490" strokeWidth={2.5} fill="url(#operationFill)"/></AreaChart></ResponsiveContainer>}</div>
        </Panel>
        <Panel><h2 className="font-semibold">Salud de integraciones</h2><p className="mt-1 text-xs text-slate-400">Disponibilidad y errores recientes</p><div className="mt-6 grid grid-cols-2 gap-3"><div className="rounded-xl bg-emerald-50 p-4"><span className="text-xs text-emerald-700">Proveedores activos</span><p className="mt-2 text-2xl font-bold text-emerald-800">{summary.availableProviders}</p></div><div className="rounded-xl bg-rose-50 p-4"><span className="text-xs text-rose-700">Errores recientes</span><p className="mt-2 text-2xl font-bold text-rose-800">{summary.recentIntegrationErrors}</p></div></div><div className="mt-5 rounded-xl bg-slate-50 p-4"><p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Operaciones fallidas</p><p className="mt-1 text-lg font-bold">{summary.failedOperations}</p></div></Panel>
      </div>
      <Panel className="mt-5 !p-0"><div className="flex items-center justify-between p-5 sm:px-6"><div><h2 className="font-semibold">Operaciones recientes</h2><p className="mt-1 text-xs text-slate-400">Últimos movimientos registrados</p></div></div><div className="overflow-x-auto"><table className="w-full min-w-[700px] text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-400"><tr><th className="px-5 py-3">Código</th><th className="px-5 py-3">Cliente</th><th className="px-5 py-3">Tipo</th><th className="px-5 py-3">Monto</th><th className="px-5 py-3">Estado</th></tr></thead><tbody className="divide-y divide-slate-100">{summary.recentOperations.map(operation => <tr key={operation.id}><td className="px-5 py-3 font-semibold">{operation.operationCode}</td><td className="px-5 py-3 text-slate-500">{operation.customerId.slice(0, 8)}…</td><td className="px-5 py-3 text-slate-500">{operation.type}</td><td className="px-5 py-3 font-medium">{money(operation.amount, operation.currency)}</td><td className="px-5 py-3"><StatusPill value={operation.status}/></td></tr>)}</tbody></table>{summary.recentOperations.length === 0 && <Empty>No hay operaciones recientes.</Empty>}</div></Panel>
    </>}
  </>
}
