import { DMTModel } from "../models/dmt"
import type { DMT, CreateDMTDto, UpdateDMTDto, DMTBugRelation } from "@/types/dmt"

export class DMTRepository {
  static async findAll(): Promise<DMT[]> {
    return DMTModel.findAll()
  }

  static async findById(id: string): Promise<DMT | null> {
    return DMTModel.findById(id)
  }

  static async create(dmt: CreateDMTDto): Promise<DMT> {
    return DMTModel.create(dmt)
  }

  static async update(id: string, dmt: UpdateDMTDto): Promise<DMT | null> {
    return DMTModel.update(id, dmt)
  }

  static async delete(id: string): Promise<boolean> {
    return DMTModel.delete(id)
  }

  static async addBugToDMT(dmtId: string, bugId: string): Promise<DMTBugRelation> {
    return DMTModel.addBugToDMT(dmtId, bugId)
  }

  static async removeBugFromDMT(dmtId: string, bugId: string): Promise<boolean> {
    return DMTModel.removeBugFromDMT(dmtId, bugId)
  }

  static async getBugsForDMT(dmtId: string): Promise<string[]> {
    return DMTModel.getBugsForDMT(dmtId)
  }

  static async getDMTsForBug(bugId: string): Promise<DMT[]> {
    return DMTModel.getDMTsForBug(bugId)
  }

  // Add this method that was missing
  static async findByBugId(bugId: string): Promise<DMT[]> {
    return DMTModel.getDMTsForBug(bugId)
  }
}
