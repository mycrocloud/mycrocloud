import { useContext, useEffect, useRef, useState } from "react";
import { AppContext } from "..";
import { useAuth0 } from "@auth0/auth0-react";
import Ajv, { JSONSchemaType } from "ajv";
import { toast } from "react-toastify";
import { Button, HelperText } from "flowbite-react";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";

interface CorsSettings {
    allowedHeaders?: string[];
    allowedMethods?: string[];
    allowedOrigins?: string[];
    exposeHeaders?: string[];
    maxAgeSeconds?: number;
}

const corsSettingsSchema: JSONSchemaType<CorsSettings> = {
    type: "object",
    properties: {
        allowedHeaders: {
            type: "array",
            nullable: true,
            items: { type: "string" },
        },
        allowedMethods: {
            type: "array",
            nullable: true,
            items: { type: "string" },
        },
        allowedOrigins: {
            type: "array",
            nullable: true,
            items: { type: "string" },
        },
        exposeHeaders: { type: "array", nullable: true, items: { type: "string" } },
        maxAgeSeconds: { type: "number", nullable: true },
    },
    additionalProperties: false,
};

export default function CorsSettingsSection() {
    const { app } = useContext(AppContext)!;
    if (!app) throw new Error();
    const { getAccessTokenSilently } = useAuth0();

    const editorElRef = useRef(null);
    const editor = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);
    const [error, setError] = useState<string>();

    useEffect(() => {
        editor.current?.dispose();

        editor.current = monaco.editor.create(editorElRef.current!, {
            language: "json",
            value: "",
            minimap: { enabled: false },
        });

        return () => {
            editor.current?.dispose();
        };
    }, []);

    useEffect(() => {
        if (!editor.current) {
            return;
        }
        const fetchCorsSettings = async () => {
            const accessToken = await getAccessTokenSilently();
            const res = await fetch(`/api/apps/${app.id}/cors`, {
                headers: { Authorization: `Bearer ${accessToken}` },
            });
            if (res.ok) {
                const json = await res.json();
                editor.current!.setValue(JSON.stringify(json, null, 2));
            }
        };

        fetchCorsSettings();
    }, [editor.current]);

    const handleSaveClick = async () => {
        if (!editor.current) return;
        if (error) {
            setError(undefined);
        }
        const json = editor.current.getValue();
        let data;
        try {
            data = JSON.parse(json);
        } catch (e) {
            setError("Invalid JSON");
            return;
        }
        const ajv = new Ajv();
        const validate = ajv.compile(corsSettingsSchema);
        const valid = validate(data);
        if (!valid) {
            setError(validate.errors?.[0].message!);
            return;
        }
        const accessToken = await getAccessTokenSilently();
        const res = await fetch(`/api/apps/${app.id}/cors`, {
            method: "PATCH",
            headers: {
                "content-type": "application/json",
                Authorization: `Bearer ${accessToken}`,
            },
            body: json,
        });
        if (res.ok) {
            toast("CORS settings saved");
        }
    };
    return (
        <section className="space-y-4 rounded-lg border border-slate-200 bg-white p-4">
            {/* Section header */}
            <header>
                <h3 className="text-base font-semibold">
                    CORS
                </h3>
            </header>

            {/* Section body */}
            <div className="space-y-2">
                <div
                    ref={editorElRef}
                    className="h-40 w-full rounded border border-slate-300"
                />

                {error && (
                    <HelperText color="failure">
                        {error}
                    </HelperText>
                )}
            </div>

            {/* Section footer */}
            <footer className="flex justify-end">
                <Button
                    type="button"
                    onClick={handleSaveClick}
                >
                    Save
                </Button>
            </footer>
        </section>

    );
}