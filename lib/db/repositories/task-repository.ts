import { TaskModel } from "../models/task"
import type { Task, CreateTaskDto, UpdateTaskDto } from "@/types/task"

export class TaskRepository {
  static async findAll(): Promise<Task[]> {
    try {
      console.log("TaskRepository: Finding all tasks")
      const tasks = await TaskModel.findAll()
      console.log(`TaskRepository: Found ${tasks.length} tasks`)
      return tasks
    } catch (error) {
      console.error("TaskRepository: Error finding all tasks:", error)
      throw error
    }
  }

  static async findById(id: string): Promise<Task | null> {
    return TaskModel.findById(id)
  }

  static async findByBugId(bugId: string): Promise<Task[]> {
    return TaskModel.findByBugId(bugId)
  }

  static async findByProductId(productId: string): Promise<Task[]> {
    return TaskModel.findByProductId(productId)
  }

  static async create(task: CreateTaskDto): Promise<Task> {
    return TaskModel.create(task)
  }

  static async update(id: string, task: UpdateTaskDto): Promise<Task | null> {
    return TaskModel.update(id, task)
  }

  static async delete(id: string): Promise<boolean> {
    return TaskModel.delete(id)
  }

  // Add the updateStep method to the TaskRepository class
  static async updateStep(taskId: string, stepId: string, updates: any): Promise<Task | null> {
    try {
      const task = await this.findById(taskId)
      if (!task) {
        return null
      }

      const stepIndex = task.steps.findIndex((step) => step.id === stepId)
      if (stepIndex === -1) {
        return null
      }

      // Update the step with the provided updates
      task.steps[stepIndex] = {
        ...task.steps[stepIndex],
        ...updates,
      }

      // Update the task with the modified steps
      return this.update(taskId, { steps: task.steps })
    } catch (error) {
      console.error("TaskRepository: Error updating step:", error)
      throw error
    }
  }
}
