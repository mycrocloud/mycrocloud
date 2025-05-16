// types/editorBridge.ts
export interface EditorAPI {
  setValue: (value: string) => void;
  getValue: () => string;
  focus: () => void;
  setLanguage: (language: string) => void;
}

export type OnEditorContentChange = (editorId: string, value: string) => void;