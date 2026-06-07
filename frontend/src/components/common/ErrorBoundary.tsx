import { Component, type ReactNode } from 'react'

interface Props {
  children: ReactNode
}

interface State {
  hasError: boolean
}

export default class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  private handleRetry = () => this.setState({ hasError: false })

  render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-[50vh] flex items-center justify-center p-6">
          <div className="max-w-sm w-full bg-gray-900 border border-gray-800 rounded-2xl p-6 text-center space-y-3">
            <p className="text-base font-semibold text-white">Something went wrong</p>
            <p className="text-sm text-gray-400">
              This page hit an unexpected error. You can try again or navigate elsewhere.
            </p>
            <button
              onClick={this.handleRetry}
              className="bg-emerald-500 hover:bg-emerald-600 text-gray-950 font-semibold rounded-xl px-4 py-2.5 min-h-[44px] transition-colors"
            >
              Try again
            </button>
          </div>
        </div>
      )
    }

    return this.props.children
  }
}
