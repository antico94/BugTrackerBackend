import sql from "mssql"
import { getDbConnection } from "../index"
import type { IRT, CreateIRTDto } from "@/types/product-types"
import { ExternalModuleModel } from "./external-module"
import { ECOAModel } from "./ecoa"

export class IRTModel {
  private static readonly tableName = "IRTs" // Changed from "IRT" to "IRTs"

  static async findAll(): Promise<IRT[]> {
    const pool = await getDbConnection()
    const result = await pool.request().query(`
      SELECT
        id,
        name,
        protocol,
        version,
        isOpenLabel,
        irtLink,
        jiraLink,
        tmId,
        ecoaId,
        createdAt,
        updatedAt
      FROM ${this.tableName}
    `)

    const irts = result.recordset.map(this.mapToIRT)

    // Load related entities for each IRT
    for (const irt of irts) {
      irt.externalModules = await ExternalModuleModel.findByIRTId(irt.id)

      if (irt.ecoaId) {
        irt.ecoa = await ECOAModel.findById(irt.ecoaId)
      }
    }

    return irts
  }

  static async findById(id: string): Promise<IRT | null> {
    const pool = await getDbConnection()
    const result = await pool
        .request()
        .input("id", sql.UniqueIdentifier, id)
        .query(`
          SELECT
            id,
            name,
            protocol,
            version,
            isOpenLabel,
            irtLink,
            jiraLink,
            tmId,
            ecoaId,
            createdAt,
            updatedAt
          FROM ${this.tableName}
          WHERE id = @id
        `)

    if (result.recordset.length === 0) {
      return null
    }

    const irt = this.mapToIRT(result.recordset[0])

    // Load related entities
    irt.externalModules = await ExternalModuleModel.findByIRTId(irt.id)

    if (irt.ecoaId) {
      irt.ecoa = await ECOAModel.findById(irt.ecoaId)
    }

    return irt
  }

  static async findByTMId(tmId: string): Promise<IRT[]> {
    const pool = await getDbConnection()
    const result = await pool
        .request()
        .input("tmId", sql.UniqueIdentifier, tmId)
        .query(`
        SELECT 
          id,
          name,
          protocol,
          version,
          isOpenLabel,
          irtLink,
          jiraLink,
          tmId,
          ecoaId,
          createdAt,
          updatedAt
        FROM ${this.tableName}
        WHERE tmId = @tmId
      `)

    const irts = result.recordset.map(this.mapToIRT)

    // Load related entities for each IRT
    for (const irt of irts) {
      irt.externalModules = await ExternalModuleModel.findByIRTId(irt.id)

      if (irt.ecoaId) {
        irt.ecoa = await ECOAModel.findById(irt.ecoaId)
      }
    }

    return irts
  }

  static async create(irt: CreateIRTDto): Promise<IRT> {
    const pool = await getDbConnection()
    const id = crypto.randomUUID()
    const now = new Date()

    const request = pool
        .request()
        .input("id", sql.UniqueIdentifier, id)
        .input("name", sql.NVarChar, irt.name)
        .input("protocol", sql.NVarChar, irt.protocol)
        .input("version", sql.NVarChar, irt.version)
        .input("isOpenLabel", sql.Bit, irt.isOpenLabel ? 1 : 0)
        .input("irtLink", sql.NVarChar, irt.irtLink)
        .input("jiraLink", sql.NVarChar, irt.jiraLink)
        .input("tmId", sql.UniqueIdentifier, irt.tmId)
        .input("createdAt", sql.DateTime, now)
        .input("updatedAt", sql.DateTime, now)

    // Only add ecoaId parameter if it exists
    if (irt.ecoaId) {
      request.input("ecoaId", sql.UniqueIdentifier, irt.ecoaId)
    }

    const result = await request.query(`
      INSERT INTO ${this.tableName} (
        id,
        name,
        protocol,
        version,
        isOpenLabel,
        irtLink,
        jiraLink,
        tmId,
        ${irt.ecoaId ? "ecoaId," : ""}
        createdAt,
        updatedAt
      )
      VALUES (
        @id,
        @name,
        @protocol,
        @version,
        @isOpenLabel,
        @irtLink,
        @jiraLink,
        @tmId,
        ${irt.ecoaId ? "@ecoaId," : ""}
        @createdAt,
        @updatedAt
      );
      
      SELECT 
        id,
        name,
        protocol,
        version,
        isOpenLabel,
        irtLink,
        jiraLink,
        tmId,
        ecoaId,
        createdAt,
        updatedAt
      FROM ${this.tableName}
      WHERE id = @id
    `)

    return this.mapToIRT(result.recordset[0])
  }

  static async update(id: string, irt: Partial<CreateIRTDto>): Promise<IRT | null> {
    const pool = await getDbConnection()
    const now = new Date()

    // Build dynamic update query based on provided fields
    const updateFields: string[] = []
    const request = pool.request().input("id", sql.UniqueIdentifier, id)

    if (irt.name !== undefined) {
      updateFields.push("name = @name")
      request.input("name", sql.NVarChar, irt.name)
    }

    if (irt.protocol !== undefined) {
      updateFields.push("protocol = @protocol")
      request.input("protocol", sql.NVarChar, irt.protocol)
    }

    if (irt.version !== undefined) {
      updateFields.push("version = @version")
      request.input("version", sql.NVarChar, irt.version)
    }

    if (irt.isOpenLabel !== undefined) {
      updateFields.push("isOpenLabel = @isOpenLabel")
      request.input("isOpenLabel", sql.Bit, irt.isOpenLabel ? 1 : 0)
    }

    if (irt.irtLink !== undefined) {
      updateFields.push("irtLink = @irtLink")
      request.input("irtLink", sql.NVarChar, irt.irtLink)
    }

    if (irt.jiraLink !== undefined) {
      updateFields.push("jiraLink = @jiraLink")
      request.input("jiraLink", sql.NVarChar, irt.jiraLink)
    }

    if (irt.tmId !== undefined) {
      updateFields.push("tmId = @tmId")
      request.input("tmId", sql.UniqueIdentifier, irt.tmId)
    }

    if (irt.ecoaId !== undefined) {
      if (irt.ecoaId === null) {
        updateFields.push("ecoaId = NULL")
      } else {
        updateFields.push("ecoaId = @ecoaId")
        request.input("ecoaId", sql.UniqueIdentifier, irt.ecoaId)
      }
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
        protocol,
        version,
        isOpenLabel,
        irtLink,
        jiraLink,
        tmId,
        ecoaId,
        createdAt,
        updatedAt
      FROM ${this.tableName}
      WHERE id = @id
    `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToIRT(result.recordset[0])
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

  private static mapToIRT(record: any): IRT {
    return {
      id: record.id,
      name: record.name,
      protocol: record.protocol,
      version: record.version,
      isOpenLabel: Boolean(record.isOpenLabel),
      irtLink: record.irtLink,
      jiraLink: record.jiraLink,
      tmId: record.tmId,
      ecoaId: record.ecoaId,
      createdAt: new Date(record.createdAt),
      updatedAt: new Date(record.updatedAt),
      externalModules: [], // Will be populated later
    }
  }
}
