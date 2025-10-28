/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./src/**/*.{js,jsx,ts,tsx}",
    "node_modules/flowbite-react/**/*.{js,jsx,ts,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: "#0891b2",
        secondary: "#718096",
        tertiary: "#e2e8f0",
        quaternary: "#f7fafc",
        quinary: "#ffffff",
      },
    },
  },
  plugins: ["flowbite/plugin"],
};
