// This is a placeholder for the Action repository
// In a real application, you would implement CRUD operations here

import type { Action } from "@/types/action"

export const ActionRepository = {
  findAll: async (): Promise<Action[]> => {
    return []
  },
  findById: async (id: string): Promise<Action | null> => {
    return null
  },
  create: async (data: Omit<Action, "id" | "createdAt" | "updatedAt">): Promise<Action> => {
    throw new Error("Not implemented")
  },
  update: async (id: string, data: Partial<Action>): Promise<Action> => {
    throw new Error("Not implemented")
  },
  delete: async (id: string): Promise<void> => {
    throw new Error("Not implemented")
  },
}
