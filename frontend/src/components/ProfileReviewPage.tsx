import { useInterviewStore } from '../store/interviewStore'
import { ui } from '../i18n/strings'
import { InterviewLanguageField } from './InterviewLanguageField'
import type { Seniority } from '../types/interview'

const seniorities: Seniority[] = ['unknown', 'junior', 'mid', 'senior', 'lead']

export function ProfileReviewPage() {
  const profile = useInterviewStore((state) => state.profile)
  const busy = useInterviewStore((state) => state.busy)
  const error = useInterviewStore((state) => state.error)
  const updateRole = useInterviewStore((state) => state.updateRole)
  const updateSeniority = useInterviewStore((state) => state.updateSeniority)
  const updateRequiredSkill = useInterviewStore((state) => state.updateRequiredSkill)
  const addRequiredSkill = useInterviewStore((state) => state.addRequiredSkill)
  const removeRequiredSkill = useInterviewStore((state) => state.removeRequiredSkill)
  const updateNiceToHaveSkills = useInterviewStore((state) => state.updateNiceToHaveSkills)
  const updateResponsibility = useInterviewStore((state) => state.updateResponsibility)
  const addResponsibility = useInterviewStore((state) => state.addResponsibility)
  const removeResponsibility = useInterviewStore((state) => state.removeResponsibility)
  const startInterview = useInterviewStore((state) => state.startInterview)
  const reset = useInterviewStore((state) => state.reset)
  const strings = ui(useInterviewStore((state) => state.uiLanguage))

  if (!profile) {
    return null
  }

  return (
    <section className="page" aria-labelledby="profile-title">
      <header className="page-header">
        <p className="eyebrow">{strings.reviewStep}</p>
        <h1 id="profile-title">{strings.reviewTitle}</h1>
        <p>{strings.reviewSubtitle}</p>
      </header>

      <div className="card profile-card">
        <div className="form-grid">
          <div className="field">
            <label htmlFor="role">{strings.reviewRoleLabel}</label>
            <input id="role" value={profile.role} onChange={(event) => updateRole(event.target.value)} />
          </div>

          <div className="field">
            <label htmlFor="seniority">{strings.reviewSeniorityLabel}</label>
            <select
              id="seniority"
              value={profile.seniority}
              onChange={(event) => updateSeniority(event.target.value as Seniority)}
            >
              {seniorities.map((item) => (
                <option key={item} value={item}>{item}</option>
              ))}
            </select>
          </div>
        </div>

        <div className="section-block">
          <div className="section-heading">
            <h2>{strings.reviewRequiredSkills}</h2>
            <button type="button" onClick={addRequiredSkill}>{strings.reviewAddSkill}</button>
          </div>

          <ul className="editable-list" aria-label={strings.reviewRequiredSkills}>
            {profile.requiredSkills.map((skill, index) => (
              <li key={`${skill.name}-${index}`}>
                <input
                  aria-label={`${strings.reviewSkillNamePrefix} ${index + 1}`}
                  value={skill.name}
                  onChange={(event) => updateRequiredSkill(index, { name: event.target.value })}
                />
                <input
                  aria-label={`${strings.reviewSkillWeightPrefix} ${index + 1}`}
                  type="number"
                  min="0"
                  max="1"
                  step="0.05"
                  value={skill.weight}
                  onChange={(event) => updateRequiredSkill(index, { weight: Number(event.target.value) })}
                />
                <button
                  type="button"
                  onClick={() => removeRequiredSkill(index)}
                  aria-label={`${strings.reviewDeleteSkillPrefix} ${skill.name || index + 1}`}
                >
                  {strings.reviewRemoveSkill}
                </button>
              </li>
            ))}
          </ul>
        </div>

        <div className="field">
          <label htmlFor="nice-to-have">{strings.reviewNiceToHave}</label>
          <input
            id="nice-to-have"
            value={profile.niceToHaveSkills.join(', ')}
            onChange={(event) => updateNiceToHaveSkills(event.target.value)}
          />
        </div>

        <div className="section-block">
          <div className="section-heading">
            <h2>{strings.reviewResponsibilities}</h2>
            <button type="button" onClick={addResponsibility}>{strings.reviewAddResponsibility}</button>
          </div>

          <ul className="editable-list">
            {profile.responsibilities.map((responsibility, index) => (
              <li key={`${responsibility}-${index}`}>
                <input
                  aria-label={`${strings.reviewResponsibilityPrefix} ${index + 1}`}
                  value={responsibility}
                  onChange={(event) => updateResponsibility(index, event.target.value)}
                />
                <button
                  type="button"
                  onClick={() => removeResponsibility(index)}
                  aria-label={`${strings.reviewDeleteResponsibilityPrefix} ${index + 1}`}
                >
                  {strings.reviewRemoveSkill}
                </button>
              </li>
            ))}
          </ul>
        </div>

        <InterviewLanguageField />

        <div className="actions">
          <button className="secondary-button" type="button" onClick={() => void reset()}>{strings.reviewAnotherJd}</button>
          <button className="primary-button" type="button" disabled={busy} onClick={() => void startInterview()}>
            {busy ? strings.reviewStarting : strings.reviewStartInterview}
          </button>
        </div>

        {error ? <p className="error" role="alert">{error}</p> : null}
      </div>
    </section>
  )
}
