import { createContext, type Dispatch, type SetStateAction } from "react";
import { type IApp } from ".";

interface AppContext {
  app: IApp | undefined;
  setApp: Dispatch<SetStateAction<IApp | undefined>>;
}

export const Context = createContext<AppContext | undefined>(undefined);
