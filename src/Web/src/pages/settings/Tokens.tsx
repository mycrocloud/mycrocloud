import { useAuth0 } from "@auth0/auth0-react";
import { useEffect, useState } from "react"
import { toast } from "react-toastify";
import TextCopyButton from "../../components/ui/TextCopyButton";

interface IToken {
    name: string
    token: string
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

    const onCreateClickHandler = async () => {
        const token = await getAccessTokenSilently();

        const res = await fetch("/api/usersettings/tokens", {
            method: "POST",
            headers: {
                Authorization: `Bearer ${token}`,
                "content-type": "application/json"
            },
            body: JSON.stringify({
                name: `Token_${new Date().toLocaleString()}` //TODO: input form
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
            <button className="bg-primary text-white px-2 py-1" onClick={onCreateClickHandler}>Create</button>
            <table className="table-auto">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Token</th>
                    </tr>
                </thead>
                <tbody>
                    {tokens.map(t => {
                        return (
                            <tr key={t.token}>
                                <td>{t.name}</td>
                                <td>{t.token}<TextCopyButton text={t.token} /></td>
                            </tr>
                        )
                    })}
                </tbody>
            </table>
        </section>
    )
}