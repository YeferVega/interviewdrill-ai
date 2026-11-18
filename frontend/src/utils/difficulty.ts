import type { Difficulty } from '../types/interview'
import { ui, type UiLanguage } from '../i18n/strings'

export function formatDifficulty(
  difficulty: Difficulty | undefined,
  lang: UiLanguage = 'es',
): string {
  const strings = ui(lang)

  switch (difficulty) {
    case 'easy':
      return strings.difficultyEasy
    case 'medium':
      return strings.difficultyMedium
    case 'hard':
      return strings.difficultyHard
    case 'expert':
      return strings.difficultyExpert
    default:
      return strings.difficultyNone
  }
}
