// Health check API endpoint for Docker containers
import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  try {
    // Perform basic health checks
    const timestamp = new Date().toISOString();

    // You can add additional health checks here:
    // - Database connectivity
    // - External API availability
    // - Memory usage checks
    // - etc.

    const healthStatus = {
      status: 'healthy',
      timestamp,
      environment: process.env.NODE_ENV,
      uptime: process.uptime(),
      memory: process.memoryUsage(),
      version: process.env.npm_package_version || '0.1.0',
    };

    return NextResponse.json(healthStatus, { status: 200 });
  } catch (error) {
    const errorStatus = {
      status: 'unhealthy',
      timestamp: new Date().toISOString(),
      error: error instanceof Error ? error.message : 'Unknown error',
    };

    return NextResponse.json(errorStatus, { status: 503 });
  }
}
