import { useEffect, useState } from "react"
import { toast } from "react-toastify";
import TextCopyButton from "../../components/ui/TextCopyButton";
import { useForm } from "react-hook-form";
import { useApiClient } from "@/hooks";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
                <Button size="sm" onClick={() => setShowCreateModal(true)}>Create</Button>
            </div>
            <Table className="mt-2">
                <TableHeader>
                    <TableRow>
                        <TableHead className="w-[30%]">Name</TableHead>
                        <TableHead className="w-[50%]">Token</TableHead>
                        <TableHead className="w-[5%]">Status</TableHead>
                        <TableHead className="w-[15%]">Actions</TableHead>
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {tokens.map(t => (
                        <TableRow key={t.id}>
                            <TableCell className="truncate">{t.name}</TableCell>
                            <TableCell className="flex items-center gap-2 truncate">
                                {t.token}
                                <TextCopyButton text={t.token} />
                            </TableCell>
                            <TableCell>{t.status == "None" ? "-" : t.status}</TableCell>
                            <TableCell>
                                <div className="flex gap-2 justify-end">
                                    <Button variant="link" onClick={() => revoke(t.id)} className="text-blue-600 p-0 h-auto">Revoke</Button>
                                    <Button variant="link" onClick={() => _delete(t.id)} className="text-red-600 p-0 h-auto">Delete</Button>
                                </div>
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
            <Dialog open={showCreateModal} onOpenChange={setShowCreateModal}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Generate new token</DialogTitle>
                    </DialogHeader>
                    <form>
                        <Input
                            {...register("name", {
                                required: "Name is required",
                                maxLength: {
                                    value: 50,
                                    message: "Name cannot exceed 50 characters",
                                }
                            })}
                            placeholder="Token name"
                        />
                        {errors.name && <span className="text-red-500 text-sm mt-1">{errors.name.message}</span>}
                    </form>
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setShowCreateModal(false)}>
                            Cancel
                        </Button>
                        <Button onClick={handleSubmit(onCreateClickHandler)}>
                            Create
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </section>
    )
}