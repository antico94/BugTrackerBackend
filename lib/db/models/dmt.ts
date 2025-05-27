import sql from "mssql"
import { getDbConnection } from "../index"
import type { DMT, CreateDMTDto, UpdateDMTDto, DMTBugRelation } from "@/types/dmt"

export class DMTModel {
  private static readonly tableName = "DMTs"
  private static readonly relationTableName = "DMT_Bugs"

  static async findAll(): Promise<DMT[]> {
    const pool = await getDbConnection()
    const result = await pool.request().query(`
      SELECT 
        id,
        name,
        description,
        dateCreated,
        isCompleted,
        completedAt,
        createdBy,
        updatedAt
      FROM ${this.tableName}
      ORDER BY dateCreated DESC
    `)

    const dmts = result.recordset.map(this.mapToDMT)

    // For each DMT, fetch the related bugs
    for (const dmt of dmts) {
      const bugsResult = await pool
        .request()
        .input("dmtId", sql.UniqueIdentifier, dmt.id)
        .query(`
          SELECT bugId
          FROM ${this.relationTableName}
          WHERE dmtId = @dmtId
        `)

      dmt.bugs = bugsResult.recordset.map((row) => row.bugId)
    }

    return dmts
  }

  static async findById(id: string): Promise<DMT | null> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .query(`
        SELECT 
          id,
          name,
          description,
          dateCreated,
          isCompleted,
          completedAt,
          createdBy,
          updatedAt
        FROM ${this.tableName}
        WHERE id = @id
      `)

    if (result.recordset.length === 0) {
      return null
    }

    const dmt = this.mapToDMT(result.recordset[0])

    // Fetch related bugs
    const bugsResult = await pool
      .request()
      .input("dmtId", sql.UniqueIdentifier, id)
      .query(`
        SELECT bugId
        FROM ${this.relationTableName}
        WHERE dmtId = @dmtId
      `)

    dmt.bugs = bugsResult.recordset.map((row) => row.bugId)

    return dmt
  }

  static async create(dmt: CreateDMTDto): Promise<DMT> {
    const pool = await getDbConnection()
    const id = dmt.id || crypto.randomUUID()
    const now = new Date()

    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .input("name", sql.NVarChar, dmt.name)
      .input("description", sql.NVarChar, dmt.description || null)
      .input("dateCreated", sql.DateTime, now)
      .input("isCompleted", sql.Bit, dmt.isCompleted || false)
      .input("completedAt", sql.DateTime, dmt.completedAt || null)
      .input("createdBy", sql.NVarChar, dmt.createdBy || null)
      .input("updatedAt", sql.DateTime, null)
      .query(`
        INSERT INTO ${this.tableName} (
          id,
          name,
          description,
          dateCreated,
          isCompleted,
          completedAt,
          createdBy,
          updatedAt
        )
        VALUES (
          @id,
          @name,
          @description,
          @dateCreated,
          @isCompleted,
          @completedAt,
          @createdBy,
          @updatedAt
        );
        
        SELECT 
          id,
          name,
          description,
          dateCreated,
          isCompleted,
          completedAt,
          createdBy,
          updatedAt
        FROM ${this.tableName}
        WHERE id = @id
      `)

    return this.mapToDMT(result.recordset[0])
  }

  static async update(id: string, dmt: UpdateDMTDto): Promise<DMT | null> {
    const pool = await getDbConnection()
    const now = new Date()

    // Build dynamic update query based on provided fields
    const updateFields: string[] = []
    const request = pool.request().input("id", sql.UniqueIdentifier, id)

    if (dmt.name !== undefined) {
      updateFields.push("name = @name")
      request.input("name", sql.NVarChar, dmt.name)
    }

    if (dmt.description !== undefined) {
      updateFields.push("description = @description")
      request.input("description", sql.NVarChar, dmt.description)
    }

    if (dmt.isCompleted !== undefined) {
      updateFields.push("isCompleted = @isCompleted")
      request.input("isCompleted", sql.Bit, dmt.isCompleted)
    }

    if (dmt.completedAt !== undefined) {
      updateFields.push("completedAt = @completedAt")
      request.input("completedAt", sql.DateTime, dmt.completedAt)
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
        description,
        dateCreated,
        isCompleted,
        completedAt,
        createdBy,
        updatedAt
      FROM ${this.tableName}
      WHERE id = @id
    `)

    if (result.recordset.length === 0) {
      return null
    }

    const updatedDmt = this.mapToDMT(result.recordset[0])

    // Fetch related bugs
    const bugsResult = await pool
      .request()
      .input("dmtId", sql.UniqueIdentifier, id)
      .query(`
        SELECT bugId
        FROM ${this.relationTableName}
        WHERE dmtId = @dmtId
      `)

    updatedDmt.bugs = bugsResult.recordset.map((row) => row.bugId)

    return updatedDmt
  }

  static async delete(id: string): Promise<boolean> {
    const pool = await getDbConnection()

    // First delete all relations
    await pool
      .request()
      .input("dmtId", sql.UniqueIdentifier, id)
      .query(`
        DELETE FROM ${this.relationTableName}
        WHERE dmtId = @dmtId
      `)

    // Then delete the DMT
    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .query(`
        DELETE FROM ${this.tableName}
        WHERE id = @id
      `)

    return result.rowsAffected[0] > 0
  }

  static async addBugToDMT(dmtId: string, bugId: string): Promise<DMTBugRelation> {
    const pool = await getDbConnection()
    const now = new Date()

    // Check if relation already exists
    const checkResult = await pool
      .request()
      .input("dmtId", sql.UniqueIdentifier, dmtId)
      .input("bugId", sql.UniqueIdentifier, bugId)
      .query(`
        SELECT COUNT(*) as count
        FROM ${this.relationTableName}
        WHERE dmtId = @dmtId AND bugId = @bugId
      `)

    if (checkResult.recordset[0].count > 0) {
      // Relation already exists, return it
      const existingResult = await pool
        .request()
        .input("dmtId", sql.UniqueIdentifier, dmtId)
        .input("bugId", sql.UniqueIdentifier, bugId)
        .query(`
          SELECT dmtId, bugId, dateAdded
          FROM ${this.relationTableName}
          WHERE dmtId = @dmtId AND bugId = @bugId
        `)

      return {
        dmtId: existingResult.recordset[0].dmtId,
        bugId: existingResult.recordset[0].bugId,
        dateAdded: new Date(existingResult.recordset[0].dateAdded),
      }
    }

    // Create new relation
    const result = await pool
      .request()
      .input("dmtId", sql.UniqueIdentifier, dmtId)
      .input("bugId", sql.UniqueIdentifier, bugId)
      .input("dateAdded", sql.DateTime, now)
      .query(`
        INSERT INTO ${this.relationTableName} (
          dmtId,
          bugId,
          dateAdded
        )
        VALUES (
          @dmtId,
          @bugId,
          @dateAdded
        );
        
        SELECT dmtId, bugId, dateAdded
        FROM ${this.relationTableName}
        WHERE dmtId = @dmtId AND bugId = @bugId
      `)

    return {
      dmtId: result.recordset[0].dmtId,
      bugId: result.recordset[0].bugId,
      dateAdded: new Date(result.recordset[0].dateAdded),
    }
  }

  static async removeBugFromDMT(dmtId: string, bugId: string): Promise<boolean> {
    const pool = await getDbConnection()

    const result = await pool
      .request()
      .input("dmtId", sql.UniqueIdentifier, dmtId)
      .input("bugId", sql.UniqueIdentifier, bugId)
      .query(`
        DELETE FROM ${this.relationTableName}
        WHERE dmtId = @dmtId AND bugId = @bugId
      `)

    return result.rowsAffected[0] > 0
  }

  static async getBugsForDMT(dmtId: string): Promise<string[]> {
    const pool = await getDbConnection()

    const result = await pool
      .request()
      .input("dmtId", sql.UniqueIdentifier, dmtId)
      .query(`
        SELECT bugId
        FROM ${this.relationTableName}
        WHERE dmtId = @dmtId
      `)

    return result.recordset.map((row) => row.bugId)
  }

  static async getDMTsForBug(bugId: string): Promise<DMT[]> {
    const pool = await getDbConnection()

    const result = await pool
      .request()
      .input("bugId", sql.UniqueIdentifier, bugId)
      .query(`
        SELECT d.*
        FROM ${this.tableName} d
        INNER JOIN ${this.relationTableName} db ON d.id = db.dmtId
        WHERE db.bugId = @bugId
      `)

    return result.recordset.map(this.mapToDMT)
  }

  private static mapToDMT(record: any): DMT {
    return {
      id: record.id,
      name: record.name,
      description: record.description,
      dateCreated: new Date(record.dateCreated),
      isCompleted: record.isCompleted,
      completedAt: record.completedAt ? new Date(record.completedAt) : null,
      createdBy: record.createdBy,
      updatedAt: record.updatedAt ? new Date(record.updatedAt) : null,
      bugs: [], // Will be populated separately
    }
  }
}
