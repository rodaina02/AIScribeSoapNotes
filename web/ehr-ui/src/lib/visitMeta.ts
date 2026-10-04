export function suggestTitleFromConversation(text: string, fallback = 'Office visit'): string {
  const trimmed = text.trim()
  if (!trimmed) {
    return fallback
  }
  const firstClause = trimmed.split(/[.!?\n\r]/, 1)[0]?.trim() ?? ''
  const words = firstClause.split(/\s+/).filter(Boolean)
  if (words.length === 0) {
    return fallback
  }
  const title = words.slice(0, 8).join(' ')
  return title.length > 80 ? `${title.slice(0, 77)}…` : title
}

function pad2(n: number): string {
  return String(n).padStart(2, '0')
}

export function toDateInputValue(d: Date): string {
  return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`
}

export function toTimeInputValue(d: Date): string {
  return `${pad2(d.getHours())}:${pad2(d.getMinutes())}`
}

export function dateAndTimeToIso(dateValue: string, timeValue: string): string {
  const time = timeValue.length === 5 ? `${timeValue}:00` : timeValue
  const d = new Date(`${dateValue}T${time}`)
  if (Number.isNaN(d.getTime())) {
    return new Date().toISOString()
  }
  return d.toISOString()
}

export function formatVisitWhen(serviceDate: string, scheduledTime: string | null): string {
  const datePart = serviceDate.includes('T') ? serviceDate.slice(0, 10) : serviceDate
  const timePart = scheduledTime ? scheduledTime.slice(0, 5) : ''
  const parsed = new Date(`${datePart}T${timePart || '00:00'}`)
  if (Number.isNaN(parsed.getTime())) {
    return datePart
  }
  return parsed.toLocaleString(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: timePart ? 'numeric' : undefined,
    minute: timePart ? '2-digit' : undefined,
  })
}
