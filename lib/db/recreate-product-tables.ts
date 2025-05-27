import { getDbConnection } from "./index"
import fs from "fs"
import path from "path"

export async function recreateProductTables() {
  try {
    const pool = await getDbConnection()

    // First, drop the tables if they exist (in reverse order of dependencies)
    console.log("Dropping existing product tables...")

    // Drop ExternalModules first as it depends on IRTs and ExternalModuleTypes
    await pool.request().query(`
      IF OBJECT_ID('ExternalModules', 'U') IS NOT NULL
          DROP TABLE ExternalModules
    `)

    // Drop IRTs next as it depends on TMs and ECOAs
    await pool.request().query(`
      IF OBJECT_ID('IRTs', 'U') IS NOT NULL
          DROP TABLE IRTs
    `)

    // Drop the independent tables
    await pool.request().query(`
      IF OBJECT_ID('TMs', 'U') IS NOT NULL
          DROP TABLE TMs;
          
      IF OBJECT_ID('ECOAs', 'U') IS NOT NULL
          DROP TABLE ECOAs;
          
      IF OBJECT_ID('ExternalModuleTypes', 'U') IS NOT NULL
          DROP TABLE ExternalModuleTypes;
    `)

    console.log("Tables dropped successfully")

    // Now recreate the tables using the script
    console.log("Recreating product tables...")
    const scriptPath = path.join(process.cwd(), "lib/db/scripts/create-product-tables.sql")
    const script = fs.readFileSync(scriptPath, "utf8")
    await pool.request().query(script)

    console.log("Product tables recreated successfully")

    return { success: true, message: "Product tables recreated successfully" }
  } catch (error) {
    console.error("Error recreating product tables:", error)
    return {
      success: false,
      message: `Failed to recreate product tables: ${error instanceof Error ? error.message : String(error)}`,
    }
  }
}
