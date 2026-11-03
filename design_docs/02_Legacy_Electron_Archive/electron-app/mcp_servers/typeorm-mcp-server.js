// typeorm-mcp-server.js
// Custom MCP Server for TypeORM Entity and Migration Management

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { DataSource } from "typeorm";
import fs from "fs/promises";
import path from "path";

// Initialize TypeORM DataSource
const dataSource = new DataSource({
  type: process.env.TYPEORM_CONNECTION || "postgres",
  host: process.env.TYPEORM_HOST || "localhost",
  port: parseInt(process.env.TYPEORM_PORT || "5432"),
  username: process.env.TYPEORM_USERNAME,
  password: process.env.TYPEORM_PASSWORD,
  database: process.env.TYPEORM_DATABASE,
  entities: [process.env.TYPEORM_ENTITIES || "./src/entities/**/*.ts"],
  migrations: [process.env.TYPEORM_MIGRATIONS || "./src/migrations/**/*.ts"],
  synchronize: false,
  logging: false,
});

const server = new Server(
  {
    name: "typeorm-inspector",
    version: "1.0.0",
  },
  {
    capabilities: {
      resources: {},
      tools: {},
    },
  }
);

// Initialize database connection
let isConnected = false;

async function ensureConnection() {
  if (!isConnected) {
    await dataSource.initialize();
    isConnected = true;
  }
}

// ============================================
// TOOL DEFINITIONS
// ============================================

server.setRequestHandler("tools/list", async () => {
  return {
    tools: [
      {
        name: "list_entities",
        description: "List all TypeORM entities in the project",
        inputSchema: {
          type: "object",
          properties: {},
        },
      },
      {
        name: "inspect_entity",
        description: "Get detailed information about a specific entity including columns, relations, and indexes",
        inputSchema: {
          type: "object",
          properties: {
            entityName: {
              type: "string",
              description: "Name of the entity to inspect (e.g., 'User', 'Product')",
            },
          },
          required: ["entityName"],
        },
      },
      {
        name: "query_entity",
        description: "Query data from an entity with optional filters",
        inputSchema: {
          type: "object",
          properties: {
            entityName: {
              type: "string",
              description: "Name of the entity to query",
            },
            limit: {
              type: "number",
              description: "Number of records to return",
              default: 10,
            },
            where: {
              type: "object",
              description: "Filter conditions (e.g., {status: 'active'})",
            },
          },
          required: ["entityName"],
        },
      },
      {
        name: "list_migrations",
        description: "List all TypeORM migrations and their status",
        inputSchema: {
          type: "object",
          properties: {},
        },
      },
      {
        name: "generate_migration",
        description: "Suggest a new migration based on entity changes",
        inputSchema: {
          type: "object",
          properties: {
            name: {
              type: "string",
              description: "Migration name (e.g., 'AddUserEmailColumn')",
            },
          },
          required: ["name"],
        },
      },
      {
        name: "analyze_relations",
        description: "Analyze relationships between entities",
        inputSchema: {
          type: "object",
          properties: {
            entityName: {
              type: "string",
              description: "Starting entity name (optional)",
            },
          },
        },
      },
      {
        name: "check_indexes",
        description: "Check and suggest database indexes for performance",
        inputSchema: {
          type: "object",
          properties: {
            entityName: {
              type: "string",
              description: "Entity name to check indexes (optional)",
            },
          },
        },
      },
      {
        name: "validate_schema",
        description: "Validate that database schema matches entity definitions",
        inputSchema: {
          type: "object",
          properties: {},
        },
      },
      {
        name: "raw_query",
        description: "Execute raw SQL query (use with caution)",
        inputSchema: {
          type: "object",
          properties: {
            query: {
              type: "string",
              description: "SQL query to execute",
            },
            parameters: {
              type: "array",
              description: "Query parameters",
            },
          },
          required: ["query"],
        },
      },
    ],
  };
});

// ============================================
// TOOL IMPLEMENTATIONS
// ============================================

server.setRequestHandler("tools/call", async (request) => {
  const { name, arguments: args } = request.params;

  try {
    await ensureConnection();

    // List all entities
    if (name === "list_entities") {
      const entities = dataSource.entityMetadatas.map((meta) => ({
        name: meta.name,
        tableName: meta.tableName,
        columns: meta.columns.length,
        relations: meta.relations.length,
      }));

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(entities, null, 2),
          },
        ],
      };
    }

    // Inspect specific entity
    if (name === "inspect_entity") {
      const meta = dataSource.entityMetadatas.find(
        (m) => m.name === args.entityName
      );

      if (!meta) {
        throw new Error(`Entity '${args.entityName}' not found`);
      }

      const entityInfo = {
        name: meta.name,
        tableName: meta.tableName,
        columns: meta.columns.map((col) => ({
          name: col.propertyName,
          type: col.type,
          nullable: col.isNullable,
          primary: col.isPrimary,
          unique: col.isUnique,
          default: col.default,
        })),
        relations: meta.relations.map((rel) => ({
          name: rel.propertyName,
          type: rel.relationType,
          target: rel.inverseEntityMetadata.name,
          nullable: rel.isNullable,
        })),
        indexes: meta.indices.map((idx) => ({
          name: idx.name,
          columns: idx.columns.map((c) => c.propertyName),
          unique: idx.isUnique,
        })),
      };

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(entityInfo, null, 2),
          },
        ],
      };
    }

    // Query entity data
    if (name === "query_entity") {
      const repo = dataSource.getRepository(args.entityName);
      const data = await repo.find({
        where: args.where || {},
        take: args.limit || 10,
      });

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(data, null, 2),
          },
        ],
      };
    }

    // List migrations
    if (name === "list_migrations") {
      const queryRunner = dataSource.createQueryRunner();
      const executedMigrations = await queryRunner.getExecutedMigrations();
      const allMigrations = dataSource.migrations;

      const migrationStatus = allMigrations.map((mig) => {
        const executed = executedMigrations.find((em) => em.name === mig.name);
        return {
          name: mig.name,
          executed: !!executed,
          timestamp: executed?.timestamp || null,
        };
      });

      await queryRunner.release();

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(migrationStatus, null, 2),
          },
        ],
      };
    }

    // Generate migration suggestion
    if (name === "generate_migration") {
      const queryRunner = dataSource.createQueryRunner();
      const sqlInMemory = await queryRunner.generateMigration(args.name);
      await queryRunner.release();

      return {
        content: [
          {
            type: "text",
            text: `Suggested migration:\n\n${sqlInMemory}`,
          },
        ],
      };
    }

    // Analyze relations
    if (name === "analyze_relations") {
      let entities = dataSource.entityMetadatas;

      if (args.entityName) {
        entities = entities.filter((e) => e.name === args.entityName);
      }

      const relationGraph = entities.map((meta) => ({
        entity: meta.name,
        relations: meta.relations.map((rel) => ({
          property: rel.propertyName,
          type: rel.relationType,
          target: rel.inverseEntityMetadata.name,
          cascades: rel.isCascadeInsert || rel.isCascadeUpdate,
          eager: rel.isEager,
        })),
      }));

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(relationGraph, null, 2),
          },
        ],
      };
    }

    // Check indexes
    if (name === "check_indexes") {
      let entities = dataSource.entityMetadatas;

      if (args.entityName) {
        entities = entities.filter((e) => e.name === args.entityName);
      }

      const indexAnalysis = entities.map((meta) => {
        const foreignKeys = meta.relations
          .filter((r) => r.relationType === "many-to-one")
          .map((r) => r.propertyName);

        const indexedColumns = meta.indices.flatMap((idx) =>
          idx.columns.map((c) => c.propertyName)
        );

        const missingIndexes = foreignKeys.filter(
          (fk) => !indexedColumns.includes(fk)
        );

        return {
          entity: meta.name,
          existingIndexes: meta.indices.map((idx) => ({
            name: idx.name,
            columns: idx.columns.map((c) => c.propertyName),
          })),
          suggestedIndexes: missingIndexes.map((col) => ({
            reason: "Foreign key without index",
            column: col,
            suggestion: `CREATE INDEX idx_${meta.tableName}_${col} ON ${meta.tableName}(${col})`,
          })),
        };
      });

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(indexAnalysis, null, 2),
          },
        ],
      };
    }

    // Validate schema
    if (name === "validate_schema") {
      const queryRunner = dataSource.createQueryRunner();

      try {
        // This will throw if schema doesn't match
        await queryRunner.syncSchema();
        await queryRunner.release();

        return {
          content: [
            {
              type: "text",
              text: "✓ Schema validation passed - database matches entity definitions",
            },
          ],
        };
      } catch (error) {
        await queryRunner.release();
        return {
          content: [
            {
              type: "text",
              text: `✗ Schema validation failed:\n${error.message}`,
            },
          ],
        };
      }
    }

    // Raw query
    if (name === "raw_query") {
      const result = await dataSource.query(
        args.query,
        args.parameters || []
      );

      return {
        content: [
          {
            type: "text",
            text: JSON.stringify(result, null, 2),
          },
        ],
      };
    }

    throw new Error(`Unknown tool: ${name}`);
  } catch (error) {
    return {
      content: [
        {
          type: "text",
          text: `Error: ${error.message}`,
        },
      ],
      isError: true,
    };
  }
});

// ============================================
// START SERVER
// ============================================

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("TypeORM MCP Server running");
}

main().catch((error) => {
  console.error("Fatal error:", error);
  process.exit(1);
});