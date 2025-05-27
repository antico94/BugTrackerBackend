import { IRTModel } from "../models/irt"
import type { IRT, CreateIRTDto } from "@/types/product-types"

export class IRTRepository {
  static async findAll(): Promise<IRT[]> {
    return IRTModel.findAll()
  }

  static async findById(id: string): Promise<IRT | null> {
    return IRTModel.findById(id)
  }

  static async findByTMId(tmId: string): Promise<IRT[]> {
    return IRTModel.findByTMId(tmId)
  }

  static async create(data: CreateIRTDto): Promise<IRT> {
    return IRTModel.create(data)
  }

  static async update(id: string, data: Partial<CreateIRTDto>): Promise<IRT | null> {
    return IRTModel.update(id, data)
  }

  static async delete(id: string): Promise<boolean> {
    return IRTModel.delete(id)
  }
}
