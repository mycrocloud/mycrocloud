import { useParams } from "react-router-dom"
import BuildLogs from "./_BuildLogs";
import { useContext, useState } from "react";
import { AppContext } from "../apps";
import { IBuildJob } from "@/models/apps";

export default function AppBuild() {
    const { app } = useContext(AppContext)!;
    const buildId = useParams()["buildId"];
    if (!app || !buildId) return;

    const [build, setBuild] = useState<IBuildJob | null>(null);

    return <div>
        <header>
            Build {buildId}
        </header>
        <section>
            <BuildLogs appId={app.id} buildId={buildId} />
        </section>
    </div>
}