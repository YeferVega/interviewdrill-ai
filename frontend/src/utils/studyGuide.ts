import type { InterviewQuestion } from '../types/interview'

export interface MarkdownBlock {
  kind: 'h1' | 'h2' | 'h3' | 'li' | 'p' | 'hr'
  text: string
}

export function parseMarkdownBlocks(markdown: string): MarkdownBlock[] {
  return markdown
    .replace(/\r\n/g, '\n')
    .split('\n')
    .reduce<MarkdownBlock[]>((blocks, rawLine) => {
      const line = rawLine.trim()

      if (!line) {
        return blocks
      }

      if (line === '---') {
        blocks.push({ kind: 'hr', text: line })
      } else if (line.startsWith('### ')) {
        blocks.push({ kind: 'h3', text: line.slice(4).trim() })
      } else if (line.startsWith('## ')) {
        blocks.push({ kind: 'h2', text: line.slice(3).trim() })
      } else if (line.startsWith('# ')) {
        blocks.push({ kind: 'h1', text: line.slice(2).trim() })
      } else if (line.startsWith('- ') || line.startsWith('* ') || /^\d+\.\s/.test(line)) {
        blocks.push({ kind: 'li', text: line.replace(/^[-*]\s+|^\d+\.\s+/, '').trim() })
      } else {
        blocks.push({ kind: 'p', text: line })
      }

      return blocks
    }, [])
}

export function toPlainText(markdown: string): string {
  return parseMarkdownBlocks(markdown)
    .filter((block) => block.kind !== 'hr')
    .map((block) => (block.kind === 'li' ? `- ${block.text}` : block.text))
    .join('\n')
}

export function createDownloadFile(markdown: string): File {
  return new File([markdown], 'study-guide.md', { type: 'text/markdown;charset=utf-8' })
}

export function formatQuestion(question: InterviewQuestion | null | undefined): string {
  return question ? `${question.text} (${question.difficulty})` : 'Sin pregunta activa'
}
