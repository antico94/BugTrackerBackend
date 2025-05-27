import sql from "mssql"
import { getDbConnection } from "../index"
import type { Study, CreateStudyDto, UpdateStudyDto } from "@/types/study"

export class StudyModel {
  private static readonly tableName = "Studies"

  static async findAll(): Promise<Study[]> {
    const pool = await getDbConnection()
    const result = await pool.request().query(`
      SELECT 
        id,
        jiraKey,
        jiraLink,
        summary,
        irtVersion,
        clientName,
        protocol,
        irtLink,
        tmLink,
        openLabel,
        createdAt,
        updatedAt
      FROM ${this.tableName}
    `)

    return result.recordset.map(this.mapToStudy)
  }

  static async findById(id: string): Promise<Study | null> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .query(`
        SELECT 
          id,
          jiraKey,
          jiraLink,
          summary,
          irtVersion,
          clientName,
          protocol,
          irtLink,
          tmLink,
          openLabel,
          createdAt,
          updatedAt
        FROM ${this.tableName}
        WHERE id = @id
      `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToStudy(result.recordset[0])
  }

  static async create(study: CreateStudyDto): Promise<Study> {
    const pool = await getDbConnection()
    const id = crypto.randomUUID()
    const now = new Date()

    // Ensure openLabel is a proper boolean value for SQL Server
    const openLabelValue = study.openLabel === true || study.openLabel === 1 ? 1 : 0

    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .input("jiraKey", sql.NVarChar, study.jiraKey)
      .input("jiraLink", sql.NVarChar, study.jiraLink)
      .input("summary", sql.NVarChar, study.summary)
      .input("irtVersion", sql.NVarChar, study.irtVersion)
      .input("clientName", sql.NVarChar, study.clientName)
      .input("protocol", sql.NVarChar, study.protocol)
      .input("irtLink", sql.NVarChar, study.irtLink)
      .input("tmLink", sql.NVarChar, study.tmLink)
      .input("openLabel", sql.Bit, openLabelValue) // Use Bit type with 0/1 value
      .input("createdAt", sql.DateTime, now)
      .input("updatedAt", sql.DateTime, now)
      .query(`
        INSERT INTO ${this.tableName} (
          id,
          jiraKey,
          jiraLink,
          summary,
          irtVersion,
          clientName,
          protocol,
          irtLink,
          tmLink,
          openLabel,
          createdAt,
          updatedAt
        )
        VALUES (
          @id,
          @jiraKey,
          @jiraLink,
          @summary,
          @irtVersion,
          @clientName,
          @protocol,
          @irtLink,
          @tmLink,
          @openLabel,
          @createdAt,
          @updatedAt
        );
        
        SELECT 
          id,
          jiraKey,
          jiraLink,
          summary,
          irtVersion,
          clientName,
          protocol,
          irtLink,
          tmLink,
          openLabel,
          createdAt,
          updatedAt
        FROM ${this.tableName}
        WHERE id = @id
      `)

    return this.mapToStudy(result.recordset[0])
  }

  static async update(id: string, study: UpdateStudyDto): Promise<Study | null> {
    const pool = await getDbConnection()
    const now = new Date()

    // Build dynamic update query based on provided fields
    const updateFields: string[] = []
    const request = pool.request().input("id", sql.UniqueIdentifier, id)

    if (study.jiraKey !== undefined) {
      updateFields.push("jiraKey = @jiraKey")
      request.input("jiraKey", sql.NVarChar, study.jiraKey)
    }

    if (study.jiraLink !== undefined) {
      updateFields.push("jiraLink = @jiraLink")
      request.input("jiraLink", sql.NVarChar, study.jiraLink)
    }

    if (study.summary !== undefined) {
      updateFields.push("summary = @summary")
      request.input("summary", sql.NVarChar, study.summary)
    }

    if (study.irtVersion !== undefined) {
      updateFields.push("irtVersion = @irtVersion")
      request.input("irtVersion", sql.NVarChar, study.irtVersion)
    }

    if (study.clientName !== undefined) {
      updateFields.push("clientName = @clientName")
      request.input("clientName", sql.NVarChar, study.clientName)
    }

    if (study.protocol !== undefined) {
      updateFields.push("protocol = @protocol")
      request.input("protocol", sql.NVarChar, study.protocol)
    }

    if (study.irtLink !== undefined) {
      updateFields.push("irtLink = @irtLink")
      request.input("irtLink", sql.NVarChar, study.irtLink)
    }

    if (study.tmLink !== undefined) {
      updateFields.push("tmLink = @tmLink")
      request.input("tmLink", sql.NVarChar, study.tmLink)
    }

    if (study.openLabel !== undefined) {
      // Ensure openLabel is a proper boolean value for SQL Server
      const openLabelValue = study.openLabel === true || study.openLabel === 1 ? 1 : 0
      updateFields.push("openLabel = @openLabel")
      request.input("openLabel", sql.Bit, openLabelValue) // Use Bit type with 0/1 value
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
        jiraLink,
        summary,
        irtVersion,
        clientName,
        protocol,
        irtLink,
        tmLink,
        openLabel,
        createdAt,
        updatedAt
      FROM ${this.tableName}
      WHERE id = @id
    `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToStudy(result.recordset[0])
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

  private static mapToStudy(record: any): Study {
    return {
      id: record.id,
      jiraKey: record.jiraKey,
      jiraLink: record.jiraLink,
      summary: record.summary,
      irtVersion: record.irtVersion,
      clientName: record.clientName,
      protocol: record.protocol,
      irtLink: record.irtLink,
      tmLink: record.tmLink,
      openLabel: Boolean(record.openLabel), // Ensure it's converted to a proper boolean
      createdAt: new Date(record.createdAt),
      updatedAt: new Date(record.updatedAt),
    }
  }
}
