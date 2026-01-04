import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useState } from "react";
import { AppContext } from "../apps";
import { useAuth0 } from "@auth0/auth0-react";
import { Button, HelperText, Label, Modal, ModalBody, ModalFooter, ModalHeader, Pagination, Spinner, Table, TableBody, TableCell, TableHead, TableHeadCell, TableRow, TextInput } from "flowbite-react";
import { useForm } from "react-hook-form";
import { PaginatedResponse } from "@/models/Pagination";
import { useNavigate } from "react-router-dom";

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

    const { get, post, getPagination } = useApiClient();
    const { getAccessTokenSilently } = useAuth0();
    const navigate = useNavigate()

    const [data, setData] = useState<PaginatedResponse<IBuild> | null>(null);
    const [currentPage, setCurrentPage] = useState(1);
    const [pageSize, setPageSize] = useState(5);

    const fetchBuilds = useCallback(async () => {
        const data = await getPagination<IBuild>(`/api/apps/${app.id}/builds`, { page: currentPage, per_page: pageSize });
        setData(data);
    }, [app.id, currentPage, pageSize, getPagination]);

    const onPageChange = (page: number) => {
        setCurrentPage(page);
    };

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

    if (!data?.data) {
        return <Spinner />
    }

    return <div className="overflow-y-auto">
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
                {data?.data.map((build) => (
                    <TableRow
                        key={build.id}
                        className="cursor-pointer"
                        onClick={() => navigate(`builds/${build.id}`)}
                    >
                        <TableCell>{build.name}</TableCell>
                        <TableCell>
                            {build.status}
                        </TableCell>
                        <TableCell>{new Date(build.createdAt).toLocaleString()}</TableCell>
                    </TableRow>
                ))}
            </TableBody>
        </Table>
        <div className="mt-4 flex overflow-x-auto sm:justify-center">
            <Pagination
                currentPage={currentPage}
                totalPages={Math.ceil(data.meta.total_count / data.meta.per_page)}
                onPageChange={onPageChange}
                showIcons={true}
            />
        </div>
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