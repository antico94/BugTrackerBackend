/**
 * Encodes an ID to a URL-safe string
 * @param id The ID to encode
 * @returns The encoded ID
 */
export function encodeId(id: string): string {
  return Buffer.from(id).toString("base64").replace(/\+/g, "-").replace(/\//g, "_").replace(/=/g, "")
}

/**
 * Decodes an encoded ID back to its original form
 * @param encodedId The encoded ID
 * @returns The decoded ID
 */
export function decodeId(encodedId: string): string {
  // Replace URL-safe characters back to Base64 characters
  const base64 = encodedId.replace(/-/g, "+").replace(/_/g, "/")

  // Add padding if needed
  const padding = base64.length % 4
  const paddedBase64 = padding ? base64 + "=".repeat(4 - padding) : base64

  return Buffer.from(paddedBase64, "base64").toString()
}
