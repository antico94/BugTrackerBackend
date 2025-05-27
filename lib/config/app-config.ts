import fs from "fs"
import path from "path"

export interface DatabaseConfig {
  server: string
  database: string
  user: string
  password: string
  options?: {
    encrypt?: boolean
    trustServerCertificate?: boolean
  }
}

export interface AppConfig {
  database: DatabaseConfig
}

let cachedConfig: AppConfig | null = null

export function getAppConfig(): AppConfig {
  if (cachedConfig) {
    return cachedConfig
  }

  try {
    const configPath = path.join(process.cwd(), "AppConfig.json")
    const configFile = fs.readFileSync(configPath, "utf8")
    const config = JSON.parse(configFile) as AppConfig

    // Validate required fields
    if (!config.database) {
      throw new Error("Database configuration is missing")
    }

    if (!config.database.server || !config.database.database || !config.database.user || !config.database.password) {
      throw new Error("Required database configuration fields are missing")
    }

    cachedConfig = config
    return config
  } catch (error) {
    if (error instanceof Error) {
      throw new Error(`Failed to load configuration: ${error.message}`)
    }
    throw new Error("Failed to load configuration")
  }
}
