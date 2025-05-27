import sql from "mssql"
import { getDbConnection } from "../index"
import { ExternalModuleType, type ExternalModuleTypeEntity } from "@/types/product-types"

export class ExternalModuleTypeModel {
  private static readonly tableName = "ExternalModuleTypes"

  static async findAll(): Promise<ExternalModuleTypeEntity[]> {
    const pool = await getDbConnection()
    const result = await pool.request().query(`
      SELECT 
        id,
        name,
        createdAt,
        updatedAt
      FROM ${this.tableName}
    `)

    return result.recordset.map(this.mapToExternalModuleType)
  }

  static async findById(id: string): Promise<ExternalModuleTypeEntity | null> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .query(`
        SELECT 
          id,
          name,
          createdAt,
          updatedAt
        FROM ${this.tableName}
        WHERE id = @id
      `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToExternalModuleType(result.recordset[0])
  }

  static async findByName(name: ExternalModuleType): Promise<ExternalModuleTypeEntity | null> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("name", sql.NVarChar, name)
      .query(`
        SELECT 
          id,
          name,
          createdAt,
          updatedAt
        FROM ${this.tableName}
        WHERE name = @name
      `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToExternalModuleType(result.recordset[0])
  }

  static async seedTypes(): Promise<void> {
    const pool = await getDbConnection()
    const now = new Date()

    // Check if types already exist
    const existingTypes = await this.findAll()
    if (existingTypes.length > 0) {
      return // Types already seeded
    }

    // Insert predefined types
    for (const typeName of Object.values(ExternalModuleType)) {
      const id = crypto.randomUUID()
      await pool
        .request()
        .input("id", sql.UniqueIdentifier, id)
        .input("name", sql.NVarChar, typeName)
        .input("createdAt", sql.DateTime, now)
        .input("updatedAt", sql.DateTime, now)
        .query(`
          INSERT INTO ${this.tableName} (
            id,
            name,
            createdAt,
            updatedAt
          )
          VALUES (
            @id,
            @name,
            @createdAt,
            @updatedAt
          )
        `)
    }
  }

  private static mapToExternalModuleType(record: any): ExternalModuleTypeEntity {
    return {
      id: record.id,
      name: record.name as ExternalModuleType,
      createdAt: new Date(record.createdAt),
      updatedAt: new Date(record.updatedAt),
    }
  }
}
