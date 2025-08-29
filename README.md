# Vendomatic

A toy project to play with .NET. Simulates some vending machines with products in them and what we can do with some data we get from them.

## DynamoDB

SQL is expensive in AWS so I'm cheaping out and using DynamoDB by default.

### Schema

| Entity    | PK              | SK               | Attributes |
| --------- | --------------- | ---------------- | ---------- |
| Machine   | MAC#{id}        | MAC#{id}         | Name (S)   |
| Inventory | INV#{MachineID} | PROD#{ProductID} | Name (S), Quantity(N), CostPennies (N), RestockedAt (S) |

Purposefully don't query across machines; they're supposed to be self-contained and isolated, these are just representations.