// Application constants
export const APP_NAME = "Bug Tracker"

export const BUG_STATUSES = {
  OPEN: "open",
  IN_PROGRESS: "in-progress",
  RESOLVED: "resolved",
  CLOSED: "closed",
} as const

export const BUG_SEVERITIES = {
  LOW: "low",
  MEDIUM: "medium",
  HIGH: "high",
  CRITICAL: "critical",
} as const

export const STUDY_STATUSES = {
  PLANNED: "planned",
  IN_PROGRESS: "in-progress",
  COMPLETED: "completed",
  CANCELLED: "cancelled",
} as const

export const PRODUCT_STATUSES = {
  ACTIVE: "active",
  DEPRECATED: "deprecated",
  IN_DEVELOPMENT: "in-development",
} as const

export const ACTION_STATUSES = {
  PENDING: "pending",
  IN_PROGRESS: "in-progress",
  COMPLETED: "completed",
  CANCELLED: "cancelled",
} as const
