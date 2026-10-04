import type { PipelineResult } from '../types/pipeline'
import type { VisitHistoryItem } from '../types/visits'

const jsonHeaders = { 'Content-Type': 'application/json' }

function tryApiErrorMessage(body: string): string | null {
  const t = body.trim()
  if (!t.startsWith('{') || !t.endsWith('}')) {
    return null
  }
  try {
    const o = JSON.parse(t) as { error?: string; title?: string; detail?: string }
    if (typeof o.error === 'string' && o.error.length > 0) {
      return o.error
    }
    if (typeof o.title === 'string' && o.title.length > 0) {
      return o.title + (typeof o.detail === 'string' && o.detail ? ` — ${o.detail}` : '')
    }
  } catch {
    /* not JSON */
  }
  return null
}

function friendlyHttpError(res: Response, text: string): Error {
  const fromJson = tryApiErrorMessage(text)
  if (fromJson) {
    return new Error(fromJson)
  }
  if (res.status === 404) {
    return new Error('That visit could not be saved. Choose a patient and try again.')
  }
  return new Error('Something went wrong. Please try again.')
}

export async function listPatientVisits(
  tenantId: string,
  patientId: string
): Promise<VisitHistoryItem[]> {
  const res = await fetch(`/api/patients/${encodeURIComponent(patientId)}/visits`, {
    headers: { 'X-Tenant-Id': tenantId },
  })
  const text = await res.text()
  if (!res.ok) {
    throw friendlyHttpError(res, text)
  }
  return JSON.parse(text) as VisitHistoryItem[]
}

export async function createPatientVisit(
  tenantId: string,
  patientId: string,
  body: { title: string; occurredAt: string; rawConversation: string }
): Promise<PipelineResult> {
  const res = await fetch(`/api/patients/${encodeURIComponent(patientId)}/visits`, {
    method: 'POST',
    headers: {
      ...jsonHeaders,
      'X-Tenant-Id': tenantId,
    },
    body: JSON.stringify(body),
  })
  const text = await res.text()
  if (!res.ok) {
    throw friendlyHttpError(res, text)
  }
  return JSON.parse(text) as PipelineResult
}
