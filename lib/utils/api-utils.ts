import { NextResponse } from "next/server"

/**
 * Generic handler for API requests with consistent error handling
 */
export async function handleApiRequest<T>(
  fetchFn: () => Promise<T>,
  errorMessage = "An error occurred",
  notFoundMessage = "Resource not found",
): Promise<NextResponse> {
  try {
    const result = await fetchFn()

    if (!result) {
      return NextResponse.json({ error: notFoundMessage }, { status: 404 })
    }

    return NextResponse.json(result)
  } catch (error) {
    console.error(`API Error: ${errorMessage}`, error)
    return NextResponse.json({ error: errorMessage }, { status: 500 })
  }
}
