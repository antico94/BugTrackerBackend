import { NoteModel } from "../models/note"
import type { Note, CreateNoteDto, UpdateNoteDto, EntityType } from "@/types/note"

export class NoteRepository {
  static async findAll(): Promise<Note[]> {
    return NoteModel.findAll()
  }

  static async findByEntity(entityType: EntityType, entityId: string): Promise<Note[]> {
    return NoteModel.findByEntity(entityType, entityId)
  }

  static async findById(id: string): Promise<Note | null> {
    return NoteModel.findById(id)
  }

  static async create(note: CreateNoteDto): Promise<Note> {
    return NoteModel.create(note)
  }

  static async update(id: string, note: UpdateNoteDto): Promise<Note | null> {
    return NoteModel.update(id, note)
  }

  static async softDelete(id: string): Promise<boolean> {
    return NoteModel.softDelete(id)
  }

  static async hardDelete(id: string): Promise<boolean> {
    return NoteModel.hardDelete(id)
  }

  static async delete(id: string): Promise<boolean> {
    return NoteModel.hardDelete(id)
  }
}
