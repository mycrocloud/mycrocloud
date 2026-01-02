import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useState } from "react";
import { AppContext } from "../apps";
import { useAuth0 } from "@auth0/auth0-react";
import { Button, HelperText, Label, Modal, ModalBody, ModalFooter, ModalHeader, Table, TableBody, TableCell, TableHead, TableHeadCell, TableRow, TextInput } from "flowbite-react";
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

export default function AppBuilds() {
    const { app } = useContext(AppContext)!;
    if (!app) throw new Error();

    const { get, post } = useApiClient();
    const { getAccessTokenSilently } = useAuth0();

    const [builds, setBuilds] = useState<IBuild[]>([]);

    const fetchBuilds = useCallback(async () => {
        const builds = await get<IBuild[]>(`/api/apps/${app.id}/builds`);
        setBuilds(builds);
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

    const [showBuildModal, setShowBuildModal] = useState(false);
    const { register, formState: { errors }, handleSubmit, reset } = useForm<BuildInputs>();
    const onSubmit = async (inputs: BuildInputs) => {
        try {
            await post(`/api/apps/${app.id}/builds/build`, inputs);
            setShowBuildModal(false);
        } catch {
            alert("Something went wrong...");
        }
    }
    useEffect(() => {
        if (showBuildModal) {
            reset();
        }
    }, [showBuildModal, reset]);

    return <div>
        <header>
            <div className="flex justify-between items-center">
                <h3 className="font-semibold">Builds</h3>
                <Button onClick={() => setShowBuildModal(true)} size={'sm'}>Build</Button>
            </div>
        </header>
        <Table className="mt-2" hoverable striped>
            <TableHead>
                <TableRow>
                    <TableHeadCell className="">Name</TableHeadCell>
                    <TableHeadCell className="">Status</TableHeadCell>
                    <TableHeadCell className="">Started At</TableHeadCell>
                </TableRow>
            </TableHead>
            <TableBody>
                {builds.map((build) => (
                    <TableRow
                        key={build.id}
                        className="cursor-pointer"
                        role="Link"
                    >
                        <TableCell>{build.name}</TableCell>
                        <TableCell>
                            {build.status}
                        </TableCell>
                        <TableCell>{build.createdAt}</TableCell>
                    </TableRow>
                ))}
            </TableBody>
        </Table>
        <Modal show={showBuildModal} onClose={() => setShowBuildModal(false)}>
            <ModalHeader className="border-gray-200">Build</ModalHeader>
            <ModalBody>
                <form id="build-form" onSubmit={handleSubmit(onSubmit)}>
                    <Label>Build Name</Label>
                    <TextInput {...register("name", { required: "name is required." })} className="mt-2" placeholder="Build name" />
                    {errors.name && <HelperText color="failure">{errors.name.message}</HelperText>}
                </form>
            </ModalBody>
            <ModalFooter className="justify-end">
                <Button outline color={"gray"} onClick={() => setShowBuildModal(false)}>Cancel</Button>
                <Button type="submit" form="build-form">Build</Button>
            </ModalFooter>
        </Modal>
    </div>
}