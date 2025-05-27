import { ECOAModel } from "../models/ecoa"
import type { ECOA, CreateECOADto } from "@/types/product-types"

export class ECOARepository {
  static async findAll(): Promise<ECOA[]> {
    return ECOAModel.findAll()
  }

  static async findById(id: string): Promise<ECOA | null> {
    return ECOAModel.findById(id)
  }

  static async create(data: CreateECOADto): Promise<ECOA> {
    return ECOAModel.create(data)
  }

  static async update(id: string, data: Partial<CreateECOADto>): Promise<ECOA | null> {
    return ECOAModel.update(id, data)
  }

  static async delete(id: string): Promise<boolean> {
    return ECOAModel.delete(id)
  }
}
