import { useCallback, useEffect, useMemo, useState } from 'react'
import './App.css'
import { listPatientsForSession } from './api/patientsClient'
import { createPatientVisit, listPatientVisits } from './api/visitsClient'
import { useSpeechCapture } from './hooks/useSpeechCapture'
import { downloadSoapPdf } from './lib/exportSoapPdf'
import {
  dateAndTimeToIso,
  formatVisitWhen,
  suggestTitleFromConversation,
  toDateInputValue,
  toTimeInputValue,
} from './lib/visitMeta'
import { patientDisplayName, type PatientSessionOption } from './types/patients'
import type { PipelineResult } from './types/pipeline'
import type { VisitHistoryItem } from './types/visits'

type InputMode = 'voice' | 'text'

const SOAP_SECTIONS = [
  { key: 'subjective', letter: 'S', title: 'Subjective' },
  { key: 'objective', letter: 'O', title: 'Objective' },
  { key: 'assessment', letter: 'A', title: 'Assessment' },
  { key: 'plan', letter: 'P', title: 'Plan' },
] as const

function formatDuration(totalSeconds: number) {
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}

function countWords(text: string) {
  return text ? text.trim().split(/\s+/).filter(Boolean).length : 0
}

function StethoscopeIcon() {
  return (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M4.5 3v5a4.5 4.5 0 0 0 9 0V3" />
      <path d="M9 12.5v2a5.5 5.5 0 0 0 11 0v-2" />
      <circle cx="19.5" cy="10" r="2" />
    </svg>
  )
}

function MicIcon({ size = 34 }: { size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <rect x="9" y="2" width="6" height="12" rx="3" />
      <path d="M5 11a7 7 0 0 0 14 0" />
      <path d="M12 18v4" />
    </svg>
  )
}

function KeyboardIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <rect x="2" y="5" width="20" height="14" rx="2" />
      <path d="M6 9h.01M10 9h.01M14 9h.01M18 9h.01M6 13h.01M10 13h.01M14 13h.01M18 13h.01M8 17h8" />
    </svg>
  )
}

function ChevronIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="m6 9 6 6 6-6" />
    </svg>
  )
}

function DownloadIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M12 3v12" />
      <path d="m7 11 5 5 5-5" />
      <path d="M5 20h14" />
    </svg>
  )
}

export default function App() {
  const [patients, setPatients] = useState<PatientSessionOption[]>([])
  const [patientsLoading, setPatientsLoading] = useState(true)
  const [patientsError, setPatientsError] = useState<string | null>(null)
  const [selectedPatientId, setSelectedPatientId] = useState('')
  const [titleLocked, setTitleLocked] = useState(false)
  const [lockedTitle, setLockedTitle] = useState('Office visit')
  const [titleOverride, setTitleOverride] = useState<string | null>(null)
  const [serviceDate, setServiceDate] = useState(() => toDateInputValue(new Date()))
  const [serviceTime, setServiceTime] = useState(() => toTimeInputValue(new Date()))
  const [mode, setMode] = useState<InputMode>('text')
  const [textDraft, setTextDraft] = useState('')
  const [voiced, setVoiced] = useState({ committed: '', interim: '' })
  const speech = useSpeechCapture()
  const [elapsedSeconds, setElapsedSeconds] = useState(0)
  const [result, setResult] = useState<PipelineResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [history, setHistory] = useState<VisitHistoryItem[]>([])
  const [historyLoading, setHistoryLoading] = useState(false)
  const [historyError, setHistoryError] = useState<string | null>(null)

  const isListening = speech.state === 'listening'

  const selectedPatient = useMemo(
    () => patients.find((p) => p.patientId === selectedPatientId) ?? null,
    [patients, selectedPatientId]
  )

  const loadPatients = useCallback(async () => {
    setPatientsLoading(true)
    setPatientsError(null)
    try {
      const items = await listPatientsForSession()
      setPatients(items)
      setSelectedPatientId((prev) => {
        if (prev && items.some((p) => p.patientId === prev)) {
          return prev
        }
        return ''
      })
    } catch (e) {
      setPatients([])
      setSelectedPatientId('')
      setPatientsError(e instanceof Error ? e.message : 'Could not load patients')
    } finally {
      setPatientsLoading(false)
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      try {
        const items = await listPatientsForSession()
        if (cancelled) {
          return
        }
        setPatients(items)
        setPatientsError(null)
        setSelectedPatientId((prev) => {
          if (prev && items.some((p) => p.patientId === prev)) {
            return prev
          }
          return ''
        })
      } catch (e) {
        if (cancelled) {
          return
        }
        setPatients([])
        setSelectedPatientId('')
        setPatientsError(e instanceof Error ? e.message : 'Could not load patients')
      } finally {
        if (!cancelled) {
          setPatientsLoading(false)
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const loadHistory = useCallback(async (patient: PatientSessionOption | null) => {
    if (!patient) {
      setHistory([])
      setHistoryError(null)
      return
    }
    setHistoryLoading(true)
    setHistoryError(null)
    try {
      const items = await listPatientVisits(patient.tenantId, patient.patientId)
      setHistory(items)
    } catch (e) {
      setHistory([])
      setHistoryError(e instanceof Error ? e.message : 'Could not load visit history')
    } finally {
      setHistoryLoading(false)
    }
  }, [])

  const historyPatientId = selectedPatient?.patientId ?? ''
  const [historyForId, setHistoryForId] = useState('')
  if (historyPatientId !== historyForId) {
    setHistoryForId(historyPatientId)
    setHistory([])
    setHistoryError(null)
    setHistoryLoading(historyPatientId.length > 0)
  }

  useEffect(() => {
    if (!selectedPatient) {
      return
    }
    let cancelled = false
    const patient = selectedPatient
    ;(async () => {
      try {
        const items = await listPatientVisits(patient.tenantId, patient.patientId)
        if (cancelled) {
          return
        }
        setHistory(items)
        setHistoryError(null)
      } catch (e) {
        if (cancelled) {
          return
        }
        setHistory([])
        setHistoryError(e instanceof Error ? e.message : 'Could not load visit history')
      } finally {
        if (!cancelled) {
          setHistoryLoading(false)
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [selectedPatient])

  const effectiveTranscript = useMemo(
    () =>
      mode === 'voice'
        ? `${voiced.committed}${voiced.interim}`.trim()
        : textDraft.trim(),
    [mode, voiced, textDraft]
  )

  const title = titleLocked
    ? lockedTitle
    : (titleOverride ?? suggestTitleFromConversation(effectiveTranscript))

  useEffect(() => {
    if (!isListening) {
      return
    }
    const id = window.setInterval(() => setElapsedSeconds((s) => s + 1), 1000)
    return () => window.clearInterval(id)
  }, [isListening])

  const onSpeechResult = useCallback((final: string, interim: string) => {
    if (!titleLocked) {
      setTitleOverride(null)
    }
    if (final) {
      setVoiced((p) => ({
        committed: `${p.committed} ${final}`.trimStart(),
        interim: '',
      }))
    } else {
      setVoiced((p) => ({ ...p, interim }))
    }
  }, [titleLocked])

  const handleToggleListen = () => {
    if (isListening) {
      speech.stop()
      return
    }
    setFormError(null)
    setElapsedSeconds(0)
    speech.start(onSpeechResult)
  }

  const handleRun = async () => {
    setFormError(null)
    if (!selectedPatient) {
      setFormError('Choose a patient to continue.')
      return
    }
    if (!effectiveTranscript) {
      setFormError('Add the visit conversation first, by typing or using the microphone.')
      return
    }
    setLoading(true)
    setResult(null)
    try {
      const r = await createPatientVisit(selectedPatient.tenantId, selectedPatient.patientId, {
        title: title.trim() || suggestTitleFromConversation(effectiveTranscript),
        occurredAt: dateAndTimeToIso(serviceDate, serviceTime),
        rawConversation: effectiveTranscript,
      })
      setResult(r)
      if (!titleLocked && r.soap.chiefComplaint) {
        setTitleOverride(r.soap.chiefComplaint)
      }
      await loadHistory(selectedPatient)
    } catch (e) {
      setFormError(e instanceof Error ? e.message : 'Request failed')
    } finally {
      setLoading(false)
    }
  }

  const hasNote = (item: VisitHistoryItem) =>
    Boolean(item.subjective || item.objective || item.assessment || item.plan)

  const wordCount = countWords(effectiveTranscript)
  const micCaption = !speech.isSupported
    ? 'Speech recognition is not available in this browser. Use Chrome or Edge, or switch to typing.'
    : isListening
      ? 'Recording. Tap the microphone again to stop.'
      : voiced.committed
        ? 'Tap the microphone to keep recording, or edit the text below.'
        : 'Tap the microphone to start recording the visit conversation.'

  return (
    <div className="app">
      <header className="appbar">
        <span className="appbar__mark">
          <StethoscopeIcon />
        </span>
        <span className="appbar__titles">
          <h1>Clinical documentation</h1>
          <span className="appbar__subtitle">Capture the visit, generate a SOAP note and codes</span>
        </span>
        <span className="appbar__spacer" />
        {selectedPatient && <span className="badge badge--patient">{patientDisplayName(selectedPatient)}</span>}
        {isListening && <span className="badge badge--live">Recording {formatDuration(elapsedSeconds)}</span>}
      </header>

      <main className="page">
        <div className="layout">
          <div className="column">
            <section className="card">
              <div className="card__header">
                <span className="card__step">1</span>
                <span className="card__heading">
                  <h2>Visit details</h2>
                  <span className="card__hint">Who the visit is for and when it happened</span>
                </span>
              </div>
              <div className="card__body">
                <div className="field-grid">
                  <label className="field">
                    <span className="field__label">Patient</span>
                    <select
                      value={selectedPatientId}
                      onChange={(e) => setSelectedPatientId(e.target.value)}
                      disabled={patientsLoading || patients.length === 0}
                    >
                      <option value="">{patientsLoading ? 'Loading patients…' : 'Choose a patient'}</option>
                      {patients.map((p) => (
                        <option key={p.patientId} value={p.patientId}>
                          {patientDisplayName(p)}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="field">
                    <span className="field__label">Encounter title</span>
                    <input
                      value={title}
                      onChange={(e) => {
                        setTitleLocked(true)
                        setLockedTitle(e.target.value)
                      }}
                      placeholder="Office visit"
                    />
                  </label>
                  <label className="field">
                    <span className="field__label">Date</span>
                    <input type="date" value={serviceDate} onChange={(e) => setServiceDate(e.target.value)} />
                  </label>
                  <label className="field">
                    <span className="field__label">Time</span>
                    <input
                      type="time"
                      value={serviceTime}
                      onChange={(e) => setServiceTime(e.target.value)}
                      step={60}
                    />
                  </label>
                </div>
                {patientsError && (
                  <p className="notice notice--error" role="alert">
                    <span>
                      {patientsError}{' '}
                      <button type="button" className="btn--link" onClick={() => void loadPatients()}>
                        Retry
                      </button>
                    </span>
                  </p>
                )}
                {!patientsLoading && patients.length === 0 && !patientsError && (
                  <p className="notice notice--info" role="alert">
                    No patients are available yet. Ask an administrator to add patients, then retry.
                  </p>
                )}
              </div>
            </section>

            <section className="card">
              <div className="card__header">
                <span className="card__step">2</span>
                <span className="card__heading">
                  <h2>Conversation</h2>
                  <span className="card__hint">Dictate with the microphone or type the encounter</span>
                </span>
                <div className="card__actions">
                  <div className="segmented" role="tablist" aria-label="Input mode">
                    <button
                      type="button"
                      role="tab"
                      aria-selected={mode === 'text'}
                      className={`segmented__option${mode === 'text' ? ' is-active' : ''}`}
                      onClick={() => setMode('text')}
                    >
                      <KeyboardIcon />
                      Type
                    </button>
                    <button
                      type="button"
                      role="tab"
                      aria-selected={mode === 'voice'}
                      className={`segmented__option${mode === 'voice' ? ' is-active' : ''}`}
                      onClick={() => setMode('voice')}
                    >
                      <MicIcon size={16} />
                      Microphone
                    </button>
                  </div>
                </div>
              </div>

              <div className="card__body">
                {mode === 'text' ? (
                  <label className="field">
                    <span className="field__label">Visit conversation</span>
                    <textarea
                      value={textDraft}
                      onChange={(e) => {
                        setTextDraft(e.target.value)
                        if (!titleLocked) {
                          setTitleOverride(null)
                        }
                      }}
                      rows={12}
                      placeholder="Patient reports…"
                    />
                  </label>
                ) : (
                  <>
                    <div className={`mic-panel${isListening ? ' is-live' : ''}`}>
                      <button
                        type="button"
                        className={`mic${isListening ? ' is-live' : ''}`}
                        onClick={handleToggleListen}
                        disabled={!speech.isSupported}
                        aria-pressed={isListening}
                        aria-label={isListening ? 'Stop recording' : 'Start recording'}
                      >
                        <span className="mic__ring" aria-hidden="true" />
                        <span className="mic__ring mic__ring--delayed" aria-hidden="true" />
                        <span className="mic__icon">
                          <MicIcon />
                        </span>
                      </button>
                      {isListening ? (
                        <>
                          <span className="mic__timer">{formatDuration(elapsedSeconds)}</span>
                          <span className="waveform" aria-hidden="true">
                            <span />
                            <span />
                            <span />
                            <span />
                            <span />
                          </span>
                        </>
                      ) : (
                        <span className="mic__label">
                          {voiced.committed ? 'Recording stopped' : 'Ready to record'}
                        </span>
                      )}
                      <span className="mic__caption">{micCaption}</span>
                    </div>

                    {isListening ? (
                      <div className="transcript" aria-live="polite">
                        {voiced.committed || (
                          <span className="transcript__placeholder">Listening for the conversation…</span>
                        )}
                        <span className="transcript__interim">{voiced.interim}</span>
                      </div>
                    ) : (
                      <label className="field">
                        <span className="field__label">Captured conversation</span>
                        <textarea
                          value={voiced.committed + voiced.interim}
                          onChange={(e) => {
                            setVoiced({ committed: e.target.value, interim: '' })
                            if (!titleLocked) {
                              setTitleOverride(null)
                            }
                          }}
                          rows={8}
                          placeholder="Your dictation appears here, and you can edit it before generating."
                        />
                      </label>
                    )}

                    <div className="row">
                      <button
                        type="button"
                        className="btn btn--ghost"
                        onClick={() => {
                          speech.stop()
                          setVoiced({ committed: '', interim: '' })
                          if (!titleLocked) {
                            setTitleOverride(null)
                          }
                        }}
                        disabled={!isListening && !voiced.committed}
                      >
                        Clear recording
                      </button>
                    </div>

                    {speech.error && speech.state !== 'unsupported' && (
                      <p className="notice notice--error" role="alert">
                        {speech.error}
                      </p>
                    )}
                  </>
                )}

                {formError && (
                  <p className="notice notice--error" role="alert">
                    {formError}
                  </p>
                )}
              </div>

              <div className="card__footer">
                <span className="hint">
                  {wordCount === 0 ? 'No conversation captured yet' : `${wordCount} words captured`}
                </span>
                <button
                  type="button"
                  className="btn btn--primary"
                  onClick={handleRun}
                  disabled={loading || !selectedPatient}
                >
                  {loading && <span className="spinner" aria-hidden="true" />}
                  {loading ? 'Generating…' : 'Generate SOAP & codes'}
                </button>
              </div>
            </section>

            {result && (
              <section className="card">
                <div className="card__header">
                  <span className="card__step">3</span>
                  <span className="card__heading">
                    <h2>Generated note</h2>
                    <span className="card__hint">Review before it goes in the chart</span>
                  </span>
                  <div className="card__actions">
                    <button type="button" className="btn" onClick={() => downloadSoapPdf(result)}>
                      <DownloadIcon />
                      Download PDF
                    </button>
                  </div>
                </div>
                <div className="card__body">
                  <div className="soap-grid">
                    {SOAP_SECTIONS.map((section) => (
                      <article className="soap-card" key={section.letter}>
                        <div className="soap-card__header">
                          <span className="soap-card__letter">{section.letter}</span>
                          <span className="soap-card__title">{section.title}</span>
                        </div>
                        <p className="soap-card__body">{result.soap[section.key]}</p>
                      </article>
                    ))}
                    {result.soap.chiefComplaint && (
                      <article className="soap-card soap-card--wide">
                        <div className="soap-card__header">
                          <span className="soap-card__letter">CC</span>
                          <span className="soap-card__title">Chief complaint</span>
                        </div>
                        <p className="soap-card__body">{result.soap.chiefComplaint}</p>
                      </article>
                    )}
                  </div>

                  <div className="stack">
                    <span className="section-title">ICD-10-CM</span>
                    <div className="row">
                      <span className="code-chip">{result.coding.primaryIcd10}</span>
                      <span className="hint">Primary diagnosis</span>
                    </div>
                    {result.coding.additionalNotes && (
                      <pre className="pre-block">{result.coding.additionalNotes}</pre>
                    )}
                  </div>

                  {result.coding.lineItems.length > 0 && (
                    <div className="stack">
                      <span className="section-title">CPT / procedures</span>
                      <ul className="code-list">
                        {result.coding.lineItems.map((line, i) => (
                          <li key={i}>
                            <span className="code-list__head">
                              <span className="code-chip">{line.cptCode}</span>
                              {line.quantity > 1 && <span className="badge">×{line.quantity}</span>}
                              <span className="hint">{line.icd10Codes.join(', ')}</span>
                            </span>
                            <span className="code-list__reason">{line.reasoning}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}

                  <div className="stack">
                    <span className="section-title">Conversation (cleaned)</span>
                    <pre className="pre-block">{result.cleanedTranscript}</pre>
                  </div>
                </div>
              </section>
            )}
          </div>

          <div className="column column--aside">
            <section className="card">
              <div className="card__header">
                <span className="card__heading">
                  <h2>Visit history</h2>
                  <span className="card__hint">Previous notes for the selected patient</span>
                </span>
              </div>
              <div className="card__body">
                {!selectedPatient && <p className="empty">Choose a patient to see prior visits.</p>}
                {selectedPatient && historyLoading && (
                  <p className="empty">
                    <span className="spinner spinner--muted" aria-hidden="true" />
                    Loading visits…
                  </p>
                )}
                {selectedPatient && historyError && (
                  <p className="notice notice--error" role="alert">
                    <span>
                      {historyError}{' '}
                      <button
                        type="button"
                        className="btn--link"
                        onClick={() => void loadHistory(selectedPatient)}
                      >
                        Retry
                      </button>
                    </span>
                  </p>
                )}
                {selectedPatient && !historyLoading && !historyError && history.length === 0 && (
                  <p className="empty">No prior visits for this patient yet.</p>
                )}
                {selectedPatient && history.length > 0 && (
                  <ul className="history-list">
                    {history.map((item) => (
                      <li key={item.encounterId}>
                        <details className="history-item">
                          <summary>
                            <span className="stack">
                              <span className="history-item__title">{item.title}</span>
                              <span className="history-item__when">
                                {formatVisitWhen(item.serviceDate, item.scheduledTime)}
                              </span>
                            </span>
                            <span className="history-item__chevron">
                              <ChevronIcon />
                            </span>
                          </summary>
                          <div className="history-item__body">
                            {hasNote(item) ? (
                              <div className="soap-grid">
                                {SOAP_SECTIONS.map((section) => (
                                  <article className="soap-card" key={section.letter}>
                                    <div className="soap-card__header">
                                      <span className="soap-card__letter">{section.letter}</span>
                                      <span className="soap-card__title">{section.title}</span>
                                    </div>
                                    <p className="soap-card__body">{item[section.key] || '—'}</p>
                                  </article>
                                ))}
                              </div>
                            ) : (
                              <p className="hint">No note is stored for this visit.</p>
                            )}
                            <div className="stack">
                              <span className="section-title">Transcript</span>
                              {item.transcript ? (
                                <pre className="pre-block">{item.transcript}</pre>
                              ) : (
                                <p className="hint">No transcript is stored for this visit.</p>
                              )}
                            </div>
                          </div>
                        </details>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </section>
          </div>
        </div>
      </main>
    </div>
  )
}
