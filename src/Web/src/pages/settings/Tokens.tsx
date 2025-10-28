import { useAuth0 } from "@auth0/auth0-react";
import { useEffect, useState } from "react"
import { toast } from "react-toastify";
import TextCopyButton from "../../components/ui/TextCopyButton";
import { Modal } from "flowbite-react";
import { useForm } from "react-hook-form";

interface IToken {
    name: string
    token: string
}

type CreateType = {
    name: string
}

export default function Tokens() {
    const { getAccessTokenSilently } = useAuth0()
    const [tokens, setTokens] = useState<IToken[]>([]);

    useEffect(() => {
        (async () => {
            const token = await getAccessTokenSilently();
            const res = await fetch("/api/usersettings/tokens", {
                method: "GET",
                headers: {
                    Authorization: `Bearer ${token}`
                }
            });

            const tokens = await res.json() as IToken[];
            setTokens(tokens);
        })();

    }, []);

    const [showCreateModal, setShowCreateModal] = useState(false);
    const { handleSubmit, register, formState: { errors } } = useForm<CreateType>();

    const onCreateClickHandler = async (data: CreateType) => {
        const token = await getAccessTokenSilently();

        const res = await fetch("/api/usersettings/tokens", {
            method: "POST",
            headers: {
                Authorization: `Bearer ${token}`,
                "content-type": "application/json"
            },
            body: JSON.stringify({
                name: data.name
            })
        });

        try {
            const pat = await res.json() as IToken;
            setTokens([...tokens, pat]);
        } catch (error) {
            toast.error('Something went wrong.')
        }
    }

    return (
        <section className="mt-4">
            <h2>Tokens</h2>
            <button className="bg-primary text-white px-2 py-1" onClick={() => setShowCreateModal(true)}>Create</button>
            <table className="table-fixed mt-2 w-full border border-gray-200 rounded-lg overflow-hidden">
                <thead className="bg-gray-50">
                    <tr>
                        <th className="w-[30%] px-4 py-2 text-left text-gray-700">Name</th>
                        <th className="w-[50%] px-4 py-2 text-left text-gray-700">Token</th>
                        <th className="w-[20%] px-4 py-2 text-left text-gray-700">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {tokens.map(t => (
                        <tr key={t.token} className="border-t hover:bg-gray-50">
                            <td className="px-4 py-2 truncate">{t.name}</td>
                            <td className="px-4 py-2 flex items-center gap-2 truncate">
                                {t.token}
                                <TextCopyButton text={t.token} />
                            </td>
                            <td className="px-4 py-2">
                                <div className="flex gap-2 justify-end">
                                    <button className="text-blue-600 hover:underline">Revoke</button>
                                    <button className="text-red-600 hover:underline">Delete</button>
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