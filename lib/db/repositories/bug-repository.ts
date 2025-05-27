import { BugModel } from "../models/bug"
import type { Bug, CreateBugDto, UpdateBugDto } from "@/types/bug"

export class BugRepository {
  static async findAll(): Promise<Bug[]> {
    return BugModel.findAll()
  }

  static async findById(id: string): Promise<Bug | null> {
    return BugModel.findById(id)
  }

  static async findByJiraKey(jiraKey: string): Promise<Bug | null> {
    return BugModel.findByJiraKey(jiraKey)
  }

  static async create(bug: CreateBugDto): Promise<Bug> {
    // Ensure assessed is set to false by default
    const bugWithDefaults = {
      ...bug,
      assessed: bug.assessed ?? false,
    }
    return BugModel.create(bugWithDefaults)
  }

  static async update(id: string, bug: UpdateBugDto): Promise<Bug | null> {
    console.log("Updating bug in repository:", id, bug)
    return BugModel.update(id, bug)
  }

  static async delete(id: string): Promise<boolean> {
    return BugModel.delete(id)
  }
}
