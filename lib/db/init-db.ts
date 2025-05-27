import { sql } from "@vercel/postgres"
import fs from "fs"
import path from "path"

export async function initializeDatabase() {
  try {
    // Drop existing tables (optional, for development/testing)
    // console.log("Dropping existing tables...")
    // await sql`DROP TABLE IF EXISTS Notes;`
    // await sql`DROP TABLE IF EXISTS DMTs;`

    // Create Notes table
    console.log("Creating Notes table...")
    await sql`
      CREATE TABLE IF NOT EXISTS Notes (
        id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
        title VARCHAR(255) NOT NULL,
        content TEXT,
        created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
        updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
      );
    `

    // Create DMTs table
    console.log("Creating DMTs table...")
    const pool = await sql.connect()
    await pool.query(fs.readFileSync(path.join(__dirname, "scripts", "create-dmt-table.sql"), "utf8"))

    console.log("Database initialized successfully!")
  } catch (error) {
    console.error("Error initializing database:", error)
  }
}
