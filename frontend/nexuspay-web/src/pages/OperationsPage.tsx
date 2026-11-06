import { useCallback, useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { ErrorNotice, useAuth } from '../App'
import { api, type Customer, type Operation, type Page, money } from '../services/api'
import { Button, Empty, Field, inputClass, Loading, PageHeader, Pager, Panel, StatusPill } from '../components/ui'

const schema = z.object({ customerId: z.uuid('Selecciona un cliente válido.'), type: z.enum(['Payment', 'Payout']), amount: z.string().refine(value => Number(value) > 0 && Number(value) <= 1_000_000, 'El importe debe ser mayor a cero.'), currency: z.enum(['PEN', 'USD', 'EUR']), description: z.string().max(500) })
type FormData = z.infer<typeof schema>
type Details = { operation: Operation; statusHistory: { fromStatus: string | null; toStatus: string; changedAtUtc: string; reason: string | null }[]; paymentAttempts: { attemptNumber: number; providerCode: string; status: string; failureCode: string | null; providerReference: string | null }[] }

export function OperationsPage() {
  const { user } = useAuth(), canProcess = user?.roles.some(role => ['Administrator', 'Operator'].includes(role)) ?? false
  const [data, setData] = useState<Page<Operation>>({ items: [], totalCount: 0, page: 1, pageSize: 20 }), [page, setPage] = useState(1)
  const [status, setStatus] = useState(''), [loading, setLoading] = useState(true), [error, setError] = useState<unknown>(), [formOpen, setFormOpen] = useState(false), [saving, setSaving] = useState(false), [details, setDetails] = useState<Details | null>(null), [busyId, setBusyId] = useState('')
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormData>({ resolver: zodResolver(schema), defaultValues: { customerId: '', type: 'Payment', amount: '1', currency: 'PEN', description: '' } })
  const refresh = useCallback(() => {
    const query = new URLSearchParams({ page: String(page), pageSize: '20', sortBy: 'createdAt', sortDirection: 'desc' }); if (status) query.set('status', status)
    void api<Page<Operation>>(`/api/operations?${query}`).then(setData).catch(setError).finally(() => setLoading(false))
  }, [page, status])
  useEffect(refresh, [refresh])
  const create = handleSubmit(async values => {
    setSaving(true); setError(undefined)
    try { await api('/api/operations', { method: 'POST', headers: { 'Idempotency-Key': crypto.randomUUID() }, body: JSON.stringify({ ...values, amount: Number(values.amount), description: values.description || null }) }); setFormOpen(false); reset(); refresh() }
    catch (reason) { setError(reason) } finally { setSaving(false) }
  })
  const process = async (operation: Operation) => {
    setBusyId(operation.id); setError(undefined)
    try { await api(`/api/operations/${operation.id}/process`, { method: 'POST', headers: { 'Idempotency-Key': crypto.randomUUID() } }); refresh() }
    catch (reason) { setError(reason) } finally { setBusyId('') }
  }
  const cancel = async (operation: Operation) => {
    setBusyId(operation.id); setError(undefined)
    try { await api(`/api/operations/${operation.id}/cancel`, { method: 'POST' }); refresh() }
    catch (reason) { setError(reason) } finally { setBusyId('') }
  }
  const showDetails = async (id: string) => {
    setError(undefined)
    try { setDetails(await api<Details>(`/api/operations/${id}`)) } catch (reason) { setError(reason) }
  }
  return <>
    <PageHeader eyebrow="Procesamiento" title="Operaciones" description="Crea, revisa y procesa transacciones ficticias." action={canProcess && <Button onClick={() => setFormOpen(value => !value)}>＋ Nueva operación</Button>} />
    {error && <div className="mb-4"><ErrorNotice error={error}/></div>}
    {formOpen && <OperationForm onSubmit={create} onClose={() => setFormOpen(false)} register={register} errors={errors} saving={saving}/>}
    {details && <Panel className="mb-5"><div className="flex items-center justify-between"><div><p className="text-xs font-semibold uppercase tracking-wide text-cyan-700">Detalle de operación</p><h2 className="mt-1 font-semibold">{details.operation.operationCode}</h2></div><button aria-label="Cerrar detalle" className="text-slate-400" onClick={() => setDetails(null)}>✕</button></div><div className="mt-4 grid gap-5 md:grid-cols-2"><div><h3 className="mb-2 text-xs font-bold uppercase text-slate-400">Historial de estados</h3>{details.statusHistory.map((item, index) => <div key={`${item.toStatus}-${index}`} className="border-l-2 border-cyan-200 py-1 pl-3 text-sm"><b>{item.toStatus}</b><span className="ml-2 text-xs text-slate-400">{new Date(item.changedAtUtc).toLocaleString('es-PE')}</span><p className="text-xs text-slate-500">{item.reason}</p></div>)}</div><div><h3 className="mb-2 text-xs font-bold uppercase text-slate-400">Intentos de pago</h3>{details.paymentAttempts.length === 0 ? <p className="text-sm text-slate-400">Aún no se ha procesado.</p> : details.paymentAttempts.map(attempt => <div key={attempt.attemptNumber} className="flex items-center justify-between border-b border-slate-100 py-2 text-sm"><span>Intento {attempt.attemptNumber} · {attempt.providerCode}</span><StatusPill value={attempt.status}/></div>)}</div></div></Panel>}
    <Panel className="!p-0"><div className="flex flex-wrap items-center justify-between gap-3 p-4 sm:px-5"><div><h2 className="font-semibold">Registro de operaciones</h2><p className="mt-1 text-xs text-slate-400">La clave de idempotencia evita duplicados accidentales.</p></div><select aria-label="Filtrar estado" className={inputClass + ' max-w-44'} value={status} onChange={event => { setPage(1); setStatus(event.target.value) }}><option value="">Todos los estados</option>{['Pending', 'Processing', 'Completed', 'Failed', 'Cancelled'].map(item => <option key={item}>{item}</option>)}</select></div>
      {loading ? <Loading/> : data.items.length === 0 ? <Empty>No hay operaciones que coincidan con los filtros.</Empty> : <div className="overflow-x-auto"><table className="w-full min-w-[900px] text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-400"><tr><th className="px-5 py-3">Operación</th><th className="px-5 py-3">Cliente</th><th className="px-5 py-3">Tipo</th><th className="px-5 py-3">Importe</th><th className="px-5 py-3">Proveedor</th><th className="px-5 py-3">Estado</th><th className="px-5 py-3 text-right">Acciones</th></tr></thead><tbody className="divide-y divide-slate-100">{data.items.map(operation => <tr key={operation.id}><td className="px-5 py-3"><button className="font-semibold text-cyan-900 hover:underline" onClick={() => void showDetails(operation.id)}>{operation.operationCode}</button><p className="mt-0.5 text-xs text-slate-400">{new Date(operation.createdAtUtc).toLocaleString('es-PE')}</p></td><td className="px-5 py-3 font-mono text-xs text-slate-500">{operation.customerId.slice(0, 8)}…</td><td className="px-5 py-3 text-slate-500">{operation.type}</td><td className="px-5 py-3 font-medium">{money(operation.amount, operation.currency)}</td><td className="px-5 py-3 text-slate-500">{operation.providerCode ?? '—'}</td><td className="px-5 py-3"><StatusPill value={operation.status}/></td><td className="px-5 py-3 text-right">{canProcess && <>{['Pending', 'Failed'].includes(operation.status) && <button disabled={busyId === operation.id} className="mr-3 text-xs font-semibold text-cyan-800 disabled:opacity-50" onClick={() => void process(operation)}>{busyId === operation.id ? 'Procesando…' : 'Procesar'}</button>}{operation.status === 'Pending' && <button disabled={busyId === operation.id} className="text-xs font-semibold text-rose-700" onClick={() => void cancel(operation)}>Cancelar</button>}</>}</td></tr>)}</tbody></table></div>}
      <Pager page={page} pageSize={20} totalCount={data.totalCount} onChange={setPage}/>
    </Panel>
  </>
}

function OperationForm({ onSubmit, onClose, register, errors, saving }: { onSubmit: (event?: React.BaseSyntheticEvent) => Promise<void>; onClose: () => void; register: ReturnType<typeof useForm<FormData>>['register']; errors: ReturnType<typeof useForm<FormData>>['formState']['errors']; saving: boolean }) {
  const [customers, setCustomers] = useState<Customer[]>([])
  useEffect(() => { void api<Page<Customer>>('/api/customers?isActive=true&page=1&pageSize=100').then(result => setCustomers(result.items)).catch(() => setCustomers([])) }, [])
  return <Panel className="mb-5"><div className="mb-4 flex justify-between"><div><h2 className="font-semibold">Crear operación</h2><p className="mt-1 text-xs text-slate-400">Solo se procesan valores ficticios.</p></div><button aria-label="Cerrar formulario" onClick={onClose} className="text-slate-400">✕</button></div><form onSubmit={onSubmit} className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
    <Field label="Cliente" error={errors.customerId?.message}><select className={inputClass} {...register('customerId')}><option value="">Selecciona un cliente</option>{customers.map(customer => <option key={customer.id} value={customer.id}>{customer.customerCode} · {customer.firstName} {customer.lastName}</option>)}</select></Field>
    <Field label="Tipo"><select className={inputClass} {...register('type')}><option value="Payment">Pago</option><option value="Payout">Desembolso</option></select></Field>
    <Field label="Importe" error={errors.amount?.message}><input className={inputClass} type="number" min="0.01" step="0.01" {...register('amount')}/></Field>
    <Field label="Moneda"><select className={inputClass} {...register('currency')}><option>PEN</option><option>USD</option><option>EUR</option></select></Field>
    <Field label="Descripción" error={errors.description?.message}><input className={inputClass} {...register('description')} placeholder="Descripción ficticia"/></Field>
    <div className="flex items-end gap-2"><Button type="submit" disabled={saving || customers.length === 0}>{saving ? 'Guardando…' : 'Crear pendiente'}</Button><Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button></div>
  </form>{customers.length === 0 && <p className="mt-3 text-xs text-amber-700">Registra primero un cliente activo para crear operaciones.</p>}</Panel>
}
