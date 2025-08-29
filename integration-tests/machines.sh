#!/usr/bin/env bash

baseUrl="https://vendomatic.evertras.com/api/v1"
machineName="Integration Test Machine $(date)"

echo "Creating machine $machineName"
createResult=$(curl -s -XPOST "$baseUrl/machines" -H "Content-Type: application/json" -d '{
  "name": "'"$machineName"'"
}')

echo "$createResult"
machineId=$(echo "$createResult" | jq -r '.machine.id')

echo "Machine ID: $machineId"

echo ""
echo "Getting list"
curl -XGET "$baseUrl/machines"

echo ""
echo "Checking machine inventory"
curl -XGET "$baseUrl/machines/$machineId"

echo ""
echo "Restocking machine"
curl -XPUT "$baseUrl/machines/$machineId/inventory" -H "Content-Type: application/json" -d '{
  "inventory": [
    {
      "name": "Soda",
      "quantityTarget": 10,
      "costPennies": 100
    },
    {
      "name": "Chips",
      "quantityTarget": 5,
      "costPennies": 150
    }
  ]
}'

echo ""
echo "Checking machine inventory"
curl -XGET "$baseUrl/machines/$machineId"