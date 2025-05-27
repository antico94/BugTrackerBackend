import { ExternalModuleModel } from "../models/external-module"
import type { ExternalModule, CreateExternalModuleDto } from "@/types/product-types"

export class ExternalModuleRepository {
  static async findAll(): Promise<ExternalModule[]> {
    return ExternalModuleModel.findAll()
  }

  static async findById(id: string): Promise<ExternalModule | null> {
    return ExternalModuleModel.findById(id)
  }

  static async findByIRTId(irtId: string): Promise<ExternalModule[]> {
    return ExternalModuleModel.findByIRTId(irtId)
  }

  static async create(data: CreateExternalModuleDto): Promise<ExternalModule> {
    return ExternalModuleModel.create(data)
  }

  static async update(id: string, data: Partial<CreateExternalModuleDto>): Promise<ExternalModule | null> {
    return ExternalModuleModel.update(id, data)
  }

  static async delete(id: string): Promise<boolean> {
    return ExternalModuleModel.delete(id)
  }
}
