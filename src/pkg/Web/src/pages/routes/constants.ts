export const methods = ["get", "post", "put", "delete", "patch"];

export const functionExecutionEnvironmentMap = new Map<number, string>([
  [1, "In-process (Queued)"],
  [2, "Isolated (Queued)"],
]);
