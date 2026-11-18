import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { StudyGuidePanel } from './StudyGuidePanel'
import { parseMarkdownBlocks } from '../utils/studyGuide'

const markdown = `# Guía de estudio para AI Engineer

## Temas
### Resiliencia
- Practica retry con backoff.
`

describe('StudyGuidePanel', () => {
  it('renders parsed markdown blocks', () => {
    render(<StudyGuidePanel markdown={markdown} />)

    expect(screen.getByRole('heading', { name: 'Guía de estudio para AI Engineer' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Temas' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Resiliencia' })).toBeInTheDocument()
    expect(screen.getByRole('listitem')).toHaveTextContent('Practica retry con backoff.')
  })

  it('renders nothing when markdown is absent', () => {
    render(<StudyGuidePanel markdown={null} />)

    expect(screen.queryByRole('heading', { name: 'Guía de estudio' })).not.toBeInTheDocument()
  })

  it('parses headings, bullets and paragraphs', () => {
    expect(parseMarkdownBlocks('# Title\n## Section\n- one\n\nbody')).toEqual([
      { kind: 'h1', text: 'Title' },
      { kind: 'h2', text: 'Section' },
      { kind: 'li', text: 'one' },
      { kind: 'p', text: 'body' },
    ])
  })
})
