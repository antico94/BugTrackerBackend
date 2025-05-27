import sql from "mssql"
import { getDbConnection } from "../index"
import type { Bug, CreateBugDto, UpdateBugDto } from "@/types/bug"

export class BugModel {
  private static readonly tableName = "Bugs"

  static async findAll(): Promise<Bug[]> {
    const pool = await getDbConnection()
    const result = await pool.request().query(`
      SELECT 
        id,
        jiraKey,
        title,
        description,
        severity,
        foundInBuild,
        affectedVersions,
        productId,
        assignedTo,
        status,
        createdAt,
        updatedAt,
        isAssessmentCompleted,
        assessedProductType
      FROM ${this.tableName}
    `)

    return result.recordset.map(this.mapToBug)
  }

  static async findById(id: string): Promise<Bug | null> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .query(`
        SELECT 
          id,
          jiraKey,
          title,
          description,
          severity,
          foundInBuild,
          affectedVersions,
          productId,
          assignedTo,
          status,
          createdAt,
          updatedAt,
          isAssessmentCompleted,
          assessedProductType
        FROM ${this.tableName}
        WHERE id = @id
      `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToBug(result.recordset[0])
  }

  static async findByJiraKey(jiraKey: string): Promise<Bug | null> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("jiraKey", sql.NVarChar, jiraKey)
      .query(`
        SELECT 
          id,
          jiraKey,
          title,
          description,
          severity,
          foundInBuild,
          affectedVersions,
          productId,
          assignedTo,
          status,
          createdAt,
          updatedAt,
          isAssessmentCompleted,
          assessedProductType
        FROM ${this.tableName}
        WHERE jiraKey = @jiraKey
      `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToBug(result.recordset[0])
  }

  static async create(bug: CreateBugDto): Promise<Bug> {
    const pool = await getDbConnection()
    const id = bug.id || crypto.randomUUID()
    const now = new Date()

    // Convert array to JSON string for storage
    const affectedVersionsJson = JSON.stringify(bug.affectedVersions || [])

    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .input("jiraKey", sql.NVarChar, bug.jiraKey)
      .input("title", sql.NVarChar, bug.title)
      .input("description", sql.NVarChar, bug.description)
      .input("severity", sql.NVarChar, bug.severity)
      .input("foundInBuild", sql.NVarChar, bug.foundInBuild || null)
      .input("affectedVersions", sql.NVarChar, affectedVersionsJson)
      .input("productId", sql.UniqueIdentifier, bug.productId || null)
      .input("assignedTo", sql.NVarChar, bug.assignedTo || null)
      .input("status", sql.NVarChar, bug.status)
      .input("createdAt", sql.DateTime, now)
      .input("updatedAt", sql.DateTime, now)
      .input("isAssessmentCompleted", sql.Bit, bug.isAssessmentCompleted || false)
      .input("assessedProductType", sql.NVarChar, bug.assessedProductType || null)
      .query(`
        INSERT INTO ${this.tableName} (
          id,
          jiraKey,
          title,
          description,
          severity,
          foundInBuild,
          affectedVersions,
          productId,
          assignedTo,
          status,
          createdAt,
          updatedAt,
          isAssessmentCompleted,
          assessedProductType
        )
        VALUES (
          @id,
          @jiraKey,
          @title,
          @description,
          @severity,
          @foundInBuild,
          @affectedVersions,
          @productId,
          @assignedTo,
          @status,
          @createdAt,
          @updatedAt,
          @isAssessmentCompleted,
          @assessedProductType
        );
        
        SELECT 
          id,
          jiraKey,
          title,
          description,
          severity,
          foundInBuild,
          affectedVersions,
          productId,
          assignedTo,
          status,
          createdAt,
          updatedAt,
          isAssessmentCompleted,
          assessedProductType
        FROM ${this.tableName}
        WHERE id = @id
      `)

    return this.mapToBug(result.recordset[0])
  }

  static async update(id: string, bug: UpdateBugDto): Promise<Bug | null> {
    const pool = await getDbConnection()
    const now = new Date()

    // Build dynamic update query based on provided fields
    const updateFields: string[] = []
    const request = pool.request().input("id", sql.UniqueIdentifier, id)

    if (bug.jiraKey !== undefined) {
      updateFields.push("jiraKey = @jiraKey")
      request.input("jiraKey", sql.NVarChar, bug.jiraKey)
    }

    if (bug.title !== undefined) {
      updateFields.push("title = @title")
      request.input("title", sql.NVarChar, bug.title)
    }

    if (bug.description !== undefined) {
      updateFields.push("description = @description")
      request.input("description", sql.NVarChar, bug.description)
    }

    if (bug.severity !== undefined) {
      updateFields.push("severity = @severity")
      request.input("severity", sql.NVarChar, bug.severity)
    }

    if (bug.foundInBuild !== undefined) {
      updateFields.push("foundInBuild = @foundInBuild")
      request.input("foundInBuild", sql.NVarChar, bug.foundInBuild || null)
    }

    if (bug.affectedVersions !== undefined) {
      updateFields.push("affectedVersions = @affectedVersions")
      request.input("affectedVersions", sql.NVarChar, JSON.stringify(bug.affectedVersions))
    }

    if (bug.productId !== undefined) {
      updateFields.push("productId = @productId")
      request.input("productId", sql.UniqueIdentifier, bug.productId || null)
    }

    if (bug.assignedTo !== undefined) {
      updateFields.push("assignedTo = @assignedTo")
      request.input("assignedTo", sql.NVarChar, bug.assignedTo || null)
    }

    if (bug.status !== undefined) {
      updateFields.push("status = @status")
      request.input("status", sql.NVarChar, bug.status)
    }

    if (bug.isAssessmentCompleted !== undefined) {
      updateFields.push("isAssessmentCompleted = @isAssessmentCompleted")
      request.input("isAssessmentCompleted", sql.Bit, bug.isAssessmentCompleted)
    }

    if (bug.assessedProductType !== undefined) {
      updateFields.push("assessedProductType = @assessedProductType")
      request.input("assessedProductType", sql.NVarChar, bug.assessedProductType || null)
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
        jiraKey,
        title,
        description,
        severity,
        foundInBuild,
        affectedVersions,
        productId,
        assignedTo,
        status,
        createdAt,
        updatedAt,
        isAssessmentCompleted,
        assessedProductType
      FROM ${this.tableName}
      WHERE id = @id
    `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToBug(result.recordset[0])
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

  private static mapToBug(record: any): Bug {
    // Parse the JSON string back to an array
    let affectedVersions: string[] = []
    try {
      if (record.affectedVersions) {
        affectedVersions = JSON.parse(record.affectedVersions)
      }
    } catch (error) {
      console.error("Error parsing affectedVersions:", error)
    }

    return {
      id: record.id,
      jiraKey: record.jiraKey,
      title: record.title,
      description: record.description,
      severity: record.severity,
      foundInBuild: record.foundInBuild,
      affectedVersions,
      productId: record.productId,
      assignedTo: record.assignedTo,
      status: record.status,
      createdAt: new Date(record.createdAt),
      updatedAt: new Date(record.updatedAt),
      isAssessmentCompleted: record.isAssessmentCompleted || false,
      assessedProductType: record.assessedProductType || null,
    }
  }
}
