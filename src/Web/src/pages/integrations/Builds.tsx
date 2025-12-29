import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useRef, useState } from "react";
import { AppContext } from "../apps";
import { useAuth0 } from "@auth0/auth0-react";
import BuildLogs from "./BuildLogs";
import { Button, Label, Modal, ModalBody, ModalFooter, ModalHeader, TextInput } from "flowbite-react";
import { useForm } from "react-hook-form";

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
        <div>
            <Button onClick={() => setShowBuildModal(true)} size={'sm'}>Build</Button>
        </div>
        <div className="flex">
            <div className="overflow-y-auto">
                <table className="mt-2 table-fixed">
                    <thead>
                        <tr className="border">
                            <th className="w-80 p-2 text-start">Name</th>
                            <th className="w-20 text-start">Status</th>
                            <th className="w-60 text-start">Started At</th>
                        </tr>
                    </thead>
                    <tbody>
                        {builds.map((build) => (
                            <tr
                                key={build.id}
                                className={
                                    "cursor-pointer border hover:bg-slate-100" +
                                    (buildId === build.id ? " bg-slate-200" : "")
                                }
                                onClick={() => setBuildId(build.id)}
                            >
                                <td className="p-2">{build.name}</td>
                                <td className={statusClass(build.status)}>
                                    {build.status}
                                </td>
                                <td>{build.createdAt}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
            <div className="flex-1 overflow-hidden">
                {buildId ? (
                    <BuildLogs appId={app.id} buildId={buildId} />
                ) : (
                    <div className="p-4 text-gray-400">Select a build to view logs</div>
                )}
            </div>
        </div>
        <Modal show={showBuildModal} onClose={() => setShowBuildModal(false)}>
            <ModalHeader>
                <h1>Build</h1>
            </ModalHeader>
            <ModalBody>
                <form id="build-form" onSubmit={handleSubmit(onSubmit)}>
                    <Label>Build Name</Label>
                    <TextInput {...register("name")} className="mt-2" placeholder="Build name" />
                    { errors.name && <span className="text-red-500">{errors.name.message}</span>}
                </form>
            </ModalBody>
            <ModalFooter>
                <Button type="submit" form="build-form" className="ms-auto">Build</Button>
                <Button outline onClick={() => setShowBuildModal(false)}>Cancel</Button>
            </ModalFooter>
        </Modal>
    </section>
}