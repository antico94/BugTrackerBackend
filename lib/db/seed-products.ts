import fs from "fs"
import path from "path"
import { getDbConnection } from "./index"

export async function seedProductsData(): Promise<void> {
  try {
    const pool = await getDbConnection()

    // Read and execute the sample products data SQL script
    const scriptPath = path.join(process.cwd(), "lib/db/scripts/insert-sample-products.sql")
    const script = fs.readFileSync(scriptPath, "utf8")
    await pool.request().query(script)

    console.log("Sample products data inserted successfully")
  } catch (error) {
    if (error instanceof Error) {
      console.error(`Failed to insert sample products data: ${error.message}`)
      throw error
    }
    throw new Error("Failed to insert sample products data")
  }
}
