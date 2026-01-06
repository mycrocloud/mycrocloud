import React from "react";
import ReactDOM from "react-dom/client";
import "./index.css";
import './styles/components/code-snippet.css';
import "./userWorker";
import App from "./App.tsx";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);