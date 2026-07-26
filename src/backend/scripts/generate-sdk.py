import os
import subprocess
import requests

# This script generates a Python SDK using OpenAPI Generator CLI
SWAGGER_URL = "http://localhost:5000/swagger/v1/swagger.json"
OUTPUT_DIR = "../../clients/python"

def generate_sdk():
    print(f"Downloading OpenAPI spec from {SWAGGER_URL}...")
    try:
        response = requests.get(SWAGGER_URL)
        response.raise_for_status()
        with open("swagger.json", "w") as f:
            f.write(response.text)
    except Exception as e:
        print(f"⚠️ Could not pull remote swagger, using local if exists. Error: {e}")

    print("Generating Python SDK...")
    # Requires openapi-generator-cli installed via npm or brew
    subprocess.run([
        "npx", "@openapitools/openapi-generator-cli", "generate",
        "-i", "swagger.json",
        "-g", "python",
        "-o", OUTPUT_DIR,
        "--additional-properties=packageName=upkilo_sdk,projectName=Upkilo.SDK"
    ])
    
    print(f"✅ Python SDK generated successfully in {OUTPUT_DIR}")

if __name__ == "__main__":
    generate_sdk()
