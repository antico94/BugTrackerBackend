import sql from "mssql"
import { getDbConnection } from "../index"
import type { Note, CreateNoteDto, UpdateNoteDto, EntityType } from "@/types/note"

export class NoteModel {
  private static readonly tableName = "Notes"

  static async findAll(): Promise<Note[]> {
    const pool = await getDbConnection()
    const result = await pool.request().query(`
      SELECT 
        id,
        content,
        entityType,
        entityId,
        createdAt,
        updatedAt,
        isDeleted,
        deletedAt
      FROM ${this.tableName}
      WHERE isDeleted = 0
      ORDER BY createdAt DESC
    `)

    return result.recordset.map(this.mapToNote)
  }

  static async findByEntity(entityType: EntityType, entityId: string): Promise<Note[]> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("entityType", sql.NVarChar, entityType)
      .input("entityId", sql.UniqueIdentifier, entityId)
      .query(`
        SELECT 
          id,
          content,
          entityType,
          entityId,
          createdAt,
          updatedAt,
          isDeleted,
          deletedAt
        FROM ${this.tableName}
        WHERE entityType = @entityType 
          AND entityId = @entityId 
          AND isDeleted = 0
        ORDER BY createdAt DESC
      `)

    return result.recordset.map(this.mapToNote)
  }

  static async findById(id: string): Promise<Note | null> {
    const pool = await getDbConnection()
    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .query(`
        SELECT 
          id,
          content,
          entityType,
          entityId,
          createdAt,
          updatedAt,
          isDeleted,
          deletedAt
        FROM ${this.tableName}
        WHERE id = @id AND isDeleted = 0
      `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToNote(result.recordset[0])
  }

  static async create(note: CreateNoteDto): Promise<Note> {
    const pool = await getDbConnection()
    const id = crypto.randomUUID()
    const now = new Date()

    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .input("content", sql.NVarChar, note.content)
      .input("entityType", sql.NVarChar, note.entityType)
      .input("entityId", sql.UniqueIdentifier, note.entityId)
      .input("createdAt", sql.DateTime, now)
      .query(`
        INSERT INTO ${this.tableName} (
          id,
          content,
          entityType,
          entityId,
          createdAt
        )
        VALUES (
          @id,
          @content,
          @entityType,
          @entityId,
          @createdAt
        );
        
        SELECT 
          id,
          content,
          entityType,
          entityId,
          createdAt,
          updatedAt,
          isDeleted,
          deletedAt
        FROM ${this.tableName}
        WHERE id = @id
      `)

    return this.mapToNote(result.recordset[0])
  }

  static async update(id: string, note: UpdateNoteDto): Promise<Note | null> {
    const pool = await getDbConnection()
    const now = new Date()

    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .input("content", sql.NVarChar, note.content)
      .input("updatedAt", sql.DateTime, now)
      .query(`
        UPDATE ${this.tableName}
        SET 
          content = @content,
          updatedAt = @updatedAt
        WHERE id = @id AND isDeleted = 0;
        
        SELECT 
          id,
          content,
          entityType,
          entityId,
          createdAt,
          updatedAt,
          isDeleted,
          deletedAt
        FROM ${this.tableName}
        WHERE id = @id
      `)

    if (result.recordset.length === 0) {
      return null
    }

    return this.mapToNote(result.recordset[0])
  }

  static async softDelete(id: string): Promise<boolean> {
    const pool = await getDbConnection()
    const now = new Date()

    const result = await pool
      .request()
      .input("id", sql.UniqueIdentifier, id)
      .input("deletedAt", sql.DateTime, now)
      .query(`
        UPDATE ${this.tableName}
        SET 
          isDeleted = 1,
          deletedAt = @deletedAt
        WHERE id = @id AND isDeleted = 0
      `)

    return result.rowsAffected[0] > 0
  }

  static async hardDelete(id: string): Promise<boolean> {
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

  private static mapToNote(record: any): Note {
    return {
      id: record.id,
      content: record.content,
      entityType: record.entityType,
      entityId: record.entityId,
      createdAt: new Date(record.createdAt),
      updatedAt: record.updatedAt ? new Date(record.updatedAt) : undefined,
      isDeleted: Boolean(record.isDeleted),
      deletedAt: record.deletedAt ? new Date(record.deletedAt) : undefined,
    }
  }
}
