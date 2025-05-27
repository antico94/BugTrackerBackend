import sql from "mssql"
import { getDbConnection } from "../index"
import type { ExternalModule, CreateExternalModuleDto } from "@/types/product-types"
import { ExternalModuleTypeModel } from "./external-module-type"

export class ExternalModuleModel {
  private static readonly tableName = "ExternalModules"

  static async findAll(): Promise<ExternalModule[]> {
    const pool = await getDbConnection()
    const result = await pool.request().query(`
      SELECT 
        id,
        name,
        isCustom,
        version,
        typeId,
        irtId,
        createdAt,
        updatedAt
      FROM ${this.tableName}
    `)

    const modules = result.recordset.map(this.mapToExternalModule)

    // Load type for each module
    for (const module of modules) {
      module.type = await ExternalModuleTypeModel.findById(module.typeId)
    }

    return modules
  }

  static async findById(id: string): Promise<ExternalModule | null> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .query(`
        SELECT 
          id,
          name,
          isCustom,
          version,
          typeId,
          irtId,
          createdAt,
          updatedAt
        FROM ${this.tableName}
        WHERE id = @id
      `)

    if (result.recordset.length === 0) {
      return null
    }

    const module = this.mapToExternalModule(result.recordset[0])

    // Load type
    module.type = await ExternalModuleTypeModel.findById(module.typeId)

    return module
  }

  static async findByIRTId(irtId: string): Promise<ExternalModule[]> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("irtId", sql.UniqueIdentifier, irtId)
      .query(`
        SELECT 
          id,
          name,
          isCustom,
          version,
          typeId,
          irtId,
          createdAt,
          updatedAt
        FROM ${this.tableName}
        WHERE irtId = @irtId
      `)

    const modules = result.recordset.map(this.mapToExternalModule)

    // Load type for each module
    for (const module of modules) {
      module.type = await ExternalModuleTypeModel.findById(module.typeId)
    }

    return modules
  }

  static async create(module: CreateExternalModuleDto): Promise<ExternalModule> {
    const pool = await getDbConnection()
    const id = crypto.randomUUID()
    const now = new Date()

    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .input("name", sql.NVarChar, module.name)
      .input("isCustom", sql.Bit, module.isCustom ? 1 : 0)
      .input("version", sql.NVarChar, module.version)
      .input("typeId", sql.UniqueIdentifier, module.typeId)
      .input("irtId", sql.UniqueIdentifier, module.irtId)
      .input("createdAt", sql.DateTime, now)
      .input("updatedAt", sql.DateTime, now)
      .query(`
        INSERT INTO ${this.tableName} (
          id,
          name,
          isCustom,
          version,
          typeId,
          irtId,
          createdAt,
          updatedAt
        )
        VALUES (
          @id,
          @name,
          @isCustom,
          @version,
          @typeId,
          @irtId,
          @createdAt,
          @updatedAt
        );
        
        SELECT 
          id,
          name,
          isCustom,
          version,
          typeId,
          irtId,
          createdAt,
          updatedAt
        FROM ${this.tableName}
        WHERE id = @id
      `)

    const createdModule = this.mapToExternalModule(result.recordset[0])

    // Load type
    createdModule.type = await ExternalModuleTypeModel.findById(createdModule.typeId)

    return createdModule
  }

  static async update(id: string, module: Partial<CreateExternalModuleDto>): Promise<ExternalModule | null> {
    const pool = await getDbConnection()
    const now = new Date()

    // Build dynamic update query based on provided fields
    const updateFields: string[] = []
    const request = pool.request().input("id", sql.UniqueIdentifier, id)

    if (module.name !== undefined) {
      updateFields.push("name = @name")
      request.input("name", sql.NVarChar, module.name)
    }

    if (module.isCustom !== undefined) {
      updateFields.push("isCustom = @isCustom")
      request.input("isCustom", sql.Bit, module.isCustom ? 1 : 0)
    }

    if (module.version !== undefined) {
      updateFields.push("version = @version")
      request.input("version", sql.NVarChar, module.version)
    }

    if (module.typeId !== undefined) {
      updateFields.push("typeId = @typeId")
      request.input("typeId", sql.UniqueIdentifier, module.typeId)
    }

    if (module.irtId !== undefined) {
      updateFields.push("irtId = @irtId")
      request.input("irtId", sql.UniqueIdentifier, module.irtId)
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
        isCustom,
        version,
        typeId,
        irtId,
        createdAt,
        updatedAt
      FROM ${this.tableName}
      WHERE id = @id
    `)

    if (result.recordset.length === 0) {
      return null
    }

    const updatedModule = this.mapToExternalModule(result.recordset[0])

    // Load type
    updatedModule.type = await ExternalModuleTypeModel.findById(updatedModule.typeId)

    return updatedModule
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

  private static mapToExternalModule(record: any): ExternalModule {
    return {
      id: record.id,
      name: record.name,
      isCustom: Boolean(record.isCustom),
      version: record.version,
      typeId: record.typeId,
      irtId: record.irtId,
      createdAt: new Date(record.createdAt),
      updatedAt: new Date(record.updatedAt),
    }
  }
}
