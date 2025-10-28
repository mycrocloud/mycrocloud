export function generateApiKey() {
  const chars =
    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
  const keyLength = 32; // Length of the API key

  let apiKey = "";
  for (let i = 0; i < keyLength; i++) {
    // Select a random character from the chars string
    const randomIndex = Math.floor(Math.random() * chars.length);
    apiKey += chars.charAt(randomIndex);
  }

  return apiKey;
}
