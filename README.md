# Vendomatic

A toy project to play with .NET. Simulates some vending machines with products in them and what we can do with some data we get from them.

## DynamoDB

SQL is expensive in AWS so I'm cheaping out and using DynamoDB by default.

### Schema

| Entity    | PK              | SK               |
| --------- | --------------- | ---------------- |
| Machine   | MAC#{id}        | MAC#{id}         |
| Inventory | INV#{MachineID} | PROD#{ProductID} |

Purposefully don't query across machines; they're supposed to be self-contained and isolated, these are just representations.