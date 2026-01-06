export const scanValueTypes = [
  {
    id: "short",
    label: "Short (2 Bytes)",
  },
  {
    id: "integer",
    label: "Integer (4 Bytes)",
  },
  {
    id: "float",
    label: "Float (4 Bytes)",
  },
  {
    id: "long",
    label: "Long (8 Bytes)",
  },
  {
    id: "double",
    label: "Double (8 Bytes)",
  },
] as const;

export const scanValueTypeById = Object.fromEntries(
  scanValueTypes.map((t) => [t.id, t])
);