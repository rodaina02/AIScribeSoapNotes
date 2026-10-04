import { useCallback, useRef, useState } from 'react'

type SpeechState = 'idle' | 'listening' | 'unsupported'

export function useSpeechCapture() {
  const [state, setState] = useState<SpeechState>('idle')
  const [error, setError] = useState<string | null>(null)
  const recRef = useRef<SpeechRecognition | null>(null)

  const isSupported =
    typeof window !== 'undefined' &&
    (window.SpeechRecognition || window.webkitSpeechRecognition)

  const stop = useCallback(() => {
    recRef.current?.stop()
    recRef.current = null
    setState('idle')
  }, [])

  const start = useCallback(
    (onChunk: (final: string, interim: string) => void) => {
      setError(null)
      if (!isSupported) {
        setState('unsupported')
        setError('Speech recognition is not available in this browser. Use Chrome/Edge or text mode.')
        return
      }
      const Ctor = window.SpeechRecognition || window.webkitSpeechRecognition
      const rec = new Ctor()
      rec.lang = 'en-US'
      rec.continuous = true
      rec.interimResults = true
      rec.onresult = (ev: SpeechRecognitionEvent) => {
        let interim = ''
        let final = ''
        for (let i = ev.resultIndex; i < ev.results.length; i++) {
          const r = ev.results[i]
          if (r.isFinal) final += r[0].transcript
          else interim += r[0].transcript
        }
        onChunk(final, interim)
      }
      rec.onerror = (e) => {
        setError((e as { error?: string }).error || 'Speech error')
        setState('idle')
      }
      rec.onend = () => {
        if (recRef.current === rec) {
          setState('idle')
          recRef.current = null
        }
      }
      recRef.current = rec
      setState('listening')
      rec.start()
    },
    [isSupported]
  )

  return { state, error, isSupported, start, stop }
}
