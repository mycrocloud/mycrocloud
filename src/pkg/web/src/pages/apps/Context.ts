import { createContext, Dispatch, SetStateAction } from "react";
import { IApp } from ".";

interface AppContext {
  app: IApp | undefined;
  setApp: Dispatch<SetStateAction<IApp | undefined>>;
}

export const Context = createContext<AppContext | undefined>(undefined);
