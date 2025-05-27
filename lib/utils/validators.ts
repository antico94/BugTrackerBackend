// Basic validation functions
export const isValidEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

export const isNonEmptyString = (value: string): boolean => {
  return typeof value === "string" && value.trim().length > 0
}

export const isValidDate = (date: Date): boolean => {
  return date instanceof Date && !isNaN(date.getTime())
}
