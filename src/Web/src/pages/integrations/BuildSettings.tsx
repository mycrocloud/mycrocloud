import { useApiClient } from "@/hooks";
import { useEffect } from "react";
import { useApp } from "../apps";
import { toast } from "react-toastify";
import { useForm } from "react-hook-form";
import { Alert, Spinner, Tooltip } from "flowbite-react"
import InfoIcon from "@/components/ui/InfoIcon";


export default function BuildSettings() {
    const { get, post } = useApiClient();
    const { app } = useApp();
    if (!app) return <Spinner aria-label="Loading..." />

    const {
        register,
        handleSubmit,
        formState: { errors },
        setValue,
    } = useForm<BuildConfig>({
        defaultValues: {
            branch: "default",
            directory: ".",
            buildCommand: "npm run build",
            outDir: "dist",
        },
    });

    useEffect(() => {
        (async () => {
            const config = await get<IBuildConfig>(`/api/apps/${app.id}/builds/config`);

            setValue("branch", config.branch);
            setValue("directory", config.directory);
            setValue("buildCommand", config.buildCommand);
            setValue("outDir", config.outDir);
        })();
    }, []);

    const onSubmitConfig = async (data: BuildConfig) => {
        await post(`/api/apps/${app.id}/builds/config`, data);
        toast.success("Build configuration saved");
    };

    return <div className="mt-4">
        <h2 className="font-semibold mb-3">Build Settings</h2>
        <Alert color="warning" className="mb-4">
            Settings can’t be edited right now. Please try again shortly.
        </Alert>
        <form className="ps-2" onSubmit={handleSubmit(onSubmitConfig)}>
            <div className="flex items-start gap-4 py-2">
                <div className="flex items-center gap-2 w-40 pt-1">
                    <label className="text-sm font-medium text-gray-700">Branch</label>
                    <Tooltip content="The Git branch used for deployment.">
                        <InfoIcon />
                    </Tooltip>
                </div>
                <div className="flex-1">
                    <input
                        {...register("branch", { required: "branch is required" })}
                        type="text"
                        className="border rounded px-2 py-1.5 w-full"
                        readOnly
                    />
                    {errors.branch && (
                        <p className="text-red-500 text-sm mt-1">
                            {errors.branch.message}
                        </p>
                    )}
                </div>
            </div>
            <div className="flex items-start gap-4 py-2">
                <div className="flex items-center gap-2 w-40 pt-1">
                    <label className="text-sm font-medium text-gray-700">Build Directory</label>
                    <Tooltip content="Path relative to the root of the repository where the build
                    is to be run.">
                        <InfoIcon />
                    </Tooltip>
                </div>
                <div className="flex-1">
                    <input
                        {...register("directory", { required: "directory is required" })}
                        type="text"
                        className="border rounded px-2 py-1.5 w-full"
                        readOnly
                    />
                    {errors.directory && (
                        <p className="text-red-500 text-sm mt-1">
                            {errors.directory.message}
                        </p>
                    )}
                </div>
            </div>
            <div className="flex items-start gap-4 py-2">
                <div className="flex items-center gap-2 w-40 pt-1">
                    <label className="text-sm font-medium text-gray-700">Output Directory</label>
                    <Tooltip content="Path relative to the root of the repository where the build
                    output is located.">
                        <InfoIcon />
                    </Tooltip>
                </div>
                <div className="flex-1">
                    <input
                        {...register("outDir", { required: "outDir is required" })}
                        type="text"
                        className="border rounded px-2 py-1.5 w-full"
                        readOnly
                    />
                    {errors.outDir && (
                        <p className="text-red-500 text-sm mt-1">
                            {errors.outDir.message}
                        </p>
                    )}
                </div>
            </div>

            <div className="flex items-start gap-4 py-2">
                <div className="flex items-center gap-2 w-40 pt-1">
                    <label className="text-sm font-medium text-gray-700">Build Command</label>
                    <Tooltip content="The command that runs your build process (e.g. npm run build).">
                        <InfoIcon />
                    </Tooltip>
                </div>
                <div className="flex-1">
                    <input
                        {...register("buildCommand", { required: "buildCommand is required" })}
                        type="text"
                        className="border rounded px-2 py-1.5 w-full"
                        readOnly
                    />
                    {errors.buildCommand && (
                        <p className="text-red-500 text-sm mt-1">
                            {errors.buildCommand.message}
                        </p>
                    )}
                </div>
            </div>

            <button
                type="submit"
                className="mt-2 bg-primary px-4 py-1.5 text-white rounded"
                disabled
            >
                Save
            </button>
        </form>
    </div>
}

interface IBuildConfig {
    branch: string;
    directory: string;
    buildCommand: string;
    outDir: string;
}

type BuildConfig = {
    branch: string;
    directory: string;
    buildCommand: string;
    outDir: string;
};