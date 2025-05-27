import { parseStringPromise } from "xml2js"

// Your required BugEntity interface
export interface BugEntity {
  id: string // JIRA issue ID
  key: string // JIRA issue key
  title: string // Issue title/summary
  description: string // Full description
  severity: string // Severity level from custom fields
  foundInBuild?: string // Build version where bug was found
  affectedVersions: string[] // List of affected versions
}

/**
 * Parses JIRA XML content and extracts bug entities with the specified fields
 * @param xmlContent The JIRA XML content as string
 * @returns Promise resolving to an array of BugEntity objects
 */
export async function extractBugsFromJiraXml(xmlContent: string): Promise<BugEntity[]> {
  try {
    // Parse XML with attributes and without arrays for single elements
    const parsed = await parseStringPromise(xmlContent, {
      explicitArray: false,
      mergeAttrs: false,
      attrkey: "@",
      charkey: "#",
      explicitCharkey: false,
    })

    // Handle both single item and multiple items
    const items = Array.isArray(parsed.rss.channel.item) ? parsed.rss.channel.item : [parsed.rss.channel.item]

    return items.map((item) => {
      // Extract affected versions (handling both single and multiple versions)
      let affectedVersions: string[] = []
      if (item.version) {
        affectedVersions = Array.isArray(item.version) ? item.version : [item.version]
      }

      // Find severity and foundInBuild custom fields
      let severity = "Unknown"
      let foundInBuild: string | undefined = undefined

      // Handle both array and single customfield
      const customfields = Array.isArray(item.customfields.customfield)
        ? item.customfields.customfield
        : [item.customfields.customfield]

      for (const field of customfields) {
        if (field.customfieldname === "Severity") {
          const cfValue = field.customfieldvalues.customfieldvalue
          severity = Array.isArray(cfValue) ? extractTextContent(cfValue[0]) : extractTextContent(cfValue)
        }

        if (field.customfieldname === "Found in Build") {
          const cfValue = field.customfieldvalues.customfieldvalue
          foundInBuild = Array.isArray(cfValue) ? extractTextContent(cfValue[0]) : extractTextContent(cfValue)
        }
      }

      // Create BugEntity with required fields
      return {
        id: item.key["@"].id,
        key: item.key["#"] || item.key,
        title: item.summary || item.title,
        description: item.description,
        severity,
        foundInBuild,
        affectedVersions,
      }
    })
  } catch (error) {
    console.error("Error parsing JIRA XML:", error)
    throw new Error(`Failed to parse JIRA XML: ${error instanceof Error ? error.message : String(error)}`)
  }
}

/**
 * Helper function to extract text content from a custom field value
 */
function extractTextContent(fieldValue: any): string {
  if (!fieldValue) return ""

  // Check for CDATA section content
  if (fieldValue["#cdata-section"]) {
    return fieldValue["#cdata-section"]
  }

  // Check for regular text content
  if (fieldValue["#"]) {
    return fieldValue["#"]
  }

  // If it's just a string
  if (typeof fieldValue === "string") {
    return fieldValue
  }

  // Default case
  return fieldValue.toString()
}
