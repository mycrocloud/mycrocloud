import { useEffect, useState } from "react"
import { toast } from "react-toastify";
import TextCopyButton from "../../components/ui/TextCopyButton";
import { Badge, Button, Dropdown, DropdownItem, HelperText, Modal, ModalBody, ModalFooter, ModalHeader, Table, TableBody, TableCell, TableHead, TableHeadCell, TableRow, TextInput } from "flowbite-react";
import { useForm } from "react-hook-form";
import { useApiClient } from "@/hooks";
import { EllipsisHorizontalIcon } from "@heroicons/react/24/solid";

interface IApiToken {
    id: number
    name: string
    token: string
    status: "None" | "Revoked"
}

type CreateType = {
    name: string
}

type CreateModalState = {
    step: number;
    token?: string;
}

export default function ApiTokens() {
    const { get, post, del } = useApiClient();

    const [tokens, setTokens] = useState<IApiToken[]>([]);

    useEffect(() => {
        (async () => {
            const tokens = await get<IApiToken[]>("/api/usersettings/tokens");
            setTokens(tokens);
        })();
    }, []);

    const [showCreateModal, setShowCreateModal] = useState(false);
    const [createModalState, setCreateModalState] = useState<CreateModalState>({ step: 0 });

    useEffect(() => {
        if (showCreateModal) {
            reset();
            setCreateModalState({ step: 0 });
        }
    }, [showCreateModal]);

    const { handleSubmit, register, formState: { errors }, reset } = useForm<CreateType>();

    const onCreateClickHandler = async (data: CreateType) => {
        try {
            const token = await post<IApiToken>("/api/usersettings/tokens", { name: data.name })
            setTokens([...tokens, token]);

            setCreateModalState({ step: 1, token: token.token });
        } catch (error) {
            toast.error('Something went wrong.')
        }
    }

    const _delete = async (id: number) => {
        if (confirm('Are you sure want to delete this token?')) {
            await del(`/api/usersettings/tokens/${id}`)
            setTokens(tokens.filter(t => t.id !== id));
        }
    }

    const revoke = async (id: number) => {
        if (confirm('Are you sure want to revoke this token?')) {
            await post(`/api/usersettings/tokens/${id}/revoke`)

            setTokens((tokens) =>
                tokens.map((t) =>
                    t.id === id ? { ...t, status: "Revoked" } : t
                )
            );
        }
    }

    return (
        <section className="space-y-4 rounded-lg border border-slate-200 bg-white p-4">
            <header className="flex items-center justify-between">
                <h2 className="text-base font-semibold">Tokens</h2>
                <Button onClick={() => setShowCreateModal(true)}>Create</Button>
            </header>
            <Table>
                <TableHead className="bg-slate-50">
                    <TableRow>
                        <TableHeadCell className="text-xs font-semibold uppercase tracking-wide text-slate-600">
                            Name
                        </TableHeadCell>
                        <TableHeadCell className="text-xs font-semibold uppercase tracking-wide text-slate-600">
                            Status
                        </TableHeadCell>
                        <TableHeadCell className="text-right text-xs font-semibold uppercase tracking-wide text-slate-600">
                            Actions
                        </TableHeadCell>
                    </TableRow>
                </TableHead>

                <TableBody className="divide-y">
                    {tokens.map((t) => (
                        <TableRow
                            key={t.id}
                            className="bg-white hover:bg-slate-50"
                        >
                            <TableCell className="font-medium">
                                {t.name}
                            </TableCell>

                            <TableCell>
                                <span className="inline-flex rounded-full bg-cyan-100 px-2 py-0.5 text-xs font-medium text-cyan-700">
                                    {t.status === "None" ? "Active" : t.status}
                                </span>
                            </TableCell>

                            <TableCell className="relative text-right">
                                <Dropdown
                                    inline
                                    color="gray"
                                    outline
                                    arrowIcon={false}

                                    label={
                                        <EllipsisHorizontalIcon
                                            className="h-5 w-5"
                                            aria-hidden
                                        />
                                    }
                                    className="origin-top-right right-0"
                                >
                                    <DropdownItem onClick={() => revoke(t.id)}>
                                        Revoke
                                    </DropdownItem>
                                    <DropdownItem
                                        className="text-red-600"
                                        onClick={() => _delete(t.id)}
                                    >
                                        Delete
                                    </DropdownItem>
                                </Dropdown>
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>

            <Modal
                show={showCreateModal}
                onClose={() => setShowCreateModal(false)}
            >
                <ModalHeader>Generate new token</ModalHeader>
                <ModalBody>
                    {createModalState.step == 0 && <form>
                        <TextInput {...register("name", {
                            required: "Name is required", maxLength: {
                                value: 50,
                                message: "Name cannot exceed 50 characters",
                            }
                        })} placeholder="Token name" />
                        {errors.name && <HelperText color="failure">{errors.name.message}</HelperText>}
                    </form>}
                    {createModalState.step == 1 && <div>
                        <p>Copy token</p>
                        {createModalState.token}
                    </div> }
                </ModalBody>
                <ModalFooter className="justify-end">
                    <Button
                        onClick={() => {
                            setShowCreateModal(false);
                        }}
                        color={"gray"}
                        outline
                    >
                        {createModalState.step == 0 ? "Cancel" : "Close"}
                    </Button>
                    {createModalState.step == 0 && <Button
                        onClick={handleSubmit(onCreateClickHandler)}
                    >
                        Create
                    </Button>}
                </ModalFooter>
            </Modal>
        </section>
    )
}