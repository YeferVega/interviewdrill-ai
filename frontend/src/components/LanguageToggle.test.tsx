import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { UiLanguageToggle } from './UiLanguageToggle'
import { InterviewLanguageField } from './InterviewLanguageField'
import { useInterviewStore } from '../store/interviewStore'

describe('UiLanguageToggle', () => {
  it('switches the store language when clicked', async () => {
    useInterviewStore.getState().setUiLanguage('es')

    render(<UiLanguageToggle />)

    await userEvent.click(screen.getByRole('button', { name: /idioma de la interfaz/i }))

    expect(useInterviewStore.getState().uiLanguage).toBe('en')

    await userEvent.click(screen.getByRole('button', { name: /interface language/i }))

    expect(useInterviewStore.getState().uiLanguage).toBe('es')
  })
})

describe('InterviewLanguageField', () => {
  it('selects English and updates the store', async () => {
    useInterviewStore.getState().setUiLanguage('en')
    useInterviewStore.getState().setInterviewLanguage('es')

    render(<InterviewLanguageField />)

    await userEvent.click(screen.getByRole('radio', { name: 'English' }))

    expect(useInterviewStore.getState().interviewLanguage).toBe('en')

    useInterviewStore.getState().setUiLanguage('es')
  })
})
