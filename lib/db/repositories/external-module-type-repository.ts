import { ExternalModuleTypeModel } from "../models/external-module-type"
import type { ExternalModuleType, ExternalModuleTypeEntity } from "@/types/product-types"

export class ExternalModuleTypeRepository {
  static async findAll(): Promise<ExternalModuleTypeEntity[]> {
    return ExternalModuleTypeModel.findAll()
  }

  static async findById(id: string): Promise<ExternalModuleTypeEntity | null> {
    return ExternalModuleTypeModel.findById(id)
  }

  static async findByName(name: ExternalModuleType): Promise<ExternalModuleTypeEntity | null> {
    return ExternalModuleTypeModel.findByName(name)
  }

  static async seedTypes(): Promise<void> {
    return ExternalModuleTypeModel.seedTypes()
  }
}
