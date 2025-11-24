import { useParams } from "react-router-dom";
import { useEffect, useState, createContext, useContext, Dispatch, SetStateAction } from "react";
import { IApp } from ".";
import { useApiClient } from "@/hooks";

interface AppContextType {
  app: IApp | undefined;
  setApp: Dispatch<SetStateAction<IApp | undefined>>;
  loading: boolean;
}

export const AppContext = createContext<AppContextType | undefined>(undefined);

export const AppProvider = ({ children }: { children: React.ReactNode }) => {
  const { get } = useApiClient();
  const appId = parseInt(useParams()["appId"]!.toString());
  const [app, setApp] = useState<IApp>();
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchApp = async () => {
      try {
        setLoading(true);
        const data = await get<IApp>(`/api/apps/${appId}`);
        setApp(data);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchApp();
  }, [appId, get]);

  return (
    <AppContext.Provider value={{ app, setApp, loading }}>
      {children}
    </AppContext.Provider>
  );
};

export const useApp = () => {
  const ctx = useContext(AppContext);
  if (!ctx) throw new Error("useApp must be used within <AppProvider />");
  return ctx;
};