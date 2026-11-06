import { useCallback, useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useAuth, ErrorNotice } from '../App'
import { api, type Customer, type Page } from '../services/api'
import { Button, Empty, Field, inputClass, Loading, PageHeader, Pager, Panel, StatusPill } from '../components/ui'

const schema = z.object({ customerCode: z.string().min(2).max(40), firstName: z.string().min(1).max(100), lastName: z.string().min(1).max(100), email: z.email(), phone: z.string().max(30), documentType: z.string().max(30), documentNumber: z.string().max(50) })
type FormData = z.infer<typeof schema>
const initial: FormData = { customerCode: '', firstName: '', lastName: '', email: '', phone: '', documentType: '', documentNumber: '' }
const documentTypes = ['DNI', 'Carné de extranjería', 'Pasaporte', 'RUC', 'Otro']

export function CustomersPage() {
  const { user } = useAuth()
  const canEdit = user?.roles.some(role => ['Administrator', 'Operator'].includes(role)) ?? false
  const isAdmin = user?.roles.includes('Administrator') ?? false
  const [data, setData] = useState<Page<Customer>>({ items: [], totalCount: 0, page: 1, pageSize: 20 })
  const [page, setPage] = useState(1), [search, setSearch] = useState(''), [query, setQuery] = useState(''), [activeFilter, setActiveFilter] = useState('')
  const [loading, setLoading] = useState(true), [error, setError] = useState<unknown>(), [formOpen, setFormOpen] = useState(false), [editing, setEditing] = useState<Customer | null>(null), [saving, setSaving] = useState(false)
  const [details, setDetails] = useState<Customer | null>(null), [loadingDetails, setLoadingDetails] = useState(false)
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormData>({ resolver: zodResolver(schema), defaultValues: initial })
  const refresh = useCallback(() => {
    const params = new URLSearchParams({ page: String(page), pageSize: '20', sortBy: 'createdAt', sortDirection: 'desc' })
    if (query) params.set('search', query)
    if (activeFilter) params.set('isActive', activeFilter)
    void api<Page<Customer>>(`/api/customers?${params}`).then(setData).catch(setError).finally(() => setLoading(false))
  }, [activeFilter, page, query])
  useEffect(refresh, [refresh])
  const openForm = (customer?: Customer) => {
    setEditing(customer ?? null)
    reset(customer ? { customerCode: customer.customerCode, firstName: customer.firstName, lastName: customer.lastName, email: customer.email, phone: customer.phone ?? '', documentType: customer.documentType ?? '', documentNumber: customer.documentNumber ?? '' } : initial)
    setFormOpen(true)
  }
  const save = handleSubmit(async values => {
    setSaving(true); setError(undefined)
    const body = editing ? { firstName: values.firstName, lastName: values.lastName, email: values.email, phone: values.phone || null, documentType: values.documentType || null, documentNumber: values.documentNumber || null }
      : { ...values, phone: values.phone || null, documentType: values.documentType || null, documentNumber: values.documentNumber || null }
    try {
      await api(editing ? `/api/customers/${editing.id}` : '/api/customers', { method: editing ? 'PUT' : 'POST', body: JSON.stringify(body) })
      setFormOpen(false); refresh()
    } catch (reason) { setError(reason) } finally { setSaving(false) }
  })
  const toggleStatus = async (customer: Customer) => {
    try { await api(`/api/customers/${customer.id}/status`, { method: 'PATCH', body: JSON.stringify({ isActive: !customer.isActive }) }); refresh() }
    catch (reason) { setError(reason) }
  }
  const showDetails = async (id: string) => {
    setLoadingDetails(true); setError(undefined)
    try { setDetails(await api<Customer>(`/api/customers/${id}`)) }
    catch (reason) { setError(reason) }
    finally { setLoadingDetails(false) }
  }
  return <>
    <PageHeader eyebrow="Relaciones" title="Clientes" description="Consulta y administra los clientes ficticios de NexusPay." action={canEdit && <Button onClick={() => openForm()}>＋ Nuevo cliente</Button>} />
    {error && <div className="mb-4"><ErrorNotice error={error}/></div>}
    {loadingDetails && <Panel className="mb-5"><Loading label="Cargando ficha del cliente…"/></Panel>}
    {details && <Panel className="mb-5"><div className="flex items-start justify-between gap-3"><div><p className="text-xs font-bold uppercase tracking-wide text-cyan-700">Ficha del cliente</p><h2 className="mt-1 text-lg font-semibold">{details.firstName} {details.lastName}</h2><p className="text-xs text-slate-500">{details.customerCode}</p></div><button aria-label="Cerrar ficha del cliente" onClick={() => setDetails(null)} className="rounded p-1 text-slate-500 hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-cyan-700">✕</button></div><dl className="mt-4 grid gap-4 text-sm sm:grid-cols-2 xl:grid-cols-4"><div><dt className="text-xs text-slate-500">Correo</dt><dd className="mt-1 break-all font-medium">{details.email}</dd></div><div><dt className="text-xs text-slate-500">Teléfono ficticio</dt><dd className="mt-1 font-medium">{details.phone || '—'}</dd></div><div><dt className="text-xs text-slate-500">Documento</dt><dd className="mt-1 font-medium">{details.documentType && details.documentNumber ? `${details.documentType} · ${details.documentNumber}` : '—'}</dd></div><div><dt className="text-xs text-slate-500">Estado y alta</dt><dd className="mt-1 font-medium">{details.isActive ? 'Activo' : 'Inactivo'} · {new Date(details.createdAtUtc).toLocaleDateString('es-PE')}</dd></div></dl></Panel>}
    {formOpen && <Panel className="mb-5"><div className="mb-5 flex justify-between"><div><h2 className="font-semibold">{editing ? 'Editar cliente' : 'Registrar cliente'}</h2><p className="mt-1 text-xs text-slate-400">Los datos deben ser ficticios.</p></div><button aria-label="Cerrar formulario" onClick={() => setFormOpen(false)} className="text-slate-400">✕</button></div>
      <form className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" onSubmit={save}>
        {!editing && <Field label="Código" error={errors.customerCode?.message}><input className={inputClass} {...register('customerCode')}/></Field>}
        <Field label="Nombre" error={errors.firstName?.message}><input className={inputClass} {...register('firstName')}/></Field><Field label="Apellido" error={errors.lastName?.message}><input className={inputClass} {...register('lastName')}/></Field>
        <Field label="Correo" error={errors.email?.message}><input className={inputClass} type="email" {...register('email')}/></Field><Field label="Teléfono ficticio"><input className={inputClass} {...register('phone')}/></Field>
        <Field label="Tipo de documento"><select className={inputClass} {...register('documentType')}>
          <option value="">Selecciona un tipo</option>
          {editing?.documentType && !documentTypes.includes(editing.documentType) && <option value={editing.documentType}>{editing.documentType}</option>}
          {documentTypes.map(type => <option key={type} value={type}>{type}</option>)}
        </select></Field><Field label="Número de documento"><input className={inputClass} {...register('documentNumber')}/></Field>
        <div className="flex items-end gap-2"><Button disabled={saving} type="submit">{saving ? 'Guardando…' : 'Guardar cliente'}</Button><Button type="button" variant="secondary" onClick={() => setFormOpen(false)}>Cancelar</Button></div>
      </form>
    </Panel>}
    <Panel className="!p-0"><div className="flex flex-wrap items-center justify-between gap-3 p-4 sm:px-5"><form className="flex min-w-[250px] flex-1 gap-2" onSubmit={event => { event.preventDefault(); setPage(1); setQuery(search.trim()) }}><input aria-label="Buscar clientes" className={`${inputClass} max-w-sm`} value={search} onChange={event => setSearch(event.target.value)} placeholder="Código, nombre o correo"/><Button variant="secondary" type="submit">Buscar</Button></form><select aria-label="Filtrar por estado" className={inputClass + ' max-w-40'} value={activeFilter} onChange={event => { setPage(1); setActiveFilter(event.target.value) }}><option value="">Todos</option><option value="true">Activos</option><option value="false">Inactivos</option></select></div>
      {loading ? <Loading/> : data.items.length === 0 ? <Empty>No hay clientes que coincidan con la búsqueda.</Empty> : <div className="overflow-x-auto"><table className="w-full min-w-[760px] text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-400"><tr><th className="px-5 py-3">Cliente</th><th className="px-5 py-3">Correo</th><th className="px-5 py-3">Documento</th><th className="px-5 py-3">Estado</th><th className="px-5 py-3 text-right">Acciones</th></tr></thead><tbody className="divide-y divide-slate-100">{data.items.map(customer => <tr key={customer.id}><td className="px-5 py-3"><p className="font-semibold">{customer.firstName} {customer.lastName}</p><p className="mt-0.5 text-xs text-slate-400">{customer.customerCode}</p></td><td className="px-5 py-3 text-slate-600">{customer.email}</td><td className="px-5 py-3 text-slate-500">{customer.documentType && customer.documentNumber ? `${customer.documentType} · ${customer.documentNumber}` : '—'}</td><td className="px-5 py-3"><StatusPill value={customer.isActive ? 'Activo' : 'Inactivo'}/></td><td className="px-5 py-3 text-right"><button className="mr-3 text-xs font-semibold text-cyan-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-cyan-700" onClick={() => void showDetails(customer.id)}>Ver ficha</button>{canEdit && <><button className="mr-3 text-xs font-semibold text-cyan-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-cyan-700" onClick={() => openForm(customer)}>Editar</button>{isAdmin && <button className="text-xs font-semibold text-rose-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-rose-700" onClick={() => void toggleStatus(customer)}>{customer.isActive ? 'Desactivar' : 'Activar'}</button>}</>}</td></tr>)}</tbody></table></div>}
      <Pager page={page} pageSize={20} totalCount={data.totalCount} onChange={setPage}/>
    </Panel>
  </>
}
