import { useCallback, useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { ErrorNotice, useAuth } from '../App'
import { api, type Page, type Provider } from '../services/api'
import { Button, Empty, Field, inputClass, Loading, PageHeader, Panel, StatusPill } from '../components/ui'

const schema = z.object({ providerCode: z.string().min(2).max(64), name: z.string().min(1).max(120) })
type FormData = z.infer<typeof schema>
export function ProvidersPage() {
  const { user } = useAuth(), isAdmin = user?.roles.includes('Administrator') ?? false
  const [providers, setProviders] = useState<Page<Provider>>({ items: [], totalCount: 0, page: 1, pageSize: 20 }), [loading, setLoading] = useState(true), [error, setError] = useState<unknown>(), [open, setOpen] = useState(false), [saving, setSaving] = useState(false), [busy, setBusy] = useState('')
  const { register, handleSubmit, reset, formState: { errors } } = useForm<FormData>({ resolver: zodResolver(schema) })
  const refresh = useCallback(() => { void api<Page<Provider>>('/api/providers?page=1&pageSize=100').then(setProviders).catch(setError).finally(() => setLoading(false)) }, [])
  useEffect(refresh, [refresh])
  const create = handleSubmit(async values => { setSaving(true); setError(undefined); try { await api('/api/providers', { method: 'POST', body: JSON.stringify(values) }); reset(); setOpen(false); refresh() } catch (reason) { setError(reason) } finally { setSaving(false) } })
  const toggle = async (provider: Provider) => { setBusy(provider.id); setError(undefined); try { await api(`/api/providers/${provider.id}`, { method: 'PUT', body: JSON.stringify({ name: provider.name, isEnabled: !provider.isEnabled, isAvailable: provider.isEnabled }) }); refresh() } catch (reason) { setError(reason) } finally { setBusy('') } }
  return <><PageHeader eyebrow="Integraciones" title="Proveedores" description="Simuladores locales disponibles para procesar operaciones." action={isAdmin && <Button onClick={() => setOpen(value => !value)}>＋ Registrar proveedor</Button>}/>
    {error && <div className="mb-4"><ErrorNotice error={error}/></div>}
    {open && <Panel className="mb-5"><h2 className="mb-4 font-semibold">Nuevo proveedor simulado</h2><form onSubmit={create} className="grid gap-4 sm:grid-cols-[1fr_1.5fr_auto]"><Field label="Código" error={errors.providerCode?.message}><input className={inputClass} {...register('providerCode')}/></Field><Field label="Nombre" error={errors.name?.message}><input className={inputClass} {...register('name')}/></Field><div className="flex items-end gap-2"><Button disabled={saving}>{saving ? 'Guardando…' : 'Crear'}</Button><Button type="button" variant="secondary" onClick={() => setOpen(false)}>Cancelar</Button></div></form><p className="mt-3 text-xs text-slate-400">El registro queda auditado. La ejecución actual usa el simulador configurado en el entorno.</p></Panel>}
    <div className="mb-5 grid gap-4 sm:grid-cols-3">{[{ value: providers.totalCount, label: 'Registrados' }, { value: providers.items.filter(provider => provider.isEnabled).length, label: 'Habilitados' }, { value: providers.items.filter(provider => provider.isEnabled && provider.isAvailable).length, label: 'Disponibles' }].map(item => <Panel key={item.label}><p className="text-sm text-slate-500">{item.label}</p><p className="mt-2 text-2xl font-bold">{item.value}</p></Panel>)}</div>
    <Panel className="!p-0">{loading ? <Loading/> : providers.items.length === 0 ? <Empty/> : <div className="overflow-x-auto"><table className="w-full min-w-[650px] text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-400"><tr><th className="px-5 py-3">Proveedor</th><th className="px-5 py-3">Tipo</th><th className="px-5 py-3">Estado</th><th className="px-5 py-3 text-right">Gestión</th></tr></thead><tbody className="divide-y divide-slate-100">{providers.items.map(provider => <tr key={provider.id}><td className="px-5 py-4"><p className="font-semibold">{provider.name}</p><p className="mt-1 font-mono text-xs text-slate-400">{provider.providerCode}</p></td><td className="px-5 py-4"><StatusPill value={provider.type}/></td><td className="px-5 py-4"><div className="flex gap-2"><StatusPill value={provider.isEnabled ? 'Habilitado' : 'Deshabilitado'}/><StatusPill value={provider.isAvailable ? 'Disponible' : 'No disponible'}/></div></td><td className="px-5 py-4 text-right">{isAdmin && <Button variant="secondary" disabled={busy === provider.id} onClick={() => void toggle(provider)}>{provider.isEnabled ? 'Deshabilitar' : 'Habilitar'}</Button>}</td></tr>)}</tbody></table></div>}</Panel>
  </>
}
