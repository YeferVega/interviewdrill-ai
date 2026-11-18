import { useEffect } from 'react'
import { HistoryPage } from './components/HistoryPage'
import { InterviewPage } from './components/InterviewPage'
import { JdUploadPage } from './components/JdUploadPage'
import { ProfileReviewPage } from './components/ProfileReviewPage'
import { ThemeToggle } from './components/ThemeToggle'
import { UiLanguageToggle } from './components/UiLanguageToggle'
import { useInterviewStore } from './store/interviewStore'
import { ui } from './i18n/strings'

function InterviewView() {
  const status = useInterviewStore((state) => state.status)
  const sessionId = useInterviewStore((state) => state.sessionId)

  if (sessionId) {
    return <InterviewPage />
  }

  if (status === 'review') {
    return <ProfileReviewPage />
  }

  return <JdUploadPage />
}

function App() {
  const activeView = useInterviewStore((state) => state.activeView)
  const openView = useInterviewStore((state) => state.openView)
  const uiLanguage = useInterviewStore((state) => state.uiLanguage)
  const strings = ui(uiLanguage)

  useEffect(() => {
    document.documentElement.lang = uiLanguage
  }, [uiLanguage])

  return (
    <div className="app-shell">
      <header className="app-toolbar">
        <span className="brand">InterviewDrill AI</span>
        <nav className="app-nav" aria-label={strings.mainNavAria}>
          <button
            type="button"
            className={activeView === 'interview' ? 'nav-link active' : 'nav-link'}
            aria-current={activeView === 'interview' ? 'page' : undefined}
            onClick={() => openView('interview')}
          >
            {strings.navInterview}
          </button>
          <button
            type="button"
            className={activeView === 'history' ? 'nav-link active' : 'nav-link'}
            aria-current={activeView === 'history' ? 'page' : undefined}
            onClick={() => openView('history')}
          >
            {strings.navHistory}
          </button>
          <UiLanguageToggle />
          <ThemeToggle />
        </nav>
      </header>

      {activeView === 'history' ? <HistoryPage /> : <InterviewView />}
    </div>
  )
}

export default App
