import fs from "fs"
import path from "path"
import { getDbConnection } from "./index"
import { ExternalModuleTypeRepository } from "./repositories/external-module-type-repository"

export async function initializeProductTables() {
  try {
    const pool = await getDbConnection()

    // Read and execute the SQL script to create product tables
    const scriptPath = path.join(process.cwd(), "lib/db/scripts/create-product-tables.sql")
    if (fs.existsSync(scriptPath)) {
      const script = fs.readFileSync(scriptPath, "utf8")
      await pool.request().query(script)
      console.log("Product tables created successfully")
    } else {
      console.warn("Product tables SQL script not found")
    }

    // Seed the ExternalModuleTypes
    await ExternalModuleTypeRepository.seedTypes()
    console.log("External Module Types seeded successfully")

    console.log("Product tables initialized successfully")
  } catch (error) {
    console.error("Error initializing product tables:", error)
    throw error
  }
}
