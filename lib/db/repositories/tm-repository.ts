import { TMModel } from "../models/tm"
import type { TM, CreateTMDto } from "@/types/product-types"

export class TMRepository {
  static async findAll(): Promise<TM[]> {
    return TMModel.findAll()
  }

  static async findById(id: string): Promise<TM | null> {
    return TMModel.findById(id)
  }

  static async create(data: CreateTMDto): Promise<TM> {
    return TMModel.create(data)
  }

  static async update(id: string, data: Partial<CreateTMDto>): Promise<TM | null> {
    return TMModel.update(id, data)
  }

  static async delete(id: string): Promise<boolean> {
    return TMModel.delete(id)
  }
}
