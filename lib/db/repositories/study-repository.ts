import { StudyModel } from "../models/study"
import type { Study, CreateStudyDto, UpdateStudyDto } from "@/types/study"

export class StudyRepository {
  static async findAll(): Promise<Study[]> {
    return StudyModel.findAll()
  }

  static async findById(id: string): Promise<Study | null> {
    return StudyModel.findById(id)
  }

  static async create(data: CreateStudyDto): Promise<Study> {
    return StudyModel.create(data)
  }

  static async update(id: string, data: UpdateStudyDto): Promise<Study | null> {
    return StudyModel.update(id, data)
  }

  static async delete(id: string): Promise<boolean> {
    return StudyModel.delete(id)
  }
}
