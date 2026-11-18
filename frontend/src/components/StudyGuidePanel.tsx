import type { InterviewQuestion } from '../types/interview'
import { useInterviewStore } from '../store/interviewStore'
import { ui } from '../i18n/strings'
import { parseMarkdownBlocks } from '../utils/studyGuide'

interface StudyGuidePanelProps {
  markdown: string | null | undefined
  currentQuestion?: InterviewQuestion | null
}

export function StudyGuidePanel({ markdown }: StudyGuidePanelProps) {
  const strings = ui(useInterviewStore((state) => state.uiLanguage))

  if (!markdown) {
    return null
  }

  const blocks = parseMarkdownBlocks(markdown)

  const handleDownload = () => {
    const blob = new Blob([markdown], { type: 'text/markdown;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const anchor = document.createElement('a')

    anchor.href = url
    anchor.download = 'study-guide.md'
    anchor.click()
    URL.revokeObjectURL(url)
  }

  const handlePrint = () => {
    window.print()
  }

  return (
    <section className="study-guide" aria-labelledby="study-guide-title">
      <div className="study-guide-header">
        <h2 id="study-guide-title">{strings.guideTitle}</h2>
        <div className="study-guide-actions">
          <button type="button" onClick={handleDownload}>{strings.guideDownload}</button>
          <button type="button" onClick={handlePrint}>{strings.guidePrint}</button>
        </div>
      </div>

      {blocks.map((block, index) => {
        const key = `${block.kind}-${index}-${block.text.slice(0, 24)}`

        if (block.kind === 'h1') {
          return <h3 key={key}>{block.text}</h3>
        }

        if (block.kind === 'h2') {
          return <h4 key={key}>{block.text}</h4>
        }

        if (block.kind === 'h3') {
          return <h5 key={key}>{block.text}</h5>
        }

        if (block.kind === 'li') {
          return <li key={key}>{block.text}</li>
        }

        return <p key={key}>{block.text}</p>
      })}
    </section>
  )
}
