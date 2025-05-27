import sql from "mssql"
import { getDbConnection } from "../index"
import type { TM, CreateTMDto } from "@/types/product-types"

export class TMModel {
  private static readonly tableName = "TMs"

  // Make sure the column names in the SQL queries match the database schema exactly
  static async findAll(): Promise<TM[]> {
    const pool = await getDbConnection()
    const result = await pool.request().query(`
      SELECT
        id,
        name,
        version,
        tmLink,          -- Ensure this matches exactly with the database column
        jiraLink,
        createdAt,
        updatedAt
      FROM ${this.tableName}
    `)

    const tms = result.recordset.map(this.mapToTM)

    // Initialize empty IRTs array for each TM
    for (const tm of tms) {
      tm.irts = []
    }

    return tms
  }

  static async findById(id: string): Promise<TM | null> {
    const pool = await getDbConnection()
    const result = await pool
        .request()
        .input("id", sql.UniqueIdentifier, id)
        .query(`
          SELECT
            id,
            name,
            version,
            tmLink,
            jiraLink,
            createdAt,
            updatedAt
          FROM ${this.tableName}
          WHERE id = @id
        `)

    if (result.recordset.length === 0) {
      return null
    }

    const tm = this.mapToTM(result.recordset[0])

    // Initialize empty IRTs array
    tm.irts = []

    return tm
  }

  static async create(tm: CreateTMDto): Promise<TM> {
    const pool = await getDbConnection()
    const id = crypto.randomUUID()
    const now = new Date()

    const result = await pool
        .request()
        .input("id", sql.UniqueIdentifier, id)
        .input("name", sql.NVarChar, tm.name)
        .input("version", sql.NVarChar, tm.version)
        .input("tmLink", sql.NVarChar, tm.tmLink)
        .input("jiraLink", sql.NVarChar, tm.jiraLink)
        .input("createdAt", sql.DateTime, now)
        .input("updatedAt", sql.DateTime, now)
        .query(`
          INSERT INTO ${this.tableName} (
            id,
            name,
            version,
            tmLink,
            jiraLink,
            createdAt,
            updatedAt
          )
          VALUES (
                   @id,
                   @name,
                   @version,
                   @tmLink,
                   @jiraLink,
                   @createdAt,
                   @updatedAt
                 );

          SELECT
            id,
            name,
            version,
            tmLink,
            jiraLink,
            createdAt,
            updatedAt
          FROM ${this.tableName}
          WHERE id = @id
        `)

    return this.mapToTM(result.recordset[0])
  }

  static async update(id: string, tm: Partial<CreateTMDto>): Promise<TM | null> {
    const pool = await getDbConnection()
    const now = new Date()

    // Build dynamic update query based on provided fields
    const updateFields: string[] = []
    const request = pool.request().input("id", sql.UniqueIdentifier, id)

    if (tm.name !== undefined) {
      updateFields.push("name = @name")
      request.input("name", sql.NVarChar, tm.name)
    }

    if (tm.version !== undefined) {
      updateFields.push("version = @version")
      request.input("version", sql.NVarChar, tm.version)
    }

    if (tm.tmLink !== undefined) {
      updateFields.push("tmLink = @tmLink")
      request.input("tmLink", sql.NVarChar, tm.tmLink)
    }

    if (tm.jiraLink !== undefined) {
      updateFields.push("jiraLink = @jiraLink")
      request.input("jiraLink", sql.NVarChar, tm.jiraLink)
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
        name,
        version,
        tmLink,
        jiraLink,
        createdAt,
        updatedAt
      FROM ${this.tableName}
      WHERE id = @id
    `)

    if (result.recordset.length === 0) {
      return null
    }

    const updatedTM = this.mapToTM(result.recordset[0])

    // Initialize empty IRTs array
    updatedTM.irts = []

    return updatedTM
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

  private static mapToTM(record: any): TM {
    return {
      id: record.id,
      name: record.name,
      version: record.version,
      tmLink: record.tmLink,
      jiraLink: record.jiraLink,
      createdAt: new Date(record.createdAt),
      updatedAt: new Date(record.updatedAt),
      irts: [], // Will be populated later
    }
  }
}
