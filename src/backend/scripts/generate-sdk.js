const { execSync } = require('child_process');
const fs = require('fs');

const SWAGGER_URL = "http://localhost:5000/swagger/v1/swagger.json";
const OUTPUT_DIR = "../../clients/typescript";

console.log(`Downloading OpenAPI spec from ${SWAGGER_URL}...`);

// Use curl to download the spec (assumes curl is available)
try {
    execSync(`curl -s -o swagger.json ${SWAGGER_URL}`);
} catch (e) {
    console.warn("⚠️ Could not pull remote swagger, assuming local swagger.json exists.");
}

console.log("Generating TypeScript Fetch SDK...");

try {
    execSync(`npx @openapitools/openapi-generator-cli generate -i swagger.json -g typescript-fetch -o ${OUTPUT_DIR} --additional-properties=supportsES6=true,typescriptThreePlus=true`);
    console.log(`✅ TypeScript SDK generated successfully in ${OUTPUT_DIR}`);
} catch (err) {
    console.error("❌ SDK Generation failed", err);
}
