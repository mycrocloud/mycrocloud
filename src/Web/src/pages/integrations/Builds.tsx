import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useState } from "react";
import { AppContext } from "../apps";
import { useAuth0 } from "@auth0/auth0-react";
import BuildLogs from "./BuildLogs";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

interface IBuild {
    id: string;
    name: string;
    status: string;
    createdAt: string;
    finishedAt: string;
}

type BuildInputs = {
    name?: string
}

export default function Builds() {
    const { app } = useContext(AppContext)!;
    if (!app) throw new Error();

    const { get, post } = useApiClient();
    const { getAccessTokenSilently } = useAuth0();

    const [builds, setBuilds] = useState<IBuild[]>([]);

    const fetchBuilds = useCallback(async () => {
        const data = await get<IBuild[]>(`/api/apps/${app.id}/builds`);
        setBuilds(data);
    }, [app.id]);

    // SSE subscription
    useEffect(() => {
        let isMounted = true;
        const evtRef = { current: null as EventSource | null };

        (async () => {
            const accessToken = await getAccessTokenSilently();
            if (!isMounted) return;

            const evtSource = new EventSource(
                `/api/apps/${app.id}/builds/stream?access_token=${accessToken}`
            );
            evtRef.current = evtSource;

            // Initial load
            fetchBuilds();

            evtSource.onmessage = () => {
                if (!isMounted) return;
                fetchBuilds();
            };

            evtSource.onerror = (error) => {
                console.error("SSE error:", error);
            };
        })();

        return () => {
            isMounted = false;

            if (evtRef.current) {
                evtRef.current.close();
                evtRef.current = null;
            }
        };
    }, [app.id, fetchBuilds]);

    const [buildId, setBuildId] = useState<string>();

    function statusClass(status: string) {
        if (status === "pending") {
            return "text-yellow-500";
        } else if (status === "success" || status === "done") {
            return "text-sky-500";
        } else if (status === "failed") {
            return "text-red-500";
        } else {
            return "text-gray-300";
        }
    }

    const [showBuildModal, setShowBuildModal] = useState(false);
    const { register, formState: { errors }, handleSubmit, reset } = useForm<BuildInputs>()
    const onSubmit = async (inputs: BuildInputs) => {
        try {
            await post(`/api/apps/${app.id}/builds/build`, inputs)
            setShowBuildModal(false)
        } catch {
            alert("Something went wrong...")
        }
    }
    useEffect(() => {
        if (showBuildModal) {
            reset()
        }
    }, [showBuildModal, reset])

    return <section>
        <div className="mt-4 flex items-center">
            <h2 className="font-semibold">Builds</h2>
        </div>
        <div className="mt-2">
            <Button onClick={() => setShowBuildModal(true)} size="sm">Build</Button>
        </div>
        <div className="flex mt-2">
            <div className="overflow-y-auto">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead className="w-80">Name</TableHead>
                            <TableHead className="w-20">Status</TableHead>
                            <TableHead className="w-60">Started At</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {builds.map((build) => (
                            <TableRow
                                key={build.id}
                                className={
                                    "cursor-pointer" +
                                    (buildId === build.id ? " bg-muted" : "")
                                }
                                onClick={() => setBuildId(build.id)}
                            >
                                <TableCell>{build.name}</TableCell>
                                <TableCell className={statusClass(build.status)}>
                                    {build.status}
                                </TableCell>
                                <TableCell>{build.createdAt}</TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </div>
            <div className="flex-1 overflow-hidden">
                {buildId ? (
                    <BuildLogs appId={app.id} buildId={buildId} />
                ) : (
                    <div className="p-4 text-muted-foreground">Select a build to view logs</div>
                )}
            </div>
        </div>
        <Dialog open={showBuildModal} onOpenChange={setShowBuildModal}>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>Build</DialogTitle>
                </DialogHeader>
                <form id="build-form" onSubmit={handleSubmit(onSubmit)}>
                    <div className="space-y-2">
                        <Label>Build Name</Label>
                        <Input {...register("name")} placeholder="Build name" />
                        {errors.name && <span className="text-red-500 text-sm">{errors.name.message}</span>}
                    </div>
                </form>
                <DialogFooter>
                    <Button variant="outline" onClick={() => setShowBuildModal(false)}>Cancel</Button>
                    <Button type="submit" form="build-form">Build</Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    </section>
}