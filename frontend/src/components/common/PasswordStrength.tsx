interface Rule {
  label: string
  test: (pw: string) => boolean
}

const rules: Rule[] = [
  { label: 'At least 8 characters', test: (pw) => pw.length >= 8 },
  { label: 'An uppercase letter', test: (pw) => /[A-Z]/.test(pw) },
  { label: 'A lowercase letter', test: (pw) => /[a-z]/.test(pw) },
  { label: 'A digit', test: (pw) => /\d/.test(pw) },
  { label: 'A special character', test: (pw) => /[^A-Za-z0-9]/.test(pw) },
]

export function isPasswordValid(pw: string): boolean {
  return rules.every((r) => r.test(pw))
}

export default function PasswordStrength({ password }: { password: string }) {
  if (!password) return null

  const passed = rules.filter((r) => r.test(password)).length
  const barColor =
    passed <= 2 ? 'bg-red-500' : passed <= 4 ? 'bg-amber-500' : 'bg-emerald-500'

  return (
    <div className="space-y-1.5">
      <div className="h-1.5 bg-gray-800 rounded-full overflow-hidden">
        <div
          className={`h-full transition-all ${barColor}`}
          style={{ width: `${(passed / rules.length) * 100}%` }}
        />
      </div>
      <ul className="grid grid-cols-2 gap-x-2 gap-y-0.5 text-xs">
        {rules.map((r) => {
          const ok = r.test(password)
          return (
            <li key={r.label} className={ok ? 'text-emerald-400' : 'text-gray-500'}>
              {ok ? '✓' : '·'} {r.label}
            </li>
          )
        })}
      </ul>
    </div>
  )
}
