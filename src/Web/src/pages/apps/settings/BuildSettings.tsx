import { useApiClient } from "@/hooks";
import { useContext, useEffect } from "react";
import { toast } from "react-toastify";
import { useForm } from "react-hook-form";
import { Alert, Button, HelperText, Label, TextInput, Tooltip } from "flowbite-react"
import InfoIcon from "@/components/ui/InfoIcon";
import { AppContext } from "..";


export default function BuildSettings() {
    const { get, post } = useApiClient();
    const { app } = useContext(AppContext)!;
    if (!app) throw new Error();

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
        <h6 className="font-semibold mb-3 text-sm">Build Settings</h6>
        <Alert color="warning" className="mb-4">
            Settings can’t be edited right now. Please try again shortly.
        </Alert>
        <form className="ps-2" onSubmit={handleSubmit(onSubmitConfig)}>
            <div className="flex items-start gap-4 py-2">
                <div className="flex items-center gap-2 w-40 pt-1">
                    <Label>Branch</Label>
                    <Tooltip content="The Git branch used for deployment.">
                        <InfoIcon />
                    </Tooltip>
                </div>
                <div className="flex-1">
                    <TextInput
                        sizing="sm"
                        {...register("branch", { required: "branch is required" })}
                        readOnly
                    />
                    {errors.branch && (
                        <HelperText color="failure">
                            {errors.branch.message}
                        </HelperText>
                    )}
                </div>
            </div>
            <div className="flex items-start gap-4 py-2">
                <div className="flex items-center gap-2 w-40 pt-1">
                    <Label>Build Directory</Label>
                    <Tooltip content="Path relative to the root of the repository where the build
                    is to be run.">
                        <InfoIcon />
                    </Tooltip>
                </div>
                <div className="flex-1">
                    <TextInput
                        {...register("directory", { required: "directory is required" })}
                        sizing="sm"
                        readOnly
                    />
                    {errors.directory && (
                        <HelperText color="failure">
                            {errors.directory.message}
                        </HelperText>
                    )}
                </div>
            </div>
            <div className="flex items-start gap-4 py-2">
                <div className="flex items-center gap-2 w-40 pt-1">
                    <Label>Output Directory</Label>
                    <Tooltip content="Path relative to the root of the repository where the build
                    output is located.">
                        <InfoIcon />
                    </Tooltip>
                </div>
                <div className="flex-1">
                    <TextInput
                        {...register("outDir", { required: "outDir is required" })}
                        sizing="sm"
                        readOnly
                    />
                    {errors.outDir && (
                        <HelperText color="failure">
                            {errors.outDir.message}
                        </HelperText>
                    )}
                </div>
            </div>

            <div className="flex items-start gap-4 py-2">
                <div className="flex items-center gap-2 w-40 pt-1">
                    <Label>Build Command</Label>
                    <Tooltip content="The command that runs your build process (e.g. npm run build).">
                        <InfoIcon />
                    </Tooltip>
                </div>
                <div className="flex-1">
                    <TextInput
                        {...register("buildCommand", { required: "buildCommand is required" })}
                        sizing="sm"
                        readOnly
                    />
                    {errors.buildCommand && (
                        <HelperText color="failure">
                            {errors.buildCommand.message}
                        </HelperText>
                    )}
                </div>
            </div>

            <div className="flex justify-end">
                <Button
                    type="submit"
                    disabled
                >
                    Save
                </Button>
            </div>
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