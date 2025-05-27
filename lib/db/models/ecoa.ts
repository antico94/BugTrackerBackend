import sql from "mssql"
import { getDbConnection } from "../index"
import type { ECOA, CreateECOADto } from "@/types/product-types"

export class ECOAModel {
  private static readonly tableName = "ECOAs"

  static async findAll(): Promise<ECOA[]> {
    const pool = await getDbConnection()
    const result = await pool.request().query(`
      SELECT 
        id,
        name,
        version,
        createdAt,
        updatedAt
      FROM ${this.tableName}
    `)

    return result.recordset.map(this.mapToECOA)
  }

  static async findById(id: string): Promise<ECOA | null> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .query(`
        SELECT 
          id,
          name,
          version,
          createdAt,
          updatedAt
        FROM ${this.tableName}
        WHERE id = @id
      `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToECOA(result.recordset[0])
  }

  static async create(ecoa: CreateECOADto): Promise<ECOA> {
    const pool = await getDbConnection()
    const id = crypto.randomUUID()
    const now = new Date()

    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .input("name", sql.NVarChar, ecoa.name)
      .input("version", sql.NVarChar, ecoa.version)
      .input("createdAt", sql.DateTime, now)
      .input("updatedAt", sql.DateTime, now)
      .query(`
        INSERT INTO ${this.tableName} (
          id,
          name,
          version,
          createdAt,
          updatedAt
        )
        VALUES (
          @id,
          @name,
          @version,
          @createdAt,
          @updatedAt
        );
        
        SELECT 
          id,
          name,
          version,
          createdAt,
          updatedAt
        FROM ${this.tableName}
        WHERE id = @id
      `)

    return this.mapToECOA(result.recordset[0])
  }

  static async update(id: string, ecoa: Partial<CreateECOADto>): Promise<ECOA | null> {
    const pool = await getDbConnection()
    const now = new Date()

    // Build dynamic update query based on provided fields
    const updateFields: string[] = []
    const request = pool.request().input("id", sql.UniqueIdentifier, id)

    if (ecoa.name !== undefined) {
      updateFields.push("name = @name")
      request.input("name", sql.NVarChar, ecoa.name)
    }

    if (ecoa.version !== undefined) {
      updateFields.push("version = @version")
      request.input("version", sql.NVarChar, ecoa.version)
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
        createdAt,
        updatedAt
      FROM ${this.tableName}
      WHERE id = @id
    `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToECOA(result.recordset[0])
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

  private static mapToECOA(record: any): ECOA {
    return {
      id: record.id,
      name: record.name,
      version: record.version,
      createdAt: new Date(record.createdAt),
      updatedAt: new Date(record.updatedAt),
    }
  }
}
