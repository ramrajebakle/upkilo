import sys
import re

file_path = "Upkilo.Infrastructure/Data/AppDbContext.cs"
with open(file_path, "r", encoding="utf-8") as f:
    lines = f.readlines()

seen_dbsets = set()
new_lines = []
dbset_pattern = re.compile(r"public DbSet<(\w+)> (\w+)")

for line in lines:
    match = dbset_pattern.search(line)
    if match:
        entity_type = match.group(1)
        dbset_name = match.group(2)
        # Check for both type and name to be thorough
        if entity_type in seen_dbsets:
            print(f"Removing duplicate DbSet: {line.strip()}")
            continue
        seen_dbsets.add(entity_type)
    new_lines.append(line)

with open(file_path, "w", encoding="utf-8") as f:
    f.writelines(new_lines)
