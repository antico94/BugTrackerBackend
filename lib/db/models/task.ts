import sql from "mssql"
import { getDbConnection } from "../index"
import type { Task, CreateTaskDto, UpdateTaskDto, TaskStep } from "@/types/task"

export class TaskModel {
  private static readonly tableName = "Tasks"

  static async findAll(): Promise<Task[]> {
    try {
      console.log("TaskModel: Finding all tasks")
      const pool = await getDbConnection()
      const result = await pool.request().query(`
        SELECT 
          id,
          title,
          description,
          bugId,
          productId,
          productType,
          status,
          steps,
          currentStepIndex,
          createdAt,
          updatedAt,
          completedAt,
          assignedTo
        FROM ${this.tableName}
      `)

      console.log(`TaskModel: Found ${result.recordset.length} tasks in database`)

      // Log the first task for debugging
      if (result.recordset.length > 0) {
        console.log("TaskModel: First task sample:", {
          id: result.recordset[0].id,
          title: result.recordset[0].title,
          status: result.recordset[0].status,
        })
      }

      return result.recordset.map(this.mapToTask)
    } catch (error) {
      console.error("TaskModel: Error finding all tasks:", error)
      throw error
    }
  }

  static async findById(id: string): Promise<Task | null> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .query(`
        SELECT 
          id,
          title,
          description,
          bugId,
          productId,
          productType,
          status,
          steps,
          currentStepIndex,
          createdAt,
          updatedAt,
          completedAt,
          assignedTo
        FROM ${this.tableName}
        WHERE id = @id
      `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToTask(result.recordset[0])
  }

  static async findByBugId(bugId: string): Promise<Task[]> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("bugId", sql.UniqueIdentifier, bugId)
      .query(`
        SELECT 
          id,
          title,
          description,
          bugId,
          productId,
          productType,
          status,
          steps,
          currentStepIndex,
          createdAt,
          updatedAt,
          completedAt,
          assignedTo
        FROM ${this.tableName}
        WHERE bugId = @bugId
      `)

    return result.recordset.map(this.mapToTask)
  }

  static async findByProductId(productId: string): Promise<Task[]> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("productId", sql.UniqueIdentifier, productId)
      .query(`
        SELECT 
          id,
          title,
          description,
          bugId,
          productId,
          productType,
          status,
          steps,
          currentStepIndex,
          createdAt,
          updatedAt,
          completedAt,
          assignedTo
        FROM ${this.tableName}
        WHERE productId = @productId
      `)

    return result.recordset.map(this.mapToTask)
  }

  static async create(task: CreateTaskDto): Promise<Task> {
    const pool = await getDbConnection()
    const id = task.id || crypto.randomUUID()
    const now = new Date()

    // Process steps to ensure taskId is set and they're sorted by order
    const steps =
      task.steps?.map((step, index) => ({
        ...step,
        id: step.id || crypto.randomUUID(),
        taskId: id,
      })) || []

    // Sort steps by order to ensure we get the first step correctly
    const sortedSteps = [...steps].sort((a, b) => a.order - b.order)

    // Always set currentStepIndex to 0 (the first step)
    const currentStepIndex = 0

    // Convert steps array to JSON string for storage
    const stepsJson = JSON.stringify(steps)

    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .input("title", sql.NVarChar, task.title)
      .input("description", sql.NVarChar, task.description || null)
      .input("bugId", sql.UniqueIdentifier, task.bugId)
      .input("productId", sql.UniqueIdentifier, task.productId || null)
      .input("productType", sql.NVarChar, task.productType)
      .input("status", sql.NVarChar, task.status || "Pending")
      .input("steps", sql.NVarChar, stepsJson)
      .input("currentStepIndex", sql.Int, currentStepIndex)
      .input("createdAt", sql.DateTime, now)
      .input("updatedAt", sql.DateTime, now)
      .input("assignedTo", sql.NVarChar, task.assignedTo || null)
      .input("completedAt", sql.DateTime, task.completedAt || null)
      .query(`
        INSERT INTO ${this.tableName} (
          id,
          title,
          description,
          bugId,
          productId,
          productType,
          status,
          steps,
          currentStepIndex,
          createdAt,
          updatedAt,
          assignedTo,
          completedAt
        )
        VALUES (
          @id,
          @title,
          @description,
          @bugId,
          @productId,
          @productType,
          @status,
          @steps,
          @currentStepIndex,
          @createdAt,
          @updatedAt,
          @assignedTo,
          @completedAt
        );
        
        SELECT 
          id,
          title,
          description,
          bugId,
          productId,
          productType,
          status,
          steps,
          currentStepIndex,
          createdAt,
          updatedAt,
          completedAt,
          assignedTo
        FROM ${this.tableName}
        WHERE id = @id
      `)

    return this.mapToTask(result.recordset[0])
  }

  static async update(id: string, task: UpdateTaskDto): Promise<Task | null> {
    const pool = await getDbConnection()
    const now = new Date()

    // Build dynamic update query based on provided fields
    const updateFields: string[] = []
    const request = pool.request().input("id", sql.UniqueIdentifier, id)

    if (task.title !== undefined) {
      updateFields.push("title = @title")
      request.input("title", sql.NVarChar, task.title)
    }

    if (task.description !== undefined) {
      updateFields.push("description = @description")
      request.input("description", sql.NVarChar, task.description)
    }

    if (task.status !== undefined) {
      updateFields.push("status = @status")
      request.input("status", sql.NVarChar, task.status)
    }

    if (task.steps !== undefined) {
      updateFields.push("steps = @steps")
      request.input("steps", sql.NVarChar, JSON.stringify(task.steps))
    }

    // Handle currentStepId by converting it to currentStepIndex
    if (task.currentStepId !== undefined) {
      // First get the current task to find the step index
      const currentTask = await this.findById(id)
      if (currentTask) {
        let currentStepIndex = 0
        if (task.currentStepId === null) {
          // If currentStepId is null, set currentStepIndex to -1 or another value indicating no current step
          currentStepIndex = -1
        } else {
          // Find the index of the step with the given ID
          const stepIndex = currentTask.steps.findIndex((step) => step.id === task.currentStepId)
          if (stepIndex !== -1) {
            currentStepIndex = stepIndex
          }
        }
        updateFields.push("currentStepIndex = @currentStepIndex")
        request.input("currentStepIndex", sql.Int, currentStepIndex)
      }
    }

    if (task.completedAt !== undefined) {
      updateFields.push("completedAt = @completedAt")
      request.input("completedAt", sql.DateTime, task.completedAt)
    }

    if (task.assignedTo !== undefined) {
      updateFields.push("assignedTo = @assignedTo")
      request.input("assignedTo", sql.NVarChar, task.assignedTo || null)
    }

    // Always update the updatedAt timestamp
    updateFields.push("updatedAt = @updatedAt")
    request.input("updatedAt", sql.DateTime, now)

    if (updateFields.length === 0) {
      // No fields to update
      return this.findById(id)
    }

    const result = await request.query(`
      UPDATE ${this.tableName}
      SET ${updateFields.join(", ")}
      WHERE id = @id;
      
      SELECT 
        id,
        title,
        description,
        bugId,
        productId,
        productType,
        status,
        steps,
        currentStepIndex,
        createdAt,
        updatedAt,
        completedAt,
        assignedTo
      FROM ${this.tableName}
      WHERE id = @id
    `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToTask(result.recordset[0])
  }

  static async delete(id: string): Promise<boolean> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .query(`
        DELETE FROM ${this.tableName}
        WHERE id = @id
      `)

    return result.rowsAffected[0] > 0
  }

  private static mapToTask(record: any): Task {
    // Parse the JSON string back to an array of steps
    let steps: TaskStep[] = []
    try {
      if (record.steps) {
        steps = JSON.parse(record.steps)
      }
    } catch (error) {
      console.error("Error parsing steps:", error)
    }

    // Sort steps by order
    steps = steps.sort((a, b) => a.order - b.order)

    // Determine the current step ID based on the currentStepIndex
    let currentStepId: string | null = null
    if (record.currentStepIndex >= 0 && record.currentStepIndex < steps.length) {
      currentStepId = steps[record.currentStepIndex]?.id || null
    }

    return {
      id: record.id,
      title: record.title,
      description: record.description,
      bugId: record.bugId,
      productId: record.productId,
      productType: record.productType,
      status: record.status,
      steps,
      currentStepId,
      createdAt: new Date(record.createdAt),
      updatedAt: new Date(record.updatedAt),
      completedAt: record.completedAt ? new Date(record.completedAt) : undefined,
      assignedTo: record.assignedTo,
    }
  }
}
