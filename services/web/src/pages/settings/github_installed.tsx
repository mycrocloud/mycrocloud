import { useEffect } from "react";
import { Loader2 } from "lucide-react";

export default function GitHubInstalled() {
  useEffect(() => {
    window.close();
  }, []);

  return (
    <div className="flex min-h-[50vh] items-center justify-center">
      <div className="flex flex-col items-center gap-3">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        <p className="text-sm text-muted-foreground">GitHub Installation Completed</p>
      </div>
    </div>
  );
}
