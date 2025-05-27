import sql from "mssql"
import { getAppConfig } from "../config/app-config"

let pool: sql.ConnectionPool | null = null

export async function getDbConnection(): Promise<sql.ConnectionPool> {
  if (pool) {
    return pool
  }

  try {
    const config = getAppConfig()
    pool = await sql.connect(config.database)
    return pool
  } catch (error) {
    if (error instanceof Error) {
      throw new Error(`Database connection error: ${error.message}`)
    }
    throw new Error("Database connection error")
  }
}

export async function closeDbConnection(): Promise<void> {
  if (pool) {
    await pool.close()
    pool = null
  }
}
