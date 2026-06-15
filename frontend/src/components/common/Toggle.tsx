interface ToggleProps {
  checked?: boolean
  onChange: (v: boolean) => void
  disabled?: boolean
}

export function Toggle({ checked = false, onChange, disabled = false }: ToggleProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={() => !disabled && onChange(!checked)}
      className={`relative inline-flex w-10 h-6 rounded-full transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-teal-500 focus:ring-offset-2 focus:ring-offset-gray-900 ${
        disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'
      } ${checked ? 'bg-teal-500' : 'bg-gray-700'}`}
    >
      <span
        className={`inline-block w-4 h-4 mt-1 rounded-full bg-white shadow transition-transform duration-200 ${
          checked ? 'translate-x-5' : 'translate-x-1'
        }`}
      />
    </button>
  )
}
