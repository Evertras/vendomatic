#!/usr/bin/env bash

baseUrl="https://vendomatic.evertras.com/api/v1"

# Get list of machines where the name starts with Integration Test, then go through and delete
# each one individually
list=$(curl -s -XGET "$baseUrl/machines" | jq -r '.machines[] | select(.name | startswith("Integration Test")) | .id' | tr -d '\r')
echo "$list"

echo "Deleting all found machines"
for id in $list; do
  echo "Deleting machine $id"
  curl -XDELETE "$baseUrl/machines/$id"
  echo ""
done