import { useEffect, useState } from "react"
import { toast } from "react-toastify";
import TextCopyButton from "../../components/ui/TextCopyButton";
import { Modal } from "flowbite-react";
import { useForm } from "react-hook-form";
import { useApiClient } from "@/hooks";

interface IToken {
    id: number
    name: string
    token: string
    status: "None" | "Revoked"
}

type CreateType = {
    name: string
}

export default function Tokens() {
    const { get, post, del } = useApiClient();

    const [tokens, setTokens] = useState<IToken[]>([]);

    useEffect(() => {
        (async () => {
            const tokens = await get<IToken[]>("/api/usersettings/tokens");
            setTokens(tokens);
        })();
    }, []);

    const [showCreateModal, setShowCreateModal] = useState(false);
    const { handleSubmit, register, formState: { errors }, reset } = useForm<CreateType>();

    const onCreateClickHandler = async (data: CreateType) => {
        try {
            const token = await post<IToken>("/api/usersettings/tokens", { name: data.name })
            setTokens([...tokens, token]);

            reset();
            setShowCreateModal(false);
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
        <section className="mt-4">
            <div className="flex items-center justify-between">
                <h2 className="font-semibold">Tokens</h2>
                <button className="bg-primary text-white px-2 py-1" onClick={() => setShowCreateModal(true)}>Create</button>
            </div>
            <table className="table-fixed mt-2 w-full border border-gray-200 rounded-lg overflow-hidden">
                <thead className="bg-gray-50">
                    <tr>
                        <th className="w-[30%] px-4 py-2 text-left text-gray-700">Name</th>
                        <th className="w-[50%] px-4 py-2 text-left text-gray-700">Token</th>
                        <th className="w-[5%] px-4 py-2 text-left text-gray-700">Status</th>
                        <th className="w-[15%] px-4 py-2 text-left text-gray-700">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {tokens.map(t => (
                        <tr key={t.id} className="border-t hover:bg-gray-50">
                            <td className="px-4 py-2 truncate">{t.name}</td>
                            <td className="px-4 py-2 flex items-center gap-2 truncate">
                                {t.token}
                                <TextCopyButton text={t.token} />
                            </td>
                            <td>{t.status == "None" ? "-" : t.status}</td>
                            <td className="px-4 py-2">
                                <div className="flex gap-2 justify-end">
                                    <button onClick={() => revoke(t.id)} className="text-blue-600 hover:underline">Revoke</button>
                                    <button onClick={() => _delete(t.id)} className="text-red-600 hover:underline">Delete</button>
                                </div>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
            <Modal
                show={showCreateModal}
                onClose={() => setShowCreateModal(false)}
            >
                <Modal.Header>Generate new token</Modal.Header>
                <Modal.Body>
                    <form>
                        <input {...register("name", {
                            required: "Name is required", maxLength: {
                                value: 50,
                                message: "Name cannot exceed 50 characters",
                            }
                        })} placeholder="Token name" className="block w-full border border-gray-300 bg-gray-50 p-1.5  text-gray-900 focus:border-blue-500 focus:ring-blue-500" />
                        {errors.name && <span className="text-red-500">{errors.name.message}</span>}
                    </form>
                </Modal.Body>
                <Modal.Footer className="justify-end">
                    <button
                        onClick={() => {
                            setShowCreateModal(false);
                        }}
                        className="rounded-sm border px-3 py-1.5"
                    >
                        Cancel
                    </button>
                    <button
                        className="bg-primary px-3 py-1.5 text-white"
                        onClick={handleSubmit(onCreateClickHandler)}
                    >
                        Create
                    </button>
                </Modal.Footer>
            </Modal>
        </section>
    )
}