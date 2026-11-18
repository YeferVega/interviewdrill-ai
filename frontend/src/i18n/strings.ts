export type UiLanguage = 'es' | 'en'

/**
 * Static UI strings for both languages (spec 021). Components receive the dictionary via
 * `ui(lang)` and compose parameterized labels (like "Skill name 2") with templates so the
 * dictionaries stay plain strings. Interview prose (questions, advice, guides) comes from
 * the backend, localized by the session language, and is NOT part of this dictionary.
 */
export interface UiStrings {
  // App shell
  navInterview: string
  navHistory: string
  mainNavAria: string
  uiLanguageToggleAria: string
  uiLanguageName: string
  // Theme
  themeSystem: string
  themeLight: string
  themeDark: string
  // Interview language selector
  interviewLangLabel: string
  interviewLangEs: string
  interviewLangEn: string
  // JD upload
  jdStep: string
  jdTitle: string
  jdSubtitle: string
  jdFileLabel: string
  jdPasteLabel: string
  jdPlaceholder: string
  jdAnalyze: string
  jdAnalyzing: string
  // Profile review
  reviewStep: string
  reviewTitle: string
  reviewSubtitle: string
  reviewRoleLabel: string
  reviewSeniorityLabel: string
  reviewRequiredSkills: string
  reviewAddSkill: string
  reviewSkillNamePrefix: string
  reviewSkillWeightPrefix: string
  reviewDeleteSkillPrefix: string
  reviewRemoveSkill: string
  reviewNiceToHave: string
  reviewResponsibilities: string
  reviewAddResponsibility: string
  reviewResponsibilityPrefix: string
  reviewDeleteResponsibilityPrefix: string
  reviewAnotherJd: string
  reviewStartInterview: string
  reviewStarting: string
  // Interview page
  interviewTitle: string
  interviewLayoutAria: string
  messagesAria: string
  difficultyPrefix: string
  signalrConnected: string
  restMode: string
  speakerInterviewer: string
  speakerYou: string
  completedTitle: string
  completedBody: string
  practiceAnother: string
  answerLabel: string
  answerPlaceholder: string
  sendAnswer: string
  evaluating: string
  // Feedback panel
  scoreOverall: string
  scoreTechnicalDepth: string
  scoreProduction: string
  verdictPass: string
  verdictNeedsPractice: string
  verdictFail: string
  progressPrefix: string
  // Difficulty labels
  difficultyEasy: string
  difficultyMedium: string
  difficultyHard: string
  difficultyExpert: string
  difficultyNone: string
  // History page
  historyEyebrow: string
  historyTitle: string
  historySubtitle: string
  historyRefresh: string
  historyLoading: string
  historyEmpty: string
  historySessionsAria: string
  historyViewGuide: string
  historySelectSessionPrefix: string
  // Study guide panel
  guideTitle: string
  guideDownload: string
  guidePrint: string
  // Date
  invalidDate: string
  // Store-level validation errors
  errorJdRequired: string
  errorProfileFirst: string
  errorAnswerRequired: string
  errorUnexpected: string
  errorSignalRPrefix: string
  errorSignalRSuffix: string
  // PDF export
  pdfFailed: string
}

const es: UiStrings = {
  navInterview: 'Entrevista',
  navHistory: 'Historial',
  mainNavAria: 'Navegación principal',
  uiLanguageToggleAria: 'Cambiar el idioma de la interfaz',
  uiLanguageName: 'Español',
  themeSystem: 'Tema del sistema',
  themeLight: 'Tema claro',
  themeDark: 'Tema oscuro',
  interviewLangLabel: 'Idioma de la entrevista (preguntas y feedback)',
  interviewLangEs: 'Español',
  interviewLangEn: 'English',
  jdStep: 'Paso 1',
  jdTitle: 'Practica para una vacante real',
  jdSubtitle: 'Sube el Job Description, revisa los skills detectados y realiza una entrevista adaptativa con feedback inmediato.',
  jdFileLabel: 'Job Description (PDF, DOCX o TXT)',
  jdPasteLabel: 'O pega el texto del Job Description',
  jdPlaceholder: 'Senior Backend Engineer. Required skills: RAG, Kafka, observabilidad...',
  jdAnalyze: 'Analizar JD',
  jdAnalyzing: 'Analizando...',
  reviewStep: 'Paso 2',
  reviewTitle: 'Revisa el perfil de la vacante',
  reviewSubtitle: 'Ajusta skills y responsabilidades antes de iniciar la entrevista.',
  reviewRoleLabel: 'Rol detectado',
  reviewSeniorityLabel: 'Seniority',
  reviewRequiredSkills: 'Required skills',
  reviewAddSkill: 'Agregar skill',
  reviewSkillNamePrefix: 'Nombre del skill',
  reviewSkillWeightPrefix: 'Peso del skill',
  reviewDeleteSkillPrefix: 'Eliminar skill',
  reviewRemoveSkill: 'Quitar',
  reviewNiceToHave: 'Nice to have (separados por coma)',
  reviewResponsibilities: 'Responsabilidades',
  reviewAddResponsibility: 'Agregar responsabilidad',
  reviewResponsibilityPrefix: 'Responsabilidad',
  reviewDeleteResponsibilityPrefix: 'Eliminar responsabilidad',
  reviewAnotherJd: 'Usar otro JD',
  reviewStartInterview: 'Iniciar entrevista',
  reviewStarting: 'Iniciando...',
  interviewTitle: 'Entrevista adaptativa',
  interviewLayoutAria: 'Entrevista en progreso',
  messagesAria: 'Historial de la entrevista',
  difficultyPrefix: 'Dificultad',
  signalrConnected: 'SignalR conectado',
  restMode: 'Modo REST',
  speakerInterviewer: 'Entrevistador',
  speakerYou: 'Tú',
  completedTitle: 'Entrevista completada',
  completedBody: 'La guía de estudio se generó con prioridad calculada a partir de las evaluaciones de la sesión.',
  practiceAnother: 'Practicar otra vacante',
  answerLabel: 'Respuesta técnica',
  answerPlaceholder: 'Explica arquitectura, trade-offs, seguridad, observabilidad y producción.',
  sendAnswer: 'Enviar respuesta',
  evaluating: 'Evaluando...',
  scoreOverall: 'Overall',
  scoreTechnicalDepth: 'Profundidad técnica',
  scoreProduction: 'Producción',
  verdictPass: 'PASS',
  verdictNeedsPractice: 'NECESITA PRÁCTICA',
  verdictFail: 'FALLA',
  progressPrefix: 'Progreso',
  difficultyEasy: 'Básica',
  difficultyMedium: 'Media',
  difficultyHard: 'Avanzada',
  difficultyExpert: 'Experta',
  difficultyNone: 'Sin iniciar',
  historyEyebrow: 'Progreso',
  historyTitle: 'Historial de entrevistas',
  historySubtitle: 'Revisa tus sesiones, veredictos y guías de estudio generadas.',
  historyRefresh: 'Actualizar',
  historyLoading: 'Cargando...',
  historyEmpty: 'No hay sesiones registradas todavía.',
  historySessionsAria: 'Sesiones',
  historyViewGuide: 'Ver guía',
  historySelectSessionPrefix: 'Selecciona una sesión para ver su guía. Sesión:',
  guideTitle: 'Guía de estudio',
  guideDownload: 'Descargar Markdown',
  guidePrint: 'Guardar PDF / Imprimir',
  invalidDate: 'Fecha inválida',
  errorJdRequired: 'Sube un archivo o pega el Job Description.',
  errorProfileFirst: 'Primero analiza un Job Description.',
  errorAnswerRequired: 'Escribe una respuesta antes de enviarla.',
  errorUnexpected: 'Ocurrió un error inesperado.',
  errorSignalRPrefix: 'SignalR no disponible:',
  errorSignalRSuffix: 'Usando REST.',
  pdfFailed: 'No se pudo generar el PDF.',
}

const en: UiStrings = {
  navInterview: 'Interview',
  navHistory: 'History',
  mainNavAria: 'Main navigation',
  uiLanguageToggleAria: 'Change interface language',
  uiLanguageName: 'English',
  themeSystem: 'System theme',
  themeLight: 'Light theme',
  themeDark: 'Dark theme',
  interviewLangLabel: 'Interview language (questions and feedback)',
  interviewLangEs: 'Español',
  interviewLangEn: 'English',
  jdStep: 'Step 1',
  jdTitle: 'Practice for a real job opening',
  jdSubtitle: 'Upload the Job Description, review the detected skills and run an adaptive interview with immediate feedback.',
  jdFileLabel: 'Job Description (PDF, DOCX or TXT)',
  jdPasteLabel: 'Or paste the Job Description text',
  jdPlaceholder: 'Senior Backend Engineer. Required skills: RAG, Kafka, observability...',
  jdAnalyze: 'Analyze JD',
  jdAnalyzing: 'Analyzing...',
  reviewStep: 'Step 2',
  reviewTitle: 'Review the job profile',
  reviewSubtitle: 'Adjust skills and responsibilities before starting the interview.',
  reviewRoleLabel: 'Detected role',
  reviewSeniorityLabel: 'Seniority',
  reviewRequiredSkills: 'Required skills',
  reviewAddSkill: 'Add skill',
  reviewSkillNamePrefix: 'Skill name',
  reviewSkillWeightPrefix: 'Skill weight',
  reviewDeleteSkillPrefix: 'Delete skill',
  reviewRemoveSkill: 'Remove',
  reviewNiceToHave: 'Nice to have (comma separated)',
  reviewResponsibilities: 'Responsibilities',
  reviewAddResponsibility: 'Add responsibility',
  reviewResponsibilityPrefix: 'Responsibility',
  reviewDeleteResponsibilityPrefix: 'Delete responsibility',
  reviewAnotherJd: 'Use another JD',
  reviewStartInterview: 'Start interview',
  reviewStarting: 'Starting...',
  interviewTitle: 'Adaptive interview',
  interviewLayoutAria: 'Interview in progress',
  messagesAria: 'Interview transcript',
  difficultyPrefix: 'Difficulty',
  signalrConnected: 'SignalR connected',
  restMode: 'REST mode',
  speakerInterviewer: 'Interviewer',
  speakerYou: 'You',
  completedTitle: 'Interview completed',
  completedBody: 'The study guide was generated with priority calculated from the session evaluations.',
  practiceAnother: 'Practice another opening',
  answerLabel: 'Technical answer',
  answerPlaceholder: 'Explain architecture, trade-offs, security, observability and production aspects.',
  sendAnswer: 'Submit answer',
  evaluating: 'Evaluating...',
  scoreOverall: 'Overall',
  scoreTechnicalDepth: 'Technical depth',
  scoreProduction: 'Production',
  verdictPass: 'PASS',
  verdictNeedsPractice: 'NEEDS PRACTICE',
  verdictFail: 'FAIL',
  progressPrefix: 'Progress',
  difficultyEasy: 'Basic',
  difficultyMedium: 'Medium',
  difficultyHard: 'Advanced',
  difficultyExpert: 'Expert',
  difficultyNone: 'Not started',
  historyEyebrow: 'Progress',
  historyTitle: 'Interview history',
  historySubtitle: 'Review your sessions, verdicts and generated study guides.',
  historyRefresh: 'Refresh',
  historyLoading: 'Loading...',
  historyEmpty: 'No sessions recorded yet.',
  historySessionsAria: 'Sessions',
  historyViewGuide: 'View guide',
  historySelectSessionPrefix: 'Select a session to view its guide. Session:',
  guideTitle: 'Study guide',
  guideDownload: 'Download Markdown',
  guidePrint: 'Save PDF / Print',
  invalidDate: 'Invalid date',
  errorJdRequired: 'Upload a file or paste the Job Description.',
  errorProfileFirst: 'Analyze a Job Description first.',
  errorAnswerRequired: 'Write an answer before submitting it.',
  errorUnexpected: 'An unexpected error occurred.',
  errorSignalRPrefix: 'SignalR unavailable:',
  errorSignalRSuffix: 'Using REST.',
  pdfFailed: 'Could not generate the PDF.',
}

const dictionaries: Record<UiLanguage, UiStrings> = { es, en }

export function ui(lang: UiLanguage | undefined | null): UiStrings {
  return dictionaries[lang === 'en' ? 'en' : 'es']
}
